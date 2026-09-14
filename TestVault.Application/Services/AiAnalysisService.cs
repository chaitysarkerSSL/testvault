using TestVault.Application.DTOs;
using TestVault.Application.Exceptions;
using TestVault.Application.Interfaces;
using TestVault.Domain.Entities;
using TestVault.Domain.Enums;

namespace TestVault.Application.Services;

/// <summary>
/// Business logic for AI failure analysis, ported from
/// backend/routes/analysis.js. Composes ITestCasesRepository,
/// IAiAnalysisRepository and the vendor-agnostic IAiAnalysisProvider - no
/// SQL, no direct HTTP calls, no controller/framework dependency.
/// </summary>
public class AiAnalysisService
{
    private readonly ITestCasesRepository _testCasesRepository;
    private readonly IAiAnalysisRepository _aiAnalysisRepository;
    private readonly IAiAnalysisProvider _aiAnalysisProvider;

    public AiAnalysisService(
        ITestCasesRepository testCasesRepository,
        IAiAnalysisRepository aiAnalysisRepository,
        IAiAnalysisProvider aiAnalysisProvider)
    {
        _testCasesRepository = testCasesRepository;
        _aiAnalysisRepository = aiAnalysisRepository;
        _aiAnalysisProvider = aiAnalysisProvider;
    }

    /// <summary>
    /// Retrieve existing analysis for a test case, or null if it hasn't
    /// been analyzed yet. The Node app never had a standalone endpoint for
    /// this (see IAiAnalysisRepository.GetByTestCaseIdAsync's doc comment)
    /// but it's a natural read this service should expose regardless.
    /// </summary>
    public async Task<AiAnalysisResultDto?> GetExistingAnalysisAsync(int testCaseId, CancellationToken cancellationToken = default)
    {
        var analysis = await _aiAnalysisRepository.GetByTestCaseIdAsync(testCaseId, cancellationToken);

        return analysis is null
            ? null
            : new AiAnalysisResultDto { RootCause = analysis.RootCause, FixSuggestion = analysis.FixSuggestion };
    }

    /// <summary>
    /// Validates the test case, asks the configured AI provider to analyze
    /// it, and persists the result. Near-verbatim port of the whole
    /// POST /api/analysis/analyze/:testCaseId flow in
    /// backend/routes/analysis.js:6-79.
    /// </summary>
    /// <exception cref="NotFoundException">No test case exists with this id (ported from analysis.js:16's 404).</exception>
    /// <exception cref="ValidationException">The test case did not fail, so it cannot be analyzed (ported from analysis.js:17-19's 400).</exception>
    public async Task<AiAnalysisResultDto> AnalyzeAsync(int testCaseId, CancellationToken cancellationToken = default)
    {
        // --- Validate test case before analysis (analysis.js:11-19) -----
        var testCase = await _testCasesRepository.GetTestDetailAsync(testCaseId, cancellationToken);
        if (testCase is null)
        {
            throw new NotFoundException($"Test case '{testCaseId}' was not found.");
        }

        if (testCase.Status != TestCaseStatus.Failed)
        {
            // Message preserved verbatim from analysis.js:18 - this is
            // existing user-facing behavior, not translated or reworded.
            throw new ValidationException("শুধু failed test analyze করা যাবে।");
        }

        // --- Trigger AI analysis workflow (analysis.js:22-54) -----------
        var prompt = BuildPrompt(testCase);
        var result = await _aiAnalysisProvider.AnalyzeAsync(prompt, cancellationToken);

        // --- Save analysis result through repository (analysis.js:58-72) -
        // UpsertAsync mirrors the existing MERGE exactly (insert-or-update
        // in one atomic call) - see IAiAnalysisRepository.UpsertAsync.
        await _aiAnalysisRepository.UpsertAsync(testCaseId, result.RootCause, result.FixSuggestion, cancellationToken);

        return new AiAnalysisResultDto { RootCause = result.RootCause, FixSuggestion = result.FixSuggestion };
    }

    /// <summary>
    /// Builds the analysis prompt. Ported verbatim (including the Bengali
    /// instructions and the required JSON reply shape) from
    /// backend/routes/analysis.js:22-36 - deciding what to ask the AI is a
    /// business/application concern, independent of which vendor answers it.
    /// </summary>
    private static string BuildPrompt(TestCase testCase) => $$"""
        তুমি একজন QA Engineer। নিচের Playwright test failure টি analyze করো।

        Test Name: {{testCase.Title}}
        Error Message: {{testCase.ErrorMessage ?? "N/A"}}
        Browser: {{testCase.Browser ?? "chromium"}}
        Duration: {{testCase.DurationMs}}ms
        Retry Count: {{testCase.RetryCount}}

        নিচের format এ JSON দাও (শুধু JSON, অন্য কিছু না):
        {
          "root_cause": "সম্ভাব্য কারণ বাংলায় ২-৩ বাক্যে",
          "fix_suggestion": "কীভাবে ঠিক করবে বাংলায় ধাপে ধাপে"
        }
        """;
}
