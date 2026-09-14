namespace TestVault.Application.Interfaces;

/// <summary>
/// Abstraction over whichever AI vendor produces a root-cause analysis for
/// a failed test. Ported from the Claude API call in
/// backend/routes/analysis.js, but deliberately vendor-agnostic: the
/// implementation could be Claude, OpenAI, or a local placeholder without
/// AiAnalysisService (or anything else in this project) changing.
///
/// The prompt text itself is built by AiAnalysisService (a business
/// decision - "what to ask") and handed in as a plain string; everything
/// vendor-specific (endpoint, auth headers, request/response envelope,
/// parsing the raw reply into structured fields) is the implementation's
/// concern, not the caller's - see TestVault.Infrastructure/ExternalServices.
/// </summary>
public interface IAiAnalysisProvider
{
    Task<AiAnalysisResult> AnalyzeAsync(string prompt, CancellationToken cancellationToken = default);
}

/// <summary>The AI vendor's structured reply, already parsed.</summary>
public class AiAnalysisResult
{
    public string RootCause { get; set; } = string.Empty;
    public string FixSuggestion { get; set; } = string.Empty;
}
