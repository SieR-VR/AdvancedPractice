# Repository Guidelines

## Project Structure & Module Organization
AdvancedPractice extends Beat Saber's practice setup screen. `Plugin.cs` owns BSIPA startup, Harmony registration, and menu-ready settings registration. `AdvancedPracticeController.cs` binds the controls. `Compatibility/` isolates game loaders, BSML field/property differences, and optional VNJS APIs. `Models/` contains game-independent timing, selection, and hint calculations. `Views/` contains embedded BSML and the native UI direction graphic.

`Build/Versions.props` defines supported targets; `Build/VersionedBuild.targets` generates per-version manifests. LegatoBS 0.1.1 is source-included through NuGet. Add new directories to the explicit C# includes in `AdvancedPractice.csproj`.

## Build, Test, and Development Commands
Use PowerShell 7 and .NET 10 SDK. Supply the exact target game's installation with BSIPA and matching BSML references.

- `pwsh ./Build/Build.ps1 -GameVersion 1.44.1 -GameDirectory 'D:\Games\Beat Saber 1.44.1'` builds, checks API contracts and the embedded manifest, and creates a DLL/PDB installation ZIP and a separate corresponding-source ZIP.
- Add `-ModReferencesDirectory 'D:\Refs\Plugins'` for separate BSML references.
- `dotnet run --project Tests/AdvancedPractice.Tests.csproj -c Release` checks timing, speed, selection restoration, and per-hand note selection.
- `pwsh ./Tests/BuildProfiles.Tests.ps1` validates all five Legato profile configurations without compiling provisional targets.

Outputs live in `bin/Release/<game-version>/` and `artifacts/`. Builds never install files into the game.

## Coding Style & Naming Conventions
Use four-space indentation, separate-line braces, PascalCase for types/public members, and camelCase for private fields and parameters. Keep namespaces under `AdvancedPractice`. Use `Plugin.Log` for diagnostics. Keep version branches in `Compatibility/` when practical. No formatter or linter is configured.

## Testing Guidelines
1.44.1 is the primary target; 1.40.8 remains build-verified. 1.29.0, 1.37.1, and 1.38.0 are provisional until compiled against matching references. Do not equate profile validation with game compatibility. Record reference versions and distinguish build/API checks from VR validation. Verify UI layout, VNJS easing, lifecycle, and JDFixer/BSPlus interactions in game.

## Commit & Pull Request Guidelines
Use short imperative subjects and separate calculation, integration, hint, and documentation changes. Describe behavior, validation, and version limitations; link issues and include screenshots for UI changes. Keep game DLLs, local paths, and generated outputs out of commits. Synchronize manifest/assembly versions and preserve GPLv3 notices and corresponding source when distributing source-included Legato builds.
