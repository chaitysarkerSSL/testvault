namespace TestVault.Application.DTOs;

/// <summary>
/// One day's point on the 14-day pass-rate trend chart. Ported from the
/// "trendResult" query in backend/routes/runs.js (GET /api/runs/stats/summary).
/// </summary>
public class RunTrendPointDto
{
    public DateTime RunDate { get; set; }
    public int Passed { get; set; }
    public int Failures { get; set; }

    /// <summary>Percentage, one decimal place (matches the source SQL's ROUND(..., 1)).</summary>
    public decimal PassRate { get; set; }
}
