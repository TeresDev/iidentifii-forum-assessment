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

## Running the web client

With the API already running, in a second terminal:

```bash
cd web
npm ci
npm start
```

The client is served on **http://localhost:4200** and talks to the API over HTTP only.

Use `npm ci`, not `npm install`. npm 10.x can crash resolving this dependency tree from scratch
(`Cannot read properties of null (reading 'edgesOut')`); the committed lockfile avoids it.

## Running the tests

```bash
cd api
dotnet test              # 77 tests: domain rules, application services, API integration

cd ../web
npm test                 # 10 specs: API client contract, auth interceptor, session restore
npm run e2e              # 12 end-to-end journeys in a real browser
```

`npm run e2e` starts the API and the web client itself, so nothing needs to be running first.
Browsers are downloaded once with `npx playwright install chromium`.

The suites are deliberately layered rather than overlapping. Each business rule is asserted at exactly one
level: the self-like guard as a domain unit test, constant-time login as an application unit test with a
mocked hasher, the database constraints and every endpoint as API integration tests, and the user journeys
end to end. Nothing is asserted three times in three places.

## API documentation (Postman)

**Published collection:** https://documenter.getpostman.com/view/14578065/2sBYAysTps

`postman/iidentifii-forum.postman_collection.json` — 19 requests across all 13 endpoints, with 25
assertions. Import it into Postman, or run it headlessly:

```bash
npx newman run postman/iidentifii-forum.postman_collection.json
```

Start the API first, then run **Auth / Login as a user** — the bearer token is captured into a collection
variable automatically, so every authenticated request below it works without copying anything by hand.
**Auth / Login as a moderator** does the same for the Moderation folder.

The **Failure cases** folder is the interesting one. It exercises the rules that are easy to get wrong:
liking your own post, a regular user attempting moderation with a perfectly valid token, and the two login
failures that must be indistinguishable.

The API also serves an OpenAPI document at `http://localhost:5080/openapi/v1.json` while running.

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

*Rejected:* SQL Server and PostgreSQL. SQL Server is the likelier production target for an ASP.NET Core
stack, PostgreSQL the better story for concurrency, but both need a running instance or a container — and
LocalDB is Windows-only, so the assessor's platform would decide whether the clone runs at all.

*At production scale:* move to SQL Server or PostgreSQL. The model and the queries are provider-agnostic —
no provider-specific SQL and no raw queries — so the code change is the provider registration and the
connection string. The committed migration is SQLite-generated and would be regenerated against the target
provider; nothing in the schema itself assumes SQLite. SQLite serialises writers, which is fine for a demo
and wrong for a forum with real traffic.

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

### Front-end state is signals in a service, not a store library

Each screen's state is three signals — data, loading, error — held in an injectable service, with derived
values as `computed`. Components read them and render; they do not hold copies.

There is no state shared between unrelated features, nothing to replay, and no reducer indirection worth
the ceremony at this size. NgRx would add actions, reducers, effects and selectors to express what is
currently one `subscribe` and three `set` calls. Introducing it later is a local change, because components
already read from the service rather than from HTTP.

Angular 21 is zoneless, so change detection runs off signal updates rather than monkey-patched async
callbacks. Components are `OnPush`, which in a signal-driven app is the natural default rather than an
optimisation.

**The client's query-parameter names are pinned by a test.** The API ignores unrecognised parameters and
still returns `200` with the full unfiltered list, so a typo — `authorId` for `author`, `direction` for
`dir` — produces a page that looks like it works while every filter silently does nothing. The spec asserts
the exact query string the client emits, which is the only thing that catches it.

### Moderation is enforced by the server, not by the UI

Tagging is guarded by one named authorization policy applied to the whole controller, rather than role
checks repeated inside each action. There is a single place to read it and a single place to change it.

The web client hides the tag control from non-moderators, but that is presentation. The control is the
policy, and the distinction is observable:

```
anonymous                     → 401   we do not know who you are
regular user, valid token     → 403   we do, and you may not
moderator                     → 201
```

That middle case is the one that matters — a third-party consumer holding a perfectly valid regular-user
token, calling the endpoint directly and bypassing the browser entirely. There is an integration test for
it, because it is the difference between an access rule and a hidden button.

The tag vocabulary is closed and seeded: applying a slug that is not in it returns `404`. Moderators flag
content against defined categories rather than inventing them at the point of moderation, which is what
makes the regulatory motivation in the brief meaningful. Each application records who tagged and when.

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

**The client keeps the session in `localStorage`, and that is the weakest decision here.** It survives a
page refresh, which matters for anything usable. It is also readable by any JavaScript running on the page,
so a successful XSS can exfiltrate the token — and unlike a cookie, `HttpOnly` cannot protect it.

The better arrangement is a short-lived access token held only in memory plus an `HttpOnly`, `SameSite`
refresh cookie: it survives refresh without ever exposing the token to script. That costs a refresh
endpoint, rotation, and a CSRF consideration on the refresh call. It was not built here, deliberately, and
it is the first thing I would add.

Two things narrow the window in the meantime. The interceptor attaches the token **only to this API's
origin**, so it is never sent to a third-party host — there is a test for that, because an interceptor that
attaches unconditionally hands the session to every domain the app ever calls. And expiry is checked when
the session is restored as well as when it is used, so a stale token is discarded on load rather than
replayed into a wall of 401s.

*At production scale:* the signing key moves out of `appsettings.json` to an environment variable or secret
store. There is currently no rate limiting on the login endpoint.

### `LikeCount` is denormalised

Sorting by popularity is a stated requirement. Counting likes per post on every page load means a join and
aggregate across the whole result set; a column means an indexed read.

The counter is maintained with `ExecuteUpdateAsync` in the same transaction as the like row, never through
the change tracker — a tracked increment would read-modify-write and lose concurrent updates.

*Trade-off:* denormalised data can drift. It is written in exactly two places, both transactional.

## What it does

| Flow | Who |
|---|---|
| Browse posts with filters (author, tag, date range), sorting (date, likes) and paging | Anyone |
| Read a post and its comments, paged | Anyone |
| Register and sign in | Anyone |
| Create a post | Signed in |
| Comment on a post | Signed in |
| Like and unlike a post — once per post, never your own | Signed in |
| Tag a post as misleading or false information, and untag it | Moderators |

Write actions are hidden from users who cannot perform them, but hiding is presentation only — every rule
is enforced server-side and there are tests calling the endpoints directly to prove it.

## Known limitations and trade-offs

Scoped to what the brief asks for. It took longer than the stated six hours — the QA section says every
flow is verified rather than just the happy path, and I was not willing to ship required flows untested.
What follows is what I would have cut for a strict six-hour version, and what I left out deliberately.

**The session token lives in `localStorage`.** Readable by any script on the page, and unlike a cookie it
cannot be made `HttpOnly`. The production answer is a short-lived in-memory access token plus an `HttpOnly`
refresh cookie. That costs a refresh endpoint, rotation and a CSRF consideration; it is the first thing I
would add. Mitigations in place: the interceptor attaches the token only to this API's origin, and expiry
is checked on restore as well as on use.

**SQLite serialises writers.** Correct for a demo an assessor runs from a clone, wrong for a forum with
real traffic. The model and the queries are provider-agnostic — no raw SQL, no provider-specific
constructs — so moving to SQL Server or PostgreSQL is a provider registration, a connection string and a
migration regenerated against that provider.

**No rate limiting on `/auth/login`.** Password guessing is only slowed by PBKDF2. Per-IP and per-account
limits belong here.

**The JWT signing key is committed** in `appsettings.json` so the project runs from a clone with no setup.
In any real deployment it comes from an environment variable or secret store.

**Roles are seeded, not administered.** `mod.jordan` is the only moderator, created on first run.
Registration hardcodes the regular role — accepting one from the request would let anyone register as a
moderator and tag content. A real deployment needs an admin-only endpoint, or a claim from whatever identity
provider the organisation already runs, so the forum never owns role assignment at all. The related catch is
that the role is a claim inside the token, so a change to it takes effect only when that token expires, up
to eight hours later. Promotion is harmless. Demotion is not: a moderator you strip keeps the power until
then. The short-lived access token described above closes both.

**Offset paging, not keyset.** `LIMIT/OFFSET` degrades on deep pages because the database still walks the
skipped rows. Fine at 26 posts; at a million, page 10,000 is slow. Keyset paging on `(sortKey, Id)` is the
fix, and the indexes are already shaped for it.

**Posts and comments cannot be edited or deleted.** Not in the brief, so not built — but a real forum needs
both, plus an ownership check and a moderation trail.

**What I would cut for six hours:** the end-to-end suite, the Postman failure-case folder, half the
integration tests, and this document. The domain rules, the list query and the authorisation tests would
stay — they are the parts that would be embarrassing to get wrong.

## Troubleshooting

**`npm install` fails with `Cannot read properties of null (reading 'edgesOut')`.** An npm 10.x resolver
bug on this dependency tree. Use `npm ci`, which is what the lockfile is committed for.

**`npx playwright install` times out downloading Chromium.** Usually a machine with a broken IPv6 route
where DNS still advertises an AAAA record: Playwright resolves IPv6-first and its 5-second attempt timeout
fires before any fallback, then reports it as a socket timeout, which is misleading. Confirm with
`curl -6 https://cdn.playwright.dev/` — if that hangs while `curl -4` succeeds, that is the cause.

## Status

Complete. All flows in the brief are implemented, tested and documented.
