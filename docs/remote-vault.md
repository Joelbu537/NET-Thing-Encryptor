# Remote-Tresor auf Linux

## Sicherheitsmodell

Der Remote-Server ist ein Speicher für bereits verschlüsselte NTE-Dateien. Verschlüsselung, Entschlüsselung und Prüfung des Tresorpassworts finden ausschließlich im Windows-, Linux-, macOS- oder Android-Client statt. Der Server erhält nur:

- die verschlüsselte Root-Datei `0.nte`;
- verschlüsselte Objektdateien mit zufälligen 64-Bit-IDs als Namen;
- das separate Server-Zugangspasswort zur HTTP-Authentifizierung.

Server-Zugangspasswort und Tresorpasswort sollten verschieden sein. Basic Authentication schützt nicht vor Abhören. Außerhalb eines isolierten LANs oder eines bereits verschlüsselten VPNs muss deshalb ein TLS-Reverse-Proxy vor dem Dienst stehen. Das Datenverzeichnis sollte zusätzlich mit normalen Linux-Dateirechten geschützt und regelmäßig gesichert werden.

Der Server versieht jede Änderung mit einer neuen Revision. Schreibt ein zweites Gerät auf Basis eines veralteten Standes, erhält es einen Konflikt statt unbemerkt neuere Daten zu überschreiben. Nach einem Konflikt den Tresor sperren und neu verbinden. Gleichzeitige Bearbeitung desselben Tresors durch mehrere Geräte ist nicht als kollaborativer Modus gedacht.

## Docker Compose

Voraussetzungen sind Docker Engine und das Compose-Plugin. Im Repository:

```bash
cd deploy
export NTE_REMOTE_ACCESS_PASSWORD='ein-langes-zufaelliges-server-passwort'
docker compose -f docker-compose.remote.yml up -d --build
docker compose -f docker-compose.remote.yml ps
curl http://127.0.0.1:5248/health
```

Die verschlüsselten Daten liegen im Docker-Volume `deploy_nte-remote-data`. Das Image läuft als unprivilegierter Benutzer. Port `5248` ist nur an Loopback gebunden, damit ein Reverse-Proxy HTTPS übernehmen kann. Er darf nicht ungeschützt aus dem Internet oder LAN erreichbar gemacht werden.

## Direkt mit systemd

GitHub-Releases enthalten runtimeabhängige Archive für `linux-x64` und `linux-arm64` samt SHA-256-Datei. Alternativ den Server aus dem Repository veröffentlichen:

```bash
dotnet publish Nte.RemoteServer/Nte.RemoteServer.csproj \
  -c Release -r linux-x64 --self-contained false -o publish/nte-remote
sudo useradd --system --home /var/lib/nte-remote --shell /usr/sbin/nologin nte
sudo install -d -o nte -g nte -m 0700 /var/lib/nte-remote
sudo install -d -o root -g root -m 0755 /opt/nte-remote
sudo cp -a publish/nte-remote/. /opt/nte-remote/
sudo install -o root -g nte -m 0640 /dev/null /etc/nte-remote-password
sudoedit /etc/nte-remote-password
```

Beispiel für `/etc/systemd/system/nte-remote.service`:

```ini
[Unit]
Description=NET Thing Encryptor remote vault
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
User=nte
Group=nte
WorkingDirectory=/opt/nte-remote
Environment=ASPNETCORE_URLS=http://127.0.0.1:5248
Environment=NTE_REMOTE_DATA_DIRECTORY=/var/lib/nte-remote
Environment=NTE_REMOTE_ACCESS_PASSWORD_FILE=/etc/nte-remote-password
ExecStart=/usr/bin/dotnet /opt/nte-remote/Nte.RemoteServer.dll
Restart=on-failure
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ReadWritePaths=/var/lib/nte-remote

[Install]
WantedBy=multi-user.target
```

Danach:

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now nte-remote
sudo systemctl status nte-remote
```

## HTTPS mit Caddy

Nachdem ein DNS-Name wie `vault.example.net` auf den Server zeigt, kann Caddy ein öffentlich vertrauenswürdiges Zertifikat automatisch beziehen. Beispiel für `/etc/caddy/Caddyfile`:

```caddyfile
vault.example.net {
    reverse_proxy 127.0.0.1:5248
}
```

Anschließend Caddy neu laden und in der Firewall nur SSH sowie TCP 80/443 freigeben. Der Client verwendet dann `https://vault.example.net`. Bei einem VPN kann stattdessen dessen HTTPS-/Zertifikatsfunktion verwendet werden; selbstsignierte Zertifikate werden von normalen Desktop- und Android-Clients nicht automatisch vertraut.

## Konfiguration

| Variable | Bedeutung | Standard |
| --- | --- | --- |
| `NTE_REMOTE_ACCESS_PASSWORD` | Server-Zugangspasswort direkt aus der Umgebung | – |
| `NTE_REMOTE_ACCESS_PASSWORD_FILE` | Datei mit dem Server-Zugangspasswort; für systemd/Docker-Secrets bevorzugt | – |
| `NTE_REMOTE_DATA_DIRECTORY` | Verzeichnis für verschlüsselte `.nte`-Dateien | `./vault-data` |
| `NTE_REMOTE_MAX_OBJECT_BYTES` | Maximale HTTP-Requestgröße je verschlüsseltem Objekt | 8 GiB |
| `ASPNETCORE_URLS` | Bindeadresse und Port von Kestrel | .NET-Standard |

Mindestens eine der beiden Passwortvarianten ist erforderlich. Die Passwortdatei hat Vorrang.

## Client und Migration

Auf dem Sperrbildschirm ist die Remote-Verbindung auch dann erreichbar, wenn auf dem Gerät bereits ein lokaler Tresor liegt:

1. Servernamen oder IP-Adresse eingeben, zum Beispiel `vault.example.net`. Fehlt das Schema, ergänzt der Client automatisch `https://`. Unverschlüsseltes HTTP ist ausschließlich über `localhost`, `127.0.0.1` oder `::1` für lokale Tests erlaubt.
2. Server-Zugangspasswort eingeben und verbinden.
3. Einen vorhandenen Remote-Tresor mit seinem separaten Tresorpasswort entsperren.
4. Ist der Remote-Speicher leer, entweder einen neuen Tresor anlegen oder ein `.ntevault`-Archiv importieren.

Zum Auslagern eines lokalen Tresors zuerst ein `.ntevault`-Archiv über die Einstellungen exportieren. Danach auf dem Sperrbildschirm mit einem leeren Remote-Server verbinden und das Archiv importieren. Die Payloads bleiben dabei verschlüsselt; nur der plattformspezifische Speicherlocator in der Root-Metadatei wird angepasst. Die lokale Kopie bleibt als Rückfall und wird nicht automatisch entfernt.

Das Server-Zugangspasswort wird nicht dauerhaft auf dem Client gespeichert. Nach einem Neustart wird es erneut eingegeben. Damit landet kein zweites, zur Serveranmeldung ausreichendes Geheimnis unverschlüsselt in den App-Einstellungen.

### Abgebrochenen Erstimport bereinigen

Beim Import wird `0.nte`, die Root-Datei des Tresors, absichtlich zuletzt geschrieben. Wird der Client während des Imports beendet, können deshalb bereits verschlüsselte Objektdateien auf dem Server liegen, obwohl die App den Remote-Speicher weiterhin als nicht eingerichteten Tresor anzeigt. Ein erneuter Import überschreibt diese Dateien nicht.

Für die oben gezeigte systemd-Konfiguration den Dienst stoppen und das gesamte Datenverzeichnis unter einem eindeutigen Namen beiseitelegen. So bleibt der unvollständige Stand wiederherstellbar:

```bash
sudo systemctl stop nte-remote
sudo ls -la /var/lib/nte-remote
sudo test ! -e /var/lib/nte-remote.incomplete-backup
sudo mv -- /var/lib/nte-remote /var/lib/nte-remote.incomplete-backup
sudo install -d -o nte -g nte -m 0700 /var/lib/nte-remote
sudo systemctl start nte-remote
curl https://vault.example.net/health
```

Existiert `/var/lib/nte-remote.incomplete-backup` bereits, vor dem `mv` einen anderen eindeutigen Zielnamen einsetzen. Anschließend den Remote-Tresor im Client neu verbinden und das Archiv erneut importieren. Erst wenn der neue Tresor vollständig funktioniert und eine separate Sicherung vorhanden ist, kann das beiseitegelegte Verzeichnis entfernt werden. Bei einer abweichenden Einstellung von `NTE_REMOTE_DATA_DIRECTORY` müssen die Pfade entsprechend angepasst werden.

## Backup und Wiederherstellung

Für ein konsistentes Serverbackup den Dienst kurz stoppen und das komplette Datenverzeichnis beziehungsweise Docker-Volume sichern. Alternativ im Client ein `.ntevault`-Archiv exportieren. Bei Wiederherstellung keine einzelnen Objektdateien verschiedener Zeitpunkte mischen; Root und Objekte bilden gemeinsam einen Stand.

Vor einem Serverupdate:

```bash
docker compose -f docker-compose.remote.yml stop
# Volume sichern
docker compose -f docker-compose.remote.yml build --pull
docker compose -f docker-compose.remote.yml up -d
```

Der ungeschützte Health-Endpunkt `GET /health` liefert nur Dienststatus und API-Version, keine Tresormetadaten. Alle Routen unter `/api` verlangen Authentifizierung.
