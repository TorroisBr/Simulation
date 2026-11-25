# CORE-F Core-owned P12 continuation persistence — independent review

**Verdict: PASS / VALIDATED CANDIDATE.** No actionable findings remain in this bounded source delta.

- Candidate commit/tree: `0fa5e8970b67abae16061ad0fc69a78d15602f69` / `1926ec004537f8e53228665dbb1b28243cbcfaec`.
- Review base: `422d54e016bbd323c402a5f2c7ea4845fa9ab260`.
- Core canonical inspected: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Reviewed by: independent reviewer `core_runtime_boundary_gate_review`, read-only exact-tip pass.
- Complete code delta: `Assets/_Project/Scripts/TesteSimulacao.cs`, `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs`, and `Packages/com.simulation.core/Runtime/Persistence/DailyV1P12GContinuationSourceWitness.cs`.

## Review findings

The save operation remains active across the Unity capture/staging path. Before encoding, Core verifies that the operation is still active, belongs to this exact application, and refers to its exact published session and owner thread. Core revalidates the source witness before census/encoding and again after traversing the owner image immediately before storage. It preserves the existing P12 census, codec, result categories and atomic v1/.NET-v2 store implementations. The Unity Core-bound path delegates to this Core boundary; the legacy non-Core route retains its previous behavior.

The exact candidate passed CoreValidation, the persisted save/restore/continuation test, the operation-invalidation regressions, full EditMode and official Smoke. The reviewer verified the private run manifests, stable source snapshots and exact changed-source hashes. The full EditMode normalized package verifies successfully.

## Scope boundary

This is a genuine bounded Core persistence boundary, not the complete shared save transaction. P12-G image capture, detached staging, source/target graph validation and restored-boundary admission are still performed by the Unity-side coordinator. The candidate does not claim final `TrySaveContinuation`, `TryStart`, detached hosting, or CORE-F completion.

The two local `ProjectSettings` edits were not modified or included in the reviewed candidate.