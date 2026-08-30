[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$InstallerPath,
    [Parameter(Mandatory)]
    [string]$ExpectedVersion,
    [switch]$RunInstallation,
    [switch]$AllowLocalMachineChanges,
    [switch]$RequireSignature
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$resolvedInstaller = (Resolve-Path -LiteralPath $InstallerPath).Path
$installerItem = Get-Item -LiteralPath $resolvedInstaller
if (-not $installerItem.VersionInfo.ProductVersion.StartsWith($ExpectedVersion, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Installer version '$($installerItem.VersionInfo.ProductVersion)' does not match '$ExpectedVersion'."
}

$checksumPath = "$resolvedInstaller.sha256"
if (-not (Test-Path -LiteralPath $checksumPath -PathType Leaf)) {
    throw "Checksum file is missing: $checksumPath"
}
$expectedHash = ((Get-Content -LiteralPath $checksumPath -Raw).Trim() -split '\s+')[0]
$actualHash = (Get-FileHash -LiteralPath $resolvedInstaller -Algorithm SHA256).Hash
if ($expectedHash -ne $actualHash) {
    throw "Installer SHA-256 checksum verification failed."
}

$signature = Get-AuthenticodeSignature -LiteralPath $resolvedInstaller
if ($RequireSignature -and $signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid) {
    throw "Installer signature verification failed: $($signature.StatusMessage)"
}

Write-Host "Static installer checks passed for $($installerItem.Name)."
if (-not $RunInstallation) {
    return
}

if (-not $env:CI -and -not $AllowLocalMachineChanges) {
    throw "Installation smoke tests modify the current user's uninstall registry. Use -AllowLocalMachineChanges only on an isolated test machine."
}

$uninstallKey = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{D2AE186D-517D-406F-99D9-8D538AC9607D}_is1"
if (Test-Path -LiteralPath $uninstallKey) {
    throw "NET Thing Encryptor is already registered for the current user. Refusing to replace it during a smoke test."
}

$userDataDirectory = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) "NET Thing Encryptor"
if (Test-Path -LiteralPath $userDataDirectory) {
    throw "The NET Thing Encryptor user-data directory already exists. Refusing to use this account for a destructive smoke test."
}

$testRoot = Join-Path ([System.IO.Path]::GetTempPath()) "NETThingEncryptor.InstallerTests\$([Guid]::NewGuid().ToString('N'))"
$sentinelPath = Join-Path $userDataDirectory "Data\installer-smoke-test.sentinel"

try {
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $sentinelPath) | Out-Null
    Set-Content -LiteralPath $sentinelPath -Value "must survive uninstall" -Encoding ascii

    foreach ($language in @("english", "german")) {
        $installDirectory = Join-Path $testRoot $language
        New-Item -ItemType Directory -Force -Path $installDirectory | Out-Null
        $setupLog = Join-Path $testRoot "setup-$language.log"
        $installArguments = @(
            "/VERYSILENT",
            "/SUPPRESSMSGBOXES",
            "/NORESTART",
            "/SP-",
            "/NOICONS",
            "/LANG=$language",
            "/DIR=`"$installDirectory`"",
            "/LOG=`"$setupLog`""
        )
        $installProcess = Start-Process -FilePath $resolvedInstaller -ArgumentList $installArguments -Wait -PassThru
        if ($installProcess.ExitCode -ne 0) {
            throw "Silent $language installation failed with exit code $($installProcess.ExitCode). See $setupLog"
        }

        $installedExecutable = Join-Path $installDirectory "NET Thing Encryptor.exe"
        if (-not (Test-Path -LiteralPath $installedExecutable -PathType Leaf)) {
            throw "Installed executable is missing after the $language installation."
        }
        $installedVersion = (Get-Item -LiteralPath $installedExecutable).VersionInfo.ProductVersion
        if (-not $installedVersion.StartsWith($ExpectedVersion, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Installed executable version '$installedVersion' does not match '$ExpectedVersion'."
        }

        foreach ($requiredFile in @("Avalonia.dll", "Nte.App.dll", "Nte.Core.dll", "Nte.Storage.dll")) {
            if (-not (Test-Path -LiteralPath (Join-Path $installDirectory $requiredFile) -PathType Leaf)) {
                throw "The $language installation is missing $requiredFile."
            }
        }

        $probeOutputLog = Join-Path $testRoot "startup-probe-$language.out.log"
        $probeErrorLog = Join-Path $testRoot "startup-probe-$language.err.log"
        $probeEnvironment = @{ NTE_DATA_DIRECTORY = Join-Path $testRoot "probe-data-$language" }
        $probeProcess = Start-Process `
            -FilePath $installedExecutable `
            -ArgumentList "--startup-probe" `
            -WorkingDirectory $installDirectory `
            -RedirectStandardOutput $probeOutputLog `
            -RedirectStandardError $probeErrorLog `
            -Environment $probeEnvironment `
            -WindowStyle Hidden `
            -Wait `
            -PassThru
        if ($probeProcess.ExitCode -ne 0) {
            throw "The installed $language application failed its startup probe with exit code $($probeProcess.ExitCode). See $probeOutputLog and $probeErrorLog"
        }

        $uninstaller = Get-ChildItem -LiteralPath $installDirectory -Filter "unins*.exe" -File |
            Select-Object -First 1
        if (-not $uninstaller) {
            throw "Uninstaller is missing after the $language installation."
        }
        $uninstallLog = Join-Path $testRoot "uninstall-$language.log"
        $uninstallArguments = @(
            "/VERYSILENT",
            "/SUPPRESSMSGBOXES",
            "/NORESTART",
            "/LOG=`"$uninstallLog`""
        )
        $uninstallProcess = Start-Process -FilePath $uninstaller.FullName -ArgumentList $uninstallArguments -Wait -PassThru
        if ($uninstallProcess.ExitCode -ne 0) {
            throw "Silent $language uninstall failed with exit code $($uninstallProcess.ExitCode). See $uninstallLog"
        }
        if (Test-Path -LiteralPath $installedExecutable) {
            throw "The application executable remains after the $language uninstall."
        }
        if (-not (Test-Path -LiteralPath $sentinelPath -PathType Leaf)) {
            throw "Uninstall removed user data during the $language smoke test."
        }
    }

    Write-Host "English and German install/uninstall smoke tests passed; user data was preserved."
}
finally {
    foreach ($path in @($testRoot, $userDataDirectory)) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Recurse -Force
        }
    }
}
