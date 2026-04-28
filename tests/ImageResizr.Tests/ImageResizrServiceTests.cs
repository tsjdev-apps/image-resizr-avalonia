using ImageResizr.Core.Models;
using ImageResizr.Core.Services;
using SkiaSharp;

namespace ImageResizr.Tests;

/// <summary>
/// Verifies the image resize service behavior.
/// </summary>
public sealed class ImageResizrServiceTests
{
    /// <summary>
    /// Verifies that fit mode preserves file names and extensions while resizing supported files.
    /// </summary>
    [Fact]
    public async Task ResizeAsyncPreservesFileNamesAndExtensionsWhenUsingFitMode()
    {
        using TestWorkspace workspace = new();
        await workspace.CreateInputImageAsync("camera.PNG", 120, 80);
        await workspace.CreateInputImageAsync("portrait.jpg", 80, 120);
        workspace.CreateInputFile("legacy-scan.tif", "not a supported image");
        workspace.CreateInputFile("legacy-photo.tiff", "not a supported image");
        workspace.CreateInputFile("notes.txt", "not an image");
        ListProgress progress = new();
        ImageResizrService service = new();

        ResizeImagesResult result = await service.ResizeAsync(
            new ResizeImagesRequest(
                workspace.InputFolder,
                workspace.OutputFolder,
                TargetWidth: 60,
                TargetHeight: 60,
                ImageResizeMode.Fit,
                ShrinkOnly: true,
                IgnoreOrientation: true,
                OverwriteExisting: false),
            progress,
            TestContext.Current.CancellationToken);

        Assert.Equal(2, result.TotalFiles);
        Assert.True(
            result.FailedFiles == 0,
            string.Join(" | ", progress.Updates.Select(update => $"{update.Level}: {update.Message}")));
        Assert.Equal(0, result.SkippedFiles);
        Assert.Equal(2, result.ResizedFiles);
        await AssertImageDimensionsAsync(Path.Combine(workspace.OutputFolder, "camera.PNG"), 60, 40);
        await AssertImageDimensionsAsync(Path.Combine(workspace.OutputFolder, "portrait.jpg"), 40, 60);
        Assert.False(File.Exists(Path.Combine(workspace.OutputFolder, "notes.txt")));
        Assert.False(File.Exists(Path.Combine(workspace.OutputFolder, "legacy-scan.tif")));
        Assert.False(File.Exists(Path.Combine(workspace.OutputFolder, "legacy-photo.tiff")));
        Assert.Contains(progress.Updates, update => update.Message.Contains("camera.PNG", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that fill mode crops images to the exact requested dimensions.
    /// </summary>
    [Fact]
    public async Task ResizeAsyncCropsToExactTargetWhenUsingFillMode()
    {
        using TestWorkspace workspace = new();
        await workspace.CreateInputImageAsync("wide.png", 120, 80);
        ImageResizrService service = new();

        ResizeImagesResult result = await service.ResizeAsync(
            new ResizeImagesRequest(
                workspace.InputFolder,
                workspace.OutputFolder,
                TargetWidth: 50,
                TargetHeight: 50,
                ImageResizeMode.Fill,
                ShrinkOnly: true,
                IgnoreOrientation: true,
                OverwriteExisting: false),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(1, result.ResizedFiles);
        await AssertImageDimensionsAsync(Path.Combine(workspace.OutputFolder, "wide.png"), 50, 50);
    }

    /// <summary>
    /// Verifies that existing output files are skipped when overwrite is disabled.
    /// </summary>
    [Fact]
    public async Task ResizeAsyncSkipsExistingOutputWhenOverwriteIsDisabled()
    {
        using TestWorkspace workspace = new();
        await workspace.CreateInputImageAsync("camera.png", 120, 80);
        string existingOutputPath = workspace.CreateOutputFile("camera.png", "existing content");
        ImageResizrService service = new();

        ResizeImagesResult result = await service.ResizeAsync(
            new ResizeImagesRequest(
                workspace.InputFolder,
                workspace.OutputFolder,
                TargetWidth: 60,
                TargetHeight: 60,
                ImageResizeMode.Fit,
                ShrinkOnly: true,
                IgnoreOrientation: true,
                OverwriteExisting: false),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(1, result.TotalFiles);
        Assert.Equal(0, result.ResizedFiles);
        Assert.Equal(1, result.SkippedFiles);
        Assert.Equal("existing content", await File.ReadAllTextAsync(existingOutputPath, TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Verifies that existing output files are replaced when overwrite is enabled.
    /// </summary>
    [Fact]
    public async Task ResizeAsyncOverwritesExistingOutputWhenOverwriteIsEnabled()
    {
        using TestWorkspace workspace = new();
        await workspace.CreateInputImageAsync("camera.png", 120, 80);
        workspace.CreateOutputFile("camera.png", "existing content");
        ImageResizrService service = new();

        ResizeImagesResult result = await service.ResizeAsync(
            new ResizeImagesRequest(
                workspace.InputFolder,
                workspace.OutputFolder,
                TargetWidth: 60,
                TargetHeight: 60,
                ImageResizeMode.Fit,
                ShrinkOnly: true,
                IgnoreOrientation: true,
                OverwriteExisting: true),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(1, result.ResizedFiles);
        await AssertImageDimensionsAsync(Path.Combine(workspace.OutputFolder, "camera.png"), 60, 40);
    }

    /// <summary>
    /// Verifies that smaller images are copied unchanged when shrink-only mode is enabled.
    /// </summary>
    [Fact]
    public async Task ResizeAsyncCopiesSmallerImagesUnchangedWhenShrinkOnlyIsEnabled()
    {
        using TestWorkspace workspace = new();
        await workspace.CreateInputImageAsync("small.png", 40, 30);
        ImageResizrService service = new();

        ResizeImagesResult result = await service.ResizeAsync(
            new ResizeImagesRequest(
                workspace.InputFolder,
                workspace.OutputFolder,
                TargetWidth: 100,
                TargetHeight: 100,
                ImageResizeMode.Fit,
                ShrinkOnly: true,
                IgnoreOrientation: true,
                OverwriteExisting: false),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(0, result.ResizedFiles);
        Assert.Equal(1, result.SkippedFiles);
        await AssertImageDimensionsAsync(Path.Combine(workspace.OutputFolder, "small.png"), 40, 30);
    }

    /// <summary>
    /// Verifies that input files can be safely overwritten by using temporary files.
    /// </summary>
    [Fact]
    public async Task ResizeAsyncCanOverwriteImagesInTheInputFolderWithTemporaryFiles()
    {
        using TestWorkspace workspace = new();
        string inputPath = await workspace.CreateInputImageAsync("camera.png", 120, 80);
        ImageResizrService service = new();

        ResizeImagesResult result = await service.ResizeAsync(
            new ResizeImagesRequest(
                workspace.InputFolder,
                workspace.InputFolder,
                TargetWidth: 60,
                TargetHeight: 60,
                ImageResizeMode.Fit,
                ShrinkOnly: true,
                IgnoreOrientation: true,
                OverwriteExisting: true),
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(1, result.ResizedFiles);
        await AssertImageDimensionsAsync(inputPath, 60, 40);
    }

    /// <summary>
    /// Verifies that a corrupt supported file is counted as failed without aborting the batch.
    /// </summary>
    [Fact]
    public async Task ResizeAsyncCountsCorruptSupportedFilesAsFailuresAndContinuesBatch()
    {
        using TestWorkspace workspace = new();
        workspace.CreateInputFile("broken.jpg", "this is not a real image");
        await workspace.CreateInputImageAsync("camera.png", 120, 80);
        ListProgress progress = new();
        ImageResizrService service = new();

        ResizeImagesResult result = await service.ResizeAsync(
            new ResizeImagesRequest(
                workspace.InputFolder,
                workspace.OutputFolder,
                TargetWidth: 60,
                TargetHeight: 60,
                ImageResizeMode.Fit,
                ShrinkOnly: true,
                IgnoreOrientation: true,
                OverwriteExisting: false),
            progress,
            TestContext.Current.CancellationToken);

        Assert.Equal(2, result.TotalFiles);
        Assert.Equal(1, result.ResizedFiles);
        Assert.Equal(1, result.FailedFiles);
        await AssertImageDimensionsAsync(Path.Combine(workspace.OutputFolder, "camera.png"), 60, 40);
        Assert.Contains(
            progress.Updates,
            update => update.Level == ResizeProgressLevel.Error
                && update.Message.Contains("broken.jpg", StringComparison.Ordinal)
                && (update.Message.Contains("could not be decoded", StringComparison.Ordinal)
                    || update.Message.Contains("unsupported image format", StringComparison.Ordinal)));
    }

    /// <summary>
    /// Verifies that a missing input folder is rejected with a clear exception.
    /// </summary>
    [Fact]
    public async Task ResizeAsyncRejectsMissingInputFolder()
    {
        using TestWorkspace workspace = new();
        ImageResizrService service = new();

        DirectoryNotFoundException exception = await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => service.ResizeAsync(
                new ResizeImagesRequest(
                    Path.Combine(workspace.RootFolder, "missing"),
                    workspace.OutputFolder,
                    TargetWidth: 60,
                    TargetHeight: 60,
                    ImageResizeMode.Fit,
                    ShrinkOnly: true,
                    IgnoreOrientation: true,
                    OverwriteExisting: false),
                cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("does not exist", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Asserts that an image file has the expected dimensions.
    /// </summary>
    private static async Task AssertImageDimensionsAsync(
        string path,
        int expectedWidth,
        int expectedHeight)
    {
        await Task.Yield();

        using SKCodec codec = SKCodec.Create(path);
        SKImageInfo imageInfo = codec.Info;

        Assert.Equal(expectedWidth, imageInfo.Width);
        Assert.Equal(expectedHeight, imageInfo.Height);
    }

    /// <summary>
    /// Captures progress updates reported by the image resize service.
    /// </summary>
    private sealed class ListProgress : IProgress<ResizeProgressUpdate>
    {
        /// <summary>
        /// Gets the captured progress updates.
        /// </summary>
        public List<ResizeProgressUpdate> Updates { get; } = [];

        /// <summary>
        /// Records a progress update.
        /// </summary>
        public void Report(ResizeProgressUpdate value)
        {
            Updates.Add(value);
        }
    }

    /// <summary>
    /// Creates and cleans up temporary folders for image resize tests.
    /// </summary>
    private sealed class TestWorkspace : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TestWorkspace" /> class.
        /// </summary>
        public TestWorkspace()
        {
            RootFolder = Path.Combine(Path.GetTempPath(), "ImageResizr.Tests", Guid.NewGuid().ToString("N"));
            InputFolder = Path.Combine(RootFolder, "input");
            OutputFolder = Path.Combine(RootFolder, "output");

            _ = Directory.CreateDirectory(InputFolder);
            _ = Directory.CreateDirectory(OutputFolder);
        }

        /// <summary>
        /// Gets the temporary root folder path.
        /// </summary>
        public string RootFolder { get; }

        /// <summary>
        /// Gets the temporary input folder path.
        /// </summary>
        public string InputFolder { get; }

        /// <summary>
        /// Gets the temporary output folder path.
        /// </summary>
        public string OutputFolder { get; }

        /// <summary>
        /// Deletes the temporary workspace folder.
        /// </summary>
        public void Dispose()
        {
            if (Directory.Exists(RootFolder))
            {
                Directory.Delete(RootFolder, recursive: true);
            }
        }

        /// <summary>
        /// Creates a plain input file in the workspace.
        /// </summary>
        public void CreateInputFile(string fileName, string content)
        {
            File.WriteAllText(Path.Combine(InputFolder, fileName), content);
        }

        /// <summary>
        /// Creates a plain output file in the workspace.
        /// </summary>
        /// <returns>The created output file path.</returns>
        public string CreateOutputFile(string fileName, string content)
        {
            string path = Path.Combine(OutputFolder, fileName);
            File.WriteAllText(path, content);
            return path;
        }

        /// <summary>
        /// Creates an input image with the requested dimensions.
        /// </summary>
        /// <returns>The created input image path.</returns>
        public async Task<string> CreateInputImageAsync(
            string fileName,
            int width,
            int height)
        {
            string path = Path.Combine(InputFolder, fileName);

            using SKBitmap image = new(width, height);
            using SKCanvas canvas = new(image);
            canvas.Clear(SKColors.CornflowerBlue);

            using SKData encodedImage = image.Encode(ResolveEncodedImageFormat(path), quality: 100)
                ?? throw new InvalidOperationException($"The test image '{path}' could not be encoded.");
            await using FileStream outputStream = new(path, FileMode.Create, FileAccess.Write, FileShare.None);
            encodedImage.SaveTo(outputStream);
            await outputStream.FlushAsync(TestContext.Current.CancellationToken);

            return path;
        }

        /// <summary>
        /// Resolves the encoder used for test image creation.
        /// </summary>
        private static SKEncodedImageFormat ResolveEncodedImageFormat(string path)
        {
            string extension = Path.GetExtension(path);

            if (extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase))
            {
                return SKEncodedImageFormat.Jpeg;
            }

            if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
            {
                return SKEncodedImageFormat.Png;
            }

            if (extension.Equals(".webp", StringComparison.OrdinalIgnoreCase))
            {
                return SKEncodedImageFormat.Webp;
            }

            if (extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase))
            {
                return SKEncodedImageFormat.Bmp;
            }

            if (extension.Equals(".gif", StringComparison.OrdinalIgnoreCase))
            {
                return SKEncodedImageFormat.Gif;
            }

            throw new NotSupportedException($"The '{extension}' test format is not supported.");
        }
    }
}
