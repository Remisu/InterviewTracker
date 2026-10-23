using InterviewTracker.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace InterviewTracker.Models;

public class JobApplication : IValidatableObject
{
    public int Id { get; set; }

    public Guid SyncId { get; set; } = Guid.NewGuid();

    // Company / Position
    [Required, MaxLength(200)]
    public string JobTitle { get; set; } = string.Empty;

    [MaxLength(200)]
    public string ClientCompany { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? RecruitingCompany { get; set; }

    [MaxLength(2000)]
    public string? JobDescription { get; set; }

    [MaxLength(500)]
    public string? JobUrl { get; set; }

    [MaxLength(100)]
    public string? JobReference { get; set; }

    // Location
    [MaxLength(200)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    public WorkMode WorkMode { get; set; }

    public EmploymentType EmploymentType { get; set; }

    // Compensation
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }

    [MaxLength(10)]
    public string? Currency { get; set; }

    // Requirements
    public bool RelocationRequired { get; set; }
    public bool VisaSponsorshipAvailable { get; set; }

    [MaxLength(50)]
    public string? JapaneseLevelRequired { get; set; }

    [MaxLength(50)]
    public string? EnglishLevelRequired { get; set; }

    // Tracking
    public ApplicationStage Stage { get; set; } = ApplicationStage.Applied;
    public Priority Priority { get; set; } = Priority.Medium;

    public DateTime? AppliedDate { get; set; }
    public DateTime? LastActivityDate { get; set; }
    public DateTime? NextFollowUpDate { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Contact> Contacts { get; set; } = [];
    public ICollection<JobTechnologyRequirement> TechnologyRequirements { get; set; } = [];
    public ICollection<Interview> Interviews { get; set; } = [];
    public ICollection<ApplicationActivity> Activities { get; set; } = [];

    // UTC activity timestamps; date-only AppliedDate is interpreted as a local calendar date.
    public bool IsNoResponse => !IsTerminalStage && DaysSinceLastActivity >= 12;

    public bool IsTerminalStage => Stage is ApplicationStage.Rejected or ApplicationStage.NoResponse
        or ApplicationStage.Accepted or ApplicationStage.Withdrawn or ApplicationStage.Offer;

    public int DaysSinceLastActivity => WaitingDays(DateTime.Today);

    public int WaitingDays(DateTime localToday)
    {
        var activity = LastActivityDate ?? (AppliedDate.HasValue ? null : UpdatedAt);
        var date = activity.HasValue
            ? (activity.Value.Kind == DateTimeKind.Local ? activity.Value : DateTime.SpecifyKind(activity.Value, DateTimeKind.Utc).ToLocalTime()).Date
            : AppliedDate!.Value.Date;
        return Math.Max(0, (localToday.Date - date).Days);
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(ClientCompany) && string.IsNullOrWhiteSpace(RecruitingCompany))
            yield return new ValidationResult("Enter a Client Company or Recruiting Company (at least one is required).",
                [nameof(ClientCompany)]);
    }
}
