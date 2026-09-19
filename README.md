# NET Thing Encryptor

NET Thing Encryptor ist eine Anwendung zum verschlüsselten Verwalten von Dateien. Seit M6 ist der plattformübergreifende Avalonia-Client das kanonische Produkt für den Windows-Installer und Android-Releases. Er kann Tresore entsperren, Ordner navigieren und anlegen, Dokumente über Systemdialoge importieren und einzeln, mehrfach oder als Ordnerstruktur exportieren sowie vollständige `.ntevault`-Archive übertragen. Hinzu kommen lokale und globale Suche, Mehrfachauswahl, Umbenennen, Verschieben, rekursives Löschen, Textbearbeitung, Bildserien mit Zufallsreihenfolge und Autoplay, Audio- und Videowiedergabe aus dem Arbeitsspeicher sowie portable Einstellungen. Ein optionaler Linux-Server stellt die weiterhin clientseitig verschlüsselten Tresordateien mehreren Geräten über HTTPS bereit. Android verwendet alternativ einen privaten App-Tresor, streambasierte Dokumentanbieter-Zugriffe und automatische Sitzungssperren. `Nte.Core` enthält die UI-unabhängige Kernlogik für .NET 10; `Nte.Storage` kapselt Dateisystem, Sandbox, Remotezugriff und Transfer.

Der frühere WinForms-Client bleibt vorerst als eingefrorener, separat getesteter Rückfallpfad im Quellbaum. Neue Produktfunktionen und reguläre Pakete entstehen ausschließlich in den Avalonia-Projekten.

## Unterstützte Systeme

- Windows 10 ab Build 19041 oder Windows 11, x64, selbstenthaltender Avalonia-Installer pro Benutzer
- Android 12 oder neuer (API 31 bis 36), AAB für Stores und APK für direkte Testinstallation
- Linux x64 auf Debian/Ubuntu, selbstenthaltendes `.deb`-Paket und portables `.tar.gz`
- macOS ARM64, Runtime-Publish
- Dedicated Server mit Linux x64 oder ARM64 und .NET-10-Runtime beziehungsweise Docker

Das Linux-Paket installiert die App samt Desktop-Eintrag und verwendet die von Debian beziehungsweise Ubuntu bereitgestellte LibVLC-Laufzeit. macOS bleibt eine technische Vorschau: Die CI prüft den Publish, aber es gibt noch kein signiertes beziehungsweise notarisiertes App-Bundle.

## Remote-Tresor

`Nte.RemoteServer` stellt einen verschlüsselten Tresor auf einem Linux-Server bereit. Der Server sieht weder das Tresorpasswort noch entschlüsselte Dateinamen oder Inhalte. Das separate Server-Zugangspasswort schützt die HTTP-API. Remote-Verbindungen verwenden immer HTTPS; unverschlüsseltes HTTP ist ausschließlich für lokale Tests auf demselben Gerät zulässig.

Schnellstart mit Docker Compose:

```bash
cd deploy
export NTE_REMOTE_ACCESS_PASSWORD='ein-langes-zufaelliges-server-passwort'
docker compose -f docker-compose.remote.yml up -d --build
```

Die Compose-Vorgabe bindet den Dienst sicher an `127.0.0.1:5248`; für andere Geräte wird anschließend ein HTTPS-Reverse-Proxy eingerichtet. Der Sperrbildschirm trennt lokale und entfernte Tresore in eigene Reiter. Für die Remote-Verbindung genügen Servername oder IP-Adresse, da `https://` automatisch ergänzt wird. Danach das Server-Zugangspasswort eingeben und den Tresor wie gewohnt mit dessen eigenem Tresorpasswort entsperren. Einen bestehenden lokalen Tresor lagert man ohne erneute Entschlüsselung aus: zuerst unter „Einstellungen → Tresorsicherung“ als `.ntevault` exportieren, mit dem leeren Remote-Speicher verbinden und dort das Archiv importieren. Der Import zeigt seinen Prüf- und Übertragungsfortschritt; die lokale Kopie wird dabei nicht automatisch gelöscht.

Produktionsbetrieb, TLS-Reverse-Proxy, Umgebungsvariablen, Backup und Update sind in [docs/remote-vault.md](docs/remote-vault.md) beschrieben.

## Plattformspezifische Bedienung

Der Windows-Client verwendet eine kompakte Symbolleiste, eine Dateiliste in Detailansicht und eine feste Statuszeile mit Version, Meldung, Datei- und Ordnerzahl sowie sichtbarer Gesamtgröße. Ordner und Dokumente werden per Doppelklick geöffnet; Auswahlaktionen liegen im Rechtsklick-Kontextmenü. Beim Anzeigen des Sperrbildschirms erhält das Passwortfeld automatisch den Eingabefokus und markiert einen eventuell vorhandenen Inhalt. Die Einstellungen erscheinen als eigenes modales Fenster, der vollständige Tresorexport liegt dort im Bereich „Tresorsicherung“. Text-, Bild-, Audio- und Videodokumente öffnen in eigenen nicht-modalen Fenstern, sodass die Hauptansicht bedienbar bleibt und mehrere Dokumente gleichzeitig geöffnet sein können. Medien können über Schaltflächen, Zeitleiste oder Tastatur gesteuert werden. Der in separaten Fenstern überflüssige Zurück-Knopf bleibt nur in der eingebetteten Android-Ansicht sichtbar.

Android verwendet dieselben Funktionen und Dateitypsymbole in einer kompakteren Zeilenansicht. Auswahlaktionen sind über den Drei-Punkte-Knopf einer Zeile erreichbar, die Desktop-Statuszeile entfällt und die Einstellungen belegen als deckende Seite die verfügbare App-Fläche. Der Sperrbildschirm bietet neben dem lokalen Tresor die Verbindung zu einem Remote-Tresor; auf einem leeren Speicher kann außerdem ein Archiv importiert werden. Der genaue UI-Vertrag und die Prüfschritte stehen in [docs/platform-ui-refinement.md](docs/platform-ui-refinement.md); Architektur, Speicherregeln und Abnahme der Medienwiedergabe beschreibt [docs/video-player.md](docs/video-player.md).

## Avalonia-Desktop-Client starten

Mit .NET SDK 10.0.400:

```powershell
dotnet run --project ".\Nte.Desktop\Nte.Desktop.csproj"
```

Der Client verwendet denselben Benutzerdatenordner und dasselbe Tresorformat wie die WinForms-Referenz. Für einen isolierten Entwicklungsstart kann `NTE_DATA_DIRECTORY` auf ein separates Verzeichnis gesetzt werden. `--startup-probe` startet die vollständige Oberfläche mit einem temporären Tresor und schließt sie nach der Initialisierung automatisch.

## Linux-Pakete bauen

Der Linux-x64-Build ist selbstenthaltend und benötigt keine separat installierte .NET-Laufzeit. Auf Debian oder Ubuntu werden PowerShell, `dpkg-deb`, `tar` und die nativen Desktop-/LibVLC-Abhängigkeiten benötigt. Das Paket-Skript erzeugt ein installierbares `.deb`, ein portables `.tar.gz` und SHA-256-Dateien:

```powershell
./build/build-linux-package.ps1 -Clean
```

Die auslieferbaren Ergebnisse liegen unter `artifacts/release/desktop/linux-x64`; Publish- und Paketierungszwischenstände unter `artifacts/staging/desktop/linux-x64`. Das Debian-Paket deklariert `libvlc5`, `vlc-plugin-base`, `vlc-plugin-video-output` sowie die benötigten X11-/Fontconfig-Bibliotheken als Abhängigkeiten. CI installiert das erzeugte Paket und prüft sowohl den Avalonia-Start unter Xvfb als auch das Laden der systemweiten LibVLC-Laufzeit. Details stehen in [docs/linux.md](docs/linux.md).

## Android-Pakete bauen

Zusätzlich zu .NET SDK 10.0.400 werden Java 17, Android SDK 36 und der .NET-Android-Workload benötigt:

```powershell
dotnet workload install android
$env:AVALONIA_TELEMETRY_OPTOUT = '1'
dotnet publish ".\Nte.Android\Nte.Android.csproj" -c Release
```

Die AAB- und APK-Dateien liegen anschließend unter `Nte.Android\bin\Release\net10.0-android`. Lokal erzeugte Ausgaben dienen nur Entwicklung und Tests. Der Tag-Workflow verlangt einen geschützten Produktionsschlüssel, prüft beide Signaturen und veröffentlicht beide Formate mit SHA-256-Prüfsummen. Bei jeder Android-Veröffentlichung muss neben `Version` auch der monotone ganzzahlige `ApplicationVersion`-Wert erhöht werden. Die plattformgebundene VideoLAN-Laufzeit erhöht die Größe der Android-Pakete deutlich.

Der Android-Medienplayer verwendet `VideoLAN.LibVLC.Android` 3.7.0-beta für `arm64-v8a` und `x86_64`. Die stabile Version 3.6.5 erzeugte im aktuellen Android-Release-Build vier `XA0141`-Warnungen wegen fehlender Android-16-/16-KB-Seitengrößenunterstützung; 3.7.0-beta baut ohne diese Warnungen. Da es sich um eine Vorabversion handelt, bleiben Audio- und Videowiedergabe auf realer Android-Hardware ein verpflichtendes Release-Gate.

## Tests ausführen

Die kanonischen plattformübergreifenden Tests und der Windows-Installer benötigen nur .NET SDK 10.0.400:

```powershell
dotnet run --project ".\Nte.Core.Tests\Nte.Core.Tests.csproj" -c Release -- --minimum-expected-tests 1
dotnet run --project ".\Nte.Storage.Tests\Nte.Storage.Tests.csproj" -c Release -- --minimum-expected-tests 1
dotnet run --project ".\Nte.App.Tests\Nte.App.Tests.csproj" -c Release -- --minimum-expected-tests 1
```

Der WinForms-Rückfallpfad benötigt weiterhin .NET 11 Preview 7 und wird in einem eigenen CI-Job über `build/legacy-winforms.slnf` geprüft. CI testet den gemeinsamen Kern unter Linux, startet dort den Avalonia-Client unter Xvfb, publiziert den Desktop-Host für macOS ARM64, baut Android-AAB und -APK und erzeugt den Avalonia-Windows-Installer. Die Architekturentscheidungen stehen in [docs/m1-core-extraction.md](docs/m1-core-extraction.md), [docs/m2-storage-transfer.md](docs/m2-storage-transfer.md), [docs/m3-avalonia-client.md](docs/m3-avalonia-client.md), [docs/m4-android-client.md](docs/m4-android-client.md), [docs/m5-feature-parity.md](docs/m5-feature-parity.md), [docs/m6-cutover.md](docs/m6-cutover.md), [docs/m7-winforms-retirement.md](docs/m7-winforms-retirement.md) und [docs/m8-post-cutover-hardening.md](docs/m8-post-cutover-hardening.md). Die nach M8 vorgezogene Oberflächenangleichung ist separat in [docs/platform-ui-refinement.md](docs/platform-ui-refinement.md) beschrieben; sie startet M9 ausdrücklich nicht.

## Installer bauen

Voraussetzungen:

- .NET 10 SDK (`10.0.400`)
- Inno Setup 6 (`winget install JRSoftware.InnoSetup`)

```powershell
.\build\build-installer.ps1 -Clean
```

Das Skript stellt Abhängigkeiten wieder her, führt die Release-Tests aus, veröffentlicht die App self-contained nach `artifacts\staging\desktop\windows-x64\publish` und baut anschließend den Installer. Die auslieferbare Ausgabe liegt unter `artifacts\release\desktop\windows-x64` und besteht aus:

- `NET-Thing-Encryptor-Setup-<Version>.exe`
- `NET-Thing-Encryptor-Setup-<Version>.exe.sha256`

Nützliche Optionen:

```powershell
# Nur veröffentlichen, keinen Installer bauen
.\build\build-installer.ps1 -SkipInstaller

# Tests überspringen, beispielsweise nach einem bereits erfolgreichen CI-Testlauf
.\build\build-installer.ps1 -SkipTests

# Sicherstellen, dass Projekt und Release-Tag dieselbe Version verwenden
.\build\build-installer.ps1 -ExpectedVersion 4.3.3

# Optionales Single-File-Paket; vor einer Veröffentlichung separat prüfen
.\build\build-installer.ps1 -SingleFile
```

Die Multi-File-Ausgabe bleibt der geprüfte Standard. Der Installer enthält `Nte.Desktop` einschließlich der für den Medienplayer benötigten Windows-Laufzeit von VideoLAN. Beim Upgrade entfernt er ausschließlich eindeutig veraltete Medien- und ImageMagick-Komponenten der WinForms-Ausgabe, ohne aktive Laufzeitdateien oder den möglicherweise portablen `Data`-Ordner im Installationsverzeichnis zu verändern. Durch die native Medienlaufzeit ist das Setup größer als die bisherigen Avalonia-Installer ohne Wiedergabebackend.

### WinForms-Rückfallpaket

M7 hält den eingefrorenen WinForms-Stand bis zum Erreichen der dokumentierten Löschkriterien reproduzierbar. Der Build benötigt zusätzlich .NET 11 Preview 7:

```powershell
.\build\build-installer.ps1 `
  -ProductLine WinFormsRollback `
  -Clean
```

Die Ausgabe `artifacts\verification\legacy-winforms\windows-x64\NET-Thing-Encryptor-WinForms-Rollback-Setup-<Version>.exe` dient ausschließlich CI und einem ausdrücklich beschlossenen Notfall-Rückfall. Der reguläre Release-Workflow durchsucht dieses Verzeichnis nicht. `build/test-winforms-upgrade.ps1` installiert das Rückfallpaket in einem isolierten Konto, aktualisiert es in-place auf Avalonia und vergleicht das M0-Kompatibilitätsfixture vor Upgrade und nach Deinstallation bytegenau.

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
  -InstallerPath ".\artifacts\release\desktop\windows-x64\NET-Thing-Encryptor-Setup-4.3.3.exe" `
  -ExpectedVersion 4.3.3
```

Der vollständige Smoke-Test installiert und deinstalliert die englische und deutsche Variante. Er darf nur in einem isolierten CI-Konto oder einer Test-VM ausgeführt werden:

```powershell
.\build\test-installer.ps1 `
  -InstallerPath ".\artifacts\release\desktop\windows-x64\NET-Thing-Encryptor-Setup-4.3.3.exe" `
  -ExpectedVersion 4.3.3 `
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

Updates verwenden dieselbe stabile App-ID. Der interaktive Assistent liest die vorhandene Version aus der Benutzer- oder Maschinenregistrierung und kündigt ein Update von der installierten auf die neue Version beziehungsweise eine Reparatur derselben Version an. Ein älterer Installer darf keine neuere installierte Version überschreiben. Die Deinstallation entfernt Programmdateien und Verknüpfungen, aber niemals Benutzerdaten.

Ein alter `Data`-Ordner neben einer portablen EXE wird beim ersten Start atomisch kopiert. Existieren im Ziel bereits andere Dateien, stoppt die Anwendung und meldet den Konflikt, anstatt Daten zu überschreiben. Der ursprüngliche Datenordner bleibt erhalten.

### Unbeaufsichtigte Installation

```powershell
NET-Thing-Encryptor-Setup-4.3.3.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-
```

Der Uninstaller befindet sich im Installationsverzeichnis und akzeptiert dieselben Silent-Schalter.

## Automatisierte Releases

Build-Ausgaben folgen einem gemeinsamen Vertrag:

- `artifacts/release`: ausschließlich veröffentlichbare, benannte Pakete samt Prüfsummen;
- `artifacts/staging`: entpackte Publishes und temporäre Paketierungsbäume;
- `artifacts/verification`: Smoke-Test-, Rückfall- und sonstige Prüfausgaben.

Desktop-Artefakte sind darunter nach Plattform und Architektur (`windows-x64`, `linux-x64`, `macos-arm64`) gegliedert. Android liegt unter `release/android`, Serverpakete unter `release/server/<Runtime>`.

- `.github/workflows/ci.yml` testet auf Pull Requests und auf `master` die gemeinsamen Komponenten, beide priorisierten Plattformen, die sekundären Desktop-Publishes sowie den WinForms-Rückfallbuild und das in-place Upgrade auf Avalonia.
- `.github/workflows/release.yml` wird durch Tags wie `v4.3.3` gestartet, prüft die Versionsgleichheit, verlangt Windows- und Android-Signaturen, installiert und prüft das Linux-Paket, führt den Windows-Installations-Smoke-Test aus und veröffentlicht Setup, AAB, APK, Linux-DEB, Linux-Tarball sowie alle Prüfsummen als GitHub Release.

Für signierte Releases werden diese Repository-Secrets benötigt:

- `CODE_SIGNING_CERTIFICATE_BASE64`: PFX-Datei als Base64
- `CODE_SIGNING_CERTIFICATE_PASSWORD`: Passwort der PFX-Datei
- `ANDROID_SIGNING_KEYSTORE_BASE64`: Android-Keystore als Base64
- `ANDROID_SIGNING_KEY_ALIAS`: Alias des Produktionsschlüssels
- `ANDROID_SIGNING_KEY_PASSWORD`: Passwort des Schlüssels
- `ANDROID_SIGNING_STORE_PASSWORD`: Passwort des Keystores

Weitere Details und die manuelle Abnahmeliste stehen in [docs/installer.md](docs/installer.md).
