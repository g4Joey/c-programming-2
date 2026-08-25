namespace LeaveMate.Web.Models;

/// <summary>
/// Data for the reusable modal component (<c>_Modal.cshtml</c>).
/// Trigger it from any element with <c>data-modal-open="{Id}"</c>.
/// </summary>
public class ModalViewModel
{
    /// <summary>Unique id used to wire triggers and close buttons to this modal.</summary>
    public string Id { get; set; } = "modal";

    public string Title { get; set; } = string.Empty;

/// <summary>Body copy. For richer content, create a dedicated partial (or a ViewComponent) for the modal body.</summary>
    public string? Body { get; set; }

    public string ConfirmText { get; set; } = "Confirm";

    public string CancelText { get; set; } = "Cancel";

    /// <summary>Visual weight of the confirm action.</summary>
    public AlertKind ConfirmKind { get; set; } = AlertKind.Info;
}
