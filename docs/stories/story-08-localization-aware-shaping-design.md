# Story 08 Design: Localization-Aware Shaping

## Goal

Return book data in a language-aware shape so the frontend can prefer English or Chinese without duplicating business logic.

## Scope

In scope:

- Preferred-language selection
- English fallback behavior
- Localized variants for book-facing text

Out of scope:

- UI theme switching
- Recommendation scoring
- External metadata provider integration

## Design

Keep localization logic in the domain so later layers can reuse it.

### Preferred value selection

Choose the preferred value from localized text using the user's selected language and a fallback to English.

### Canonical source behavior

Preserve English as the canonical source where no localized value exists.

## Behavior

- The API can return localized variants for title, subtitle, summary, genre labels, recommendation text, and duplicate warning text
- English remains the canonical fallback

## Testing

Add coverage for:

- preferred-language selection
- English fallback behavior
- localized API output