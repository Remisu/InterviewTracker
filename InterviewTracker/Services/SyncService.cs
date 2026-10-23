using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using InterviewTracker.Data;
using InterviewTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace InterviewTracker.Services;

public class SyncService(IDbContextFactory<AppDbContext> factory)
{
    public const long MaxFileBytes = 20 * 1024 * 1024;
    public static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(info =>
        {
            if (info.Type.Namespace != typeof(JobApplication).Namespace || info.Type.Name.StartsWith("Sync")) return;
            // Export only persisted scalars. Navigation graphs are explicit in SyncApplication.
            foreach (var property in info.Properties.ToList())
                if (property.Set is null || (!property.PropertyType.IsValueType && property.PropertyType != typeof(string)))
                    info.Properties.Remove(property);
        });
        return new JsonSerializerOptions
        {
            TypeInfoResolver = resolver,
            WriteIndented = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            Converters = { new JsonStringEnumConverter(allowIntegerValues: false) }
        };
    }

    public async Task<string> ExportAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        // One consistent database snapshot, including the installation identifier.
        await using var transaction = await db.Database.BeginTransactionAsync();
        var installation = await db.SyncInstallations.SingleAsync();
        var records = await ReadAllAsync(db);
        var document = new SyncDocument
        {
            SourceInstallationId = installation.InstallationId,
            Applications = records.Values.OrderBy(a => a.Application.SyncId).ToList()
        };
        await transaction.CommitAsync();
        return JsonSerializer.Serialize(document, JsonOptions);
    }

    public static SyncDocument Parse(string json)
    {
        if (Encoding.UTF8.GetByteCount(json) > MaxFileBytes) throw new ValidationException("The file exceeds the 20 MB limit.");
        try
        {
            using var raw = JsonDocument.Parse(json);
            // Entity initializers must never manufacture missing sync identities or clocks.
            foreach (var record in raw.RootElement.GetProperty("Applications").EnumerateArray())
            {
                RequireScalars(record.GetProperty("Application"), typeof(JobApplication));
                foreach (var (name, type) in new[] {
                    ("Contacts", typeof(Contact)), ("Interviews", typeof(Interview)),
                    ("Questions", typeof(InterviewQuestion)), ("Activities", typeof(ApplicationActivity)),
                    ("Requirements", typeof(JobTechnologyRequirement)), ("Technologies", typeof(Technology)) })
                    foreach (var entity in record.GetProperty(name).EnumerateArray()) RequireScalars(entity, type);
            }
            var document = JsonSerializer.Deserialize<SyncDocument>(json, JsonOptions)
                ?? throw new ValidationException("The file contains no document.");
            Validate(document);
            // Detach from the JSON's ordering and normalize internal numeric references.
            document.Applications = document.Applications.Select(Canonicalize).ToList();
            return document;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException or ArgumentException)
        {
            throw new ValidationException("Invalid synchronization JSON. Select an InterviewTracker export file.", ex);
        }
    }

    private static void RequireScalars(JsonElement entity, Type type)
    {
        foreach (var property in JsonOptions.GetTypeInfo(type).Properties)
            if (!entity.TryGetProperty(property.Name, out _))
                throw new ValidationException($"{type.Name} is missing {property.Name}.");
    }

    public async Task<SyncPreview> PreviewAsync(string json)
    {
        var document = Parse(json);
        await using var db = await factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var local = await ReadAllAsync(db);
        var items = document.Applications.Select(imported =>
        {
            local.TryGetValue(imported.Application.SyncId, out var existing);
            var action = existing is null ? MergeAction.Add
                : imported.Application.UpdatedAt > existing.Application.UpdatedAt ? MergeAction.Update
                : imported.Application.UpdatedAt < existing.Application.UpdatedAt ? MergeAction.Unchanged
                : Fingerprint(imported) == Fingerprint(existing) ? MergeAction.Unchanged : MergeAction.Conflict;
            return new SyncPreviewItem
            {
                Imported = imported, Local = existing, Action = action,
                LocalFingerprint = existing is null ? "" : Fingerprint(existing)
            };
        }).ToList();
        return new SyncPreview { Document = document, Items = items };
    }

    public async Task<SyncSummary> MergeAsync(SyncPreview preview)
    {
        // Validate again at the service boundary, independent of the UI.
        Validate(preview.Document);
        if (preview.Items.Any(i => i.Action == MergeAction.Conflict && i.Resolution == ConflictResolution.Unresolved))
            throw new ValidationException("Resolve every conflict before confirming the merge.");
        await using var db = await factory.CreateDbContextAsync();
        db.PreserveSyncTimestamps = true;
        await using var transaction = await db.Database.BeginTransactionAsync();
        var current = await ReadAllAsync(db);
        foreach (var item in preview.Items)
        {
            var fingerprint = current.TryGetValue(item.Imported.Application.SyncId, out var local) ? Fingerprint(local) : "";
            if (fingerprint != item.LocalFingerprint)
                throw new ValidationException("Local data changed after the preview. Select the file again to review a fresh preview.");
        }
        int added = 0, updated = 0, unchanged = 0;
        foreach (var item in preview.Items)
        {
            if (item.Action == MergeAction.Unchanged ||
                item.Action == MergeAction.Conflict && item.Resolution == ConflictResolution.KeepLocal)
            {
                // Persist the explicit local decision with a newer clock so future syncs converge.
                if (item.Action == MergeAction.Conflict)
                {
                    var local = await db.JobApplications.SingleAsync(a => a.SyncId == item.Imported.Application.SyncId);
                    local.UpdatedAt = LaterThan(local.UpdatedAt);
                    await db.SaveChangesAsync();
                }
                unchanged++;
                continue;
            }
            var record = Clone(item.Imported);
            // A resolution is a new modification. Activity dates remain unchanged.
            if (item.Action == MergeAction.Conflict) record.Application.UpdatedAt = LaterThan(record.Application.UpdatedAt);
            await ApplyAsync(db, record);
            if (item.Action == MergeAction.Add) added++; else updated++;
        }
        await transaction.CommitAsync();
        return new SyncSummary(added, updated, unchanged);
    }

    private static DateTime LaterThan(DateTime timestamp) =>
        DateTime.UtcNow > timestamp ? DateTime.UtcNow : timestamp.AddTicks(1);

    private static async Task<Dictionary<Guid, SyncApplication>> ReadAllAsync(AppDbContext db)
    {
        var applications = await db.JobApplications.AsNoTracking()
            .Include(a => a.Contacts)
            .Include(a => a.Interviews).ThenInclude(i => i.Questions)
            .Include(a => a.Activities)
            .Include(a => a.TechnologyRequirements).ThenInclude(r => r.Technology)
            .AsSplitQuery().ToListAsync();
        return applications.ToDictionary(a => a.SyncId, a => Canonicalize(new SyncApplication
        {
            Application = a, Contacts = a.Contacts.ToList(), Interviews = a.Interviews.ToList(),
            Questions = a.Interviews.SelectMany(i => i.Questions).ToList(), Activities = a.Activities.ToList(),
            Requirements = a.TechnologyRequirements.ToList(),
            Technologies = a.TechnologyRequirements.Select(r => r.Technology).DistinctBy(t => t.Id).ToList()
        }));
    }

    private static SyncApplication Clone(SyncApplication value) =>
        JsonSerializer.Deserialize<SyncApplication>(JsonSerializer.Serialize(value, JsonOptions), JsonOptions)!;

    // Canonical IDs make content comparisons independent of SQLite auto-increment IDs on each computer.
    private static SyncApplication Canonicalize(SyncApplication value)
    {
        var record = Clone(value);
        var contacts = record.Contacts.OrderBy(c => c.Id).Select((c, i) => (c.Id, NewId: i + 1)).ToDictionary(x => x.Id, x => x.NewId);
        var interviews = record.Interviews.OrderBy(i => i.Id).Select((c, i) => (c.Id, NewId: i + 1)).ToDictionary(x => x.Id, x => x.NewId);
        var technologies = record.Technologies.OrderBy(t => t.Name, StringComparer.Ordinal).Select((c, i) => (c.Id, NewId: i + 1)).ToDictionary(x => x.Id, x => x.NewId);
        record.Application.Id = 1;
        record.Application.CreatedAt = Utc(record.Application.CreatedAt);
        record.Application.UpdatedAt = Utc(record.Application.UpdatedAt);
        if (record.Application.LastActivityDate is DateTime last) record.Application.LastActivityDate = Utc(last);
        foreach (var c in record.Contacts) { c.Id = contacts[c.Id]; c.JobApplicationId = 1; c.CreatedAt = Utc(c.CreatedAt); }
        foreach (var i in record.Interviews)
        {
            i.Id = interviews[i.Id]; i.JobApplicationId = 1;
            if (i.InterviewerContactId is int id) i.InterviewerContactId = contacts[id];
            i.CreatedAt = Utc(i.CreatedAt); i.UpdatedAt = Utc(i.UpdatedAt);
        }
        foreach (var q in record.Questions) { q.InterviewId = interviews[q.InterviewId]; q.CreatedAt = Utc(q.CreatedAt); }
        foreach (var a in record.Activities)
        {
            a.JobApplicationId = 1;
            if (a.ContactId is int c) a.ContactId = contacts[c];
            if (a.InterviewId is int i) a.InterviewId = interviews[i];
            a.CreatedAt = Utc(a.CreatedAt); a.UpdatedAt = Utc(a.UpdatedAt); a.ActivityDate = Utc(a.ActivityDate);
        }
        foreach (var r in record.Requirements) { r.JobApplicationId = 1; r.TechnologyId = technologies[r.TechnologyId]; }
        foreach (var t in record.Technologies) t.Id = technologies[t.Id];
        record.Contacts = record.Contacts.OrderBy(c => c.Id).ToList();
        record.Interviews = record.Interviews.OrderBy(i => i.Id).ToList();
        record.Questions = record.Questions.OrderBy(q => q.Id).ToList();
        record.Activities = record.Activities.OrderBy(a => a.Id).ToList();
        record.Requirements = record.Requirements.OrderBy(r => r.TechnologyId).ToList();
        record.Technologies = record.Technologies.OrderBy(t => t.Id).ToList();
        for (int i = 0; i < record.Questions.Count; i++) record.Questions[i].Id = i + 1;
        for (int i = 0; i < record.Activities.Count; i++) record.Activities[i].Id = i + 1;
        for (int i = 0; i < record.Requirements.Count; i++) record.Requirements[i].Id = i + 1;
        return record;
    }

    private static DateTime Utc(DateTime value) => value.Kind == DateTimeKind.Local
        ? value.ToUniversalTime() : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    public static string Fingerprint(SyncApplication value)
    {
        var canonical = Canonicalize(value);
        // Technology metadata belongs to the shared local catalog, not an application version.
        // Names identify references; importing never overwrites local catalog descriptions/profile.
        foreach (var technology in canonical.Technologies)
        {
            technology.Category = default;
            technology.Description = null;
            technology.IsActive = true;
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(canonical, JsonOptions))));
    }

    private static void Validate(SyncDocument document)
    {
        if (document.FormatVersion != 1) throw new ValidationException($"Unsupported file version {document.FormatVersion}. This app supports version 1.");
        if (document.SourceInstallationId == Guid.Empty || document.ExportedAtUtc.Kind != DateTimeKind.Utc)
            throw new ValidationException("The export must have an installation ID and UTC export timestamp.");
        if (document.Applications is null || document.Applications.Count > 10000)
            throw new ValidationException("The export must contain an application list (up to 10,000 records).");
        var ids = new HashSet<Guid>();
        foreach (var r in document.Applications)
        {
            if (r is null || r.Application is null || r.Contacts is null || r.Interviews is null || r.Questions is null ||
                r.Activities is null || r.Requirements is null || r.Technologies is null)
                throw new ValidationException("An application is missing its related data lists.");
            var app = r.Application;
            if (app.SyncId == Guid.Empty || !ids.Add(app.SyncId)) throw new ValidationException("Application IDs must be unique non-empty GUIDs.");
            if (app.CreatedAt.Kind != DateTimeKind.Utc || app.UpdatedAt.Kind != DateTimeKind.Utc ||
                app.CreatedAt == default || app.UpdatedAt < app.CreatedAt || app.UpdatedAt == DateTime.MaxValue)
                throw new ValidationException("Applications must have valid UTC creation and modification timestamps.");
            var all = new object[] { app }.Concat(r.Contacts).Concat(r.Interviews).Concat(r.Questions)
                .Concat(r.Activities).Concat(r.Requirements).Concat(r.Technologies);
            foreach (var entity in all)
            {
                if (entity is null) throw new ValidationException("Related records cannot be null.");
                Validator.ValidateObject(entity, new ValidationContext(entity), true);
                foreach (var property in entity.GetType().GetProperties().Where(p => p.PropertyType.IsEnum))
                    if (!Enum.IsDefined(property.PropertyType, property.GetValue(entity)!))
                        throw new ValidationException($"Invalid value for {property.Name}.");
            }
            var contacts = CheckIds(r.Contacts.Select(c => c.Id));
            var interviews = CheckIds(r.Interviews.Select(i => i.Id));
            CheckIds(r.Questions.Select(q => q.Id)); CheckIds(r.Activities.Select(a => a.Id));
            CheckIds(r.Requirements.Select(x => x.Id));
            var technologies = CheckIds(r.Technologies.Select(t => t.Id));
            bool BadContact(int? id) => id.HasValue && !contacts.Contains(id.Value);
            bool BadInterview(int? id) => id.HasValue && !interviews.Contains(id.Value);
            if (app.Id <= 0 || r.Contacts.Any(c => c.JobApplicationId != app.Id) ||
                r.Interviews.Any(i => i.JobApplicationId != app.Id || BadContact(i.InterviewerContactId)) ||
                r.Questions.Any(q => !interviews.Contains(q.InterviewId)) ||
                r.Activities.Any(a => a.JobApplicationId != app.Id || BadContact(a.ContactId) || BadInterview(a.InterviewId)) ||
                r.Requirements.Any(x => x.JobApplicationId != app.Id || !technologies.Contains(x.TechnologyId)))
                throw new ValidationException("Related records contain invalid application, contact, interview, or technology references.");
            if (r.Requirements.Select(x => x.TechnologyId).Distinct().Count() != r.Requirements.Count ||
                r.Technologies.Select(t => t.Name).Distinct(StringComparer.Ordinal).Count() != r.Technologies.Count)
                throw new ValidationException("Duplicate technology requirements or names are not allowed.");
        }
    }

    private static HashSet<int> CheckIds(IEnumerable<int> values)
    {
        var list = values.ToList();
        if (list.Any(i => i <= 0) || list.Distinct().Count() != list.Count)
            throw new ValidationException("Related records must have unique positive reference IDs.");
        return list.ToHashSet();
    }

    private static async Task ApplyAsync(AppDbContext db, SyncApplication record)
    {
        var app = await db.JobApplications.SingleOrDefaultAsync(a => a.SyncId == record.Application.SyncId);
        if (app is null)
        {
            app = record.Application;
            app.Id = 0;
            db.JobApplications.Add(app);
            await db.SaveChangesAsync();
        }
        else
        {
            var id = app.Id;
            // Delete only this accepted aggregate's children, inside the surrounding transaction.
            await db.ApplicationActivities.Where(a => a.JobApplicationId == id).ExecuteDeleteAsync();
            await db.InterviewQuestions.Where(q => q.Interview.JobApplicationId == id).ExecuteDeleteAsync();
            await db.Interviews.Where(i => i.JobApplicationId == id).ExecuteDeleteAsync();
            await db.Contacts.Where(c => c.JobApplicationId == id).ExecuteDeleteAsync();
            await db.JobTechnologyRequirements.Where(r => r.JobApplicationId == id).ExecuteDeleteAsync();
            record.Application.Id = id;
            db.Entry(app).CurrentValues.SetValues(record.Application);
            await db.SaveChangesAsync();
        }
        var contacts = new Dictionary<int, Contact>();
        foreach (var c in record.Contacts)
        {
            var oldId = c.Id;
            c.Id = 0; c.JobApplicationId = app.Id;
            db.Contacts.Add(c); contacts.Add(oldId, c);
        }
        await db.SaveChangesAsync();
        var interviews = new Dictionary<int, Interview>();
        foreach (var i in record.Interviews)
        {
            var oldId = i.Id;
            i.Id = 0; i.JobApplicationId = app.Id;
            i.InterviewerContactId = i.InterviewerContactId is int c ? contacts[c].Id : null;
            db.Interviews.Add(i); interviews.Add(oldId, i);
        }
        await db.SaveChangesAsync();
        foreach (var q in record.Questions)
        {
            q.Id = 0; q.InterviewId = interviews[q.InterviewId].Id;
            db.InterviewQuestions.Add(q);
        }
        foreach (var a in record.Activities)
        {
            a.Id = 0; a.JobApplicationId = app.Id;
            a.ContactId = a.ContactId is int c ? contacts[c].Id : null;
            a.InterviewId = a.InterviewId is int i ? interviews[i].Id : null;
            db.ApplicationActivities.Add(a);
        }
        foreach (var r in record.Requirements)
        {
            var imported = record.Technologies.Single(t => t.Id == r.TechnologyId);
            // Shared catalog entries are reused by exact name; local descriptions/profile are preserved.
            var technology = await db.Technologies.SingleOrDefaultAsync(t => t.Name == imported.Name);
            if (technology is null)
            {
                technology = new Technology { Name = imported.Name, Category = imported.Category,
                    Description = imported.Description, IsActive = imported.IsActive };
                db.Technologies.Add(technology);
                await db.SaveChangesAsync();
            }
            r.Id = 0; r.JobApplicationId = app.Id; r.TechnologyId = technology.Id;
            db.JobTechnologyRequirements.Add(r);
        }
        await db.SaveChangesAsync();
    }
}
