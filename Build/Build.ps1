param(
    [ValidateSet('1.44.1','1.40.8','1.29.0','1.37.1','1.38.0')][string]$GameVersion = '1.44.1',
    [Parameter(Mandatory)][string]$GameDirectory,
    [string]$ModReferencesDirectory,
    [ValidateSet('Release','Debug')][string]$Configuration = 'Release',
    [switch]$NoRestore
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$GameDirectory = (Resolve-Path -LiteralPath $GameDirectory).Path
if (!$ModReferencesDirectory) { $ModReferencesDirectory = Join-Path $GameDirectory 'Plugins' }
$ModReferencesDirectory = (Resolve-Path -LiteralPath $ModReferencesDirectory).Path
$versionFile = Join-Path $GameDirectory 'BeatSaberVersion.txt'
if (Test-Path -LiteralPath $versionFile) {
    $installed = (Get-Content -Raw -LiteralPath $versionFile).Trim().Split('_')[0]
    if ($installed -ne $GameVersion) { throw "References are for $installed, requested $GameVersion." }
}
& "$PSScriptRoot/Verify-GameApi.ps1" -GameDirectory $GameDirectory -GameVersion $GameVersion
$buildArgs = @('build', (Join-Path $repo 'AdvancedPractice.csproj'), '-c', $Configuration,
    "/p:LegatoTargetVersion=$GameVersion", "/p:BeatSaberDir=$GameDirectory", "/p:ModReferencesDir=$ModReferencesDirectory")
if ($NoRestore) { $buildArgs += '--no-restore' }
& dotnet @buildArgs
if ($LASTEXITCODE -ne 0) { throw "Build failed for $GameVersion." }
$dll = Join-Path $repo "bin/$Configuration/$GameVersion/AdvancedPractice.dll"
$pdb = [IO.Path]::ChangeExtension($dll, '.pdb')
if (!(Test-Path -LiteralPath $pdb)) { throw "Debug symbols are missing: $pdb" }
& "$PSScriptRoot/Verify-GameApi.ps1" -GameDirectory $GameDirectory -GameVersion $GameVersion -PluginPath $dll
$version = (Get-Content -Raw (Join-Path $repo 'manifest.json') | ConvertFrom-Json).version
$tier = if ($GameVersion -in @('1.44.1','1.40.8')) { 'build-verified; VR unverified' } else { 'provisional; VR unverified' }
$packageName = "AdvancedPractice-$version-bs$GameVersion"
if ($GameVersion -notin @('1.44.1','1.40.8')) { $packageName += '-preview' }
$artifacts = Join-Path $repo 'artifacts'
$stage = Join-Path $artifacts ('.staging/' + [Guid]::NewGuid().ToString('N'))
$package = Join-Path $stage $packageName
New-Item -ItemType Directory -Force (Join-Path $package 'Plugins') | Out-Null
Copy-Item -LiteralPath $dll -Destination (Join-Path $package 'Plugins/AdvancedPractice.dll')
Copy-Item -LiteralPath $pdb -Destination (Join-Path $package 'Plugins/AdvancedPractice.pdb')
$zip = Join-Path $artifacts "$packageName.zip"
Compress-Archive -Path (Join-Path $package '*') -DestinationPath $zip -Force
Write-Host "Package: $zip"
Write-Host "Support: $tier. No game files were installed or modified."
