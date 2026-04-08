using DisplayLibrary.Interfaces;
using Microsoft.Extensions.Logging;

namespace DisplayLibrary.Logger;

/// <summary>
/// Non-interactive IDisplayFactory registered when environment detection fires.
/// Each call to Create() returns a builder backed by a category-named ILogger.
/// </summary>
internal sealed class LoggerDisplayFactory : IDisplayFactory
{
    private readonly ILoggerFactory _loggerFactory;

    public LoggerDisplayFactory(ILoggerFactory loggerFactory) =>
        _loggerFactory = loggerFactory;

    public IDisplayBuilder Create()
    {
        var logger = _loggerFactory.CreateLogger("DisplayLibrary");
        return new LoggerDisplayBuilder(logger);
    }
}
