using Microsoft.AspNetCore.Components.Routing;

namespace InterviewTracker.Components.Shared;

public class KanbanNavLink : NavLink
{
    protected override bool ShouldMatch(string currentUri) =>
        new Uri(currentUri).AbsolutePath.TrimEnd('/') is "" or "/kanban";
}
