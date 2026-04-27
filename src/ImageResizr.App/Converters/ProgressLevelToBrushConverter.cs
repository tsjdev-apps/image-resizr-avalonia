using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using ImageResizr.Core.Models;

namespace ImageResizr.App.Converters;

/// <summary>
/// Converts resize progress levels into brushes for status text.
/// </summary>
public sealed class ProgressLevelToBrushConverter : IValueConverter
{
    private static readonly IBrush InfoBrush = new SolidColorBrush(Color.Parse("#475569"));
    private static readonly IBrush SuccessBrush = new SolidColorBrush(Color.Parse("#15803D"));
    private static readonly IBrush WarningBrush = new SolidColorBrush(Color.Parse("#B45309"));
    private static readonly IBrush ErrorBrush = new SolidColorBrush(Color.Parse("#B91C1C"));

    /// <summary>
    /// Converts a resize progress level into the matching status brush.
    /// </summary>
    /// <returns>The brush used to render the progress level.</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            ResizeProgressLevel.Success => SuccessBrush,
            ResizeProgressLevel.Warning => WarningBrush,
            ResizeProgressLevel.Error => ErrorBrush,
            _ => InfoBrush
        };
    }

    /// <summary>
    /// Rejects conversion from a brush back to a resize progress level.
    /// </summary>
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("Converting a brush back to a resize progress level is not supported.");
    }
}
