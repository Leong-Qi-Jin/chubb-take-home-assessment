# Development Seed Data

Development startup applies pending migrations and inserts deterministic demo data when `SeedData__Enabled` is not `false`. Repeated startup does not duplicate the data.

## Primary demo journey

Use `alex.tan@example.com` as `X-Claimant-Email`. Alex owns six claims covering the complete lifecycle:

- `CLM-DEMO-0001`: submitted and available for an officer to take.
- `CLM-DEMO-0007`: under review and ready for liability, information-request, settlement, or rejection actions.
- `CLM-DEMO-0008`: under review after an answered information request.
- `CLM-DEMO-0014`: awaiting a response to an open information request.
- `CLM-DEMO-0018`: settled with a recorded decision.
- `CLM-DEMO-0023`: rejected with a recorded decision.

The other seeded claimants are `maya.lim@example.com`, `daniel.wong@example.com`, `priya.nair@example.com`, `james.lee@example.com`, and `siti.rahman@example.com`.

## Claims officers

Use one of these values as `X-Staff-Id`:

- Sam Lee: `00000000-0000-0000-0000-000000000101`
- Aisha Khan: `00000000-0000-0000-0000-000000000102`
- Benjamin Tan: `00000000-0000-0000-0000-000000000103`

`00000000-0000-0000-0000-000000000104` is an inactive officer for caller-rejection scenarios.

## Dataset coverage

The seed includes six submitted, seven under review, four awaiting information, five settled, and three rejected claims. They cover both claim types, all three active officers, and SGD and MYR liability totals for dashboard testing.

Fixed claim IDs and dates make the demo data reproducible across database setups. Integration tests should create the records they need instead of relying on this demo data.
