using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using TestVault.Application.DTOs;
using TestVault.Application.Exceptions;
using TestVault.Application.Interfaces;
using TestVault.Domain.Entities;
using TestVault.Infrastructure.Data;

namespace TestVault.Infrastructure.Repositories;

/// <summary>
/// Dapper implementation of <see cref="ITestCasesRepository"/>. Every query
/// here is a near-verbatim port of the corresponding query in
/// backend/routes/runs.js, backend/routes/tests.js and
/// backend/routes/analysis.js - see each method's XML doc for the exact
/// source.
/// </summary>
public class TestCasesRepository : ITestCasesRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public TestCasesRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    // Ported from the test-cases half of GET /api/runs/:id
    // (backend/routes/runs.js:94-105). aa.test_case_id is selected as the
    // multi-mapping split column: when the LEFT JOIN has no matching
    // ai_analysis row, this column is NULL and Dapper passes a genuine
    // `null` AiAnalysis to the map function (verified empirically before
    // writing this), never a partially-populated object.
    private const string ByRunIdSql = """
        SELECT
            tc.id             AS Id,
            tc.run_id         AS RunId,
            tc.title          AS Title,
            tc.status         AS Status,
            tc.duration_ms    AS DurationMs,
            tc.browser        AS Browser,
            tc.worker_index   AS WorkerIndex,
            tc.retry_count    AS RetryCount,
            tc.error_message  AS ErrorMessage,
            tc.screenshot     AS Screenshot,
            tc.video          AS Video,
            tc.trace          AS Trace,
            tc.created_at     AS CreatedAt,
            aa.test_case_id   AS TestCaseId,
            aa.root_cause     AS RootCause,
            aa.fix_suggestion AS FixSuggestion,
            aa.analyzed_at    AS AnalyzedAt
        FROM test_cases tc
        LEFT JOIN ai_analysis aa ON aa.test_case_id = tc.id
        WHERE tc.run_id = @RunId
        ORDER BY tc.created_at ASC
        """;

    public async Task<IReadOnlyList<TestCase>> GetByRunIdAsync(string runId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

            // run_id is VARCHAR(64) - bind explicitly as AnsiString (see
            // RunsRepository.GetRunDetailAsync for why).
            var parameters = new DynamicParameters();
            parameters.Add("RunId", runId, DbType.AnsiString, size: 64);

            var command = new CommandDefinition(ByRunIdSql, parameters, cancellationToken: cancellationToken);

            var result = await connection.QueryAsync<TestCase, AiAnalysis, TestCase>(
                command,
                (testCase, aiAnalysis) =>
                {
                    testCase.AiAnalysis = aiAnalysis;
                    return testCase;
                },
                splitOn: "TestCaseId");

            return result.AsList();
        }
        catch (SqlException ex)
        {
            throw new RepositoryException($"Failed to load test cases for run '{runId}'.", ex);
        }
    }

    // Ported from GET /api/tests/failed (backend/routes/tests.js:6-25). This
    // is a flat 3-table projection, so it maps directly onto FailedTestDto
    // with a single QueryAsync<T> - no multi-mapping needed.
    private const string FailedTestsSql = """
        SELECT TOP 20
            tc.id             AS Id,
            tc.run_id         AS RunId,
            tc.title          AS Title,
            tc.status         AS Status,
            tc.duration_ms    AS DurationMs,
            tc.browser        AS Browser,
            tc.worker_index   AS WorkerIndex,
            tc.retry_count    AS RetryCount,
            tc.error_message  AS ErrorMessage,
            tc.screenshot     AS Screenshot,
            tc.video          AS Video,
            tc.trace          AS Trace,
            tc.created_at     AS CreatedAt,
            tr.started_at     AS RunStartedAt,
            aa.root_cause     AS RootCause,
            aa.fix_suggestion AS FixSuggestion
        FROM test_cases tc
        JOIN test_runs tr ON tr.id = tc.run_id
        LEFT JOIN ai_analysis aa ON aa.test_case_id = tc.id
        WHERE tc.status = 'failed'
        ORDER BY tc.created_at DESC
        """;

    public async Task<IReadOnlyList<FailedTestDto>> GetFailedTestsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
            var command = new CommandDefinition(FailedTestsSql, cancellationToken: cancellationToken);
            var result = await connection.QueryAsync<FailedTestDto>(command);
            return result.AsList();
        }
        catch (SqlException ex)
        {
            throw new RepositoryException("Failed to load failed tests.", ex);
        }
    }

    // Ported from backend/routes/analysis.js:11-13. Bare lookup, no join -
    // matches its one caller (validating a test case before calling the
    // Claude API).
    private const string TestDetailSql = """
        SELECT
            id             AS Id,
            run_id         AS RunId,
            title          AS Title,
            status         AS Status,
            duration_ms    AS DurationMs,
            browser        AS Browser,
            worker_index   AS WorkerIndex,
            retry_count    AS RetryCount,
            error_message  AS ErrorMessage,
            screenshot     AS Screenshot,
            video          AS Video,
            trace          AS Trace,
            created_at     AS CreatedAt
        FROM test_cases
        WHERE id = @TestCaseId
        """;

    public async Task<TestCase?> GetTestDetailAsync(int testCaseId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
            var command = new CommandDefinition(TestDetailSql, new { TestCaseId = testCaseId }, cancellationToken: cancellationToken);
            return await connection.QuerySingleOrDefaultAsync<TestCase>(command);
        }
        catch (SqlException ex)
        {
            throw new RepositoryException($"Failed to load test case '{testCaseId}'.", ex);
        }
    }

    // Ported from GET /api/tests/slowest (backend/routes/tests.js:28-41).
    // See SlowestTestDto for why AvgMs is int, not a fractional type, and why
    // it's named AvgMs (matching the original query's own "avg_ms" alias)
    // rather than AvgDurationMs.
    private const string SlowestTestsSql = """
        SELECT TOP 10
            title                     AS Title,
            AVG(duration_ms)          AS AvgMs,
            COUNT(*)                  AS RunCount
        FROM test_cases
        GROUP BY title
        ORDER BY AvgMs DESC
        """;

    public async Task<IReadOnlyList<SlowestTestDto>> GetSlowestTestsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
            var command = new CommandDefinition(SlowestTestsSql, cancellationToken: cancellationToken);
            var result = await connection.QueryAsync<SlowestTestDto>(command);
            return result.AsList();
        }
        catch (SqlException ex)
        {
            throw new RepositoryException("Failed to load slowest tests.", ex);
        }
    }
}
