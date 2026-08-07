---
title: Network Adapter Architecture
version: 1.0.0
status: Approved
---

# 1. Adapter identity

The adapter identity used by IPMan must be stable for the lifetime of the
Windows adapter and must not rely on display name alone.

Infrastructure maps Windows adapter identifiers into `NetworkAdapterId`.

Display name/description are metadata, not identity.

# 2. Reading current state

Primary managed read API:

`System.Net.NetworkInformation.NetworkInterface`

Read:
- adapter ID,
- name,
- description,
- operational state,
- MAC,
- speed,
- IPv4 unicast addresses,
- gateways,
- DNS servers,
- DHCP-related IPv4 properties where exposed.

If a field cannot be read reliably from NetworkInterface, Infrastructure may
augment it through WMI/native Windows APIs.

# 3. Configuration mutation

Persistent IPv4 mutation baseline:

`Win32_NetworkAdapterConfiguration` through `System.Management`.

Methods expected:
- `EnableStatic`
- `SetGateways`
- `SetDNSServerSearchOrder`
- `EnableDHCP`

All WMI return codes are mapped to an internal typed result. ViewModels must not
know WMI numeric codes.

# 4. DHCP caveat

Windows WMI documentation states that `EnableDHCP` does not itself clear every
static default gateway.

Therefore DHCP transition is not considered complete until post-operation state
is read and checked.

If stale static routes remain, Infrastructure may use the Windows IP Helper /
NetIO route API behind a dedicated internal route-management component.

No route cleanup should delete unrelated routes.

# 5. Multiple IPv4 addresses

Windows adapters can contain multiple IPv4 addresses.

Release 1.0 UI edits one primary configuration, but Infrastructure must not
blindly delete additional addresses.

Before implementing mutation, a dedicated adapter-state mapping test/spike shall
verify behavior on:
- one IPv4 address,
- multiple IPv4 addresses,
- APIPA address,
- DHCP lease plus additional address.

# 6. DNS

Primary and secondary DNS are user-editable.

Infrastructure must preserve user intent:
- blank DNS does not mean "invent a public DNS",
- DHCP action returns DNS to automatic behavior,
- static DNS uses supplied server order.

# 7. Gateway

Gateway is optional for static configuration.

No default gateway should be fabricated when the user leaves it empty.

# 8. Privilege

Mutation functions execute only in the elevated application process.

No credentials are handled by application code.
