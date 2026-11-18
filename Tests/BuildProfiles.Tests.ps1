param()
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$profiles = @{
    '1.29.0' = @('1.29.0','provisional','^1.6.0')
    '1.37.1' = @('1.37.1','provisional','^1.11.0')
    '1.38.0' = @('1.38.0','provisional','^1.12.0')
    '1.40.8' = @('1.40.0','build-verified','^1.12.0')
    '1.44.1' = @('1.42.0','build-verified','^1.14.1')
}
foreach ($version in $profiles.Keys | Sort-Object) {
    $json = & dotnet msbuild (Join-Path $repo 'AdvancedPractice.csproj') -nologo -verbosity:quiet -target:LegatoConfigure "/p:LegatoTargetVersion=$version" '-getProperty:LegatoTargetProfile,LegatoSourceProfile,OutputPath,IntermediateOutputPath,BsmlRequirement,SupportTier'
    if ($LASTEXITCODE -ne 0) { throw "Legato configuration failed for $version." }
    $properties = ($json -join "`n" | ConvertFrom-Json).Properties
    $expected = $profiles[$version]
    if ($properties.LegatoTargetProfile -ne $expected[0] -or $properties.LegatoSourceProfile -ne '1.40.0') { throw "Incorrect Legato profiles for $version." }
    if ($properties.SupportTier -ne $expected[1] -or $properties.BsmlRequirement -ne $expected[2]) { throw "Incorrect support metadata for $version." }
    if ($properties.OutputPath -notlike "*$version*" -or $properties.IntermediateOutputPath -notlike "*$version*") { throw "Build outputs are not isolated for $version." }
    Write-Host "PASS $version -> Legato $($properties.LegatoTargetProfile) ($($properties.SupportTier))"
}
Write-Host 'Five build profiles verified. These configuration checks do not compile or run the provisional targets.'
