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

## Status

In progress. Setup and run instructions, architecture notes and the Postman collection land as the
corresponding features do — see the commit history.

## Repository layout

```
api/        ASP.NET Core solution
web/        Angular client
postman/    Public Postman collection
docs/       Architecture decisions and design rationale
```
