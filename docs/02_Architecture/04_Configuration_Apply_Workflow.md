---
title: Configuration Apply Workflow
version: 1.0.0
status: Approved
---

# Static apply pipeline

The application layer orchestrates the following logical pipeline:

1. **Resolve selected adapter**
   - Capture `NetworkAdapterId`.
   - Do not use tab index as adapter identity.

2. **Fresh-read current state**
   - Prevent stale edit/session data from being treated as current.

3. **Validate desired configuration**
   - IPv4.
   - subnet mask.
   - optional gateway.
   - optional DNS.

4. **Normalize**
   - canonical IPv4 string forms,
   - normalized empty optional values.

5. **Compare**
   - if equivalent, return `NoChange`.

6. **Capture rollback snapshot**
   - save exact relevant current configuration before mutation.

7. **Conflict probe**
   - best-effort check only.
   - failure to receive ping does not prove IP is free.

8. **User warning if suspected conflict**
   - the UI decides whether to continue based on the result.

9. **Mutation**
   - execute on background thread/infrastructure boundary because WMI calls can
     block.

10. **Reconciliation**
    - allow Windows a short bounded interval to update configuration.

11. **Fresh read**
    - read selected adapter again.

12. **Verification**
    - compare actual state with desired state.

13. **Result**
    - verified success,
    - verified failure,
    - indeterminate/adapter disappeared.

14. **UI refresh**
    - update tab/current panel/edit state as specified by UX.

# DHCP pipeline

Same safety model:
- fresh read,
- no-op detection where possible,
- rollback capture,
- DHCP + automatic DNS mutation,
- route cleanup only if required,
- bounded reconciliation,
- fresh read,
- verification.

# Failure policy

Never report success merely because the WMI method returned success.

Technical return values are retained for diagnostics while user messages remain
friendly and localized.
