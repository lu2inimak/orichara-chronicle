# Data Model

## 1. Domain Relationships

The following class diagram illustrates the core entities and their associations.

```mermaid
classDiagram
    class User {
        +UUID id
    }
    class Character {
        +UUID id
        +UUID owner_id
    }
    class World {
        +UUID id
        +UUID host_id
    }
    class Activity {
        +UUID id
        +UUID creator_id (FK)
        +List~UUID~ participant_ids
        +Content content
        +Status status
    }
    class Affiliation {
        +UUID id
        +UUID char_id
        +UUID world_id
    }

    User "1" -- "0..*" Character : owns
    User "1" -- "0..*" World : hosts
    Character "1" -- "0..*" Affiliation : participates
    World "1" -- "0..*" Affiliation : manages
    User "1" -- "0..*" Activity : creates
    Activity "1" -- "1..*" Affiliation : involves
```

**Implementation Note:** To optimize performance and minimize database lookups, the `owner_id` is denormalized and stored directly within the `Affiliation` entity.

## 2. State Transitions

### 2.1 Affiliation Lifecycle

This diagram defines the process of a character joining or being removed from a world.

```mermaid
stateDiagram-v2
    [*] --> Pending : Request to Join
    [*] --> Invited : Invited by Host
    
    Pending --> Active : Host Approves
    Pending --> Rejected : Host Denies
    
    Invited --> Active : User Accepts
    Invited --> Rejected : User Declines
    
    Active --> Banned : Policy Violation
    Active --> Archived : Character/World Deletion
    
    Rejected --> [*]
    Banned --> [*]
```

### 2.2 Activity Lifecycle (Multi-party Approval)

This flow manages the publication of activities, including those requiring consensus from multiple participants.

```mermaid
stateDiagram-v2
    [*] --> Draft : Create Activity
    Draft --> Published : Single-person Post
    Draft --> PendingMultiSig : Collaborative Post
    
    state PendingMultiSig {
        [*] --> WaitingForSignatures
        WaitingForSignatures --> WaitingForSignatures : Sign(UserID)
        WaitingForSignatures --> AllSigned : Threshold Reached
    }
    
    AllSigned --> Published : Automatic Release
    PendingMultiSig --> Redacted : Timeout / Cancellation
    Published --> Redacted : Logic Delete
    
    Redacted --> [*]
```

## 3. DynamoDB Physical Data Model

### 3.1 Primary Index (Base Table)

| **PK (Partition Key)** | **SK (Sort Key)**     | **Type**    | **Description / Usage**                                       |
| ---------------------- | --------------------- | ----------- | ------------------------------------------------------------- |
| `USER#<UserID>`        | `PROFILE`             | User        | Retrieve account profile information.                         |
| `USER#<UserID>`        | `OWN_CHAR#<CharID>`   | (Pointer)   | List all characters owned by the user.                        |
| `USER#<UserID>`        | `OWN_WORLD#<WorldID>` | (Pointer)   | List all worlds hosted by the user.                           |
| `CHAR#<CharID>`        | `INFO`                | Character   | Master data for character details.                            |
| `WORLD#<WorldID>`      | `INFO`                | World       | Master data for world settings and configuration.             |
| `WORLD#<WorldID>`      | `AFF#<CharID>`        | Affiliation | List of all participants (characters) within a world.         |
| `AFF#<AffID>`          | `ACT#<Timestamp>`     | Activity    | Activity history associated with a specific role/affiliation. |

### 3.2 Global Secondary Index (GSI)

|**Index Name**|**PK**|**SK**|**Usage**|
|---|---|---|---|
|`GSI_ReverseLookup`|`CHAR#<CharacterID>`|`AFF#<WorldID>`|Retrieve all worlds a specific character has joined.|
|`GSI_Timeline`|`WORLD#<WorldID>`|`ACT#<Timestamp>`|Fetch a chronological activity feed for an entire world.|
