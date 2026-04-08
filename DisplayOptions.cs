namespace DisplayLibrary;

/// <summary>
/// Global configuration applied to every display session.
/// All properties have safe defaults so calling AddDisplayLibrary()
/// with no arguments is valid.
/// </summary>
public sealed class DisplayOptions
{
    /// <summary>Number of rolling log rows always visible.</summary>
    public int MaxLogs { get; set; } = 12;

    /// <summary>Number of recently-completed task rows always visible.</summary>
    public int MaxCompleted { get; set; } = 3;

    /// <summary>Width of each task progress bar in characters.</summary>
    public int BarWidth { get; set; } = 25;

    /// <summary>Render loop tick interval in milliseconds.</summary>
    public int TickMs { get; set; } = 50;

    /// <summary>
    /// FigletText font name to load (looks for &lt;name&gt;.flf next to the assembly).
    /// Falls back to FigletFont.Default if the file is not found.
    /// </summary>
    public string FigletFont { get; set; } = "small";
}
