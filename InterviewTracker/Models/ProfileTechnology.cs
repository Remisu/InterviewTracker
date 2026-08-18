using InterviewTracker.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace InterviewTracker.Models;

public class ProfileTechnology
{
    public int Id { get; set; }

    public int TechnologyId { get; set; }
    public Technology Technology { get; set; } = null!;

    public decimal YearsExperience { get; set; }

    public SkillLevel SkillLevel { get; set; }

    public int? LastUsedYear { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
