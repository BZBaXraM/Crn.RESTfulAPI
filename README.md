# CRN — Products API

A RESTful CRUD API for Products (and their related Items), built in ASP.NET Core with a clean,
layered architecture, JWT authentication with refresh-token rotation, and role-based authorization.

This is a technical-assessment submission. The full requirements are in
[`RESTful Backend API Solution - Technical Assessment.md`](./RESTful%20Backend%20API%20Solution%20-%20Technical%20Assessment.md).

## Tech stack

| Concern | Choice |
|---|---|
| Framework | .NET 10, ASP.NET Core Web API |
| Database | SQL Server, via EF Core |
| Auth | JWT access tokens + rotating refresh tokens |
| Validation | FluentValidation |
| API docs | Swagger / OpenAPI (Swashbuckle) |
| Testing | xUnit, Moq, `WebApplicationFactory`, SQLite in-memory |
| Containerization | Docker, Docker Compose |

## Architecture

Four projects under `src/`, dependencies pointing inward:

```
API  ─┬─▶ Application ──▶ Domain
      └─▶ Infrastructure ──▶ Application, Domain
```

- **Domain** — `Product`, `Item`, `User`, `RefreshToken` entities; the `Role` enum; domain exceptions
  (`NotFoundException`, `ConflictException`, `AuthenticationFailedException`, `ValidationException`).
  No package dependencies.
- **Application** — DTOs, service interfaces, `ProductService` / `ItemService` / `AuthService`,
  FluentValidation validators, and mapping extension methods. All business logic lives here; nothing
  in this layer knows about HTTP or SQL Server.
- **Infrastructure** — `ApplicationDbContext`, EF Core entity configurations, repositories, the unit
  of work, the JWT token service, the password hasher, and the startup admin-user seeder.
- **API** — controllers, middleware, the FluentValidation action filter, Swagger setup, and
  `Program.cs`. This is the only layer that knows about HTTP.

Three test projects under `tests/` mirror this: `Application.Tests` (unit, Moq), `Infrastructure.Tests`
(against a real EF Core provider — SQLite in-memory — not a fake), and `API.Tests` (full HTTP
round-trips via `WebApplicationFactory`).

## Getting started

### Option A — Docker Compose (API + SQL Server, no local .NET SDK needed)

```bash
cp .env.example .env      # fill in SA_PASSWORD, JWT_SIGNING_KEY, Seed_Admin_* — see comments in the file
docker compose up --build
```

The API applies pending EF Core migrations and seeds the admin account automatically on first boot —
no manual `dotnet ef database update` step. Once both containers report healthy:

```bash
curl http://localhost:8080/health
curl -X POST http://localhost:8080/api/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"userName":"admin","password":"<SEED_ADMIN_PASSWORD from your .env>"}'
```

`docker compose down` stops the stack; add `-v` to also drop the database volume.

### Option B — Local .NET SDK

Requires the .NET 10 SDK and a reachable SQL Server instance.

```bash
# Local SQL Server via Docker, if you don't have one:
docker run -d --name crn-sql --platform linux/amd64 -e ACCEPT_EULA=Y \
  -e 'MSSQL_SA_PASSWORD=Dev_Passw0rd!' -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest

dotnet ef database update --project src/Infrastructure --startup-project src/API
dotnet run --project src/API
```

`appsettings.Development.json` points at that container and seeds `admin` / `Admin123!` on first run.
The API listens on `http://localhost:5085` (and `https://localhost:7220`); Swagger UI is at `/swagger`
in the Development environment.

## API surface

All routes are versioned under `/api/v1`. Requests and responses are JSON; errors follow RFC 7807
(`application/problem+json`).

| Method | Route | Auth | Description |
|---|---|---|---|
| POST | `/api/v1/auth/register` | anonymous | Create a new user (`Role.User`) |
| POST | `/api/v1/auth/login` | anonymous | Exchange credentials for an access + refresh token |
| POST | `/api/v1/auth/refresh` | anonymous | Rotate a refresh token for a new pair |
| POST | `/api/v1/auth/revoke` | anonymous | Revoke a refresh token (logout) |
| GET | `/api/v1/products` | any authenticated user | Paginated product list |
| GET | `/api/v1/products/{id}` | any authenticated user | Get one product |
| POST | `/api/v1/products` | Admin | Create a product |
| PUT | `/api/v1/products/{id}` | Admin | Update a product |
| DELETE | `/api/v1/products/{id}` | Admin | Delete a product (cascades to its items) |
| GET | `/api/v1/products/{productId}/items` | any authenticated user | Paginated items for a product |
| GET | `/api/v1/products/{productId}/items/{itemId}` | any authenticated user | Get one item |
| POST | `/api/v1/products/{productId}/items` | Admin | Add an item to a product |
| PUT | `/api/v1/products/{productId}/items/{itemId}` | Admin | Update an item |
| DELETE | `/api/v1/products/{productId}/items/{itemId}` | Admin | Remove an item |
| GET | `/health` | anonymous | Liveness/readiness probe (checks the database) |

Send the access token as `Authorization: Bearer <token>`. Collection endpoints accept
`?pageNumber=&pageSize=` (page size capped at 100).

### Authentication flow

- **Access tokens** are short-lived (15 minutes by default) HMAC-signed JWTs carrying the user's id,
  name, email, and role.
- **Refresh tokens** are long-lived (7 days by default), opaque, single-use, and rotate on every
  `/auth/refresh` call: the old token is revoked and a new pair is issued. Only the SHA-256 hash of a
  refresh token is ever stored — the raw value is returned to the client once.
- **Reuse detection**: presenting a refresh token that has already been rotated out is treated as a
  sign of theft. It revokes every other active refresh token for that user, not just the one presented,
  forcing a fresh login everywhere.

## Testing

```bash
dotnet test CRN.slnx
```

83 tests across three projects, none of which need a real SQL Server — `Infrastructure.Tests` and
`API.Tests` both run against SQLite in-memory.

```bash
dotnet test tests/Application.Tests                              # run a single test project
dotnet test --filter "FullyQualifiedName~ProductServiceTests"    # run a single test class
```

## Project layout

```
src/
  API/             ASP.NET Core Web API — controllers, middleware, Program.cs
  Application/     DTOs, service interfaces & implementations, validators
  Domain/          Entities, enums, exceptions
  Infrastructure/  EF Core, repositories, JWT/password services, migrations
tests/
  API.Tests/             Integration tests (WebApplicationFactory)
  Application.Tests/     Unit tests (Moq)
  Infrastructure.Tests/  Data-layer tests (SQLite in-memory)
docker-compose.yml       API + SQL Server, for local/demo use
src/API/Dockerfile       Multi-stage build for the API image
```
# Crn.RESTfulAPI
