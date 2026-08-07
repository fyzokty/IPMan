---
title: Rollback Snapshot Capture
version: 1.0.0
status: Approved
---

# Rule

No Windows network mutation may begin until the current adapter configuration
has been captured successfully.

If rollback snapshot persistence fails:
- abort mutation,
- return a typed failure,
- do not touch the adapter.

# Storage

Use the approved local root:

`%LocalAppData%\IPMan\Backup\`

Rollback data is operational recovery data, not a user profile.

# Snapshot contents

At minimum persist:

- schema version,
- snapshot/correlation ID,
- captured UTC timestamp,
- adapter identity,
- adapter name/description as diagnostic metadata,
- DHCP/static/unknown mode,
- all IPv4 address+mask assignments,
- all IPv4 default gateways,
- all IPv4 DNS servers,
- relevant configuration fields needed for later restore,
- state marker such as Pending/Verified/Failed if that fits the design.

Do not depend on display name to restore identity.

# Write safety

Use safe JSON persistence:

1. serialize to a temporary file,
2. close/flush,
3. move/replace into final location,
4. never leave a half-written final JSON file.

Use `System.Text.Json`.

# Retention

Do not delete the most recent rollback snapshot merely because a mutation
reported success.

Later product work will expose `Son Yapılandırmayı Geri Yükle`.

The snapshot must remain available after application restart.

# Crash recovery

A snapshot captured before mutation should remain identifiable as potentially
recoverable if the process exits before verification.

Do not implement full crash-recovery UI in Sprint 07.
