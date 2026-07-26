# 2026-07-26 Frontend Auth Integration

- Replaced the login page's dev-login buttons with real Google and Microsoft entry links.
- Kept the frontend thin by letting the backend own the OAuth round trip and session cookie issuance.
- Added a visible sign-out action in settings that clears the auth session and returns the user to the public sign-in page.
- Kept the existing `/api/family/current` session hydration flow intact so authenticated state still restores after login.
- Left email login out of scope for a later slice.
