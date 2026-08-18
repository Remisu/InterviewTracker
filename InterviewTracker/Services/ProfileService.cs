using InterviewTracker.Data;
using InterviewTracker.Models;
using InterviewTracker.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace InterviewTracker.Services;

public class ProfileService(IDbContextFactory<AppDbContext> factory)
{
    public async Task<List<ProfileTechnology>> GetAllAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.ProfileTechnologies
            .Include(p => p.Technology)
            .OrderBy(p => p.Technology.Category)
            .ThenBy(p => p.Technology.Name)
            .ToListAsync();
    }

    public async Task<ProfileTechnology?> GetByTechnologyIdAsync(int technologyId)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.ProfileTechnologies
            .Include(p => p.Technology)
            .FirstOrDefaultAsync(p => p.TechnologyId == technologyId);
    }

    public async Task<ProfileTechnology> UpsertAsync(ProfileTechnology profile)
    {
        await using var db = await factory.CreateDbContextAsync();
        var existing = await db.ProfileTechnologies
            .FirstOrDefaultAsync(p => p.TechnologyId == profile.TechnologyId);

        if (existing is null)
        {
            db.ProfileTechnologies.Add(profile);
        }
        else
        {
            existing.YearsExperience = profile.YearsExperience;
            existing.SkillLevel      = profile.SkillLevel;
            existing.LastUsedYear    = profile.LastUsedYear;
            existing.Notes           = profile.Notes;
        }

        await db.SaveChangesAsync();
        return existing ?? profile;
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        var entry = await db.ProfileTechnologies.FindAsync(id);
        if (entry is null) return;
        db.ProfileTechnologies.Remove(entry);
        await db.SaveChangesAsync();
    }

    /// <summary>Returns a dictionary keyed by TechnologyId for fast lookup in match calculations.</summary>
    public async Task<Dictionary<int, ProfileTechnology>> GetLookupAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.ProfileTechnologies
            .Include(p => p.Technology)
            .ToDictionaryAsync(p => p.TechnologyId);
    }
}
