# Repository Guidelines

## Project Structure & Module Organization
AdvancedPractice is a BSIPA plugin for Beat Saber, built as a .NET Framework 4.7.2 library. `Plugin.cs` handles plugin initialization and starts the persistent Unity component in `AdvancedPracticeController.cs`. `Configuration/PluginConfig.cs` contains a commented-out configuration template; configuration is currently disabled. `Properties/AssemblyInfo.cs` holds assembly metadata, and `manifest.json` declares plugin identity, version, and game dependencies.

`AdvancedPractice.csproj` explicitly lists source files and embeds the manifest. Add new C# files to its `Compile` items. `Directory.Build.props` enables BeatSaberModdingTools integration. There are currently no test or asset directories.

## Build, Test, and Development Commands
Use a Windows Visual Studio Developer PowerShell with MSBuild and the .NET Framework 4.7.2 targeting pack installed. Set `BeatSaberDir` in the ignored `AdvancedPractice.csproj.user` file to your local game installation; referenced game and BSIPA DLLs must exist under `Beat Saber_Data/Managed`.

- `msbuild AdvancedPractice.csproj /restore /p:Configuration=Debug /p:DisableCopyToPlugins=True` restores build dependencies and compiles into `bin/Debug/` without copying the plugin into the game.
- `msbuild AdvancedPractice.csproj /restore /p:Configuration=Release /p:DisableCopyToPlugins=True` builds the optimized library in `bin/Release/`.
- Omit `/p:DisableCopyToPlugins=True` when intentionally deploying through the modding build tasks for local game testing.

## Coding Style & Naming Conventions
Follow the existing C# style: four-space indentation, braces on separate lines, PascalCase for types, methods, and properties, and camelCase for parameters and local variables. Keep namespaces under `AdvancedPractice` and filenames aligned with class names. Use `Plugin.Log` for diagnostics and preserve Unity lifecycle method names. No formatter or lint configuration is present.

## Testing Guidelines
No automated test framework or coverage threshold is configured. Build changes and manually verify plugin startup, scene transitions, singleton behavior, and shutdown in a compatible Beat Saber installation. The manifest currently targets game version `1.18.3` and BSIPA `^4.2.0`. Include reproduction steps and relevant log output for behavior changes.

## Commit & Pull Request Guidelines
The repository has no commits yet, so no historical message convention exists. Use short, imperative commit subjects, such as `Fix controller cleanup on shutdown`. Pull requests should describe the change, link relevant issues, and record build configuration, game version, and manual checks. Include screenshots for visible UI changes. Keep local installation paths, game DLLs, and generated `bin/` and `obj/` files out of commits.
