---
title: IPMan Use Cases
version: 1.0.0
status: Approved
---

# UC-01 — Apply static IPv4 configuration

## Preconditions
- IPMan runs elevated.
- An adapter is selected.

## Main flow
1. User enters IPv4 and mask.
2. User optionally enters gateway and DNS.
3. UI validates values.
4. User selects Apply.
5. IPMan compares desired/current state.
6. IPMan captures rollback snapshot.
7. IPMan performs best-effort conflict check.
8. If no blocking validation issue exists, IPMan applies configuration.
9. IPMan rereads Windows state.
10. IPMan verifies outcome.
11. UI refreshes and notification is shown.

## Alternatives
- Configuration already matches: no Windows write is performed.
- Suspected conflict: user is warned and may continue.
- Apply fails: failure is shown and rollback remains available.

# UC-02 — Switch selected adapter to DHCP

1. User selects adapter.
2. User chooses DHCP.
3. IPMan captures rollback snapshot.
4. IPMan enables automatic addressing and DNS.
5. IPMan rereads and verifies state.
6. UI refreshes.

# UC-03 — Load a profile safely

1. User selects a profile.
2. By default IPMan loads profile values into edit fields.
3. Differences from current configuration are highlighted.
4. User may edit values.
5. User explicitly applies.

If auto-apply is enabled, step 5 is triggered automatically but still uses the
standard validation and safety workflow.

# UC-04 — Save a profile

1. User enters/loads desired configuration.
2. User chooses Save Profile.
3. User supplies name and optional description.
4. IPMan creates standalone JSON.
5. If name already exists, IPMan uses next available numbered name.
6. Profile panel refreshes immediately.

# UC-05 — Restore last configuration

1. User chooses restore-last-configuration.
2. IPMan identifies the relevant rollback snapshot.
3. User confirms if confirmation is required by UI specification.
4. IPMan applies rollback state.
5. IPMan rereads and verifies Windows state.

# UC-06 — Adapter inserted while running

1. Windows exposes a new adapter.
2. IPMan receives a network-change event.
3. IPMan discovers adapter metadata/state.
4. New tab is created.
5. UI remains usable.

# UC-07 — Adapter removed while running

1. Adapter disappears.
2. IPMan receives event/reconciliation result.
3. Tab is removed.
4. Selection moves to a valid remaining adapter.
5. Notification identifies removed adapter.

# UC-08 — Import profile

1. User chooses import.
2. User selects JSON file.
3. IPMan validates schema/content.
4. Valid profile is copied into application profile storage.
5. Name collisions use safe numbering.
6. Profile list refreshes.

# UC-09 — Malformed profile detected

1. Profile watcher/loader sees JSON file.
2. Parsing or validation fails.
3. Remaining profiles continue loading.
4. Invalid file is preserved.
5. UI surfaces the problematic file.
6. Critical technical issue is logged.

# UC-10 — Second application launch

1. IPMan is already running.
2. User launches IPMan again.
3. New process signals existing instance.
4. Existing main window is restored and focused.
5. Second process exits.
