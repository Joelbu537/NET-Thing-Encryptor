WIP

## Installer bauen

Voraussetzungen:

- .NET SDK fuer Windows
- Inno Setup 6 fuer die `Setup.exe` (`winget install JRSoftware.InnoSetup`)

Installer erstellen:

```powershell
.\build\build-installer.ps1
```

Das Skript published die App selbstenthaltend nach `artifacts\publish\NET Thing Encryptor\win-x64` und erstellt den Installer unter `artifacts\installer`.

Die App speichert Benutzerdaten unter `%LOCALAPPDATA%\NET Thing Encryptor\Data`. Ein alter `Data`-Ordner neben der EXE wird beim ersten Start kopiert, damit Installationen ohne Adminrechte funktionieren.
