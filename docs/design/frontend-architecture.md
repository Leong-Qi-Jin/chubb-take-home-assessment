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
| Icons | Lucide SVG icons via `@ng-icons/core` and `@ng-icons/lucide` |
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
│   │   │   ├── http/                        # App-wide HTTP setup
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

Keep each page and its supporting code in the same route folder. Start flat, create subfolders only for cohesive units, and move code to `shared` only after it is reused. `core` holds app-wide infrastructure, including shared HTTP setup; feature-specific adapters stay with their route slices. `layout` owns the shell, and the claimant and officer routes are lazy-loaded.

## Modern Angular conventions

- Use standalone components, strict templates, zoneless change detection, OnPush, and the current CLI file naming style.
- Prefer signals and `computed`, signal inputs/outputs, built-in control flow, and functional interceptors and guards.

## Data and state

Generate runtime-free OpenAPI types with `openapi-typescript`. Use signals for local state, URL parameters for list state, `httpResource` for reads, and `HttpClient` for commands. Do not add RxJS-based application state; use RxJS only where Angular APIs require it. TanStack Query and NgRx are unnecessary for this application.

## Forms and tables

Use Signal Forms with native Material inputs where possible. Use typed Reactive Forms only when a complex control requires them.

Use Material Table, Sort, and Paginator with the server-side operations defined by the API.

## Visual system

Use Tailwind for application layout and Material theme tokens for components. Base the Material 3 theme on Chubb dark blue and expose the official Chubb colours as application tokens:

```text
#01C1D6  #FF6600  #150F96  #6E27C5  #7ACB00
#FF0198  #F1352B  #FFB617  #AFAFAF
```

Import Lucide SVGs individually.

## Tooling

Use Angular ESLint flat config with TypeScript, Vitest, import-order, and `eslint-plugin-better-tailwindcss` correctness rules. Use Prettier with `eslint-config-prettier` and `prettier-plugin-tailwindcss`.
