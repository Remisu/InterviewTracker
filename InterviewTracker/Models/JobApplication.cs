using InterviewTracker.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace InterviewTracker.Models;

public class JobApplication
{
    public int Id { get; set; }

    // Company / Position
    [Required, MaxLength(200)]
    public string JobTitle { get; set; } = string.Empty;

    [Required, MaxLength(200)]
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
    public ApplicationStage Stage { get; set; } = ApplicationStage.Interested;
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

    // Calculated - not persisted
    public bool IsNoResponse =>
        LastActivityDate.HasValue
        && !IsTerminalStage
        && (DateTime.UtcNow - LastActivityDate.Value).TotalDays >= 12;

    public bool IsTerminalStage =>
        Stage is ApplicationStage.Rejected
            or ApplicationStage.Accepted
            or ApplicationStage.Withdrawn
            or ApplicationStage.Offer;

    public int? DaysSinceLastActivity =>
        LastActivityDate.HasValue
            ? (int)(DateTime.UtcNow - LastActivityDate.Value).TotalDays
            : null;
}
