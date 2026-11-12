# CORE-F Selected NPC Core Membership Transaction Review

**Verdict: `VALIDATED_CANDIDATE`**

## Identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Exact candidate tip: `2ca23aab32bc71e40097f6e10b38579f0eb3fb44`
- Implementation commit: `caee8293945384b31c72167a98f90ae414351be6`
- Implementation tree: `fe905a7a614671fe7c9883719ac45d9bf66666e6`
- Candidate base: `33491d0ab0a312f8b3b84f82de3b28780b9f172f`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Independent reviewer: `/root/review_npc_materialization`
- Review result: PASS; no actionable findings.

## Reviewed behavior

The selected NPC membership change now prepares the exact replacement `DailyV1SelectedRuntimeOwnerGraph` and actor roster in Core, then publishes both through the execution-state guarded replacement. Rollback compares against the published graph/roster and restores the original pair, rebuilding execution roots through the existing state replacement path. This prevents a graph-only or roster-only partial commit.

The Unity host continues to own Unity-specific provider/guard binding, host registries and projections, runtime identity publication, and the enclosing P12 `NpcMembershipCensusScope`. Those host operations compensate through the Core transaction when a later step fails. The identity census remains after unregister as required by the existing operation contract. The mixed-roster compatibility path is unchanged.

The selected-profile integration test exercises add and remove through the real Unity host path and checks exact Core graph/roster membership, host lookup, dynamic owner-family census, retained identity cardinality/revision, and one shared epoch advance per successful operation. The Core test checks prepare-without-publication, publish, rollback and removal. Existing tests were not weakened.

## Changed implementation/test files

The implementation commit changes exactly these four files:

- `Packages/com.simulation.core/Runtime/Application/DailyV1ExecutionBoundaryState.cs`
- `tests/Simulation.Core.Tests/DailyV1ActorTurnExecutionStageTests.cs`
- `Assets/_Project/Scripts/SimulationRuntime.cs`
- `Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs`

The validation record contains matching SHA-256 values for the four source/test files; the independent reviewer recomputed and confirmed those values against the exact candidate. The reviewed implementation tree is `fe905a7a614671fe7c9883719ac45d9bf66666e6`.

## Validation evidence

| Gate | Result | Identity |
| --- | --- | --- |
| CoreValidation cold | 1,101/1,101 PASS | run `16402a9b9eb94b31bbaf6448c2978a6e` |
| CoreValidation warm | 1,101/1,101 PASS | run `16402a9b9eb94b31bbaf6448c2978a6e` |
| Focused selected membership EditMode | 1/1 PASS | run `0322f00989834654b4f51cc53f560be4` |
| Full Unity EditMode | 3,318/3,318 PASS | run `9267c47b4f954c38a70100ce3a7f7282` |
| Official Smoke | 5/5 PASS | run `9612e681e5164df59308f9146741d8f6` |
| `git diff --check` | PASS | implementation preflight |

Unity full-suite input digest: `e51af85783aaa00b5b1ef40b095e9b9fb819b6c22166413fe19cfa020ba2f346`. The original full EditMode XML SHA-256 is `7e53f966c95bb76ced9fcb47c5184c5fe14ac4f9a134e403662d3d54cf3d61f3`. The privacy-safe Unity package is `docs/validation/CORE_F_SelectedNpcCoreMembershipTransaction_20261112/Unity/coref-selected-npc-membership-editmode.zip` (SHA-256 `06d3e7876635587926dd975cd4357cafb7bcc822bbf4607ae3dfc33e9cabb`); its package manifest SHA-256 is `171b314a923018297c05d2d37c0ab6c5938cd2d9c3b4d34a45adb70e612fbffd`. The independent reviewer verified the package, manifest, normalized XML hash, run records, zero failures, and that the new integration test occurs exactly once as Passed. No absolute Windows paths or user/private-path strings were detected in published artifacts or this record.

The CoreValidation raw TRX remains in protected private evidence and was not publicly repackaged: its runner schema does not satisfy the separate public TRX packager's required source-context schema. That limitation is recorded and was not bypassed or reinterpreted. The independent reviewer did not rerun tests because source and test identities were unchanged.

## Scope and limitations

This validates only the selected NPC owner-graph/actor-roster membership publication and rollback slice. It does not establish full Core-owned runtime composition, `DailyV1Application.TryStart`, full 89-family P12 capture/restoration, detached .NET hosting, process-separated continuation, or complete CORE-F acceptance. The host-owned registration envelope and P12 census scope remain in Unity. Core canonical was not advanced by this review record.