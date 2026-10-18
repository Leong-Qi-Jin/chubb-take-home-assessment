# Monorepo Structure

The repository contains one Angular SPA and one ASP.NET Core API. They remain independently buildable and deployable, while root tooling provides a consistent local and CI entry point.

## Layout

```text
.
├── apps/
│   ├── api/
│   │   ├── src/
│   │   │   └── Claims.Api/
│   │   │       ├── Features/
│   │   │       └── Claims.Api.csproj
│   │   └── tests/
│   │       ├── Claims.Api.UnitTests/
│   │       │   ├── Features/
│   │       │   └── Claims.Api.UnitTests.csproj
│   │       └── Claims.Api.IntegrationTests/
│   │           ├── Features/
│   │           └── Claims.Api.IntegrationTests.csproj
│   └── web/
│       ├── package.json
│       ├── package-lock.json
│       ├── angular.json
│       └── src/
├── docs/
│   ├── design/
│   └── reference/
├── Claims.slnx
├── global.json
├── .editorconfig
├── Directory.Build.props
└── docker-compose.yml
```

Application-owned folders are detailed in the [frontend](frontend-architecture.md) and [backend](backend-architecture.md) architecture documents.

## Responsibilities

- `apps/api` owns the API and its tests. Production and test folders use matching feature slices so a use case is easy to trace.
- Unit and integration tests are separate projects so database and web-host dependencies do not leak into fast unit tests.
- `apps/web` is a self-contained Angular workspace and owns its generated API types and HTTP adapters. Frontend tests stay beside their source files.
- `docs` contains the brief, design decisions, and supporting reference material.

## Tooling

The Angular package files stay in `apps/web`; a root npm workspace would add little while there is only one JavaScript package.

Docker Compose provides one-command startup for the SPA, API, and SQL Server.

## Boundaries

- The SPA communicates with the API only through HTTP, using generated OpenAPI types and handwritten adapters.
- Shared projects are introduced only when there is a demonstrated second consumer.
