namespace DisplayLibrary;

/// <summary>
/// Status flag carried by a <see cref="ProgressUpdate"/>.
/// </summary>
public enum UpdateStatus
{
    Running,
    Complete,
    Warning,
    Failed
}

/// <summary>
/// Single shared progress update type used across all commands and service layers.
/// Business logic that wants to report progress accepts
/// <see cref="System.IProgress{T}">IProgress&lt;ProgressUpdate&gt;</see>
/// as a method parameter and never references any display type directly.
/// </summary>
/// <param name="TaskId">Matches the taskId passed to <see cref="Interfaces.IDisplaySession.AddTask"/>.</param>
/// <param name="Current">How much work has been done.</param>
/// <param name="Total">Total units of work (replaces the maxValue supplied at task creation).</param>
/// <param name="CurrentItemName">Optional human-readable label for the item being processed.</param>
/// <param name="Status">Running by default; set to Complete to mark the task finished.</param>
/// <summary>
/// There is no TaskId — the <see cref="Interfaces.ITaskHandle"/> that created the
/// <c>IProgress&lt;ProgressUpdate&gt;</c> via <c>AsProgress()</c> owns the binding.
/// Business logic simply reports numbers and the current item name.
/// </summary>
public record ProgressUpdate(
    double       Current,
    double       Total,
    string?      CurrentItemName = null,
    UpdateStatus Status          = UpdateStatus.Running
);
