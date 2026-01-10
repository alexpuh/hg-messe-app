# Messe App - Setup Zusammenfassung

## ✅ Erfolgreich erstellt!

Die WPF-Anwendung mit WebView2-Integration wurde erfolgreich erstellt und konfiguriert.

## Was wurde erstellt?

### 1. **messe-app** Projekt
- **Typ:** WPF .NET 9.0 Anwendung
- **Framework:** net9.0-windows
- **Speicherort:** `C:\Work\github-herrmann\hg-messe-app\server\messe-app\`

### 2. Hauptkomponenten

#### MainWindow.xaml
- WebView2 Control für Browser-Integration
- Status-Bar mit Indikator (Grün = läuft, Orange = wird gestartet)
- Maximiertes Fenster (1024x768)

#### MainWindow.xaml.cs
- Automatischer Server-Start (sucht messe-server.exe)
- Wartet bis zu 30 Sekunden auf Server-Bereitschaft
- Lädt automatisch Swagger UI (`http://localhost:5227/swagger`)
- Beendet den Server beim App-Schließen

### 3. Dependencies
- **Microsoft.Web.WebView2** (1.0.3650.58)

## 🚀 Wie starte ich die Anwendung?

### Option A: Mit dotnet run
```powershell
cd C:\Work\github-herrmann\hg-messe-app\server\messe-app
dotnet run
```

### Option B: Mit der EXE
```powershell
cd C:\Work\github-herrmann\hg-messe-app\server\messe-app
.\bin\Debug\net9.0-windows\messe-app.exe
```

### Option C: In der IDE
- Öffnen Sie `server.sln` in JetBrains Rider oder Visual Studio
- Setzen Sie `messe-app` als Startup-Projekt
- Drücken Sie F5 oder klicken Sie auf "Run"

## Was passiert beim Start?

1. ✅ Die App startet und zeigt "Server wird gestartet..."
2. ✅ Sucht nach `messe-server.exe` in bekannten Pfaden
3. ✅ Startet den Server im Hintergrund
4. ✅ Wartet, bis `http://localhost:5227` antwortet
5. ✅ Lädt die Swagger UI im WebView2
6. ✅ Status wird auf "Server läuft" mit grünem Indikator gesetzt

## 📋 Projektstruktur

```
server/
├── messe-server/              # ASP.NET Core Web API
│   ├── Controllers/
│   │   └── ArticlesController.cs
│   ├── Dto/
│   │   └── DtoArticle.cs
│   ├── Program.cs            # OpenAPI/Swagger konfiguriert
│   └── README.md
│
├── messe-app/                 # WPF WebView2 Client
│   ├── MainWindow.xaml       # UI mit WebView2
│   ├── MainWindow.xaml.cs    # Server-Start Logik
│   ├── App.xaml
│   ├── bin/Debug/net9.0-windows/
│   │   └── messe-app.exe     # ← FERTIGE ANWENDUNG
│   └── README.md
│
├── server.sln                 # Visual Studio Solution
└── README.md                  # Hauptdokumentation
```

## 🔧 Weitere Schritte

### Server erweitern
```powershell
cd messe-server
# Fügen Sie neue Controller in Controllers/ hinzu
# Swagger wird automatisch aktualisiert
```

### Client anpassen
- **URL ändern:** Bearbeiten Sie `_serverUrl` und `_swaggerUrl` in `MainWindow.xaml.cs`
- **UI anpassen:** Bearbeiten Sie `MainWindow.xaml`
- **Fenster-Größe:** Ändern Sie `Height` und `Width` in `MainWindow.xaml`

## ⚠️ Wichtige Hinweise

### Voraussetzungen
- ✅ .NET 9.0 SDK installiert
- ✅ Windows 10/11 (für WPF und WebView2)
- ✅ WebView2 Runtime (normalerweise vorinstalliert)

### Port 5227
- Der Server läuft standardmäßig auf Port 5227
- Falls der Port belegt ist, ändern Sie ihn in `messe-server/Properties/launchSettings.json`
- Vergessen Sie nicht, auch die URLs in `MainWindow.xaml.cs` anzupassen

### Server muss kompiliert sein
Die App sucht nach `messe-server.exe`. Stellen Sie sicher, dass der Server gebaut wurde:
```powershell
cd messe-server
dotnet build
```

## 📚 Dokumentation

Vollständige Dokumentation finden Sie in:
- [`server/README.md`](./README.md) - Gesamtübersicht
- [`messe-server/README.md`](./messe-server/README.md) - Server-Dokumentation
- [`messe-app/README.md`](./messe-app/README.md) - Client-Dokumentation

## 🎯 OpenAPI/Swagger

Der Server bietet nun vollständige OpenAPI-Unterstützung:
- ✅ **Swagger UI:** Interaktive API-Dokumentation
- ✅ **OpenAPI JSON:** Maschinenlesbare API-Spezifikation
- ✅ **API-Testing:** Direkt im Browser testen

**Swagger UI URL:** http://localhost:5227/swagger

## 🎉 Fertig!

Die Anwendung ist betriebsbereit. Führen Sie einfach `dotnet run` im messe-app-Verzeichnis aus, und alles wird automatisch gestartet!

**Viel Erfolg mit Ihrer Messe-Anwendung! 🚀**

