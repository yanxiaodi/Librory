# 2026-07-26 Login Home Shell Auth

- Added a public landing page and a dedicated `/login` surface while keeping the authenticated product shell under `/app/*`.
- Wired the login buttons to the development auth flow so the UI no longer looks dead during local use.
- Added auth-session hydration so the app can resolve `/api/family/current` before showing protected content.
- Kept the first authenticated landing screen scan-first, with the primary action centered on shelf scanning.
- Verified the frontend flow with targeted Vitest coverage and a production build.
