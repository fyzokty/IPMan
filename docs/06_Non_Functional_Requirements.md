---
title: IPMan Non-Functional Requirements
version: 1.0.0
status: Approved
---

# Non-Functional Requirements

## Performance

- **NFR-PERF-001** The UI shall remain responsive during adapter discovery,
  profile I/O and network configuration changes.
- **NFR-PERF-002** Idle CPU usage should remain near zero under normal stable
  network conditions.
- **NFR-PERF-003** Event-driven observation is preferred to sub-second polling.
- **NFR-PERF-004** Startup shall present the main window promptly; slow data may
  populate progressively with loading states.

## Reliability

- **NFR-REL-001** A malformed profile file shall not crash application startup.
- **NFR-REL-002** Profile/settings writes shall minimize risk of partial-file
  corruption.
- **NFR-REL-003** Configuration success shall not be reported until Windows state
  has been reread and checked.
- **NFR-REL-004** Rollback information shall be captured before configuration
  mutation.
- **NFR-REL-005** Unexpected fatal failures shall produce a critical log.

## Security

- **NFR-SEC-001** Use Windows UAC for elevation.
- **NFR-SEC-002** Do not request/store administrator credentials.
- **NFR-SEC-003** Do not transmit network/profile data externally.
- **NFR-SEC-004** Validate imported JSON before treating it as a valid profile.
- **NFR-SEC-005** Treat external profile filenames/content as untrusted input.

## Maintainability

- **NFR-MNT-001** Use MVVM.
- **NFR-MNT-002** Isolate Windows networking behind interfaces.
- **NFR-MNT-003** Isolate persistence behind interfaces.
- **NFR-MNT-004** Use dependency injection.
- **NFR-MNT-005** Enable nullable reference types.
- **NFR-MNT-006** Treat compiler warnings as errors.
- **NFR-MNT-007** Keep core logic unit-testable without WPF.

## Compatibility

- **NFR-COMP-001** Support Windows 10 x64.
- **NFR-COMP-002** Support Windows 11 x64.
- **NFR-COMP-003** Target .NET 8 LTS.
- **NFR-COMP-004** Provide portable and installer-based release forms.

## Usability

- **NFR-UX-001** Connection state shall never be communicated by color alone.
- **NFR-UX-002** Invalid IP input shall provide field-specific feedback.
- **NFR-UX-003** Main workflows shall not require command-line knowledge.
- **NFR-UX-004** Important failures shall have persistent in-app feedback in
  addition to transient notifications.
- **NFR-UX-005** Application shall support keyboard focus/navigation consistent
  with normal Windows desktop expectations, even though custom keyboard
  shortcuts are not required in release 1.0.

## Localization

- **NFR-L10N-001** User-facing strings shall be externalized.
- **NFR-L10N-002** Turkish is the only required translation for release 1.0.
- **NFR-L10N-003** UI layout shall tolerate future translated strings.

## Privacy

- **NFR-PRV-001** No analytics/telemetry is required.
- **NFR-PRV-002** No routine user action history is stored.
- **NFR-PRV-003** Critical logs should contain only troubleshooting-relevant
  technical data.
