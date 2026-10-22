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
| Styling | Component-scoped styles; global tokens and Material theme in `styles.css` |
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
│   │   ├── core/                           # App-wide API, identity, and titles
│   │   │   ├── api/
│   │   │   │   ├── generated/              # OpenAPI client; never edit by hand
│   │   │   │   ├── claims-api.ts           # Adapter over the generated SDK
│   │   │   │   └── identity.interceptor.ts # Development identity headers
│   │   │   ├── identity/                   # Demo users and each role's routes
│   │   │   └── title-strategy.ts           # Browser tab titles
│   │   ├── features/                       # Route slices
│   │   │   ├── claims/
│   │   │   │   ├── claims-list.ts          # List for both roles
│   │   │   │   ├── claim-details.ts        # Review and actions
│   │   │   │   ├── submit-claim.ts         # Claimant intake
│   │   │   │   └── claims.routes.ts        # Claimant and officer route sets
│   │   │   └── dashboard/                  # One-page features stay flat
│   │   │       └── dashboard.ts
│   │   ├── layout/
│   │   │   ├── app-shell.ts                # Chrome and demo user switcher
│   │   │   └── error-pages.ts              # Not found and unavailable
│   │   ├── shared/
│   │   │   ├── ui/                         # Generic components
│   │   │   └── money.ts                    # Currency formatting
│   │   ├── app.config.ts                   # Providers
│   │   └── app.routes.ts                   # Role guards and lazy-loaded features
│   ├── preflight.css                       # Base reset, based on Tailwind's preflight
│   └── styles.css                          # Tokens, typography, shared classes, and Material overrides
├── e2e/                                    # Playwright journeys
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

Build pages from the shared components and Material rather than repeating markup, and keep status, claim-type, and route mappings in one place.

Keep each component's styles in the component. `styles.css` holds only tokens, typography, shared classes, and Material overrides. Style the app's own class names rather than tag selectors, which can leak into Material components.

## Accessibility

- Every control works by keyboard with a visible focus ring; a skip link and focus management on navigation support keyboard and screen reader users.
- Text meets WCAG AA contrast, and touch targets are at least 24px.
- Each page has a descriptive title.

## Testing

Use focused Vitest tests for behaviour and important failure paths. Playwright runs the critical claimant and officer journeys through the Angular SPA, API, and SQL Server. `tools/ui-screenshots` regenerates [laptop and phone screenshots](../generated/ui-screenshots.md) of the main screens.
