# Claims Platform

This repository contains an Angular SPA and an ASP.NET Core API for the Chubb APAC claims take-home assessment.

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
dotnet restore Claims.slnx
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

In a second terminal, select the pinned Node.js version and start the SPA:

```sh
nvm use
npm --prefix apps/web start
```

Open <http://localhost:4200>.
