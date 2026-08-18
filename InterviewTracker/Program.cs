using InterviewTracker.Components;
using InterviewTracker.Data;
using InterviewTracker.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// SQLite database — use factory for safe Blazor Server scoped access
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "interviewtracker.db");
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Application services
builder.Services.AddScoped<TechnologyService>();
builder.Services.AddScoped<ApplicationService>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<JobMatchService>();
builder.Services.AddScoped<ContactService>();
builder.Services.AddScoped<ActivityService>();
builder.Services.AddScoped<InterviewService>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(InterviewTracker.Client._Imports).Assembly);

// Apply migrations and seed data on startup
using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    await using var db = await dbFactory.CreateDbContextAsync();
    await db.Database.MigrateAsync();
    await SeedData.SeedTechnologiesAsync(db);
}

app.Run();
