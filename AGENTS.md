# Claims Platform

This repository contains the Chubb APAC claims take-home assessment: one Angular SPA, one ASP.NET Core API, and one SQL Server database.

## Core Technologies

- Angular 22, TypeScript, and Angular Material/CDK
- .NET 10, C# 14, ASP.NET Core Minimal APIs, and Entity Framework Core 10
- SQL Server 2025
- REST/HTTP with an OpenAPI-defined contract
- Vitest and Playwright for the frontend; xUnit for the backend
- Docker Compose for local SQL Server

## Project Information

Read the relevant documentation before making changes:

- [Assessment brief](docs/BRIEF.md) for requirements and evaluation context
- [Implementation plan](docs/PLAN.md) for scope and progress
- [Design documents](docs/design/) for the preliminary domain, API, application structure, and user flows
- [Contributing guide](CONTRIBUTING.md) for coding and testing conventions

The design documents are preliminary, not absolute. Use sound engineering judgment when they are unclear or mistaken, update them when implementation decisions change, and raise material changes to scope, architecture, or the public contract.

## Agent Guidelines

- Before modifying code, read and follow `CONTRIBUTING.md` and the relevant project documentation.
- Keep changes within the agreed MVP unless the task explicitly changes scope.
- Do not hand-edit generated files.
- Add or update focused tests for changed behaviour. Prefer meaningful behaviour and failure-path coverage over test counts or a coverage target.
- Update relevant documentation when behaviour, architecture, commands, or decisions change. Do not duplicate detailed guidance across documents.
- When a task is done, update [docs/PLAN.md](docs/PLAN.md) if its progress changed and append important prompts and decisions to [AI_JOURNAL.md](AI_JOURNAL.md). Skip trivial edits.
- Do not fix existing ESLint disable comments unless explicitly instructed.
- If satisfying a lint rule adds significant complexity, leave it unresolved and ask for direction. Treat complexity, nesting, and magic-number warnings the same way.
- Write comments as permanent documentation for future maintainers. Preserve existing comments during refactors unless they no longer apply.
- Never add secrets, credentials, local environment files, build output, coverage output, or editor-specific state to source control.
- Do not stage, unstage, commit, or push unless explicitly instructed. When asked to commit, use [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/).
