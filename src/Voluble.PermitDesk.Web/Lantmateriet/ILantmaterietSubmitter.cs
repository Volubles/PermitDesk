namespace Voluble.PermitDesk.Web.Lantmateriet;

public interface ILantmaterietSubmitter
{
    /// <summary>
    /// Drives Lantmäteriet's spridningstillstånd form to a successful submission
    /// for the given session. Pushes step updates through the session,
    /// awaits broker confirmation before the final click on Skicka, and
    /// stores the confirmation screenshot on the session on completion.
    ///
    /// Designed to run as a background task — the Submit.razor page reacts
    /// to session state changes rather than awaiting this directly.
    /// </summary>
    Task RunAsync(SubmissionSession session);
}
