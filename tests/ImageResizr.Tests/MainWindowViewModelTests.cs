using ImageResizr.App.ViewModels;
using ImageResizr.Core.Models;
using ImageResizr.Core.Services;

namespace ImageResizr.Tests;

/// <summary>
/// Verifies the main window view model behavior.
/// </summary>
public sealed class MainWindowViewModelTests
{
    /// <summary>
    /// Verifies that a new resize run clears the previous completion summary immediately.
    /// </summary>
    [Fact]
    public async Task StartCommandClearsLatestActivityWhenANewRunStarts()
    {
        ControlledProgressImageResizrService service = new();
        MainWindowViewModel viewModel = new(service)
        {
            InputFolder = "input",
            OutputFolder = "output",
            LatestActivityMessage = "3 images processed. 1.00 MB saved.",
            LatestActivityLevel = ResizeProgressLevel.Success
        };

        Task startTask = viewModel.StartCommand.ExecuteAsync(null);

        await service.WaitUntilStartedAsync();

        Assert.True(viewModel.IsBusy);
        Assert.Equal(string.Empty, viewModel.LatestActivityMessage);
        Assert.Equal(ResizeProgressLevel.Info, viewModel.LatestActivityLevel);
        Assert.Equal("Preparing the resize operation.", viewModel.StatusMessage);
        Assert.Equal(0, viewModel.ProgressValue);
        Assert.Equal(1, viewModel.ProgressMaximum);

        service.Complete();
        await startTask;
    }

    /// <summary>
    /// Verifies that progress updates reach the view model before the resize operation completes.
    /// </summary>
    [Fact]
    public async Task StartCommandAppliesProgressUpdatesBeforeServiceCompletes()
    {
        ControlledProgressImageResizrService service = new();
        MainWindowViewModel viewModel = new(service)
        {
            InputFolder = "input",
            OutputFolder = "output"
        };

        Task startTask = viewModel.StartCommand.ExecuteAsync(null);

        await service.WaitUntilStartedAsync();

        const string progressMessage = "Resized 'demo.jpg' to 800 x 600 pixels.";
        service.ReportProgress(new ResizeProgressUpdate(
            ProcessedCount: 1,
            TotalCount: 3,
            Message: progressMessage,
            Level: ResizeProgressLevel.Success));

        await WaitForConditionAsync(
            () => viewModel.ProgressValue == 1
                  && viewModel.ProgressMaximum == 3
                  && viewModel.LatestActivityMessage == progressMessage,
            TestContext.Current.CancellationToken);

        Assert.True(viewModel.IsBusy);
        Assert.False(startTask.IsCompleted);

        service.Complete();
        await startTask;
    }

    /// <summary>
    /// Verifies that late progress reports cannot replace the completion summary.
    /// </summary>
    [Fact]
    public async Task StartCommandKeepsCompletionSummaryWhenProgressArrivesAfterServiceCompletes()
    {
        LateProgressImageResizrService service = new();
        MainWindowViewModel viewModel = new(service)
        {
            InputFolder = "input",
            OutputFolder = "output"
        };

        await viewModel.StartCommand.ExecuteAsync(null);

        const string expectedSummary = "3 images processed. 1.00 MB saved.";
        Assert.Equal(expectedSummary, viewModel.LatestActivityMessage);

        service.ReportLateProgress();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        Assert.Equal(expectedSummary, viewModel.LatestActivityMessage);
    }

    /// <summary>
    /// Waits until a test condition becomes true.
    /// </summary>
    private static async Task WaitForConditionAsync(
        Func<bool> condition,
        CancellationToken cancellationToken)
    {
        while (!condition())
        {
            await Task.Delay(10, cancellationToken);
        }
    }

    /// <summary>
    /// Provides a test ImageResizr service that can be held open while progress is reported.
    /// </summary>
    private sealed class ControlledProgressImageResizrService : IImageResizrService
    {
        private readonly TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly ResizeImagesResult completionResult = new(
            TotalFiles: 3,
            ResizedFiles: 3,
            SkippedFiles: 0,
            FailedFiles: 0,
            InputBytes: 2 * 1024 * 1024,
            OutputBytes: 1024 * 1024,
            OutputFiles: []);
        private IProgress<ResizeProgressUpdate>? capturedProgress;

        /// <summary>
        /// Waits until the resize operation has started.
        /// </summary>
        public Task WaitUntilStartedAsync()
        {
            return started.Task;
        }

        /// <summary>
        /// Reports progress while the resize operation is still running.
        /// </summary>
        public void ReportProgress(ResizeProgressUpdate update)
        {
            capturedProgress?.Report(update);
        }

        /// <summary>
        /// Completes the resize operation.
        /// </summary>
        public void Complete()
        {
            completion.TrySetResult();
        }

        /// <summary>
        /// Captures the progress reporter and waits until the test completes the operation.
        /// </summary>
        public async Task<ResizeImagesResult> ResizeAsync(
            ResizeImagesRequest request,
            IProgress<ResizeProgressUpdate>? progress = null,
            CancellationToken cancellationToken = default)
        {
            capturedProgress = progress;
            started.TrySetResult();

            await completion.Task.WaitAsync(cancellationToken);
            return completionResult;
        }
    }

    /// <summary>
    /// Provides a test ImageResizr service that reports progress after completing.
    /// </summary>
    private sealed class LateProgressImageResizrService : IImageResizrService
    {
        private IProgress<ResizeProgressUpdate>? capturedProgress;

        /// <summary>
        /// Captures the progress reporter and returns a completed resize result.
        /// </summary>
        /// <returns>The completed resize result.</returns>
        public Task<ResizeImagesResult> ResizeAsync(
            ResizeImagesRequest request,
            IProgress<ResizeProgressUpdate>? progress = null,
            CancellationToken cancellationToken = default)
        {
            capturedProgress = progress;

            ResizeImagesResult result = new(
                TotalFiles: 3,
                ResizedFiles: 3,
                SkippedFiles: 0,
                FailedFiles: 0,
                InputBytes: 2 * 1024 * 1024,
                OutputBytes: 1024 * 1024,
                OutputFiles: []);

            return Task.FromResult(result);
        }

        /// <summary>
        /// Reports progress after the resize operation has already completed.
        /// </summary>
        public void ReportLateProgress()
        {
            capturedProgress?.Report(new ResizeProgressUpdate(
                ProcessedCount: 3,
                TotalCount: 3,
                Message: "Resized 'P1012828.JPG' to 1364 x 768 pixels.",
                Level: ResizeProgressLevel.Success));
        }
    }
}
