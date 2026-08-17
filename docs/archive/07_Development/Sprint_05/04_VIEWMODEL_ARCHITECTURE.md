---
title: Sprint 05 ViewModel Architecture Constraints
version: 1.0.0
status: Approved
---

# ViewModel principles

The current temporary ViewModel may be refactored into smaller presentation
ViewModels if needed.

Expected conceptual responsibilities:

## Main window ViewModel

Coordinates:
- adapter collection,
- selected adapter,
- global loading/error state,
- status bar state.

It must not read Windows APIs directly.

## Adapter tab/item ViewModel

Represents:
- identity,
- display name,
- connection presentation state.

## Selected adapter details ViewModel

Projects current snapshot into read-only display values.

## Adapter edit-draft ViewModel

Owns:
- IPv4 text,
- mask text,
- gateway text,
- primary DNS text,
- secondary DNS text,
- dirty state,
- copy commands,
- reset/get-current-values command.

Exact class structure is Claude's implementation choice, but avoid one enormous
ViewModel that mixes every future feature.

# Snapshot/draft separation

Do not mutate `NetworkAdapterSnapshot`.

Domain snapshots are current-state facts.

Draft state is presentation/application state.

# UI dispatcher

Continue using the approved `IUiDispatcher` boundary.

Do not call WPF Dispatcher from Infrastructure or Application.

# Clipboard

Clipboard integration is a presentation/OS UI concern.

Keep it behind a small abstraction if needed for ViewModel testability.

Do not place direct static clipboard calls throughout ViewModels.

# Administrator status

Reading/displaying current elevation status belongs in an App/Infrastructure
boundary, not hard-coded as `"Administrator"`.

If a minimal abstraction is required, keep it narrowly scoped.
