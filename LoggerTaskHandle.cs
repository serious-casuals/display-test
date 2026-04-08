using DisplayLibrary.Interfaces;
using DisplayLibrary.Internal;
using Microsoft.Extensions.Logging;

namespace DisplayLibrary.Logger;

/// <summary>
/// Non-interactive <see cref="ITaskHandle"/>.
/// Every call maps to an <see cref="ILogger"/> call; no console output occurs.
/// </summary>
internal sealed class LoggerTaskHandle : ITaskHandle
{
    private readonly ILogger _logger;
    private          string  _description;
    private          double  _maxValue;

    internal LoggerTaskHandle(ILogger logger, string description, double maxValue)
    {
        _logger      = logger;
        _description = description;
        _maxValue    = maxValue;
    }

    public void Update(double current, string? description = null)
    {
        if (description is not null)
            _description = description;

        _logger.LogDebug("Task progress: {Description} {Current}/{Total}",
            _description, current, _maxValue);
    }

    public void Complete(string? finalDescription = null) =>
        _logger.LogInformation("Task complete: {Description}",
            finalDescription ?? _description);

    public IProgress<ProgressUpdate> AsProgress() =>
        new Progress<ProgressUpdate>(update =>
        {
            if (update.Total > 0) _maxValue = update.Total;

            switch (update.Status)
            {
                case UpdateStatus.Complete:
                    Complete(update.CurrentItemName);
                    break;

                case UpdateStatus.Warning:
                    _logger.LogWarning("Task warning: {Description} {Current}/{Total} ({Item})",
                        _description, update.Current, update.Total, update.CurrentItemName);
                    break;

                case UpdateStatus.Failed:
                    _logger.LogError("Task failed: {Description} {Current}/{Total} ({Item})",
                        _description, update.Current, update.Total, update.CurrentItemName);
                    break;

                default:
                    Update(update.Current, update.CurrentItemName);
                    break;
            }
        });
}
