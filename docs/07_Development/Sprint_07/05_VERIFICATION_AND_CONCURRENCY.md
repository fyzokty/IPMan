---
title: Verification and Concurrency
version: 1.0.0
status: Approved
---

# Verification

Windows network state can settle asynchronously after mutation.

Use a bounded reconciliation strategy:

- immediate fresh read,
- if not yet equivalent, a small number of delayed retries,
- stop once verified,
- stop on adapter disappearance,
- respect cancellation where safe.

Do not use an infinite polling loop.

Suggested architecture target:
- total verification window roughly a few seconds,
- deterministic options object,
- delay abstraction for tests.

Do not hard-code sleeps in ViewModels.

# NetworkChange interaction

IPMan's own mutation will raise network change events.

The existing refresh coordinator may receive those events.

The mutation/apply service owns its own final verification and must not depend on
the UI refresh event arriving first.

# Mutation lock

Only one mutation may run against a given adapter at a time.

For release 1.0 it is acceptable to serialize all network mutations globally if
that makes correctness simpler.

Do not permit:
- two static applies concurrently,
- static apply and future DHCP mutation concurrently,
- future rollback restore racing an apply.

Use a testable application/service coordination mechanism.

# Cancellation

Before mutation:
- cancellation may stop normally.

After a critical native/WMI mutation call has begun:
- do not pretend cancellation rolled it back.

Complete enough of the workflow to fresh-read and return the actual/indeterminate
result.

Document this behavior in code/tests.
