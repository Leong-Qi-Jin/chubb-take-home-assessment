# Claims Platform

This repository contains an Angular SPA and an ASP.NET Core API for the Chubb APAC claims take-home assessment.

## Prerequisites

- Node.js 26 and npm 11
- .NET SDK 10.0.402 (pinned in `global.json`)

The repository includes an `.nvmrc` file. If you use NVM, select the pinned Node.js version before running npm commands:

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

## Run the applications

Start the API:

```sh
dotnet run --launch-profile https --project apps/api/src/Claims.Api
```

The API runs at <https://localhost:5001> and redirects HTTP requests from <http://localhost:5000>.

In a second terminal, start the SPA:

```sh
npm --prefix apps/web start
```

Open <http://localhost:4200>.
