---
title: IPMan Engineering and Product Principles
version: 1.0.0
status: Approved
---

# Project Principles

1. Windows is the authority for current network state.
2. Selecting an adapter scopes all configuration actions to that adapter.
3. Disconnected adapters remain configurable.
4. Profile selection does not automatically apply by default.
5. Users may enable automatic profile application in Settings.
6. Every configuration change is verified by rereading Windows state.
7. A rollback snapshot is captured before modifications.
8. Duplicate profile names are preserved through automatic numbering.
9. Routine user activity is not logged; critical technical failures are.
10. Profiles are portable standalone JSON files.
11. The application is offline-first and requires no Internet connection.
12. Localization is architectural from day one even though v1 ships in Turkish.
13. UI stays responsive during all I/O and Windows management operations.
14. Adapter observation should be event-driven, with only lightweight fallback
    reconciliation where necessary.
