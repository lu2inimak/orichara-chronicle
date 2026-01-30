# ADR-0002: Activity Ownership is Defined by Affiliation

## Status
Accepted

## Context
Activities are created by characters acting within a world.
The true "actor" is not the User nor the World, but the Character's role
(Affiliation) inside the World.

## Decision
Define the ownership of an Activity by `owner_aff_id`.
This allows the system to represent that "a character, in a specific world,
performed this action".

## Consequences
### Pros
- Correct domain modeling of role-based actions
- Clear mapping between character identity and activity history

### Cons
- Requires additional indexing for world-level timelines