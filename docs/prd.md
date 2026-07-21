# PRD: AI Second-Hand Book Hunting Assistant

## 1. Background

The primary pain point is not cataloging a personal library. It is making fast decisions while standing in front of a second-hand bookstore shelf.

The user wants to know:

- Whether the book is already owned
- Whether the book is appropriate for a child’s age and preferences
- Whether different editions make the book worth buying again
- How to keep a shared family library organized after purchase

## 2. Product Goal

Provide an AI-assisted decision tool for second-hand book shopping that helps the user quickly answer:

- What book is this?
- Is it worth buying?
- Do we already own it?
- If we already own it, is this a better edition?
- Is it suitable for a specific family member, especially a daughter?

## 3. Product Positioning

This is not a traditional book cataloging app.
It is a family-oriented AI Book Hunting Assistant.

## 4. MVP Scope

The MVP focuses on one primary scenario:

- Scan a second-hand bookstore shelf
- Return a list of books
- Show recommendation strength for each book
- Show duplicate detection under each book
- Preserve temporary scan results
- Allow purchased books to be scanned again and added to the family library
- Support family accounts and ownership assignment
- Support a wishlist
- Support manual recommendation preferences

## 5. Target Users

Primary user:

- A parent buying English books for a daughter

Secondary users:

- Parents managing a shared family library
- People who regularly shop second-hand bookstores
- Readers who collect authors or series

## 6. Core Scenarios

Scenario 1:

- The user scans a shelf in a second-hand bookstore
- The system recognizes books
- The system sorts books by recommendation strength
- The system shows whether a book is already owned
- The user decides whether to buy

Scenario 2:

- The user rescans a purchased book at home
- The system adds it to the family library
- The user fills in ownership, price, store, and location information

Scenario 3:

- The user adds a book to the wishlist from search or an author page
- The wishlist is used later when shopping

## 7. Product Principles

- Shelf scanning comes first; ISBN scanning is secondary
- Duplicate detection is a warning, not a blocker
- Recommendation and duplicate detection are independent, but duplicate detection is more prominent
- Partial recognition is acceptable if missing fields are clearly marked
- Start with something usable, then deepen later
- Recommendation should begin with manual configuration, with automatic learning added later

## 8. Information Architecture

Page 1: Shelf Scan Results

- Show the scan results as a book list
- Each book displays:
  - Title
  - Author
  - Recommendation strength
  - Age fit
  - Genre / style
  - Summary
  - Already owned / duplicate warning
- Duplicate results appear below the recommendation output
- Version differences are shown when detected
- Missing fields and low-confidence results are clearly marked

Page 2: Book Detail

- Title
- Author
- Cover image
- Recommendation information
- Duplicate information
- Age fit
- Genre / style
- Summary
- Edition information
- Collection value hints
- Wishlist action

Page 3: Purchase Intake

- Title
- Cover image
- Owning member
- Purchase date
- Price
- Store
- Shelf location
- Edition information
- Notes

Page 4: Family Library

- All books owned by the family
- Filter by member
- Search by author, genre, location, or store

Page 5: Wishlist

- A long-term wishlist only
- Sources include manual search and missing books from author pages

## 9. Functional Requirements

### 9.1 Shelf Scanning

Must support:

- Capturing a whole shelf in one photo
- Recognizing book titles
- Returning a list of books
- Supporting multiple matches
- Supporting low-confidence output
- Supporting candidates and source explanations
- Allowing the flow to continue without interruption

Suggested inputs:

- Shelf photo
- Optional single-book cover photo for follow-up

Outputs:

- Title
- Author
- Recommendation strength
- Age fit
- Genre / style
- Summary
- Already owned / duplicate warning
- Missing-field indicators

### 9.2 Recommendation System

Recommendation logic:

- AI-led
- Rules act as constraints
- Manual configuration first, automatic learning later

Current configuration dimensions:

- Age range
- Favorite authors
- Favorite genres
- Favorite styles

Recommendation notes:

- Explanations are required
- Author is a supporting signal, not a hard gate
- Age mismatch reduces the score
- Already owned triggers a strong warning but does not block purchase
- Low confidence should be surfaced for manual review

### 9.3 Duplicate Detection

Goals:

- Prevent accidental duplicate purchases
- Support edition-aware comparison
- Support family-wide duplicate checking

Detection strategy:

- Warn while scanning the shelf
- Re-check before purchase if needed
- Re-check again during home intake
- Cover all three stages, with shelf-time warning as the default emphasis

Duplicate warning behavior:

- Show a strong warning when the book is already owned
- Allow the user to continue buying
- If a different edition is better or more collectible, keep the decision open
- Prefer comparing:
  - Edition type
  - Binding quality
  - Publication year

### 9.4 Temporary Scan Results

Requirements:

- Temporary only
- Retain for 7 days by default
- No requirement for long-term storage
- Allow recent scans to be reviewed later
- Not strongly coupled to the home library

### 9.5 Home Intake

Meaning:

- The book has been purchased
- It should be recorded as fully as possible
- Missing fields may be completed later

Minimum required at intake:

- Title
- Cover image
- Owning member
- Duplicate check confirmation

Fields that can be completed later:

- Author
- Edition
- Price
- Store
- Shelf location
- Purchase date
- Notes

### 9.6 Family Accounts

Requirements:

- Support family accounts
- Support multiple members
- Allow a book to belong to a specific member
- Check duplicates across the full family library
- Search across the whole family library during shopping
- Keep the model simple; do not add a complex permission system in the first version

### 9.7 Wishlist

The first version keeps only a wishlist, not a separate “want to buy now” state.

Sources:

- Manual search
- Missing books on author pages

### 9.8 Search

Support search by:

- Title
- Author
- Year
- Genre
- Style
- Owning member
- Store
- Shelf location
- Wishlist status

## 10. Data Model

Core entities:

- Family
- Member
- BookWork
- BookEdition
- BookCopy
- Author
- Store
- ShelfLocation
- PurchaseRecord
- Review
- WishlistItem
- ScanSession
- RecommendationProfile

Relationship notes:

- A Work is the abstract book title
- An Edition is a specific version
- A Copy is the physical book
- The same Work can have multiple Editions
- A Family can have multiple Members
- A physical Copy belongs to a Member but remains searchable across the family

## 11. Data Sources

Priority:

- Library and ISBN-based sources are preferred
- They act as the authoritative factual layer

Supplementary sources:

- Other book metadata providers
- AI summarization and explanation on top of the source data

Conflict handling:

- Cross-validate multiple sources
- Prefer the authoritative source when data conflicts
- Mark uncertain data when the system cannot resolve the conflict

## 12. Recognition Strategy

Shelf scanning:

- Title recognition is the primary signal
- Ambiguity is allowed
- Low-confidence results are allowed
- Choose the most likely match by default
- Show source and reasoning alongside the result

Single-book scanning:

- ISBN can be used to retrieve more fields
- More metadata can be filled in

## 13. Sorting

Default sorting:

- Recommendation strength first

Optional sorting:

- Year
- Genre / style
- Already owned
- Recognition confidence

## 14. Edition Handling

The system should not attempt overly deep bibliographic analysis, but it should try to recognize:

- Hardcover
- Edition type
- Collector’s edition
- Illustrated edition
- Year differences

For duplicates:

- Already owned is a warning, not a rejection
- A better edition may still justify purchase

## 15. Non-Functional Requirements

Performance goals:

- Shelf scan results should return within a few seconds
- Single-book recognition should be fast
- Search should remain lightweight

Experience goals:

- Results should be easy to scan visually
- Missing information must be explicitly marked
- The user should quickly understand why a book is recommended or why it is flagged as a duplicate

## 16. Non-Goals

The first version does not include:

- Complex permissions
- Public social reviews
- A full statistics dashboard
- Deep edition-market analysis
- Automatic learning as the only recommendation method
- Complex cross-device sync

## 17. Risks

- Spine recognition may be unreliable
- Edition detection is hard
- Second-hand bookstores have inconsistent lighting
- Same-title and multi-edition books can be confused
- Black-box recommendations reduce trust
- A heavy intake flow will reduce adoption

## 18. MVP Success Criteria

- The user can scan a shelf quickly
- The system gives understandable recommendations
- The system warns about owned books
- The system highlights edition differences
- The user trusts it enough to use it while shopping
- The user is willing to rescan purchased books at home

## 19. Future Iterations

Future enhancements may include:

- Automatic preference learning
- Stronger series completion tracking
- Author completion tracking
- Purchase location statistics
- Price statistics
- Reading reviews
- Public/private reviews
- Richer collection value signals
- Map and store analytics
