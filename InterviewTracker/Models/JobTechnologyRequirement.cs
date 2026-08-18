using InterviewTracker.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace InterviewTracker.Models;

public class JobTechnologyRequirement
{
    public int Id { get; set; }

    public int JobApplicationId { get; set; }
    public JobApplication JobApplication { get; set; } = null!;

    public int TechnologyId { get; set; }
    public Technology Technology { get; set; } = null!;

    public decimal? RequiredYearsExperience { get; set; }

    public RequirementLevel RequirementLevel { get; set; } = RequirementLevel.Required;

    public bool IsMandatory { get; set; } = true;

    [MaxLength(500)]
    public string? Notes { get; set; }
}
