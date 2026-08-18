using InterviewTracker.Data;
using InterviewTracker.Models;
using InterviewTracker.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace InterviewTracker.Services;

public class InterviewService(IDbContextFactory<AppDbContext> factory)
{
    // ── Queries ───────────────────────────────────────────────────────────────

    public async Task<List<Interview>> GetByApplicationAsync(int appId)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Interviews
            .Include(i => i.Questions)
            .Include(i => i.InterviewerContact)
            .Where(i => i.JobApplicationId == appId)
            .OrderByDescending(i => i.ScheduledDate)
            .ToListAsync();
    }

    public async Task<Interview?> GetByIdAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Interviews
            .Include(i => i.Questions)
            .Include(i => i.InterviewerContact)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<List<Interview>> GetAllUpcomingAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Interviews
            .Include(i => i.JobApplication)
            .Include(i => i.InterviewerContact)
            .Where(i => i.Status == InterviewStatus.Scheduled
                     && i.ScheduledDate.HasValue
                     && i.ScheduledDate.Value >= DateTime.UtcNow)
            .OrderBy(i => i.ScheduledDate)
            .ToListAsync();
    }

    public async Task<List<Interview>> GetAllAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Interviews
            .Include(i => i.JobApplication)
            .Include(i => i.Questions)
            .OrderByDescending(i => i.ScheduledDate)
            .ToListAsync();
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    public async Task<Interview> CreateAsync(Interview interview, ActivityService activityService)
    {
        await using var db = await factory.CreateDbContextAsync();
        interview.CreatedAt = DateTime.UtcNow;
        interview.UpdatedAt = DateTime.UtcNow;
        db.Interviews.Add(interview);
        await db.SaveChangesAsync();

        // Auto-activity
        await activityService.AddAutoAsync(
            interview.JobApplicationId,
            ActivityType.InterviewScheduled,
            $"{Helpers.EnumHelper.DisplayName(interview.InterviewType)} scheduled",
            interview.ScheduledDate.HasValue
                ? $"Scheduled for {interview.ScheduledDate.Value:MMM d, yyyy HH:mm}"
                : null,
            interviewId: interview.Id);

        return interview;
    }

    public async Task UpdateAsync(Interview interview, ActivityService activityService)
    {
        await using var db = await factory.CreateDbContextAsync();
        var existing = await db.Interviews.FindAsync(interview.Id);
        if (existing is null) return;

        var wasScheduled = existing.Status;

        existing.InterviewType         = interview.InterviewType;
        existing.ScheduledDate         = interview.ScheduledDate;
        existing.DurationMinutes       = interview.DurationMinutes;
        existing.Interviewer           = interview.Interviewer;
        existing.InterviewerContactId  = interview.InterviewerContactId;
        existing.LocationOrMeetingUrl  = interview.LocationOrMeetingUrl;
        existing.Status                = interview.Status;
        existing.Notes                 = interview.Notes;
        existing.Result                = interview.Result;
        existing.Difficulty            = interview.Difficulty;
        existing.PerformanceRating     = interview.PerformanceRating;
        existing.UpdatedAt             = DateTime.UtcNow;

        await db.SaveChangesAsync();

        // Auto-activity when marked complete
        if (wasScheduled != InterviewStatus.Completed && interview.Status == InterviewStatus.Completed)
        {
            await activityService.AddAutoAsync(
                interview.JobApplicationId,
                ActivityType.InterviewCompleted,
                $"{Helpers.EnumHelper.DisplayName(interview.InterviewType)} completed",
                interview.Result,
                interviewId: interview.Id);
        }
    }

    public async Task DeleteAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        var interview = await db.Interviews.FindAsync(id);
        if (interview is null) return;
        db.Interviews.Remove(interview);
        await db.SaveChangesAsync();
    }

    // ── Questions ─────────────────────────────────────────────────────────────

    public async Task<List<InterviewQuestion>> GetQuestionsAsync(int interviewId)
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.InterviewQuestions
            .Where(q => q.InterviewId == interviewId)
            .OrderBy(q => q.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<InterviewQuestion>> GetAllQuestionsAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.InterviewQuestions
            .Include(q => q.Interview)
                .ThenInclude(i => i.JobApplication)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
    }

    public async Task<InterviewQuestion> AddQuestionAsync(InterviewQuestion question)
    {
        await using var db = await factory.CreateDbContextAsync();
        question.CreatedAt = DateTime.UtcNow;
        db.InterviewQuestions.Add(question);
        await db.SaveChangesAsync();
        return question;
    }

    public async Task UpdateQuestionAsync(InterviewQuestion question)
    {
        await using var db = await factory.CreateDbContextAsync();
        db.InterviewQuestions.Update(question);
        await db.SaveChangesAsync();
    }

    public async Task DeleteQuestionAsync(int id)
    {
        await using var db = await factory.CreateDbContextAsync();
        var q = await db.InterviewQuestions.FindAsync(id);
        if (q is null) return;
        db.InterviewQuestions.Remove(q);
        await db.SaveChangesAsync();
    }
}
