using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageResizr.Core.Models;
using ImageResizr.Core.Services;

namespace ImageResizr.App.ViewModels;

/// <summary>
/// Provides bindable state and commands for the main image resize window.
/// </summary>
/// <param name="imageResizrService">The service used to resize images.</param>
public sealed partial class MainWindowViewModel(
    IImageResizrService imageResizrService) : ObservableObject
{
    /// <summary>
    /// Gets the available resize preset options.
    /// </summary>
    public IReadOnlyList<ResizePresetOption> Presets => ResizePresetOption.All;

    /// <summary>
    /// Gets the available resize modes.
    /// </summary>
    public IReadOnlyList<ImageResizeMode> ResizeModes { get; } =
    [
        ImageResizeMode.Fit,
        ImageResizeMode.Fill,
        ImageResizeMode.Stretch
    ];

    /// <summary>
    /// Gets or sets a value indicating whether a resize operation is running.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyPropertyChangedFor(nameof(CanEditInputs))]
    [NotifyPropertyChangedFor(nameof(CanEditCustomDimensions))]
    [NotifyPropertyChangedFor(nameof(StartButtonText))]
    public partial bool IsBusy { get; set; }

    /// <summary>
    /// Gets or sets the selected input folder path.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    public partial string InputFolder { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the selected output folder path.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    public partial string OutputFolder { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the selected resize preset.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCustomPreset))]
    [NotifyPropertyChangedFor(nameof(CanEditCustomDimensions))]
    public partial ResizePresetOption SelectedPreset { get; set; } = ResizePresetOption.Small;

    /// <summary>
    /// Gets or sets the target width in pixels.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    public partial int TargetWidth { get; set; } = ResizePresetOption.Small.Width;

    /// <summary>
    /// Gets or sets the target height in pixels.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    public partial int TargetHeight { get; set; } = ResizePresetOption.Small.Height;

    /// <summary>
    /// Gets or sets the selected resize mode.
    /// </summary>
    [ObservableProperty]
    public partial ImageResizeMode SelectedResizeMode { get; set; } = ImageResizeMode.Fit;

    /// <summary>
    /// Gets or sets a value indicating whether images should only be reduced in size.
    /// </summary>
    [ObservableProperty]
    public partial bool ShrinkOnly { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether EXIF orientation should be ignored.
    /// </summary>
    [ObservableProperty]
    public partial bool IgnoreOrientation { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether existing output files should be replaced.
    /// </summary>
    [ObservableProperty]
    public partial bool OverwriteExisting { get; set; }

    /// <summary>
    /// Gets or sets the current validation message.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValidationMessage))]
    public partial string ValidationMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of files processed so far.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressSummary))]
    [NotifyPropertyChangedFor(nameof(ProgressPercentageText))]
    public partial int ProgressValue { get; set; }

    /// <summary>
    /// Gets or sets the total number of files to process.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressSummary))]
    [NotifyPropertyChangedFor(nameof(ProgressPercentageText))]
    public partial int ProgressMaximum { get; set; } = 1;

    /// <summary>
    /// Gets or sets the primary status message.
    /// </summary>
    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Choose folders, pick a size, and start the batch.";

    /// <summary>
    /// Gets or sets the latest detailed activity message.
    /// </summary>
    [ObservableProperty]
    public partial string LatestActivityMessage { get; set; } = "No resize activity yet.";

    /// <summary>
    /// Gets or sets the severity level of the latest activity message.
    /// </summary>
    [ObservableProperty]
    public partial ResizeProgressLevel LatestActivityLevel { get; set; } = ResizeProgressLevel.Info;

    /// <summary>
    /// Gets a value indicating whether input controls can be edited.
    /// </summary>
    public bool CanEditInputs => !IsBusy;

    /// <summary>
    /// Gets a value indicating whether the selected preset uses custom dimensions.
    /// </summary>
    public bool IsCustomPreset => SelectedPreset.IsCustom;

    /// <summary>
    /// Gets a value indicating whether custom width and height controls can be edited.
    /// </summary>
    public bool CanEditCustomDimensions => CanEditInputs && IsCustomPreset;

    /// <summary>
    /// Gets a value indicating whether a validation message should be shown.
    /// </summary>
    public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);

    /// <summary>
    /// Gets the progress summary text.
    /// </summary>
    public string ProgressSummary => $"{ProgressValue.ToString(CultureInfo.CurrentCulture)} / {ProgressMaximum.ToString(CultureInfo.CurrentCulture)} files processed";

    /// <summary>
    /// Gets the progress percentage text.
    /// </summary>
    public string ProgressPercentageText => ProgressMaximum <= 0
        ? "0%"
        : $"{Math.Round((double)ProgressValue / ProgressMaximum * 100, MidpointRounding.AwayFromZero).ToString(CultureInfo.CurrentCulture)}%";

    /// <summary>
    /// Gets the text shown on the start button.
    /// </summary>
    public string StartButtonText => IsBusy ? "Resizing images..." : "Resize images";

    /// <summary>
    /// Gets or sets the delegate used to pick an input folder.
    /// </summary>
    public Func<Task<string?>>? PickInputFolderDelegate { get; set; }

    /// <summary>
    /// Gets or sets the delegate used to pick an output folder.
    /// </summary>
    public Func<Task<string?>>? PickOutputFolderDelegate { get; set; }

    /// <summary>
    /// Updates target dimensions when the selected preset changes.
    /// </summary>
    partial void OnSelectedPresetChanged(ResizePresetOption value)
    {
        if (!value.IsCustom)
        {
            TargetWidth = value.Width;
            TargetHeight = value.Height;
        }
    }

    /// <summary>
    /// Starts the resize operation with the current user selections.
    /// </summary>
    [RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanStart))]
    private async Task StartAsync(CancellationToken cancellationToken)
    {
        ValidationMessage = string.Empty;
        IsBusy = true;
        ProgressValue = 0;
        ProgressMaximum = 1;
        StatusMessage = "Preparing the resize operation.";
        LatestActivityMessage = string.Empty;
        LatestActivityLevel = ResizeProgressLevel.Info;
        int acceptProgressUpdates = 1;

        Progress<ResizeProgressUpdate> progress = new(update =>
        {
            if (Volatile.Read(ref acceptProgressUpdates) == 0)
            {
                return;
            }

            ProgressMaximum = Math.Max(1, update.TotalCount);
            ProgressValue = Math.Min(update.ProcessedCount, ProgressMaximum);
            LatestActivityMessage = update.Message;
            LatestActivityLevel = update.Level;
        });

        try
        {
            ResizeImagesRequest request = new(
                InputFolder,
                OutputFolder,
                TargetWidth,
                TargetHeight,
                SelectedResizeMode,
                ShrinkOnly,
                IgnoreOrientation,
                OverwriteExisting);

            ResizeImagesResult result = await imageResizrService.ResizeAsync(request, progress, cancellationToken);
            Volatile.Write(ref acceptProgressUpdates, 0);

            ProgressMaximum = Math.Max(1, result.TotalFiles);
            ProgressValue = result.TotalFiles;

            if (result.TotalFiles == 0)
            {
                StatusMessage = "The input folder did not contain any supported image files.";
                LatestActivityMessage = FormatCompletionSummary(result);
                LatestActivityLevel = ResizeProgressLevel.Warning;
                return;
            }

            StatusMessage = "Resize operation completed.";
            LatestActivityLevel = result.FailedFiles > 0 ? ResizeProgressLevel.Warning : ResizeProgressLevel.Success;
            LatestActivityMessage = FormatCompletionSummary(result);
        }
        catch (ArgumentException exception)
        {
            Volatile.Write(ref acceptProgressUpdates, 0);
            HandleExpectedFailure("Please review the folders and resize settings, then try again.", exception.Message);
        }
        catch (DirectoryNotFoundException exception)
        {
            Volatile.Write(ref acceptProgressUpdates, 0);
            HandleExpectedFailure("The input folder could not be found.", exception.Message);
        }
        catch (IOException exception)
        {
            Volatile.Write(ref acceptProgressUpdates, 0);
            HandleExpectedFailure("A file-system error interrupted the resize operation.", exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            Volatile.Write(ref acceptProgressUpdates, 0);
            HandleExpectedFailure("The app does not have permission to access one of the selected folders.", exception.Message);
        }
        finally
        {
            Volatile.Write(ref acceptProgressUpdates, 0);
            IsBusy = false;
        }
    }

    /// <summary>
    /// Picks the input folder by using the assigned folder picker delegate.
    /// </summary>
    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task PickInputFolderAsync()
    {
        if (PickInputFolderDelegate is null)
        {
            throw new InvalidOperationException("No folder-pick delegate assigned. The view must assign PickInputFolderDelegate.");
        }

        InputFolder = await PickInputFolderDelegate() ?? string.Empty;
    }

    /// <summary>
    /// Picks the output folder by using the assigned folder picker delegate.
    /// </summary>
    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task PickOutputFolderAsync()
    {
        if (PickOutputFolderDelegate is null)
        {
            throw new InvalidOperationException("No folder-pick delegate assigned. The view must assign PickOutputFolderDelegate.");
        }

        OutputFolder = await PickOutputFolderDelegate() ?? string.Empty;
    }

    /// <summary>
    /// Determines whether the resize operation can be started.
    /// </summary>
    private bool CanStart()
    {
        return !IsBusy
               && !string.IsNullOrWhiteSpace(InputFolder)
               && !string.IsNullOrWhiteSpace(OutputFolder)
               && TargetWidth > 0
               && TargetHeight > 0;
    }

    /// <summary>
    /// Shows a recoverable failure in the view model state.
    /// </summary>
    private void HandleExpectedFailure(
        string status,
        string details)
    {
        StatusMessage = status;
        LatestActivityMessage = details;
        LatestActivityLevel = ResizeProgressLevel.Error;
        ValidationMessage = details;
    }

    /// <summary>
    /// Formats a byte count as megabytes.
    /// </summary>
    private static string FormatBytes(long bytes)
    {
        double megabytes = bytes / (1024.0 * 1024.0);
        return $"{megabytes.ToString("F2", CultureInfo.CurrentCulture)} MB";
    }

    /// <summary>
    /// Formats the final completion summary for a resize operation.
    /// </summary>
    private static string FormatCompletionSummary(ResizeImagesResult result)
    {
        string imageText = result.TotalFiles == 1
            ? "1 image"
            : $"{result.TotalFiles.ToString(CultureInfo.CurrentCulture)} images";

        return $"{imageText} processed. {FormatBytes(result.SavedBytes)} saved.";
    }
}
