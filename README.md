# Todo API

The backend for a TODO list app, built with **ASP.NET Core 10 (.NET 10 LTS)** Minimal APIs.
You can list, add and delete TODO items. Data lives in memory, so it resets when the process
restarts. The Angular frontend lives in a separate repository.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (10.0.100 or later; pinned in [global.json](global.json))

## Running

```bash
dotnet restore
dotnet run --project src/TodoApi
```

The API listens on **http://localhost:5215**. To use HTTPS instead
(https://localhost:7051), run `dotnet dev-certs https --trust` once, then:

```bash
dotnet run --project src/TodoApi --launch-profile https
```

In Development you also get:

| URL | What it is |
| --- | --- |
| http://localhost:5215/scalar/v1 | Interactive API reference (Scalar) |
| http://localhost:5215/openapi/v1.json | OpenAPI 3.1 document |
| http://localhost:5215/health | Health check |

[src/TodoApi/TodoApi.http](src/TodoApi/TodoApi.http) has ready-made requests for the
VS Code REST Client, Visual Studio and Rider.

### Connecting the Angular frontend

CORS allows `http://localhost:4200` (the `ng serve` default). To allow other origins,
edit `Cors:AllowedOrigins` in [appsettings.json](src/TodoApi/appsettings.json), or override it
without touching the file:

```bash
Cors__AllowedOrigins__0=http://localhost:4300 dotnet run --project src/TodoApi
```

## Testing

```bash
dotnet test
```

Tests use xUnit v3 on Microsoft.Testing.Platform (turned on in `global.json`). To collect coverage:

```bash
dotnet test -- --coverage --coverage-output-format cobertura
```

## API

All endpoints are under `/api/todos`. Errors are returned as
[RFC 9457 Problem Details](https://www.rfc-editor.org/rfc/rfc9457).

| Method | Route | Body | Responses |
| --- | --- | --- | --- |
| `GET` | `/api/todos` | – | `200` array of items, oldest first |
| `GET` | `/api/todos/{id}` | – | `200` item · `404` |
| `POST` | `/api/todos` | `{ "title": "Buy milk" }` | `201` item + `Location` header · `400` validation problem |
| `DELETE` | `/api/todos/{id}` | – | `204` · `404` |

An item looks like this:

```json
{ "id": "01a15136-2ae9-70a4-a490-863a67d528e0", "title": "Buy milk", "createdAt": "2026-10-19T09:30:00+00:00" }
```

`title` is required, cannot be blank, can be at most 200 characters, and has surrounding
whitespace trimmed.

## Project layout

```
src/TodoApi/
  Domain/       TodoItem: the core model
  Data/         ITodoRepository + thread-safe InMemoryTodoRepository
  Services/     ITodoService / TodoService: application logic (ids, timestamps, trimming)
  Contracts/    Request/response DTOs (the HTTP contract, kept separate from the domain)
  Endpoints/    Minimal API route group for /api/todos
  Program.cs    Composition root: DI, validation, ProblemDetails, CORS, OpenAPI
tests/TodoApi.Tests/
  Unit/         Repository and service tests
  Integration/  End-to-end HTTP tests via WebApplicationFactory
```

## Design notes

- **Layering.** Requests flow from the endpoint to the service to the repository. Each layer
  depends only on an interface, so you can replace the in-memory store with a database
  (EF Core, for example) by registering a new `ITodoRepository`. Nothing else changes. The
  repository API is async for the same reason.
- **One project instead of several.** The domain is small, so layers are folders rather than
  separate assemblies. You can split them into projects later without changing any code.
- **Validation** uses the built-in Minimal API validation from .NET 10 (`AddValidation()`)
  with DataAnnotations on the request DTO. Invalid input gets a `400` with field-level errors.
  The service also checks its own inputs (defense in depth).
- **Testable time.** `TimeProvider` is injected, so unit tests use `FakeTimeProvider` and
  get deterministic timestamps.
- **Ids** are UUIDv7 (`Guid.CreateVersion7()`). They are sortable by time, which suits
  database keys later on.
- **Thread safety.** The repository is a singleton backed by `ConcurrentDictionary`.
- **Build hygiene.** Nullable reference types are on, warnings are treated as errors, code
  style is enforced in the build, and package versions are managed centrally
  ([Directory.Packages.props](Directory.Packages.props)).
- **CI.** A GitHub Actions workflow ([.github/workflows/ci.yml](.github/workflows/ci.yml))
  builds and tests every push and pull request.
