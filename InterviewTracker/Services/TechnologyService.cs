using InterviewTracker.Data;
using InterviewTracker.Models;
using InterviewTracker.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace InterviewTracker.Services;

public class TechnologyService(IDbContextFactory<AppDbContext> factory)
{
    public async Task<List<Technology>> GetAllAsync(bool includeInactive = false)
    {
        await using var db = await factory.CreateDbContextAsync();
        var q = db.Technologies.AsQueryable();
        if (!includeInactive) q = q.Where(t => t.IsActive);
        return await q.OrderBy(t => t.Category).ThenBy(t => t.Name).ToListAsync();
    }

    public async Task<List<Technology>> SearchAsync(string? search, TechnologyCategory? category, bool includeInactive = false)
    {
        await using var db = await factory.CreateDbContextAsync();
        var q = db.Technologies.AsQueryable();
        if (!includeInactive) q = q.Where(t => t.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(t => t.Name.Contains(search) || (t.Description != null && t.Description.Contains(search)));
        if (category.HasValue)
            q = q.Where(t => t.Category == category.Value);
        return await q.OrderBy(t => t.Category).ThenBy(t => t.Name).ToListAsync();
    }

    public async Task<Technology?> GetByIdAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Technologies.FindAsync(id);
    }

    public async Task<Technology> CreateAsync(Technology tech)
    {
        await using var db = await factory.CreateDbContextAsync();
        db.Technologies.Add(tech);
        await db.SaveChangesAsync();
        return tech;
    }

    public async Task UpdateAsync(Technology tech)
    {
        await using var db = await factory.CreateDbContextAsync();
        db.Technologies.Update(tech);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        var tech = await db.Technologies.FindAsync(id);
        if (tech is null) return;
        db.Technologies.Remove(tech);
        await db.SaveChangesAsync();
    }

    public async Task SetActiveAsync(int id, bool isActive)
    {
        await using var db = await factory.CreateDbContextAsync();
        var tech = await db.Technologies.FindAsync(id);
        if (tech is null) return;
        tech.IsActive = isActive;
        await db.SaveChangesAsync();
    }

    public async Task<Dictionary<int, int>> GetApplicationCountsAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.JobTechnologyRequirements
            .GroupBy(r => r.TechnologyId)
            .Select(g => new { TechId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TechId, x => x.Count);
    }

    public async Task<bool> NameExistsAsync(string name, int? excludeId = null)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Technologies.AnyAsync(t =>
            t.Name == name && (excludeId == null || t.Id != excludeId.Value));
    }
}
