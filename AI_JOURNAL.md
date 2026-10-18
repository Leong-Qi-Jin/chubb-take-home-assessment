# AI Working Journal

Format per entry: prompt, what the AI did, decision (accepted / challenged / overrode), reasoning. Keep it concise. See [docs/BRIEF.md](docs/BRIEF.md) deliverable 3.

## Entries

### 2026-10-18: repo setup

- **Prompt:** Convert brief.docx to BRIEF.md; add gitignore; fix lint; add AGENTS.md and AI_JOURNAL.md.
- **AI:** Did all of the above.
- **Decision:** accepted; shortened the first drafts of AGENTS.md and the journal.
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
- **Decision:** Accepted after shortening the wording, restructuring the layout, and reviewing the roles, query parameters, status codes, and error codes. Overrode the claimant identity to use seeded users with verified emails instead of user management, and updated the domain model to match, including missing indexes and constraints found in review.
- **Reasoning:** Action endpoints map cleanly to domain rules and history events; seeded users keep the MVP simple until the identity step.
