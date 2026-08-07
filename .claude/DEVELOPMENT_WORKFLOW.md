# Development Workflow

## Task lifecycle

1. Read repository instructions.
2. Read the current requirement(s).
3. Read related ADRs.
4. Inspect existing implementation before creating new abstractions.
5. Implement the smallest coherent change.
6. Add/update tests.
7. Run build and tests.
8. Update documentation if behavior changed.
9. Summarize changed files and remaining risks.

## Prohibited workflow

Do not:
- rewrite the whole architecture for a local feature,
- add packages solely for convenience,
- place Windows networking commands directly in ViewModels,
- hard-code Turkish UI text inside ViewModels or services,
- suppress compiler warnings without explaining the cause.
