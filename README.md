# Messe App

Trade-show barcode-scanning application for loading and inventory workflows.

## Sub-projects

| Path | Type | Purpose |
|---|---|---|
| `client/` | Angular 21 SPA | User interface |
| `server/messe-server/` | ASP.NET Core 9 | REST API + SignalR + serial scanner |
| `server/messe-app/` | WPF .NET 9 (Windows) | Desktop host |

## Quick Start

```bash
# API
cd server/messe-server && dotnet run

# Angular (dev)
cd client && npm start
```

## Documentation

Full technical documentation is in [`tech-doc/`](./tech-doc/):

- [`tech-doc/architecture.md`](./tech-doc/architecture.md) — Architecture, data model, API reference, workflows, deployment
