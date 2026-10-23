using InterviewTracker.Data;
using InterviewTracker.Models;
using InterviewTracker.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace InterviewTracker.Services;

public class ApplicationService(IDbContextFactory<AppDbContext> factory)
{
    // ── Queries ───────────────────────────────────────────────────────────────

    public async Task<List<JobApplication>> GetAllAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.JobApplications
            .Include(a => a.TechnologyRequirements)
                .ThenInclude(r => r.Technology)
            .Include(a => a.Interviews)
            .OrderByDescending(a => a.UpdatedAt)
            .ToListAsync();
    }

    public async Task<JobApplication?> GetByIdAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.JobApplications
            .Include(a => a.Contacts)
            .Include(a => a.TechnologyRequirements)
                .ThenInclude(r => r.Technology)
            .Include(a => a.Interviews)
                .ThenInclude(i => i.Questions)
            .Include(a => a.Activities)
                .ThenInclude(act => act.Contact)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<List<JobApplication>> GetFilteredAsync(ApplicationFilter filter)
    {
        await using var db = await factory.CreateDbContextAsync();
        var q = db.JobApplications
            .Include(a => a.TechnologyRequirements)
                .ThenInclude(r => r.Technology)
            .Include(a => a.Interviews)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Company))
            q = q.Where(a => a.ClientCompany.Contains(filter.Company)
                           || (a.RecruitingCompany != null && a.RecruitingCompany.Contains(filter.Company)));

        if (!string.IsNullOrWhiteSpace(filter.JobTitle))
            q = q.Where(a => a.JobTitle.Contains(filter.JobTitle));

        if (filter.Stage.HasValue)
            q = q.Where(a => a.Stage == filter.Stage.Value);

        if (filter.Priority.HasValue)
            q = q.Where(a => a.Priority == filter.Priority.Value);

        if (filter.WorkMode.HasValue)
            q = q.Where(a => a.WorkMode == filter.WorkMode.Value);

        if (!string.IsNullOrWhiteSpace(filter.Country))
            q = q.Where(a => a.Country != null && a.Country.Contains(filter.Country));

        if (filter.TechnologyId.HasValue)
            q = q.Where(a => a.TechnologyRequirements.Any(r => r.TechnologyId == filter.TechnologyId.Value));

        if (filter.HasUpcomingInterview)
            q = q.Where(a => a.Interviews.Any(i =>
                i.Status == InterviewStatus.Scheduled && i.ScheduledDate >= DateTime.UtcNow));

        if (filter.NeedsFollowUp)
            q = q.Where(a => a.NextFollowUpDate.HasValue && a.NextFollowUpDate.Value.Date <= DateTime.UtcNow.Date);

        return await q.OrderByDescending(a => a.UpdatedAt).ToListAsync();
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public async Task<JobApplication> CreateAsync(JobApplication app)
    {
        Validator.ValidateObject(app, new ValidationContext(app), validateAllProperties: true);
        await using var db = await factory.CreateDbContextAsync();
        app.Id = 0;
        app.CreatedAt = app.UpdatedAt = DateTime.UtcNow;
        app.LastActivityDate = app.AppliedDate?.Date.ToUniversalTime() ?? DateTime.UtcNow;
        app.Activities.Add(new ApplicationActivity
        {
            ActivityType = ActivityType.Applied, Title = "Applied",
            ActivityDate = app.LastActivityDate.Value, IsManual = false
        });
        db.JobApplications.Add(app);
        try { await db.SaveChangesAsync(); }
        catch { app.Activities.Clear(); app.Id = 0; throw; }
        return app;
    }

    public async Task UpdateAsync(JobApplication app)
    {
        Validator.ValidateObject(app, new ValidationContext(app), validateAllProperties: true);
        await using var db = await factory.CreateDbContextAsync();
        var existing = await db.JobApplications.FindAsync(app.Id)
            ?? throw new InvalidOperationException("This application no longer exists. Refresh and try again.");

        var oldStage = existing.Stage;

        existing.JobTitle = app.JobTitle;
        existing.ClientCompany = app.ClientCompany;
        existing.RecruitingCompany = app.RecruitingCompany;
        existing.JobDescription = app.JobDescription;
        existing.JobUrl = app.JobUrl;
        existing.JobReference = app.JobReference;
        existing.City = app.City;
        existing.Country = app.Country;
        existing.WorkMode = app.WorkMode;
        existing.EmploymentType = app.EmploymentType;
        existing.SalaryMin = app.SalaryMin;
        existing.SalaryMax = app.SalaryMax;
        existing.Currency = app.Currency;
        existing.RelocationRequired = app.RelocationRequired;
        existing.VisaSponsorshipAvailable = app.VisaSponsorshipAvailable;
        existing.JapaneseLevelRequired = app.JapaneseLevelRequired;
        existing.EnglishLevelRequired = app.EnglishLevelRequired;
        existing.Stage = app.Stage;
        existing.Priority = app.Priority;
        existing.AppliedDate = app.AppliedDate;
        existing.NextFollowUpDate = app.NextFollowUpDate;
        existing.Notes = app.Notes;

        if (app.Stage != oldStage)
        {
            existing.LastActivityDate = DateTime.UtcNow;
            AddAutoActivity(db, app.Id, ActivityType.StatusChanged,
                $"Stage changed to {EnumDisplayName(app.Stage)}", null);
        }

        await db.SaveChangesAsync();
    }

    public async Task UpdateStageAsync(int id, ApplicationStage stage)
    {
        await using var db = await factory.CreateDbContextAsync();
        var app = await db.JobApplications.FindAsync(id);
        if (!Helpers.ApplicationWorkflow.Stages.Contains(stage))
            throw new ValidationException("Choose a stage from the current workflow.");
        if (app is null) throw new InvalidOperationException("This application no longer exists.");
        if (app.Stage == stage) return;
        app.Stage = stage;
        app.LastActivityDate = DateTime.UtcNow;
        AddAutoActivity(db, id, ActivityType.StatusChanged,
            $"Stage changed to {EnumDisplayName(stage)}", null);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        var app = await db.JobApplications.FindAsync(id);
        if (app is null) return;
        db.JobApplications.Remove(app);
        await db.SaveChangesAsync();
    }

    // ── Technology Requirements ───────────────────────────────────────────────

    public async Task<List<JobTechnologyRequirement>> GetRequirementsAsync(int appId)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.JobTechnologyRequirements
            .Include(r => r.Technology)
            .Where(r => r.JobApplicationId == appId)
            .OrderBy(r => r.RequirementLevel)
            .ThenBy(r => r.Technology.Name)
            .ToListAsync();
    }

    public async Task<JobTechnologyRequirement> AddRequirementAsync(JobTechnologyRequirement req)
    {
        await using var db = await factory.CreateDbContextAsync();
        db.JobTechnologyRequirements.Add(req);
        await db.SaveChangesAsync();
        return req;
    }

    public async Task UpdateRequirementAsync(JobTechnologyRequirement req)
    {
        await using var db = await factory.CreateDbContextAsync();
        db.JobTechnologyRequirements.Update(req);
        await db.SaveChangesAsync();
    }

    public async Task DeleteRequirementAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        var req = await db.JobTechnologyRequirements.FindAsync(id);
        if (req is null) return;
        db.JobTechnologyRequirements.Remove(req);
        await db.SaveChangesAsync();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void AddAutoActivity(AppDbContext db, int appId, ActivityType type, string title, string? description)
    {
        db.ApplicationActivities.Add(new ApplicationActivity
        {
            JobApplicationId = appId,
            ActivityType     = type,
            Title            = title,
            Description      = description,
            ActivityDate     = DateTime.UtcNow,
            IsManual         = false
        });
    }

    private static string EnumDisplayName(ApplicationStage stage) =>
        Helpers.EnumHelper.DisplayName(stage);
}

// ── Filter DTO ────────────────────────────────────────────────────────────────

public class ApplicationFilter
{
    public string? Company        { get; set; }
    public string? JobTitle       { get; set; }
    public ApplicationStage? Stage   { get; set; }
    public Priority? Priority        { get; set; }
    public WorkMode? WorkMode        { get; set; }
    public string? Country           { get; set; }
    public int? TechnologyId         { get; set; }
    public bool HasUpcomingInterview { get; set; }
    public bool NeedsFollowUp        { get; set; }
}
