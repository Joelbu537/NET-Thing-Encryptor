[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$LegacyInstallerPath,
    [Parameter(Mandatory)]
    [string]$AvaloniaInstallerPath,
    [Parameter(Mandatory)]
    [string]$LegacyExpectedVersion,
    [Parameter(Mandatory)]
    [string]$AvaloniaExpectedVersion,
    [switch]$AllowLocalMachineChanges
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Resolve-TestInstaller {
    param(
        [string]$Path,
        [string]$Label,
        [string]$ExpectedVersion
    )

    $resolved = (Resolve-Path -LiteralPath $Path).Path
    $item = Get-Item -LiteralPath $resolved
    if (-not $item.VersionInfo.ProductVersion.StartsWith($ExpectedVersion, [StringComparison]::OrdinalIgnoreCase)) {
        throw "$Label installer version '$($item.VersionInfo.ProductVersion)' does not match '$ExpectedVersion'."
    }

    return $resolved
}

function Invoke-InstallerProcess {
    param(
        [string]$FilePath,
        [string[]]$Arguments,
        [string]$Description
    )

    $process = Start-Process `
        -FilePath $FilePath `
        -ArgumentList $Arguments `
        -WindowStyle Hidden `
        -Wait `
        -PassThru
    if ($process.ExitCode -ne 0) {
        throw "$Description failed with exit code $($process.ExitCode)."
    }
}

function Get-DirectoryFingerprint {
    param([string]$Path)

    $root = [IO.Path]::GetFullPath($Path).TrimEnd(
        [IO.Path]::DirectorySeparatorChar,
        [IO.Path]::AltDirectorySeparatorChar)
    return @(
        Get-ChildItem -LiteralPath $root -File -Recurse |
            Sort-Object FullName |
            ForEach-Object {
                $relativePath = $_.FullName.Substring($root.Length + 1)
                $hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
                "$relativePath|$($_.Length)|$hash"
            }
    )
}

function Assert-ActiveVlcRuntime {
    param([string]$InstallDirectory)

    $libVlcDirectory = Join-Path $InstallDirectory "libvlc"
    $runtimeDirectory = Join-Path $libVlcDirectory "win-x64"
    foreach ($fileName in @("libvlc.dll", "libvlccore.dll")) {
        $filePath = Join-Path $runtimeDirectory $fileName
        if (-not (Test-Path -LiteralPath $filePath -PathType Leaf)) {
            throw "The upgraded Avalonia installation is missing the native VLC file $filePath."
        }
    }

    $pluginDirectory = Join-Path $runtimeDirectory "plugins"
    foreach ($relativePluginPath in @(
        "audio_output\libwasapi_plugin.dll",
        "codec\libavcodec_plugin.dll",
        "demux\libmkv_plugin.dll",
        "demux\libmp4_plugin.dll",
        "video_output\libdirect3d11_plugin.dll")) {
        $pluginPath = Join-Path $pluginDirectory $relativePluginPath
        if (-not (Test-Path -LiteralPath $pluginPath -PathType Leaf)) {
            throw "The upgraded Avalonia installation is missing the native VLC plugin $pluginPath."
        }
    }

    $unexpectedRuntimes = Get-ChildItem -LiteralPath $libVlcDirectory -Directory |
        Where-Object { $_.Name -like "win-*" -and $_.Name -ne "win-x64" }
    if ($unexpectedRuntimes) {
        throw "The upgraded installation contains VLC runtimes other than win-x64: $($unexpectedRuntimes.Name -join ', ')"
    }
}

$legacyInstaller = Resolve-TestInstaller `
    -Path $LegacyInstallerPath `
    -Label "WinForms rollback" `
    -ExpectedVersion $LegacyExpectedVersion
$avaloniaInstaller = Resolve-TestInstaller `
    -Path $AvaloniaInstallerPath `
    -Label "Avalonia" `
    -ExpectedVersion $AvaloniaExpectedVersion

$isCi = [string]::Equals($env:CI, "true", [StringComparison]::OrdinalIgnoreCase)
if (-not $isCi -and -not $AllowLocalMachineChanges) {
    throw "The upgrade smoke test modifies the current user's uninstall registry. Use -AllowLocalMachineChanges only on an isolated test machine."
}

$uninstallKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{D2AE186D-517D-406F-99D9-8D538AC9607D}_is1"
if (Test-Path -LiteralPath $uninstallKey) {
    throw "NET Thing Encryptor is already registered for the current user. Refusing to replace it during an upgrade smoke test."
}

$userDataDirectory = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) "NET Thing Encryptor"
if (Test-Path -LiteralPath $userDataDirectory) {
    throw "The NET Thing Encryptor user-data directory already exists. Refusing to use this account for a destructive upgrade smoke test."
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$fixtureDirectory = Join-Path $repoRoot "Nte.Core.Tests\Fixtures\baseline-v3.7"
if (-not (Test-Path -LiteralPath $fixtureDirectory -PathType Container)) {
    throw "The M0 compatibility fixture is missing: $fixtureDirectory"
}

$testRoot = Join-Path ([IO.Path]::GetTempPath()) "NETThingEncryptor.UpgradeTests\$([Guid]::NewGuid().ToString('N'))"
$installDirectory = Join-Path $testRoot "application"
$dataDirectory = Join-Path $userDataDirectory "Data"
$installedExecutable = Join-Path $installDirectory "NET Thing Encryptor.exe"
$cleanupUninstaller = $null

try {
    New-Item -ItemType Directory -Force -Path $installDirectory, $dataDirectory | Out-Null
    Copy-Item -Path (Join-Path $fixtureDirectory "*") -Destination $dataDirectory -Recurse
    $fixtureFingerprint = Get-DirectoryFingerprint -Path $dataDirectory

    $legacyLog = Join-Path $testRoot "legacy-install.log"
    $installArguments = @(
        "/VERYSILENT",
        "/SUPPRESSMSGBOXES",
        "/NORESTART",
        "/SP-",
        "/NOICONS",
        "/LANG=english",
        "/DIR=`"$installDirectory`"",
        "/LOG=`"$legacyLog`""
    )
    Invoke-InstallerProcess `
        -FilePath $legacyInstaller `
        -Arguments $installArguments `
        -Description "WinForms rollback installation"

    if (-not (Test-Path -LiteralPath $installedExecutable -PathType Leaf)) {
        throw "The WinForms executable is missing after the legacy installation."
    }
    $legacyInstalledVersion = (Get-Item -LiteralPath $installedExecutable).VersionInfo.ProductVersion
    if (-not $legacyInstalledVersion.StartsWith($LegacyExpectedVersion, [StringComparison]::OrdinalIgnoreCase)) {
        throw "The installed WinForms version '$legacyInstalledVersion' does not match '$LegacyExpectedVersion'."
    }
    if (-not (Get-ChildItem -LiteralPath $installDirectory -Filter "libvlc.dll" -File -Recurse | Select-Object -First 1)) {
        throw "The WinForms rollback installation is missing native VLC files."
    }
    if (-not (Get-ChildItem -LiteralPath $installDirectory -Filter "Magick.NET.Core.dll" -File -Recurse | Select-Object -First 1)) {
        throw "The WinForms rollback installation is missing ImageMagick files."
    }
    $obsoleteVlcSentinel = Join-Path $installDirectory `
        "libvlc\win-x64\plugins\obsolete-upgrade-runtime.sentinel"
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $obsoleteVlcSentinel) |
        Out-Null
    Set-Content -LiteralPath $obsoleteVlcSentinel -Value "must be removed during upgrade" -Encoding ascii
    $cleanupUninstaller = Get-ChildItem -LiteralPath $installDirectory -Filter "unins*.exe" -File |
        Select-Object -First 1
    if (-not $cleanupUninstaller) {
        throw "The WinForms rollback installation does not contain an uninstaller."
    }

    $avaloniaLog = Join-Path $testRoot "avalonia-upgrade.log"
    $upgradeArguments = @(
        "/VERYSILENT",
        "/SUPPRESSMSGBOXES",
        "/NORESTART",
        "/SP-",
        "/NOICONS",
        "/LANG=english",
        "/DIR=`"$installDirectory`"",
        "/LOG=`"$avaloniaLog`""
    )
    Invoke-InstallerProcess `
        -FilePath $avaloniaInstaller `
        -Arguments $upgradeArguments `
        -Description "Avalonia in-place upgrade"

    foreach ($requiredFile in @(
        "Avalonia.dll",
        "LibVLCSharp.dll",
        "LibVLCSharp.Avalonia.dll",
        "Nte.App.dll",
        "Nte.Core.dll",
        "Nte.Storage.dll")) {
        if (-not (Test-Path -LiteralPath (Join-Path $installDirectory $requiredFile) -PathType Leaf)) {
            throw "The upgraded installation is missing $requiredFile."
        }
    }
    Assert-ActiveVlcRuntime -InstallDirectory $installDirectory
    if (Test-Path -LiteralPath $obsoleteVlcSentinel -PathType Leaf) {
        throw "The Avalonia upgrade did not replace the legacy VLC runtime directory."
    }
    if (Get-ChildItem -LiteralPath $installDirectory -File |
        Where-Object { $_.Name -eq "LibVLCSharp.WinForms.dll" -or $_.Name -like "Magick*.dll" }) {
        throw "The Avalonia upgrade left retired WinForms media assemblies behind."
    }

    $upgradedVersion = (Get-Item -LiteralPath $installedExecutable).VersionInfo.ProductVersion
    if (-not $upgradedVersion.StartsWith($AvaloniaExpectedVersion, [StringComparison]::OrdinalIgnoreCase)) {
        throw "The upgraded executable version '$upgradedVersion' does not match '$AvaloniaExpectedVersion'."
    }
    if (Compare-Object $fixtureFingerprint (Get-DirectoryFingerprint -Path $dataDirectory)) {
        throw "The in-place upgrade changed the M0 compatibility fixture."
    }

    $probeOutput = Join-Path $testRoot "startup-probe.out.log"
    $probeError = Join-Path $testRoot "startup-probe.err.log"
    $probe = Start-Process `
        -FilePath $installedExecutable `
        -ArgumentList "--startup-probe" `
        -WorkingDirectory $installDirectory `
        -RedirectStandardOutput $probeOutput `
        -RedirectStandardError $probeError `
        -Environment @{ NTE_DATA_DIRECTORY = Join-Path $testRoot "probe-data" } `
        -WindowStyle Hidden `
        -Wait `
        -PassThru
    if ($probe.ExitCode -ne 0) {
        throw "The upgraded Avalonia application failed its startup probe with exit code $($probe.ExitCode)."
    }

    $cleanupUninstaller = Get-ChildItem -LiteralPath $installDirectory -Filter "unins*.exe" -File |
        Select-Object -First 1
    if (-not $cleanupUninstaller) {
        throw "The upgraded installation does not contain an uninstaller."
    }
    $uninstallLog = Join-Path $testRoot "uninstall.log"
    Invoke-InstallerProcess `
        -FilePath $cleanupUninstaller.FullName `
        -Arguments @("/VERYSILENT", "/SUPPRESSMSGBOXES", "/NORESTART", "/LOG=`"$uninstallLog`"") `
        -Description "Avalonia uninstall"
    $cleanupUninstaller = $null

    if (Test-Path -LiteralPath $installedExecutable) {
        throw "The application executable remains after uninstalling the upgraded installation."
    }
    if (Compare-Object $fixtureFingerprint (Get-DirectoryFingerprint -Path $dataDirectory)) {
        throw "Uninstalling the upgraded application changed the M0 compatibility fixture."
    }

    Write-Host "WinForms-to-Avalonia in-place upgrade passed; the compatibility fixture survived upgrade and uninstall unchanged."
}
finally {
    if (-not $cleanupUninstaller -and (Test-Path -LiteralPath $installDirectory -PathType Container)) {
        $cleanupUninstaller = Get-ChildItem -LiteralPath $installDirectory -Filter "unins*.exe" -File |
            Select-Object -First 1
    }
    if ($cleanupUninstaller -and (Test-Path -LiteralPath $cleanupUninstaller.FullName -PathType Leaf)) {
        $cleanupProcess = Start-Process `
            -FilePath $cleanupUninstaller.FullName `
            -ArgumentList @("/VERYSILENT", "/SUPPRESSMSGBOXES", "/NORESTART") `
            -WindowStyle Hidden `
            -Wait `
            -PassThru
        if ($cleanupProcess.ExitCode -ne 0) {
            Write-Warning "Cleanup uninstall failed with exit code $($cleanupProcess.ExitCode)."
        }
    }

    foreach ($path in @($testRoot, $userDataDirectory)) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Recurse -Force
        }
    }
}
