namespace InterviewTracker.Models;

public class SyncInstallation
{
    public int Id { get; set; } = 1;
    public Guid InstallationId { get; set; } = Guid.NewGuid();
}
