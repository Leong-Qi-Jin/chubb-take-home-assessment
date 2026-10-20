# Roles and Permissions

The MVP has two roles. Some officer actions also require the officer to own the claim.

<!-- prettier-ignore -->
| Capability | Claimant | Officer | Condition |
| --- | --- | --- | --- |
| Submit a claim | Yes | No | — |
| List claims | Own claims | All claims | — |
| View claim details and history | Own claims | All claims | — |
| View dashboard and liability | No | Yes | — |
| Take a claim | No | Yes | Claim is submitted and unassigned. |
| Answer an information request | Yes | No | Request is open on the claimant's own claim. |
| Set estimated liability | No | Yes | Officer owns the claim; status is under review or awaiting information. |
| Request information | No | Yes | Officer owns the claim; status is under review. |
| Cancel an information request | No | Yes | Officer owns the claim; request is open. |
| Settle a claim | No | Yes | Officer owns the claim; status is under review and an estimate exists. |
| Reject a claim | No | Yes | Officer owns the claim; status is under review. |

Claimants cannot infer whether another claimant's reference exists: the API returns `404`. Their responses also hide estimated liability, officer IDs, and estimated-liability history entries. Officers may read every claim, but only the assigned officer may change a claim after it has been taken.

## Development identity

Production authentication is outside the MVP. The header user switcher selects a seeded identity and the SPA sends exactly one development-only header:

<!-- prettier-ignore -->
| Role | Header | Value |
| --- | --- | --- |
| Claimant | `X-Claimant-Email` | Seeded claimant email |
| Officer | `X-Staff-Id` | Seeded active staff ID |

The API resolves the identity from the database and enforces the role, claim ownership, and assignment rules. See [development seed data](../development-seed-data.md) for the demo users and [the API contract](api-contract.md) for endpoint-specific requirements.
