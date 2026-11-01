# Independent review — P12 Core owner-stage composition

## Verdict

**VALIDATED_CANDIDATE** for the bounded restore-stage composition change.
This is not a Core canonical promotion or a full CORE-F acceptance verdict.

## Exact source identity

- Branch: `codex/core/CORE-F-core-owned-runtime-application`
- Candidate: `bc8d024ce35d1cf8deb4ea455fc40b74692fd354`
- Tree: `e97e87376afece57af5496eab9b6da56633d7098`
- Base: `9bb0fd65d89f49995b08dddc5c28d96bf19607ec`
- Current Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

## Review findings and resolution

The first exact-tip review identified one compatibility blocker: detached
restore composition did not verify that the supplied effective configuration
was the exact object admitted with the selected definitions. The follow-up
passes that configuration from P12-G, rejects any reference mismatch before
examining owner stages, and forwards the verified object to the existing
composer. An additive Core test verifies early rejection with no stage values.

The independent follow-up review of `bc8d024` passed. It also confirmed that:

- Detached-image restoration composes from the exact retained Core D/E/F
  stage owners and the staged C roots.
- The live capture-bound path retains its separate existing composition
  contract while it owns an in-flight staging attempt.
- No roster-cardinality assumption was added, and failed composition does
  not publish a session.
- The source change preserves exact configuration and Core owner identities;
  no new actionable finding remains.

The reviewer inspected the implementation and the official validation
manifests listed in [VALIDATION.md](VALIDATION.md). The reviewer did not rerun
tests. No assertion was weakened.

## Integration limits

This checkpoint establishes a Core-owned composition boundary for detached
image restores only. `DailyV1Application.TryStart`, full P12 capture and
restore ownership, Unity forwarding, the fixed detached .NET host, separate-
process T/T+1/T+2 parity, and complete CORE-F acceptance remain outstanding.
No Core canonical promotion is authorized or claimed by this record.
