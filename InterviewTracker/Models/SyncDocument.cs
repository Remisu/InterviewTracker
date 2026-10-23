using System.Text.Json.Serialization;

namespace InterviewTracker.Models;

public class SyncDocument
{
    [JsonRequired] public int FormatVersion { get; set; } = 1;
    [JsonRequired] public DateTime ExportedAtUtc { get; set; } = DateTime.UtcNow;
    [JsonRequired] public Guid SourceInstallationId { get; set; }
    [JsonRequired] public List<SyncApplication> Applications { get; set; } = [];
}

// The application is the sync unit: accepting a version includes all of its related data.
// Numeric IDs are references inside this aggregate only; SyncId is the permanent identity.
public class SyncApplication
{
    [JsonRequired] public JobApplication Application { get; set; } = new();
    [JsonRequired] public List<Contact> Contacts { get; set; } = [];
    [JsonRequired] public List<Interview> Interviews { get; set; } = [];
    [JsonRequired] public List<InterviewQuestion> Questions { get; set; } = [];
    [JsonRequired] public List<ApplicationActivity> Activities { get; set; } = [];
    [JsonRequired] public List<JobTechnologyRequirement> Requirements { get; set; } = [];
    [JsonRequired] public List<Technology> Technologies { get; set; } = [];
}

public enum MergeAction { Add, Update, Unchanged, Conflict }
public enum ConflictResolution { Unresolved, KeepLocal, UseImported }

public class SyncPreviewItem
{
    public required SyncApplication Imported { get; init; }
    public SyncApplication? Local { get; init; }
    public MergeAction Action { get; init; }
    public ConflictResolution Resolution { get; set; }
    public required string LocalFingerprint { get; init; }
}

public class SyncPreview
{
    public required SyncDocument Document { get; init; }
    public required List<SyncPreviewItem> Items { get; init; }
    public int Count(MergeAction action) => Items.Count(i => i.Action == action);
}

public record SyncSummary(int Added, int Updated, int Unchanged);
