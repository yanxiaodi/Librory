# Story 01 Design: Identity, Family, and Login

## Goal

Allow a user to authenticate, create or join a family scope, and establish the member record that the rest of the product uses.

## Scope

In scope:

- Sign-in through Google
- Sign-in through Microsoft
- Creation of a user profile on first login
- Creation of a family record for a new individual user or family group
- Member records with at least `admin` and `member` roles
- A singleton family mode for individual users
- Family membership lookup for all later API requests
- A `preferredLanguage` field on the user or member profile

Out of scope:

- Complex permission hierarchies
- Platform-admin tooling
- Multi-tenant enterprise identity
- Invitation email templates and delivery infrastructure if the product can defer them to a later story

## Design

Keep authentication simple and align it with the existing Koviva login pattern.

### External identity resolution

Resolve Google or Microsoft sign-in into a stable external identity record.

### Family and member persistence

Persist the domain objects that represent a family, its members, and the member role.

### First-login bootstrap

Create the default family and member records when a user signs in for the first time.

### Shared family creation

Create a family group that can later accept invited members.

### Current family context

Resolve the authenticated user to the current family and member for every family-scoped request.

## Behavior

- First login can create a personal family
- Shared families can be created for multiple members
- Family-scoped requests resolve the current family and member consistently
- Authorization failures are returned when the identity does not map to a valid family member

## Testing

Add coverage for:

- resolving external identity providers
- creating a singleton family on first login
- creating a shared family
- resolving the current family context
- rejecting unauthorized family-scoped access