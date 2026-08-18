namespace InterviewTracker.Models;

/// <summary>
/// Immutable result returned by JobMatchService for a single job application.
/// All calculation happens in the service; this DTO is pure data.
/// </summary>
public class JobMatchResult
{
    /// <summary>Weighted score from 0 to 100.</summary>
    public int Score { get; init; }

    /// <summary>Human-readable category label.</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>Bootstrap colour name for the badge (e.g. "success", "warning").</summary>
    public string Color { get; init; } = string.Empty;

    public List<TechMatchDetail> StrongMatches   { get; init; } = [];
    public List<TechMatchDetail> PartialMatches  { get; init; } = [];
    public List<TechMatchDetail> MissingSkills   { get; init; } = [];

    /// <summary>True when the application has no technology requirements at all.</summary>
    public bool HasRequirements { get; init; }
}

/// <summary>Per-technology breakdown included in a JobMatchResult.</summary>
public class TechMatchDetail
{
    public string TechnologyName      { get; init; } = string.Empty;
    public string Category            { get; init; } = string.Empty;

    /// <summary>Required years from the job posting (null = not specified).</summary>
    public decimal? RequiredYears     { get; init; }

    /// <summary>My years of experience (null = not in my profile).</summary>
    public decimal? MyYears           { get; init; }

    /// <summary>0.0 – 1.0 individual technology score.</summary>
    public double TechScore           { get; init; }

    public string RequirementLevel    { get; init; } = string.Empty;
    public bool IsMandatory           { get; init; }
}
