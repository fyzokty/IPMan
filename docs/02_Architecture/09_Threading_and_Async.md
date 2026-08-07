---
title: Threading and Async Architecture
version: 1.0.0
status: Approved
---

# UI thread rule

No blocking network-management or file I/O on the WPF UI thread.

# WMI

`System.Management` operations are synchronous/blocking.

Infrastructure must isolate these calls from the UI thread, for example through
a controlled worker/task boundary.

Do not scatter `Task.Run` calls through ViewModels.

# Cancellation

Use `CancellationToken` on application-facing async APIs.

Cancellation does not imply a native WMI call can always be interrupted after it
has entered COM/WMI. Services must distinguish:
- cancellation before start,
- cancellation while waiting/reconciling,
- non-cancellable critical mutation already in progress.

# Concurrent mutation

Only one configuration mutation should run per adapter at a time.

Initial release may serialize all network mutation globally for additional
safety.

Do not allow overlapping static/DHCP/rollback operations against the same
adapter.

# Refresh during mutation

NetworkChange events will occur because IPMan itself changes configuration.

Refresh coordinator must coalesce these events and avoid racing verification.

The apply workflow owns final verification read.
