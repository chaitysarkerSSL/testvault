namespace TestVault.Application.DTOs;

/// <summary>
/// A bare { message } response. Matches the original's
/// res.json({ message }) shape used by POST /api/manual/stop
/// (backend/routes/manual.js:77) - kept generic/reusable rather than named
/// after that one endpoint since it's just a literal string, not a
/// projection of any particular data.
/// </summary>
public class MessageResponseDto
{
    public string Message { get; set; } = string.Empty;
}
