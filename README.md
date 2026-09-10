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

On first run the database file is created, the schema is migrated and demo data is seeded. Running again
does not duplicate it. To start over, stop the API and delete `api/src/Forum.Api/forum.db`.

## Demo accounts

Seeded on first run. Both roles are represented so moderator behaviour can be exercised.

| Username | Password | Role |
|---|---|---|
| `mod.jordan` | `Moderator1!` | Moderator |
| `alice` | `Password123!` | User |
| `ben` | `Password123!` | User |
| `chi` | `Password123!` | User |
| `dana` | `Password123!` | User |
| `eli`, `fay`, `gus` | `Password123!` | User |

The seed is deliberately shaped so the list features are demonstrable: 26 posts across 7 authors with an
uneven spread (`alice` has 11, enough to page a filtered result), deliberate ties in like count, nine posts
with no comments, and one with thirteen so comment paging has something to page.

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

The constraint violation is caught and translated to `409 duplicate-like` rather than surfacing as a 500.
Self-likes are a separate `409 self-like`, raised by the domain guard before any database work happens —
they are two distinct rules and the client can tell them apart.

The insert and the counter update share one transaction, and the counter moves with a set-based
`ExecuteUpdateAsync` rather than a tracked read-modify-write. Two concurrent likes on the same post would
otherwise both read the same starting value and write the same result, losing one.

`DELETE` is idempotent: unliking something you never liked returns `204`, because the caller asked for the
like to be absent and afterwards it is. The count is never driven negative.

### The post list is one query, and the ordering is total

Listing posts supports filtering by author, tag and date range, sorting by date or like count in either
direction, and paging. All of it resolves in **two SQL statements per request** — one `COUNT(*)` for the
total, one `SELECT` for the page — and that number does not grow with the rows, tags or comments returned.

Everything the response needs is inside a single projection, so EF Core compiles it to joins and correlated
subqueries rather than issuing a follow-up query per post. Captured from the running application with EF
command logging on:

```sql
SELECT "p1"."Id", "p1"."Title", ..., "u"."Username",
       (SELECT COUNT(*) FROM "Comments" AS "c" WHERE "p1"."Id" = "c"."PostId"),
       "s"."TagSlug", "s"."DisplayName"
FROM (
    SELECT "p"."Id", "p"."AuthorId", "p"."Body", "p"."CreatedAtUtc", "p"."LikeCount", "p"."Title"
    FROM "Posts" AS "p"
    WHERE EXISTS (SELECT 1 FROM "PostTags" AS "p0"
                  WHERE "p"."Id" = "p0"."PostId" AND "p0"."TagSlug" = @query_Tag)
    ORDER BY "p"."LikeCount" DESC, "p"."Id" DESC
    LIMIT @p2 OFFSET @p
) AS "p1"
INNER JOIN "Users" AS "u" ON "p1"."AuthorId" = "u"."Id"
LEFT JOIN ( ... "PostTags" INNER JOIN "Tags" ... ) AS "s" ON "p1"."Id" = "s"."PostId"
ORDER BY "p1"."LikeCount" DESC, "p1"."Id" DESC, ...
```

Note the `LIMIT`/`OFFSET` applies **inside** the subquery, before the joins, so paging limits how many post
rows are read rather than how many joined rows are returned.

To see this for yourself: run the API, request `/api/v1/posts?sort=likes&dir=desc`, and read the SQL from
the console — EF command logging is on in Development.

**`ORDER BY ... , "Id" DESC` is not decoration.** Like counts tie, and ties have no inherent order. Without
a unique final column the engine may return tied rows differently for the query behind page 1 and the query
behind page 2, so a post appears twice and another never appears at all. The seed contains deliberate ties
and a test pages the entire list one row at a time, asserting every post is seen exactly once and in the
same order as the unpaged result.

The two supporting indexes are `(CreatedAtUtc DESC, Id DESC)` and `(LikeCount DESC, Id DESC)` — they match
the sort expressions including the tiebreaker.

**Date ranges are half-open:** `from` is inclusive, `to` is exclusive. Adjacent ranges therefore tile
without double-counting the boundary row, and a test asserts that splitting the corpus at a timestamp
yields two sets whose sizes sum to the whole.

### Authentication is JWT, issued in-process

The brief requires authentication handled in the application rather than by an external provider.
Passwords are hashed with PBKDF2 via ASP.NET Core Identity's `PasswordHasher`; only that hashing primitive
is borrowed, and none of Identity's stores, managers or tables are used. The authentication logic stays in
this codebase where it can be read.

Tokens are stateless bearer JWTs, so the same credentials work identically for the web client and for a
third-party consumer hitting the API from Postman — which the brief requires as a first-class case.

**Login does not reveal whether an account exists.** Both failure modes return the same status, the same
body and the same message. Less obviously, both also cost the same: when the username is unknown the
password is still verified, against a fixed dummy hash. Returning early would make the unknown-user path
finish in microseconds while a real user costs around 100 ms of PBKDF2, and that difference is measurable
over a network. There is a unit test asserting the hasher is invoked even when no user was found.

**The registration endpoint never accepts a role.** New accounts are always regular users. Taking the role
from the request would let anyone register as a moderator and tag content.

*Rejected:* full ASP.NET Core Identity, which brings roughly seven tables and hides the authentication
logic being assessed. Also rejected: cookie sessions, which are arguably safer for a browser client but
awkward for the third-party API access the brief calls out.

*At production scale:* the signing key moves out of `appsettings.json` to an environment variable or secret
store. There is currently no rate limiting on the login endpoint.

### `LikeCount` is denormalised

Sorting by popularity is a stated requirement. Counting likes per post on every page load means a join and
aggregate across the whole result set; a column means an indexed read.

The counter is maintained with `ExecuteUpdateAsync` in the same transaction as the like row, never through
the change tracker — a tracked increment would read-modify-write and lose concurrent updates.

*Trade-off:* denormalised data can drift. It is written in exactly two places, both transactional.

## Status

In progress. Setup instructions, architecture notes and the Postman collection land as the corresponding
features do — see the commit history.
