# Story 09 Design: Frontend Shell and Theme Switching

## Goal

Create the first real front-end shell for Librory: a mobile-first app frame with four switchable pages, a settings page that controls appearance, and a theme system that defaults to Botanical Press and persists locally for the current member.

## Scope

In scope:

- A shared app shell with bottom navigation
- Four switchable placeholder pages: `Home`, `Scans`, `Library`, and `Settings`
- A `Settings` page with a simple style dropdown
- Local persistence for the selected style theme
- A theme architecture with shared base tokens and per-theme overrides
- Botanical Press as the default theme on first load
- The four prototype styles already defined in `static-prototypes-v2`

Out of scope:

- Backend preference storage
- Full settings forms for language, account, or family preferences
- Floating theme menus
- Business content for `Home`, `Scans`, or `Library`
- Reworking the prototype page layouts into new structures

## Design

Treat the front end as a shell plus routed content placeholders.

### App shell

The shell owns:

- the page title
- the main content slot
- the bottom navigation
- the current theme state

The navigation should expose four entries:

- `Home`
- `Scans`
- `Library`
- `Settings`

Each entry can load a placeholder page at first. The important behavior is that the navigation is real and the active page state is visible.

### Theme system

Use one shared theme registry that maps theme names to token overrides.

Recommended structure:

- base tokens define the shared layout and typography contract
- each theme overrides only the tokens it needs
- themes may also adjust a small number of density rules, such as button height, card radius, or spacing

The four themes should align with the prototype set:

- `Classic Scholar`
- `Modern Scout`
- `Cozy Archive`
- `Botanical Press`

Botanical Press should be the initial theme when no saved preference exists.

### Theme application

Apply the selected theme at the app root, not inside individual pages.

The root should:

- read the saved theme from local storage on startup
- fall back to Botanical Press if nothing is saved
- update the active theme immediately when the user changes the selection
- write the new choice back to local storage

If the stored value is invalid or missing, the app should silently recover to Botanical Press.

### Settings page

Keep the settings page intentionally small for this iteration.

The only required control is a style selector:

- label: `Style`
- control: dropdown select
- options: the four themes above
- behavior: change immediately on selection

Language-related and account-related settings can remain as visible placeholders, but they should not be implemented as full form sections yet.

### Page placeholders

The first version of `Home`, `Scans`, and `Library` should be structural placeholders only.

Each page should still feel like part of the app shell:

- a page title
- a short description or empty-state block
- consistent spacing and typography from the active theme

This keeps the shell usable without committing to business content too early.

## Behavior

- First load uses Botanical Press
- Theme changes are immediate
- Theme changes persist across refreshes on the same device
- The selected theme remains stable when switching between pages
- Invalid saved theme values fall back to Botanical Press
- The settings selector reflects the active theme when the page opens

## Testing

Add UI coverage for:

- rendering the default Botanical Press theme on first load
- switching themes from the Settings page
- persisting the selected theme in local storage
- restoring the saved theme after a reload
- falling back to Botanical Press when local storage contains an unknown value
- switching between the four bottom navigation pages

## Risks

- Theme token drift can accumulate if base tokens and per-theme overrides are not kept separate.
- A settings page that grows too quickly will become a second app shell inside the shell.
- Local storage is the right first step, but the persistence boundary should stay narrow so it can be replaced with member-scoped API storage later.

## Decision

Build the page shell first, keep the settings page minimal, and make style switching a root-level theme concern backed by local storage.