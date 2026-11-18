param(
    [Parameter(Mandatory)][string]$GameDirectory,
    [Parameter(Mandatory)][ValidateSet('1.29.0','1.37.1','1.38.0','1.40.8','1.44.1')][string]$GameVersion,
    [string]$PluginPath
)
$ErrorActionPreference = 'Stop'
$cecilPath = Join-Path $GameDirectory 'Libs/Mono.Cecil.dll'
if (!(Test-Path -LiteralPath $cecilPath)) { throw "Missing BSIPA metadata reader: $cecilPath" }
Add-Type -Path $cecilPath
$main = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $GameDirectory 'Beat Saber_Data/Managed/Main.dll'))
try {
    $view = $main.MainModule.Types | Where-Object Name -EQ 'PracticeViewController'
    if (!$view) { throw 'PracticeViewController was not found.' }
    $expectedFields = @('_songStartSlider','_speedSlider','_playButton','_maxStartSongTime','_playerDataModel')
    if ($GameVersion -eq '1.29.0') { $expectedFields += '_difficultyBeatmap' }
    else { $expectedFields += @('_beatmapKey','_beatmapLevel','_beatmapLevelsModel','_beatmapLevelsEntitlementModel') }
    foreach ($name in $expectedFields) {
        if (!($view.Fields | Where-Object Name -EQ $name)) { throw "PracticeViewController.$name is unavailable." }
    }
    foreach ($name in @('DidActivate','HandlePlayButtonPressed')) {
        if (!($view.Methods | Where-Object Name -EQ $name)) { throw "Harmony target PracticeViewController.$name is unavailable." }
    }
    $installer = $main.MainModule.Types | Where-Object Name -EQ 'GameplayCoreInstaller'
    if (!($installer.Methods | Where-Object Name -EQ 'InstallBindings') -or !($installer.Fields | Where-Object Name -EQ '_sceneSetupData')) {
        throw 'GameplayCoreInstaller patch contract changed.'
    }
    $spawn = $main.MainModule.Types | Where-Object Name -EQ 'BeatmapObjectSpawnController'
    $init = $spawn.NestedTypes | Where-Object Name -EQ 'InitData'
    $constructors = @($init.Methods | Where-Object { $_.IsConstructor -and ($_.Parameters | Where-Object { $_.Name -eq 'noteJumpMovementSpeed' -and $_.ParameterType.FullName -eq 'System.Single' }) })
    if ($constructors.Count -ne 1) { throw 'The NJS constructor patch needs exactly one matching constructor.' }
    Write-Host "Game API contracts verified for $GameVersion."
} finally { $main.Dispose() }
if ($PluginPath) {
    $plugin = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($PluginPath)
    try {
        $resource = $plugin.MainModule.Resources | Where-Object Name -EQ 'AdvancedPractice.manifest.json'
        if (!$resource) { throw 'Embedded plugin manifest is missing.' }
        $manifest = [Text.Encoding]::UTF8.GetString($resource.GetResourceData()).TrimStart([char]0xFEFF) | ConvertFrom-Json
        if ($manifest.gameVersion -ne $GameVersion) { throw "Manifest targets $($manifest.gameVersion), expected $GameVersion." }
        if ($plugin.MainModule.AssemblyReferences | Where-Object Name -EQ 'LegatoBS') { throw 'Legato must be source-included, not a runtime DLL dependency.' }
        Write-Host "Embedded manifest and source-included Legato verified for $GameVersion."
    } finally { $plugin.Dispose() }
}
