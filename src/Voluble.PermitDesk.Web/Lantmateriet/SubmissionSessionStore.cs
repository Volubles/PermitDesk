using System.Collections.Concurrent;

namespace Voluble.PermitDesk.Web.Lantmateriet;

/// <summary>
/// Singleton in-memory store of active submissions. Survives Blazor circuit
/// reconnects (which lose component state) so the broker can refresh the
/// /skicka page mid-submission and still see status. Sessions are evicted
/// when the submitter finishes or the broker cancels.
/// </summary>
public class SubmissionSessionStore
{
    private readonly ConcurrentDictionary<Guid, SubmissionSession> _sessions = new();

    public SubmissionSession Start(SubmissionContext context)
    {
        var session = new SubmissionSession(context.CaseId, context);
        _sessions[context.CaseId] = session;
        return session;
    }

    public SubmissionSession? Get(Guid caseId)
        => _sessions.TryGetValue(caseId, out var s) ? s : null;

    public void End(Guid caseId) => _sessions.TryRemove(caseId, out _);
}
