using System.Globalization;
using ImageResizr.App.Localization;
using ImageResizr.App.ViewModels;
using ImageResizr.Core.Models;
using ImageResizr.Core.Services;

namespace ImageResizr.Tests;

/// <summary>
/// Verifies main-window progress, history, and localization behavior.
/// </summary>
public sealed class MainWindowViewModelTests
{
    private static readonly TimeSpan ConditionWaitTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Verifies that a new resize run clears previous results and resets progress immediately.
    /// </summary>
    [Fact]
    public async Task StartCommandClearsResizeHistoryWhenANewRunStarts()
    {
        ControlledProgressImageResizrService service = new();
        MainWindowViewModel viewModel = CreateReadyViewModel(service);
        viewModel.ResizeHistory.Add(new ResizeHistoryItemViewModel(CreateSuccessfulEntry("old.jpg")));
        viewModel.ProgressValue = 3;
        viewModel.ProgressMaximum = 3;

        Task startTask = viewModel.StartCommand.ExecuteAsync(null);
        await service.WaitUntilStartedAsync();

        Assert.True(viewModel.IsBusy);
        Assert.Empty(viewModel.ResizeHistory);
        Assert.False(viewModel.HasHistory);
        Assert.Equal(0, viewModel.ProgressValue);
        Assert.Equal(0, viewModel.ProgressMaximum);
        Assert.Equal(Strings.Status_Preparing, viewModel.StatusMessage);

        service.Complete();
        await startTask;
    }

    /// <summary>
    /// Verifies that structured file progress updates counts, percentage, and history dimensions.
    /// </summary>
    [Fact]
    public async Task StartCommandAddsStructuredResizeHistoryBeforeServiceCompletes()
    {
        ControlledProgressImageResizrService service = new();
        MainWindowViewModel viewModel = CreateReadyViewModel(service);

        Task startTask = viewModel.StartCommand.ExecuteAsync(null);
        await service.WaitUntilStartedAsync();

        service.ReportProgress(new ResizeProgressUpdate(
            ProcessedCount: 1,
            TotalCount: 3,
            CreateSuccessfulEntry("demo.jpg")));

        await WaitForConditionAsync(
            () => viewModel.ProgressValue == 1
                  && viewModel.ProgressMaximum == 3
                  && viewModel.ResizeHistory.Count == 1,
            "Timed out waiting for progress to update the view model.",
            TestContext.Current.CancellationToken);

        ResizeHistoryItemViewModel historyItem = Assert.Single(viewModel.ResizeHistory);
        Assert.Equal("demo.jpg", historyItem.FileName);
        Assert.Equal(ResizeStatus.Resized, historyItem.Status);
        Assert.Equal("4032 × 3024 → 1920 × 1440", historyItem.DetailText);
        Assert.Equal(33, viewModel.ProgressPercentage);
        Assert.True(viewModel.IsBusy);

        service.Complete();
        await startTask;
    }

    /// <summary>
    /// Verifies that progress delivered after completion cannot modify the completed history.
    /// </summary>
    [Fact]
    public async Task StartCommandIgnoresProgressThatArrivesAfterCompletion()
    {
        LateProgressImageResizrService service = new();
        MainWindowViewModel viewModel = CreateReadyViewModel(service);

        await viewModel.StartCommand.ExecuteAsync(null);
        ResizeHistoryItemViewModel summary = Assert.Single(viewModel.ResizeHistory);
        Assert.Equal("Σ", summary.StatusSymbol);

        await service.ReportLateProgressAsync(TestContext.Current.CancellationToken);

        Assert.Same(summary, Assert.Single(viewModel.ResizeHistory));
        Assert.Equal(3, viewModel.ProgressValue);
        Assert.Equal(3, viewModel.ProgressMaximum);
    }

    /// <summary>
    /// Verifies that the final history entry summarizes every processing outcome.
    /// </summary>
    [Fact]
    public void ResizeHistorySummarySeparatesOverwrittenFiles()
    {
        ResizeImagesResult result = new(
            TotalFiles: 8,
            ResizedFiles: 5,
            SkippedFiles: 2,
            FailedFiles: 1,
            InputBytes: 0,
            OutputBytes: 0,
            OutputFiles: [])
        {
            OverwrittenFiles = 2
        };

        ResizeHistoryItemViewModel summary = ResizeHistoryItemViewModel.CreateSummary(result);

        Assert.Equal("Σ", summary.StatusSymbol);
        Assert.Contains("3", summary.DetailText, StringComparison.Ordinal);
        Assert.Contains("2", summary.DetailText, StringComparison.Ordinal);
        Assert.Contains("1", summary.DetailText, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies English and German resources plus unsupported-culture fallback.
    /// </summary>
    [Fact]
    public void ResourcesResolveGermanAndFallBackToEnglish()
    {
        Assert.Equal("Resize images", Strings.Get("ResizeImages_Button", CultureInfo.GetCultureInfo("en-US")));
        Assert.Equal("Bilder skalieren", Strings.Get("ResizeImages_Button", CultureInfo.GetCultureInfo("de-DE")));
        Assert.Equal("Bilder skalieren", Strings.Get("ResizeImages_Button", CultureInfo.GetCultureInfo("de-CH")));
        Assert.Equal("Resize images", Strings.Get("ResizeImages_Button", CultureInfo.GetCultureInfo("fr-FR")));
    }

    /// <summary>
    /// Verifies separate singular and plural localized progress templates.
    /// </summary>
    [Fact]
    public void ProgressResourcesSupportSingularAndPlural()
    {
        CultureInfo english = CultureInfo.GetCultureInfo("en-US");
        CultureInfo german = CultureInfo.GetCultureInfo("de-DE");

        Assert.Equal("1 / 1 file processed", string.Format(english, Strings.Get("Progress_ProcessedFile", english), 1, 1));
        Assert.Equal("2 / 3 files processed", string.Format(english, Strings.Get("Progress_ProcessedFiles", english), 2, 3));
        Assert.Equal("1 / 1 Datei verarbeitet", string.Format(german, Strings.Get("Progress_ProcessedFile", german), 1, 1));
        Assert.Equal("2 / 3 Dateien verarbeitet", string.Format(german, Strings.Get("Progress_ProcessedFiles", german), 2, 3));
    }

    private static MainWindowViewModel CreateReadyViewModel(IImageResizrService service)
    {
        return new MainWindowViewModel(service)
        {
            InputFolder = "input",
            OutputFolder = "output"
        };
    }

    private static ResizeEntry CreateSuccessfulEntry(string fileName)
    {
        return new ResizeEntry(
            fileName,
            OriginalWidth: 4032,
            OriginalHeight: 3024,
            NewWidth: 1920,
            NewHeight: 1440,
            ResizeStatus.Resized,
            OutputPath: fileName);
    }

    private static async Task WaitForConditionAsync(
        Func<bool> condition,
        string failureMessage,
        CancellationToken cancellationToken)
    {
        if (condition())
        {
            return;
        }

        using CancellationTokenSource timeoutSource = new(ConditionWaitTimeout);
        using CancellationTokenSource linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);

        while (!condition())
        {
            try
            {
                await Task.Delay(10, linkedCancellation.Token);
            }
            catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
            {
                throw new TimeoutException(failureMessage);
            }
        }
    }

    private sealed class ControlledProgressImageResizrService : IImageResizrService
    {
        private readonly TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private IProgress<ResizeProgressUpdate>? capturedProgress;

        public Task WaitUntilStartedAsync() => started.Task;

        public void ReportProgress(ResizeProgressUpdate update) => capturedProgress?.Report(update);

        public void Complete() => completion.TrySetResult();

        public async Task<ResizeImagesResult> ResizeAsync(
            ResizeImagesRequest request,
            IProgress<ResizeProgressUpdate>? progress = null,
            CancellationToken cancellationToken = default)
        {
            capturedProgress = progress;
            started.TrySetResult();
            await completion.Task.WaitAsync(cancellationToken);

            return new ResizeImagesResult(
                TotalFiles: 3,
                ResizedFiles: 3,
                SkippedFiles: 0,
                FailedFiles: 0,
                InputBytes: 2 * 1024 * 1024,
                OutputBytes: 1024 * 1024,
                OutputFiles: []);
        }
    }

    private sealed class LateProgressImageResizrService : IImageResizrService
    {
        private readonly TaskCompletionSource lateProgressHandled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private IProgress<ResizeProgressUpdate>? capturedProgress;

        public Task<ResizeImagesResult> ResizeAsync(
            ResizeImagesRequest request,
            IProgress<ResizeProgressUpdate>? progress = null,
            CancellationToken cancellationToken = default)
        {
            capturedProgress = progress
                ?? throw new InvalidOperationException("The test requires a progress reporter.");

            if (capturedProgress is Progress<ResizeProgressUpdate> concreteProgress)
            {
                concreteProgress.ProgressChanged += HandleProgressChanged;
            }

            return Task.FromResult(new ResizeImagesResult(
                TotalFiles: 3,
                ResizedFiles: 3,
                SkippedFiles: 0,
                FailedFiles: 0,
                InputBytes: 2 * 1024 * 1024,
                OutputBytes: 1024 * 1024,
                OutputFiles: []));
        }

        public async Task ReportLateProgressAsync(CancellationToken cancellationToken)
        {
            capturedProgress?.Report(new ResizeProgressUpdate(
                ProcessedCount: 3,
                TotalCount: 3,
                CreateSuccessfulEntry("late.jpg")));

            await lateProgressHandled.Task.WaitAsync(cancellationToken);
        }

        private void HandleProgressChanged(object? sender, ResizeProgressUpdate update)
        {
            if (sender is Progress<ResizeProgressUpdate> concreteProgress)
            {
                concreteProgress.ProgressChanged -= HandleProgressChanged;
            }

            lateProgressHandled.TrySetResult();
        }
    }
}
