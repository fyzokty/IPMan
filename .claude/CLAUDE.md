# CLAUDE.md — IPMan Repository Instructions

This file is mandatory guidance for Claude Code.

## 1. Read before coding

Before modifying implementation, read:

1. `README.md`
2. `docs/00_Project/00_Project_Charter.md`
3. `docs/00_Project/01_Project_Vision.md`
4. `docs/01_Requirements/01_Product_Requirements_Baseline.md`
5. Relevant files under `docs/ADR/`
6. The current sprint/task document when one exists.

## 2. Source of truth

Do not invent product behavior.

When documentation is explicit, follow it.

When documentation conflicts:
- stop the conflicting implementation change,
- identify the conflicting documents,
- prefer the newer approved decision only when version/status metadata clearly
  establishes precedence,
- otherwise request a product decision.

## 3. Architecture rules

- Target .NET 8 and Windows 10/11 x64.
- UI technology: WPF.
- Architecture: MVVM.
- Use dependency injection.
- Views contain presentation only.
- ViewModels coordinate presentation state and commands.
- Windows networking operations must be behind interfaces/services.
- File persistence must be behind interfaces/services.
- Do not invoke `netsh`, PowerShell or CMD as the default network-management
  implementation.
- Prefer supported Windows APIs / management interfaces.
- Command-line fallback, if ever required, must be isolated and documented by
  an ADR before introduction.
- Keep all long-running and I/O work off the UI thread.
- Do not use `async void` except event handlers where unavoidable.

## 4. UI rules

- Single main window.
- Multiple network adapters are represented as tabs.
- Each adapter tab acts only on its own adapter.
- User-facing strings must come from localization resources.
- Initial language: Turkish.
- Theme choices: Follow Windows, Light, Dark.
- Preserve window size, position and last selected adapter where valid.
- Do not auto-apply a profile unless the user's saved setting enables it.

## 5. Network safety rules

Before applying a network configuration:
- validate all values,
- compare with current configuration,
- do nothing if configuration is already equivalent,
- capture a rollback snapshot,
- optionally warn on likely IPv4 conflict,
- apply changes,
- read the actual state back from Windows,
- report success only after verification.

If application fails:
- preserve recoverability,
- do not leave UI claiming an unverified state.

## 6. Persistence rules

Runtime application data belongs under:

`%LocalAppData%\IPMan\`

Expected logical subdirectories:
- `Config`
- `Profiles`
- `Backup`
- `Logs`
- `Temp`

Profiles are separate JSON files.

Settings are JSON.

Use explicit schema/version fields so files can be migrated in the future.

Write profile/settings changes immediately and safely.

## 7. Logging rules

Do not create verbose user activity surveillance.

Log critical technical failures only, such as:
- unreadable JSON,
- persistence failures,
- Windows API failures,
- unhandled exceptions.

Never log secrets or unnecessary personal information.

## 8. Coding rules

- Enable nullable reference types.
- Treat warnings as errors.
- Prefer small single-purpose types.
- Avoid service locator patterns.
- Avoid static mutable global state.
- Prefer immutable models where practical.
- Use cancellation tokens for operations that can reasonably be cancelled.
- Add XML docs where public behavior is non-obvious, not mechanically everywhere.
- Do not add dependencies without a clear requirement.

## 9. Testing rules

Every core service must be testable without launching WPF.

Abstract Windows-specific calls so unit tests can use fakes.

At minimum test:
- IPv4 validation,
- mask validation,
- profile serialization/deserialization,
- duplicate profile naming,
- settings persistence,
- comparison logic,
- rollback snapshot logic,
- adapter filtering,
- behavior when malformed JSON exists.

## 10. Change reporting

At the end of each development task, report:
- files changed,
- behavior implemented,
- tests added/run,
- unresolved issues,
- documentation updated.
