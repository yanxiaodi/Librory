# Localization

## Supported Languages

The first version supports:

- English
- Chinese

## Product Requirement

The app UI must be localized, and UI language resources should be extracted into dedicated resource files.

## Content Display Rules

- Search results should include both English and Chinese when available.
- Book intake should support both English and Chinese metadata.
- Book content is primarily English, but a `preferredLanguage` field should guide display for users who are less comfortable with English.
- Where localized values exist, the UI should prefer the selected language and fall back to the other language.

## Suggested Data Fields

For book records, the following should be language-aware where applicable:

- Title
- Subtitle
- Summary
- Genre labels
- Recommendation text
- Duplicate warning text

## Notes

- English remains the canonical source for most books in the initial version.
- Chinese fields are not a separate product line; they are a supported presentation layer and metadata layer.
