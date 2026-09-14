namespace TestVault.Application.DTOs;

/// <summary>
/// Response for POST /api/manual/run. Matches the original's
/// res.json({ message }) (backend/routes/manual.js:19) plus RunId, a
/// straightforward addition now that the id is known synchronously before
/// responding (see RunStartedEvent's doc comment for why the original
/// couldn't do this).
/// </summary>
public class ManualRunStartResponseDto
{
    public string Message { get; set; } = string.Empty;
    public string RunId { get; set; } = string.Empty;
}
