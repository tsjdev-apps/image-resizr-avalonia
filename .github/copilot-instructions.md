# Copilot Instructions

## Build, test, and lint commands

- Use the SDK pinned in `global.json`.
- Restore: `dotnet restore ImageResizr.slnx`
- Build: `dotnet build ImageResizr.slnx --configuration Release`
- Run the desktop app: `dotnet run --project src\ImageResizr.App\ImageResizr.App.csproj --configuration Release`
- Run all tests: `dotnet test ImageResizr.slnx --configuration Release`
- Run a single test: `dotnet test tests\ImageResizr.Tests\ImageResizr.Tests.csproj --configuration Release --filter "FullyQualifiedName=ImageResizr.Tests.ImageResizrServiceTests.ResizeAsyncPreservesFileNamesAndExtensionsWhenUsingFitMode"`
- There is no separate lint command. Repo-wide analyzers and code style checks are enforced during `dotnet build` through `Directory.Build.props`.

## High-level architecture

- The solution is split into three projects:
  - `src\ImageResizr.App`: Avalonia desktop UI shell.
  - `src\ImageResizr.Core`: image resize workflow, models, and file/image processing.
  - `tests\ImageResizr.Tests`: xUnit tests for both the core service and the view model.
- `ImageResizr.App` is intentionally thin:
  - `App.axaml.cs` builds the DI container and registers `IImageResizrService`, `MainWindowViewModel`, and `MainWindow` as singletons.
  - `MainWindow.axaml` binds directly to `MainWindowViewModel` with compiled bindings (`x:DataType`).
  - `MainWindow.axaml.cs` only handles Avalonia-specific folder picking and passes those operations into the view model through delegates.
- `ImageResizr.Core` owns the resize pipeline:
  - `ResizeImagesRequest`, `ResizeImagesResult`, `ResizeProgressUpdate`, `ResizePresetOption`, and `ImageResizeMode` define the batch contract.
  - `ImageResizrService` validates paths and dimensions, enumerates supported image files, applies EXIF orientation when requested, computes fit/fill/stretch resize plans, and writes outputs through temporary files so overwriting input/output paths is safe.
  - Progress messages are produced in the core service as user-facing text plus a `ResizeProgressLevel`, then surfaced by the view model in the right-side progress panel.
- Tests mirror that separation:
  - `ImageResizrServiceTests` uses real temp folders and real ImageSharp images to verify file-system behavior, supported formats, overwrite rules, shrink-only behavior, and resize results.
  - `MainWindowViewModelTests` uses a fake `IImageResizrService` to verify UI-state behavior such as preserving the final completion summary even if late progress arrives after completion.

## Key conventions

- Keep Avalonia-specific work in the app project and resize/business logic in `ImageResizr.Core`. If a change does not require Avalonia APIs, it usually belongs in the core service or models rather than the window code-behind.
- The view model depends on `IImageResizrService` and exposes bindable state through CommunityToolkit.Mvvm source generators (`[ObservableProperty]`, `[RelayCommand]`, partial change handlers). Follow that pattern instead of hand-written `INotifyPropertyChanged`.
- The window code-behind should stay minimal. In this repo it only initializes the view and wires folder-picker delegates; it does not contain resize workflow logic.
- User-visible progress/status text originates in the core service via `ResizeProgressUpdate`. When changing resize behavior, keep progress reporting aligned with the actual workflow so the view model and tests continue to reflect accurate messages and severity levels.
- Expected operational failures are surfaced explicitly. `ImageResizrService` throws clear exceptions for invalid inputs or missing folders, and `MainWindowViewModel` maps known exceptions into `StatusMessage`, `LatestActivityMessage`, and `ValidationMessage`.
- Repo-wide build rules are strict: `Directory.Build.props` enables latest analyzers, enforces code style in build, generates XML docs, and treats warnings as errors for every project.
- Match the existing C# style from `.editorconfig`: file-scoped namespaces, explicit types instead of `var`, braces required, primary constructors where they fit, and collection expressions/record types for small immutable model objects.
- Package versions are centralized in `Directory.Packages.props`; add or update package versions there instead of hardcoding versions in individual project files.
