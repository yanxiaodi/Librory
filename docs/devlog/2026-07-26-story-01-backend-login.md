# 2026-07-26 Story 01 Backend Login

- Added Google and Microsoft auth endpoints that challenge the provider, complete the callback, and issue the app cookie.
- Persisted external identities on members so the backend can resolve repeat logins without creating duplicate family records.
- Bootstrapped the first singleton family on first external login so solo users can start using the app immediately.
- Kept the existing development auth endpoints for local debugging and Scalar work.
- Added focused API and service tests to cover the login callback, logout, and first-login bootstrap behavior.
