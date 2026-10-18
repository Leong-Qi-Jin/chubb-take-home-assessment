# Frontend Architecture

## Decisions

Prefer Angular defaults; otherwise choose a stable modern library that materially improves developer experience.

| Concern | Choice |
| --- | --- |
| Runtime | Angular/CLI 22 with compatible Node.js and TypeScript versions |
| Package management | npm with a committed lockfile |
| Build | Angular CLI `application` builder and its Vite development server |
| Routing | Angular Router with lazy route groups and functional guards |
| Components | Angular Material/CDK 22 |
| Styling | Tailwind CSS 4 for layout; Angular Material 3 theme tokens for components |
| Forms | Angular 22 Signal Forms |
| Tables | Material Table, Sort, and Paginator with server-side data |
| HTTP and server state | Generated OpenAPI types; Angular `httpResource` for reads and `HttpClient` for commands |
| Local state | Signals and `computed`; list state in URL query parameters |
| Icons | Lucide SVG icons via `@ng-icons` |
| Unit/integration tests | Angular CLI Vitest, `TestBed`, HTTP and router test utilities |
| End-to-end tests | Playwright against the running application stack |

Keep Angular packages aligned, pin Material/CDK to the same release, and commit `package-lock.json`.

Angular Material and PrimeNG are two of the most popular full Angular component libraries. Material is the default because it is first-party, MIT-licensed, and sufficient here; PrimeNG is not MIT-licensed.

## Structure

Use route-oriented feature slices and keep code with its only consumer until reuse is demonstrated.

```sh
apps/web/
├── e2e/                                     # Playwright
├── public/                                  # Static assets
├── src/
│   ├── app/
│   │   ├── core/                            # App-wide infrastructure
│   │   │   ├── api/
│   │   │   │   └── generated/               # OpenAPI types
│   │   │   ├── config/
│   │   │   ├── http/
│   │   │   └── identity/
│   │   ├── features/                        # Route slices
│   │   │   ├── claimant/
│   │   │   │   ├── my-claims/
│   │   │   │   ├── claim-details/
│   │   │   │   ├── submit-claim/
│   │   │   │   └── claimant.routes.ts
│   │   │   └── officer/
│   │   │       ├── dashboard/
│   │   │       ├── work-queue/
│   │   │       ├── claim-review/
│   │   │       └── officer.routes.ts
│   │   ├── layout/                          # Application shell
│   │   │   ├── app-shell/
│   │   │   └── user-switcher/
│   │   ├── shared/                          # Reused UI
│   │   │   └── ui/
│   │   ├── app.config.ts
│   │   ├── app.routes.ts
│   │   └── app.ts
│   ├── styles/                              # Brand and Material theme
│   │   ├── chubb-tokens.scss
│   │   └── material-theme.scss
│   ├── index.html
│   ├── main.ts
│   └── styles.css
├── angular.json
├── package-lock.json
└── package.json
```

Keep each page and its supporting code in the same route folder. Start flat, create subfolders only for cohesive units, and move code to `shared` only after it is reused. `core` holds app-wide infrastructure, `layout` owns the shell, and the claimant and officer routes are lazy-loaded.

## Modern Angular conventions

- Use standalone components, strict templates, zoneless change detection, OnPush, and the current CLI file naming style.
- Prefer signals and `computed`, signal inputs/outputs, built-in control flow, and functional interceptors and guards.
- Use `httpResource` for reads and `@defer` only for a genuinely heavy, non-critical view.

## Data and state

Generate runtime-free OpenAPI types with `openapi-typescript`; never edit them by hand and check drift in CI. Use signals for local state, URL parameters for list state, `httpResource` for reads, and `HttpClient` for commands. Keep RxJS inside the HTTP adapter and do not add TanStack Query or NgRx.

An interceptor adds the seeded-user header. Commands observe the full response, replace the displayed claim with its body, retain its `ETag`, and refresh related history and lists. Send `If-Match` on updates except `take`. On a conflict, show the latest server state without clearing typed input. Map RFC 9457 errors by `code`.

## Forms and tables

Use Signal Forms with native Material inputs where possible, keeping the model, validation, and request mapping together. Use typed Reactive Forms only when a complex control needs them. Preserve input after errors, prevent duplicate submissions, and confirm final decisions.

Use Material Table, Sort, and Paginator with server-side operations. Provide typed columns, loading and empty states, and responsive overflow. Virtual scrolling is unnecessary for the page sizes involved.

## Visual system

Install Tailwind through `ng add tailwindcss`. Use it for application layout and Material's supported theme tokens for components; do not style Material internals.

Generate a Material 3 tonal palette from Chubb dark blue with the Material `theme-color` schematic. Expose all nine official Chubb colours separately as application tokens:

```text
#01C1D6  #FF6600  #150F96  #6E27C5  #7ACB00
#FF0198  #F1352B  #FFB617  #AFAFAF
```

Publish the brand colours as CSS variables and use the generated palette for Material. Use dark blue for interactive elements and reserve red/green/yellow/orange for status.

Use authorised `Publico Chubb` and logo assets if supplied; otherwise use Georgia for brand headings and system sans-serif for UI. Import Lucide SVGs individually through `@ng-icons` and use them alongside labels.

## Testing and tooling

- Unit: Vitest for schemas, mappers, formatters, and other pure logic.
- Integration: `TestBed`, Material harnesses, `HttpTestingController`, and `RouterTestingHarness` for forms, routing, tables, errors, and conflicts.
- E2E: Playwright for one cross-role journey against deterministic seed data; add more journeys if time permits.
- Tooling: Angular ESLint flat config with TypeScript, Vitest, import-order, and `eslint-plugin-better-tailwindcss` correctness rules; Prettier with `eslint-config-prettier` and `prettier-plugin-tailwindcss`.
- Defer MSW, Storybook, visual regression, and additional utility libraries until there is a demonstrated need.
