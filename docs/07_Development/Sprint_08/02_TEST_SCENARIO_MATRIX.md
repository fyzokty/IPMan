---
title: Isolated Mutation Scenario Matrix
version: 1.0.0
status: Approved
---

# Core scenarios

Run scenarios only on a disposable VM or dedicated isolated test adapter.

Each scenario must capture:
- before state,
- mutation request,
- low-level WMI result,
- post-write actual state,
- rollback snapshot reference,
- verification result,
- after state.

# Scenario S08-01 — Static to different static

Starting state:
- one static IPv4,
- zero or one gateway,
- zero to two DNS servers,
- no topology safety block.

Apply a different valid static configuration.

Verify:
- IP/mask changed,
- gateway matches request,
- DNS matches request,
- product result is VerifiedSuccess,
- rollback snapshot represents the immediately prior state.

This scenario is expected to exercise documented EnableStatic return 81 on
systems/providers that exhibit it.

Record the actual code observed; do not require every machine to return 81.

# Scenario S08-02 — DHCP to static

Starting state:
- DHCP IPv4.

Apply valid static configuration.

Verify:
- mode becomes Static,
- actual values match,
- rollback captures DHCP mode and recovery-relevant DNS state.

# Scenario S08-03 — Set gateway

Starting state:
- no IPv4 default gateway.

Apply static configuration with a gateway.

Verify exact gateway and metric behavior.

# Scenario S08-04 — Clear gateway

Starting state:
- one IPv4 default gateway.

Apply a static configuration with no gateway.

Verify Windows actual state contains no IPv4 default gateway after settling.

Record the WMI return code.

# Scenario S08-05 — Manual DNS

Apply:
- one DNS server,
- then two DNS servers.

Verify:
- order,
- actual IPv4 DNS list,
- detected DNS configuration mode.

# Scenario S08-06 — Automatic/empty DNS

Starting state:
- manual DNS.

Apply configuration with both DNS fields empty.

Verify:
- manual DNS search order is removed,
- detected DNS mode becomes automatic/appropriate Windows automatic semantics,
- actual DNS state is recorded after network settles.

# Scenario S08-07 — Safety blocks

With fake/unit-level or safely prepared isolated state, validate no mutation when:
- multiple IPv4 addresses,
- multiple gateways,
- more than two IPv4 DNS servers.

Real destructive preparation of these states is optional if it adds unnecessary
risk; deterministic unit coverage remains acceptable for the block itself.

# Scenario S08-08 — Verification failure

Use the integration seam/fake where necessary to prove WMI return success alone
cannot produce product-level success.

Do not deliberately corrupt a live adapter merely to force this condition.
