# IPMan Project State

- Inherited P01 baseline:
  `e30ccd5e64d804316a03d8e65c7cb7cb8746ad40`
- Accepted P02R production HEAD:
  `39916fd09ccaf7f3f908f815ebfe24c396ba9331`.
- Sprint 08 / P02R status: `VALIDATED / CLOSED`.
- Official Sprint 08 gate: `INTEGRATION_VALIDATED`.
- P02R HOST controlled validation: `ACCEPTED / PASS`.
- Validated HOST adapter GUID:
  `727139AD-AEF5-434A-B53B-E1B877B19F90`.
- Validated operations: read-only discovery, DNS truth diagnostic, DNS controlled
  mutation plus rollback, and IPv4 controlled mutation plus rollback.
- Final HOST baseline restored: IPv4 `10.250.0.1/24`, IPv4 DNS empty, gateway
  empty, and no target IPv4/IPv6 default route.
- No substantive non-target drift was observed.
- VM final cross-validation: `ACCEPTED / PASS`.
- Accepted VM evidence ZIP SHA256:
  `7ad61098050b0a2c5f82400014d69c4c040e0282a35bcb81256d248269b59d3e`.
- Richer DNS/DoH: `NOT EXECUTED` because no testable state was available;
  non-blocking for Sprint 08 and not a PASS claim.
- Historical optional-field FINAL verifier failure is retained as provenance and
  superseded by the later hotfixed FINAL PASS.
- Known unresolved P02R integration blockers: none.
- Production Apply UI remains `CLOSED`.
- Sprint 09 remains `CLOSED`.
- Mandatory Hyper-V PowerShell discovery is superseded.
- P02/P02R historical artifacts are not active instructions by filename alone.
- Gate changes may be made only through an architect-approved task.
