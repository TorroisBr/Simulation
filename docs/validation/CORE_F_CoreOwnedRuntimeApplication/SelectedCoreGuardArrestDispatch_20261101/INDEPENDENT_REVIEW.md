# Independent review — selected Core Guard Arrest dispatch

**Verdict: VALIDATED_CANDIDATE**

## Reviewed identity

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Exact candidate tip: `5dc06abb0ba682da59d027dc7fd02dcdaf101a2f`
- Candidate tree: `e5c1c84e9449a52d232b8e993ff79b516a60964b`
- Implementation commit: `be6fabda651e2497a6ff07f2213fef800c8fdff7`
- Implementation tree: `f32a07836665eea948649d178938a01858203bb6`
- Implementation base: `fb38325cde4c25f5fab2e758f336fbff109316e6`
- Current fetched remote candidate matched the exact candidate tip at review.

## Review result

The implementation diff is limited to `SimulationRuntime.cs` and its selected-action admission regression. For a selected Daily-v1 Guard Arrest slot without a Unity action projection, the adapter requires the exact installed Core payload, resolves the target through the admitted owner graph, and verifies the exact graph-owner/runtime mapping and unique target projection. It reconstructs only the compatibility `NpcActionRuntime` projection and installs it without a P12 mutation-boundary notification. Core remains responsible for the Arrest decision and authoritative execution.

The test verifies missing-target rejection before dispatch with unchanged owner revisions and P12 epoch, projection reconstruction without owner mutation, successful Core Arrest execution with exact target and deterministic draw, Justice/target owner changes, P12 epoch advancement, and census readability after closing the daily operation scope. Review found no production defect and no weakened assertion.

The first review identified one inaccurate validation sentence that implied a duplicate-projection regression existed. The validation record was corrected to state that the test covers missing-target rejection while duplicate runtime projections are rejected by implementation but lack a separate assertion. Re-review confirmed that correction. This documentation-only edit did not change implementation/test source or validation evidence.

## Evidence applicability

The exact implementation/test source hashes match the committed files and validation manifest. The verified privacy-safe package contains the full EditMode result, and the package archive and normalized report hashes match the manifest. Full EditMode passed 3288/3288; focused Guard Arrest passed 1/1; official Smoke passed 5/5. The filtered Smoke report remains private because the approved packager rejects its stale aggregate count; its cases are present as passed in the packaged full EditMode report. No tests were rerun after the documentation-only wording correction.

See [`VALIDATION.md`](VALIDATION.md) and `coref-guard-arrest-20261101.manifest.json` for validation identities and hashes.

## Scope and status

This is a bounded selected-action adapter integration. It does not complete CORE-F, move the authoritative runtime to Core, or establish DailyV1Application/.NET host completion. No Core-canonical promotion is implied.

**Actionable findings:** none.