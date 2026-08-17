---
title: Integration Safety Policy
version: 1.0.0
status: Approved
---

# Non-negotiable safety requirements

The destructive integration harness must refuse to run unless ALL required
opt-ins are present.

At minimum require:

1. explicit destructive-test enable flag;
2. exact `NetworkAdapterId` / interface GUID;
3. explicit acknowledgement that the adapter is disposable/isolated;
4. test configuration values supplied by the operator;
5. successful pre-test state capture;
6. successful recovery/rollback-state capture.

# Suggested opt-in shape

Use environment variables, command-line options, test settings or an equivalent
explicit mechanism.

Example intent only:

- `IPMAN_DESTRUCTIVE_NETWORK_TESTS=1`
- `IPMAN_TEST_ADAPTER_ID={GUID}`
- `IPMAN_TEST_ADAPTER_IS_ISOLATED=YES`

Exact names are implementation choice.

Do not use a single weak flag if target identity is missing.

# Refusal behavior

If opt-in data is absent, malformed or ambiguous:

- skip/refuse the destructive integration test,
- explain what requirement is missing,
- do not mutate anything.

# Target verification

Before mutation:

- read the exact adapter by identity,
- resolve the exact WMI object by SettingID,
- ensure both layers refer to the same adapter,
- capture name/description/MAC only for human evidence,
- never use those display properties as identity.

# Connectivity warning

The harness must warn that mutation can disconnect the machine.

Do not run against:
- the only remote-management path,
- the developer's active internet adapter,
- a production network interface.

The harness cannot technically prove all of those conditions; explicit operator
acknowledgement is required.
