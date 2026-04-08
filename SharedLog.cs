namespace DisplayLibrary.Internal;

/// <summary>
/// Application-lifetime rolling log buffer.
/// The only display zone that is not command-scoped: log entries persist across
/// the full process lifetime and are visible to every session.
/// Thread-safe for concurrent appends from any service layer.
/// </summary>
internal sealed class SharedLog
{
    private readonly Queue<string> _entries = new();
    private readonly object        _lock    = new();
    private readonly int           _maxLogs;

    public SharedLog(int maxLogs) => _maxLogs = maxLogs;

    /// <summary>
    /// Appends a Spectre markup string.  Oldest entry is dropped when the queue is full.
    /// </summary>
    public void Add(string spectreMarkup)
    {
        lock (_lock)
        {
            _entries.Enqueue(spectreMarkup);
            while (_entries.Count > _maxLogs)
                _entries.Dequeue();
        }
    }

    /// <summary>Returns a point-in-time copy safe to read outside the lock.</summary>
    public IReadOnlyList<string> Snapshot()
    {
        lock (_lock)
        {
            return [.. _entries];
        }
    }

    /// <summary>Total entries ever added (for the summary counter).</summary>
    public int TotalCount { get; private set; }
}
