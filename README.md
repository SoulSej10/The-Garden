# The Garden

A fully autonomous, deterministic living world simulation.

Citizens are born, form settlements, develop economies, establish governance, found kingdoms, and record their history — all without player intervention. The simulation engine owns the world state. The Observatory observes; it never decides.

## Architecture

```
Observatory (React) ←→ Garden API (.NET) ←→ Simulation Engine ←→ PostgreSQL
```

| Layer | Technology |
|-------|-----------|
| Frontend | React 19, TypeScript 6, Vite 8, Tailwind CSS v4, shadcn/ui v4, TanStack Query 5, SignalR |
| API | .NET 10, ASP.NET Core, C#, SignalR hubs |
| Engine | Deterministic tick loop, scheduler, event bus, singleton world state |
| Storage | PostgreSQL via EF Core, file-based saves/backups |

## Quick Start

### Prerequisites
- [.NET SDK 10+](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org)
- [PostgreSQL 16+](https://www.postgresql.org/download/) (optional — app runs without DB)

### Local Development

```bash
# Backend
cd src/Garden.Api
dotnet run

# Frontend (separate terminal)
cd src/Garden.Observatory
npm install
npm run dev
```

Open `http://localhost:5173` in your browser.

### Docker (one command)

```bash
docker compose up -d
```

Services:
- **API** → `http://localhost:5088`
- **Observatory** → `http://localhost:5173`
- **PostgreSQL** → `localhost:5432`

## Project Structure

```
src/
├── Garden.Api/           ASP.NET Core controllers, hubs, services
├── Garden.Core/          Domain foundation (events, interfaces, IDs)
├── Garden.Contracts/     Shared contracts
├── Garden.Engine/        Simulation engine (systems, services, generation)
├── Garden.Infrastructure/ Persistence, DI, backups
├── Garden.Shared/        Shared utilities
├── Garden.World/         Domain entities, collections, world state
└── Garden.Observatory/   React frontend

tests/
└── Garden.UnitTests/     17 unit tests

blueprint/                Design documents (TG-DEV-001 through TG-DEV-009)
docker/                   Nginx config, infrastructure
```

## How It Works

1. **World Generation** — Deterministic seed-based (elevation → terrain → rivers → climate → resources)
2. **Time System** — Autonomous tick loop, tunable speed multiplier, scheduler
3. **Citizens** — Autonomous agents with needs (hunger, thirst, energy), personality traits, utility-based decisions, A* pathfinding
4. **Settlements** — Emerge when citizens choose to settle. Buildings, storage, territory expansion
5. **Economy** — Resource consumption, workshop production, trade
6. **History** — Append-only archive. Significance filtering. Template-based stories
7. **Civilization** — Emergent leadership, governance progression, kingdom formation, diplomacy, migration, trade routes, technology, culture, religion
8. **Observatory** — Real-time SignalR updates. 40+ read-only API endpoints across 12 controller groups
9. **Persistence** — Periodic auto-save (30s), manual named saves, hourly/daily/weekly backups

## Design Principles

- **Emergent, not scripted** — No "Create Kingdom" button. Kingdoms form from citizen interactions
- **Engine owns the world** — Singleton WorldState. API is read-only. Observatory observes
- **Immutable history** — Append-only archive. Facts don't change. Stories interpret facts
- **AI observes, never decides** — AI summarizes and narrates. Simulation remains deterministic.

## API Endpoints

| Prefix | Endpoints | Purpose |
|--------|-----------|---------|
| `/simulation` | status, start, pause, speed, step | Simulation control |
| `/world` | status, map, tiles/{x}/{y} | World state & map |
| `/environment` | weather, climate, resources, events | Environmental systems |
| `/citizens` | list, {id}, {id}/memories, population | Citizen tracking |
| `/settlements` | list, {id}, {id}/buildings, plan | Settlement management |
| `/economy` | resources | Economic overview |
| `/history` | list, timeline, search, stats | Historical records |
| `/stories` | list, {id} | Generated narratives |
| `/civilization` | summary, kingdoms, governments, leaders, diplomacy, trade-routes, technology, culture, religion, migration | Advanced society |
| `/dashboard` | summary, activity, performance | Dashboard data |
| `/diagnostics` | health | System diagnostics |
| `/search` | ?q=&limit= | Global search |
| `/system` | health, statistics, saves, save, load, backups | Admin operations |
| `/assistant` | summary, question | AI narration & Q&A |

## Configuration

```json
// appsettings.json
{
  "Simulation": {
    "TickDelayMs": 100,
    "DefaultSpeed": 5,
    "MaxSpeed": 100
  },
  "Persistence": {
    "SaveIntervalSeconds": 30,
    "AutoSave": true
  },
  "Backups": {
    "HourlyRetention": 24,
    "DailyRetention": 7,
    "WeeklyRetention": 4
  }
}
```

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `ASPNETCORE_ENVIRONMENT` | Development | Environment profile |
| `ASPNETCORE_URLS` | http://+:5088 | Bind address |
| `ConnectionStrings__DefaultConnection` | Host=localhost;Database=TheGarden;Username=postgres;Password=postgres | PostgreSQL connection |

## Tests

```bash
dotnet test tests/Garden.UnitTests/
```

17 tests covering: deterministic world generation (elevation, terrain, rivers, lakes, climate, biome, forest, resource placement), A* pathfinding, map seed reproducibility.

## Further Reading

All design documents are in `blueprint/development/`:
- TG-DEV-000: Engineering Standard
- TG-DEV-001 through TG-DEV-009: Phase specifications (v2.0 Completed versions include final reports)

## License

Proprietary (The Garden project)