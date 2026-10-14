# CORE-C implementation review — 2026-10-14

## Verdict

**PASS — no actionable findings** for the bounded CORE-C candidate at
`114e3a7ec9f794b2996305eb1d640a8a58ab1083`, tree
`2359e06c25dc9f244fe48b653eb5ace9d9930583`.

The candidate is based on current Core canonical
`bb82446c279ef47cf0d18a371f4728b2be4090e3` (exact merge base) and P14
canonical `09fcbe46ffb5a7d80377185692d550310debc110`. Its implementation
source commit is `6355d37f416f88f90eb11ce6c8283c23d8acfb51`, tree
`b5ad86fd6114db106d7223e6e70da3bd5ff2b62c`; the test-input inventory commit
is `b33a302f9b78e8f35ba0ea94bf7168e6ee94f86f`, tree
`170d95b3fa7fc73a069a472f029261e880680415`. The final candidate adds its
validation record and public evidence packages. The reviewed base-to-candidate
diff includes the four P14-C/current-market integration commits and both
CORE-C implementation commits.

## Review basis and scope

Reviewed the complete diff from the Core canonical base, including the Market
price adapter, finite-source Core kernel and DTOs, Unity owner integration,
P14-C mixed-source regressions, CoreValidation inventory/project links,
validation record and evidence package references. Checked against:

- CORE-C reviewed technical design `e80ae27cdf597e9f551fe94dd22827f59636712a`.
- Independent technical design review `cb98d3e99c9491ce2c34278516272763333b271e`.
- Current-baseline applicability review `8fa0408e481f457c153d7552eaed8ed5551cc612`.
- Core canonical `bb82446c279ef47cf0d18a371f4728b2be4090e3` and P14 canonical
  `09fcbe46ffb5a7d80377185692d550310debc110`.

## Findings

- The shared price calculator retains the Unity formula's binary32 operation
  sequence and comparison behavior: minimum-base selection, integer stock
  floor, float division, ordered lower/upper clamps, and final multiplication.
  Shared bit vectors cover ordinary values, signed zero, infinities and NaN;
  Unity baseline and Core tests assert the pinned result bits.
- The detached finite-source request copies its ordered rows and represents
  null rows, null items and item rows distinctly. The kernel preserves order
  and cardinality, finds exactly one target definition, and rejects ambiguous
  or missing targets and stock overflow. The Unity bridge maps every prepared
  row back to the corresponding owner row, preserving unrelated values and
  the target's `ItemData` reference.
- Source and Market identity, revision, day, capacity and installation
  admission are checked before installation. The install path prepares both
  replacements, installs the source and Market owners, then issues the Market
  notification. Stale source/Market revisions, duplicate boundaries, zero
  reserve, and source/Market revision exhaustion retain explicit outcomes.
- P14-C remains on its Unity-owned mixed-source preparation path; it does not
  call `FiniteSourceProductionKernel`. Regression cases cover source ordering,
  capacity rejection, exhausted finite contribution, stale owners/configuration,
  continuation receipts and replay.
- `Tools/CoreValidation` includes the new Core runtime files, shared parity
  vectors and linked Core tests. No `ProjectSettings` paths are changed.
  `git diff --check` passes for the full base-to-candidate diff.

## Validation evidence reviewed

The committed validation record is
[`docs/validation/COREC_FiniteSource/VALIDATION.md`](../validation/COREC_FiniteSource/VALIDATION.md).
Its public package manifests and documented verification results were checked;
test suites were **not rerun** for this independent review.

- CoreValidation private runner run `33efe825d64147029781a08524fefca3`:
  cold/warm 104/104; runner manifest SHA-256
  `34ac78e7cfbf8a362c73c210ee36ab06e64595675420d298ceb8659f530cb503`.
  The record reports public package verification with
  `originalComparisonVerified` against private source context.
- Focused Unity results: Market price 1/1, finite-source kernel 19/19,
  P14-C mixed-source 9/9, economy transaction 45/45, settlement stock
  ownership 14/14, City daily economy continuation 19/19, and runtime
  admission 205/205.
- ALL EditMode 3110/3110; public archive SHA-256
  `d9bd54dd07a86e5d9fb626240bdcf9894feed6b1f835e76734e62d6b3b03ae00`;
  manifest SHA-256
  `03a65535ae248e520832b77d3cf94626f467f199b429df684e56c246ceaef2b0`.
  The record states verification against its protected source run returned
  `VALID`.
- Official filtered Smoke 5/5. Its separate package was rejected because
  the raw XML aggregate suite count disagreed with its descendant cases. The
  record says the raw report was preserved privately and unchanged; the
  complete ALL EditMode report includes the five Smoke cases.

## Boundary

This PASS covers only the exact CORE-C candidate and bounded extraction scope
reviewed above. It does not claim CORE-C promotion, P12 readiness, capture
eligibility, persistence compatibility, or broader product readiness. The
reviewer did not execute Unity or .NET tests. This record is on the isolated
review branch `codex/core/CORE-C-implementation-review-114e3a7`, based on the
exact candidate; no implementation files were changed and no remote write was
attempted.
