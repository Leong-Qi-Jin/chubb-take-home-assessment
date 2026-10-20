# Contributing

Follow conventions in this order of precedence:

1. Project configuration, including compiler settings, analyzers, linters, and formatters.
2. This guide.
3. Established patterns in the relevant application or feature.
4. Official framework and library guidance.

Prefer idiomatic, well-architected solutions over minimising churn. Add dependencies only when they remove meaningful work or risk.

## File Organization

- Keep files close to the feature that owns and uses them.
- Move feature-specific code to a shared location only when another feature reuses it.
- A page that combines multiple features (e.g., a dashboard or profile page) may import from those features. Do not move feature-specific code into `shared` just to avoid that import.
- Keep a small, single-purpose module as a flat file. Create a folder when several related files belong together.
- Nest code that is used only within one feature. Avoid folders that contain one file and add no clarity.
- Check for import cycles before reorganising modules.
- Add a shared project only when at least two projects need it.

## Code

- Use comments to explain non-obvious intent, invariants, and edge cases. Match nearby comment style.
- Prefer fixing lint violations. When a suppression makes the code clearer or more maintainable, add a brief reason unless project configuration documents an exception.

### TypeScript and Angular

- Declare types, constants, and helpers immediately before first use; place shared declarations before their earliest use.
- Prefer signals for local state and RxJS for values that are naturally streams.

### C# and ASP.NET Core

- Pass request cancellation tokens through asynchronous EF Core operations.

## UI

- Prefer container-driven layouts: avoid negative margins, use gaps instead of sibling margins, and use container alignment over directional auto margins when practical.
- Build responsive, accessible interfaces using semantic HTML, keyboard support, visible focus, and associated labels and errors.
- Prefer `rem` over `px` except for borders.
- Place icons on the left side of button labels, except for directional arrows.

## Docs

- In Markdown, put `<!-- prettier-ignore -->` before each table so Prettier keeps it compact instead of padding the columns.

## Copy

- Use clear, plain, professional language for user-facing text, and label every value and message so its meaning is unambiguous.

## Tests

- Test observable behaviour and meaningful failure paths rather than implementation details.
- Do not hardcode values duplicated from production definitions. Avoid tautologies, assertions that repeat the implementation, and trivially different cases.
- Keep suites lean: prefer cases that cover different branches and regressions over many small input variations.
- Use realistic IDs, statuses, ranges, and other inputs that match the application.
- Remove flaky assertions rather than over-mocking to force them to pass.
- Run the narrowest relevant checks while iterating.
- Before handoff, run the affected application's formatter, linter, tests, and either its type check or build.
- Run all repository checks before delivery.
