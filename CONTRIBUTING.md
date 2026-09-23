# Contributing to IPMan

IPMan follows a documentation-first workflow.

## Before changing code

1. Read the relevant requirement documents.
2. Read applicable ADRs.
3. Do not change established architecture implicitly.
4. If a requirement is ambiguous, record the ambiguity instead of inventing
   product behavior.

## Change discipline

- Keep changes focused.
- Avoid unrelated refactors.
- Keep UI logic out of Views.
- Keep Windows-specific implementation behind interfaces.
- Keep user-facing strings localizable.
- Add or update tests when behavior changes.
- Update documentation when public behavior changes.

## Commit style

Use concise imperative messages, for example:

- `Add adapter discovery service`
- `Implement profile JSON validation`
- `Fix DHCP state refresh`
