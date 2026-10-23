using InterviewTracker.Data;
using InterviewTracker.Models;
using InterviewTracker.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace InterviewTracker.Services;

public class ActivityService(IDbContextFactory<AppDbContext> factory)
{
    // ── Queries ───────────────────────────────────────────────────────────────

    public async Task<List<ApplicationActivity>> GetByApplicationAsync(int appId)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.ApplicationActivities
            .Include(a => a.Contact)
            .Include(a => a.Interview)
            .Where(a => a.JobApplicationId == appId)
            .OrderByDescending(a => a.ActivityDate)
            .ToListAsync();
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public async Task<ApplicationActivity> CreateAsync(ApplicationActivity activity)
    {
        await using var db = await factory.CreateDbContextAsync();
        activity.ActivityDate = activity.ActivityDate.ToUniversalTime();
        activity.CreatedAt   = DateTime.UtcNow;
        activity.UpdatedAt   = DateTime.UtcNow;
        activity.IsManual    = true;
        db.ApplicationActivities.Add(activity);
        await db.SaveChangesAsync();
        return activity;
    }

    public async Task UpdateAsync(ApplicationActivity activity)
    {
        await using var db = await factory.CreateDbContextAsync();
        var existing = await db.ApplicationActivities.FindAsync(activity.Id);
        if (existing is null) return;

        existing.ActivityType  = activity.ActivityType;
        existing.Title         = activity.Title;
        existing.Description   = activity.Description;
        existing.ActivityDate  = activity.ActivityDate.ToUniversalTime();
        existing.ContactId     = activity.ContactId;
        existing.FollowUpDate  = activity.FollowUpDate;
        existing.UpdatedAt     = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        var activity = await db.ApplicationActivities.FindAsync(id);
        if (activity is null) return;
        var appId = activity.JobApplicationId;
        db.ApplicationActivities.Remove(activity);
        await db.SaveChangesAsync();
    }

    /// <summary>Creates a system-generated (non-manual) activity entry.</summary>
    public async Task AddAutoAsync(
        int appId,
        ActivityType type,
        string title,
        string? description = null,
        int? contactId = null,
        int? interviewId = null)
    {
        await using var db = await factory.CreateDbContextAsync();
        db.ApplicationActivities.Add(new ApplicationActivity
        {
            JobApplicationId = appId,
            ActivityType     = type,
            Title            = title,
            Description      = description,
            ActivityDate     = DateTime.UtcNow,
            ContactId        = contactId,
            InterviewId      = interviewId,
            IsManual         = false,
            CreatedAt        = DateTime.UtcNow,
            UpdatedAt        = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();
    }

}
