namespace DisplayLibrary.Internal;

/// <summary>Mutable state for a single active task, written by workers and read by the render loop.</summary>
internal sealed class TaskState
{
    public string            TaskId        { get; }
    public string            Description   { get; set; }
    public double            Value         { get; set; }
    public double            MaxValue      { get; set; }
    public DateTime          StartedAt     { get; }
    public TaskDisplayStatus DisplayStatus { get; set; } = TaskDisplayStatus.Running;

    /// <summary>
    /// True for tasks added via <c>AddTask(description)</c> — no bar, no percentage,
    /// no ETA.  The spinner animates regardless.
    /// </summary>
    public bool IsIndeterminate { get; init; }

    public TaskState(string taskId, string description, double maxValue)
    {
        TaskId      = taskId;
        Description = description;
        MaxValue    = maxValue;
        StartedAt   = DateTime.Now;
    }

    /// <summary>0 – 100.</summary>
    public double Percentage => MaxValue > 0 ? Math.Clamp(Value / MaxValue * 100.0, 0, 100) : 0;

    /// <summary>
    /// Returns <c>null</c> until at least 0.5 % progress to avoid wild early estimates.
    /// </summary>
    public TimeSpan? EstimatedRemaining
    {
        get
        {
            var pct = Percentage;
            if (pct < 0.5) return null;

            var elapsed = DateTime.Now - StartedAt;
            var totalSeconds = elapsed.TotalSeconds / (pct / 100.0);
            var remaining = totalSeconds - elapsed.TotalSeconds;
            return TimeSpan.FromSeconds(Math.Max(0, remaining));
        }
    }
}
