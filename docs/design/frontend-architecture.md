# Frontend Architecture

## Decisions

Prefer Angular defaults and add a dependency only when it materially improves the implementation.

<!-- prettier-ignore -->
| Concern | Choice |
| --- | --- |
| Runtime | Angular/CLI 22: standalone, zoneless, and OnPush by default |
| Package management | npm with a committed lockfile |
| Routing | Angular Router with lazy-loaded feature routes, role guards, and page titles |
| Components | Angular Material/CDK 22 for controls; semantic HTML for application-specific views |
| Styling | One token system in `styles.css` that also themes Material |
| Forms | Typed Reactive Forms; Material date and time pickers with the date-fns adapter |
| Server state | Generated OpenAPI client behind a thin `ClaimsApi` adapter; Angular `resource()` for reads |
| Local state | Signals; list filters in URL query parameters |
| Icons | Lucide via `@ng-icons/lucide` |
| Tests | Vitest with `TestBed`; browser checks against the running stack |
| Linting | ESLint with Angular, TypeScript, import-order, and Vitest rules; Prettier for formatting |

Keep Angular packages aligned, pin Material/CDK to the same release, and commit `package-lock.json`.

## Structure

Use feature slices around user journeys, not root-level components, services, and models folders.

```sh
apps/web/
├── src/
│   ├── app/
│   │   ├── core/
│   │   │   ├── api/
│   │   │   │   ├── generated/              # OpenAPI client and types; never edit by hand
│   │   │   │   ├── claims-api.ts           # Adapter over the generated SDK
│   │   │   │   └── identity.interceptor.ts # Development identity headers
│   │   │   ├── identity/                   # Demo users and each role's routes
│   │   │   └── title-strategy.ts           # "<page> · ClaimTrack" titles
│   │   ├── features/
│   │   │   ├── claims/
│   │   │   │   ├── claims-list.ts          # Pages, each beside its spec
│   │   │   │   ├── claim-details.ts
│   │   │   │   ├── submit-claim.ts
│   │   │   │   ├── claims.routes.ts        # Claimant and officer route sets
│   │   │   │   ├── claim-status.ts         # Status labels and tones
│   │   │   │   ├── claim-type.ts
│   │   │   │   └── status-badge.ts         # Claim-specific UI
│   │   │   └── dashboard/                  # One-page features stay flat
│   │   ├── layout/                         # Application shell and error pages
│   │   ├── shared/
│   │   │   ├── ui/                         # Generic components: headers, alert, badge, icon, dialog
│   │   │   └── money.ts                    # Money formatting and pipe
│   │   ├── app.config.ts                   # Providers
│   │   └── app.routes.ts                   # Role guards and lazy-loaded features
│   └── styles.css                          # Design tokens and component styles
├── openapi-ts.config.ts                    # API client generation
└── proxy.conf.json                         # Development proxy to the API
```

Each feature owns its pages, routes, and the UI and mappings only it needs. The claims list and details pages serve both roles, because their behaviour differs only by route mode and available actions; the dashboard imports from the claims feature. Move code into `shared` only once more than one feature uses it.

## OpenAPI client

The backend OpenAPI document is the source of truth for endpoints and types. With the API running, regenerate the client from `apps/web` with `npm run generate:api`. Never edit `core/api/generated` by hand.

`ClaimsApi` wraps the generated SDK: it configures `HttpClient`, reads ETags, and turns errors into problem details. It does not redefine the contract.

## Visual system

The UI follows chubb.com's design language:

- Lato for text and a serif for headlines (Source Serif 4 stands in for Chubb's licensed Publico).
- Black text and buttons on white, with the Chubb palette for accents and every Material colour mapped to it.
- Square corners; circles only for avatars, step numbers, and dots.
- Filled and outlined buttons turn Chubb purple with an underlined label on hover; text links underline.
- Shared tones colour badges, alerts, and status markers consistently.

Build pages from the shared components and Material rather than repeating markup, and keep status, claim-type, and route mappings in one place. Style the app's own class names rather than tag selectors, which can leak into Material components.

## Accessibility

- Every control works by keyboard with a visible focus ring; a skip link and focus management on navigation support keyboard and screen reader users.
- Text meets WCAG AA contrast, and touch targets are at least 24px.
- Each page has a descriptive title.

## Testing

Use focused Vitest tests for behaviour and important failure paths, and browser checks for the claimant and officer journeys. `tools/ui-screenshots` regenerates [laptop and phone screenshots](../generated/ui-screenshots.md) of the main screens.
