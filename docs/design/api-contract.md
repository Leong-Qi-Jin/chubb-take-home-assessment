# API Contract

## Principles

- The backend owns the contract via OpenAPI; TypeScript contract types are generated from it.
- JSON under `/api/v1`, with one endpoint per workflow action.
- Claims are addressed by `Reference`, not `Id`.
- Enums are strings, timestamps are UTC ISO 8601, and money is `{ "amount": "1250.00", "currency": "SGD" }`.
- Operations are synchronous; a success means the change and its history entry are committed.

## Audiences

The [roles and permissions matrix](roles-and-permissions.md) summarises application access. Development-only MVP identity:

- `Claimant`: a seeded claimant, sent as `X-Claimant-Email` and assumed verified. Sees only their own claims.
- `Officer`: an active seeded staff member, sent as `X-Staff-Id`.
- `AssignedOfficer`: the staff member who owns the claim.

Send exactly one identity header; sending both returns `401`.

## Endpoints

Paths are relative to `/api/v1`. Body lists the request fields. Request amounts are non-negative decimal strings with up to 2 decimal places, in the claim's currency.

### Claimant

<!-- prettier-ignore -->
| Method | Path | Role | Body | Purpose |
| --- | --- | --- | --- | --- |
| `POST` | `/claims` | Claimant | See [example](#submit-a-claim) | Submit a motor or property claim for the caller. |
| `GET` | `/claims` | Claimant |  | The caller's own claims, paginated. |
| `GET` | `/claims/{reference}` | Claimant, Officer |  | Claim details, status, decision, and information requests. |
| `GET` | `/claims/{reference}/history` | Claimant, Officer |  | Timeline, oldest first. |
| `POST` | `/claims/{reference}/information-requests/{requestId}/response` | Claimant | `responseText` | Answer the open request; the claim returns to `UnderReview`. |

### Claims staff

<!-- prettier-ignore -->
| Method | Path | Role | Body | Purpose |
| --- | --- | --- | --- | --- |
| `GET` | `/claims` | Officer |  | Work queue, paginated, with filters and sorting. |
| `POST` | `/claims/{reference}/take` | Officer |  | Take an unassigned `Submitted` claim; moves it to `UnderReview`. |
| `PUT` | `/claims/{reference}/estimated-liability` | AssignedOfficer | `amount` | Set or change the estimate while `UnderReview` or `AwaitingInformation`. An unchanged amount records nothing. |
| `POST` | `/claims/{reference}/information-requests` | AssignedOfficer | `requestText` | Request information while `UnderReview`; the claim is then `AwaitingInformation`. |
| `POST` | `/claims/{reference}/information-requests/{requestId}/cancel` | AssignedOfficer | `cancellationReason` | Cancel the open request; the claim returns to `UnderReview`. |
| `POST` | `/claims/{reference}/settle` | AssignedOfficer | `settlementAmount`, `rationale` | Settle an `UnderReview` claim that has an estimate. |
| `POST` | `/claims/{reference}/reject` | AssignedOfficer | `rationale` | Reject an `UnderReview` claim. |
| `GET` | `/dashboard` | Officer |  | Workload counts and outstanding liability. |

### Shared

<!-- prettier-ignore -->
| Method | Path | Role | Body | Purpose |
| --- | --- | --- | --- | --- |
| `GET` | `/currencies` | Claimant, Officer |  | Currency codes a claim can be submitted in, such as `["SGD", "MYR"]`. |

Submit and request information return `201` (submit adds a `Location` header); other changes return `200`. All return the claim.

### Queue query

`GET /claims?status=Submitted&type=Motor&assignedTo={staffId}|unassigned&claimantId={claimantId}&sort=submittedAt|updatedAt&order=asc|desc&page=1&pageSize=20`

All parameters are optional, and `status` can repeat. By default, officers get the oldest claims first and claimants the newest. Ties break on the internal claim `Id`, so pages stay stable. `page` starts at 1, and `pageSize` is 5, 10, 20 (the default), 50, or 100; other values return `400`. A page past the end returns empty `items`.

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalCount": 0
}
```

Items are claim summaries: `reference`, `type`, `status`, `claimantId`, `claimantName`, `assignedOfficer`, `estimatedLiability`, `submittedAt`, `updatedAt`.

Claimants get only their own claims. They cannot filter by `assignedTo` or `claimantId` (`400`), and see the same redactions as claim details.

## Examples

### Submit a claim

```json
{
  "type": "Motor",
  "policyNumber": "POL-123456",
  "incident": {
    "occurredAt": "2026-10-12T08:30:00Z",
    "location": "Orchard Road, Singapore",
    "description": "Rear-ended at a junction."
  },
  "motor": {
    "vehicleRegistration": "SBA1234A"
  },
  "property": null,
  "currency": "SGD"
}
```

Only the detail object matching `type` is set; `property` takes `{ "propertyAddress": "…" }`. `currency` must be one of the codes from `GET /currencies`.

### Claim details

```json
{
  "reference": "CLM-7Q4K-9XRM-2PDA",
  "type": "Motor",
  "status": "AwaitingInformation",
  "policyNumber": "POL-123456",
  "claimant": {
    "fullName": "Alex Tan",
    "email": "alex@example.com",
    "phone": null
  },
  "incident": {
    "occurredAt": "2026-10-12T08:30:00Z",
    "location": "Orchard Road, Singapore",
    "description": "Rear-ended at a junction."
  },
  "motor": {
    "vehicleRegistration": "SBA1234A"
  },
  "property": null,
  "assignedOfficer": {
    "id": "…",
    "displayName": "Sam Lee"
  },
  "currency": "SGD",
  "estimatedLiability": {
    "amount": "4200.00",
    "currency": "SGD"
  },
  "informationRequests": [
    {
      "id": "…",
      "status": "Open",
      "requestText": "Please provide the police report number.",
      "requestedAt": "…",
      "responseText": null,
      "respondedAt": null,
      "cancellationReason": null,
      "cancelledAt": null
    }
  ],
  "decision": null,
  "submittedAt": "…",
  "updatedAt": "…"
}
```

Once decided, `decision` holds `outcome`, `settlementAmount` (null if rejected), `rationale`, `decidedBy` (`{ id, displayName }`), and `decidedAt`.

Claimants get the same shape, with `estimatedLiability` and officer `id`s set to `null`.

### History entry

```json
{
  "eventType": "InformationRequested",
  "summary": "…",
  "actorType": "ClaimsOfficer",
  "actorName": "Sam Lee",
  "informationRequestId": "…",
  "occurredAt": "…"
}
```

Claimants do not see `EstimatedLiabilityUpdated` entries.

### Dashboard

```json
{
  "countsByStatus": {
    "Submitted": 4,
    "UnderReview": 7,
    "AwaitingInformation": 2,
    "Settled": 10,
    "Rejected": 3
  },
  "myOpenClaims": 3,
  "openClaimsByOfficer": [
    {
      "officer": { "id": "…", "displayName": "Sam Lee" },
      "count": 3
    }
  ],
  "outstandingLiability": [{ "amount": "58200.00", "currency": "SGD" }]
}
```

Liability is grouped by currency; there is no conversion.

## Concurrency

Every claim response has an `ETag` header holding the claim's version (`Claims.Version`). To change an existing claim (except `take`), the client sends it back in `If-Match`, so the change is rejected if someone else updated the claim first.

- Missing `If-Match` returns `428`; `*` matches any version.
- A stale `If-Match` returns `409` `claim_modified`, with the current claim in the error's `claim` field and its `ETag` header.
- `take` needs no `If-Match`; if two officers race, the loser gets `409` `claim_already_assigned`.

## Errors

Errors use the standard JSON error format from [RFC 9457 (Problem Details)](https://www.rfc-editor.org/rfc/rfc9457), which ASP.NET Core supports natively. A `code` field lets the frontend react without parsing messages; the codes below are the complete set, published as an OpenAPI enum.

```json
{
  "type": "https://errors.example/claim-status-not-allowed",
  "title": "Action not allowed in the claim's status",
  "status": 409,
  "detail": "Only an UnderReview claim can be settled.",
  "code": "claim_status_not_allowed"
}
```

<!-- prettier-ignore -->
| Status | When | `code` |
| --- | --- | --- |
| `400` | Malformed or invalid input. Invalid fields are listed in `errors`, keyed by camelCase JSON path such as `incident.occurredAt`; unreadable JSON or query values have no field list. | `validation_failed` |
| `401` | Missing identity header, an unknown claimant or staff member, or an inactive officer. | `caller_not_identified` |
| `403` | Wrong role for the endpoint, or an officer acting on a claim they are not assigned. | `role_not_allowed`, `not_assigned_officer` |
| `404` | Unknown claim or request; claimants also get this for others' claims. | `claim_not_found`, `information_request_not_found` |
| `409` | Action not allowed in the current state, or the claim changed since it was read. | `claim_status_not_allowed`, `claim_already_assigned`, `information_request_not_open`, `estimated_liability_required`, `claim_modified` |
| `428` | Missing `If-Match`. | `if_match_required` |
| `500` | Unexpected server error; details are logged, not returned. | `internal_error` |

Framework responses for unknown routes, methods, or media types (`404`, `405`, `415`) have no `code`.

## Deferred

- User management and sign-up; claimants and staff are seeded.
- Real-time updates; the client refreshes instead.
- Several open information requests per claim.
- Reassigning or taking over a claim.
- Webhooks, bulk operations, and uploads.
- Duplicate-submission protection: a retried submit after a network error may create a second claim.
