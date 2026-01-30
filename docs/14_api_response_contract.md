# API Shared Types & Error Format (Temporary)

This document defines shared request/response types and a unified error format.

## 1. Conventions

- **JSON** only.
- **Field naming:** `snake_case`.
- **Envelope:** all responses return `ok`, `request_id`.
- **Versioning:** `meta.version` for now; URL versioning can be added later.

## 2. Shared Response Envelope

### 2.1 Success

```json
{
  "ok": true,
  "data": {},
  "meta": {
    "version": "v1"
  },
  "request_id": "req_01H..."
}
```

### 2.2 Error

```json
{
  "ok": false,
  "error": {
    "code": "VALIDATION_FAILED",
    "message": "Input validation failed.",
    "details": {}
  },
  "request_id": "req_01H..."
}
```

## 3. Error Format

### 3.1 Fields

- `code` (string, stable, machine-readable)
- `message` (string, user-facing summary)
- `details` (object, optional, context for debugging)

### 3.2 Error Code Categories

#### Auth / Session
- `AUTH_REQUIRED` (401)
- `AUTH_INVALID` (401)
- `AUTH_EXPIRED` (401)

#### Authorization
- `FORBIDDEN` (403)
- `OWNERSHIP_MISMATCH` (403)

#### Validation / Input
- `VALIDATION_FAILED` (400)
- `MALFORMED_JSON` (400)
- `MISSING_FIELD` (400)

#### Not Found / Conflict
- `NOT_FOUND` (404)
- `ALREADY_EXISTS` (409)
- `CONFLICT` (409)

#### Domain Rules (API-specific)
- `AFFILIATION_NOT_ACTIVE` (403)
- `AFFILIATION_NOT_FOUND` (404)
- `WORLD_JOIN_PENDING` (409)
- `MULTISIG_REQUIRED` (409)
- `MULTISIG_ALREADY_SIGNED` (409)

#### System / Infrastructure
- `RATE_LIMITED` (429)
- `INTERNAL_ERROR` (500)
- `UPSTREAM_ERROR` (502)

## 4. Common Request Headers (Suggested)

- `Authorization: Bearer <JWT>`
- `X-Request-Id: <uuid>` (optional; echoed back as `request_id`)
- `Idempotency-Key: <uuid>` (for POST where applicable)

## 5. Pagination (Reserved Shape)

When returning a list:

```json
{
  "ok": true,
  "data": {
    "items": [],
    "next_token": "opaque_cursor_or_null"
  },
  "meta": {
    "version": "v1"
  },
  "request_id": "req_01H..."
}
```

## 6. Notes & Decisions

- `request_id` is always returned to support tracing.
- `details` should not include PII.
- Error codes are stable; message can change without breaking clients.
