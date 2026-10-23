using InterviewTracker.Data;
using InterviewTracker.Services;
using Microsoft.EntityFrameworkCore;

namespace InterviewTracker.Tests;

public sealed class DatabaseFixture : IDbContextFactory<AppDbContext>, IAsyncDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "interviewtracker-test-" + Guid.NewGuid());
    public DbContextOptions<AppDbContext> Options { get; }
    public ApplicationService Applications => new(this);
    public ActivityService Activities => new(this);
    public SyncService Sync => new(this);

    public DatabaseFixture()
    {
        Directory.CreateDirectory(_directory);
        Options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite($"Data Source={Path.Combine(_directory, "test.db")}").Options;
    }

    public AppDbContext CreateDbContext() => new(Options);
    public Task<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    public async Task InitializeAsync()
    {
        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }
    public ValueTask DisposeAsync() { Directory.Delete(_directory, recursive: true); return ValueTask.CompletedTask; }
}
