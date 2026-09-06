# Image Resizr

Image Resizr is a cross-platform desktop app for resizing entire folders of images without turning the task into a script. Built with Avalonia on .NET, it focuses on a practical batch workflow: choose a source folder, pick a preset or custom size, decide how the image should fit the target bounds, and follow every result in a live processing history.

![Illustrated header for Image Resizr](docs/header.jpg)

The project is aimed at everyday image-processing jobs such as preparing lighter photo folders, generating web-ready assets, creating preview copies, or reducing large batches before sharing them.

## Highlights

- Batch resize supported images from a desktop UI
- Preserve original file names and output formats
- Choose between `Fit`, `Fill`, and `Stretch` resize modes
- Use built-in presets or define custom pixel dimensions
- Prevent upscaling with the `Shrink only` option
- Respect or ignore EXIF orientation metadata
- Skip or overwrite existing files with safe temporary-file writes
- Track every processed image with its original and resulting dimensions
- Distinguish resized, overwritten, skipped, and failed files in the live history
- Follow overall progress through processed-file counts, a progress bar, and percentage updates
- Automatically use English or German based on the operating-system UI language
- Follow the operating-system light or dark appearance while retaining the orange accent color
- Support `.jpg`, `.jpeg`, `.png`, `.gif`, `.bmp`, and `.webp`

> Note: the current resize workflow processes files from the selected input folder only. It does not recurse into nested subfolders.

## Screenshots

### Resize configuration

![Image Resizr resize configuration and empty progress history](docs/screenshot01.png)

The two-card layout keeps configuration and processing feedback visible together. Select source and target folders, apply a preset or custom size, and control how the batch should treat orientation, overwrites, and upscaling.

### Live progress during a running batch

![Image Resizr live progress while resizing images](docs/screenshot02.png)

While the batch is running, Image Resizr updates the processed-file count, percentage, progress bar, and a scrollable per-image history. Successful entries show the dimension change in the compact `4032 × 3024 → 1920 × 1440` format, while skipped and failed files include a readable reason.

### Completion summary

![Image Resizr completion summary and resize history](docs/screenshot03.png)

After the batch finishes, the same workspace retains the complete result history and shows the final summary, including processed files and saved space, without forcing you into a separate report view. Starting another batch clears the previous history and resets all progress values.

## Resize Workflow

1. Select the folder that contains the source images.
2. Choose where the resized copies should be written.
3. Pick a preset or switch to a custom size.
4. Select the resize mode: `Fit` keeps the full image inside the target bounds, `Fill` fills the target bounds and crops as needed, and `Stretch` forces the exact size.
5. Decide whether to avoid enlarging smaller images, ignore EXIF orientation, or overwrite existing files.
6. Start the batch and follow overall progress and individual image results in the live progress panel.

## Built-in Presets

- `Small`: fits within `854 × 480`
- `Medium`: fits within `1366 × 768`
- `Large`: fits within `1920 × 1080`
- `Phone`: fits within `320 × 568`
- `Custom`: lets you enter your own pixel dimensions

## Live Progress and Resize History

The progress card occupies the full right side of the application and remains the same effective height as the configuration card. Its compact summary displays the current file count, progress percentage, and progress bar, leaving most of the available space for the virtualized history list.

Each completed image creates one history entry:

- **Resized** and **overwritten** entries show original and resulting dimensions.
- **Skipped** entries explain whether the output already exists or the image already satisfies the requested size.
- **Failed** entries remain visible without interrupting the rest of the batch.
- A final **Summary** entry reports resized, overwritten, skipped, and failed totals.

The latest entry is kept visible automatically while processing. The list scrolls internally, so large batches do not increase the window height.

## Localization and Appearance

Image Resizr includes English and German resources. The application uses the operating system's current UI culture automatically:

- German cultures such as `de-DE`, `de-AT`, and `de-CH` use German.
- English cultures use English.
- Other cultures fall back to English.

Localized text covers the complete workflow, including labels, folder dialogs, presets, resize modes, validation, progress summaries, and history statuses. The interface also follows the operating system's light or dark theme.

## Supported Formats

Image Resizr currently works with the following file types:

- `.jpg`
- `.jpeg`
- `.png`
- `.gif`
- `.bmp`
- `.webp`

The service keeps the original file extension and writes resized output with the same format family. Animated `.gif` and `.webp` files are processed as still images, so only the first frame is used in the resized output.

## Getting Started

### Prerequisites

- .NET SDK matching the version pinned in `global.json`

### Restore

```bash
dotnet restore ImageResizr.slnx
```

### Build

```bash
dotnet build ImageResizr.slnx --configuration Release
```

### Run the desktop app

```bash
dotnet run --project src/ImageResizr.App/ImageResizr.App.csproj --configuration Release
```

### Run the tests

```bash
dotnet test ImageResizr.slnx --configuration Release
```

There is no separate lint command. Repository analyzers and code-style checks run as part of the build.

## Project Structure

- `src/ImageResizr.App`: Avalonia desktop UI, localization resources, and presentation models
- `src/ImageResizr.Core`: resize pipeline, structured result models, and file/image processing
- `tests/ImageResizr.Tests`: xUnit tests for resizing, progress history, and localization

The solution intentionally keeps the UI thin. Avalonia-specific code lives in the app project, while the resize workflow, validation, format handling, and progress reporting live in the core library.

## Tech Stack

- [.NET](https://dotnet.microsoft.com/)
- [Avalonia UI](https://avaloniaui.net/)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
- [SkiaSharp](https://github.com/mono/SkiaSharp)
- [xUnit](https://xunit.net/)

## Quality

The repository includes automated coverage for both the batch resize service and the window view model. Tests verify resize behavior, file handling, overwrite rules, shrink-only logic, structured progress entries, dimension reporting, history reset behavior, percentage calculation, German resources, and English fallback.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
