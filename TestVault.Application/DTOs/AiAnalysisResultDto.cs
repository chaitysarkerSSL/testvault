namespace TestVault.Application.DTOs;

/// <summary>
/// The API-facing shape of an AI analysis result. Matches
/// res.json(analysis) in backend/routes/analysis.js:74, i.e.
/// { root_cause, fix_suggestion } - returned both when an analysis already
/// exists and after a new one is produced.
/// </summary>
public class AiAnalysisResultDto
{
    public string RootCause { get; set; } = string.Empty;
    public string FixSuggestion { get; set; } = string.Empty;
}
