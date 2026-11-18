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
foreach ($name in @('README.md','LICENSE','THIRD_PARTY_NOTICES.md')) {
    Copy-Item -LiteralPath (Join-Path $repo $name) -Destination $package
}
@{ GameVersion = $GameVersion; Support = $tier; LegatoPackage = '0.1.1'; SourceApi = '1.40.8'; Configuration = $Configuration } |
    ConvertTo-Json | Set-Content (Join-Path $package 'BUILD-INFO.json')
# Ship the exact corresponding source alongside the binary. Never include game DLLs or local paths.
$source = Join-Path $stage 'source'
New-Item -ItemType Directory -Force $source | Out-Null
foreach ($name in @('AdvancedPractice.csproj','Directory.Build.props','Plugin.cs','AdvancedPracticeController.cs','manifest.json','README.md','AGENTS.md','LICENSE','THIRD_PARTY_NOTICES.md','.gitignore')) {
    Copy-Item -LiteralPath (Join-Path $repo $name) -Destination $source
}
foreach ($directory in @('Build','Compatibility','Configuration','Models','Views','Properties','Tests')) {
    $base = Join-Path $repo $directory
    foreach ($file in Get-ChildItem -LiteralPath $base -File -Recurse) {
        $relative = [IO.Path]::GetRelativePath($repo, $file.FullName)
        if ($relative -match '[\\/](bin|obj)[\\/]') { continue }
        $destination = Join-Path $source $relative
        New-Item -ItemType Directory -Force (Split-Path $destination -Parent) | Out-Null
        Copy-Item -LiteralPath $file.FullName -Destination $destination
    }
}
$assets = Get-Content -Raw (Join-Path $repo 'obj/project.assets.json') | ConvertFrom-Json
$legatoBuild = $null
foreach ($folder in $assets.packageFolders.PSObject.Properties.Name) {
    $candidate = Join-Path $folder 'legatobs/0.1.1/build'
    if (Test-Path -LiteralPath $candidate) { $legatoBuild = $candidate; break }
}
if (!$legatoBuild) { throw 'The exact Legato source package is required for corresponding-source.zip. Restore packages first.' }
$thirdParty = Join-Path $source 'ThirdParty/Legato'
New-Item -ItemType Directory -Force $thirdParty | Out-Null
Copy-Item -LiteralPath $legatoBuild -Destination $thirdParty -Recurse
Compress-Archive -Path (Join-Path $source '*') -DestinationPath (Join-Path $package 'corresponding-source.zip') -Force
$zip = Join-Path $artifacts "$packageName.zip"
Compress-Archive -Path (Join-Path $package '*') -DestinationPath $zip -Force
Write-Host "Package: $zip"
Write-Host "Support: $tier. No game files were installed or modified."
