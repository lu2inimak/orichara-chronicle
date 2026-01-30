# ADR-0001: Adopt DynamoDB Single Table Design

## Status
Accepted

## Context
The system requires modeling Users, Characters, Worlds, Affiliations, Activities, and Policies
with highly interconnected access patterns and timeline-oriented reads.

Using multiple tables would require frequent joins at the application layer and increase
query complexity and latency.

## Decision
Adopt a DynamoDB Single Table design using composite PK/SK patterns to colocate
related entities and optimize for access patterns.

## Consequences
### Pros
- Efficient timeline and relationship queries
- Reduced network round trips
- Access-pattern–oriented modeling

### Cons
- Higher modeling complexity
- Requires strict key naming conventions