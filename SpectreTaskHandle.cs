using DisplayLibrary.Interfaces;
using DisplayLibrary.Internal;

namespace DisplayLibrary.Spectre;

/// <summary>
/// Concrete <see cref="ITaskHandle"/> for the interactive Spectre rendering path.
/// Mutates its <see cref="TaskState"/> directly (the ConcurrentDictionary value is a
/// reference type, so writes are immediately visible to the render loop snapshot).
/// Delegates <see cref="Complete"/> back to the owning session so the task can be
/// atomically moved from the active dictionary to the completed list.
/// </summary>
internal sealed class SpectreTaskHandle : ITaskHandle
{
    private readonly TaskState             _state;
    private readonly SpectreDisplaySession _session;

    internal SpectreTaskHandle(TaskState state, SpectreDisplaySession session)
    {
        _state   = state;
        _session = session;
    }

    /// <inheritdoc/>
    public void Update(double current, string? description = null)
    {
        if (_state.IsIndeterminate) return; // spinner-only task — no-op

        _state.Value = current;
        if (description is not null)
            _state.Description = description;

        // Reset to Running if the task was in a warning/failed state
        _state.DisplayStatus = TaskDisplayStatus.Running;
    }

    /// <inheritdoc/>
    public void Complete(string? finalDescription = null) =>
        _session.CompleteTask(_state, finalDescription);

    /// <inheritdoc/>
    public IProgress<ProgressUpdate> AsProgress() =>
        new Progress<ProgressUpdate>(update =>
        {
            // Keep maxValue in sync with what the business layer reports
            if (!_state.IsIndeterminate && update.Total > 0)
                _state.MaxValue = update.Total;

            switch (update.Status)
            {
                case UpdateStatus.Complete:
                    Complete(update.CurrentItemName);
                    break;

                case UpdateStatus.Warning:
                    _state.DisplayStatus = TaskDisplayStatus.Warning;
                    if (!_state.IsIndeterminate) _state.Value = update.Current;
                    if (update.CurrentItemName is not null)
                        _state.Description = update.CurrentItemName;
                    break;

                case UpdateStatus.Failed:
                    _state.DisplayStatus = TaskDisplayStatus.Failed;
                    if (!_state.IsIndeterminate) _state.Value = update.Current;
                    if (update.CurrentItemName is not null)
                        _state.Description = update.CurrentItemName;
                    break;

                default: // Running
                    Update(update.Current, update.CurrentItemName);
                    break;
            }
        });
}
