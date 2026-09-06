using ImageResizr.Core.Models;

namespace ImageResizr.App.ViewModels;

/// <summary>
/// Provides localized display text for a resize mode.
/// </summary>
/// <param name="Mode">The underlying resize mode.</param>
/// <param name="DisplayName">The localized mode name.</param>
public sealed record ResizeModeItemViewModel(
    ImageResizeMode Mode,
    string DisplayName);
