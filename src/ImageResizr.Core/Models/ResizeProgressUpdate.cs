namespace ImageResizr.Core.Models;

/// <summary>
/// Describes progress reported while a batch image resize operation is running.
/// </summary>
/// <param name="ProcessedCount">The number of source files processed so far.</param>
/// <param name="TotalCount">The total number of source files to process.</param>
/// <param name="Entry">The completed image result, or <see langword="null" /> for batch-level updates.</param>
public sealed record ResizeProgressUpdate(
    int ProcessedCount,
    int TotalCount,
    ResizeEntry? Entry = null);
