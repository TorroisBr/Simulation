# P12-B P8-D passive route-owner census review

**Verdict:** `PASS` — validated candidate; no actionable findings.

**Independent exact-tip review date:** 2026-09-29.

**Reviewed candidate:** `codex/phase12/P12BP8DZeroWitness` at
`d92fdfb6b5ceb517c210be7cea5faab52ebb5641`.

**Code-bearing commit:** `f0575ef43a77898aae8fb8565d4b709b850a46d8`.

**Actual base and current canonical at review:**
`c7ff9fd9f4245c3f8968b9196a4c20de19dd0fb2`.

**Technical design:** `9dd1e54dc0f600c641831a57791f2a02904a18c1`.

## Review findings

The providers retain the installed runtime-store references and return the
designed section ID, schema version, owner identity, cardinality, and local
revision. Plan cardinality correctly uses `PlanCount` for all retained history
rows rather than `Plans.Count`, which contains only each actor's latest plan.
Tests cover new observations, duplicate replay, conflicting/invalid input,
accepted and stale plan records, and P8-E status transitions that advance the
plan revision without adding a retained history row.

The candidate remains passive. It does not register sections in the incomplete
census, connect writes to the shared mutation epoch, add thread/quiescence
enforcement, grant capture eligibility, or introduce serialization or
hydration. Its current owner reads are unsynchronized and do not claim an
atomic snapshot. No P12-B completion or P12-A readiness claim is justified.

The independent reviewer confirmed these result files exist and report the
stated passing counts:

- Route planning: 21/21 —
  `Library/ValidationResults/P12BP8D/EditMode-20260929-210438-0f774368f53445d9bd790f795afa5803.xml`.
- Selected-profile composition: 14/14 —
  `Library/ValidationResults/P12BP8D/EditMode-20260929-210633-22f7685d17814e16be07403f8784b8b7.xml`.
- ALL EditMode: 1955/1955 —
  `Library/ValidationResults/P12BP8D/EditMode-20260929-210654-1e491621c60e40e5a4535ed67a9586cd.xml`.
- Official complete Smoke filter: 5/5 —
  `Library/ValidationResults/P12BP8D/EditMode-20260929-210739-e7fd58276d3d40f98a820a5e526c2bc6.xml`.
- `git diff --check`: clean.

The reviewed tip adds only the candidate evidence document beyond the
code-bearing commit; validation applies to the unchanged code tree.

## Promotion boundary

This review validates the P8-D passive witness candidate only. It does not
promote the candidate, complete P12-B, make P12-A ready, or close Phase 12.
Canonical promotion remains a separate human gate.
