# Messe Server & App Solution

Diese Solution enthält zwei Projekte:
1. **messe-server** - ASP.NET Core Web API mit OpenAPI/Swagger
2. **messe-app** - WPF Client-Anwendung mit WebView2

## Quick Start

### Option 1: Nur Server starten

```powershell
cd messe-server
dotnet run
```

Dann öffnen Sie im Browser: `http://localhost:5227/swagger`

### Option 2: Client-App starten (empfohlen)

```powershell
cd messe-app
dotnet run
```

Die App startet automatisch den Server und zeigt die Swagger-UI an.

## Projekte

### messe-server

ASP.NET Core 9.0 Web API Server mit:
- OpenAPI/Swagger-Unterstützung
- Articles Controller (Beispiel-API)
- Läuft auf: `http://localhost:5227`
- Swagger-UI: `http://localhost:5227/swagger`

[Weitere Details](./messe-server/README.md)

### messe-app

WPF .NET 9.0 Client-Anwendung mit:
- WebView2-Integration
- Automatischer Server-Start
- Integrierte Swagger-UI-Anzeige
- Status-Überwachung

[Weitere Details](./messe-app/README.md)

## Build & Run

### Gesamte Solution bauen

```powershell
dotnet build
```

### Einzelne Projekte bauen

```powershell
dotnet build messe-server/messe-server.csproj
dotnet build messe-app/messe-app.csproj
```

## Entwicklung

### API-Endpunkte

Die API stellt derzeit folgende Endpunkte bereit:

- `GET /Articles` - Liste aller Artikel

### Neue Controller hinzufügen

1. Erstellen Sie eine neue Controller-Klasse in `messe-server/Controllers/`
2. Fügen Sie DTO-Klassen in `messe-server/Dto/` hinzu
3. Der Swagger wird automatisch aktualisiert

## Architektur

```
┌─────────────────────────────────────────┐
│         messe-app (WPF Client)          │
│  ┌───────────────────────────────────┐  │
│  │      WebView2 (Swagger UI)        │  │
│  └───────────────────────────────────┘  │
└──────────────────┬──────────────────────┘
                   │ HTTP
                   ▼
┌─────────────────────────────────────────┐
│    messe-server (ASP.NET Core API)      │
│  ┌───────────────────────────────────┐  │
│  │     Controllers (REST API)        │  │
│  ├───────────────────────────────────┤  │
│  │  OpenAPI/Swagger Middleware       │  │
│  └───────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

## Technologie-Stack

- **.NET 9.0** - Framework
- **ASP.NET Core** - Web API
- **WPF** - Desktop UI
- **WebView2** - Embedded Browser
- **Swashbuckle** - OpenAPI/Swagger-Implementierung

## Requirements

- .NET 9.0 SDK
- Windows 10/11 (für WPF und WebView2)
- Visual Studio 2022 oder JetBrains Rider (optional)

## Deployment

### Development

Beide Projekte laufen im Debug-Modus direkt mit `dotnet run`.

### Production

1. Server publizieren:
   ```powershell
   cd messe-server
   dotnet publish -c Release -o ../publish/server
   ```

2. Client publizieren:
   ```powershell
   cd messe-app
   dotnet publish -c Release -o ../publish/app
   ```

3. Server-Binaries in Client-Verzeichnis kopieren:
   ```powershell
   Copy-Item -Recurse ../publish/server ../publish/app/messe-server
   ```

4. `messe-app.exe` verteilen

## Lizenz

[Ihre Lizenz hier]

