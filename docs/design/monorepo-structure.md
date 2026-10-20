# Monorepo Structure

The repository contains one Angular SPA and one ASP.NET Core API. They remain independently buildable and deployable, while root tooling provides a consistent entry point for local commands.

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
│   │       │   ├── Domain/
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

- `apps/api` owns the API and its tests. Test folders mirror the production folders they cover, so a use case is easy to trace.
- Unit and integration tests are separate projects so database and web-host dependencies do not leak into fast unit tests.
- `apps/web` is a self-contained Angular workspace and owns its generated API types and HTTP adapters. Frontend tests stay beside their source files.
- `docs` contains the brief, design decisions, and supporting reference material.

## Tooling

The Angular package files stay in `apps/web`, with their own ESLint and Prettier config. A small root package (not an npm workspace) lints and formats the rest of the repository: the Node.js tools, Markdown, JSON, and YAML. The .NET code uses `dotnet format`.

Docker Compose starts the local SQL Server. The SPA and API are started separately with `npm run start:web` and `npm run start:api`.

## Boundaries

- The SPA communicates with the API only through HTTP, using generated OpenAPI types and handwritten adapters.
- Shared projects are introduced only when there is a demonstrated second consumer.
