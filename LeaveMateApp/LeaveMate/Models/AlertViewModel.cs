namespace LeaveMate.Web.Models;

public enum AlertKind
{
    Success,
    Danger,
    Warning,
    Info
}

/// <summary>
/// Data for the reusable alert component (<c>_Alert.cshtml</c>).
/// </summary>
public class AlertViewModel
{
    public AlertKind Kind { get; set; } = AlertKind.Info;

    /// <summary>Optional bold lead-in shown before the message.</summary>
    public string? Title { get; set; }

    public string Message { get; set; } = string.Empty;

    /// <summary>When true, renders a close button that dismisses the alert.</summary>
    public bool Dismissible { get; set; } = true;
}
