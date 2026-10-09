# P12-E Justice Records Owner Snapshot Design Review

**Result:** PASS — independent exact-content technical design review.
**Reviewed design candidate:** `codex/phase12/P12EJusticeOwnerSnapshotDesign` at `fb16e4f`.
**P12 canonical base:** `565f6b5817e0126c2f89e0a1452b09a8fb03cc19`.
**Architecture baseline:** `codex/architecture/world-identity-projection` at `47eff220c7ce00f6e7c759bd2c76780bb46f628`.
**Review date:** 2026-10-09.
**Reviewer:** independent read-only review agent.

## Review

The bounded owner design is compatible with the accepted P12-E owner map and
the current P12-B Justice census. It captures the ordered wanted-record and
prison-sentence rows, their exact local revision, and the existing exact-zero
Justice P18 receipt admission witness. The sentence-to-warrant ordinal is
explicitly transport-only; hydration preserves the exact object relationship
without introducing domain identity or replaying mutations.

The review checked the architecture §91 continuation identity rule. Each
Justice target carries `NpcRuntimeId` for exact staged-runtime resolution and
nullable `PersonId` for stable identity when the target is Person-backed.
Legacy NPC-only targets retain null; capture and staging do not mint or infer
PersonIds. Staging requires a non-null PersonId to resolve to the exact staged
Person whose materialized NPC RuntimeId matches, checks the reverse
PersonStore lookup returns that same Person object, and checks the P12-D D/F
NPC root carries the same PersonId. Null requires an NPC-only root with no
reciprocal PersonStore materialization binding. These rules align with the
P12-D root contract and reject dangling, mismatched, or one-sided bindings.

The updated test plan covers both Person-backed and legacy targets, rejects
identity mismatches, and retains the proposed ordering, cardinality/revision,
token/vector, stale-witness, exact-zero receipt, and no-side-effect cases. The
owner boundary and exclusions remain within accepted P12-E scope. No
architecture or product decision remains for this slice.

`git diff --check` passes for the design candidate. This is a design review
only; Unity tests were not run. No implementation or canonical promotion is
included in this review record.

## Readiness

Verdict: `VALIDATED_CANDIDATE` for the bounded Justice owner snapshot and
private staged-hydration implementation slice. The accepted P12-E prerequisite
authorization applies. Implementation must remain isolated to the reviewed
Justice owner adapter and tests, follow the recorded capture/staging contract,
and obtain a separate exact-tip implementation review and validation. This
does not complete P12-E, establish P12-A readiness, unblock P13, or close
Phase 12.
