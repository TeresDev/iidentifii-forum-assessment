# iiDENTIFii Forum

A small full-stack forum: an ASP.NET Core REST API over a SQLite datastore, and an Angular web client that
consumes it. Users browse anonymously, and log in to post, comment and like. Moderators can tag posts as
misleading or false information.

Built for the iiDENTIFii Senior Full Stack Engineer technical assessment.

## Stack

| Layer | Choice |
|---|---|
| API | ASP.NET Core 10 (.NET 10) |
| Datastore | SQLite via EF Core 10 |
| Web | Angular 21 (LTS) |
| API tests | xUnit + Moq |
| Web tests | Vitest |
| End-to-end | Playwright |

## Prerequisites

- **.NET SDK 10.0.302** or later — `dotnet --version`
- **Node.js** `>= 20.19`, `>= 22.12`, or `>= 24` — `node --version`

No database server is required. SQLite runs in-process and the database file is created and seeded on
first run.

## Running the API

```bash
cd api
dotnet run --project src/Forum.Api
```

The API listens on **http://localhost:5080**. HTTP only — there is no HTTPS profile, so no
`dotnet dev-certs` step is needed.

## Running the tests

```bash
cd api
dotnet test
```

## Repository layout

```
api/        ASP.NET Core solution
  src/Forum.Domain           entities and business rules — no dependencies
  src/Forum.Application      use-case services and port interfaces
  src/Forum.Infrastructure   EF Core, security and other outward adapters
  src/Forum.Api              controllers and composition root
  tests/                     unit and integration tests
web/        Angular client
postman/    Public Postman collection
```

Dependencies point inward: `Domain ← Application ← Infrastructure`, with `Api` composing them.
`Forum.Application` has no EF Core reference, so persistence cannot leak into business logic.

## Design decisions

Grows as decisions are made. Each records what was chosen, what was rejected, and what would change at
production scale.

### SQLite rather than a database server

The assessor has to run this from the README alone, so every external dependency is a way for that to
fail. SQLite runs in-process, needs no server, no container and no connection-string editing, and the
database is created and seeded on first run.

The model is relational and stays that way — real foreign keys, real indexes, and a composite primary key
enforcing one-like-per-user. Nothing about the schema assumes SQLite.

*Rejected:* PostgreSQL, which is the production answer and would be a better story for concurrency. It
needs a running server or Docker, which is a prerequisite the assessor may not have.

*At production scale:* move to PostgreSQL. Persistence is provider-agnostic — no provider-specific SQL and
no raw queries — so it is a change of provider registration and connection string. SQLite serialises
writers, which is fine for a demo and wrong for a forum with real traffic.

### Four projects, dependencies pointing inward

`Forum.Domain ← Forum.Application ← Forum.Infrastructure`, with `Forum.Api` composing them.

This is checkable rather than aspirational: `Forum.Domain` has no package references at all, and
`Forum.Application` has no reference to EF Core. You cannot write a database query inside a use-case
service, because neither the type nor the field exists on that reference graph. The compiler enforces the
layering.

*Rejected:* a single project, which is defensible at this size but does not survive growth — once
persistence concerns leak into business logic they are expensive to separate again.

### One like per user is enforced by the database

`Like` is keyed on `(PostId, UserId)` rather than a surrogate id.

Checking for an existing like and then inserting is a race: two concurrent requests both pass the check
and both insert. The composite primary key makes the second insert fail at the database, which is the only
place the guarantee can actually be made. The application also checks first, so the common case returns a
clean error rather than an exception — but the key is the control, not the check.

### `LikeCount` is denormalised

Sorting by popularity is a stated requirement. Counting likes per post on every page load means a join and
aggregate across the whole result set; a column means an indexed read.

The counter is maintained with `ExecuteUpdateAsync` in the same transaction as the like row, never through
the change tracker — a tracked increment would read-modify-write and lose concurrent updates.

*Trade-off:* denormalised data can drift. It is written in exactly two places, both transactional.

## Status

In progress. Setup instructions, architecture notes and the Postman collection land as the corresponding
features do — see the commit history.
