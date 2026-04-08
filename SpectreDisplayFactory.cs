using DisplayLibrary.Interfaces;
using DisplayLibrary.Internal;

namespace DisplayLibrary.Spectre;

/// <summary>
/// Singleton factory used when the process is running interactively.
/// Owns the <see cref="SharedLog"/> that persists across all sessions.
/// </summary>
internal sealed class SpectreDisplayFactory : IDisplayFactory
{
    private readonly SharedLog      _sharedLog;
    private readonly DisplayOptions _opts;

    public SpectreDisplayFactory(DisplayOptions opts)
    {
        _opts      = opts;
        _sharedLog = new SharedLog(opts.MaxLogs);
    }

    public IDisplayBuilder Create() =>
        new SpectreDisplayBuilder(_sharedLog, _opts);
}
