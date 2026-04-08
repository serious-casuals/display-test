namespace DisplayLibrary.Interfaces;

/// <summary>
/// Returned by <see cref="IDisplaySession.AddTask(string,double)"/> and
/// <see cref="IDisplaySession.AddTask(string)"/>.
/// The command holds this reference and drives the task through its lifecycle.
/// There are no string IDs to pass around — the handle is the identity.
/// </summary>
public interface ITaskHandle
{
    /// <summary>
    /// Advances the progress bar to <paramref name="current"/> and optionally
    /// relabels the task.
    /// <para>
    /// <b>No-op on indeterminate tasks</b> — the spinner keeps animating and no error
    /// is raised. This allows generic wrappers to call <c>Update</c> without knowing
    /// the task kind.
    /// </para>
    /// </summary>
    void Update(double current, string? description = null);

    /// <summary>
    /// Transitions the task to completed and moves it to the Recently Completed panel.
    /// </summary>
    void Complete(string? finalDescription = null);

    /// <summary>
    /// Returns an <see cref="IProgress{T}"/> bound to this handle for deep-stack
    /// reporting.  Business logic accepts <c>IProgress&lt;ProgressUpdate&gt;</c> as a
    /// method parameter and never references any display type directly.
    /// </summary>
    IProgress<ProgressUpdate> AsProgress();
}
