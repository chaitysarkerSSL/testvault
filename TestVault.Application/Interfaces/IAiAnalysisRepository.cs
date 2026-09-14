using TestVault.Domain.Entities;

namespace TestVault.Application.Interfaces;

/// <summary>
/// Read/write access to ai_analysis. Ported from backend/routes/analysis.js.
/// Unlike test_runs/test_cases, this table IS written by the web
/// application itself (the AI-analysis feature is being ported to C#, not
/// left on the Node side).
/// </summary>
public interface IAiAnalysisRepository
{
    /// <summary>
    /// The AI analysis for one test case, or null if it hasn't been
    /// analyzed yet. The Node app never had a standalone endpoint for this
    /// (it only ever appears embedded in the JOINs in runs.js/tests.js),
    /// but it's a natural, explicitly requested read that stands on its own
    /// against the ai_analysis table.
    /// </summary>
    Task<AiAnalysis?> GetByTestCaseIdAsync(int testCaseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a new AI analysis row. Throws if one already exists for this
    /// test case - use <see cref="UpsertAsync"/> if the caller doesn't
    /// already know which case applies.
    /// </summary>
    Task InsertAsync(AiAnalysis analysis, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing AI analysis row (root cause, fix suggestion, and
    /// AnalyzedAt). No-ops at the database level (affects 0 rows) if none
    /// exists yet for this test case - callers should check via
    /// <see cref="GetByTestCaseIdAsync"/> first, or prefer
    /// <see cref="UpsertAsync"/>.
    /// </summary>
    Task UpdateAsync(AiAnalysis analysis, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts or updates the AI analysis for a test case in one atomic
    /// database operation. This is a near-verbatim port of the existing
    /// MERGE statement in backend/routes/analysis.js:63-72, and is the
    /// method that flow should actually call: doing the equivalent by
    /// composing GetByTestCaseIdAsync + Insert/Update from the outside
    /// would introduce a check-then-act race that the original MERGE does
    /// not have.
    /// </summary>
    Task UpsertAsync(int testCaseId, string rootCause, string fixSuggestion, CancellationToken cancellationToken = default);
}
