using System.Collections.Frozen;
using System.Globalization;
using ImageResizr.Core.Models;
using SkiaSharp;

namespace ImageResizr.Core.Services;

/// <summary>
/// Resizes supported image files while preserving file names and formats.
/// </summary>
public sealed class ImageResizrService : IImageResizrService
{
    private const int DefaultEncodeQuality = 100;
    private static readonly SKSamplingOptions ResizeSampling = new(SKFilterMode.Linear, SKMipmapMode.None);
    private static readonly FrozenSet<string> SupportedExtensions = new[]
    {
        ".bmp",
        ".gif",
        ".jpeg",
        ".jpg",
        ".png",
        ".webp"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Resizes supported images from the input folder into the output folder.
    /// </summary>
    /// <param name="request">The resize request to execute.</param>
    /// <param name="progress">The optional progress reporter for user-facing updates.</param>
    /// <param name="cancellationToken">The token used to cancel the resize operation.</param>
    /// <returns>The resize operation result.</returns>
    public async Task<ResizeImagesResult> ResizeAsync(
        ResizeImagesRequest request,
        IProgress<ResizeProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(
            () => ResizeCoreAsync(request, progress, cancellationToken),
            cancellationToken);
    }

    private static async Task<ResizeImagesResult> ResizeCoreAsync(
        ResizeImagesRequest request,
        IProgress<ResizeProgressUpdate>? progress,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        string inputFolder = ValidateFolderPath(request.InputFolder, nameof(request.InputFolder));
        string outputFolder = ValidateFolderPath(request.OutputFolder, nameof(request.OutputFolder));
        int targetWidth = ValidateDimension(request.TargetWidth, nameof(request.TargetWidth));
        int targetHeight = ValidateDimension(request.TargetHeight, nameof(request.TargetHeight));

        if (!Directory.Exists(inputFolder))
        {
            throw new DirectoryNotFoundException($"The input folder '{inputFolder}' does not exist.");
        }

        _ = Directory.CreateDirectory(outputFolder);

        progress?.Report(new ResizeProgressUpdate(0, 0, "Scanning the input folder for supported image files."));

        List<string> sourceFiles = [.. Directory
            .EnumerateFiles(inputFolder, "*", SearchOption.TopDirectoryOnly)
            .Where(filePath => SupportedExtensions.Contains(Path.GetExtension(filePath)))
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)];

        if (sourceFiles.Count == 0)
        {
            progress?.Report(new ResizeProgressUpdate(0, 0, "No supported image files were found in the input folder."));
            return new ResizeImagesResult(0, 0, 0, 0, 0, 0, []);
        }

        int resizedFiles = 0;
        int skippedFiles = 0;
        int failedFiles = 0;
        long inputBytes = 0;
        long outputBytes = 0;
        List<string> outputFiles = [];

        ResizeImagesRequest validatedRequest = request with
        {
            TargetWidth = targetWidth,
            TargetHeight = targetHeight
        };

        for (int index = 0; index < sourceFiles.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string sourceFile = sourceFiles[index];
            string destinationFile = Path.Combine(outputFolder, Path.GetFileName(sourceFile));
            int processedCount = index + 1;

            if (File.Exists(destinationFile) && !validatedRequest.OverwriteExisting)
            {
                skippedFiles++;
                progress?.Report(new ResizeProgressUpdate(
                    processedCount,
                    sourceFiles.Count,
                    $"Skipped '{Path.GetFileName(sourceFile)}' because it already exists in the output folder.",
                    ResizeProgressLevel.Warning));
                continue;
            }

            try
            {
                FileInfo inputInfo = new(sourceFile);
                inputBytes += inputInfo.Length;

                using SKBitmap decodedBitmap = LoadBitmap(sourceFile, out SKEncodedOrigin encodedOrigin);

                using SKBitmap? orientedBitmap = validatedRequest.IgnoreOrientation
                    ? null
                    : ApplyOrientation(decodedBitmap, encodedOrigin);
                SKBitmap workingBitmap = orientedBitmap ?? decodedBitmap;

                ResizePlan resizePlan = BuildResizePlan(workingBitmap.Width, workingBitmap.Height, validatedRequest);

                if (!resizePlan.ShouldResize)
                {
                    bool copied = CopyUnchanged(sourceFile, destinationFile, validatedRequest.OverwriteExisting);

                    if (copied || File.Exists(destinationFile))
                    {
                        outputBytes += new FileInfo(destinationFile).Length;
                        outputFiles.Add(destinationFile);
                    }

                    skippedFiles++;
                    progress?.Report(new ResizeProgressUpdate(
                        processedCount,
                        sourceFiles.Count,
                        $"Kept '{Path.GetFileName(sourceFile)}' unchanged because it is already within the target size.",
                        ResizeProgressLevel.Info));
                    continue;
                }

                using SKBitmap resizedBitmap = ApplyResize(workingBitmap, resizePlan, validatedRequest.ResizeMode);
                await SaveImageAsync(resizedBitmap, destinationFile, validatedRequest.OverwriteExisting, cancellationToken);

                resizedFiles++;
                outputFiles.Add(destinationFile);
                outputBytes += new FileInfo(destinationFile).Length;

                progress?.Report(new ResizeProgressUpdate(
                    processedCount,
                    sourceFiles.Count,
                    $"Resized '{Path.GetFileName(sourceFile)}' to {resizePlan.Width.ToString(CultureInfo.CurrentCulture)} x {resizePlan.Height.ToString(CultureInfo.CurrentCulture)} pixels.",
                    ResizeProgressLevel.Success));
            }
            catch (InvalidDataException exception)
            {
                failedFiles++;
                ReportFailure(progress, processedCount, sourceFiles.Count, sourceFile, exception.Message);
            }
            catch (NotSupportedException exception)
            {
                failedFiles++;
                ReportFailure(progress, processedCount, sourceFiles.Count, sourceFile, exception.Message);
            }
            catch (IOException exception)
            {
                failedFiles++;
                ReportFailure(progress, processedCount, sourceFiles.Count, sourceFile, exception.Message);
            }
            catch (UnauthorizedAccessException exception)
            {
                failedFiles++;
                ReportFailure(progress, processedCount, sourceFiles.Count, sourceFile, exception.Message);
            }
        }

        return new ResizeImagesResult(
            sourceFiles.Count,
            resizedFiles,
            skippedFiles,
            failedFiles,
            inputBytes,
            outputBytes,
            outputFiles);
    }

    /// <summary>
    /// Applies the selected resize operation to the loaded image.
    /// </summary>
    private static SKBitmap ApplyResize(
        SKBitmap image,
        ResizePlan resizePlan,
        ImageResizeMode resizeMode)
    {
        SKRect sourceRectangle = resizeMode == ImageResizeMode.Fill
            ? BuildFillSourceRectangle(image.Width, image.Height, resizePlan.Width, resizePlan.Height)
            : new SKRect(0, 0, image.Width, image.Height);
        SKRect destinationRectangle = new(0, 0, resizePlan.Width, resizePlan.Height);
        SKBitmap resizedBitmap = new(resizePlan.Width, resizePlan.Height);

        using SKCanvas canvas = new(resizedBitmap);
        using SKImage sourceImage = SKImage.FromBitmap(image);
        canvas.Clear(SKColors.Transparent);
        canvas.DrawImage(sourceImage, sourceRectangle, destinationRectangle, ResizeSampling, paint: null);
        canvas.Flush();

        return resizedBitmap;
    }

    /// <summary>
    /// Builds the final resize plan for the requested resize mode.
    /// </summary>
    private static ResizePlan BuildResizePlan(
        int originalWidth,
        int originalHeight,
        ResizeImagesRequest request)
    {
        ResizePlan resizePlan = request.ResizeMode switch
        {
            ImageResizeMode.Fill => BuildFillPlan(originalWidth, originalHeight, request),
            ImageResizeMode.Stretch => BuildStretchPlan(originalWidth, originalHeight, request),
            _ => BuildFitPlan(originalWidth, originalHeight, request)
        };

        return resizePlan with
        {
            Width = Math.Max(1, resizePlan.Width),
            Height = Math.Max(1, resizePlan.Height)
        };
    }

    /// <summary>
    /// Builds a resize plan that keeps the image inside the target bounds.
    /// </summary>
    private static ResizePlan BuildFitPlan(
        int originalWidth,
        int originalHeight,
        ResizeImagesRequest request)
    {
        double scale = Math.Min(
            (double)request.TargetWidth / originalWidth,
            (double)request.TargetHeight / originalHeight);

        if (request.ShrinkOnly)
        {
            scale = Math.Min(1, scale);
        }

        int width = (int)Math.Round(originalWidth * scale, MidpointRounding.AwayFromZero);
        int height = (int)Math.Round(originalHeight * scale, MidpointRounding.AwayFromZero);

        return new ResizePlan(width, height, width != originalWidth || height != originalHeight);
    }

    /// <summary>
    /// Builds a resize plan that crops the image to the target bounds.
    /// </summary>
    private static ResizePlan BuildFillPlan(
        int originalWidth,
        int originalHeight,
        ResizeImagesRequest request)
    {
        double scale = Math.Max(
            (double)request.TargetWidth / originalWidth,
            (double)request.TargetHeight / originalHeight);

        if (request.ShrinkOnly && scale > 1)
        {
            return new ResizePlan(originalWidth, originalHeight, ShouldResize: false);
        }

        return new ResizePlan(
            request.TargetWidth,
            request.TargetHeight,
            request.TargetWidth != originalWidth || request.TargetHeight != originalHeight);
    }

    /// <summary>
    /// Builds a resize plan that stretches the image to the target bounds.
    /// </summary>
    private static ResizePlan BuildStretchPlan(
        int originalWidth,
        int originalHeight,
        ResizeImagesRequest request)
    {
        int width = request.ShrinkOnly ? Math.Min(originalWidth, request.TargetWidth) : request.TargetWidth;
        int height = request.ShrinkOnly ? Math.Min(originalHeight, request.TargetHeight) : request.TargetHeight;

        return new ResizePlan(width, height, width != originalWidth || height != originalHeight);
    }

    /// <summary>
    /// Copies an image that already satisfies the requested size.
    /// </summary>
    private static bool CopyUnchanged(
        string sourceFile,
        string destinationFile,
        bool overwriteExisting)
    {
        if (PathsMatch(sourceFile, destinationFile))
        {
            return false;
        }

        string temporaryFile = BuildTemporaryFilePath(destinationFile);

        try
        {
            File.Copy(sourceFile, temporaryFile, overwrite: true);
            File.Move(temporaryFile, destinationFile, overwriteExisting);
            return true;
        }
        finally
        {
            DeleteTemporaryFile(temporaryFile);
        }
    }

    /// <summary>
    /// Deletes a temporary file when it still exists.
    /// </summary>
    private static void DeleteTemporaryFile(string temporaryFile)
    {
        if (File.Exists(temporaryFile))
        {
            File.Delete(temporaryFile);
        }
    }

    /// <summary>
    /// Builds a temporary output path next to the final destination file.
    /// </summary>
    private static string BuildTemporaryFilePath(string destinationFile)
    {
        string directory = Path.GetDirectoryName(destinationFile)
            ?? throw new InvalidOperationException("The destination file must include a directory path.");
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(destinationFile);
        string extension = Path.GetExtension(destinationFile);

        return Path.Combine(directory, $"{fileNameWithoutExtension}.{Guid.NewGuid():N}{extension}");
    }

    /// <summary>
    /// Builds the centered crop rectangle needed for fill mode.
    /// </summary>
    private static SKRect BuildFillSourceRectangle(
        int originalWidth,
        int originalHeight,
        int targetWidth,
        int targetHeight)
    {
        double targetAspectRatio = (double)targetWidth / targetHeight;
        double originalAspectRatio = (double)originalWidth / originalHeight;

        if (originalAspectRatio > targetAspectRatio)
        {
            float cropWidth = (float)(originalHeight * targetAspectRatio);
            float offsetX = (originalWidth - cropWidth) / 2F;
            return new SKRect(offsetX, 0, offsetX + cropWidth, originalHeight);
        }

        float cropHeight = (float)(originalWidth / targetAspectRatio);
        float offsetY = (originalHeight - cropHeight) / 2F;
        return new SKRect(0, offsetY, originalWidth, offsetY + cropHeight);
    }

    /// <summary>
    /// Saves an image through a temporary file and then moves it into place.
    /// </summary>
    private static async Task SaveImageAsync(
        SKBitmap image,
        string destinationFile,
        bool overwriteExisting,
        CancellationToken cancellationToken)
    {
        string temporaryFile = BuildTemporaryFilePath(destinationFile);

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            SKEncodedImageFormat format = ResolveEncodedImageFormat(destinationFile);
            await using (FileStream outputStream = new(temporaryFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using SKData encodedImage = image.Encode(format, DefaultEncodeQuality)
                    ?? throw new NotSupportedException($"SkiaSharp could not encode the '{Path.GetExtension(destinationFile)}' format.");
                encodedImage.SaveTo(outputStream);
                await outputStream.FlushAsync(cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryFile, destinationFile, overwriteExisting);
        }
        finally
        {
            DeleteTemporaryFile(temporaryFile);
        }
    }

    /// <summary>
    /// Applies the encoded EXIF orientation to the decoded bitmap when needed.
    /// </summary>
    private static SKBitmap? ApplyOrientation(SKBitmap image, SKEncodedOrigin orientation)
    {
        if (orientation is SKEncodedOrigin.Default or SKEncodedOrigin.TopLeft)
        {
            return null;
        }

        int outputWidth = RequiresDimensionSwap(orientation) ? image.Height : image.Width;
        int outputHeight = RequiresDimensionSwap(orientation) ? image.Width : image.Height;
        SKBitmap orientedBitmap = new(outputWidth, outputHeight);

        using SKCanvas canvas = new(orientedBitmap);
        using SKImage sourceImage = SKImage.FromBitmap(image);
        canvas.Clear(SKColors.Transparent);
        canvas.SetMatrix(CreateOrientationMatrix(orientation, image.Width, image.Height));
        canvas.DrawImage(sourceImage, 0, 0, ResizeSampling, paint: null);
        canvas.Flush();

        return orientedBitmap;
    }

    /// <summary>
    /// Loads a bitmap and captures the encoded orientation without keeping the source file locked.
    /// </summary>
    private static SKBitmap LoadBitmap(string sourceFile, out SKEncodedOrigin orientation)
    {
        using SKCodec codec = SKCodec.Create(sourceFile);
        orientation = codec.EncodedOrigin;

        return SKBitmap.Decode(codec)
            ?? throw new InvalidDataException($"The file '{sourceFile}' could not be decoded.");
    }

    /// <summary>
    /// Resolves the output encoder from the destination file extension.
    /// </summary>
    private static SKEncodedImageFormat ResolveEncodedImageFormat(string destinationFile)
    {
        string extension = Path.GetExtension(destinationFile);

        if (extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase))
        {
            return SKEncodedImageFormat.Bmp;
        }

        if (extension.Equals(".gif", StringComparison.OrdinalIgnoreCase))
        {
            return SKEncodedImageFormat.Gif;
        }

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

        throw new NotSupportedException($"SkiaSharp cannot encode the '{extension}' format.");
    }

    /// <summary>
    /// Determines whether the orientation swaps width and height.
    /// </summary>
    private static bool RequiresDimensionSwap(SKEncodedOrigin orientation)
    {
        return orientation is SKEncodedOrigin.LeftBottom
            or SKEncodedOrigin.LeftTop
            or SKEncodedOrigin.RightBottom
            or SKEncodedOrigin.RightTop;
    }

    /// <summary>
    /// Creates the affine transform that normalizes one EXIF orientation to top-left.
    /// </summary>
    private static SKMatrix CreateOrientationMatrix(
        SKEncodedOrigin orientation,
        int width,
        int height)
    {
        return orientation switch
        {
            SKEncodedOrigin.TopRight => CreateMatrix(-1, 0, width, 0, 1, 0),
            SKEncodedOrigin.BottomRight => CreateMatrix(-1, 0, width, 0, -1, height),
            SKEncodedOrigin.BottomLeft => CreateMatrix(1, 0, 0, 0, -1, height),
            SKEncodedOrigin.LeftTop => CreateMatrix(0, 1, 0, 1, 0, 0),
            SKEncodedOrigin.RightTop => CreateMatrix(0, -1, height, 1, 0, 0),
            SKEncodedOrigin.RightBottom => CreateMatrix(0, -1, height, -1, 0, width),
            SKEncodedOrigin.LeftBottom => CreateMatrix(0, 1, 0, -1, 0, width),
            _ => SKMatrix.CreateIdentity()
        };
    }

    /// <summary>
    /// Creates an affine matrix from the six non-perspective coefficients.
    /// </summary>
    private static SKMatrix CreateMatrix(
        float scaleX,
        float skewX,
        float transX,
        float skewY,
        float scaleY,
        float transY)
    {
        return new SKMatrix
        {
            ScaleX = scaleX,
            SkewX = skewX,
            TransX = transX,
            SkewY = skewY,
            ScaleY = scaleY,
            TransY = transY,
            Persp0 = 0,
            Persp1 = 0,
            Persp2 = 1
        };
    }

    /// <summary>
    /// Reports a recoverable image resize failure to the caller.
    /// </summary>
    private static void ReportFailure(
        IProgress<ResizeProgressUpdate>? progress,
        int processedCount,
        int totalCount,
        string sourceFile,
        string reason)
    {
        progress?.Report(new ResizeProgressUpdate(
            processedCount,
            totalCount,
            $"Failed to resize '{Path.GetFileName(sourceFile)}': {reason}",
            ResizeProgressLevel.Error));
    }

    /// <summary>
    /// Determines whether two file-system paths point to the same normalized location.
    /// </summary>
    private static bool PathsMatch(
        string left,
        string right)
    {
        StringComparison comparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return string.Equals(
            NormalizeFullPath(left),
            NormalizeFullPath(right),
            comparison);
    }

    /// <summary>
    /// Normalizes a path for reliable file-system comparisons.
    /// </summary>
    private static string NormalizeFullPath(string path)
    {
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
    }

    /// <summary>
    /// Validates and normalizes a folder path argument.
    /// </summary>
    private static string ValidateFolderPath(string folderPath, string argumentName)
    {
        return string.IsNullOrWhiteSpace(folderPath)
            ? throw new ArgumentException("Both the input and output folders are required.", argumentName)
            : Path.GetFullPath(folderPath.Trim());
    }

    /// <summary>
    /// Validates that a resize dimension is greater than zero.
    /// </summary>
    private static int ValidateDimension(int dimension, string argumentName)
    {
        return dimension <= 0
            ? throw new ArgumentOutOfRangeException(argumentName, "Resize dimensions must be greater than zero.")
            : dimension;
    }

    /// <summary>
    /// Describes the concrete dimensions and resize requirement for one image.
    /// </summary>
    private readonly record struct ResizePlan(
        int Width,
        int Height,
        bool ShouldResize);
}
