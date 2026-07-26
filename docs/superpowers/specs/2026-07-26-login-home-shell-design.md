# Story 01 Design: Login and Home Shell

## Goal

Make Librory a login-gated app with a clear public landing page, a dedicated login page, and a simple authenticated home page optimized for the shelf-scanning workflow.

## Scope

In scope:

- Public landing page at `/` with product intro and screenshots
- Login page at `/login` with Google, Microsoft, and email sign-in or registration
- Authenticated app area at `/app/*`
- Default post-login route at `/app/home`
- Route guards that send unauthenticated users to `/login`
- Route guards that send authenticated users away from public pages and into `/app/home`
- First-login flow that creates a singleton family when the user has not joined or created one yet
- Home page layout with a primary scan CTA and a few compact stats

Out of scope:

- Public content feeds or rankings
- Invitation workflows
- Family administration screens
- Deep onboarding beyond creating a singleton family
- Full dashboard analytics

## Design

Treat Librory as a private app with a thin public face.

### Public layer

The landing page exists to explain the product and move the user into login. It can include:

- one short value proposition
- a few screenshots or mockup panels
- a short list of core benefits
- a clear login button

The public layer should not expose product data. It is marketing and orientation only.

### Authentication layer

The login page should offer:

- Google sign-in
- Microsoft sign-in
- email login or registration

The app should treat authentication as mandatory for usage. Any request or route that reaches protected app state without a valid session should redirect to `/login`.

After login, the app resolves the current user profile and family context.

- If the user already has a family context, go to `/app/home`
- If the user does not have one yet, create a singleton family automatically and then go to `/app/home`

That keeps solo users on the same path as family users. A user can stay a one-person family forever and still use the product normally.

### Authenticated app shell

`/app/*` is the product surface.

The home page should stay close to the prototype direction:

- one dominant scan action
- a small set of summary stats
- a lightweight recent-activity section if data exists

The home page should optimize for the core use case: a user standing in a bookshop deciding whether to scan a shelf. It should not become a generic dashboard.

Recommended summary stats:

- books saved
- recent scans
- family size, which is `1` for solo users

If the current user is a solo family of one, the page should still read naturally. Do not require the user to manage members before the app becomes useful.

## Behavior

- Unauthenticated visit to `/` shows the public landing page
- Unauthenticated visit to `/login` shows the login form
- Unauthenticated visit to `/app/*` redirects to `/login`
- Authenticated visit to `/` or `/login` redirects to `/app/home`
- Successful first login creates a usable singleton family if no family exists yet
- Users who never invite anyone else remain fully supported as a solo family
- The home page always keeps the scan action visible near the top
- The summary stat strip can remain useful even when the family contains only one person

## Testing

Add coverage for:

- public landing page rendering
- login page rendering
- unauthenticated redirect from `/app/*` to `/login`
- authenticated redirect from public pages to `/app/home`
- first-login singleton-family bootstrap
- home page rendering with and without family-member data

## Risks

- If the login and family bootstrap flow are too tightly coupled, solo users may get stuck during first use.
- The home page can easily become too busy; keep the scan CTA dominant and the stats minimal.
- Public landing content should stay clearly separate from authenticated product state so future public pages can be added without reworking the app shell.
