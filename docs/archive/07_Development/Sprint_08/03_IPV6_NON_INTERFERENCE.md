---
title: IPv6 Non-Interference Gate
version: 1.0.0
status: Approved
---

# Product rule

IPMan release 1.0 does not configure IPv6.

Therefore IPv4 mutation must not silently delete or alter unrelated IPv6
configuration.

# Before each real mutation scenario

Capture IPv6 evidence for the exact interface.

At minimum record, when available:

- enabled/bound state,
- IPv6 unicast addresses,
- prefix lengths,
- IPv6 default gateway/routes relevant to the interface,
- IPv6 DNS servers/settings exposed by the selected read APIs.

Prefer documented Windows APIs.

Do not parse localized command-line text as the production mechanism.

A diagnostic integration helper may use documented PowerShell only if absolutely
necessary for independent test observation, not for IPMan production mutation.
Prefer Windows APIs/WMI/native APIs first.

# After mutation

Re-read the same IPv6 evidence.

Compare before/after.

# Pass rule

The test passes only if all IPv6 changes are explainable by normal transient
Windows behavior and no configured IPv6 state has been lost or rewritten by the
IPv4 mutation path.

If unexpected IPv6 state changes:

- fail the integration gate,
- preserve evidence,
- do not enable production Apply,
- report to architect.

# Important distinction

The application still does not need an IPv6 editor.

This is a non-interference test only.
