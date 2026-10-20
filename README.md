# Claims Platform

This repository contains an Angular SPA and an ASP.NET Core API for the Chubb APAC claims take-home assessment.

![Officer dashboard: claim counts by status, outstanding liability by currency, and open claims per officer](docs/generated/ui/dashboard-laptop.png)

More screens at laptop and phone sizes are in [the UI screenshots](docs/generated/ui-screenshots.md).

## Prerequisites

- macOS or Linux
- [nvm](https://github.com/nvm-sh/nvm) with Node.js 26 and npm 11 installed
- .NET SDK 10.0.402 (pinned in `global.json`)
- Docker Desktop

The repository pins Node.js in `.nvmrc`. Select it in each new terminal before running npm commands:

```sh
nvm use
```

Trust the ASP.NET Core development certificate once:

```sh
dotnet dev-certs https --trust
```

## Install dependencies

From the repository root:

```sh
npm --prefix apps/web ci
npm --prefix tools/database-erd ci
npm --prefix tools/ui-screenshots ci
npm ci
dotnet restore Claims.slnx
dotnet tool restore
```

## Run SQL Server

Create the local environment file and start SQL Server 2025 Developer:

```sh
cp .env.example .env
docker compose up -d sqlserver
```

SQL Server listens on `localhost:1433` and persists its data in a Docker volume. Apple Silicon uses `linux/amd64` emulation because the SQL Server image is x86-64 only.

## Run the applications

Load the local environment and start the API:

```sh
set -a
. ./.env
set +a
dotnet run --launch-profile https --project apps/api/src/Claims.Api
```

The API runs at <https://localhost:5001> and redirects HTTP requests from <http://localhost:5000>.

In Development, startup applies migrations and seeds [demo data](docs/development-seed-data.md). Set `SeedData__Enabled=false` to skip seeding.

In a second terminal, select the pinned Node.js version and start the SPA:

```sh
nvm use
npm --prefix apps/web start
```

Open <http://localhost:4200>.

## Run the tests

Run the frontend tests:

```sh
npm --prefix apps/web test -- --watch=false
```

With SQL Server running and the local environment loaded, run the backend unit and integration tests:

```sh
set -a
. ./.env
set +a
dotnet test Claims.slnx
```

Integration tests create a temporary database on the local SQL Server and drop it afterwards.

## Lint and format

Lint and check formatting for the SPA, then for the rest of the repository (tools, Markdown, JSON, and YAML):

```sh
npm --prefix apps/web run lint
npm --prefix apps/web run format:check
npm run lint
npm run format:check
```

Use `lint:fix` and `format` to apply fixes.

## Generate the database ERD

With the local environment loaded and the database migrated, regenerate [the ERD](docs/generated/database-erd.md):

```sh
npm --prefix tools/database-erd run generate
```

## Generate the UI screenshots

With the API and the SPA running on the seeded database, regenerate [the UI screenshots](docs/generated/ui-screenshots.md) at laptop and phone sizes:

```sh
npx --prefix tools/ui-screenshots playwright install chromium
npm --prefix tools/ui-screenshots run generate
```

Run the install command only the first time. If the SPA is not at <http://localhost:4200>, set `UI_BASE_URL` to its URL.
