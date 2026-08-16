# IPMan Project State

- Inherited P01 baseline:
  `e30ccd5e64d804316a03d8e65c7cb7cb8746ad40`
- Accepted P02R implementation baseline:
  `1ddc918b752137cbcaa56b529de6133c4f7587a7`
- P02R HOST controlled validation: `PASS`.
- Validated HOST adapter GUID:
  `727139AD-AEF5-434A-B53B-E1B877B19F90`.
- Validated operations: read-only discovery, DNS truth diagnostic, DNS controlled
  mutation plus rollback, and IPv4 controlled mutation plus rollback.
- Final HOST baseline restored: IPv4 `10.250.0.1/24`, IPv4 DNS empty, gateway
  empty, and no target IPv4/IPv6 default route.
- No substantive non-target drift was observed.
- VM final cross-validation: `PENDING / OPERATOR-OWNED`.
- Current Sprint 08 gate remains `HARNESS_READY_REAL_RUN_PENDING` until the
  architect approves the VM/final validation requirement or explicitly changes
  the gate.
- Production Apply UI remains `CLOSED`.
- Sprint 09 remains `CLOSED`.
- Mandatory Hyper-V PowerShell discovery is superseded.
- P02/P02R historical artifacts are not active instructions by filename alone.
- Gate changes may be made only through an architect-approved task.
