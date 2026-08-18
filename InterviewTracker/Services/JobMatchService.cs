using InterviewTracker.Data;
using InterviewTracker.Helpers;
using InterviewTracker.Models;
using InterviewTracker.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace InterviewTracker.Services;

public class JobMatchService(IDbContextFactory<AppDbContext> factory, ProfileService profileService)
{
    // ── Weight map ────────────────────────────────────────────────────────────

    private static readonly Dictionary<RequirementLevel, int> Weights = new()
    {
        { RequirementLevel.NiceToHave, 1 },
        { RequirementLevel.Preferred,  2 },
        { RequirementLevel.Required,   3 },
        { RequirementLevel.Critical,   4 },
    };

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Calculates the match score for a single job application against the
    /// current profile. Requirements and profile are loaded from the database.
    /// </summary>
    public async Task<JobMatchResult> CalculateAsync(int jobApplicationId)
    {
        await using var db = await factory.CreateDbContextAsync();

        var requirements = await db.JobTechnologyRequirements
            .Include(r => r.Technology)
            .Where(r => r.JobApplicationId == jobApplicationId)
            .ToListAsync();

        if (requirements.Count == 0)
            return EmptyResult();

        var profileLookup = await profileService.GetLookupAsync();
        return Calculate(requirements, profileLookup);
    }

    /// <summary>
    /// Calculates the match score from already-loaded requirements and a
    /// profile lookup dictionary (avoids extra DB round-trips on detail pages).
    /// </summary>
    public JobMatchResult Calculate(
        IReadOnlyList<JobTechnologyRequirement> requirements,
        IReadOnlyDictionary<int, ProfileTechnology> profileLookup)
    {
        if (requirements.Count == 0)
            return EmptyResult();

        double weightedScoreSum = 0;
        int weightSum = 0;

        var strongMatches  = new List<TechMatchDetail>();
        var partialMatches = new List<TechMatchDetail>();
        var missingSkills  = new List<TechMatchDetail>();

        foreach (var req in requirements)
        {
            int weight = Weights.GetValueOrDefault(req.RequirementLevel, 1);
            weightSum += weight;

            profileLookup.TryGetValue(req.TechnologyId, out var profileEntry);

            double techScore;

            if (profileEntry is null)
            {
                // Technology not in profile at all
                techScore = 0.0;
            }
            else if (!req.RequiredYearsExperience.HasValue || req.RequiredYearsExperience.Value <= 0)
            {
                // No specific years required — having the tech counts as full match
                techScore = 1.0;
            }
            else
            {
                techScore = Math.Min(
                    (double)profileEntry.YearsExperience / (double)req.RequiredYearsExperience.Value,
                    1.0);
            }

            weightedScoreSum += techScore * weight;

            var detail = new TechMatchDetail
            {
                TechnologyName   = req.Technology.Name,
                Category         = EnumHelper.DisplayName(req.Technology.Category),
                RequiredYears    = req.RequiredYearsExperience,
                MyYears          = profileEntry?.YearsExperience,
                TechScore        = techScore,
                RequirementLevel = EnumHelper.DisplayName(req.RequirementLevel),
                IsMandatory      = req.IsMandatory,
            };

            if (techScore >= 1.0)
                strongMatches.Add(detail);
            else if (techScore > 0.0)
                partialMatches.Add(detail);
            else
                missingSkills.Add(detail);
        }

        int score = weightSum > 0
            ? (int)Math.Round((weightedScoreSum / weightSum) * 100)
            : 0;

        var (label, color) = ScoreCategory(score);

        return new JobMatchResult
        {
            Score         = score,
            Label         = label,
            Color         = color,
            StrongMatches = strongMatches,
            PartialMatches = partialMatches,
            MissingSkills = missingSkills,
            HasRequirements = true,
        };
    }

    /// <summary>
    /// Calculates match scores for a list of applications in a single profile
    /// fetch. Returns a dictionary keyed by JobApplicationId.
    /// </summary>
    public async Task<Dictionary<int, JobMatchResult>> CalculateBatchAsync(
        IEnumerable<JobApplication> applications)
    {
        var profileLookup = await profileService.GetLookupAsync();
        var result = new Dictionary<int, JobMatchResult>();

        foreach (var app in applications)
        {
            if (app.TechnologyRequirements is null || app.TechnologyRequirements.Count == 0)
            {
                result[app.Id] = EmptyResult();
            }
            else
            {
                result[app.Id] = Calculate(
                    app.TechnologyRequirements.ToList(),
                    profileLookup);
            }
        }

        return result;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static JobMatchResult EmptyResult() => new()
    {
        Score          = 0,
        Label          = "No Requirements",
        Color          = "secondary",
        HasRequirements = false,
    };

    public static (string Label, string Color) ScoreCategory(int score) => score switch
    {
        >= 90 => ("Excellent Match", "success"),
        >= 75 => ("Good Match",      "primary"),
        >= 60 => ("Moderate Match",  "info"),
        >= 40 => ("Partial Match",   "warning"),
        _     => ("Low Match",       "danger"),
    };
}
