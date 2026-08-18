using InterviewTracker.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace InterviewTracker.Models;

public class Technology
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public TechnologyCategory Category { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<JobTechnologyRequirement> JobRequirements { get; set; } = [];
    public ICollection<ProfileTechnology> ProfileTechnologies { get; set; } = [];
}
