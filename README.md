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

## Status

In progress. Setup instructions, architecture notes and the Postman collection land as the corresponding
features do — see the commit history.
