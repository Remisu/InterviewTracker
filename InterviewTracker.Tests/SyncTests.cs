using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Nodes;
using InterviewTracker.Models;
using InterviewTracker.Models.Enums;
using InterviewTracker.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace InterviewTracker.Tests;

public class SyncTests
{
    private static string Json(SyncDocument document) => JsonSerializer.Serialize(document, SyncService.JsonOptions);

    [Fact]
    public async Task RepeatImportIsIdempotentAndKeepsDestinationOnlyRecords()
    {
        await using var source = new DatabaseFixture(); await source.InitializeAsync();
        await using var target = new DatabaseFixture(); await target.InitializeAsync();
        var app = await source.Applications.CreateAsync(ApplicationTests.NewApplication());
        var onlyLocal = await target.Applications.CreateAsync(ApplicationTests.NewApplication());
        var exported = await source.Sync.ExportAsync();
        var preview = await target.Sync.PreviewAsync(exported);
        Assert.Equal(1, preview.Count(MergeAction.Add));
        Assert.Equal(new SyncSummary(1, 0, 0), await target.Sync.MergeAsync(preview));
        var repeat = await target.Sync.PreviewAsync(exported);
        Assert.Equal(1, repeat.Count(MergeAction.Unchanged));
        await target.Sync.MergeAsync(repeat);
        var saved = await target.Applications.GetAllAsync();
        Assert.Equal(2, saved.Count);
        Assert.Contains(saved, a => a.SyncId == onlyLocal.SyncId);
        Assert.Equal(app.LastActivityDate, saved.Single(a => a.SyncId == app.SyncId).LastActivityDate);
        Assert.Equal(app.UpdatedAt, saved.Single(a => a.SyncId == app.SyncId).UpdatedAt);
        var reexport = SyncService.Parse(await target.Sync.ExportAsync());
        Assert.NotEqual(SyncService.Parse(exported).SourceInstallationId, reexport.SourceInstallationId);
    }

    [Fact]
    public async Task NewerUpdatesOlderKeepsLocalAndReadsNeverTouchClocks()
    {
        await using var source = new DatabaseFixture(); await source.InitializeAsync();
        await using var target = new DatabaseFixture(); await target.InitializeAsync();
        var app = await source.Applications.CreateAsync(ApplicationTests.NewApplication());
        var original = await source.Sync.ExportAsync();
        await target.Sync.MergeAsync(await target.Sync.PreviewAsync(original));
        app.JapaneseLevelRequired = "JLPT N1"; app.Notes = "New information";
        await source.Applications.UpdateAsync(app);
        var newer = await source.Sync.ExportAsync();
        var update = await target.Sync.PreviewAsync(newer);
        Assert.Equal(1, update.Count(MergeAction.Update));
        await target.Sync.MergeAsync(update);
        var current = (await target.Applications.GetAllAsync()).Single();
        Assert.Equal("JLPT N1", current.JapaneseLevelRequired);
        Assert.Equal(app.LastActivityDate, current.LastActivityDate);
        var old = await target.Sync.PreviewAsync(original);
        Assert.Equal(1, old.Count(MergeAction.Unchanged));
        await target.Sync.MergeAsync(old);
        await target.Sync.ExportAsync(); await target.Applications.GetByIdAsync(current.Id);
        Assert.Equal(current.UpdatedAt, (await target.Applications.GetAllAsync()).Single().UpdatedAt);
        Assert.Equal(current.LastActivityDate, (await target.Applications.GetAllAsync()).Single().LastActivityDate);
    }

    [Theory]
    [InlineData(ConflictResolution.KeepLocal)]
    [InlineData(ConflictResolution.UseImported)]
    public async Task EqualTimestampConflictsRequireDecisionAndConverge(ConflictResolution decision)
    {
        await using var fixture = new DatabaseFixture(); await fixture.InitializeAsync();
        var app = await fixture.Applications.CreateAsync(ApplicationTests.NewApplication());
        var document = SyncService.Parse(await fixture.Sync.ExportAsync());
        document.Applications.Single().Application.Notes = "Other computer";
        var json = Json(document);
        var preview = await fixture.Sync.PreviewAsync(json);
        Assert.Equal(1, preview.Count(MergeAction.Conflict));
        await Assert.ThrowsAsync<ValidationException>(() => fixture.Sync.MergeAsync(preview));
        Assert.Null((await fixture.Applications.GetByIdAsync(app.Id))!.Notes);
        preview.Items.Single().Resolution = decision;
        await fixture.Sync.MergeAsync(preview);
        var saved = (await fixture.Applications.GetByIdAsync(app.Id))!;
        Assert.Equal(decision == ConflictResolution.UseImported ? "Other computer" : null, saved.Notes);
        Assert.True(saved.UpdatedAt > app.UpdatedAt);
        Assert.Equal(app.LastActivityDate, saved.LastActivityDate);
        Assert.Equal(1, (await fixture.Sync.PreviewAsync(json)).Count(MergeAction.Unchanged));
    }

    [Fact]
    public async Task LocalEditsRemainNewerThanAnImportedClockThatIsAhead()
    {
        await using var fixture = new DatabaseFixture(); await fixture.InitializeAsync();
        var app = await fixture.Applications.CreateAsync(ApplicationTests.NewApplication());
        var document = SyncService.Parse(await fixture.Sync.ExportAsync());
        var ahead = DateTime.UtcNow.AddDays(1);
        document.Applications.Single().Application.UpdatedAt = ahead;
        var json = Json(document);
        await fixture.Sync.MergeAsync(await fixture.Sync.PreviewAsync(json));
        var local = (await fixture.Applications.GetByIdAsync(app.Id))!;
        local.Notes = "Local edit after import";
        await fixture.Applications.UpdateAsync(local);
        var edited = (await fixture.Applications.GetByIdAsync(app.Id))!;
        Assert.True(edited.UpdatedAt > ahead);
        Assert.Equal(app.LastActivityDate, edited.LastActivityDate);
        Assert.Equal(1, (await fixture.Sync.PreviewAsync(json)).Count(MergeAction.Unchanged));
        await fixture.Applications.UpdateStageAsync(app.Id, ApplicationStage.Rejected);
        Assert.True((await fixture.Applications.GetByIdAsync(app.Id))!.UpdatedAt > edited.UpdatedAt);
    }

    [Fact]
    public async Task StalePreviewCannotOverwriteAnEditOrDelete()
    {
        await using var fixture = new DatabaseFixture(); await fixture.InitializeAsync();
        var app = await fixture.Applications.CreateAsync(ApplicationTests.NewApplication());
        var document = SyncService.Parse(await fixture.Sync.ExportAsync());
        document.Applications.Single().Application.UpdatedAt = app.UpdatedAt.AddMinutes(1);
        document.Applications.Single().Application.Notes = "Imported";
        var preview = await fixture.Sync.PreviewAsync(Json(document));
        app.Notes = "Local edit";
        await fixture.Applications.UpdateAsync(app);
        await Assert.ThrowsAsync<ValidationException>(() => fixture.Sync.MergeAsync(preview));
        Assert.Equal("Local edit", (await fixture.Applications.GetByIdAsync(app.Id))!.Notes);
        preview = await fixture.Sync.PreviewAsync(Json(document));
        await fixture.Applications.DeleteAsync(app.Id);
        await Assert.ThrowsAsync<ValidationException>(() => fixture.Sync.MergeAsync(preview));
        Assert.Empty(await fixture.Applications.GetAllAsync());
    }

    [Fact]
    public async Task RelatedGraphRemapsIdsAndAllChildEditsAdvanceTheSyncClock()
    {
        await using var source = new DatabaseFixture(); await source.InitializeAsync();
        await using var target = new DatabaseFixture(); await target.InitializeAsync();
        var app = await source.Applications.CreateAsync(ApplicationTests.NewApplication());
        var contact = await new ContactService(source).CreateAsync(new() { JobApplicationId = app.Id, FullName = "Recruiter" });
        var interview = await new InterviewService(source).CreateAsync(new() { JobApplicationId = app.Id, InterviewerContactId = contact.Id }, source.Activities);
        var question = await new InterviewService(source).AddQuestionAsync(new() { InterviewId = interview.Id, Question = "Explain transactions" });
        await source.Activities.CreateAsync(new() { JobApplicationId = app.Id, Title = "Call", ContactId = contact.Id, InterviewId = interview.Id, ActivityType = ActivityType.PhoneCall });
        var technology = await new TechnologyService(source).CreateAsync(new() { Name = "C#", Description = "Source catalog" });
        await source.Applications.AddRequirementAsync(new() { JobApplicationId = app.Id, TechnologyId = technology.Id });
        await new TechnologyService(target).CreateAsync(new() { Name = "C#", Description = "Keep local catalog" });
        // Occupy numeric IDs on the destination to exercise reference remapping.
        var local = await target.Applications.CreateAsync(ApplicationTests.NewApplication());
        await new ContactService(target).CreateAsync(new() { JobApplicationId = local.Id, FullName = "Local contact" });
        await new InterviewService(target).CreateAsync(new() { JobApplicationId = local.Id }, target.Activities);
        var json = await source.Sync.ExportAsync();
        await target.Sync.MergeAsync(await target.Sync.PreviewAsync(json));
        var importedId = (await target.Applications.GetAllAsync()).Single(a => a.SyncId == app.SyncId).Id;
        var imported = (await target.Applications.GetByIdAsync(importedId))!;
        Assert.NotEqual(app.Id, imported.Id);
        Assert.Equal(imported.Contacts.Single().Id, imported.Interviews.Single().InterviewerContactId);
        Assert.Equal(imported.Interviews.Single().Id, imported.Interviews.Single().Questions.Single().InterviewId);
        var call = imported.Activities.Single(a => a.Title == "Call");
        Assert.Equal(imported.Contacts.Single().Id, call.ContactId);
        Assert.Equal(imported.Interviews.Single().Id, call.InterviewId);
        Assert.Equal("Keep local catalog", imported.TechnologyRequirements.Single().Technology.Description);
        Assert.Equal(1, (await target.Sync.PreviewAsync(json)).Count(MergeAction.Unchanged));
        // Update each kind of child; root timestamps are the aggregate merge clock.
        var before = (await source.Applications.GetByIdAsync(app.Id))!;
        question.MyAnswer = "Atomic merge";
        await new InterviewService(source).UpdateQuestionAsync(question);
        var after = (await source.Applications.GetByIdAsync(app.Id))!;
        Assert.True(after.UpdatedAt > before.UpdatedAt);
        Assert.Equal(before.LastActivityDate, after.LastActivityDate);
        contact.Notes = "Updated contact";
        await new ContactService(source).UpdateAsync(contact);
        Assert.True((await source.Applications.GetByIdAsync(app.Id))!.UpdatedAt > after.UpdatedAt);
        var next = await source.Sync.ExportAsync();
        Assert.Equal(1, (await target.Sync.PreviewAsync(next)).Count(MergeAction.Update));
        await target.Sync.MergeAsync(await target.Sync.PreviewAsync(next));
        imported = (await target.Applications.GetByIdAsync(importedId))!;
        Assert.Equal("Atomic merge", imported.Interviews.Single().Questions.Single().MyAnswer);
        Assert.Equal("Updated contact", imported.Contacts.Single().Notes);
        Assert.Single(imported.Contacts); Assert.Single(imported.Interviews);
        Assert.Equal(1, (await target.Sync.PreviewAsync(next)).Count(MergeAction.Unchanged));
    }

    [Fact]
    public async Task MidImportFailureRollsBackEveryRecordAndRelatedDeletion()
    {
        await using var source = new DatabaseFixture(); await source.InitializeAsync();
        await using var target = new DatabaseFixture(); await target.InitializeAsync();
        var app = await source.Applications.CreateAsync(ApplicationTests.NewApplication());
        await new ContactService(source).CreateAsync(new() { JobApplicationId = app.Id, FullName = "Keep me" });
        await target.Sync.MergeAsync(await target.Sync.PreviewAsync(await source.Sync.ExportAsync()));
        app.Notes = "Must roll back"; await source.Applications.UpdateAsync(app);
        var second = ApplicationTests.NewApplication(); second.JobTitle = "Fail this insert";
        await source.Applications.CreateAsync(second);
        var document = SyncService.Parse(await source.Sync.ExportAsync());
        document.Applications = document.Applications.OrderBy(a => a.Application.SyncId == app.SyncId ? 0 : 1).ToList();
        await using (var db = target.CreateDbContext())
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER fail_import BEFORE INSERT ON JobApplications WHEN NEW.JobTitle = 'Fail this insert' BEGIN SELECT RAISE(ABORT, 'simulated failure'); END;");
        var preview = await target.Sync.PreviewAsync(Json(document));
        await Assert.ThrowsAsync<DbUpdateException>(() => target.Sync.MergeAsync(preview));
        var remaining = (await target.Applications.GetAllAsync()).Single();
        Assert.Null(remaining.Notes);
        Assert.Equal("Keep me", (await target.Applications.GetByIdAsync(remaining.Id))!.Contacts.Single().FullName);
    }

    [Theory]
    [InlineData("version")]
    [InlineData("duplicate")]
    [InlineData("missingId")]
    [InlineData("missingTimestamp")]
    [InlineData("nonUtc")]
    [InlineData("reference")]
    [InlineData("company")]
    [InlineData("enum")]
    public async Task MalformedFilesAreRejectedWithoutChangingLocalData(string problem)
    {
        await using var fixture = new DatabaseFixture(); await fixture.InitializeAsync();
        var app = await fixture.Applications.CreateAsync(ApplicationTests.NewApplication());
        var json = JsonNode.Parse(await fixture.Sync.ExportAsync())!;
        var records = json["Applications"]!.AsArray();
        var root = records[0]!["Application"]!;
        switch (problem)
        {
            case "version": json["FormatVersion"] = 99; break;
            case "duplicate": records.Add(records[0]!.DeepClone()); break;
            case "missingId": root.AsObject().Remove("SyncId"); break;
            case "missingTimestamp": root.AsObject().Remove("UpdatedAt"); break;
            case "nonUtc": root["UpdatedAt"] = "2026-10-21T12:00:00"; break;
            case "reference": records[0]!["Activities"]![0]!["ContactId"] = 999; break;
            case "company": root["ClientCompany"] = "  "; root["RecruitingCompany"] = ""; break;
            case "enum": root["Stage"] = "DoesNotExist"; break;
        }
        await Assert.ThrowsAsync<ValidationException>(() => fixture.Sync.PreviewAsync(json.ToJsonString()));
        Assert.Equal(app.UpdatedAt, (await fixture.Applications.GetByIdAsync(app.Id))!.UpdatedAt);
        Assert.Single(await fixture.Applications.GetAllAsync());
    }
}
