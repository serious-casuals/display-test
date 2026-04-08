namespace DisplayLibrary.Interfaces;

/// <summary>
/// Registered as a singleton. The single entry point for commands.
/// </summary>
public interface IDisplayFactory
{
    /// <summary>Returns a fluent builder scoped to one command execution.</summary>
    IDisplayBuilder Create();
}
