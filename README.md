# dotnet-architecture-patterns

Reference kit for comparing modern .NET architecture patterns over the same Order Management / Commerce Operations domain.

![.NET](https://img.shields.io/badge/.NET-10-blue) ![license](https://img.shields.io/badge/license-MIT-green) ![tests](https://img.shields.io/badge/tests-146%20passing-brightgreen) ![coverage](https://img.shields.io/badge/coverage-95.22%25-brightgreen)

## Overview

The repository implements one fictitious commerce domain with Customers, Products, Orders, Order Items, Payments, Shipments, Inventory events, Domain Events, Audit Trail and Outbox. The same checkout flow is exercised through Clean Architecture, Vertical Slice, Modular Monolith and Hexagonal projects so the trade-offs are visible in code, tests and docs instead of only prose.

## Quick Start

```powershell
dotnet restore ArchitecturePatterns.slnx
dotnet build ArchitecturePatterns.slnx -c Release
dotnet test ArchitecturePatterns.slnx -c Release --collect:"XPlat Code Coverage" --results-directory TestResults
powershell -ExecutionPolicy Bypass -File tools/scripts/coverage-gate.ps1
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
dotnet run --project src/ArchitecturePatterns.Cli -- doctor --json
dotnet run --project src/ArchitecturePatterns.Cli -- list-patterns --json
dotnet run --project src/ArchitecturePatterns.Cli -- compare clean vertical-slice --json
dotnet run --project src/ArchitecturePatterns.Cli -- run-scenario checkout --architecture hexagonal --json
dotnet run --project src/ArchitecturePatterns.Cli -- validate-architecture --json
dotnet run --project src/ArchitecturePatterns.Cli -- validate-architecture --fail --json
dotnet run --project src/ArchitecturePatterns.Cli -- generate-report --json
```

Success commands return exit code `0`. The simulated architecture violation returns exit code `1` and a JSON error payload.

## Tests And Coverage

The primary evidence is `dotnet test`, not a custom console runner. Current local evidence: 146 passing tests.

| Category | Count |
| --- | ---: |
| Unit | 72 |
| Integration | 29 |
| Architecture | 23 |
| Contract/CLI | 12 |
| Security | 10 |

Coverage gate uses Cobertura output from `XPlat Code Coverage`, normalizes duplicate source paths across test projects, excludes only the internal `ArchitecturePatterns.Testing` helper assembly, and enforces total coverage >= 80% plus module thresholds.

| Module | Coverage |
| --- | ---: |
| ArchitecturePatterns.SharedKernel | 96.88% |
| archtool | 89.47% |
| CleanArchitecture.Application | 100% |
| Hexagonal.Application | 100% |
| Total | 95.22% |

## Docker

```powershell
docker compose config
docker compose build
docker compose up -d
curl --fail http://localhost:8080/health/live
docker compose down -v
```

The remote CI runs compose config, build, up, HTTP health smoke, CLI scenario smoke, and compose down.

## Pattern Matrix

| Pattern | Use when | Avoid when | Evidence |
| --- | --- | --- | --- |
| Clean Architecture | Dependency rules and use-case isolation matter | CRUD-only apps where projects add noise | Application use case, dependency tests, domain events |
| Vertical Slice | Teams ship feature-by-feature | Cross-cutting workflow dominates every feature | Endpoint parity and checkout scenario tests |
| Modular Monolith | You want module boundaries without distributed deployment | Independent scaling is already required | Host API, contracts/events docs, architecture tests |
| Hexagonal | Technology adapters must be replaceable | Ports would hide simple behavior | `ICheckoutUseCase`, InMemory adapter, SQLite schema boundary |
| CQRS | Write/read intent differs | Reads and writes are identical CRUD | Command/query contracts and CLI scenario reports |
| Outbox | Events must be persisted before dispatch | Eventual consistency is unnecessary | Pending/failed/published status tests and retry evidence |
| Specification | Business predicates repeat | One-off query is clearer | Pending/confirmed/customer/minimum/low-stock specs |
| Result | Expected failures are part of the domain | Truly exceptional faults | Validation/domain/not-found mappings and CLI/API responses |

## Structure

- `src/Shared`: domain model, patterns, observability and test utility assembly.
- `src/CleanArchitecture`: Domain, Application, Infrastructure and API composition root.
- `src/VerticalSlice`: API organized around endpoint slices for the same flow.
- `src/ModularMonolith`: host API representing Customers, Orders, Inventory and Payments module boundaries.
- `src/Hexagonal`: domain/application ports plus InMemory, SQLite schema and HTTP adapters.
- `src/Patterns`: focused pattern packages for CQRS, Outbox, Specifications, Result and Repositories.
- `tests`: xUnit suites for unit, integration, architecture, contract/CLI and security evidence.
- `docs`: architecture notes, Mermaid diagrams, ADRs and final audit.

## Release

Release `v1.0.0`: https://github.com/diwb/dotnet-architecture-patterns/releases/tag/v1.0.0

Assets: `archtool.zip`, `RELEASE_NOTES.md`, `SHA256SUMS`. This repository does not publish packages to NuGet.org.
