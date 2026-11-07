#Requires -Version 7.0
param(
    [Parameter(Mandatory=$true)][string]$ProjectPath,
    [Parameter(Mandatory=$true)][string]$DisplayVersion,
    [Parameter(Mandatory=$true)][long]$BuildNumber
)
$ErrorActionPreference='Stop'
if ($DisplayVersion -notmatch '^(0|[1-9][0-9]{0,4})\.(0|[1-9][0-9]{0,4})\.(0|[1-9][0-9]{0,4})$' -or
    @($DisplayVersion.Split('.') | Where-Object { [int]$_ -gt 65535 }).Count -gt 0) {
    throw 'DisplayVersion must have three numeric components from 0 to 65535, without leading zeroes or suffixes.'
}
if ($BuildNumber -lt 1 -or $BuildNumber -gt 2100000000) { throw 'BuildNumber is outside the supported demonstration range.' }
$resolvedProject=(Resolve-Path -LiteralPath $ProjectPath).Path
if ([IO.Path]::GetExtension($resolvedProject) -ne '.csproj') { throw 'Expected a csproj file.' }
$document=[xml]::new()
$document.PreserveWhitespace=$true
$document.Load($resolvedProject)
$displayNodes=$document.SelectNodes('/Project/PropertyGroup/ApplicationDisplayVersion')
$buildNodes=$document.SelectNodes('/Project/PropertyGroup/ApplicationVersion')
if ($displayNodes.Count -ne 1 -or $buildNodes.Count -ne 1) { throw 'Expected exactly one display version and build version.' }
$oldNumber=0L
if (-not [long]::TryParse($buildNodes[0].InnerText,[ref]$oldNumber)) { throw 'Existing build number is not an integer.' }
if ($BuildNumber -lt $oldNumber) { throw 'Build number must not decrease.' }
# Change only the two values: every other character of the project file stays as it was.
$text=[IO.File]::ReadAllText($resolvedProject)
$hasBom=[IO.File]::ReadAllBytes($resolvedProject)[0] -eq 0xEF
$displayPattern='(?<=<ApplicationDisplayVersion>)[^<]*(?=</ApplicationDisplayVersion>)'
$buildPattern='(?<=<ApplicationVersion>)[^<]*(?=</ApplicationVersion>)'
if ([regex]::Matches($text,$displayPattern).Count -ne 1 -or [regex]::Matches($text,$buildPattern).Count -ne 1) { throw 'Expected exactly one display version and build version.' }
$text=[regex]::Replace($text,$displayPattern,$DisplayVersion)
$text=[regex]::Replace($text,$buildPattern,$BuildNumber.ToString([Globalization.CultureInfo]::InvariantCulture))
$temporary=Join-Path ([IO.Path]::GetDirectoryName($resolvedProject)) ([IO.Path]::GetRandomFileName())
try {
    [IO.File]::WriteAllText($temporary,$text,[Text.UTF8Encoding]::new($hasBom))
    [IO.File]::Move($temporary,$resolvedProject,$true)
} finally {
    if ([IO.File]::Exists($temporary)) { [IO.File]::Delete($temporary) }
}
