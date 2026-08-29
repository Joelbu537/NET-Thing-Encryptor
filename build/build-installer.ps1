[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$ExpectedVersion,
    [switch]$SingleFile,
    [switch]$SkipInstaller,
    [switch]$SkipTests,
    [switch]$Clean,
    [string]$SignToolPath,
    [string]$CertificatePath = $env:NTE_SIGN_CERTIFICATE_PATH,
    [string]$CertificatePassword = $env:NTE_SIGN_CERTIFICATE_PASSWORD,
    [string]$CertificateThumbprint = $env:NTE_SIGN_CERTIFICATE_THUMBPRINT,
    [string]$TimestampUrl = $env:NTE_SIGN_TIMESTAMP_URL,
    [switch]$RequireSignature
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

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

function Find-SignTool {
    param([string]$ExplicitPath)

    if ($ExplicitPath) {
        if (-not (Test-Path -LiteralPath $ExplicitPath -PathType Leaf)) {
            throw "The specified SignTool path does not exist: $ExplicitPath"
        }
        return (Resolve-Path -LiteralPath $ExplicitPath).Path
    }

    $command = Get-Command "signtool.exe" -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $kitsBin = Join-Path "${env:ProgramFiles(x86)}" "Windows Kits\10\bin"
    if (Test-Path -LiteralPath $kitsBin) {
        $candidate = Get-ChildItem -LiteralPath $kitsBin -Directory -ErrorAction SilentlyContinue |
            Sort-Object Name -Descending |
            ForEach-Object { Join-Path $_.FullName "x64\signtool.exe" } |
            Where-Object { Test-Path -LiteralPath $_ } |
            Select-Object -First 1
        if ($candidate) {
            return $candidate
        }
    }

    return $null
}

function Get-ProjectProperty {
    param(
        [string]$ProjectFile,
        [string]$PropertyName,
        [string]$DefaultValue
    )

    [xml]$project = Get-Content -LiteralPath $ProjectFile
    $propertyNode = $project.SelectSingleNode("/Project/PropertyGroup/$PropertyName")
    if ($propertyNode -and -not [string]::IsNullOrWhiteSpace($propertyNode.InnerText)) {
        return $propertyNode.InnerText.Trim()
    }

    return $DefaultValue
}

function Assert-PathWithinDirectory {
    param(
        [string]$Path,
        [string]$ParentDirectory
    )

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $fullParent = [System.IO.Path]::GetFullPath($ParentDirectory).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar)
    if (-not $fullPath.StartsWith("$fullParent$([System.IO.Path]::DirectorySeparatorChar)", [StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to modify a path outside the artifact directory: $fullPath"
    }
}

function Invoke-DotNet {
    param([string[]]$Arguments)

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

function Get-SigningConfiguration {
    $hasCertificatePath = -not [string]::IsNullOrWhiteSpace($CertificatePath)
    $hasThumbprint = -not [string]::IsNullOrWhiteSpace($CertificateThumbprint)

    if ($hasCertificatePath -and $hasThumbprint) {
        throw "Specify either CertificatePath or CertificateThumbprint, not both."
    }

    if (-not $hasCertificatePath -and -not $hasThumbprint) {
        if ($RequireSignature) {
            throw "A release signature is required, but no certificate was configured."
        }
        return $null
    }

    if ($hasCertificatePath -and -not (Test-Path -LiteralPath $CertificatePath -PathType Leaf)) {
        throw "The signing certificate does not exist: $CertificatePath"
    }

    $tool = Find-SignTool -ExplicitPath $SignToolPath
    if (-not $tool) {
        throw "signtool.exe was not found. Install the Windows SDK or pass -SignToolPath."
    }

    return [pscustomobject]@{
        Tool = $tool
        CertificatePath = $CertificatePath
        CertificatePassword = $CertificatePassword
        CertificateThumbprint = $CertificateThumbprint
    }
}

function Invoke-CodeSigning {
    param(
        [pscustomobject]$Configuration,
        [string]$FilePath
    )

    if (-not $Configuration) {
        Write-Warning "Code signing is not configured; leaving unsigned: $FilePath"
        return
    }

    $arguments = @("sign", "/fd", "SHA256", "/td", "SHA256", "/tr", $TimestampUrl)
    if ($Configuration.CertificatePath) {
        $arguments += @("/f", $Configuration.CertificatePath)
        if (-not [string]::IsNullOrEmpty($Configuration.CertificatePassword)) {
            $arguments += @("/p", $Configuration.CertificatePassword)
        }
    }
    else {
        $arguments += @("/sha1", $Configuration.CertificateThumbprint)
    }
    $arguments += $FilePath

    & $Configuration.Tool @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Code signing failed for $FilePath with exit code $LASTEXITCODE."
    }

    $signature = Get-AuthenticodeSignature -LiteralPath $FilePath
    if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid) {
        throw "Signature verification failed for ${FilePath}: $($signature.StatusMessage)"
    }
}

if ([string]::IsNullOrWhiteSpace($TimestampUrl)) {
    $TimestampUrl = "http://timestamp.digicert.com"
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$solutionFile = Join-Path $repoRoot "NET Thing Encryptor.sln"
$projectFile = Join-Path $repoRoot "NET Thing Encryptor\NET Thing Encryptor.csproj"
$innoScript = Join-Path $repoRoot "installer\NETThingEncryptor.iss"
$iconPath = Join-Path $repoRoot "NET Thing Encryptor\image.ico"
$artifactRoot = Join-Path $repoRoot "artifacts"
$publishDir = Join-Path $artifactRoot "publish\NET Thing Encryptor\$Runtime"
$installerOutputDir = Join-Path $artifactRoot "installer"
$version = Get-ProjectProperty -ProjectFile $projectFile -PropertyName "Version" -DefaultValue "0.0.0"
$fileVersion = Get-ProjectProperty -ProjectFile $projectFile -PropertyName "FileVersion" -DefaultValue "$version.0"
$singleFileValue = if ($SingleFile) { "true" } else { "false" }
$signingConfiguration = Get-SigningConfiguration

if ($Runtime -ne "win-x64") {
    throw "Only the supported installer runtime 'win-x64' can currently be packaged. Found: $Runtime"
}
if ($version -notmatch '^\d+\.\d+\.\d+(\.\d+)?$') {
    throw "Installer versions must contain three or four numeric components. Found: $version"
}
if ($fileVersion -notmatch '^\d+\.\d+\.\d+\.\d+$') {
    throw "FileVersion must contain four numeric components. Found: $fileVersion"
}
if ($ExpectedVersion -and $ExpectedVersion -ne $version) {
    throw "Expected version $ExpectedVersion, but the project declares $version."
}

if ($Clean) {
    foreach ($path in @($publishDir, $installerOutputDir)) {
        Assert-PathWithinDirectory -Path $path -ParentDirectory $artifactRoot
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Recurse -Force
        }
    }
}

New-Item -ItemType Directory -Force -Path $publishDir, $installerOutputDir | Out-Null

Write-Host "Restoring dependencies..."
Invoke-DotNet -Arguments @("restore", $solutionFile)
Invoke-DotNet -Arguments @("restore", $projectFile, "-r", $Runtime)

if (-not $SkipTests) {
    Write-Host "Running release tests..."
    Invoke-DotNet -Arguments @("test", $solutionFile, "-c", $Configuration, "--no-restore")
}

$publishArguments = @(
    "publish",
    $projectFile,
    "-c", $Configuration,
    "-r", $Runtime,
    "--self-contained", "true",
    "--no-restore",
    "-o", $publishDir,
    "/p:PublishSingleFile=$singleFileValue",
    "/p:IncludeNativeLibrariesForSelfExtract=true",
    "/p:EnableCompressionInSingleFile=true",
    "/p:PublishReadyToRun=false"
)

Write-Host "Publishing NET Thing Encryptor $version for $Runtime..."
Invoke-DotNet -Arguments $publishArguments

$libVlcDirectory = Join-Path $publishDir "libvlc"
if (Test-Path -LiteralPath $libVlcDirectory -PathType Container) {
    Get-ChildItem -LiteralPath $libVlcDirectory -Directory |
        Where-Object { $_.Name -like "win-*" -and $_.Name -ne $Runtime } |
        ForEach-Object { Remove-Item -LiteralPath $_.FullName -Recurse -Force }
}

$publishedExecutable = Join-Path $publishDir "NET Thing Encryptor.exe"
$requiredPublishedFiles = @(
    $publishedExecutable,
    (Join-Path $publishDir "LibVLCSharp.dll"),
    (Join-Path $publishDir "Magick.NET.Core.dll")
)
foreach ($requiredFile in $requiredPublishedFiles) {
    if (-not (Test-Path -LiteralPath $requiredFile -PathType Leaf)) {
        throw "The published application is incomplete. Missing: $requiredFile"
    }
}

if (-not $SingleFile) {
    $nativeVlc = Get-ChildItem -LiteralPath $publishDir -Filter "libvlc.dll" -File -Recurse |
        Where-Object { $_.FullName -match 'win-x64' } |
        Select-Object -First 1
    if (-not $nativeVlc) {
        throw "The published application does not contain the native x64 libvlc.dll."
    }
}

$publishedVersion = (Get-Item -LiteralPath $publishedExecutable).VersionInfo.ProductVersion
if (-not $publishedVersion -or -not $publishedVersion.StartsWith($version, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Published executable version '$publishedVersion' does not match project version '$version'."
}

Invoke-CodeSigning -Configuration $signingConfiguration -FilePath $publishedExecutable
Write-Host "Published app: $publishDir"

if ($SkipInstaller) {
    Write-Host "Skipped installer creation."
    return
}

$iscc = Find-InnoSetupCompiler
if (-not $iscc) {
    throw "Inno Setup 6 compiler (ISCC.exe) was not found. Install it with: winget install JRSoftware.InnoSetup"
}

$innoArguments = @(
    $innoScript,
    "/DAppVersion=$version",
    "/DAppFileVersion=$fileVersion",
    "/DSourceDir=$publishDir",
    "/DOutputDir=$installerOutputDir",
    "/DIconPath=$iconPath"
)

Write-Host "Building installer with Inno Setup..."
& $iscc @innoArguments
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup failed with exit code $LASTEXITCODE."
}

$installerPath = Join-Path $installerOutputDir "NET-Thing-Encryptor-Setup-$version.exe"
if (-not (Test-Path -LiteralPath $installerPath -PathType Leaf)) {
    throw "The expected installer was not created: $installerPath"
}

Invoke-CodeSigning -Configuration $signingConfiguration -FilePath $installerPath

$hash = Get-FileHash -LiteralPath $installerPath -Algorithm SHA256
$checksumPath = "$installerPath.sha256"
"$($hash.Hash.ToLowerInvariant()) *$([System.IO.Path]::GetFileName($installerPath))" |
    Set-Content -LiteralPath $checksumPath -Encoding ascii

Write-Host "Installer output: $installerPath"
Write-Host "SHA-256 checksum: $checksumPath"
