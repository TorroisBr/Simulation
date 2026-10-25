# CORE-F P12-F Core owner snapshot migration — independent exact-tip review

**Verdict: PASS / VALIDATED_CANDIDATE.** No actionable findings.

- Candidate branch: `codex/core/CORE-F-p12d-core-hydration`.
- Candidate commit: `1a036e88ce107ea88ab7d4fa4c1f6622a07a717f`.
- Candidate tree: `af5e6575185058c72d4113f12a2523c067e71bf9`.
- Review base: `1706c17139c270ed60b9d34776b45f8e45ecbdc6` (tree `ff9108d9d508f560e654d24762b13aef43d24223`).
- Core canonical inspected: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`, an ancestor of the candidate.
- Review scope: complete 34-file candidate diff, implementation and test sources, current validation record, source-input inventories, and normalized Unity package.

## Findings

The candidate preserves the same capture token/vector identity checks and exact owner-instance, schema, cardinality, and local-revision validation. TravelParty and Expedition cross-owner relationship checks remain in Unity staging adapters where their selected NPC/spatial authorities reside. ScheduledDirective restoration resolves the stable action definition ID through the exact admitted `DailyV1EffectiveDefinitions` graph and requires the EscapePrison action type. Deterministic row order, terminal disposition, revisions, existing P12 wire fields/contracts, and historical provider/codec GUIDs are retained. Test changes update moved-type call sites and add Core coverage; existing assertions were not weakened.

## Independently checked evidence

- CoreValidation: 756/756 cold and 756/756 warm; private input hashes match 380/380 files.
- Unity EditMode: 3,263/3,263 passed; source inventory hashes match 1,884/1,884 files.
- Official Smoke: all five identities appear exactly once as passed in both the full report and filtered report.
- The normalized Unity archive and manifest hashes match the validation record; the archive contains all 3,263 normalized results and its privacy/path scan is clean.
- `git diff --check`: PASS.
- Worktree sources are clean relative to the candidate. Pre-existing protected ProjectSettings edits and local recovery/evidence directories remain outside the candidate commit.

The private .NET TRX originals and exact-input run manifest were inspected locally and remain protected. They are not packaged as a public .NET evidence artifact; this limitation is accurately recorded in `VALIDATION.md` and is not treated as resolved by this review.

## Disposition

This is an exact-tip review of a bounded candidate checkpoint. It does not promote Core canonical, complete CORE-F, or change P12/P13 status. Canonical promotion remains a separate decision/workflow after its own required evidence and readiness gates.
