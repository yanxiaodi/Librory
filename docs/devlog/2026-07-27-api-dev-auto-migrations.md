# 2026-07-27 API Dev Auto Migrations

- Moved database migration execution into API startup for the Development environment.
- The API now resolves `LibroryDbContext` after `app.Build()` and runs `Database.MigrateAsync()` before the request pipeline starts.
- Kept the change aligned with the Koviva pattern so local Aspire runs no longer depend on a separate manual migration step.
- Removed the duplicate migration call from the API test factory so tests and app startup share the same migration behavior.
- Verified the solution still builds after the startup change.
