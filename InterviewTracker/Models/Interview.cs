using InterviewTracker.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace InterviewTracker.Models;

public class Interview
{
    public int Id { get; set; }

    public int JobApplicationId { get; set; }
    public JobApplication JobApplication { get; set; } = null!;

    public InterviewType InterviewType { get; set; }

    public DateTime? ScheduledDate { get; set; }

    public int? DurationMinutes { get; set; }

    [MaxLength(200)]
    public string? Interviewer { get; set; }

    public int? InterviewerContactId { get; set; }
    public Contact? InterviewerContact { get; set; }

    [MaxLength(500)]
    public string? LocationOrMeetingUrl { get; set; }

    public InterviewStatus Status { get; set; } = InterviewStatus.Scheduled;

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [MaxLength(500)]
    public string? Result { get; set; }

    /// <summary>1-5</summary>
    public int? Difficulty { get; set; }

    /// <summary>1-5</summary>
    public int? PerformanceRating { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<InterviewQuestion> Questions { get; set; } = [];
}
