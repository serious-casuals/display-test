using System.Collections.Concurrent;
using DisplayLibrary.Interfaces;
using DisplayLibrary.Internal;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace DisplayLibrary.Spectre;

/// <summary>
/// Owns the AnsiConsole.Live() context, the per-command task state, and the render loop.
/// A new instance is created for every command execution via <see cref="SpectreDisplayBuilder"/>.
/// </summary>
internal sealed class SpectreDisplaySession : IDisplaySession
{
    // ── Visual constants ─────────────────────────────────────────────────────
    private static readonly string[] SpinnerFrames =
        ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"];

    private const char   BarChar   = '━'; // U+2501
    private const string CheckIcon = "✓";
    private const string WarnIcon  = "⚠";
    private const string FailIcon  = "✗";

    private const int FigletSmallCharWidth = 7;

    // ── Config ───────────────────────────────────────────────────────────────
    private readonly string         _title;
    private readonly int            _maxConcurrent;
    private readonly SummaryLabels? _summary;
    private readonly DisplayOptions _opts;
    private readonly FigletFont     _figletFont;
    private readonly DateTime       _sessionStartedAt = DateTime.Now;

    // ── Shared state ─────────────────────────────────────────────────────────
    private readonly SharedLog _sharedLog;

    // ── Per-session state ────────────────────────────────────────────────────
    private readonly ConcurrentDictionary<string, TaskState> _activeTasks  = new();
    private readonly List<CompletedTask>                     _completed     = [];
    private readonly object                                  _completedLock = new();

    // ── Render loop machinery ────────────────────────────────────────────────
    private readonly CancellationTokenSource    _cts      = new();
    private readonly TaskCompletionSource<bool> _ctxReady = new();
    private LiveDisplayContext?                 _liveCtx;
    private Task?                               _liveTask;
    private bool                                _disposed;

    // ── Summary counter callbacks ────────────────────────────────────────────
    private Func<int>? _completedCounter;
    private Func<int>? _activeCounter;
    private Func<int>? _queuedCounter;

    // ─────────────────────────────────────────────────────────────────────────

    internal SpectreDisplaySession(
        string         title,
        int            maxConcurrent,
        SummaryLabels? summary,
        SharedLog      sharedLog,
        DisplayOptions opts,
        FigletFont     figletFont)
    {
        _title         = title;
        _maxConcurrent = maxConcurrent;
        _summary       = summary;
        _sharedLog     = sharedLog;
        _opts          = opts;
        _figletFont    = figletFont;
    }

    internal void SetCounterCallbacks(Func<int>? completed, Func<int>? active, Func<int>? queued)
    {
        _completedCounter = completed ?? (() => { lock (_completedLock) return _completed.Count; });
        _activeCounter    = active    ?? (() => _activeTasks.Count);
        _queuedCounter    = queued;
    }

    internal async Task InitAsync()
    {
        _liveTask = Task.Run(async () =>
        {
            try
            {
                await AnsiConsole
                    .Live(new Text(string.Empty))
                    .AutoClear(false)
                    .StartAsync(async ctx =>
                    {
                        _liveCtx = ctx;
                        _ctxReady.TrySetResult(true);
                        await RenderLoopAsync(ctx, _cts.Token).ConfigureAwait(false);
                    })
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _ctxReady.TrySetException(ex);
            }
        });

        await _ctxReady.Task.ConfigureAwait(false);
    }

    // ── IDisplaySession ───────────────────────────────────────────────────────

    public ITaskHandle AddTask(string description, double maxValue)
    {
        var state = new TaskState(Guid.NewGuid().ToString("N"), description, maxValue);
        _activeTasks[state.TaskId] = state;
        return new SpectreTaskHandle(state, this);
    }

    public ITaskHandle AddTask(string description)
    {
        var state = new TaskState(Guid.NewGuid().ToString("N"), description, maxValue: 0)
        {
            IsIndeterminate = true
        };
        _activeTasks[state.TaskId] = state;
        return new SpectreTaskHandle(state, this);
    }

    public void AddLog(string spectreMarkup) => _sharedLog.Add(spectreMarkup);

    public async Task CompleteAsync()
    {
        await StopRenderLoopAsync(flushFinalFrame: true).ConfigureAwait(false);
        PrintCompletionSummary();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await StopRenderLoopAsync(flushFinalFrame: false).ConfigureAwait(false);
        _cts.Dispose();
    }

    // ── Called by SpectreTaskHandle ───────────────────────────────────────────

    /// <summary>
    /// Atomically removes the task from the active dictionary and appends it to the
    /// completed list.  Called by <see cref="SpectreTaskHandle.Complete"/>.
    /// </summary>
    internal void CompleteTask(TaskState state, string? finalDescription)
    {
        if (!_activeTasks.TryRemove(state.TaskId, out _)) return;
        var description = finalDescription ?? state.Description;
        var entry       = new CompletedTask(description, DateTime.Now - state.StartedAt);
        lock (_completedLock)
        {
            _completed.Add(entry);
        }
    }

    // ── Render loop ───────────────────────────────────────────────────────────

    private async Task RenderLoopAsync(LiveDisplayContext ctx, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                ctx.UpdateTarget(BuildDisplay());
                await Task.Delay(_opts.TickMs, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task StopRenderLoopAsync(bool flushFinalFrame)
    {
        if (_cts.IsCancellationRequested) return;
        await _cts.CancelAsync().ConfigureAwait(false);

        if (_liveTask is not null)
        {
            try { await _liveTask.ConfigureAwait(false); }
            catch (OperationCanceledException) { }
        }

        if (flushFinalFrame && _liveCtx is not null)
            _liveCtx.UpdateTarget(BuildDisplay());
    }

    // ── Completion summary ────────────────────────────────────────────────────

    private void PrintCompletionSummary()
    {
        List<CompletedTask> completedSnapshot;
        lock (_completedLock)
            completedSnapshot = [.. _completed];

        var totalElapsed = DateTime.Now - _sessionStartedAt;

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[green] Session Complete [/]") { Style = Style.Parse("grey") });
        AnsiConsole.MarkupLine(
            $"  [green]{CheckIcon}[/]  [white]{completedSnapshot.Count}[/] " +
            $"[grey]task(s) completed in[/] [white]{FormatElapsed(totalElapsed)}[/]");

        if (completedSnapshot.Count > 1)
        {
            AnsiConsole.WriteLine();
            var table = new Table()
                .BorderStyle(Style.Parse("grey"))
                .Border(TableBorder.Rounded)
                .AddColumn(new TableColumn("[grey]Task[/]"))
                .AddColumn(new TableColumn("[grey]Elapsed[/]").RightAligned());

            foreach (var t in completedSnapshot)
                table.AddRow(
                    Markup.Escape(t.Description),
                    $"[white]{t.Elapsed.TotalSeconds:F1}s[/]");

            AnsiConsole.Write(table);
        }

        AnsiConsole.WriteLine();
    }

    private static string FormatElapsed(TimeSpan ts) =>
        ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours}h {ts.Minutes:D2}m {ts.Seconds:D2}s"
            : ts.TotalMinutes >= 1
                ? $"{(int)ts.TotalMinutes}m {ts.Seconds:D2}s"
                : $"{ts.TotalSeconds:F1}s";

    // ── Display construction ──────────────────────────────────────────────────

    private IRenderable BuildDisplay()
    {
        var activeSnapshot = _activeTasks.Values
            .OrderBy(t => t.Description)
            .Take(_maxConcurrent)
            .ToList();

        List<CompletedTask> completedSnapshot;
        lock (_completedLock)
            completedSnapshot = _completed.TakeLast(_opts.MaxCompleted).ToList();

        var logSnapshot = _sharedLog.Snapshot();

        return new Rows(
            BuildTitlePanel(),
            BuildCompletedPanel(completedSnapshot),
            BuildActivePanel(activeSnapshot),
            BuildLogPanel(logSnapshot)
        );
    }

    // ── Zone: Title ───────────────────────────────────────────────────────────

    private Panel BuildTitlePanel()
    {
        var rows = new List<IRenderable> { BuildTitleBanner() };

        if (_summary is not null)
        {
            var summaryLine = BuildSummaryLine();
            if (summaryLine is not null)
                rows.Add(summaryLine);
        }

        return new Panel(new Rows(rows))
        {
            Border      = BoxBorder.Heavy,
            BorderStyle = Style.Parse("grey"),
            Expand      = true,
            Padding     = new Padding(1, 0)
        };
    }

    private IRenderable BuildTitleBanner()
    {
        var plainTitle  = MarkupStripper.Strip(_title);
        var termWidth   = AnsiConsole.Console.Profile.Width;
        var usableWidth = termWidth - 4;

        if (plainTitle.Length * FigletSmallCharWidth > usableWidth)
        {
            return new Rule($"[bold]{_title}[/]")
            {
                Style         = Style.Parse("grey"),
                Justification = Justify.Center
            };
        }

        return new FigletText(_figletFont, plainTitle).Justify(Justify.Center);
    }

    private Markup? BuildSummaryLine()
    {
        if (_summary is null) return null;

        var parts = new List<string>();

        if (_summary.Completed is not null && _completedCounter is not null)
            parts.Add($"[grey]{Markup.Escape(_summary.Completed)}:[/] [white]{_completedCounter()}[/]");

        if (_summary.Active is not null && _activeCounter is not null)
            parts.Add($"[grey]{Markup.Escape(_summary.Active)}:[/] [white]{_activeCounter()}[/]");

        if (_summary.Queued is not null && _queuedCounter is not null)
            parts.Add($"[grey]{Markup.Escape(_summary.Queued)}:[/] [white]{_queuedCounter()}[/]");

        if (parts.Count == 0) return null;

        return new Markup(string.Join("  [grey]│[/]  ", parts));
    }

    // ── Zone: Recently Completed ──────────────────────────────────────────────

    private Panel BuildCompletedPanel(List<CompletedTask> snapshot)
    {
        var rows = new List<IRenderable>();

        for (var i = snapshot.Count; i < _opts.MaxCompleted; i++)
            rows.Add(new Markup(" "));

        foreach (var task in snapshot)
            rows.Add(new Markup(
                $"[green]{CheckIcon}[/] {Markup.Escape(task.Description)}  " +
                $"[grey]{task.Elapsed.TotalSeconds:F1}s[/]"));

        return new Panel(new Rows(rows))
        {
            Header      = new PanelHeader("[green] Recently Completed [/]"),
            Border      = BoxBorder.Rounded,
            BorderStyle = Style.Parse("grey"),
            Expand      = true,
            Padding     = new Padding(1, 0)
        };
    }

    // ── Zone: Active Tasks ────────────────────────────────────────────────────

    private Panel BuildActivePanel(List<TaskState> snapshot)
    {
        var spinnerFrame = GetSpinnerFrame();
        var rows         = new List<IRenderable>();

        foreach (var task in snapshot)
            rows.Add(BuildActiveTaskRow(task, spinnerFrame));

        for (var i = snapshot.Count; i < _maxConcurrent; i++)
            rows.Add(new Markup(" "));

        return new Panel(new Rows(rows))
        {
            Header      = new PanelHeader("[yellow] Active Tasks [/]"),
            Border      = BoxBorder.Rounded,
            BorderStyle = Style.Parse("grey"),
            Expand      = true,
            Padding     = new Padding(1, 0)
        };
    }

    private IRenderable BuildActiveTaskRow(TaskState task, string spinnerFrame)
    {
        var (leadIcon, fillColor) = task.DisplayStatus switch
        {
            TaskDisplayStatus.Warning => ($"[yellow]{WarnIcon}[/]", "yellow"),
            TaskDisplayStatus.Failed  => ($"[red]{FailIcon}[/]",    "red"),
            _                         => ($"[yellow]{spinnerFrame}[/]",
                                          task.Percentage >= 85 ? "green" : "steelblue1")
        };

        // Indeterminate — spinner + label only
        if (task.IsIndeterminate)
            return new Markup($"{leadIcon} {Markup.Escape(task.Description)}");

        // Determinate — full bar + percentage + ETA
        var pct       = task.Percentage;
        var bar       = BuildProgressBar(pct, fillColor);
        var remaining = task.EstimatedRemaining;
        var remStr    = remaining.HasValue
            ? $"{(int)remaining.Value.TotalMinutes:D2}:{remaining.Value.Seconds:D2}"
            : "--:--";

        return new Markup(
            $"{leadIcon} {Markup.Escape(task.Description)} " +
            $"{bar} [white]{pct:F0}%[/] [grey]{remStr}[/]");
    }

    private string BuildProgressBar(double percentage, string fillColor)
    {
        var filled    = (int)(percentage / 100.0 * _opts.BarWidth);
        var empty     = _opts.BarWidth - filled;
        var filledBar = new string(BarChar, filled);
        var emptyBar  = new string(BarChar, empty);
        return $"[{fillColor}]{filledBar}[/][grey]{emptyBar}[/]";
    }

    // ── Zone: Activity Log ────────────────────────────────────────────────────

    private Panel BuildLogPanel(IReadOnlyList<string> snapshot)
    {
        var rows = new List<IRenderable>();

        for (var i = snapshot.Count; i < _opts.MaxLogs; i++)
            rows.Add(new Markup(" "));

        foreach (var entry in snapshot)
            rows.Add(new Markup(entry));

        return new Panel(new Rows(rows))
        {
            Header      = new PanelHeader("[blue] Activity Log [/]"),
            Border      = BoxBorder.Rounded,
            BorderStyle = Style.Parse("grey"),
            Expand      = true,
            Padding     = new Padding(1, 0)
        };
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string GetSpinnerFrame()
    {
        var frameIndex = (int)(DateTime.Now.Ticks / (TimeSpan.TicksPerMillisecond * 100))
                         % SpinnerFrames.Length;
        return SpinnerFrames[frameIndex];
    }
}
