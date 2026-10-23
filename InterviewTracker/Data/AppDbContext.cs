using InterviewTracker.Models;
using Microsoft.EntityFrameworkCore;

namespace InterviewTracker.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Technology> Technologies => Set<Technology>();
    public DbSet<ProfileTechnology> ProfileTechnologies => Set<ProfileTechnology>();
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<JobTechnologyRequirement> JobTechnologyRequirements => Set<JobTechnologyRequirement>();
    public DbSet<Interview> Interviews => Set<Interview>();
    public DbSet<InterviewQuestion> InterviewQuestions => Set<InterviewQuestion>();
    public DbSet<ApplicationActivity> ApplicationActivities => Set<ApplicationActivity>();

    // Import applies the exported UTC clocks verbatim, without synthesizing activity.
    public bool PreserveSyncTimestamps { get; set; }
    public DbSet<SyncInstallation> SyncInstallations => Set<SyncInstallation>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        SaveChangesAsync(true, cancellationToken);

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        if (!PreserveSyncTimestamps)
            await StampApplicationsAsync(cancellationToken);
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private async Task StampApplicationsAsync(CancellationToken cancellationToken)
    {
        ChangeTracker.DetectChanges();
        var changes = ChangeTracker.Entries().Where(e =>
            e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToList();
        var touched = new HashSet<int>();
        foreach (var entry in changes)
        {
            int? appId = entry.Entity switch
            {
                JobApplication app => app.Id,
                Contact contact => contact.JobApplicationId,
                Interview interview => interview.JobApplicationId,
                ApplicationActivity activity => activity.JobApplicationId,
                JobTechnologyRequirement requirement => requirement.JobApplicationId,
                InterviewQuestion question => ChangeTracker.Entries<Interview>()
                    .FirstOrDefault(e => e.Entity.Id == question.InterviewId)?.Entity.JobApplicationId
                    ?? await Interviews.Where(i => i.Id == question.InterviewId)
                        .Select(i => (int?)i.JobApplicationId).FirstOrDefaultAsync(cancellationToken),
                _ => null
            };
            if (appId is > 0) touched.Add(appId.Value);
            if (entry.Entity is Technology technology && entry.State == EntityState.Modified &&
                entry.Property(nameof(Technology.Name)).IsModified)
                touched.UnionWith(await JobTechnologyRequirements.Where(r => r.TechnologyId == technology.Id)
                    .Select(r => r.JobApplicationId).ToListAsync(cancellationToken));
        }
        foreach (var appId in touched)
        {
            var app = await JobApplications.FindAsync([appId], cancellationToken);
            if (app is null || Entry(app).State == EntityState.Deleted) continue;
            // Ensure strictly increasing UTC clocks even for rapid successive changes.
            app.UpdatedAt = DateTime.UtcNow > app.UpdatedAt ? DateTime.UtcNow : app.UpdatedAt.AddTicks(1);
        }
        foreach (var entry in changes.Where(e => e.Entity is Interview))
        {
            if (entry.State != EntityState.Added && !(entry.State == EntityState.Modified &&
                (entry.Property(nameof(Interview.Status)).IsModified ||
                 entry.Property(nameof(Interview.ScheduledDate)).IsModified ||
                 entry.Property(nameof(Interview.Result)).IsModified))) continue;
            var interview = (Interview)entry.Entity;
            var app = await JobApplications.FindAsync([interview.JobApplicationId], cancellationToken);
            if (app is not null) app.LastActivityDate = DateTime.UtcNow;
        }
        foreach (var entry in changes)
        {
            if (entry.Entity is not ApplicationActivity activity ||
                !(entry.State == EntityState.Added || entry.State == EntityState.Modified &&
                    (entry.Property(nameof(ApplicationActivity.ActivityDate)).IsModified ||
                     entry.Property(nameof(ApplicationActivity.ActivityType)).IsModified)) ||
                activity.ActivityType is Models.Enums.ActivityType.Note or Models.Enums.ActivityType.Other)
                continue;
            var app = ChangeTracker.Entries<JobApplication>().Select(e => e.Entity)
                .FirstOrDefault(a => a.Id == activity.JobApplicationId)
                ?? activity.JobApplication;
            if (app is not null && (!app.LastActivityDate.HasValue || activity.ActivityDate > app.LastActivityDate))
                app.LastActivityDate = activity.ActivityDate;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Technology
        modelBuilder.Entity<Technology>(e =>
        {
            e.HasIndex(t => t.Name).IsUnique();
            e.Property(t => t.Name).HasMaxLength(100);
        });

        // ProfileTechnology — one profile entry per technology
        modelBuilder.Entity<ProfileTechnology>(e =>
        {
            e.HasIndex(p => p.TechnologyId).IsUnique();
            e.HasOne(p => p.Technology)
             .WithMany(t => t.ProfileTechnologies)
             .HasForeignKey(p => p.TechnologyId)
             .OnDelete(DeleteBehavior.Cascade);
            e.Property(p => p.YearsExperience).HasPrecision(4, 1);
        });

        // JobApplication
        modelBuilder.Entity<JobApplication>(e =>
        {
            e.HasIndex(a => a.SyncId).IsUnique();
            e.Property(a => a.SalaryMin).HasPrecision(18, 2);
            e.Property(a => a.SalaryMax).HasPrecision(18, 2);
        });

        // Contact
        modelBuilder.Entity<Contact>(e =>
        {
            e.HasOne(c => c.JobApplication)
             .WithMany(a => a.Contacts)
             .HasForeignKey(c => c.JobApplicationId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // JobTechnologyRequirement
        modelBuilder.Entity<JobTechnologyRequirement>(e =>
        {
            e.HasIndex(r => new { r.JobApplicationId, r.TechnologyId }).IsUnique();
            e.HasOne(r => r.JobApplication)
             .WithMany(a => a.TechnologyRequirements)
             .HasForeignKey(r => r.JobApplicationId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(r => r.Technology)
             .WithMany(t => t.JobRequirements)
             .HasForeignKey(r => r.TechnologyId)
             .OnDelete(DeleteBehavior.Restrict);
            e.Property(r => r.RequiredYearsExperience).HasPrecision(4, 1);
        });

        // Interview
        modelBuilder.Entity<Interview>(e =>
        {
            e.HasOne(i => i.JobApplication)
             .WithMany(a => a.Interviews)
             .HasForeignKey(i => i.JobApplicationId)
             .OnDelete(DeleteBehavior.Cascade);
            // Optional contact reference — restrict to prevent cascade conflict
            e.HasOne(i => i.InterviewerContact)
             .WithMany()
             .HasForeignKey(i => i.InterviewerContactId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // InterviewQuestion
        modelBuilder.Entity<InterviewQuestion>(e =>
        {
            e.HasOne(q => q.Interview)
             .WithMany(i => i.Questions)
             .HasForeignKey(q => q.InterviewId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ApplicationActivity
        modelBuilder.Entity<ApplicationActivity>(e =>
        {
            e.HasOne(a => a.JobApplication)
             .WithMany(app => app.Activities)
             .HasForeignKey(a => a.JobApplicationId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.Contact)
             .WithMany()
             .HasForeignKey(a => a.ContactId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(a => a.Interview)
             .WithMany()
             .HasForeignKey(a => a.InterviewId)
             .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
