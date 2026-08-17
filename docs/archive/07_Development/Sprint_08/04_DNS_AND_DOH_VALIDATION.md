---
title: DNS and DNS-over-HTTPS Validation
version: 1.0.0
status: Approved
---

# Why this gate exists

Windows 10/11 exposes per-interface DNS settings through the documented
GetInterfaceDnsSettings API.

Newer DNS settings structures can also carry richer per-server properties such
as encrypted DNS/DoH-related configuration.

Sprint 07 uses DNS state to make rollback capture restore-capable.

Sprint 08 must verify that the static DNS mutation path does not silently damage
DNS settings that IPMan release 1.0 does not yet model.

# Microsoft API baseline

Use Microsoft Learn as the primary source for:

- `GetInterfaceDnsSettings`
- `DNS_INTERFACE_SETTINGS`
- `DNS_INTERFACE_SETTINGS_EX`
- `DNS_INTERFACE_SETTINGS3`
- related server property structures/flags where available
- `FreeInterfaceDnsSettings`

The documented API supports selecting the settings structure by Version.

# Before real DNS mutation

Capture, when supported by the OS:

- DNS source mode used by the rollback reader,
- IPv4 manual name-server string/list,
- profile name-server state,
- DNS settings flags,
- server properties relevant to encrypted DNS / DoH,
- any other per-interface DNS property that mutation could unintentionally
  destroy.

# After mutation

Capture the same state again.

# Decision rules

## User explicitly changes DNS

It is acceptable for the requested IPv4 DNS server list/source semantics to
change.

It is NOT acceptable to silently destroy unrelated DNS properties that the
product did not represent, unless Windows documentation proves the operation
necessarily resets them and the architect explicitly accepts that behavior.

## User changes only IP/mask/gateway

Existing DNS mode and richer DNS properties must remain unchanged.

If the current Sprint 07 mutation always calls DNS even when desired DNS is
unchanged, review that behavior. Avoid unnecessary native writes.

# DoH gate

If the isolated adapter has testable DoH/encrypted DNS configuration:

- capture before,
- perform the relevant mutation,
- capture after,
- verify no silent unrelated loss.

If no isolated environment with DoH can be prepared, report:
`DoH integration case not executed`

Do not convert "not executed" into "passed".
