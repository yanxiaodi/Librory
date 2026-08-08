# Story 14 Design: Family Membership and Invitations

## Goal

Support multi-family accounts, placeholder members, and invitation onboarding so the app can distinguish between a singleton family and a shared family group.

## Scope

In scope:

- Family and member persistence
- Invitation creation and acceptance
- Admin-only invitation management
- Placeholder members for family setup
- API contracts for family membership flows

Out of scope:

- Recommendation scoring changes
- Scan recognition changes
- Front-end shell redesign
- Complex permission hierarchies

## Design

Keep family membership as the core identity boundary for all later family-scoped features.

### Family model

The model should support:

- a singleton family for an individual user
- a shared family with multiple members
- a stable family identifier for downstream requests

### Invitation model

Invitations should support:

- creation by admins only
- acceptance by invited users
- revocation and resend flows
- placeholder member handling when needed for setup

### Authorization

The API should resolve the current family and member from the authenticated identity and enforce admin-only actions where required.

## Behavior

- First login can create a personal family
- Admins can invite other members
- Invitation acceptance adds the invited membership
- Family-scoped requests resolve the current family and member consistently

## Testing

Add coverage for:

- creating a singleton family
- creating a shared family
- inviting a member
- accepting an invitation
- rejecting unauthorized invitation actions