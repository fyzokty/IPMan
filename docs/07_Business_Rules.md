---
title: IPMan Business Rules
version: 1.0.0
status: Approved
---

# Business Rules

- **BR-001** Current state is whatever Windows reports after refresh.
- **BR-002** Selected adapter defines command scope.
- **BR-003** Disconnected state does not make an adapter read-only.
- **BR-004** Static configuration requires valid IPv4 and subnet mask.
- **BR-005** Gateway is optional.
- **BR-006** DNS is optional.
- **BR-007** A suspected IP conflict is a warning, not proof of collision.
- **BR-008** Equivalent desired/current configuration produces no Windows write.
- **BR-009** Rollback snapshot is captured before every attempted mutation.
- **BR-010** Profile load does not apply by default.
- **BR-011** User may persistently enable immediate profile apply.
- **BR-012** Duplicate profile names are resolved by numbering; existing file is
  not silently replaced.
- **BR-013** Favorites sort before non-favorites.
- **BR-014** Profiles sort alphabetically within grouping.
- **BR-015** One malformed profile cannot invalidate healthy profiles.
- **BR-016** Profile deletion is permanent after user confirmation.
- **BR-017** One profile is imported/exported at a time in release 1.0.
- **BR-018** Initial release user interface is Turkish.
- **BR-019** Core use does not require Internet access.
- **BR-020** IPMan runs elevated.
- **BR-021** Only one interactive IPMan instance is allowed.
- **BR-022** No automatic update functionality is required in release 1.0.
