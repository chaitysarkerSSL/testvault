namespace TestVault.Domain.Entities;

/// <summary>
/// AI-generated (Claude API) root-cause analysis for one failed
/// <see cref="TestCase"/>. One-to-one: <see cref="TestCaseId"/> is both the
/// primary key and the foreign key, matching the existing MERGE upsert in
/// backend/routes/analysis.js, which matches solely on test_case_id and has
/// no separate surrogate id.
/// </summary>
public class AiAnalysis
{
    /// <summary>Primary key and foreign key to <see cref="TestCase.Id"/>.</summary>
    public int TestCaseId { get; set; }

    public string RootCause { get; set; } = string.Empty;
    public string FixSuggestion { get; set; } = string.Empty;

    /// <summary>
    /// Audit field: when the analysis was (last) produced. Populated by a
    /// DB default on first insert and explicitly overwritten with GETDATE()
    /// on re-analysis - see backend/routes/analysis.js's MERGE.
    /// </summary>
    public DateTime AnalyzedAt { get; set; }

    /// <summary>The test case this analysis belongs to.</summary>
    public TestCase? TestCase { get; set; }
}
