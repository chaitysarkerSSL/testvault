using Microsoft.Extensions.DependencyInjection;
using TestVault.Application.Services;

namespace TestVault.Application;

/// <summary>
/// Composition-root wiring for TestVault.Application. Called once from
/// TestVault.Web's Program.cs:
///
///     builder.Services.AddApplication();
///
/// Registers the concrete service classes directly (no interfaces) since,
/// unlike the repositories, nothing else in this solution needs an
/// alternate implementation of them.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<RunsService>();
        services.AddScoped<TestsService>();
        services.AddScoped<AiAnalysisService>();
        services.AddScoped<ManualRunService>();

        return services;
    }
}
