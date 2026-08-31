# NET Thing Encryptor

NET Thing Encryptor ist eine Anwendung zum verschlüsselten Verwalten von Dateien. Seit M6 ist der plattformübergreifende Avalonia-Client das kanonische Produkt für den Windows-Installer und Android-Releases. Er kann Tresore entsperren, Ordner navigieren und anlegen, Dokumente über Systemdialoge importieren und einzeln, mehrfach oder als Ordnerstruktur exportieren sowie vollständige `.ntevault`-Archive übertragen. Hinzu kommen lokale und globale Suche, Mehrfachauswahl, Umbenennen, Verschieben, rekursives Löschen, Textbearbeitung, Bildserien mit Zufallsreihenfolge und Autoplay sowie portable Einstellungen. Android verwendet einen privaten App-Tresor, streambasierte Dokumentanbieter-Zugriffe und automatische Sitzungssperren. `Nte.Core` enthält die UI-unabhängige Kernlogik für .NET 10; `Nte.Storage` kapselt Dateisystem, Sandbox und Transfer.

Der frühere WinForms-Client bleibt vorerst als eingefrorener, separat getesteter Rückfallpfad im Quellbaum. Neue Produktfunktionen und reguläre Pakete entstehen ausschließlich in den Avalonia-Projekten.

## Unterstützte Systeme

- Windows 10 ab Build 19041 oder Windows 11, x64, selbstenthaltender Avalonia-Installer pro Benutzer
- Android 12 oder neuer (API 31 bis 36), AAB für Stores und APK für direkte Testinstallation
- Linux x64, Runtime-Publish und CI-Starttest unter Xvfb
- macOS ARM64, Runtime-Publish

Linux und macOS sind weiter technische Vorschauen: Die CI prüft die Publishes, aber es gibt noch keine distributionsspezifischen Linux-Pakete und kein signiertes beziehungsweise notarisiertes macOS-App-Bundle.

## Plattformspezifische Bedienung

Der Windows-Client verwendet eine kompakte Symbolleiste, eine Dateiliste in Detailansicht und eine feste Statuszeile mit Version, Meldung, Datei- und Ordnerzahl sowie sichtbarer Gesamtgröße. Ordner und Dokumente werden per Doppelklick geöffnet; Auswahlaktionen liegen im Rechtsklick-Kontextmenü. Die Einstellungen erscheinen als eigenes modales Fenster, der vollständige Tresorexport liegt dort im Bereich „Tresorsicherung“.

Android verwendet dieselben Funktionen und Dateitypsymbole in einer kompakteren Zeilenansicht. Auswahlaktionen sind über den Drei-Punkte-Knopf einer Zeile erreichbar, die Desktop-Statuszeile entfällt und die Einstellungen belegen als deckende Seite die verfügbare App-Fläche. Der genaue UI-Vertrag und die Prüfschritte stehen in [docs/platform-ui-refinement.md](docs/platform-ui-refinement.md).

## Avalonia-Desktop-Client starten

Mit .NET SDK 10.0.400:

```powershell
dotnet run --project ".\Nte.Desktop\Nte.Desktop.csproj"
```

Der Client verwendet denselben Benutzerdatenordner und dasselbe Tresorformat wie die WinForms-Referenz. Für einen isolierten Entwicklungsstart kann `NTE_DATA_DIRECTORY` auf ein separates Verzeichnis gesetzt werden. `--startup-probe` startet die vollständige Oberfläche mit einem temporären Tresor und schließt sie nach der Initialisierung automatisch.

## Android-Pakete bauen

Zusätzlich zu .NET SDK 10.0.400 werden Java 17, Android SDK 36 und der .NET-Android-Workload benötigt:

```powershell
dotnet workload install android
$env:AVALONIA_TELEMETRY_OPTOUT = '1'
dotnet publish ".\Nte.Android\Nte.Android.csproj" -c Release
```

Die AAB- und APK-Dateien liegen anschließend unter `Nte.Android\bin\Release\net10.0-android`. Lokal erzeugte Ausgaben dienen nur Entwicklung und Tests. Der Tag-Workflow verlangt einen geschützten Produktionsschlüssel, prüft beide Signaturen und veröffentlicht beide Formate mit SHA-256-Prüfsummen. Bei jeder Android-Veröffentlichung muss neben `Version` auch der monotone ganzzahlige `ApplicationVersion`-Wert erhöht werden.

## Tests ausführen

Die kanonischen plattformübergreifenden Tests und der Windows-Installer benötigen nur .NET SDK 10.0.400:

```powershell
dotnet test ".\Nte.Core.Tests\Nte.Core.Tests.csproj" -c Release
dotnet test ".\Nte.Storage.Tests\Nte.Storage.Tests.csproj" -c Release
dotnet test ".\Nte.App.Tests\Nte.App.Tests.csproj" -c Release
```

Der WinForms-Rückfallpfad benötigt weiterhin .NET 11 Preview 7 und wird in einem eigenen CI-Job über `build/legacy-winforms.slnf` geprüft. CI testet den gemeinsamen Kern unter Linux, startet dort den Avalonia-Client unter Xvfb, publiziert den Desktop-Host für macOS ARM64, baut Android-AAB und -APK und erzeugt den Avalonia-Windows-Installer. Die Architekturentscheidungen stehen in [docs/m1-core-extraction.md](docs/m1-core-extraction.md), [docs/m2-storage-transfer.md](docs/m2-storage-transfer.md), [docs/m3-avalonia-client.md](docs/m3-avalonia-client.md), [docs/m4-android-client.md](docs/m4-android-client.md), [docs/m5-feature-parity.md](docs/m5-feature-parity.md), [docs/m6-cutover.md](docs/m6-cutover.md), [docs/m7-winforms-retirement.md](docs/m7-winforms-retirement.md) und [docs/m8-post-cutover-hardening.md](docs/m8-post-cutover-hardening.md). Die nach M8 vorgezogene Oberflächenangleichung ist separat in [docs/platform-ui-refinement.md](docs/platform-ui-refinement.md) beschrieben; sie startet M9 ausdrücklich nicht.

## Installer bauen

Voraussetzungen:

- .NET 10 SDK (`10.0.400`)
- Inno Setup 6 (`winget install JRSoftware.InnoSetup`)

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
.\build\build-installer.ps1 -ExpectedVersion 4.0.0

# Optionales Single-File-Paket; vor einer Veröffentlichung separat prüfen
.\build\build-installer.ps1 -SingleFile
```

Die Multi-File-Ausgabe bleibt der geprüfte Standard. Der Installer enthält seit M6 `Nte.Desktop`; beim Upgrade entfernt er eindeutig veraltete VLC- und ImageMagick-Bibliotheken der WinForms-Ausgabe, ohne den möglicherweise portablen `Data`-Ordner im Installationsverzeichnis zu verändern.

### WinForms-Rückfallpaket

M7 hält den eingefrorenen WinForms-Stand bis zum Erreichen der dokumentierten Löschkriterien reproduzierbar. Der Build benötigt zusätzlich .NET 11 Preview 7:

```powershell
.\build\build-installer.ps1 `
  -ProductLine WinFormsRollback `
  -Clean
```

Die Ausgabe `artifacts\legacy-winforms\installer\NET-Thing-Encryptor-WinForms-Rollback-Setup-<Version>.exe` dient ausschließlich CI und einem ausdrücklich beschlossenen Notfall-Rückfall. Der reguläre Release-Workflow durchsucht dieses Verzeichnis nicht. `build/test-winforms-upgrade.ps1` installiert das Rückfallpaket in einem isolierten Konto, aktualisiert es in-place auf Avalonia und vergleicht das M0-Kompatibilitätsfixture vor Upgrade und nach Deinstallation bytegenau.

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
  -InstallerPath ".\artifacts\installer\NET-Thing-Encryptor-Setup-4.0.0.exe" `
  -ExpectedVersion 4.0.0
```

Der vollständige Smoke-Test installiert und deinstalliert die englische und deutsche Variante. Er darf nur in einem isolierten CI-Konto oder einer Test-VM ausgeführt werden:

```powershell
.\build\test-installer.ps1 `
  -InstallerPath ".\artifacts\installer\NET-Thing-Encryptor-Setup-4.0.0.exe" `
  -ExpectedVersion 4.0.0 `
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
NET-Thing-Encryptor-Setup-4.0.0.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-
```

Der Uninstaller befindet sich im Installationsverzeichnis und akzeptiert dieselben Silent-Schalter.

## Automatisierte Releases

- `.github/workflows/ci.yml` testet auf Pull Requests und auf `master` die gemeinsamen Komponenten, beide priorisierten Plattformen, die sekundären Desktop-Publishes sowie den WinForms-Rückfallbuild und das in-place Upgrade auf Avalonia.
- `.github/workflows/release.yml` wird durch Tags wie `v4.0.0` gestartet, prüft die Versionsgleichheit, verlangt Windows- und Android-Signaturen, führt den Windows-Installations-Smoke-Test aus und veröffentlicht Setup, AAB, APK sowie alle Prüfsummen als GitHub Release.

Für signierte Releases werden diese Repository-Secrets benötigt:

- `CODE_SIGNING_CERTIFICATE_BASE64`: PFX-Datei als Base64
- `CODE_SIGNING_CERTIFICATE_PASSWORD`: Passwort der PFX-Datei
- `ANDROID_SIGNING_KEYSTORE_BASE64`: Android-Keystore als Base64
- `ANDROID_SIGNING_KEY_ALIAS`: Alias des Produktionsschlüssels
- `ANDROID_SIGNING_KEY_PASSWORD`: Passwort des Schlüssels
- `ANDROID_SIGNING_STORE_PASSWORD`: Passwort des Keystores

Weitere Details und die manuelle Abnahmeliste stehen in [docs/installer.md](docs/installer.md).
