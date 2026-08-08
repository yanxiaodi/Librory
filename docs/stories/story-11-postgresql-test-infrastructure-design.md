# Story 11 Design: PostgreSQL Test Infrastructure

## Goal

Give the backend a repeatable integration-test harness that exercises the API against a real PostgreSQL database instead of only an in-memory provider.

## Scope

In scope:

- Real PostgreSQL-backed integration tests
- Automatic database setup and teardown
- Migration and schema smoke checks

Out of scope:

- New product behavior
- API surface changes

## Design

Keep the test harness focused on provider realism.

### Harness behavior

The API test project should boot against a real PostgreSQL instance and provision an isolated database automatically for the test run.

### Schema checks

Apply EF Core migrations as part of the test fixture setup so the generated model stays trustworthy.

## Behavior

- Tests run without manual database preparation
- At least one integration test proves the API works end-to-end against PostgreSQL

## Testing

Add coverage for:

- booting against PostgreSQL
- applying migrations in test setup
- end-to-end API verification