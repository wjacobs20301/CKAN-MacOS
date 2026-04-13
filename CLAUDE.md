# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What is CKAN?

CKAN (Comprehensive Kerbal Archive Network) is a mod manager for Kerbal Space Program. It downloads, installs, updates, and removes mods while resolving dependencies and conflicts. The metadata specification (`Spec.md`) defines the `.ckan` format used across the ecosystem.

## Build Commands

The build system uses **Cake Frosting** (C# Make). All build artifacts go to `_build/`.

```bash
# Build everything (ckan.exe + netkan.exe)
./build.sh

# Build only ckan.exe
./build.sh --target=Ckan

# Run all tests (builds first)
./build.sh --target=Test

# Run unit tests only (no rebuild)
./build.sh --target=Test-UnitTests+Only

# Run tests without recompiling
./build.sh --target=Test+Only

# Filter tests (NUnit syntax, auto-translated for dotnet test)
./build.sh --target=Test+Only --where="class=Tests.Core.UtilitiesTests"
./build.sh --target=Test+Only --where="name=SomeTestMethod"
./build.sh --target=Test+Only --where="Category!=FlakyNetwork"

# Build with specific configuration
./build.sh --configuration=Release
./build.sh --configuration=Debug

# Build the Avalonia GUI project directly
dotnet build GUI.Avalonia/CKAN-GUI-Avalonia.csproj

# Run the Avalonia GUI
dotnet run --project Cmdline -f net8.0 -- gui
```

**Build outputs:**
- `_build/out/<AssemblyName>/<Config>/bin/net8.0/` - Per-project build output
- `_build/out/CKAN-CmdLine/<Config>/bin/net8.0/osx-{arm64,x64}/publish/` - Published runtimes used by the `.app` bundle
- `_build/osx/dmg/CKAN.app` and `_build/osx/CKAN.dmg` - macOS bundle outputs (via `macosx/Makefile`)
- `_build/test/nunit/` - Test results

## Architecture

### Project dependency graph

```
Cmdline (entry point, net8.0) --> Core (shared library)
    |-> GUI.Avalonia (Avalonia UI, net8.0)
    |-> ConsoleUI (terminal UI, net8.0)
Netkan --> Core
Tests --> all projects
```

This is a **macOS-only fork**. The upstream Windows WinForms GUI (`GUI/`), the Windows AutoUpdate helper (`AutoUpdate/`), Debian/RPM packaging, Dockerfiles, and `build.ps1` have been removed. All remaining projects target net8.0 only.

### Core library (`/Core/`)

All business logic lives here. Key subsystems:
- **`Types/CkanModule.cs`** - Module metadata, JSON-serializable with `[JsonProperty]` attributes
- **`Registry/`** - `Registry` (in-memory mod database) and `RegistryManager` (persistent with file locking)
- **`Relationships/RelationshipResolver.cs`** - Dependency/conflict resolution engine
- **`GameInstance.cs` / `GameInstanceManager.cs`** - Manages KSP installations
- **`IO/ModuleInstaller.cs`** - Atomic install/uninstall via `CkanTransaction`
- **`Net/`** - Download management (`NetAsyncDownloader`, `NetModuleCache`)
- **`Configuration/IConfiguration.cs`** - Settings persistence (`JsonConfiguration` impl)
- **`Versioning/`** - Custom version comparison (not standard SemVer)
- **`Games/`** - Game abstraction (`IGame` interface, KSP1/KSP2 implementations)

### UI abstraction: `IUser` (`/Core/User.cs`)

Every UI (GUI, ConsoleUI, Avalonia) implements `IUser` to handle user interaction from Core:
- `RaiseYesNoDialog`, `RaiseSelectionDialog` - blocking prompts
- `RaiseProgress` - progress updates
- `RaiseError`, `RaiseMessage` - notifications

When implementing a new UI, create an `IUser` implementation that bridges Core callbacks to your UI framework's thread model.

### Dependency Injection

`ServiceLocator` (`/Core/ServiceLocator.cs`) provides an Autofac container with singleton registrations for `IConfiguration`, `IGameComparator`, `IGameVersionProvider`, `RepositoryDataManager`. Access via `ServiceLocator.Container.Resolve<T>()`.

### Avalonia GUI (`/GUI.Avalonia/`)

Cross-platform GUI using Avalonia 11.2 + CommunityToolkit.Mvvm (MVVM pattern):
- `[ObservableProperty]` generates property change notifications
- `[RelayCommand]` generates `ICommand` implementations
- DataGrid columns require `x:CompileBindings="False"` (compiled bindings don't work with DataGrid cell bindings)
- `ObservableCollection` must be modified on UI thread via `Dispatcher.UIThread.Post()`
- `AvaloniaUser.cs` bridges `IUser` callbacks from background threads to UI thread using `Dispatcher.UIThread.InvokeAsync().GetAwaiter().GetResult()` for blocking calls

## Target Framework

All projects target `net8.0` only. No multi-targeting, no `netstandard2.0`, no `net481`, no `net8.0-windows`.

## Code Style

- C# 9, nullable reference types enabled, warnings as errors
- `var` is preferred (IDE0008 suppressed)
- Parentheses for clarity are fine (IDE0047 suppressed)
- Alignment spaces allowed (IDE0055 suppressed)
- Local functions discouraged (IDE0039 suppressed)
- `.editorconfig` has full analyzer configuration
- Root namespace is `CKAN`; sub-namespaces for subsystems (`CKAN.Versioning`, `CKAN.Games`, etc.)
- Tests use `Tests.*` namespaces with NUnit 3 (`[TestFixture]`, `[Test]`)

## Key Patterns

- **Exception naming**: Domain exceptions end in `Kraken` (e.g., `TooManyModsProvideKraken`, `CancelledActionKraken`, `ModuleNotFoundKraken`)
- **Atomic file operations**: Install/uninstall uses `CkanTransaction` for rollback on failure
- **`GameInstance.Game`** (capital G) - the `IGame` implementation; a common source of compile errors
- **`RelationshipResolverOptions`** requires a `StabilityToleranceConfig` parameter (no parameterless constructor)
- **`ModuleInstaller`** is in `CKAN.IO` namespace (not `CKAN`)
