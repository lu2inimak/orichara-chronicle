# API & System Architecture

## 1. Architecture Overview

To maximize cost efficiency and scalability, the system utilizes a **serverless architecture** centered around **Amazon API Gateway (HTTP API)** and **AWS Lambda**.

- **Authentication (Auth):**
	- Managed via **Amazon Cognito**. It federates with external providers (e.g., X/Discord) to issue **JWTs (JSON Web Tokens)** for client-side session management.
    
- **API Layer:**
	- **API Gateway (HTTP API)** serves as the entry point, routing requests to specific Lambda functions based on the payload and path.
    
- **Authorization:**
	- Every write operation is validated using a **Lambda Authorizer** or direct **Cognito Authorizer** to ensure the `UserID` in the token matches the resource owner's ID.

## 2. API Endpoint Definitions

### 2.1 Identity Management (Character & User)

- `GET /me`
	- Retrieves the logged-in user’s profile, owned Characters, and hosted Worlds.
    
- `POST /characters`
	- Creates a new Character profile.
    
- `PATCH /characters/{id}`
	- Updates existing character metadata.

### 2.2 Governance Management (World & Affiliation)

- `POST /worlds`
	- Creates a new World and grants "Host" permissions to the creator.
    
- `POST /worlds/{id}/join`
	- Submits a request to join a specific World (Creates an `Affiliation` with `Status: Pending`).
    
- `PATCH /affiliations/{id}/approve`
	- (Host only) Approves a join request (Updates `Status: Active`).

### 2.3 ### Record Keeping (Activity)

- `GET /worlds/{id}/timeline`
	- Fetches the chronological timeline of a world (utilizing the `GSI_Timeline` Query).
    
- `POST /activities`
	- Posts a new Activity.
    - **Validation:** Verifies that the `affiliation_id` is `Active` and that the requester owns the associated `Character`.
        
- `POST /activities/{id}/sign`
	- (For collaborative posts) Issues an approval signature for another user's Activity.

## 3. Sequence: Activity Posting & Context Validation

This flow demonstrates how the system validates the "Context" (Affiliation) of a post rather than treating it as a simple data entry.

```mermaid
sequenceDiagram
    participant User as User (Client)
    participant API as API Gateway / Lambda
    participant DB as DynamoDB
    
    User->>API: POST /activities (content, affiliation_id)
    API->>DB: Get Affiliation & Character Info
    DB-->>API: Data (OwnerID, WorldID, Status)
    
    Note over API: Auth/Logic Check:<br/>1. Does JWT UserID == Character OwnerID?<br/>2. Is Affiliation Status == Active?
    
    alt Validation Success
        API->>DB: Put Activity (PK: AFF#ID, SK: ACT#Timestamp)
        API-->>User: 201 Created (Success)
    else Validation Failure
        API-->>User: 403 Forbidden
    end
```

## 4. Multi-party Approval Logic

For activities involving multiple characters (Collaborative Logs), visibility is controlled through the following state logic:

1. **Initial Submission:** The activity is created with `Status: PendingMultiSig`. It is excluded from the public `GSI_Timeline` (or filtered out by the client).
    
2. **Signing:** Other involved users call `POST /activities/{id}/sign`.
    
3. **Promotion:** Once the required signatures from all involved `Affiliations` are collected, the Lambda function updates the `Status` to `Published`, making it visible on the timeline.

## 5. System Diagrams (C4 Model)

### 5.1 System Context & Containers

```mermaid
C4Container
    title Container diagram for OC System

    Person(user, "Creator / User", "Original character owner and world host.")
    
    System_Boundary(oc_boundary, "OC System") {
        Container(web_app, "Web Application", "Next.js / TypeScript", "The interface to browse timelines and manage settings.")
        Container(auth, "Authentication", "Amazon Cognito", "Handles SNS login (X/Discord) and JWT issuance.")
        Container(api, "API Layer", "API Gateway (HTTP API)", "Entry point for all backend requests.")
        Container(lambda, "Domain Logic", "AWS Lambda (Node.js/Go)", "Validates business rules and processes 4 entities.")
        ContainerDb(db, "Primary Storage", "Amazon DynamoDB", "Single Table for User, Character, World, Affiliation, and Activity.")
        Container(storage, "Asset Storage", "Amazon S3", "Stores original character images and media.")
    }

    System_Ext(x_discord, "External SNS", "X (Twitter) / Discord for Auth and notifications.")

    Rel(user, web_app, "Uses", "HTTPS")
    Rel(web_app, auth, "Authenticates via", "OpenID Connect")
    Rel(web_app, api, "Calls", "JSON/HTTPS")
    Rel(api, lambda, "Triggers", "AWS SDK")
    Rel(lambda, db, "Reads/Writes", "DynamoDB API")
    Rel(lambda, storage, "Uploads to", "S3 API")
    Rel(auth, x_discord, "Federates with")
```

### 5.2 Component Breakdown (The "Lambdalith")

```mermaid
C4Component
    title Component diagram for OC Lambda (Lambdalith)

    Container(api_gw, "API Gateway", "HTTP API")
    
    Container_Boundary(lambda_boundary, "OC Monolith Lambda") {
        Component(router, "Routing / Handler", "Entry point", "Routes requests to specific services.")
        Component(auth_svc, "Auth Service", "Identity Logic", "Validates User/Character ownership.")
        Component(gov_svc, "Governance Service", "Affiliation Logic", "Manages World rules and join requests.")
        Component(chronicle_svc, "Chronicle Service", "Activity Logic", "Handles timeline and multi-party signing.")
        Component(db_accessor, "Data Accessor", "DynamoDB Client", "Executes Single Table patterns.")
    }

    ContainerDb(db, "DynamoDB", "Single Table")

    Rel(api_gw, router, "Proxies to")
    Rel(router, auth_svc, "Calls")
    Rel(router, gov_svc, "Calls")
    Rel(router, chronicle_svc, "Calls")
    Rel(auth_svc, db_accessor, "Uses")
    Rel(gov_svc, db_accessor, "Uses")
    Rel(chronicle_svc, db_accessor, "Uses")
    Rel(db_accessor, db, "Query/Put")
```
