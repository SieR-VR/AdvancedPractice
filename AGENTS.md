# Repository Guidelines

## Project Structure & Module Organization
AdvancedPractice extends Beat Saber 1.40.8's practice setup screen. `Plugin.cs` owns BSIPA startup, configuration, and Harmony registration. `AdvancedPracticeController.cs` loads beatmap data, binds controls, and scopes the NJS patch to practice gameplay. `Models/` contains game-independent timing and speed calculations. `Views/` contains embedded BSML for practice controls and Mod Settings. `Configuration/PluginConfig.cs` defines persistent settings; `manifest.json` and `Properties/AssemblyInfo.cs` contain version metadata.

`Tests/` is a standalone console regression suite. The project explicitly includes C# files; add new source paths to `AdvancedPractice.csproj` when introducing directories. `.reference/` and `.tools/` are ignored local research utilities, not shipped dependencies.

## Build, Test, and Development Commands
Set `BeatSaberDir` in the ignored `AdvancedPractice.csproj.user` file to a local Beat Saber 1.40.8 installation containing BSIPA and BSML. The plugin targets .NET Framework 4.8; the test runner targets .NET 10.

- `dotnet build AdvancedPractice.csproj -c Release` builds `bin/Release/AdvancedPractice.dll` without deploying into the game.
- `dotnet build AdvancedPractice.csproj -c Debug` produces a development build.
- `dotnet run --project Tests/AdvancedPractice.Tests.csproj -c Release` checks BPM boundaries, conversion round trips, and speed bounds.

Copy the DLL into the game's `Plugins/` directory only when intentionally testing it. Remove PracticePlugin first.

## Coding Style & Naming Conventions
Use four-space indentation, separate-line braces, PascalCase for types and public members, and camelCase for parameters and private fields. Match filenames to their primary class and keep namespaces under `AdvancedPractice`. Use `Plugin.Log` for diagnostics. No formatter or lint tool is configured. Keep timing calculations independent of Unity where practical.

## Testing Guidelines
Add deterministic assertions to the console suite for timing or arithmetic changes. There is no coverage threshold or external test framework. Validate Unity lifecycle, BSML layout, VNJS easing, restart behavior, and JDFixer/BSPlus interactions manually in game. Record the game version and distinguish automated checks from VR checks.

## Commit & Pull Request Guidelines
Use short imperative subjects, such as `Fix BPM boundary conversion`; no established historical convention was identified during initial inspection. Describe changed behavior, link relevant issues, and include build/test results. Include screenshots for visible UI changes. Keep local game paths, DLL dependencies, and generated outputs out of commits. Synchronize manifest and assembly versions for releases.
