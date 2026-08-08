# Sprint 08 destructive integration harness

The project is safe by default. Its destructive test is reported as
`NOT EXECUTED` / skipped before adapter discovery or mutation unless every gate
below is supplied exactly. Run only on a disposable VM or dedicated isolated
adapter, with local/VM-console access.

Required environment variables:

```text
IPMAN_DESTRUCTIVE_NETWORK_TESTS=1
IPMAN_TEST_ADAPTER_ID={EXACT-INTERFACE-GUID}
IPMAN_TEST_ADAPTER_IS_ISOLATED=YES_DISPOSABLE_ISOLATED_ADAPTER
IPMAN_DESTRUCTIVE_FILTER_ACK=DESTRUCTIVE_NETWORK_FILTER_APPLIED
IPMAN_TEST_CONFLICT_RISK_ACK=YES_ACCEPT_IP_CONFLICT_RISK
IPMAN_TEST_SCENARIO=<one scenario name>
IPMAN_TEST_IPV4=<IPv4 address>
IPMAN_TEST_SUBNET_MASK=<IPv4 subnet mask>
IPMAN_TEST_GATEWAY=<IPv4 gateway or NONE>
IPMAN_TEST_DNS=<NONE, one IPv4, or two comma-separated IPv4 values>
```

Scenario names are `StaticToStatic`, `DhcpToStatic`, `SetGateway`,
`ClearGateway`, `ManualDnsOne`, `ManualDnsTwo`, and `AutomaticDns`. Scenario
shape and starting-state checks are enforced before mutation.
Focused gateway and DNS scenarios also reject requests that would alter an
unrelated IPv4, gateway, DNS-source, or gateway-metric dimension.

Run exactly one scenario with the destructive category filter:

```powershell
dotnet test tests/IPMan.IntegrationTests/IPMan.IntegrationTests.csproj `
  --filter "Category=DestructiveNetwork"
```

Evidence is written by default under the ignored directory
`artifacts/integration/sprint08/<UTC timestamp>-<run ID>/`. Raw evidence can
contain adapter-specific data and must stay local. Share only
`summary.sanitized.json` unless the raw files have been reviewed and redacted.
The evidence includes exact-interface IPv6 route-table state. Unsupported richer
DNS property payloads fail the non-interference gate closed, and the sanitized
summary contains only stable failure metadata rather than raw exception text.

The harness does not restore the adapter automatically. After a scenario,
restore the known-good state manually or revert the VM checkpoint before
running another scenario.
