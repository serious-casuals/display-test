namespace DisplayLibrary.Interfaces;

/// <summary>
/// The handle a command holds during its run.
/// Implements <see cref="IAsyncDisposable"/> so <c>await using</c> guarantees
/// teardown on both happy and exception paths.
/// </summary>
public interface IDisplaySession : IAsyncDisposable
{
    // ── Task creation ─────────────────────────────────────────────────────────

    /// <summary>
    /// Adds a <b>determinate</b> task — shows a progress bar that advances
    /// as <see cref="ITaskHandle.Update"/> is called.
    /// </summary>
    /// <param name="description">Label shown in the Active Tasks zone.</param>
    /// <param name="maxValue">Units of work that represent 100 % completion.</param>
    ITaskHandle AddTask(string description, double maxValue);

    /// <summary>
    /// Adds an <b>indeterminate</b> task — shows a spinner with no bar or percentage.
    /// <see cref="ITaskHandle.Update"/> is a no-op; call <see cref="ITaskHandle.Complete"/>
    /// when the work finishes.
    /// </summary>
    ITaskHandle AddTask(string description);

    // ── Log ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Appends a line to the shared, application-lifetime Activity Log zone.
    /// Accepts Spectre Console markup for coloring timestamps, filenames, levels, etc.
    /// </summary>
    void AddLog(string spectreMarkup);

    // ── Completion ────────────────────────────────────────────────────────────

    /// <summary>
    /// Renders a final frame, prints a completion summary below the Live context,
    /// and disposes the session. Safe to skip on exception — 
    /// <see cref="IAsyncDisposable.DisposeAsync"/> handles minimal teardown.
    /// </summary>
    Task CompleteAsync();
}
