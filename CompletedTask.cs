namespace DisplayLibrary.Internal;

/// <summary>Immutable snapshot of a finished task stored in the recently-completed ring.</summary>
internal sealed class CompletedTask
{
    public string   Description { get; }
    public TimeSpan Elapsed     { get; }

    public CompletedTask(string description, TimeSpan elapsed)
    {
        Description = description;
        Elapsed     = elapsed;
    }
}
