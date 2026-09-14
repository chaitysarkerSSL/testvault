using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TestVault.Application.Interfaces;

namespace TestVault.Infrastructure.ExternalServices;

/// <summary>
/// Spawns `npx playwright test` as a child process and streams its output -
/// the same command scripts/run-tests.js already spawns
/// (`spawn('npx', ['playwright', 'test'], { stdio: 'inherit', shell: true })`),
/// just invoked directly by ManualRunService instead of through that
/// intermediate Node script. See ITestRunnerProcess's doc comment for why
/// this bridges to Node/Playwright rather than reimplementing test
/// execution in C#.
///
/// Reads Playwright:Browser / Playwright:Timeout / Playwright:WorkingDirectory
/// from configuration (see appsettings.json) - following the same pattern as
/// SqlConnectionFactory/ClaudeAiAnalysisProvider (inject IConfiguration
/// directly) rather than introducing IOptions&lt;T&gt; for just this one case.
/// </summary>
public class PlaywrightTestRunnerProcess : ITestRunnerProcess
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PlaywrightTestRunnerProcess> _logger;

    public PlaywrightTestRunnerProcess(IConfiguration configuration, ILogger<PlaywrightTestRunnerProcess> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<int> RunAsync(
        string runId,
        string triggeredBy,
        IReadOnlyDictionary<string, string> environmentOverrides,
        Action<string, bool> onOutputLine,
        CancellationToken cancellationToken)
    {
        var browser = _configuration["Playwright:Browser"] ?? "chromium";
        var timeoutMs = _configuration.GetValue<int?>("Playwright:Timeout") ?? 30000;

        // The ASP.NET Core app's own working directory (its published site
        // path under IIS) is never the repo root where playwright.config.js
        // and package.json live, unlike the old Node server - this MUST be
        // set explicitly in a real deployment; falling back to the current
        // directory only helps local `dotnet run` testing from the repo root.
        var workingDirectory = _configuration["Playwright:WorkingDirectory"];
        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            workingDirectory = Directory.GetCurrentDirectory();
            _logger.LogWarning(
                "Playwright:WorkingDirectory is not configured - falling back to {WorkingDirectory}. " +
                "Set it explicitly in a real deployment (the repo root containing playwright.config.js).",
                workingDirectory);
        }

        var playwrightArgs = $"playwright test --project={browser} --timeout={timeoutMs}";

        var startInfo = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new ProcessStartInfo("cmd.exe", $"/c npx {playwrightArgs}")
            : new ProcessStartInfo("/bin/sh", $"-c \"npx {playwrightArgs}\"");

        startInfo.WorkingDirectory = workingDirectory;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.UseShellExecute = false;
        startInfo.CreateNoWindow = true;

        // TESTVAULT_RUN_ID/TRIGGERED_BY are what reporters/sql-reporter.js's
        // onBegin reads (still the unchanged Node reporter). PLAYWRIGHT_PROJECT
        // activates a fallback the reporter already had but nothing
        // previously set (see sql-reporter.js:25,41) - the reporter's own
        // per-test project() lookup normally wins anyway, so this is a
        // belt-and-suspenders addition, not a behavior change.
        startInfo.Environment["TESTVAULT_RUN_ID"] = runId;
        startInfo.Environment["TRIGGERED_BY"] = triggeredBy;
        startInfo.Environment["PLAYWRIGHT_PROJECT"] = browser;

        foreach (var (key, value) in environmentOverrides)
        {
            startInfo.Environment[key] = value;
        }

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                onOutputLine(e.Data, false);
            }
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
            {
                onOutputLine(e.Data, true);
            }
        };

        if (!process.Start())
        {
            throw new InvalidOperationException("Failed to start the Playwright test process.");
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await using var registration = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    // entireProcessTree=true kills cmd.exe -> npx -> node ->
                    // playwright as one unit. This is a deliberate
                    // correctness improvement over the original: manual.js's
                    // activeProcess.kill('SIGTERM') only ever killed the
                    // outermost node process it spawned directly, and could
                    // leave scripts/run-tests.js's own shell:true-spawned
                    // `npx playwright test` tree running orphaned.
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException)
            {
                // Process already exited between the check and Kill() - fine.
            }
        });

        // Deliberately awaited with CancellationToken.None: even after a
        // stop request kills the process above, we still want to observe
        // its real (killed) exit code rather than throw/abandon it here -
        // ManualRunService decides what a cancelled run reports.
        await process.WaitForExitAsync(CancellationToken.None);

        return process.ExitCode;
    }
}
