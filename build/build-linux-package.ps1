[CmdletBinding()]
param(
    [string]$ExpectedVersion,
    [string]$OutputDirectory,
    [switch]$Clean
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if (-not $IsLinux) {
    throw 'Linux packages must be built on Linux so permissions and package metadata can be verified.'
}

$repositoryRoot = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $repositoryRoot 'artifacts/release/desktop/linux-x64'
} elseif (-not [IO.Path]::IsPathRooted($OutputDirectory)) {
    $OutputDirectory = Join-Path $repositoryRoot $OutputDirectory
}

$projectPath = Join-Path $repositoryRoot 'Nte.Desktop/Nte.Desktop.csproj'
$version = (& dotnet msbuild $projectPath -getProperty:Version).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($version)) {
    throw 'Could not read the desktop product version.'
}
if ($ExpectedVersion -and $version -ne $ExpectedVersion) {
    throw "Expected version '$ExpectedVersion', but the desktop project contains '$version'."
}

$outputRoot = [IO.Path]::GetFullPath($OutputDirectory)
$artifactsRoot = [IO.Path]::GetFullPath((Join-Path $repositoryRoot 'artifacts'))
if (-not $outputRoot.StartsWith($artifactsRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::Ordinal)) {
    throw "Output directory must be below '$artifactsRoot'."
}

$stagingRoot = Join-Path $artifactsRoot 'staging/desktop/linux-x64'
$publishDirectory = Join-Path $stagingRoot 'publish'
$stagingDirectory = Join-Path $stagingRoot 'package/deb'
$portableDirectory = Join-Path $stagingRoot "package/NET-Thing-Encryptor-$version-linux-x64"

if ($Clean -and (Test-Path -LiteralPath $outputRoot)) {
    Remove-Item -LiteralPath $outputRoot -Recurse -Force
}
if (Test-Path -LiteralPath $stagingRoot) {
    Remove-Item -LiteralPath $stagingRoot -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $publishDirectory, $outputRoot | Out-Null

& dotnet publish $projectPath -c Release -r linux-x64 --self-contained true -o $publishDirectory --nologo
if ($LASTEXITCODE -ne 0) {
    throw "Linux publish failed with exit code $LASTEXITCODE."
}

$applicationBinary = Join-Path $publishDirectory 'NET Thing Encryptor'
if (-not (Test-Path -LiteralPath $applicationBinary -PathType Leaf)) {
    throw "Linux application host is missing: $applicationBinary"
}
$windowsRuntime = Get-ChildItem -LiteralPath $publishDirectory -Recurse -File |
    Where-Object { $_.FullName -match '[\\/]libvlc[\\/]win-' } |
    Select-Object -First 1
if ($windowsRuntime) {
    throw "The Linux publish contains a Windows LibVLC runtime: $($windowsRuntime.FullName)"
}

New-Item -ItemType Directory -Force -Path `
    (Join-Path $stagingDirectory 'DEBIAN'), `
    (Join-Path $stagingDirectory 'opt/net-thing-encryptor'), `
    (Join-Path $stagingDirectory 'usr/bin'), `
    (Join-Path $stagingDirectory 'usr/share/applications'), `
    (Join-Path $stagingDirectory 'usr/share/icons/hicolor/256x256/apps') | Out-Null
Copy-Item -Path (Join-Path $publishDirectory '*') -Destination (Join-Path $stagingDirectory 'opt/net-thing-encryptor') -Recurse -Force
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'deploy/linux/net-thing-encryptor') -Destination (Join-Path $stagingDirectory 'usr/bin/net-thing-encryptor')
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'deploy/linux/net-thing-encryptor.desktop') -Destination (Join-Path $stagingDirectory 'usr/share/applications/net-thing-encryptor.desktop')
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'Nte.App/Assets/AppIcon.png') -Destination (Join-Path $stagingDirectory 'usr/share/icons/hicolor/256x256/apps/net-thing-encryptor.png')

$installedSize = [Math]::Ceiling(((Get-ChildItem -LiteralPath (Join-Path $stagingDirectory 'opt') -Recurse -File | Measure-Object Length -Sum).Sum) / 1KB)
$control = @"
Package: net-thing-encryptor
Version: $version
Section: utils
Priority: optional
Architecture: amd64
Installed-Size: $installedSize
Maintainer: NET Thing Encryptor contributors <noreply@github.com>
Depends: libvlc5, libvlc-dev, vlc-plugin-base, vlc-plugin-video-output, libx11-6, libice6, libsm6, libfontconfig1
Description: Encrypted file vault
 NET Thing Encryptor manages an encrypted local or remote file vault.
"@
[IO.File]::WriteAllText((Join-Path $stagingDirectory 'DEBIAN/control'), $control + "`n", [Text.UTF8Encoding]::new($false))

& chmod 0755 $applicationBinary
& chmod 0755 (Join-Path $stagingDirectory 'opt/net-thing-encryptor/NET Thing Encryptor')
& chmod 0755 (Join-Path $stagingDirectory 'usr/bin/net-thing-encryptor')
if ($LASTEXITCODE -ne 0) {
    throw 'Could not set Linux executable permissions.'
}

$debPath = Join-Path $outputRoot "NET-Thing-Encryptor-$version-linux-x64.deb"
& dpkg-deb --build --root-owner-group $stagingDirectory $debPath
if ($LASTEXITCODE -ne 0) {
    throw "Debian package creation failed with exit code $LASTEXITCODE."
}

New-Item -ItemType Directory -Force -Path $portableDirectory | Out-Null
Copy-Item -Path (Join-Path $publishDirectory '*') -Destination $portableDirectory -Recurse -Force
Copy-Item -LiteralPath (Join-Path $repositoryRoot 'deploy/linux/README-linux.txt') -Destination $portableDirectory
& chmod 0755 (Join-Path $portableDirectory 'NET Thing Encryptor')
if ($LASTEXITCODE -ne 0) {
    throw 'Could not set the portable Linux executable permission.'
}
$archivePath = Join-Path $outputRoot "NET-Thing-Encryptor-$version-linux-x64.tar.gz"
& tar -C (Split-Path -Parent $portableDirectory) -czf $archivePath (Split-Path -Leaf $portableDirectory)
if ($LASTEXITCODE -ne 0) {
    throw "Portable archive creation failed with exit code $LASTEXITCODE."
}

foreach ($artifact in @($debPath, $archivePath)) {
    $hash = (Get-FileHash -LiteralPath $artifact -Algorithm SHA256).Hash.ToLowerInvariant()
    $hashLine = "$hash *$([IO.Path]::GetFileName($artifact))`n"
    [IO.File]::WriteAllText("$artifact.sha256", $hashLine, [Text.Encoding]::ASCII)
}

Write-Host "Linux packages created in '$outputRoot'."
