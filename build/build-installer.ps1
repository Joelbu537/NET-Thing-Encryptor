param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$SingleFile,
    [switch]$SkipInstaller,
    [switch]$Clean
)

$ErrorActionPreference = "Stop"

function Find-InnoSetupCompiler {
    $command = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $candidatePaths = @(
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
    )

    foreach ($candidatePath in $candidatePaths) {
        if ($candidatePath -and (Test-Path -LiteralPath $candidatePath)) {
            return $candidatePath
        }
    }

    return $null
}

function Get-ProjectVersion {
    param([string]$ProjectFile)

    [xml]$project = Get-Content -LiteralPath $ProjectFile
    $versionNode = $project.SelectSingleNode("/Project/PropertyGroup/Version")
    if ($versionNode -and -not [string]::IsNullOrWhiteSpace($versionNode.InnerText)) {
        return $versionNode.InnerText.Trim()
    }

    return "0.0.0"
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectFile = Join-Path $repoRoot "NET Thing Encryptor\NET Thing Encryptor.csproj"
$innoScript = Join-Path $repoRoot "installer\NETThingEncryptor.iss"
$iconPath = Join-Path $repoRoot "NET Thing Encryptor\image.ico"
$publishDir = Join-Path $repoRoot "artifacts\publish\NET Thing Encryptor\$Runtime"
$installerOutputDir = Join-Path $repoRoot "artifacts\installer"
$version = Get-ProjectVersion -ProjectFile $projectFile
$singleFileValue = if ($SingleFile) { "true" } else { "false" }

if ($Clean) {
    foreach ($path in @($publishDir, $installerOutputDir)) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Recurse -Force
        }
    }
}

New-Item -ItemType Directory -Force -Path $publishDir, $installerOutputDir | Out-Null

$publishArgs = @(
    "publish",
    $projectFile,
    "-c", $Configuration,
    "-r", $Runtime,
    "--self-contained", "true",
    "-o", $publishDir,
    "/p:PublishSingleFile=$singleFileValue",
    "/p:IncludeNativeLibrariesForSelfExtract=true",
    "/p:EnableCompressionInSingleFile=true",
    "/p:PublishReadyToRun=false"
)

Write-Host "Publishing NET Thing Encryptor $version for $Runtime..."
& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "Published app: $publishDir"

if ($SkipInstaller) {
    Write-Host "Skipped installer creation."
    return
}

$iscc = Find-InnoSetupCompiler
if (-not $iscc) {
    throw "Inno Setup 6 compiler (ISCC.exe) was not found. Install it with: winget install JRSoftware.InnoSetup"
}

$innoArgs = @(
    $innoScript,
    "/DAppVersion=$version",
    "/DSourceDir=$publishDir",
    "/DOutputDir=$installerOutputDir",
    "/DIconPath=$iconPath"
)

Write-Host "Building installer with Inno Setup..."
& $iscc @innoArgs
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host "Installer output: $installerOutputDir"
