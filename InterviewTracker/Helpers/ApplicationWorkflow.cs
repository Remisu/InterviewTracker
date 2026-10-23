using InterviewTracker.Models;
using InterviewTracker.Models.Enums;

namespace InterviewTracker.Helpers;

public static class ApplicationWorkflow
{
    public static readonly ApplicationStage[] Stages =
    [
        ApplicationStage.Applied, ApplicationStage.RecruiterScreening, ApplicationStage.ClientInterview,
        ApplicationStage.TechnicalAssessment, ApplicationStage.FinalInterview,
        ApplicationStage.Rejected, ApplicationStage.NoResponse
    ];

    // Keep legacy values intact. Ambiguous stages require a deliberate user choice.
    public static ApplicationStage BoardStage(ApplicationStage stage) =>
        stage == ApplicationStage.Waiting ? ApplicationStage.Applied : stage;

    public static bool NeedsReview(ApplicationStage stage) => !Stages.Contains(BoardStage(stage));

    public static IOrderedEnumerable<JobApplication> Order(IEnumerable<JobApplication> applications) =>
        applications.OrderByDescending(a => a.DaysSinceLastActivity).ThenBy(a => a.CreatedAt).ThenBy(a => a.SyncId);
}
