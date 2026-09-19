# Linux-Desktop-Release

## Unterstützter Umfang

Der Linux-Desktop-Client wird für x64-Systeme mit Debian- beziehungsweise Ubuntu-Paketverwaltung ausgeliefert. Das `.deb` ist selbstenthaltend und benötigt keine separate .NET-Laufzeit. Zusätzlich steht ein portables `.tar.gz` für kompatible x64-Distributionen bereit; dort müssen die nativen Abhängigkeiten manuell installiert werden.

Die Anwendung verwendet die systemweite LibVLC-Laufzeit. Dadurch enthält der Linux-Publish keine Windows-DLLs und keine fremden Windows-VLC-Plugins. Das Debian-Paket deklariert diese Laufzeit- und Desktop-Abhängigkeiten:

- `libvlc5`
- `libvlc-dev` (stellt die für LibVLCSharp benötigten unversionierten Loaderlinks bereit)
- `vlc-plugin-base`
- `vlc-plugin-video-output`
- `libx11-6`
- `libice6`
- `libsm6`
- `libfontconfig1`

## Pakete bauen

Auf einem Linux-x64-Buildhost mit .NET SDK 10.0.400, PowerShell, `dpkg-deb` und `tar`:

```powershell
./build/build-linux-package.ps1 -Clean
```

Optional kann die erwartete Produktversion gegen `Nte.Desktop.csproj` geprüft werden:

```powershell
./build/build-linux-package.ps1 -Clean -ExpectedVersion 4.3.3
```

Unter `artifacts/release/desktop/linux-x64` entstehen:

- `NET-Thing-Encryptor-<Version>-linux-x64.deb`
- `NET-Thing-Encryptor-<Version>-linux-x64.deb.sha256`
- `NET-Thing-Encryptor-<Version>-linux-x64.tar.gz`
- `NET-Thing-Encryptor-<Version>-linux-x64.tar.gz.sha256`

Der entpackte Publish und der temporäre Debian-Paketbaum liegen getrennt davon unter `artifacts/staging/desktop/linux-x64` und werden nicht veröffentlicht.

## Installation

Das Debian-Paket löst seine Abhängigkeiten über APT auf:

```bash
sudo apt install ./NET-Thing-Encryptor-4.3.3-linux-x64.deb
net-thing-encryptor
```

Es installiert die Anwendung unter `/opt/net-thing-encryptor`, den Starter unter `/usr/bin/net-thing-encryptor`, einen Desktop-Eintrag und das Anwendungssymbol. Eine Deinstallation entfernt keine Tresordaten.

Beim portablen Archiv wird die Anwendung direkt über `./NET Thing Encryptor` gestartet. Vorher müssen LibVLC und die Desktop-Bibliotheken mit dem Paketmanager der Distribution installiert werden.

## Automatisches Release-Gate

CI und Tag-Releases führen auf `ubuntu-latest` diese Schritte aus:

1. alle gemeinsamen Core-, Storage- und Avalonia-Tests ausführen;
2. einen frischen self-contained `linux-x64`-Publish erzeugen;
3. den Publish auf versehentlich enthaltene Windows-LibVLC-Dateien prüfen;
4. `.deb`, portables Archiv und SHA-256-Dateien erzeugen;
5. Metadaten und Inhalt des Debian-Pakets lesen;
6. das Debian-Paket samt Abhängigkeiten installieren;
7. die systemweite LibVLC-Laufzeit mit `--media-runtime-probe` laden;
8. die installierte Avalonia-Anwendung unter Xvfb mit `--startup-probe` starten.

Ein Release-Tag wird nur veröffentlicht, wenn dieses Linux-Gate sowie die Android-, Server- und Windows-Gates erfolgreich sind.

## Manuelle Release-Abnahme

Vor einer Veröffentlichung bleiben auf einer unterstützten Ubuntu- oder Debian-Desktopinstallation zu prüfen:

- Installation, Menüeintrag, Symbol, normaler Start und Deinstallation;
- Tresor anlegen, sperren, erneut entsperren und vorhandenes Tresorformat öffnen;
- Datei- und Ordnerimport sowie Einzel-, Mehrfach- und Tresorexport;
- Text- und Bildansicht;
- Audio und Video einschließlich Play/Pause, Seek, Fenstergrößenänderung und Ton;
- automatische Sperre während geöffneter Medien;
- lokaler sowie HTTPS-geschützter Remote-Tresor;
- Upgrade von der vorherigen Linux-Version ohne Veränderung der Benutzerdaten.
