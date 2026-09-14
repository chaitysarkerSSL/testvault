using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using TestVault.Application.DTOs;
using TestVault.Application.Exceptions;
using TestVault.Application.Interfaces;
using TestVault.Domain.Entities;
using TestVault.Domain.Enums;
using TestVault.Infrastructure.Data;

namespace TestVault.Infrastructure.Repositories;

/// <summary>
/// Dapper implementation of <see cref="IRunsRepository"/>. Every query here
/// is a near-verbatim port of the corresponding query in
/// backend/routes/runs.js - see each method's XML doc for the exact source.
///
/// test_runs.status, .triggered_by are plain VARCHAR columns; Dapper maps
/// them directly onto the RunStatus/TriggeredBy enum properties via
/// case-insensitive name matching (verified empirically against a live
/// Dapper query before writing this), so no manual string&lt;-&gt;enum
/// conversion is needed here.
/// </summary>
public class RunsRepository : IRunsRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public RunsRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    // Ported from backend/routes/runs.js:12-26.
    private const string TodaySummarySql = """
        SELECT
            COUNT(*)                    AS TotalRuns,
            ISNULL(SUM(passed),  0)     AS TotalPassed,
            ISNULL(SUM(failed),  0)     AS TotalFailed,
            CASE
                WHEN ISNULL(SUM(total), 0) = 0 THEN 0
                ELSE ROUND(SUM(passed) * 100.0 / NULLIF(SUM(total), 0), 1)
            END                         AS AvgPassRate
        FROM test_runs
        WHERE CAST(started_at AS DATE) = CAST(GETDATE() AS DATE)
          AND status != 'running'
        """;

    public async Task<RunSummaryDto> GetTodaySummaryAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
            var command = new CommandDefinition(TodaySummarySql, cancellationToken: cancellationToken);
            var result = await connection.QuerySingleOrDefaultAsync<RunSummaryDto>(command);

            // COUNT(*)/ISNULL(SUM(...)) always return a row even with zero
            // matching runs, but default defensively to match the Node
            // route's own fallback object (runs.js:28-33).
            return result ?? new RunSummaryDto();
        }
        catch (SqlException ex)
        {
            throw new RepositoryException("Failed to load today's run summary.", ex);
        }
    }

    // Ported from backend/routes/runs.js:36-55.
    private const string TrendSql = """
        SELECT *
        FROM (
            SELECT TOP 14
                CAST(started_at AS DATE)  AS RunDate,
                ISNULL(SUM(passed), 0)    AS Passed,
                ISNULL(SUM(failed), 0)    AS Failures,
                CASE
                    WHEN ISNULL(SUM(total), 0) = 0 THEN 0
                    ELSE ROUND(SUM(passed) * 100.0 / NULLIF(SUM(total), 0), 1)
                END                       AS PassRate
            FROM test_runs
            WHERE status != 'running'
            GROUP BY CAST(started_at AS DATE)
            ORDER BY CAST(started_at AS DATE) DESC
        ) t
        ORDER BY RunDate ASC
        """;

    public async Task<IReadOnlyList<RunTrendPointDto>> GetTrendAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
            var command = new CommandDefinition(TrendSql, cancellationToken: cancellationToken);
            var result = await connection.QueryAsync<RunTrendPointDto>(command);
            return result.AsList();
        }
        catch (SqlException ex)
        {
            throw new RepositoryException("Failed to load run trend data.", ex);
        }
    }

    // Column list kept explicit (rather than "SELECT *", which the Node
    // route uses) purely for a deterministic, self-documenting Dapper
    // mapping - the columns and results are identical to the source query.
    // Ported from backend/routes/runs.js:70-81.
    private const string RunListSql = """
        SELECT TOP 50
            id           AS Id,
            total        AS Total,
            passed       AS Passed,
            failed       AS Failed,
            skipped      AS Skipped,
            triggered_by AS TriggeredBy,
            environment  AS Environment,
            browser      AS Browser,
            status       AS Status,
            started_at   AS StartedAt,
            finished_at  AS FinishedAt,
            duration_ms  AS DurationMs
        FROM test_runs
        ORDER BY started_at DESC
        """;

    public async Task<IReadOnlyList<TestRun>> GetRunListAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
            var command = new CommandDefinition(RunListSql, cancellationToken: cancellationToken);
            var result = await connection.QueryAsync<TestRun>(command);
            return result.AsList();
        }
        catch (SqlException ex)
        {
            throw new RepositoryException("Failed to load run list.", ex);
        }
    }

    // Ported from backend/routes/runs.js:90-92 (the run half of GET /:id).
    private const string RunByIdSql = """
        SELECT
            id           AS Id,
            total        AS Total,
            passed       AS Passed,
            failed       AS Failed,
            skipped      AS Skipped,
            triggered_by AS TriggeredBy,
            environment  AS Environment,
            browser      AS Browser,
            status       AS Status,
            started_at   AS StartedAt,
            finished_at  AS FinishedAt,
            duration_ms  AS DurationMs
        FROM test_runs
        WHERE id = @RunId
        """;

    public async Task<TestRun?> GetRunDetailAsync(string runId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

            // id is VARCHAR(64) (see Database/schema.sql). Dapper's default
            // mapping for a C# string parameter is NVARCHAR, which would
            // force an implicit conversion against this column and can
            // prevent the id index from being used efficiently - bind it
            // explicitly as AnsiString to match the column type exactly.
            var parameters = new DynamicParameters();
            parameters.Add("RunId", runId, DbType.AnsiString, size: 64);

            var command = new CommandDefinition(RunByIdSql, parameters, cancellationToken: cancellationToken);
            return await connection.QuerySingleOrDefaultAsync<TestRun>(command);
        }
        catch (SqlException ex)
        {
            throw new RepositoryException($"Failed to load run '{runId}'.", ex);
        }
    }

    // Ported from scripts/run-tests.js:22-33's guarded INSERT.
    private const string CreatePlaceholderRunSql = """
        IF NOT EXISTS (SELECT 1 FROM test_runs WHERE id = @Id)
            INSERT INTO test_runs
                (id, triggered_by, environment, browser, status, total, passed, failed, skipped)
            VALUES
                (@Id, @TriggeredBy, @Environment, @Browser, 'running', 0, 0, 0, 0)
        """;

    public async Task CreatePlaceholderRunAsync(string runId, TriggeredBy triggeredBy, string environment, string browser, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

            var parameters = new DynamicParameters();
            parameters.Add("Id", runId, DbType.AnsiString, size: 64);

            // Dapper only stringifies enum properties on the READ side (via
            // the same case-insensitive name mapping this class's class doc
            // comment describes); passed as a query PARAMETER, an enum
            // serializes as its underlying int by default. Convert
            // explicitly so this binds as the lowercase VARCHAR the column
            // actually expects ("manual"/"scheduler") - see TriggeredBy's
            // own doc comments for why ToLowerInvariant() alone is correct
            // here (both values are single words, no TimedOut-style
            // camelCase case to worry about).
            parameters.Add("TriggeredBy", triggeredBy.ToString().ToLowerInvariant(), DbType.AnsiString, size: 20);
            parameters.Add("Environment", environment, DbType.AnsiString, size: 50);
            parameters.Add("Browser", browser, DbType.AnsiString, size: 50);

            var command = new CommandDefinition(CreatePlaceholderRunSql, parameters, cancellationToken: cancellationToken);
            await connection.ExecuteAsync(command);
        }
        catch (SqlException ex)
        {
            throw new RepositoryException($"Failed to register placeholder run '{runId}'.", ex);
        }
    }
}
