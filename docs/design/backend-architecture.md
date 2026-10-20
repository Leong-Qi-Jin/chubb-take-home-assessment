# Backend and Database Architecture

## Decisions

Prefer the .NET platform defaults and add dependencies only when they remove meaningful work.

| Concern           | Choice                                                                 |
| ----------------- | ---------------------------------------------------------------------- |
| Runtime           | .NET 10 LTS, ASP.NET Core, and C# 14                                   |
| API style         | Minimal APIs with route groups and `TypedResults`                      |
| Architecture      | Single deployable with vertical feature slices                         |
| Domain            | `Claim` aggregate with explicit transition methods                     |
| Persistence       | EF Core 10 with the SQL Server provider                                |
| Database          | SQL Server 2025 Developer in Docker for local development              |
| Contract          | Built-in OpenAPI with request and response metadata                    |
| Validation        | Built-in request validation plus domain rules                          |
| Errors            | RFC 9457 Problem Details with stable application error codes           |
| Platform concerns | Development caller resolution; built-in .NET logging and configuration |

.NET 10 is the latest stable LTS release. Scaffold the solution, API, and tests with the official `dotnet new` templates, using the Minimal API default.

## Structure

Use vertical slices around API operations, not root-level Controllers, Services, and Repositories folders.

```sh
apps/api/
├── src/Claims.Api/
│   ├── Features/
│   │   ├── Claims/
│   │   │   ├── Submit/                     # One folder per operation
│   │   │   ├── List/
│   │   │   ├── Settle/
│   │   │   ├── …
│   │   │   ├── ClaimCommandRunner.cs       # Shared by the claim slices
│   │   │   └── ClaimResponses.cs
│   │   ├── Currencies/                     # One-operation features stay flat
│   │   ├── Dashboard/
│   │   └── Shared/                         # Response types used by several features
│   ├── Domain/
│   │   └── Claims/                         # Aggregate and workflow rules
│   ├── Infrastructure/
│   │   ├── Errors/                         # Problem Details and error codes
│   │   ├── Identity/                       # Development caller resolution
│   │   └── Persistence/
│   │       ├── Configurations/             # EF mappings
│   │       ├── Migrations/
│   │       ├── Seeding/
│   │       └── ClaimsDbContext.cs
│   ├── Program.cs                          # Composition and middleware
│   ├── appsettings.json
│   └── Claims.Api.csproj
└── tests/
    ├── Claims.Api.UnitTests/               # Mirrors Domain/ and Infrastructure/
    │   ├── Domain/
    │   └── Infrastructure/
    └── Claims.Api.IntegrationTests/        # Mirrors Features/
        ├── Features/
        └── Infrastructure/                 # Test host, users, and HTTP helpers
```

Each operation owns its endpoint, contract, validation, mapping, and use-case code. Keep small slices together and split them only as they grow. Shared workflow rules belong to the domain; EF Core and caller resolution belong to infrastructure.

## Request and workflow handling

Use route groups and pass the request `CancellationToken` through asynchronous work and EF Core calls.

Commands work through the `Claim` aggregate and save its state and history together. A shared command runner handles claim lookup, `If-Match`, and mapping workflow and concurrency failures to Problem Details. Queries project directly to response DTOs and never expose EF entities.

The `Claim` aggregate and database are enough to manage this workflow. Feature slices use EF Core directly, keeping the design simple for the MVP.

## Money

Use C# `decimal` and SQL Server `decimal(18,2)` for money, never binary floating-point types or SQL `money`. Amounts use the claim's ISO 4217 currency; supported currencies are listed in app settings (`Currencies:Supported`) and served by `GET /currencies`.

The API representation, precision limits, and currency handling remain defined in the [API contract](api-contract.md).

## EF Core and SQL Server

Use one scoped `ClaimsDbContext` and EF Core migrations. The schema, constraints, indexes, transactions, and concurrency rules remain defined in the [domain model](domain-model.md) and [API contract](api-contract.md).

Queries perform filtering, sorting, pagination, and dashboard aggregation in SQL.

## Local database and seed data

Docker Compose runs a pinned SQL Server 2025 Developer image with persistent local data. SQL Server containers are supported only on x86-64 Linux. This arm64 host must use unsupported `linux/amd64` emulation, so validate it early.

Use EF Core migrations and idempotent development seed data.

Seed multiple claimants and active officers, plus representative motor and property claims across the workflow. Include enough assignments, information requests, decisions, estimates, and history to demonstrate pagination and dashboard totals.

Seed data is for local development and demos; integration tests own their scenarios.

## API and platform concerns

Use built-in OpenAPI, Minimal API validation, Problem Details, and `ILogger`. Keep workflow rules in the domain, and consider FluentValidation only if request rules become complex or reused. The frontend generates types from the OpenAPI contract and keeps handwritten HTTP adapters.

## Platform concerns

- **Identity and authorization:** In development, a request header says who the caller is. Each endpoint states which role may use it, and the API also checks that claimants only see their own claims and officers only change claims assigned to them. Real sign-in can later replace the header check without changing the endpoints.
- **Logging and observability:** Use structured `ILogger` console logs with trace IDs; exclude personal data.
- **Configuration and secrets:** Use built-in .NET configuration and options, with secrets supplied through user secrets or environment variables, never source control or the SPA.

Production authentication, centralised telemetry, and managed secrets are deferred.

## Testing

Use xUnit, a mainstream .NET choice. Unit tests cover the domain workflow and rules; integration tests cover critical API and persistence behaviour. Prioritise important journeys and edge cases over a coverage target.

Integration tests run against a throwaway database on the local SQL Server, so they exercise real constraints and concurrency.

## Quality defaults

Pin the SDK with `global.json`. Keep nullable reference types and implicit usings enabled. Use the SDK's Roslyn analyzers in `Recommended` mode, enforce code style during builds, treat warnings as errors in the quality checks, and run `dotnet format --verify-no-changes`. Configure these in `Directory.Build.props` and `.editorconfig`; add third-party analyzers only for a demonstrated gap.
