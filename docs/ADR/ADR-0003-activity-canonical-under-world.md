# ADR-0003: Canonical Activity Records Stored Under World

## Status
Accepted

## Context
While Activity ownership belongs to Affiliation, the primary UI and
read pattern is the World timeline (world feed).

Storing activities under Affiliation would make world feeds expensive,
requiring multi-partition merges.

## Decision
Store canonical Activity records under `PK = WORLD#<WorldID>`.
Maintain Affiliation-level activity as an index/pointer for ownership history.

## Consequences
### Pros
- Efficient world timeline queries (single partition)
- Preserves affiliation-based ownership model
- Clean separation between canonical event log and ownership index

### Cons
- Requires write fan-out or indexing to affiliation timeline