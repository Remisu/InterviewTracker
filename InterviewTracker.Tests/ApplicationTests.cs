using System.ComponentModel.DataAnnotations;
using InterviewTracker.Helpers;
using InterviewTracker.Models;
using InterviewTracker.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace InterviewTracker.Tests;

public class ApplicationTests
{
    internal static JobApplication NewApplication() => new()
    {
        JobTitle = "Engineer", ClientCompany = "Client", AppliedDate = DateTime.Today.AddDays(-7)
    };

    [Theory]
    [InlineData("Client", null, true)]
    [InlineData("", "Recruiter", true)]
    [InlineData("Client", "Recruiter", true)]
    [InlineData("", null, false)]
    [InlineData("  ", " \t", false)]
    public async Task CompanyRuleIsEnforcedByModelAndService(string client, string? recruiter, bool valid)
    {
        await using var fixture = new DatabaseFixture();
        await fixture.InitializeAsync();
        var app = NewApplication(); app.ClientCompany = client; app.RecruitingCompany = recruiter;
        Assert.Equal(valid, Validator.TryValidateObject(app, new ValidationContext(app), [], true));
        if (valid) Assert.True((await fixture.Applications.CreateAsync(app)).Id > 0);
        else
        {
            await Assert.ThrowsAsync<ValidationException>(() => fixture.Applications.CreateAsync(app));
            Assert.Empty(await fixture.Applications.GetAllAsync());
        }
    }

    [Fact]
    public async Task EditingPersistsLanguageLevelsAndPreservesWaitingClock()
    {
        await using var fixture = new DatabaseFixture(); await fixture.InitializeAsync();
        var app = await fixture.Applications.CreateAsync(NewApplication());
        var last = app.LastActivityDate; var modified = app.UpdatedAt;
        app.JapaneseLevelRequired = "JLPT N2"; app.EnglishLevelRequired = "C1 (Advanced)";
        app.JobTitle = "Senior Engineer"; app.Notes = "Cosmetic note";
        await fixture.Applications.UpdateAsync(app);
        var saved = (await fixture.Applications.GetByIdAsync(app.Id))!;
        Assert.Equal("JLPT N2", saved.JapaneseLevelRequired);
        Assert.Equal("C1 (Advanced)", saved.EnglishLevelRequired);
        Assert.Equal(last, saved.LastActivityDate);
        Assert.True(saved.UpdatedAt > modified);
        saved.ClientCompany = "";
        await Assert.ThrowsAsync<ValidationException>(() => fixture.Applications.UpdateAsync(saved));
        Assert.Equal("Client", (await fixture.Applications.GetByIdAsync(app.Id))!.ClientCompany);
    }

    [Fact]
    public async Task StageChangesAreMeaningfulAndSameStageIsNoOp()
    {
        await using var fixture = new DatabaseFixture(); await fixture.InitializeAsync();
        var app = await fixture.Applications.CreateAsync(NewApplication());
        await fixture.Applications.UpdateStageAsync(app.Id, ApplicationStage.NoResponse);
        var terminal = (await fixture.Applications.GetByIdAsync(app.Id))!;
        Assert.True(terminal.IsTerminalStage); Assert.False(terminal.IsNoResponse);
        Assert.True(terminal.LastActivityDate > app.LastActivityDate);
        var count = terminal.Activities.Count;
        await fixture.Applications.UpdateStageAsync(app.Id, ApplicationStage.NoResponse);
        var same = (await fixture.Applications.GetByIdAsync(app.Id))!;
        Assert.Equal(terminal.LastActivityDate, same.LastActivityDate);
        Assert.Equal(terminal.UpdatedAt, same.UpdatedAt); Assert.Equal(count, same.Activities.Count);
        await fixture.Applications.UpdateStageAsync(app.Id, ApplicationStage.ClientInterview);
        Assert.False((await fixture.Applications.GetByIdAsync(app.Id))!.IsTerminalStage);
    }

    [Fact]
    public async Task NotesAndBackdatedEventsDoNotResetWaitingButResponsesDo()
    {
        await using var fixture = new DatabaseFixture(); await fixture.InitializeAsync();
        var app = await fixture.Applications.CreateAsync(NewApplication());
        var last = app.LastActivityDate;
        var note = await fixture.Activities.CreateAsync(new() { JobApplicationId = app.Id, Title = "Notes", ActivityType = ActivityType.Note });
        Assert.Equal(last, (await fixture.Applications.GetByIdAsync(app.Id))!.LastActivityDate);
        await fixture.Activities.CreateAsync(new() { JobApplicationId = app.Id, Title = "Old call", ActivityType = ActivityType.PhoneCall, ActivityDate = DateTime.UtcNow.AddDays(-20) });
        Assert.Equal(last, (await fixture.Applications.GetByIdAsync(app.Id))!.LastActivityDate);
        var response = await fixture.Activities.CreateAsync(new() { JobApplicationId = app.Id, Title = "Response", ActivityType = ActivityType.EmailReceived });
        Assert.Equal(response.ActivityDate, (await fixture.Applications.GetByIdAsync(app.Id))!.LastActivityDate);
        response.Description = "Fixed typo";
        await fixture.Activities.UpdateAsync(response);
        await fixture.Activities.DeleteAsync(note.Id);
        Assert.Equal(response.ActivityDate, (await fixture.Applications.GetByIdAsync(app.Id))!.LastActivityDate);
    }

    [Fact]
    public async Task InterviewStatusAndScheduleChangesAreMeaningfulButNotesAreCosmetic()
    {
        await using var fixture = new DatabaseFixture(); await fixture.InitializeAsync();
        var app = await fixture.Applications.CreateAsync(NewApplication());
        var interviews = new InterviewTracker.Services.InterviewService(fixture);
        var interview = await interviews.CreateAsync(new() { JobApplicationId = app.Id }, fixture.Activities);
        var scheduled = (await fixture.Applications.GetByIdAsync(app.Id))!;
        Assert.True(scheduled.LastActivityDate > app.LastActivityDate);
        interview.Notes = "Fixed typo";
        await interviews.UpdateAsync(interview, fixture.Activities);
        Assert.Equal(scheduled.LastActivityDate, (await fixture.Applications.GetByIdAsync(app.Id))!.LastActivityDate);
        interview.Status = InterviewStatus.Rescheduled;
        interview.ScheduledDate = DateTime.UtcNow.AddDays(3);
        await interviews.UpdateAsync(interview, fixture.Activities);
        Assert.True((await fixture.Applications.GetByIdAsync(app.Id))!.LastActivityDate > scheduled.LastActivityDate);
    }

    [Fact]
    public void CalendarDaysUseLocalMidnightWithFallbackAndStableOrdering()
    {
        var today = new DateTime(2026, 10, 21);
        var app = NewApplication();
        app.LastActivityDate = DateTime.SpecifyKind(today.AddDays(-1).AddHours(23), DateTimeKind.Local).ToUniversalTime();
        Assert.Equal(1, app.WaitingDays(today));
        app.LastActivityDate = today.AddDays(2).ToUniversalTime(); Assert.Equal(0, app.WaitingDays(today));
        app.LastActivityDate = null; app.AppliedDate = today.AddDays(-12); Assert.Equal(12, app.WaitingDays(today));
        app.AppliedDate = null; app.UpdatedAt = today.AddDays(-3).ToUniversalTime(); Assert.Equal(3, app.WaitingDays(today));
        var older = NewApplication(); older.LastActivityDate = DateTime.Today.AddDays(-10).ToUniversalTime(); older.Priority = Priority.Low;
        var newer = NewApplication(); newer.LastActivityDate = DateTime.Today.AddDays(-1).ToUniversalTime(); newer.Priority = Priority.DreamJob;
        Assert.Equal(older, ApplicationWorkflow.Order([newer, older]).First());
        newer.LastActivityDate = older.LastActivityDate; newer.CreatedAt = older.CreatedAt;
        Assert.Equal(ApplicationWorkflow.Order([older, newer]).Select(a => a.SyncId), ApplicationWorkflow.Order([newer, older]).Select(a => a.SyncId));
    }

    [Fact]
    public void EveryLegacyStageHasExactlyOneVisibleHome()
    {
        Assert.Equal(new[] { ApplicationStage.Applied, ApplicationStage.RecruiterScreening, ApplicationStage.ClientInterview,
            ApplicationStage.TechnicalAssessment, ApplicationStage.FinalInterview, ApplicationStage.Rejected, ApplicationStage.NoResponse }, ApplicationWorkflow.Stages);
        foreach (var stage in Enum.GetValues<ApplicationStage>())
            Assert.Equal(1, ApplicationWorkflow.Stages.Count(s => s == ApplicationWorkflow.BoardStage(stage)) + (ApplicationWorkflow.NeedsReview(stage) ? 1 : 0));
        Assert.True(ApplicationWorkflow.NeedsReview(ApplicationStage.Offer));
        Assert.True(ApplicationWorkflow.NeedsReview(ApplicationStage.Interview));
    }

    [Fact]
    public async Task MigrationPreservesLegacyRowsAndAssignsPermanentUniqueIds()
    {
        await using var fixture = new DatabaseFixture();
        await using (var db = fixture.CreateDbContext())
        {
            await db.GetService<IMigrator>().MigrateAsync("20260818053313_InitialCreate");
            foreach (var stage in Enum.GetValues<ApplicationStage>())
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO JobApplications (JobTitle, ClientCompany, WorkMode, EmploymentType, RelocationRequired,
                    VisaSponsorshipAvailable, Stage, Priority, CreatedAt, UpdatedAt)
                    VALUES ('Legacy job', 'Preserved company', 0, 0, 0, 0, {(int)stage}, 2, '2026-09-01 00:00:00', '2026-09-03 00:00:00')
                    """);
            }
            await db.Database.MigrateAsync();
            var rows = await db.JobApplications.OrderBy(a => a.Id).ToListAsync();
            Assert.Equal(Enum.GetValues<ApplicationStage>(), rows.Select(a => a.Stage));
            Assert.Equal(rows.Count, rows.Select(a => a.SyncId).Distinct().Count());
            Assert.All(rows, a => { Assert.NotEqual(Guid.Empty, a.SyncId); Assert.Equal(2, (int)a.Priority); Assert.Equal(a.UpdatedAt, a.LastActivityDate); });
            var ids = rows.Select(a => a.SyncId).ToArray();
            await db.Database.MigrateAsync();
            Assert.Equal(ids, await db.JobApplications.OrderBy(a => a.Id).Select(a => a.SyncId).ToArrayAsync());
            Assert.NotEqual(Guid.Empty, (await db.SyncInstallations.SingleAsync()).InstallationId);
            Assert.False(db.Database.HasPendingModelChanges());
        }
    }
}
