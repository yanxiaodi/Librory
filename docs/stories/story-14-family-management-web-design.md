# Story 14 Design: Family Management Web

## Goal

Add the first family-management experience to the web app so a signed-in user can see family membership, invite members, and manage admin actions from Settings.

## Scope

In scope:

- Family selection and display
- Member management cards
- Invitation management cards
- Admin-only controls
- Mobile-first layout using the existing shell

Out of scope:

- Invitation email delivery infrastructure
- Complex permission hierarchies
- Recommendation or scan workflows

## Design

Treat family management as a settings slice that sits beside the existing app shell.

### Family section

Show the current family and its members, with clear admin indicators.

### Invitation section

Allow admins to create, resend, and revoke invitations.

### Interaction rules

- Regular members can view family data but not manage invitations
- Admins can manage invitations and placeholder members
- One-time invitation URLs should be treated as sensitive and not persisted in long-lived state

## Testing

Add coverage for:

- rendering family and member data
- admin-only invitation actions
- invitation lifecycle actions