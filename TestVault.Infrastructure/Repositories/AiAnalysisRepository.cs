using Dapper;
using Microsoft.Data.SqlClient;
using TestVault.Application.Exceptions;
using TestVault.Application.Interfaces;
using TestVault.Domain.Entities;
using TestVault.Infrastructure.Data;

namespace TestVault.Infrastructure.Repositories;

/// <summary>
/// Dapper implementation of <see cref="IAiAnalysisRepository"/>. Ported
/// from backend/routes/analysis.js.
/// </summary>
public class AiAnalysisRepository : IAiAnalysisRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AiAnalysisRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    private const string GetByTestCaseIdSql = """
        SELECT
            test_case_id   AS TestCaseId,
            root_cause     AS RootCause,
            fix_suggestion AS FixSuggestion,
            analyzed_at    AS AnalyzedAt
        FROM ai_analysis
        WHERE test_case_id = @TestCaseId
        """;

    public async Task<AiAnalysis?> GetByTestCaseIdAsync(int testCaseId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
            var command = new CommandDefinition(GetByTestCaseIdSql, new { TestCaseId = testCaseId }, cancellationToken: cancellationToken);
            return await connection.QuerySingleOrDefaultAsync<AiAnalysis>(command);
        }
        catch (SqlException ex)
        {
            throw new RepositoryException($"Failed to load AI analysis for test case '{testCaseId}'.", ex);
        }
    }

    private const string InsertSql = """
        INSERT INTO ai_analysis (test_case_id, root_cause, fix_suggestion)
        VALUES (@TestCaseId, @RootCause, @FixSuggestion)
        """;

    public async Task InsertAsync(AiAnalysis analysis, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
            var command = new CommandDefinition(
                InsertSql,
                new { analysis.TestCaseId, analysis.RootCause, analysis.FixSuggestion },
                cancellationToken: cancellationToken);
            await connection.ExecuteAsync(command);
        }
        catch (SqlException ex)
        {
            throw new RepositoryException($"Failed to insert AI analysis for test case '{analysis.TestCaseId}'.", ex);
        }
    }

    private const string UpdateSql = """
        UPDATE ai_analysis
        SET root_cause     = @RootCause,
            fix_suggestion = @FixSuggestion,
            analyzed_at    = GETDATE()
        WHERE test_case_id = @TestCaseId
        """;

    public async Task UpdateAsync(AiAnalysis analysis, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
            var command = new CommandDefinition(
                UpdateSql,
                new { analysis.TestCaseId, analysis.RootCause, analysis.FixSuggestion },
                cancellationToken: cancellationToken);
            await connection.ExecuteAsync(command);
        }
        catch (SqlException ex)
        {
            throw new RepositoryException($"Failed to update AI analysis for test case '{analysis.TestCaseId}'.", ex);
        }
    }

    // Near-verbatim port of backend/routes/analysis.js:63-72's MERGE - this
    // is the atomic upsert that flow should actually call.
    private const string UpsertSql = """
        MERGE ai_analysis AS target
        USING (VALUES (@TestCaseId)) AS source(test_case_id)
        ON target.test_case_id = source.test_case_id
        WHEN MATCHED THEN
            UPDATE SET root_cause = @RootCause, fix_suggestion = @FixSuggestion, analyzed_at = GETDATE()
        WHEN NOT MATCHED THEN
            INSERT (test_case_id, root_cause, fix_suggestion)
            VALUES (@TestCaseId, @RootCause, @FixSuggestion);
        """;

    public async Task UpsertAsync(int testCaseId, string rootCause, string fixSuggestion, CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
            var command = new CommandDefinition(
                UpsertSql,
                new { TestCaseId = testCaseId, RootCause = rootCause, FixSuggestion = fixSuggestion },
                cancellationToken: cancellationToken);
            await connection.ExecuteAsync(command);
        }
        catch (SqlException ex)
        {
            throw new RepositoryException($"Failed to save AI analysis for test case '{testCaseId}'.", ex);
        }
    }
}
