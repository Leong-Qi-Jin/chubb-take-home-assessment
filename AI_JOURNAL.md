# AI Working Journal

Format per entry: prompt, what the AI did, decision (accepted / challenged / overrode), reasoning. Keep it concise. See [docs/BRIEF.md](docs/BRIEF.md) deliverable 3.

## Entries

### 2026-10-18: repo setup

- **Prompt:** Convert brief.docx to BRIEF.md; add gitignore; fix lint; add AGENTS.md and AI_JOURNAL.md.
- **AI:** Did all of the above.
- **Decision:** Accepted; shortened the first drafts of AGENTS.md and the journal.
- **Reasoning:** Brief's wording kept as is; Markdown selected over CSV for the journal.

### 2026-10-18: high-level architecture

- **Prompt:** Define the architecture using Angular, .NET, REST, and SQL Server without Kafka.
- **AI:** Suggested internal modules and a broader delivery plan.
- **Decision:** Rejected both as premature; kept only the agreed SPA, API, and database architecture.
- **Reasoning:** Avoid premature decisions.

### 2026-10-18: implementation planning

- **Prompt:** Order the remaining design, setup, implementation, testing, and delivery work without deciding the architecture yet.
- **AI:** Proposed steps that were too broad and added a redundant explanation.
- **Decision:** Accepted after reducing it to concise decision, setup, build, and delivery checkpoints.
- **Reasoning:** Technical details belong in later architecture discussions.

### 2026-10-18: MVP scope

- **Prompt:** Define a complete but achievable MVP.
- **AI:** Proposed must-have, should-have, and deferred capabilities.
- **Decision:** Accepted after expanding the core workflow and keeping production concerns deferred.
- **Reasoning:** Demonstrate the full claims journey while prioritising backend quality.

### 2026-10-18: domain and data model

- **Prompt:** Define and review the domain model, claim workflow, relational schema, history, and concurrency approach.
- **AI:** Proposed an initial model, then revised it through discussion and two independent reviews.
- **Decision:** Accepted after shortening the document, fixing the diagram, clarifying names, removing non-MVP fields, supporting multiple cancellable information requests, and strengthening database constraints and race handling.
- **Reasoning:** Keep the MVP practical and explainable, enforce rules in SQL Server where possible, and update claim state and history together in synchronous transactions.

### 2026-10-18: API contract

- **Prompt:** Define the API contract, then review and refine it.
- **AI:** Proposed a REST contract with one endpoint per workflow action, standard error responses, and optimistic concurrency, then revised it through the discussion and four subagent reviews.
- **Decision:** Accepted after shortening the wording, restructuring the layout, and reviewing the roles, query parameters, status codes, and error codes. Limited claims to one open information request and kept reassignment deferred, to keep the workflow simple. Overrode the claimant identity to use seeded users with verified emails instead of user management, and updated the domain model to match, including missing indexes and constraints found in review.
- **Reasoning:** Action endpoints map cleanly to domain rules and history events; seeded users keep the MVP simple until the identity step.

### 2026-10-18: screens and user flows

- **Prompt:** Add a planning step for the frontend screens after the API contract, then define them briefly.
- **AI:** Drafted the screens and user flows, then revised them after a self-review and two subagent reviews.
- **Decision:** Challenged the first draft as under-checked and later as too wordy; accepted after closing gaps against the API contract and domain model. Overrode the AI's advice to defer pagination controls in the UI, updating the plan and API contract to match, and chose disabled over hidden actions.
- **Reasoning:** Confirm that the API supports every screen before building; if the backend paginates, the UI should too.

### 2026-10-18: monorepo structure

- **Prompt:** Define and validate a monorepo structure for a feature-sliced Angular and .NET application.
- **AI:** Proposed a layout and checked it against official documentation and three independent reviews.
- **Decision:** Challenged the level of detail. Kept self-contained app folders, placed separate backend test projects under `apps/api`, and removed the unnecessary root npm workspace.
- **Reasoning:** Keep ownership and feature navigation clear without over-explaining standard tooling.

### 2026-10-18: frontend architecture

- **Prompt:** Define a concise, modern Angular frontend architecture covering libraries, feature structure, data, forms, tables, styling, and testing.
- **AI:** Checked current Angular compatibility and licensing, proposed the architecture, revised it through discussion, and completed an independent final review.
- **Decision:** Accepted Angular Material over PrimeNG, Signal Forms, Material's server-backed table controls, `httpResource` reads, `HttpClient` commands, Tailwind layout, Lucide SVG icons, Vitest, Playwright, route-oriented feature slices, and minimal RxJS, with accessibility deferred.
- **Reasoning:** Prefer first-party Angular defaults for this small application, adding focused libraries only where they improve developer experience without duplicating responsibilities.

### 2026-10-18: backend and database architecture

- **Prompt:** Decide how to structure the backend and database, including seed data, Docker, workflow state, and money.
- **AI:** Compared the current .NET options and suggested a single API organised around the claims operations, then shortened and independently reviewed the design.
- **Decision:** Use .NET 10, Minimal APIs, vertical slices, EF Core, SQL Server, decimal amounts, and repeatable local seed data. Prefer built-in validation and tooling instead of adding libraries before they are needed.
- **Reasoning:** This is the newest stable .NET stack and fits the assessment without adding extra layers or moving parts.

### 2026-10-18: architecture detail level

- **Prompt:** Keep the frontend and backend architecture documents from locking in implementation details too early.
- **AI:** Removed repeated request behaviour, setup mechanics, and speculative tooling, then completed a final independent review across the architecture documents.
- **Decision:** Keep the stack, boundaries, feature structure, and core conventions; decide lower-level mechanics during setup and implementation.
- **Reasoning:** Architecture should guide the build without prescribing details that have not yet been tested.

### 2026-10-18: testing strategy and document names

- **Prompt:** Define a concise testing strategy and review the architecture document names.
- **AI:** Added backend, frontend, and end-to-end coverage using mainstream tools.
- **Decision:** Challenged implementation detail; kept the strategy in the relevant architecture documents and retained the existing filenames.
- **Reasoning:** Document test intent without adding another file or prescribing the test harness.

### 2026-10-18: MVP platform concerns

- **Prompt:** Define identity, observability, configuration, and secrets handling without over-documenting them.
- **AI:** Consolidated the decisions into the backend architecture and aligned existing identity wording.
- **Decision:** Use seeded development identities and built-in .NET capabilities; defer production integrations.
- **Reasoning:** Keep the MVP safe and explainable without adding infrastructure outside the assessment scope.

### 2026-10-18: repository guidance

- **Prompt:** Add initial agent and contributor guidance, using sibling projects as references, then refine its scope and wording.
- **AI:** Expanded `AGENTS.md` with repository context and operational safeguards. Added `CONTRIBUTING.md` with explicit precedence and focused conventions for file organization, code, UI, copy, and tests.
- **Decision:** Kept agent workflow in `AGENTS.md`, preliminary implementation decisions in the design documents, and durable, non-obvious conventions in `CONTRIBUTING.md`. Removed duplicated, basic, and implementation-heavy rules and clarified ambiguous wording.
- **Reasoning:** Provide useful guardrails without repeating guidance or treating early design decisions as absolute.

### 2026-10-19: monorepo and application scaffolding

- **Prompt:** Create and review the monorepo and official application scaffolds, including run instructions and frontend formatting.
- **AI:** Scaffolded the .NET 10 solution, API and tests, and the Angular 22 SPA. Added run documentation and VS Code tasks, reviewed the output, and verified both applications locally, including the API through Postman.
- **Decision:** Kept the CLI defaults except for stable local API ports and a Node version pin, removed editor files and placeholder tests, and added only the requested tasks. Added Tailwind-aware Prettier formatting with an 80-character width and one HTML attribute per line; deferred extra lint flags.
- **Reasoning:** Keep the baseline buildable, convenient, and minimal.

### 2026-10-19: delivery plan refinement

- **Prompt:** Refine the remaining delivery plan.
- **AI:** Reordered tasks by dependency and delivery stage.
- **Decision:** Set up the local environment and Docker before CI; build persistence and seed data with the features; keep final documentation in delivery. Deferred the component catalogue and dedicated audit.
- **Reasoning:** Prioritise the MVP critical path.
