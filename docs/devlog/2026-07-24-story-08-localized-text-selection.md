# 2026-07-24 Story 08 Localized Text Selection

- Made `LocalizedText.GetValue(...)` explicit for `English` and `Chinese` to keep the switch safer if `PreferredLanguage` grows later.
- Kept English as the fallback for unsupported or missing localized values.
- Added a whitespace-specific test for the Chinese fallback path.
- Removed a redundant null-forgiving operator from the localization helper.
