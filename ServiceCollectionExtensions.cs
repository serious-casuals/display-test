using DisplayLibrary.Interfaces;
using DisplayLibrary.Logger;
using DisplayLibrary.Spectre;
using Microsoft.Extensions.DependencyInjection;

namespace DisplayLibrary;

/// <summary>
/// IServiceCollection extension that registers the Display Library.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers IDisplayFactory as a singleton, selecting the Spectre or Logger
    /// implementation based on environment detection.
    /// </summary>
    /// <param name="services">The host's service collection.</param>
    /// <param name="configure">Optional callback to tune DisplayOptions.</param>
    public static IServiceCollection AddDisplayLibrary(
        this IServiceCollection services,
        Action<DisplayOptions>? configure = null)
    {
        var opts = new DisplayOptions();
        configure?.Invoke(opts);

        // Register options so they can also be injected directly if needed
        services.AddSingleton(opts);

        if (IsNonInteractive())
        {
            // Delegate all output to ILogger — no console rendering
            services.AddSingleton<IDisplayFactory, LoggerDisplayFactory>();
        }
        else
        {
            // Full Spectre-backed interactive display
            services.AddSingleton<IDisplayFactory>(_ => new SpectreDisplayFactory(opts));
        }

        return services;
    }

    // ── Environment detection ─────────────────────────────────────────────────

    /// <summary>
    /// Returns <c>true</c> when the process should not attempt ANSI rendering.
    /// Detection is evaluated in order; first match wins.
    /// </summary>
    private static bool IsNonInteractive()
    {
        // 1. Stdout piped to file or another process
        if (Console.IsOutputRedirected) return true;

        // 2. Stderr redirected
        if (Console.IsErrorRedirected) return true;

        // 3. Service host or non-interactive session
        if (!Environment.UserInteractive) return true;

        // 4. Known CI environment variables
        if (IsCiEnvironment()) return true;

        return false;
    }

    private static readonly string[] CiEnvironmentVariables =
    [
        "CI",
        "TF_BUILD",
        "GITHUB_ACTIONS",
        "TEAMCITY_VERSION"
    ];

    private static bool IsCiEnvironment() =>
        CiEnvironmentVariables.Any(v =>
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(v)));
}
