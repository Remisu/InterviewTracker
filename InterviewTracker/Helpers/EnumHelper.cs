using InterviewTracker.Models.Enums;

namespace InterviewTracker.Helpers;

public static class EnumHelper
{
    // ── ApplicationStage ─────────────────────────────────────────────────────

    public static string DisplayName(ApplicationStage stage) => stage switch
    {
        ApplicationStage.Interested         => "Interested",
        ApplicationStage.Applied            => "Applied / Waiting",
        ApplicationStage.Waiting            => "Waiting",
        ApplicationStage.RecruiterScreening => "Recruiter Screening",
        ApplicationStage.Interview          => "Interview",
        ApplicationStage.TechnicalAssessment => "Technical Assessment",
        ApplicationStage.ClientInterview    => "Client Interview",
        ApplicationStage.FinalInterview     => "Final Interview",
        ApplicationStage.Offer              => "Offer",
        ApplicationStage.Accepted           => "Accepted",
        ApplicationStage.Rejected           => "Rejected",
        ApplicationStage.Withdrawn          => "Withdrawn",
        ApplicationStage.NoResponse         => "No Response",
        _                                   => stage.ToString()
    };

    public static string BadgeClass(ApplicationStage stage) => stage switch
    {
        ApplicationStage.Interested         => "badge bg-secondary",
        ApplicationStage.Applied            => "badge bg-primary",
        ApplicationStage.Waiting            => "badge bg-info text-dark",
        ApplicationStage.RecruiterScreening => "badge bg-warning text-dark",
        ApplicationStage.Interview          => "badge bg-warning text-dark",
        ApplicationStage.TechnicalAssessment => "badge bg-warning text-dark",
        ApplicationStage.ClientInterview    => "badge bg-warning text-dark",
        ApplicationStage.FinalInterview     => "badge bg-warning text-dark",
        ApplicationStage.Offer              => "badge bg-success",
        ApplicationStage.Accepted           => "badge bg-success",
        ApplicationStage.Rejected           => "badge bg-danger",
        ApplicationStage.Withdrawn          => "badge bg-dark",
        ApplicationStage.NoResponse         => "badge bg-secondary",
        _                                   => "badge bg-secondary"
    };

    // ── Priority ─────────────────────────────────────────────────────────────

    public static string DisplayName(Priority priority) => priority switch
    {
        Priority.Low      => "Low",
        Priority.Medium   => "Medium",
        Priority.High     => "High",
        Priority.DreamJob => "Dream Job",
        _                 => priority.ToString()
    };

    public static string BadgeClass(Priority priority) => priority switch
    {
        Priority.Low      => "badge bg-secondary",
        Priority.Medium   => "badge bg-primary",
        Priority.High     => "badge bg-warning text-dark",
        Priority.DreamJob => "badge bg-danger",
        _                 => "badge bg-secondary"
    };

    // ── WorkMode ─────────────────────────────────────────────────────────────

    public static string DisplayName(WorkMode mode) => mode switch
    {
        WorkMode.OnSite => "On-site",
        WorkMode.Hybrid => "Hybrid",
        WorkMode.Remote => "Remote",
        _               => mode.ToString()
    };

    // ── EmploymentType ────────────────────────────────────────────────────────

    public static string DisplayName(EmploymentType type) => type switch
    {
        EmploymentType.Permanent  => "Permanent",
        EmploymentType.Contract   => "Contract",
        EmploymentType.Freelance  => "Freelance",
        EmploymentType.PartTime   => "Part-time",
        EmploymentType.Internship => "Internship",
        _                         => type.ToString()
    };

    // ── ContactType ───────────────────────────────────────────────────────────

    public static string DisplayName(ContactType type) => type switch
    {
        ContactType.Recruiter      => "Recruiter",
        ContactType.HR             => "HR",
        ContactType.HiringManager  => "Hiring Manager",
        ContactType.Interviewer    => "Interviewer",
        ContactType.Client         => "Client",
        ContactType.Referral       => "Referral",
        ContactType.Other          => "Other",
        _                          => type.ToString()
    };

    // ── ContactDirection ──────────────────────────────────────────────────────

    public static string DisplayName(ContactDirection dir) => dir switch
    {
        ContactDirection.TheyContactedMe => "They contacted me",
        ContactDirection.IContactedThem  => "I contacted them",
        _                                => dir.ToString()
    };

    // ── TechnologyCategory ────────────────────────────────────────────────────

    public static string DisplayName(TechnologyCategory cat) => cat switch
    {
        TechnologyCategory.ProgrammingLanguage => "Programming Language",
        TechnologyCategory.Framework           => "Framework",
        TechnologyCategory.Database            => "Database",
        TechnologyCategory.Cloud               => "Cloud",
        TechnologyCategory.DevOps              => "DevOps",
        TechnologyCategory.Architecture        => "Architecture",
        TechnologyCategory.Frontend            => "Frontend",
        TechnologyCategory.Backend             => "Backend",
        TechnologyCategory.Testing             => "Testing",
        TechnologyCategory.Tool                => "Tool",
        TechnologyCategory.Other               => "Other",
        _                                      => cat.ToString()
    };

    // ── RequirementLevel ──────────────────────────────────────────────────────

    public static string DisplayName(RequirementLevel level) => level switch
    {
        RequirementLevel.NiceToHave => "Nice to Have",
        RequirementLevel.Preferred  => "Preferred",
        RequirementLevel.Required   => "Required",
        RequirementLevel.Critical   => "Critical",
        _                           => level.ToString()
    };

    public static string BadgeClass(RequirementLevel level) => level switch
    {
        RequirementLevel.NiceToHave => "badge bg-secondary",
        RequirementLevel.Preferred  => "badge bg-info text-dark",
        RequirementLevel.Required   => "badge bg-warning text-dark",
        RequirementLevel.Critical   => "badge bg-danger",
        _                           => "badge bg-secondary"
    };

    // ── SkillLevel ────────────────────────────────────────────────────────────

    public static string DisplayName(SkillLevel level) => level switch
    {
        SkillLevel.Beginner     => "Beginner",
        SkillLevel.Basic        => "Basic",
        SkillLevel.Intermediate => "Intermediate",
        SkillLevel.Advanced     => "Advanced",
        SkillLevel.Expert       => "Expert",
        _                       => level.ToString()
    };

    // ── InterviewType ─────────────────────────────────────────────────────────

    public static string DisplayName(InterviewType type) => type switch
    {
        InterviewType.RecruiterScreening => "Recruiter Screening",
        InterviewType.Initial            => "Initial Interview",
        InterviewType.HR                 => "HR Interview",
        InterviewType.Technical          => "Technical Interview",
        InterviewType.Coding             => "Coding Interview",
        InterviewType.SystemDesign       => "System Design",
        InterviewType.Client             => "Client Interview",
        InterviewType.HiringManager      => "Hiring Manager",
        InterviewType.FollowUp           => "Follow-up",
        InterviewType.Final              => "Final Interview",
        InterviewType.CEO                => "CEO Interview",
        InterviewType.CultureFit         => "Culture Fit",
        InterviewType.Other              => "Other",
        _                                => type.ToString()
    };

    // ── InterviewStatus ───────────────────────────────────────────────────────

    public static string DisplayName(InterviewStatus status) => status switch
    {
        InterviewStatus.Scheduled   => "Scheduled",
        InterviewStatus.Completed   => "Completed",
        InterviewStatus.Cancelled   => "Cancelled",
        InterviewStatus.Rescheduled => "Rescheduled",
        _                           => status.ToString()
    };

    public static string BadgeClass(InterviewStatus status) => status switch
    {
        InterviewStatus.Scheduled   => "badge bg-info text-dark",
        InterviewStatus.Completed   => "badge bg-success",
        InterviewStatus.Cancelled   => "badge bg-danger",
        InterviewStatus.Rescheduled => "badge bg-warning text-dark",
        _                           => "badge bg-secondary"
    };

    // ── ActivityType ──────────────────────────────────────────────────────────

    public static string DisplayName(ActivityType type) => type switch
    {
        ActivityType.Applied                => "Applied",
        ActivityType.EmailSent              => "Email Sent",
        ActivityType.EmailReceived          => "Email Received",
        ActivityType.RecruiterContact       => "Recruiter Contact",
        ActivityType.PhoneCall              => "Phone Call",
        ActivityType.InterviewScheduled     => "Interview Scheduled",
        ActivityType.InterviewCompleted     => "Interview Completed",
        ActivityType.TechnicalTestReceived  => "Technical Test Received",
        ActivityType.TechnicalTestSubmitted => "Technical Test Submitted",
        ActivityType.FollowUpSent           => "Follow-up Sent",
        ActivityType.StatusChanged          => "Status Changed",
        ActivityType.Rejected               => "Rejected",
        ActivityType.OfferReceived          => "Offer Received",
        ActivityType.Note                   => "Note",
        ActivityType.Other                  => "Other",
        _                                   => type.ToString()
    };

    public static string IconClass(ActivityType type) => type switch
    {
        ActivityType.Applied                => "bi bi-send",
        ActivityType.EmailSent              => "bi bi-envelope",
        ActivityType.EmailReceived          => "bi bi-envelope-open",
        ActivityType.RecruiterContact       => "bi bi-person-lines-fill",
        ActivityType.PhoneCall              => "bi bi-telephone",
        ActivityType.InterviewScheduled     => "bi bi-calendar-plus",
        ActivityType.InterviewCompleted     => "bi bi-calendar-check",
        ActivityType.TechnicalTestReceived  => "bi bi-clipboard",
        ActivityType.TechnicalTestSubmitted => "bi bi-clipboard-check",
        ActivityType.FollowUpSent           => "bi bi-arrow-repeat",
        ActivityType.StatusChanged          => "bi bi-arrow-right-circle",
        ActivityType.Rejected               => "bi bi-x-circle",
        ActivityType.OfferReceived          => "bi bi-trophy",
        ActivityType.Note                   => "bi bi-sticky",
        ActivityType.Other                  => "bi bi-three-dots",
        _                                   => "bi bi-circle"
    };

    // ── QuestionCategory ──────────────────────────────────────────────────────

    public static string DisplayName(QuestionCategory cat) => cat switch
    {
        QuestionCategory.CSharp       => "C#",
        QuestionCategory.DotNet       => ".NET",
        QuestionCategory.SQL          => "SQL",
        QuestionCategory.Architecture => "Architecture",
        QuestionCategory.Azure        => "Azure",
        QuestionCategory.React        => "React",
        QuestionCategory.Behavioral   => "Behavioral",
        QuestionCategory.Algorithms   => "Algorithms",
        QuestionCategory.SystemDesign => "System Design",
        QuestionCategory.Japanese     => "Japanese",
        QuestionCategory.Company      => "Company",
        QuestionCategory.Other        => "Other",
        _                             => cat.ToString()
    };

    // ── Generic IEnumerable helpers ───────────────────────────────────────────

    public static IEnumerable<ApplicationStage> AllStages()       => Enum.GetValues<ApplicationStage>();
    public static IEnumerable<Priority> AllPriorities()           => Enum.GetValues<Priority>();
    public static IEnumerable<WorkMode> AllWorkModes()            => Enum.GetValues<WorkMode>();
    public static IEnumerable<EmploymentType> AllEmploymentTypes() => Enum.GetValues<EmploymentType>();
    public static IEnumerable<TechnologyCategory> AllTechCategories() => Enum.GetValues<TechnologyCategory>();
    public static IEnumerable<RequirementLevel> AllRequirementLevels() => Enum.GetValues<RequirementLevel>();
    public static IEnumerable<SkillLevel> AllSkillLevels()        => Enum.GetValues<SkillLevel>();
    public static IEnumerable<ContactType> AllContactTypes()      => Enum.GetValues<ContactType>();
    public static IEnumerable<ContactDirection> AllContactDirections() => Enum.GetValues<ContactDirection>();
    public static IEnumerable<InterviewType> AllInterviewTypes()  => Enum.GetValues<InterviewType>();
    public static IEnumerable<InterviewStatus> AllInterviewStatuses() => Enum.GetValues<InterviewStatus>();
    public static IEnumerable<ActivityType> AllActivityTypes()    => Enum.GetValues<ActivityType>();
    public static IEnumerable<QuestionCategory> AllQuestionCategories() => Enum.GetValues<QuestionCategory>();
}
