using Bunit;
using InterviewTracker.Components.Pages;
using InterviewTracker.Data;
using InterviewTracker.Models;
using InterviewTracker.Models.Enums;
using InterviewTracker.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InterviewTracker.Tests;

public class ComponentTests
{
    private static BunitContext Context(DatabaseFixture fixture)
    {
        var context = new BunitContext();
        context.Services.AddLogging();
        context.Services.AddSingleton<IDbContextFactory<AppDbContext>>(fixture);
        context.Services.AddScoped<ApplicationService>();
        context.Services.AddScoped<TechnologyService>();
        context.Services.AddScoped<ProfileService>();
        context.Services.AddScoped<JobMatchService>();
        context.Services.AddScoped<NotificationService>();
        context.Services.AddScoped<SyncService>();
        return context;
    }

    [Fact]
    public async Task DragAndDropPersistsTerminalMovesAndRestorationWithoutDuplicateCards()
    {
        await using var fixture = new DatabaseFixture(); await fixture.InitializeAsync();
        var app = await fixture.Applications.CreateAsync(ApplicationTests.NewApplication());
        using var context = Context(fixture);
        var board = context.Render<Kanban>();
        board.WaitForState(() => board.FindAll(".kanban-card").Count == 1);
        Assert.Equal(7, board.FindAll(".kanban-column").Count);
        board.Find(".kanban-card").TriggerEvent("ondragstart", new DragEventArgs());
        board.FindAll(".kanban-column")[6].TriggerEvent("ondrop", new DragEventArgs());
        board.WaitForAssertion(() => Assert.Contains("Manually marked", board.Markup));
        Assert.Equal(ApplicationStage.NoResponse, (await fixture.Applications.GetByIdAsync(app.Id))!.Stage);
        Assert.DoesNotContain("days without response", board.Markup);
        Assert.Single(board.FindAll(".kanban-card"));
        board.Find(".kanban-card").TriggerEvent("ondragstart", new DragEventArgs());
        board.FindAll(".kanban-column")[2].TriggerEvent("ondrop", new DragEventArgs());
        board.WaitForAssertion(() => Assert.Single(board.FindAll(".kanban-column")[2].QuerySelectorAll(".kanban-card")));
        Assert.Equal(ApplicationStage.ClientInterview, (await fixture.Applications.GetByIdAsync(app.Id))!.Stage);
        Assert.Single(board.FindAll(".kanban-card"));
        Assert.Contains("Updated today", board.Markup);
    }

    [Fact]
    public async Task FormDefaultsValidationManualDateLanguagesAndSuccessNavigation()
    {
        await using var fixture = new DatabaseFixture(); await fixture.InitializeAsync();
        using var context = Context(fixture);
        var form = context.Render<ApplicationEdit>();
        form.WaitForState(() => form.FindAll("form").Count == 1);
        Assert.Equal(DateTime.Today.ToString("yyyy-MM-dd"), form.Find("#app-applieddate").GetAttribute("value"));
        Assert.Contains("Not specified", form.Find("#app-japaneselevelrequired").TextContent);
        Assert.Contains("Not specified", form.Find("#app-englishlevelrequired").TextContent);
        form.Find("#app-jobtitle").Change("UI test");
        form.Find("form").Submit();
        form.WaitForAssertion(() => Assert.Contains("at least one is required", form.Markup));
        Assert.Empty(await fixture.Applications.GetAllAsync());
        var selectedDate = DateTime.Today.AddDays(-4);
        form.Find("#app-applieddate").Change(selectedDate.ToString("yyyy-MM-dd"));
        form.Find("#recruiting-company").Change("Recruiter only");
        form.Find("#app-japaneselevelrequired").Change("JLPT N2");
        form.Find("#app-englishlevelrequired").Change("C1 (Advanced)");
        form.Find("form").Submit();
        form.WaitForAssertion(() => Assert.EndsWith("/kanban", context.Services.GetRequiredService<NavigationManager>().Uri));
        var saved = (await fixture.Applications.GetAllAsync()).Single();
        Assert.Equal(selectedDate, saved.AppliedDate);
        Assert.Equal("JLPT N2", saved.JapaneseLevelRequired);
        Assert.Equal("C1 (Advanced)", saved.EnglishLevelRequired);
        var board = context.Render<Kanban>();
        board.WaitForAssertion(() => Assert.Contains("Application created successfully.", board.Find("[role=status]").TextContent));
        Assert.Null(context.Services.GetRequiredService<NotificationService>().Take());
    }

    [Fact]
    public async Task FailedFormSavePreservesAllInputAndRetryCreatesOnlyOneRecord()
    {
        await using var fixture = new DatabaseFixture(); await fixture.InitializeAsync();
        await using (var db = fixture.CreateDbContext())
            await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER fail_form BEFORE INSERT ON JobApplications BEGIN SELECT RAISE(ABORT, 'simulated failure'); END;");
        using var context = Context(fixture);
        var form = context.Render<ApplicationEdit>();
        form.WaitForState(() => form.FindAll("form").Count == 1);
        form.Find("#app-jobtitle").Change("Preserve this title");
        form.Find("#client-company").Change("Client");
        form.Find("#app-notes").Change("Multiline\nnotes");
        form.Find("#app-japaneselevelrequired").Change("JLPT N1");
        form.Find("form").Submit();
        form.WaitForAssertion(() => Assert.Contains("Your entries are preserved", form.Markup));
        Assert.Equal("Preserve this title", form.Find("#app-jobtitle").GetAttribute("value"));
        Assert.Equal("Multiline\nnotes", form.Find("#app-notes").GetAttribute("value"));
        Assert.Equal("textarea", form.Find("#app-notes").LocalName);
        Assert.Null(context.Services.GetRequiredService<NotificationService>().Take());
        Assert.Empty(await fixture.Applications.GetAllAsync());
        await using (var db = fixture.CreateDbContext()) await db.Database.ExecuteSqlRawAsync("DROP TRIGGER fail_form;");
        form.Find("form").Submit();
        form.WaitForAssertion(() => Assert.EndsWith("/kanban", context.Services.GetRequiredService<NavigationManager>().Uri));
        var saved = (await fixture.Applications.GetAllAsync()).Single();
        Assert.Equal("Preserve this title", saved.JobTitle);
        Assert.Equal("Multiline\nnotes", saved.Notes);
        Assert.Equal("JLPT N1", saved.JapaneseLevelRequired);
    }

    [Fact]
    public async Task EditingLegacyLanguageValuesDoesNotResetTheAppliedDate()
    {
        await using var fixture = new DatabaseFixture(); await fixture.InitializeAsync();
        var app = ApplicationTests.NewApplication();
        app.JapaneseLevelRequired = "Business Japanese"; app.EnglishLevelRequired = "Native";
        await fixture.Applications.CreateAsync(app);
        using var context = Context(fixture);
        var form = context.Render<ApplicationEdit>(p => p.Add(x => x.Id, app.Id));
        form.WaitForState(() => form.FindAll("form").Count == 1);
        Assert.Contains("Business Japanese (existing value)", form.Find("#app-japaneselevelrequired").TextContent);
        Assert.Contains("Native (existing value)", form.Find("#app-englishlevelrequired").TextContent);
        Assert.Equal(app.AppliedDate!.Value.ToString("yyyy-MM-dd"), form.Find("#app-applieddate").GetAttribute("value"));
    }
    [Fact]
    public async Task ImportUiValidatesPreviewsConfirmsAndResolvesConflicts()
    {
        await using var source = new DatabaseFixture(); await source.InitializeAsync();
        await using var target = new DatabaseFixture(); await target.InitializeAsync();
        var app = await source.Applications.CreateAsync(ApplicationTests.NewApplication());
        var localOnly = await target.Applications.CreateAsync(ApplicationTests.NewApplication());
        using var context = Context(target);
        var page = context.Render<ImportExport>();
        page.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("invalid", "bad.json"));
        page.WaitForAssertion(() => Assert.Contains("Invalid synchronization JSON", page.Find("[role=alert]").TextContent));
        Assert.Single(await target.Applications.GetAllAsync());
        var json = await source.Sync.ExportAsync();
        page.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText(json, "export.json"));
        page.WaitForAssertion(() => Assert.Contains("1 added", page.Markup));
        Assert.Single(await target.Applications.GetAllAsync());
        page.FindAll("button").Single(b => b.TextContent == "Confirm merge").Click();
        page.WaitForAssertion(() => Assert.Contains("Merge completed: 1 added", page.Markup));
        Assert.Equal(2, (await target.Applications.GetAllAsync()).Count);
        var conflict = SyncService.Parse(json);
        conflict.Applications.Single().Application.Notes = "Resolve in UI";
        page.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText(
            System.Text.Json.JsonSerializer.Serialize(conflict, SyncService.JsonOptions), "conflict.json"));
        page.WaitForAssertion(() => Assert.Contains("1 conflicts", page.Markup));
        Assert.True(page.FindAll("button").Single(b => b.TextContent == "Confirm merge").HasAttribute("disabled"));
        page.Find("select").Change(nameof(ConflictResolution.UseImported));
        page.FindAll("button").Single(b => b.TextContent == "Confirm merge").Click();
        page.WaitForAssertion(() => Assert.Contains("1 updated", page.Markup));
        var saved = await target.Applications.GetAllAsync();
        Assert.Equal("Resolve in UI", saved.Single(a => a.SyncId == app.SyncId).Notes);
        Assert.Contains(saved, a => a.SyncId == localOnly.SyncId);
        page.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText(json, "export.json"));
        page.WaitForAssertion(() => Assert.Contains("1 unchanged", page.Markup));
        Assert.Equal(2, (await target.Applications.GetAllAsync()).Count);
    }

}
