# Frontend Auth Integration Design

## Goal

Connect the Librory web app to the real backend login flow so users can sign in with Google or Microsoft, land in the authenticated app shell, and use the app immediately after login.

## Scope

In scope:

- Wire the login page to the real backend auth start routes
- Keep `/` as the public landing page
- Keep `/login` as the public sign-in page
- Keep `/app/*` as the authenticated app shell
- Preserve the existing auth session hydration against `/api/family/current`
- Keep logout working against the real backend logout route
- Keep local dev auth available for debugging if needed

Out of scope:

- Email login or email registration
- Any activation or onboarding step after login
- Any new family setup UI
- Any backend auth provider changes
- Any redesign of the landing page or authenticated shell

## Design

Use the backend auth routes as the source of truth for sign-in.

### Login flow

The login page should present only the providers that are actually wired:

- Google
- Microsoft

Each button should navigate the browser to the corresponding backend start route:

- `/auth/google/start`
- `/auth/microsoft/start`

The backend completes the provider round trip, issues the app cookie, and redirects back to `/app/home`.

This keeps the frontend thin and avoids duplicating OAuth logic in the browser.

### Session flow

The existing auth session context should keep doing the same job:

- on app startup, call `/api/family/current`
- if the request succeeds, mark the user authenticated
- if it fails, keep the user anonymous

That means the app can refresh correctly after a successful provider login without any special front-end callback handling.

### Logout flow

Logout should call the backend logout endpoint:

- `POST /auth/logout`

After a successful logout, the frontend should clear its local session state and route the user back to `/login`.

### Routes

The route structure should stay as it is:

- `/` public landing page
- `/login` public login page
- `/app/home` authenticated home route
- `/app/scans` authenticated scans route
- `/app/library` authenticated library route
- `/app/settings` authenticated settings route

Anonymous users should still be redirected away from `/app/*`.
Authenticated users should still be redirected away from `/` and `/login`.

## Behavior

- Clicking Google or Microsoft on `/login` starts the real backend auth flow
- Login completes without any activation step
- Successful auth lands the user on `/app/home`
- The app shell remains unusable until a valid auth session exists
- Logout returns the user to the public landing page
- Email login is not shown yet

## Testing

Add or update coverage for:

- login buttons point at the correct backend auth routes
- authenticated users still land in `/app/home`
- anonymous users still get redirected to `/login`
- logout still clears the session and returns the user to the public surface
- the app still hydrates the session from `/api/family/current`

## Risks

- If the login buttons use normal navigation, the SPA will leave the current page during sign-in. That is acceptable because the backend owns the provider round trip.
- If the login page still shows the old dev-login semantics, users may think email login is already available. The UI should not promise that.
- If frontend tests keep mocking the dev-login path, they will drift from the real auth behavior and should be updated now.

## Decision

Use backend-driven Google and Microsoft sign-in only. Do not add a separate activation step. The user should be able to log in and use the app immediately after the backend issues the session cookie.
