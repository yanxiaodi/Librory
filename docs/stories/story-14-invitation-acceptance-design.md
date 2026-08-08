# Story 14 Design: Invitation Acceptance

## Goal

Let an invited user accept a family invitation and join the target family without breaking the first-login personal family flow.

## Scope

In scope:

- Invitation preview
- Invitation acceptance
- Joining the invited family
- Basic error handling for invalid or expired tokens

Out of scope:

- Invitation creation UI
- Admin management screens
- Complex permission changes

## Design

The acceptance flow should be simple and explicit.

### Preview

Show the invited family and the invitation target before acceptance.

### Acceptance

When the user accepts, add the invited membership and route them into the invited family context.

## Behavior

- Invalid tokens return a clear error state
- Expired tokens cannot be accepted
- Accepting an invitation should not destroy the user's personal family history

## Testing

Add coverage for:

- previewing a valid invitation
- accepting an invitation
- rejecting invalid or expired tokens