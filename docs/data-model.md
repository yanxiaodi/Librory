# Data Model

## Core Concept

Use a `Work -> Edition -> Copy` model.

## Entities

### Family

Represents a household or shared library group.

### Member

Represents a person in the family.

### BookWork

The abstract literary work, such as a title and author combination.

### BookEdition

A specific edition of a work.

### BookCopy

The physical copy owned by the family.

### Author

Author identity and related works.

### Store

Purchase location such as a thrift store or charity shop.

### ShelfLocation

Physical storage location inside the home.

### PurchaseRecord

Purchase metadata such as price, date, store, and condition.

### Review

A review, note, or reading journal entry attached to a book.

### WishlistItem

A book the user wants to find later.

### ScanSession

A temporary shelf scan result.

### RecommendationProfile

Rules and preferences for a child or family member.

### LocalizedText

Localized display text for a field such as title, summary, or genre labels.

## Relationship Summary

- A `Family` has many `Member` records.
- A `Family` owns many `BookCopy` records.
- A `BookWork` can have many `BookEdition` records.
- A `BookEdition` can have many `BookCopy` records.
- A `BookCopy` belongs to one `Family` and one owning `Member`.
- A `BookCopy` can have many `Review` entries.
- A `BookCopy` can have one or more `PurchaseRecord` entries over time if needed for corrections.
- A `WishlistItem` can reference a `BookWork`, `BookEdition`, or fuzzy search result.
- A `ScanSession` stores temporary scan results and expires after a short retention window.
- A localized book field may store English and Chinese variants side by side.

## Localization Notes

- English is the primary content language.
- Chinese is a supported second language.
- Book metadata may need a language-aware display value for title, summary, and labels.
- `preferredLanguage` helps the UI choose which localized content to emphasize for a user or family member.

## Notes

- Keep the model flexible for uncertain matches.
- Store confidence and source metadata where data may be inferred.
- Separate factual book data from user-generated data.
