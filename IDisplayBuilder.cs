namespace DisplayLibrary.Interfaces;

/// <summary>
/// Fluent configuration surface for a single command's display session.
/// All methods return <c>this</c> for chaining.
/// Call <see cref="StartAsync"/> to seal configuration and begin rendering.
/// </summary>
public interface IDisplayBuilder
{
    /// <summary>
    /// Sets the FigletText banner title.
    /// Accepts Spectre Console markup so commands can colorize as needed.
    /// </summary>
    IDisplayBuilder WithTitle(string spectreMarkup);

    /// <summary>
    /// Declares the maximum number of concurrently active tasks this command will display.
    /// The active-tasks zone always renders exactly this many rows.
    /// </summary>
    IDisplayBuilder WithTasks(int maxConcurrent);

    /// <summary>
    /// Configures the summary counter line shown below the title banner.
    /// Each label is optional; omit or pass <c>null</c> to suppress that counter.
    /// If no labels are supplied the summary line is not rendered at all.
    /// </summary>
    IDisplayBuilder WithSummary(string? completed = null,
                                string? active    = null,
                                string? queued    = null);

    /// <summary>
    /// Provides a live delegate that the summary line calls on every tick to read
    /// the current queued-item count.  Only meaningful when <see cref="WithSummary"/>
    /// was also called with a non-null <c>queued</c> label.
    /// </summary>
    IDisplayBuilder WithQueuedCounter(Func<int> counter);

    /// <summary>
    /// Seals the builder, starts the Live rendering context, and returns the active session.
    /// </summary>
    Task<IDisplaySession> StartAsync();
}
