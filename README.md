# Racinglazing ForumService

The community/discussion microservice for the Racinglazing motorsport platform.
It owns categories, forums, threads, comments, votes, tags, and reports. It does
**not** own users (UserService) or race data (RaceService) — it only stores
`AuthorId` and `RaceId` references and resolves the rest across service
boundaries.

This is the **MVP** scope from the API contract, built to be extended (v2
bookmarks/notifications/search, v3 Redis/real-time) without reshaping the core.

## Architecture

Clean Architecture, dependencies pointing inward:

```
Racinglazing.Forum.Api            → HTTP surface (MVC controllers), auth,
                                     error envelope, validation filter
Racinglazing.Forum.Application    → use-case services, ports (interfaces), DTOs
Racinglazing.Forum.Domain         → entities, value objects, ranking, invariants
Racinglazing.Forum.Infrastructure → EF Core + PostgreSQL, repositories, cursor
                                     pagination, external-service clients (stubs)
```

- **Domain** has no framework dependencies.
- **Application** depends only on Domain and declares interfaces (`IUnitOfWork`,
  `I*Repository`, `IUserProfileProvider`, `IRaceServiceClient`, `ICurrentUser`, …).
- **Infrastructure** implements those interfaces (EF Core, Npgsql, stubs).
- **Api** composes everything and exposes the endpoints.

### SOLID / extensibility highlights

- **SRP** — one service per use-case area; mapping, ranking, pagination, and
  enrichment are separate concerns.
- **OCP** — a new area = a new controller (auto-discovered by MVC). New votable
  entities implement `IVotable` and reuse the voting logic. New sort orders are
  an enum value + one query branch.
- **DIP** — cross-service integrations (UserService, RaceService) sit behind
  interfaces; today they're stubs, tomorrow they're HTTP clients + a read-model
  cache, with zero change to callers.

## Key design decisions (deviations from the contract's illustrative examples)

1. **UUID v7 ids** (`Guid.CreateVersion7()`) instead of `"thread-001"` strings —
   time-ordered, index-friendly, still serialized as strings on the wire.
2. **External refs (`AuthorId`, `RaceId`) stored as `text`**, never FKs — keeps
   us decoupled from UserService/RaceService id formats.
3. **Denormalized counters + a precomputed `hot_score`** on threads/comments,
   maintained transactionally on write, so every feed is a plain indexed
   `ORDER BY` — never an aggregate at read time.
4. **Keyset (cursor) pagination** everywhere lists appear — O(1) deep pages vs
   OFFSET's O(n). Cursors are opaque, URL-safe base64.
5. **Thread body = a root `Comment`** (`IsRoot = true`); top-level discussion
   comments are `parentCommentId = null, isRoot = false`, so the comments
   listing cleanly excludes the body.
6. **Soft delete** for threads/comments (tombstoned, tree preserved).

## Running locally

### 1. Database

The app connects with `Host=localhost;Database=forumservice;Username=forum;Password=forum`.

Either use Docker:

```bash
docker compose up -d
```

…or a local PostgreSQL (provision once, as a superuser):

```bash
sudo -u postgres psql -v ON_ERROR_STOP=1 <<'SQL'
CREATE ROLE forum LOGIN PASSWORD 'forum';
CREATE DATABASE forumservice OWNER forum;
SQL
```

### 2. Run

```bash
dotnet run --project src/Racinglazing.Forum.Api
```

On startup it applies EF migrations and seeds the MVP taxonomy. Swagger UI is at
`http://localhost:5080/swagger`. See [`requests.http`](requests.http) for sample calls.

### Authentication (MVP)

The API Gateway/UserService will issue JWTs later; the service is JWT-ready
(`ForumAuth:Authority`/`Issuer`/`Audience`). Until then, **development mode**
(`ForumAuth:AllowDevHeaders = true`) reads identity from headers:

- `X-User-Id: user-123`
- `X-User-Roles: moderator` (comma-separated; `moderator`/`admin` ⇒ moderator)

`AuthorId` is **always** taken from this identity, never from the request body.

### CORS

Browser origins are restricted with the `CORS_ALLOWED_ORIGINS` environment
variable. Supply a comma-separated allow-list; no wildcard origin or credentials
are enabled. For example:

```bash
CORS_ALLOWED_ORIGINS=https://raceservice-frontend-dev-dot-racingglazing.de.r.appspot.com,https://raceservice.example.com
```

The service allows API read/write methods (including `GET` and `OPTIONS`) with
`Content-Type` and `Authorization` headers for listed origins.

## Migrations

```bash
dotnet ef migrations add <Name> \
  --project src/Racinglazing.Forum.Infrastructure \
  --startup-project src/Racinglazing.Forum.Api \
  --output-dir Persistence/Migrations
```

## Not in the MVP (extension points already accounted for)

Bookmarks, following, notifications, reputation, global search, recommendations,
awards, AI moderation, real-time — all addable without changing the core model.
