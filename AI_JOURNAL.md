# AI Working Journal

Format per entry: prompt, what the AI did, human decision, and reasoning. The human decision records whether the user accepted, challenged, or overrode the AI's work. Keep it concise. See [docs/BRIEF.md](docs/BRIEF.md) deliverable 3.

## Entries

### 1. Repo setup

- **Prompt:** Convert brief.docx to BRIEF.md; add gitignore; fix lint; add AGENTS.md and AI_JOURNAL.md.
- **AI:** Did all of the above; recommended Markdown over CSV and Conventional Commits.
- **Human decision:** Accepted both and added a link to the Conventional Commits spec; challenged the first drafts as too wordy.
- **Reasoning:** Brief's wording kept as is; Markdown handles free-text reasoning better than CSV.

### 2. High-level architecture

- **Prompt:** Define the architecture using Angular, .NET, REST, and SQL Server without Kafka.
- **AI:** Proposed internal backend modules in addition to the system-level architecture.
- **Human decision:** Challenged the extra detail; kept one SPA, API, and database while deferring internal structure.
- **Reasoning:** Avoid premature decisions.

### 3. Implementation planning

- **Prompt:** Order the remaining design, setup, implementation, testing, and delivery work without deciding the architecture yet.
- **AI:** Proposed steps that were too broad and added a redundant explanation.
- **Human decision:** Challenged the initial scope, then accepted concise decision, setup, build, and delivery checkpoints.
- **Reasoning:** Technical details belong in later architecture discussions.

### 4. MVP scope

- **Prompt:** Define a complete but achievable MVP.
- **AI:** Proposed must-have, should-have, and deferred capabilities.
- **Human decision:** Accepted after expanding the core workflow and keeping production concerns deferred.
- **Reasoning:** Demonstrate the full claims journey while prioritising backend quality.

### 5. Domain and data model

- **Prompt:** Define and review the domain model, claim workflow, relational schema, history, and concurrency approach.
- **AI:** Proposed an initial model, then revised it through discussion and two independent reviews.
- **Human decision:** Challenged the wordy first draft; accepted after simplifying the scope, clarifying the workflow and terminology, and strengthening the database rules. Deferred user management and event-driven integrations.
- **Reasoning:** Keep the MVP practical and explainable, enforce rules in SQL Server where possible, and update claim state and history together in synchronous transactions.

### 6. API contract

- **Prompt:** Define the API contract, then review and refine it.
- **AI:** Proposed a REST contract with one endpoint per workflow action, standard error responses, and optimistic concurrency, then revised it through the discussion and four subagent reviews.
- **Human decision:** Challenged the drafts as wordy and requested independent reviews. Overrode public submission with seeded users identified by email, deferring user management. Limited claims to one open information request and deferred reassignment.
- **Reasoning:** Action endpoints map cleanly to domain rules and history events; seeded users and a single open request keep the MVP simple.

### 7. Screens and user flows

- **Prompt:** Add a planning step for the frontend screens after the API contract, then define them briefly.
- **AI:** Drafted the screens and user flows, then revised them after a self-review and two subagent reviews.
- **Human decision:** Challenged the drafts as under-checked and too wordy, and requested subagent reviews. Chose a header user switcher, shared loading and error states, and disabled over hidden actions. Overrode the AI's advice to defer pagination controls. Deferred encryption and data retention.
- **Reasoning:** Confirm that the API supports every screen before building; if the backend paginates, the UI should too.

### 8. Monorepo structure

- **Prompt:** Define and validate a monorepo structure for a feature-sliced Angular and .NET application.
- **AI:** Proposed a layout and checked it against official documentation and three independent reviews.
- **Human decision:** Challenged the detail level and root-level test ownership; accepted separate, feature-aligned test projects under `apps/api` and a self-contained `apps/web` without a root npm workspace.
- **Reasoning:** Keep ownership and feature navigation clear without over-explaining standard tooling.

### 9. Frontend architecture

- **Prompt:** Define a concise, modern Angular frontend architecture covering libraries, feature structure, data, forms, tables, styling, and testing.
- **AI:** Checked current Angular compatibility and licensing, proposed the architecture, revised it through discussion, and completed an independent final review.
- **Human decision:** Challenged the drafts as too long, technical, and prescriptive. Chose Angular Material over PrimeNG for its first-party, MIT-licensed fit; kept route-oriented slices, modern Angular defaults, Tailwind, Signal Forms, signals with minimal RxJS, Vitest, and Playwright; and deferred accessibility.
- **Reasoning:** Prefer first-party Angular defaults for this small application, adding focused libraries only where they improve developer experience without duplicating responsibilities.

### 10. Backend and database architecture

- **Prompt:** Decide how to structure the backend and database, including seed data, Docker, workflow state, and money.
- **AI:** Compared the current .NET options and suggested a single API organised around the claims operations, then shortened and independently reviewed the design.
- **Human decision:** Accepted .NET 10, Minimal APIs, vertical slices, EF Core, SQL Server, decimal amounts, and repeatable local seed data. Preferred built-in validation and tooling instead of adding libraries before they are needed.
- **Reasoning:** This is the newest stable .NET stack and fits the assessment without adding extra layers or moving parts.

### 11. Architecture detail level

- **Prompt:** Keep the frontend and backend architecture documents from locking in implementation details too early.
- **AI:** Removed repeated request behaviour, setup mechanics, and speculative tooling, then completed a final independent review across the architecture documents.
- **Human decision:** Accepted the stack, boundaries, feature structure, and core conventions while deferring lower-level mechanics to setup and implementation.
- **Reasoning:** Architecture should guide the build without prescribing details that have not yet been tested.

### 12. Testing strategy and document names

- **Prompt:** Define a concise testing strategy and review the architecture document names.
- **AI:** Added backend, frontend, and end-to-end coverage using mainstream tools.
- **Human decision:** Challenged the level of implementation detail and required conventional tools. Kept the concise strategy in the existing architecture documents, retained their filenames, and chose xUnit for .NET tests.
- **Reasoning:** Document test intent without adding another file or prescribing the test harness.

### 13. MVP platform concerns

- **Prompt:** Define identity, observability, configuration, and secrets handling without over-documenting them.
- **AI:** Consolidated the decisions into the backend architecture and aligned existing identity wording.
- **Human decision:** Accepted seeded development identities and built-in .NET capabilities while deferring production integrations.
- **Reasoning:** Keep the MVP safe and explainable without adding infrastructure outside the assessment scope.

### 14. Repository guidance

- **Prompt:** Add initial agent and contributor guidance, then refine its scope and wording.
- **AI:** Expanded `AGENTS.md` with repository context and operational safeguards. Added `CONTRIBUTING.md` with explicit precedence and focused conventions for file organization, code, UI, copy, and tests.
- **Human decision:** Challenged drafts as too detailed, repetitive, then too sparse. Kept workflows in `AGENTS.md`, preliminary decisions in the design documents, and focused conventions in `CONTRIBUTING.md`; restored precedence and clarified the structure and wording.
- **Reasoning:** Provide useful guardrails without repeating guidance or treating early design decisions as absolute.

### 15. Monorepo and application scaffolding

- **Prompt:** Create and review the monorepo and official application scaffolds, including run instructions and frontend formatting.
- **AI:** Scaffolded the .NET 10 solution, API and tests, and the Angular 22 SPA. Added run documentation and VS Code tasks, reviewed the output, and verified both applications locally, including the API through Postman.
- **Human decision:** Accepted the official scaffolds after requiring minimal deviation from CLI output. Chose conventional API ports, explicit HTTPS startup, pinned SDK and Node.js versions, manual `nvm use`, simple run documentation and VS Code tasks, and Prettier with an 80-character width and one HTML attribute per line; deferred extra lint flags.
- **Reasoning:** Keep the baseline buildable, convenient, and minimal.

### 16. Delivery plan refinement

- **Prompt:** Refine the remaining delivery plan.
- **AI:** Reordered tasks by dependency and delivery stage.
- **Human decision:** Accepted local environment and Docker setup before CI, persistence and seed data alongside features, and final documentation during delivery. Deferred the component catalogue and dedicated audit.
- **Reasoning:** Prioritise the MVP critical path.

### 17. Local SQL Server, environment, and run tooling

- **Prompt:** Set up local SQL Server and environment configuration; refine the Compose file, run commands, and VS Code tasks.
- **AI:** Added a SQL Server 2025 Compose service, an environment file template, and setup docs. Refined the run tooling and removed non-default Compose settings.
- **Human decision:** Accepted environment variables and a simple default password over `appsettings` and user secrets. Required Docker defaults, descriptive VS Code tasks, and nvm on macOS or Linux with `.nvmrc`. Overrode hard-coding the SQL Server port: kept it a variable reused in the connection string. Accepted explicit `linux/amd64` emulation for Apple Silicon.
- **Reasoning:** Keep secrets out of source control, use standard .NET configuration and tool defaults, run the x86-64-only SQL Server image on Apple Silicon, and prevent duplicated settings from drifting.

### 18. Scope reduction

- **Prompt:** Defer CI and accessibility and security work; merge overlapping tasks.
- **AI:** Updated the plan and design documents.
- **Human decision:** Replaced CI with pre-commit hooks and merged the testing and verification tasks. Challenged wordy drafts.
- **Reasoning:** Keep the take-home assessment focused on the MVP.

### 19. Database persistence and ERD

- **Prompts:** Implement database persistence, migrations, and development seed data. Have a subagent review the changes for readiness for the next plan step, document the work temporarily, and add an ERD that can be regenerated from the database.
- **AI:** Added the EF Core model, initial migration, Development startup migration and repeatable seed data, plus focused model and seed tests. Addressed review findings covering claimant pagination, culture-safe JSON, SQL Server compatibility, partial-seed recovery, and dataset coverage. Added an isolated Node.js tool that reads the live SQL Server catalog and inserts the resulting Mermaid diagram into a maintained Markdown template. A final review caught migration drift; the migration and ERD were regenerated and verified against a fresh database.
- **Human decision:** Challenged the initial ERD approach and unclear documentation wording. Accepted the persistence work after requiring seed data for the next `PLAN.md` step, and chose a Node.js generator, a Markdown template, and `docs/generated` for the generated ERD. Replaced precise column sizes with standard tiers and money with `decimal(18,2)`, and trimmed wordy docs.
- **Reasoning:** Make local development immediately usable with representative workflow data, while keeping the checked-in schema reference reproducible and aligned with the migrated database. Standard sizes are easier to explain.

### 20. Backend features

- **Prompts:** Build the prioritised backend features. Summarise the change in `CHANGE.md`, have a subagent review it, then review the folder structure for leftovers from the multi-feature build. Later, triage external review findings and run a final review of ASP.NET Core conventions and unnecessary classes.
- **AI:** Added the claim workflow rules to the domain and implemented every contract endpoint as a vertical slice, with development identity, consistent error codes, optimistic concurrency, and the workload dashboard. Added domain unit tests and integration tests against a temporary database. Fixed review findings, including two server errors and incomplete OpenAPI error metadata, and moved response types shared with the dashboard out of the claims feature. Suggested a database table for supported currencies.
- **Human decision:** Requested the independent subagent review, and a final review of the folder structure to catch code left inconsistent with the feature-slice layout after building many features at once. Challenged wordy and technical architecture notes, and tests for edge cases the MVP does not need. Overrode the currencies table: kept the list in app settings and exposed it through an endpoint so the SPA does not duplicate it.
- **Reasoning:** Keep documentation readable and tests focused on MVP rules. App settings are enough for a short, rarely changing list.

### 21. AI journal format

- **Prompt:** Keep a concise record of the prompts, AI work, human decisions, and reasoning.
- **AI:** Maintained the working journal as the project evolved.
- **Human decision:** Changed the journal to numbered, topic-based entries with explicit human decisions.
- **Reasoning:** Make decision ownership and the development process clear.

### 22. Frontend features

- **Prompts:** Build the matching frontend features and verify them in the browser. Over many review rounds, generate the API client from OpenAPI, match Chubb's design language, fix reported UI issues, and check mobile, keyboard, and every state. Later, restructure into feature slices, remove duplication, follow current Angular conventions, set up linting, and have a subagent audit the UI.
- **AI:** Built the claimant and officer journeys on a generated `@hey-api/openapi-ts` client, which exposed a wrongly named status. Matched chubb.com's typography, colours, shapes, and buttons, and audited layout, contrast, keyboard use, and states in the browser, fixing several real bugs. Consolidated shared components, variants, and mappings, and acted on most of the subagent's findings.
- **Human decision:** Required a generated contract, browser evidence, and reusable components over per-page fixes. Held the UI to Chubb's real design language, correcting colours, spacing, hierarchy, radii, and hover states, and removing decorative extras. Rejected casual copy and asked for every value to be labelled. Asked for dashboard totals to open the filtered queue, Material date pickers, fully clickable radio cards, and descriptive page titles. Named the app ClaimTrack, asked for feature slices, current Angular conventions, and repository-wide linting, and allowed removing only clearly unnecessary edge cases. Accepted keeping officer actions in the side column, demo users in the frontend, and hey-api.
- **Reasoning:** Generated contracts catch backend drift at compile time, shared tokens and components keep one consistent brand, and browser checks catch what code review misses.

### 23. UI screenshot documentation

- **Prompt:** Generate laptop and phone screenshots with a script, publish them in Markdown, and show the dashboard in the README.
- **AI:** Added `tools/ui-screenshots`, modelled on the ERD tool, which captures the main screens with Playwright at full page height and writes `docs/generated/ui-screenshots.md`.
- **Human decision:** Asked for generated rather than hand-taken screenshots and compact Markdown tables.
- **Reasoning:** A script keeps the visual documentation in step with the UI and makes review possible without running the stack.

### 24. Delivery documentation and quality tooling

- **Prompts:** Complete the delivery documentation, conventions, shared commands, quality checks, and development database reset.
- **AI:** Consolidated the project documentation, command structure, editor tasks, and quality tooling; reset the database and verified the repository checks.
- **Human decision:** Preferred one canonical README, focused conventions, commands owned at the lowest practical level, and no dependency overrides.
- **Reasoning:** Keep guidance concise and commands maintainable while enforcing consistent repository-wide checks.
