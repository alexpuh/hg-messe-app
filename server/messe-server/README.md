# Messe Server - ASP.NET Core Web API

REST API Server mit OpenAPI/Swagger-Unterstützung für die Messe-Anwendung.

## Features

- ✅ ASP.NET Core 9.0 Web API
- ✅ OpenAPI-Spezifikation (Microsoft.AspNetCore.OpenApi)
- ✅ Swagger UI (Swashbuckle.AspNetCore)
- ✅ Articles API (Beispiel-Implementation)
- ✅ Läuft auf HTTP (Port 5227)

## Quick Start

```powershell
dotnet run
```

Dann öffnen Sie im Browser:
- Swagger UI: http://localhost:5227/swagger
- OpenAPI JSON: http://localhost:5227/swagger/v1/swagger.json
- Alternative OpenAPI: http://localhost:5227/openapi/v1.json

## API-Endpunkte

### Articles

#### GET /Articles
Gibt eine Liste aller Artikel zurück.

**Response:**
```json
[
  {
    "id": 1,
    "name": "Test",
    "arNr": "1234567890"
  },
  {
    "id": 2,
    "name": "Test2",
    "arNr": "1234567891"
  },
  {
    "id": 3,
    "name": "Test3",
    "arNr": "1234567892"
  }
]
```

## Projektstruktur

```
messe-server/
├── Controllers/           # API Controller
│   └── ArticlesController.cs
├── Dto/                   # Data Transfer Objects
│   └── DtoArticle.cs
├── Properties/
│   └── launchSettings.json  # Launch-Konfiguration
├── appsettings.json       # App-Konfiguration
├── Program.cs             # Entry Point & Middleware-Setup
└── messe-server.csproj    # Projekt-Datei
```

## Konfiguration

### Port ändern

In `Properties/launchSettings.json`:

```json
{
  "profiles": {
    "http": {
      "applicationUrl": "http://localhost:5227"
    }
  }
}
```

### OpenAPI/Swagger konfigurieren

In `Program.cs`:

```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Messe Server API",
        Version = "v1",
        Description = "API für die Messe-Anwendung"
    });
});
```

## Entwicklung

### Neuen Controller hinzufügen

1. Erstellen Sie eine neue Datei in `Controllers/`:

```csharp
using Microsoft.AspNetCore.Mvc;

namespace Herrmann.MesseApp.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class MyController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { message = "Hello World" });
    }
}
```

2. Der Swagger aktualisiert sich automatisch!

### DTO hinzufügen

Erstellen Sie eine neue Datei in `Dto/`:

```csharp
namespace Herrmann.MesseApp.Server.Dto;

public class DtoMyEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
```

## Dependencies

- **Microsoft.AspNetCore.OpenApi** (9.0.11) - OpenAPI-Support
- **Swashbuckle.AspNetCore** (7.2.0) - Swagger UI und Generator

## Build & Run

### Development

```powershell
dotnet run
```

### Production Build

```powershell
dotnet build -c Release
```

### Publish

```powershell
dotnet publish -c Release -o ./publish
```

## Umgebungen

### Development
- Swagger UI ist aktiviert
- Detaillierte Fehlerseiten
- Environment: `Development`

### Production
- Swagger UI ist deaktiviert (aus Sicherheitsgründen)
- Minimale Fehlerinformationen
- Environment: `Production`

## Testing

### Mit Swagger UI
1. Starten Sie den Server mit `dotnet run`
2. Öffnen Sie http://localhost:5227/swagger
3. Testen Sie die Endpunkte direkt im Browser

### Mit HTTP-Datei (JetBrains Rider)
Verwenden Sie die Datei `TestRequests/messe-server.http`:

```http
GET http://localhost:5227/Articles
Accept: application/json
```

### Mit curl

```bash
curl http://localhost:5227/Articles
```

### Mit PowerShell

```powershell
Invoke-RestMethod -Uri "http://localhost:5227/Articles" -Method Get
```

## Logging

Das Standard-ASP.NET Core Logging ist aktiviert. Logs werden in der Console ausgegeben.

## CORS

Aktuell ist CORS nicht konfiguriert. Für Frontend-Entwicklung kann CORS in `Program.cs` hinzugefügt werden:

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ...

app.UseCors();
```

## Lizenz

[Ihre Lizenz hier]

