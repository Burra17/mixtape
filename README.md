# Rewind

Ett musikspel för sällskap där spelarna placerar låtar på en tidslinje efter utgivningsår. TV:n är spelplanen och mobilerna är handkontroller.

## Struktur

```
rewind/
├── backend/    .NET (Clean Architecture: Domain, Application, Infrastructure, Api) + SignalR
├── frontend/   Vite + React + TypeScript (HostView för TV, PlayerView för mobil)
└── docs/       Planering och beslut
```

## Teknik

- **Backend:** ASP.NET Core, SignalR, MediatR
- **Frontend:** React, TypeScript, Vite, Tailwind CSS, Zustand, @microsoft/signalr
- **Musik:** Spotify Web API (spellistor) + iTunes Search API (30 s ljudklipp)
