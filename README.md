# dotnet-architecture-patterns

Reference kit for comparing modern .NET architecture patterns over the same Order Management / Commerce Operations domain.

![.NET](https://img.shields.io/badge/.NET-10-blue) ![license](https://img.shields.io/badge/license-MIT-green) ![architecture](https://img.shields.io/badge/patterns-clean%20%7C%20vertical%20slice%20%7C%20modular%20monolith%20%7C%20hexagonal-orange)

## Overview

The repository implements one fictitious commerce domain with Customers, Products, Orders, Order Items, Payments, Shipments, Inventory events, Domain Events, Audit Trail and Outbox. The goal is comparison: the same checkout flow is exposed through Clean Architecture, Vertical Slice, Modular Monolith and Hexagonal projects.

## Quick Start

```powershell
dotnet restore ArchitecturePatterns.slnx
dotnet build ArchitecturePatterns.slnx -c Release
dotnet run --project src/ArchitecturePatterns.Cli -- doctor --json
dotnet run --project src/ArchitecturePatterns.Cli -- run-scenario checkout --architecture clean --json
```

## APIs

Run one API and call the common endpoints:

```powershell
dotnet run --project src/CleanArchitecture/CleanArchitecture.Api
```

Endpoints: `GET /health/live`, `GET /health/ready`, `GET /architecture`, `POST /customers`, `POST /orders`, `POST /orders/{id}/items`, `POST /orders/{id}/confirm`, `POST /orders/{id}/payments`, `GET /orders/{id}`, `GET /outbox`, `POST /outbox/dispatch`, `GET /metrics`.

## CLI

`archtool` commands:

```powershell
dotnet run --project src/ArchitecturePatterns.Cli -- doctor
dotnet run --project src/ArchitecturePatterns.Cli -- list-patterns
dotnet run --project src/ArchitecturePatterns.Cli -- compare clean vertical-slice
dotnet run --project src/ArchitecturePatterns.Cli -- run-scenario checkout --architecture hexagonal --json
dotnet run --project src/ArchitecturePatterns.Cli -- validate-architecture
dotnet run --project src/ArchitecturePatterns.Cli -- generate-report --json
```

## Tests

The suite uses a tiny in-repo runner to avoid external dependencies and keep the starter portable.

```powershell
dotnet run --project tests/ArchitecturePatterns.UnitTests -c Release
dotnet run --project tests/ArchitecturePatterns.IntegrationTests -c Release
dotnet run --project tests/ArchitecturePatterns.ArchitectureTests -c Release
dotnet run --project tests/ArchitecturePatterns.ContractTests -c Release
dotnet run --project tests/ArchitecturePatterns.SecurityTests -c Release
```

## Docker

```powershell
docker compose config
docker compose build
docker compose up -d
```

## Pattern Matrix

| Pattern | Primary benefit | Cost | Project |
| --- | --- | --- | --- |
| Clean Architecture | Dependency rule and use-case isolation | More mapping and projects | `src/CleanArchitecture` |
| Vertical Slice | Feature-local change | Duplication risk | `src/VerticalSlice` |
| Modular Monolith | Explicit module boundaries | Boundary discipline | `src/ModularMonolith` |
| Hexagonal | Technology isolation through ports | Abstraction cost | `src/Hexagonal` |
| CQRS | Separate write/read intent | More handlers | `src/Patterns/Patterns.Cqrs` |
| Outbox | Reliable event publication | Dispatch complexity | `src/Patterns/Patterns.Outbox` |
| Specification | Reusable business predicates | Can be over-abstracted | `src/Patterns/Patterns.Specifications` |
| Result | Expected failures without exceptions | Requires mapping discipline | `src/Patterns/Patterns.Result` |

## Roadmap

- Add real SQLite adapter implementation behind the existing schema contract.
- Add generated OpenAPI snapshots for each API.
- Expand Docker smoke tests with containerized scenario calls.

## Release

Target release: `v1.0.0`. This repository does not publish packages to NuGet.org.
