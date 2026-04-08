using DisplayLibrary.Interfaces;
using DisplayLibrary.Internal;
using Microsoft.Extensions.Logging;

namespace DisplayLibrary.Logger;

/// <summary>
/// Non-interactive IDisplaySession. Every method maps to an ILogger call; no console output occurs.
/// </summary>
internal sealed class LoggerDisplaySession : IDisplaySession
{
    private readonly ILogger _logger;

    internal LoggerDisplaySession(ILogger logger) => _logger = logger;

    public ITaskHandle AddTask(string description, double maxValue)
    {
        _logger.LogInformation("Task started: {Description} (max={MaxValue})", description, maxValue);
        return new LoggerTaskHandle(_logger, description, maxValue);
    }

    public ITaskHandle AddTask(string description)
    {
        _logger.LogInformation("Task started (indeterminate): {Description}", description);
        return new LoggerTaskHandle(_logger, description, maxValue: 0);
    }

    public void AddLog(string spectreMarkup) =>
        _logger.LogInformation("{Message}", MarkupStripper.Strip(spectreMarkup));

    public Task CompleteAsync()
    {
        _logger.LogInformation("Session complete");
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
