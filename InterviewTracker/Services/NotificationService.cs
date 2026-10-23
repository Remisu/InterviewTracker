namespace InterviewTracker.Services;

// Scoped to a Blazor circuit so the message survives client-side navigation.
public class NotificationService
{
    private string? _message;
    public void Success(string message) => _message = message;
    public string? Take() { var message = _message; _message = null; return message; }
}
