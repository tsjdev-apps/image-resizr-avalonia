using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageResizr.App.Localization;
using ImageResizr.Core.Models;
using ImageResizr.Core.Services;

namespace ImageResizr.App.ViewModels;

/// <summary>
/// Provides bindable state and commands for the main image resize window.
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly IImageResizrService imageResizrService;

    /// <summary>
    /// Initializes a new main-window view model.
    /// </summary>
    public MainWindowViewModel(IImageResizrService imageResizrService)
    {
        this.imageResizrService = imageResizrService;

        Presets =
        [
            new(ResizePresetOption.Small, Strings.Preset_Small),
            new(ResizePresetOption.Medium, Strings.Preset_Medium),
            new(ResizePresetOption.Large, Strings.Preset_Large),
            new(ResizePresetOption.Phone, Strings.Preset_Phone),
            new(ResizePresetOption.Custom, Strings.Preset_Custom)
        ];
        SelectedPreset = Presets[0];

        ResizeModes =
        [
            new(ImageResizeMode.Fit, Strings.ResizeMode_Fit),
            new(ImageResizeMode.Fill, Strings.ResizeMode_Fill),
            new(ImageResizeMode.Stretch, Strings.ResizeMode_Stretch)
        ];
        SelectedResizeMode = ResizeModes[0];

        ResizeHistory.CollectionChanged += ResizeHistoryOnCollectionChanged;
    }

    /// <summary>Gets the available localized resize presets.</summary>
    public IReadOnlyList<ResizePresetItemViewModel> Presets { get; }

    /// <summary>Gets the available localized resize modes.</summary>
    public IReadOnlyList<ResizeModeItemViewModel> ResizeModes { get; }

    /// <summary>Gets the results produced during the current resize operation.</summary>
    public ObservableCollection<ResizeHistoryItemViewModel> ResizeHistory { get; } = [];

    /// <summary>Gets or sets whether a resize operation is running.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyPropertyChangedFor(nameof(CanEditInputs))]
    [NotifyPropertyChangedFor(nameof(CanEditCustomDimensions))]
    [NotifyPropertyChangedFor(nameof(StartButtonText))]
    public partial bool IsBusy { get; set; }

    /// <summary>Gets or sets the selected source folder path.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    public partial string InputFolder { get; set; } = string.Empty;

    /// <summary>Gets or sets the selected target folder path.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    public partial string OutputFolder { get; set; } = string.Empty;

    /// <summary>Gets or sets the selected resize preset.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCustomPreset))]
    [NotifyPropertyChangedFor(nameof(CanEditCustomDimensions))]
    public partial ResizePresetItemViewModel SelectedPreset { get; set; }

    /// <summary>Gets or sets the target width in pixels.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    public partial int TargetWidth { get; set; } = ResizePresetOption.Small.Width;

    /// <summary>Gets or sets the target height in pixels.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    public partial int TargetHeight { get; set; } = ResizePresetOption.Small.Height;

    /// <summary>Gets or sets the selected resize mode.</summary>
    [ObservableProperty]
    public partial ResizeModeItemViewModel SelectedResizeMode { get; set; }

    /// <summary>Gets or sets whether images should only be reduced in size.</summary>
    [ObservableProperty]
    public partial bool ShrinkOnly { get; set; } = true;

    /// <summary>Gets or sets whether EXIF orientation should be ignored.</summary>
    [ObservableProperty]
    public partial bool IgnoreOrientation { get; set; } = true;

    /// <summary>Gets or sets whether existing output files should be replaced.</summary>
    [ObservableProperty]
    public partial bool OverwriteExisting { get; set; }

    /// <summary>Gets or sets the current localized validation message.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValidationMessage))]
    public partial string ValidationMessage { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of files processed so far.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressSummary))]
    [NotifyPropertyChangedFor(nameof(ProgressPercentage))]
    [NotifyPropertyChangedFor(nameof(ProgressPercentageText))]
    public partial int ProgressValue { get; set; }

    /// <summary>Gets or sets the total number of files in the current batch.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressSummary))]
    [NotifyPropertyChangedFor(nameof(ProgressPercentage))]
    [NotifyPropertyChangedFor(nameof(ProgressPercentageText))]
    [NotifyPropertyChangedFor(nameof(ProgressBarMaximum))]
    public partial int ProgressMaximum { get; set; }

    /// <summary>Gets or sets the primary localized status message.</summary>
    [ObservableProperty]
    public partial string StatusMessage { get; set; } = Strings.Status_Ready;

    /// <summary>Gets whether input controls can be edited.</summary>
    public bool CanEditInputs => !IsBusy;

    /// <summary>Gets whether the selected preset uses custom dimensions.</summary>
    public bool IsCustomPreset => SelectedPreset.IsCustom;

    /// <summary>Gets whether custom dimension controls can be edited.</summary>
    public bool CanEditCustomDimensions => CanEditInputs && IsCustomPreset;

    /// <summary>Gets whether a validation message should be shown.</summary>
    public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);

    /// <summary>Gets whether the current resize history contains entries.</summary>
    public bool HasHistory => ResizeHistory.Count > 0;

    /// <summary>Gets the localized processed-file summary.</summary>
    public string ProgressSummary => Strings.Format(
        ProgressMaximum == 1 ? "Progress_ProcessedFile" : "Progress_ProcessedFiles",
        ProgressValue,
        ProgressMaximum);

    /// <summary>Gets a safe maximum for the progress-bar control.</summary>
    public int ProgressBarMaximum => Math.Max(1, ProgressMaximum);

    /// <summary>Gets the current whole-number progress percentage.</summary>
    public int ProgressPercentage => ProgressMaximum <= 0
        ? 0
        : (int)Math.Round((double)ProgressValue / ProgressMaximum * 100, MidpointRounding.AwayFromZero);

    /// <summary>Gets the localized progress percentage text.</summary>
    public string ProgressPercentageText => $"{ProgressPercentage.ToString(CultureInfo.CurrentCulture)}%";

    /// <summary>Gets the localized text shown on the start button.</summary>
    public string StartButtonText => IsBusy ? Strings.ResizingImages_Button : Strings.ResizeImages_Button;

    /// <summary>Gets or sets the delegate used to pick a source folder.</summary>
    public Func<Task<string?>>? PickInputFolderDelegate { get; set; }

    /// <summary>Gets or sets the delegate used to pick a target folder.</summary>
    public Func<Task<string?>>? PickOutputFolderDelegate { get; set; }

    /// <summary>Updates target dimensions when the selected preset changes.</summary>
    partial void OnSelectedPresetChanged(ResizePresetItemViewModel value)
    {
        if (!value.IsCustom)
        {
            TargetWidth = value.Preset.Width;
            TargetHeight = value.Preset.Height;
        }
    }

    /// <summary>Starts the resize operation with the current selections.</summary>
    [RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanStart))]
    private async Task StartAsync(CancellationToken cancellationToken)
    {
        ValidationMessage = string.Empty;
        IsBusy = true;
        ProgressValue = 0;
        ProgressMaximum = 0;
        StatusMessage = Strings.Status_Preparing;
        ResizeHistory.Clear();
        int acceptProgressUpdates = 1;

        Progress<ResizeProgressUpdate> progress = new(update =>
        {
            if (Volatile.Read(ref acceptProgressUpdates) == 0)
            {
                return;
            }

            ProgressMaximum = Math.Max(0, update.TotalCount);
            ProgressValue = Math.Clamp(update.ProcessedCount, 0, ProgressMaximum);

            if (update.TotalCount > 0)
            {
                StatusMessage = Strings.Status_Running;
            }

            if (update.Entry is not null)
            {
                ResizeHistory.Add(new ResizeHistoryItemViewModel(update.Entry));
            }
        });

        try
        {
            ResizeImagesRequest request = new(
                InputFolder,
                OutputFolder,
                TargetWidth,
                TargetHeight,
                SelectedResizeMode.Mode,
                ShrinkOnly,
                IgnoreOrientation,
                OverwriteExisting);

            ResizeImagesResult result = await imageResizrService.ResizeAsync(request, progress, cancellationToken);
            Volatile.Write(ref acceptProgressUpdates, 0);

            ProgressMaximum = result.TotalFiles;
            ProgressValue = result.TotalFiles;

            if (result.TotalFiles > 0)
            {
                ResizeHistory.Add(ResizeHistoryItemViewModel.CreateSummary(result));
            }

            StatusMessage = result.TotalFiles == 0
                ? Strings.Status_NoSupportedFiles
                : $"{Strings.Status_Completed} {FormatCompletionSummary(result)}";
        }
        catch (ArgumentException exception)
        {
            Volatile.Write(ref acceptProgressUpdates, 0);
            HandleExpectedFailure(Strings.Status_ReviewSettings, GetValidationMessage(exception));
        }
        catch (DirectoryNotFoundException)
        {
            Volatile.Write(ref acceptProgressUpdates, 0);
            HandleExpectedFailure(Strings.Status_InputFolderMissing, Strings.Status_InputFolderMissing);
        }
        catch (IOException)
        {
            Volatile.Write(ref acceptProgressUpdates, 0);
            HandleExpectedFailure(Strings.Status_FileSystemError, Strings.Status_FileSystemError);
        }
        catch (UnauthorizedAccessException)
        {
            Volatile.Write(ref acceptProgressUpdates, 0);
            HandleExpectedFailure(Strings.Status_AccessDenied, Strings.Status_AccessDenied);
        }
        finally
        {
            Volatile.Write(ref acceptProgressUpdates, 0);
            IsBusy = false;
        }
    }

    /// <summary>Picks the source folder using the view-provided delegate.</summary>
    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task PickInputFolderAsync()
    {
        if (PickInputFolderDelegate is null)
        {
            throw new InvalidOperationException("No folder-pick delegate is assigned.");
        }

        InputFolder = await PickInputFolderDelegate() ?? string.Empty;
    }

    /// <summary>Picks the target folder using the view-provided delegate.</summary>
    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task PickOutputFolderAsync()
    {
        if (PickOutputFolderDelegate is null)
        {
            throw new InvalidOperationException("No folder-pick delegate is assigned.");
        }

        OutputFolder = await PickOutputFolderDelegate() ?? string.Empty;
    }

    private bool CanStart()
    {
        return !IsBusy
               && !string.IsNullOrWhiteSpace(InputFolder)
               && !string.IsNullOrWhiteSpace(OutputFolder)
               && TargetWidth > 0
               && TargetHeight > 0;
    }

    private void ResizeHistoryOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(HasHistory));
    }

    private void HandleExpectedFailure(string status, string details)
    {
        StatusMessage = status;
        ValidationMessage = details;
    }

    private static string GetValidationMessage(ArgumentException exception)
    {
        return exception.ParamName switch
        {
            nameof(ResizeImagesRequest.InputFolder) => Strings.Get("Validation_InputFolderRequired"),
            nameof(ResizeImagesRequest.OutputFolder) => Strings.Get("Validation_OutputFolderRequired"),
            nameof(ResizeImagesRequest.TargetWidth) or nameof(ResizeImagesRequest.TargetHeight) =>
                Strings.Get("Validation_DimensionsPositive"),
            _ => Strings.Status_ReviewSettings
        };
    }

    private static string FormatBytes(long bytes)
    {
        double megabytes = bytes / (1024.0 * 1024.0);
        return $"{megabytes.ToString("F2", CultureInfo.CurrentCulture)} MB";
    }

    private static string FormatCompletionSummary(ResizeImagesResult result)
    {
        return Strings.Format(
            result.TotalFiles == 1 ? "Completion_One" : "Completion_Many",
            result.TotalFiles,
            FormatBytes(result.SavedBytes));
    }
}
