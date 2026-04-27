namespace ImageResizr.Core.Models;

/// <summary>
/// Describes progress reported while a batch image resize operation is running.
/// </summary>
/// <param name="ProcessedCount">The number of source files processed so far.</param>
/// <param name="TotalCount">The total number of source files to process.</param>
/// <param name="Message">The progress message to show to the user.</param>
/// <param name="Level">The severity level of the progress update.</param>
public sealed record ResizeProgressUpdate(
    int ProcessedCount,
    int TotalCount,
    string Message,
    ResizeProgressLevel Level = ResizeProgressLevel.Info);
