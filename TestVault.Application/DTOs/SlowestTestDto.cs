namespace TestVault.Application.DTOs;

/// <summary>
/// One row of the "slowest tests" report. Ported from backend/routes/tests.js
/// (GET /api/tests/slowest): SELECT TOP 10 title, AVG(duration_ms), COUNT(*)
/// FROM test_cases GROUP BY title.
///
/// Not explicitly listed among ITestCasesRepository's stated responsibilities,
/// but included because /api/tests/slowest is a real, existing endpoint/query
/// discovered while analyzing tests.js - see the migration summary for why
/// this was added.
/// </summary>
public class SlowestTestDto
{
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// AVG(duration_ms) where duration_ms is INT - SQL Server's AVG() of an
    /// integer column performs integer division and returns an INT, exactly
    /// as the existing Node app already returns it. Kept as int here rather
    /// than "corrected" to a fractional type, to preserve current behavior.
    /// Named AvgMs (not AvgDurationMs) to match the query's own column alias
    /// exactly - tests.js:32 aliases this AVG(duration_ms) AS avg_ms, and
    /// Phase 4's snake_case naming policy derives the wire property name
    /// directly from this C# name, so the two must match verbatim.
    /// </summary>
    public int AvgMs { get; set; }

    public int RunCount { get; set; }
}
