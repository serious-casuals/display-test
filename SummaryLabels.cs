namespace DisplayLibrary.Spectre;

/// <summary>
/// Holds the optional label strings configured via
/// <see cref="Interfaces.IDisplayBuilder.WithSummary"/>.
/// A <c>null</c> label means that counter is not rendered.
/// </summary>
internal sealed record SummaryLabels(
    string? Completed,
    string? Active,
    string? Queued);
