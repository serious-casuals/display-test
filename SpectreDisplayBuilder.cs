using System.Reflection;
using DisplayLibrary.Interfaces;
using DisplayLibrary.Internal;
using Spectre.Console;

namespace DisplayLibrary.Spectre;

/// <summary>
/// Fluent configuration surface for a single Spectre-rendered command session.
/// Created fresh by <see cref="SpectreDisplayFactory.Create"/>.
/// </summary>
internal sealed class SpectreDisplayBuilder : IDisplayBuilder
{
    private readonly SharedLog      _sharedLog;
    private readonly DisplayOptions _opts;

    private string         _title         = "Display";
    private int            _maxConcurrent = 5;
    private SummaryLabels? _summary;
    private Func<int>?     _queuedCounter;

    internal SpectreDisplayBuilder(SharedLog sharedLog, DisplayOptions opts)
    {
        _sharedLog = sharedLog;
        _opts      = opts;
    }

    public IDisplayBuilder WithTitle(string spectreMarkup)
    {
        _title = spectreMarkup;
        return this;
    }

    public IDisplayBuilder WithTasks(int maxConcurrent)
    {
        _maxConcurrent = Math.Max(1, maxConcurrent);
        return this;
    }

    public IDisplayBuilder WithSummary(string? completed = null,
                                       string? active    = null,
                                       string? queued    = null)
    {
        // Only store if at least one label was given
        if (completed is not null || active is not null || queued is not null)
            _summary = new SummaryLabels(completed, active, queued);
        return this;
    }

    public IDisplayBuilder WithQueuedCounter(Func<int> counter)
    {
        _queuedCounter = counter;
        return this;
    }

    public async Task<IDisplaySession> StartAsync()
    {
        var font    = LoadFigletFont(_opts.FigletFont);
        var session = new SpectreDisplaySession(
            _title,
            _maxConcurrent,
            _summary,
            _sharedLog,
            _opts,
            font);

        // Wire default counter callbacks; queued is caller-supplied or omitted
        session.SetCounterCallbacks(null, null, _queuedCounter);

        await session.InitAsync().ConfigureAwait(false);
        return session;
    }

    // ── Font loading ──────────────────────────────────────────────────────────

    /// <summary>
    /// Tries to load the named FigletFont from:
    /// 1. A &lt;name&gt;.flf file next to the executing assembly
    /// 2. An embedded resource at DisplayLibrary.Fonts.&lt;name&gt;.flf
    /// Falls back to <see cref="FigletFont.Default"/> if neither is found.
    /// </summary>
    private static FigletFont LoadFigletFont(string name)
    {
        try
        {
            // 1. Side-by-side file
            var assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";
            var path        = Path.Combine(assemblyDir, $"{name}.flf");
            if (File.Exists(path))
                return FigletFont.Load(path);

            // 2. Embedded resource
            var resourceName = $"DisplayLibrary.Fonts.{name}.flf";
            using var stream = Assembly.GetExecutingAssembly()
                                       .GetManifestResourceStream(resourceName);
            if (stream is not null)
                return FigletFont.Load(stream);
        }
        catch
        {
            // Fall through to default
        }

        return FigletFont.Default;
    }
}
