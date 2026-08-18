using InterviewTracker.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace InterviewTracker.Models;

public class InterviewQuestion
{
    public int Id { get; set; }

    public int InterviewId { get; set; }
    public Interview Interview { get; set; } = null!;

    [Required, MaxLength(1000)]
    public string Question { get; set; } = string.Empty;

    public QuestionCategory Category { get; set; }

    [MaxLength(2000)]
    public string? MyAnswer { get; set; }

    [MaxLength(2000)]
    public string? IdealAnswer { get; set; }

    public bool? WasAnsweredCorrectly { get; set; }

    /// <summary>1-5</summary>
    public int? Difficulty { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public bool NeedsReview { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
