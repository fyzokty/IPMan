---
title: ADR-009 - NetworkInterface for Primary State Reading
status: Accepted
date: 2026-08-07
---

# Decision

Use `System.Net.NetworkInformation.NetworkInterface` as the primary managed API
for frequent adapter/state reads.

Use WMI/native augmentation only for data not reliably exposed by the managed
network-information API.

# Reason

Reading is frequent and should avoid heavyweight mutation-oriented WMI calls
where possible.
