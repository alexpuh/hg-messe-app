# Messe App - WPF WebView2 Client

Diese WPF-Anwendung startet automatisch den `messe-server` und zeigt die Swagger-UI in einem WebView2-Control an.

## Features

- ✅ Automatischer Start des messe-server
- ✅ Integrierte WebView2-Anzeige der Swagger-UI
- ✅ Status-Anzeige (Server läuft/nicht bereit)
- ✅ Automatisches Beenden des Servers beim Schließen der App
- ✅ Wartet automatisch, bis der Server bereit ist

## Voraussetzungen

- .NET 9.0 SDK
- Windows (für WPF)
- WebView2 Runtime (wird normalerweise mit Windows 10/11 mitgeliefert)

## Verwendung

### Starten der Anwendung

```powershell
cd C:\Work\github-herrmann\hg-messe-app\server\messe-app
dotnet run
```

Oder kompilieren und die EXE ausführen:

```powershell
dotnet build
.\bin\Debug\net9.0-windows\messe-app.exe
```

### Was passiert beim Start

1. Die Anwendung sucht automatisch nach der `messe-server.exe`
2. Der Server wird im Hintergrund gestartet
3. Die App wartet, bis der Server auf `http://localhost:5227` antwortet
4. Die Swagger-UI wird automatisch im WebView2 geladen
5. Status-Anzeige zeigt "Server läuft" mit grünem Indikator

### Server-Pfade

Die Anwendung sucht den messe-server in folgenden Pfaden (relativ zum App-Verzeichnis):

- `..\..\..\..\messe-server\bin\Debug\net9.0\messe-server.exe`
- `..\..\..\..\messe-server\bin\Release\net9.0\messe-server.exe`
- `.\messe-server\messe-server.exe`
- `..\messe-server\messe-server.exe`

## Konfiguration

### Server-URL ändern

In `MainWindow.xaml.cs`:

```csharp
private readonly string _serverUrl = "http://localhost:5227";
private readonly string _swaggerUrl = "http://localhost:5227/swagger";
```

### Fenster-Einstellungen

In `MainWindow.xaml`:

```xml
Title="Messe App" Height="768" Width="1024"
WindowState="Maximized"
```

## Architektur

### Komponenten

1. **MainWindow.xaml** - UI-Definition mit WebView2 und Status-Bar
2. **MainWindow.xaml.cs** - Logik für Server-Start und WebView-Management
3. **WebView2** - Microsoft Edge WebView2 Control für die Anzeige der Web-UI

### Ablauf

```
App Start
   ↓
MainWindow_Loaded
   ↓
Initialize WebView2
   ↓
Find Server Executable
   ↓
Start Server Process
   ↓
Wait for Server (max 30s)
   ↓
Load Swagger UI
   ↓
Show "Server läuft"
```

## Troubleshooting

### Server nicht gefunden

**Problem:** Fehlermeldung "Die Server-Anwendung (messe-server.exe) wurde nicht gefunden."

**Lösung:** 
1. Stellen Sie sicher, dass messe-server kompiliert wurde:
   ```powershell
   cd ..\messe-server
   dotnet build
   ```

### Server startet nicht

**Problem:** Fehlermeldung "Der Server konnte nicht gestartet werden..."

**Lösung:**
1. Überprüfen Sie, ob Port 5227 bereits verwendet wird
2. Starten Sie den Server manuell im Terminal, um Fehler zu sehen:
   ```powershell
   cd ..\messe-server
   dotnet run
   ```

### WebView2 Runtime fehlt

**Problem:** Die App startet, aber WebView2 funktioniert nicht.

**Lösung:** 
1. Installieren Sie die WebView2 Runtime von Microsoft
2. Oder verwenden Sie einen evergreen WebView2-Bootstrap

## Deployment

Für ein Deployment-Szenario:

1. Beide Projekte als self-contained publizieren:
   ```powershell
   dotnet publish -c Release --self-contained
   ```

2. Verzeichnisstruktur:
   ```
   messe-app/
   ├── messe-app.exe
   ├── [Dependencies...]
   └── messe-server/
       ├── messe-server.exe
       └── [Dependencies...]
   ```

## Dependencies

- **Microsoft.Web.WebView2** (1.0.3650.58) - WebView2 SDK

## Lizenz

[Ihre Lizenz hier]

