using DisplayLibrary.Interfaces;
using Microsoft.Extensions.Logging;

namespace DisplayLibrary.Logger;

/// <summary>
/// Non-interactive IDisplayBuilder.  Configuration calls are accepted but ignored.
/// </summary>
internal sealed class LoggerDisplayBuilder : IDisplayBuilder
{
    private readonly ILogger _logger;

    internal LoggerDisplayBuilder(ILogger logger) => _logger = logger;

    public IDisplayBuilder WithTitle(string spectreMarkup)         => this;
    public IDisplayBuilder WithTasks(int maxConcurrent)             => this;
    public IDisplayBuilder WithQueuedCounter(Func<int> counter)     => this;
    public IDisplayBuilder WithSummary(string? completed = null,
                                       string? active    = null,
                                       string? queued    = null)    => this;

    public Task<IDisplaySession> StartAsync() =>
        Task.FromResult<IDisplaySession>(new LoggerDisplaySession(_logger));
}
