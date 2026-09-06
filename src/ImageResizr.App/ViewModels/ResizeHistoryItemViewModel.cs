using System.Globalization;
using ImageResizr.App.Localization;
using ImageResizr.Core.Models;

namespace ImageResizr.App.ViewModels;

/// <summary>
/// Presents one structured resize result using localized, compact display text.
/// </summary>
public sealed class ResizeHistoryItemViewModel
{
    private ResizeHistoryItemViewModel(
        string fileName,
        ResizeStatus status,
        string statusSymbol,
        string statusText,
        string detailText)
    {
        FileName = fileName;
        Status = status;
        StatusSymbol = statusSymbol;
        StatusText = statusText;
        DetailText = detailText;
    }

    /// <summary>
    /// Initializes a history item from a completed resize entry.
    /// </summary>
    public ResizeHistoryItemViewModel(ResizeEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        FileName = entry.FileName;
        Status = entry.Status;
        StatusSymbol = entry.Status switch
        {
            ResizeStatus.Resized => "✓",
            ResizeStatus.Overwritten => "↻",
            ResizeStatus.Skipped => "–",
            _ => "!"
        };
        StatusText = GetStatusText(entry.Status);
        DetailText = GetDetailText(entry);
    }

    /// <summary>Gets the source file name.</summary>
    public string FileName { get; }

    /// <summary>Gets the processing status.</summary>
    public ResizeStatus Status { get; }

    /// <summary>Gets the non-color status symbol.</summary>
    public string StatusSymbol { get; }

    /// <summary>Gets the localized status label.</summary>
    public string StatusText { get; }

    /// <summary>Gets localized dimensions or outcome details.</summary>
    public string DetailText { get; }

    /// <summary>
    /// Creates the final localized summary entry for a completed batch.
    /// </summary>
    public static ResizeHistoryItemViewModel CreateSummary(ResizeImagesResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        int resizedWithoutOverwrites = Math.Max(0, result.ResizedFiles - result.OverwrittenFiles);

        return new ResizeHistoryItemViewModel(
            Strings.Get("History_SummaryTitle"),
            ResizeStatus.Resized,
            "Σ",
            Strings.Get("History_Status_Completed"),
            Strings.Format(
                "History_SummaryDetails",
                resizedWithoutOverwrites,
                result.OverwrittenFiles,
                result.SkippedFiles,
                result.FailedFiles));
    }

    private static string GetStatusText(ResizeStatus status)
    {
        return Strings.Get(status switch
        {
            ResizeStatus.Resized => "History_Status_Resized",
            ResizeStatus.Overwritten => "History_Status_Overwritten",
            ResizeStatus.Skipped => "History_Status_Skipped",
            _ => "History_Status_Failed"
        });
    }

    private static string GetDetailText(ResizeEntry entry)
    {
        if (entry.Status is ResizeStatus.Resized or ResizeStatus.Overwritten
            && entry.OriginalWidth is int originalWidth
            && entry.OriginalHeight is int originalHeight
            && entry.NewWidth is int newWidth
            && entry.NewHeight is int newHeight)
        {
            return $"{FormatDimension(originalWidth)} × {FormatDimension(originalHeight)} → {FormatDimension(newWidth)} × {FormatDimension(newHeight)}";
        }

        return entry.SkipReason switch
        {
            ResizeSkipReason.OutputFileExists => Strings.Get("History_TargetExists"),
            ResizeSkipReason.AlreadyCorrectSize => Strings.Get("History_AlreadyCorrectSize"),
            _ => Strings.Get("History_ResizeFailed")
        };
    }

    private static string FormatDimension(int value)
    {
        return value.ToString(CultureInfo.CurrentCulture);
    }
}
