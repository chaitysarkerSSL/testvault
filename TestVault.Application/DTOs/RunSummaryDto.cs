namespace TestVault.Application.DTOs;

/// <summary>
/// Today's run stats for the dashboard. Ported from the "todayResult" query
/// in backend/routes/runs.js (GET /api/runs/stats/summary).
/// </summary>
public class RunSummaryDto
{
    public int TotalRuns { get; set; }
    public int TotalPassed { get; set; }
    public int TotalFailed { get; set; }

    /// <summary>
    /// Percentage, one decimal place (matches the source SQL's ROUND(..., 1)).
    /// </summary>
    public decimal AvgPassRate { get; set; }
}
