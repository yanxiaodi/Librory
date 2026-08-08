# Story 02 Design: Core Book Domain Model

## Goal

Persist the core book entities so the app can distinguish between a work, an edition, and a physical copy.

## Scope

In scope:

- Work and edition identity
- Copy ownership and family inventory
- Book metadata and inference fields

Out of scope:

- Manual intake API behavior
- External metadata provider integration
- Recognition job orchestration

## Design

Model the book domain as a `Work -> Edition -> Copy` chain.

### Work and edition identity

Create the core relationship between a book work and one or more editions.

### Copy ownership and family inventory

Attach a physical copy to a family and one owning member.

### Metadata and inference fields

Store the metadata that later search, intake, and recommendation flows will need.

## Behavior

- A work can own many editions
- An edition belongs to exactly one work
- A copy belongs to exactly one edition, family, and owning member
- The model can store uncertain or inferred metadata without overwriting source facts

## Testing

Add coverage for:

- creating works and editions
- creating copies for a family and member
- storing inferred metadata fields
- preserving the work/edition/copy separation