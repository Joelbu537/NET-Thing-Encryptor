# NET Thing Encryptor

NET Thing Encryptor ist eine Anwendung zum verschlüsselten Verwalten von Dateien. Der veröffentlichte Installer enthält weiterhin die bewährte Windows-/WinForms-Anwendung als selbstenthaltendes x64-Paket.

Mit M5 steht daneben ein plattformübergreifender Avalonia-Client für Windows, Android, Linux und macOS bereit. Er kann Tresore entsperren, Ordner navigieren und anlegen, Dokumente über Systemdialoge importieren und exportieren sowie vollständige `.ntevault`-Archive übertragen. Hinzu kommen lokale und globale Suche, Mehrfachauswahl, Umbenennen, Verschieben, rekursives Löschen, Textbearbeitung, Einzelbildansicht und portable Einstellungen. Android verwendet einen privaten App-Tresor, streambasierte Dokumentanbieter-Zugriffe und automatische Sitzungssperren. `Nte.Core` enthält die UI-unabhängige Kernlogik für .NET 10; `Nte.Storage` kapselt Dateisystem, Sandbox und Transfer.

## Unterstützte Systeme

Veröffentlichte WinForms-Referenz:

- Windows 10 ab Build 19041 oder Windows 11
- x64-Prozessor
- Installation pro Benutzer, ohne Administratorrechte

Avalonia-M5-Client aus dem Quellcode:

- Windows x64, lokal gestartet und geprüft
- Android 12 oder neuer (API 31 bis 36); APK auf einem API-35-Emulator geprüft
- Linux x64, Runtime-Publish und CI-Starttest unter Xvfb
- macOS ARM64, Runtime-Publish

## Avalonia-Desktop-Client starten

Mit .NET SDK 10.0.400:

```powershell
dotnet run --project ".\Nte.Desktop\Nte.Desktop.csproj"
```

Der Client verwendet denselben Benutzerdatenordner und dasselbe Tresorformat wie die WinForms-Referenz. Für einen isolierten Entwicklungsstart kann `NTE_DATA_DIRECTORY` auf ein separates Verzeichnis gesetzt werden. `--startup-probe` startet die vollständige Oberfläche mit einem temporären Tresor und schließt sie nach der Initialisierung automatisch.

## Android-APK bauen

Zusätzlich zu .NET SDK 10.0.400 werden Java 17, Android SDK 36 und der .NET-Android-Workload benötigt:

```powershell
dotnet workload install android
$env:AVALONIA_TELEMETRY_OPTOUT = '1'
dotnet build ".\Nte.Android\Nte.Android.csproj" -c Release
```

Die APKs liegen anschließend unter `Nte.Android\bin\Release\net10.0-android`. Lokal signierte Debug-/Release-Ausgaben dienen nur Entwicklung und Tests; eine Veröffentlichung benötigt einen geschützten Produktionsschlüssel.

## Tests ausführen

Für alle Windows- und Core-Tests werden .NET 10 SDK `10.0.400` und .NET 11 SDK Preview 7 benötigt. Das neuere SDK führt den gesamten Solution-Test aus:

```powershell
dotnet test ".\NET Thing Encryptor.sln" -c Release
```

Der plattformneutrale Kern lässt sich mit .NET 10 unabhängig von WinForms testen:

```powershell
dotnet test ".\Nte.Core.Tests\Nte.Core.Tests.csproj" -c Release
dotnet test ".\Nte.Storage.Tests\Nte.Storage.Tests.csproj" -c Release
dotnet test ".\Nte.App.Tests\Nte.App.Tests.csproj" -c Release
```

CI führt die drei plattformneutralen Tests zusätzlich unter Linux aus, startet dort den Avalonia-Client unter Xvfb, publiziert den Desktop-Host für macOS ARM64 und baut ein Android-Release-APK. Die Architekturentscheidungen stehen in [docs/m1-core-extraction.md](docs/m1-core-extraction.md), [docs/m2-storage-transfer.md](docs/m2-storage-transfer.md), [docs/m3-avalonia-client.md](docs/m3-avalonia-client.md), [docs/m4-android-client.md](docs/m4-android-client.md) und [docs/m5-feature-parity.md](docs/m5-feature-parity.md).

## Installer bauen

Voraussetzungen:

- .NET 11 SDK Preview 7 (`11.0.100-preview.7.26381.103`) für Windows
- .NET 10 SDK (`10.0.400`) für `Nte.Core`
- Inno Setup 6 (`winget install JRSoftware.InnoSetup`)

Hinweis: .NET 11 ist bis zur geplanten finalen Veröffentlichung am 10. November 2026 eine Preview und wird von Microsoft nicht für den Produktionseinsatz unterstützt.

```powershell
.\build\build-installer.ps1 -Clean
```

Das Skript stellt Abhängigkeiten wieder her, führt die Release-Tests aus, veröffentlicht die App self-contained und baut anschließend den Installer. Die Ausgabe liegt unter `artifacts\installer` und besteht aus:

- `NET-Thing-Encryptor-Setup-<Version>.exe`
- `NET-Thing-Encryptor-Setup-<Version>.exe.sha256`

Nützliche Optionen:

```powershell
# Nur veröffentlichen, keinen Installer bauen
.\build\build-installer.ps1 -SkipInstaller

# Tests überspringen, beispielsweise nach einem bereits erfolgreichen CI-Testlauf
.\build\build-installer.ps1 -SkipTests

# Sicherstellen, dass Projekt und Release-Tag dieselbe Version verwenden
.\build\build-installer.ps1 -ExpectedVersion 3.7.0

# Experimentelles Single-File-Paket; Medien- und Bildfunktionen danach manuell prüfen
.\build\build-installer.ps1 -SingleFile
```

Die Multi-File-Ausgabe ist der Standard, weil VLC und ImageMagick native Bibliotheken verwenden. Nicht benötigte VLC-Architekturen und Debugsymbole werden aus dem Installer ausgeschlossen.

## Code-Signierung

Der Build kann ein PFX-Zertifikat oder ein Zertifikat aus dem Windows-Zertifikatsspeicher verwenden. Die Haupt-EXE wird vor dem Packen signiert, der fertige Installer danach.

```powershell
$env:NTE_SIGN_CERTIFICATE_PATH = "C:\secure\code-signing.pfx"
$env:NTE_SIGN_CERTIFICATE_PASSWORD = "<password>"
$env:NTE_SIGN_TIMESTAMP_URL = "http://timestamp.digicert.com"
.\build\build-installer.ps1 -Clean -RequireSignature
```

Alternativ kann `NTE_SIGN_CERTIFICATE_THUMBPRINT` gesetzt werden. `-RequireSignature` bricht den Build ab, wenn kein Zertifikat konfiguriert ist oder die Signaturprüfung fehlschlägt. Zertifikate und Passwörter gehören niemals in das Repository.

## Installer prüfen

Die statische Prüfung validiert Version, SHA-256-Prüfsumme und optional die Signatur:

```powershell
.\build\test-installer.ps1 `
  -InstallerPath ".\artifacts\installer\NET-Thing-Encryptor-Setup-3.7.0.exe" `
  -ExpectedVersion 3.7.0
```

Der vollständige Smoke-Test installiert und deinstalliert die englische und deutsche Variante. Er darf nur in einem isolierten CI-Konto oder einer Test-VM ausgeführt werden:

```powershell
.\build\test-installer.ps1 `
  -InstallerPath ".\artifacts\installer\NET-Thing-Encryptor-Setup-3.7.0.exe" `
  -ExpectedVersion 3.7.0 `
  -RunInstallation `
  -AllowLocalMachineChanges
```

Der Test verweigert die Ausführung, wenn NET Thing Encryptor bereits installiert ist oder ein Benutzerdatenordner existiert.

## Installation und Updates

Der Installer verwendet standardmäßig:

```text
%LOCALAPPDATA%\Programs\NET Thing Encryptor
```

Benutzerdaten liegen getrennt davon unter:

```text
%LOCALAPPDATA%\NET Thing Encryptor\Data
```

Updates verwenden dieselbe stabile App-ID. Ein älterer Installer darf keine neuere installierte Version überschreiben. Die Deinstallation entfernt Programmdateien und Verknüpfungen, aber niemals Benutzerdaten.

Ein alter `Data`-Ordner neben einer portablen EXE wird beim ersten Start atomisch kopiert. Existieren im Ziel bereits andere Dateien, stoppt die Anwendung und meldet den Konflikt, anstatt Daten zu überschreiben. Der ursprüngliche Datenordner bleibt erhalten.

### Unbeaufsichtigte Installation

```powershell
NET-Thing-Encryptor-Setup-3.7.0.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-
```

Der Uninstaller befindet sich im Installationsverzeichnis und akzeptiert dieselben Silent-Schalter.

## Automatisierte Releases

- `.github/workflows/ci.yml` baut und testet auf Pull Requests und auf `master` einen unsignierten CI-Installer.
- `.github/workflows/release.yml` wird durch Tags wie `v3.7.0` gestartet, prüft die Versionsgleichheit, verlangt eine gültige Signatur, führt den Installations-Smoke-Test aus und veröffentlicht Setup plus Prüfsumme als GitHub Release.

Für signierte Releases werden diese Repository-Secrets benötigt:

- `CODE_SIGNING_CERTIFICATE_BASE64`: PFX-Datei als Base64
- `CODE_SIGNING_CERTIFICATE_PASSWORD`: Passwort der PFX-Datei

Weitere Details und die manuelle Abnahmeliste stehen in [docs/installer.md](docs/installer.md).
