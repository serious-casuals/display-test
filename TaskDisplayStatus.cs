namespace DisplayLibrary.Internal;

/// <summary>
/// Display-layer status for an active task row.
/// Drives icon and bar color selection in the active-tasks zone.
/// </summary>
internal enum TaskDisplayStatus
{
    /// <summary>Normal spinning progress — steelblue1 → green bar.</summary>
    Running,

    /// <summary>
    /// Non-fatal issue; row shows a yellow ⚠ icon and an amber bar.
    /// Task remains active until explicitly completed or failed.
    /// </summary>
    Warning,

    /// <summary>
    /// Terminal failure; row shows a red ✗ icon and a red bar.
    /// Task stays visible until <see cref="Interfaces.IDisplaySession.CompleteTask"/> is called.
    /// </summary>
    Failed
}
