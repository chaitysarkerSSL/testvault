namespace TestVault.Application.DTOs;

/// <summary>
/// The dashboard's combined response: today's stats + the 14-day trend.
/// Matches the exact shape of GET /api/runs/stats/summary's
/// res.json({ today, trend }) in backend/routes/runs.js:57-60 - produced by
/// RunsService.GetDashboardSummaryAsync composing two independent
/// repository calls (RunSummaryDto/RunTrendPointDto already exist from
/// Phase 2 and are reused here unchanged).
/// </summary>
public class DashboardDto
{
    public RunSummaryDto Today { get; set; } = new();
    public IReadOnlyList<RunTrendPointDto> Trend { get; set; } = Array.Empty<RunTrendPointDto>();
}
