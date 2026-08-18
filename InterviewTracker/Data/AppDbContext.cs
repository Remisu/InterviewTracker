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
