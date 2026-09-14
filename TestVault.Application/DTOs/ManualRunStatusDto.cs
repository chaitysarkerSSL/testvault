namespace TestVault.Application.DTOs;

/// <summary>
/// Response for GET /api/manual/status. Matches the original's
/// res.json({ isRunning }) (backend/routes/manual.js:66) plus RunId (null
/// when nothing is running) - a natural, harmless addition now that
/// IManualRunTracker tracks it.
/// </summary>
public class ManualRunStatusDto
{
    public bool IsRunning { get; set; }
    public string? RunId { get; set; }
}
