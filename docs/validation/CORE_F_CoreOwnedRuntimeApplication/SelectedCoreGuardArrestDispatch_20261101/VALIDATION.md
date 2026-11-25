# Selected Core Guard Arrest Dispatch — Validation

## Candidate scope

Candidate implementation: `be6fabda651e2497a6ff07f2213fef800c8fdff7` (tree `f32a07836665eea948649d178938a01858203bb6`), based directly on `fb38325cde4c25f5fab2e758f336fbff109316e6`.

This bounded adapter change dispatches the already selected Core Guard Arrest slot when the Unity `NpcActionRuntime` projection is absent. It reconstructs only the exact compatibility projection from the Core payload and an identity-checked selected target. Core continues to own eligibility, action selection, deterministic resolution, authoritative Justice/target mutations, and P12 operation commits. The change does not alter Core action semantics, add a supported action, or claim complete Core runtime ownership.

Changed implementation files:

- `Assets/_Project/Scripts/SimulationRuntime.cs`
- `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs`

The focused test source SHA-256 is `3c52330c2e170a724e7b2983c6e63e94e75ce6a436d1ac71071ca15c1dfa7c49`. The runtime source SHA-256 is `ff1bb4d6b055db7177f2a731bd6be1a5f1b063921fb499ec40fb517c78f2611b`.

## Behavior covered

The new regression verifies that:

- The adapter rebuilds the projection only for the exact installed Guard Arrest payload and selected target identity.
- A missing target projection fails before dispatch, with owner revisions and the P12 mutation epoch unchanged. The implementation also rejects ambiguous duplicate runtime projections, but this test does not add a separate duplicate-projection assertion.
- Projection reconstruction itself does not mutate the Core action owner or advance the P12 epoch.
- The normal selected action stage executes Arrest through Core, preserves the exact target and deterministic draw, writes the expected Justice and target owners, and advances the P12 epoch.
- The resulting census is readable after the daily operation scope closes.

No P12 admission contract, census family, identity rule, writer, or Core action behavior was broadened.

## Validation

Unity Editor: `6000.3.9f1`, official Unity validation harness.

- Focused `SelectedDailyV1GuardArrestDispatchRebuildsProjectionFromExactCoreSlot`: **1/1 passed**. Run `60f0762d1d8c46b4b49dc5e0d157e806`; original report SHA-256 `4a48437078a81c09e83ef9fc44e145ef89611947a739d0c98f5bd96ec3c18fc3`.
- Full EditMode: **3288/3288 passed**, zero failed, skipped, or inconclusive. Run `4d994296022c4314b70080276b4bee29`; original XML SHA-256 `875258d4c2ec686ab1efbbea9c8bd7b22d102210431d86352105342759f1f514`; normalized report SHA-256 `96a0883fbb58dc0875e6e546ac2087226ace6a9094b6fe1e22ac1294b76397b0`.
- Official Smoke selection: **5/5 passed**. Run `9497a8760047408d9fa6290f0803ded1`; original XML SHA-256 `67dce7d8415dc18614c0bfb448aa0c28de0e53ec911b69b7e67802a783d434a3`. The selected Smoke report is retained privately and was not rewritten: Unity emits a stale full-suite aggregate count in filtered reports, so the approved packager rejects that filtered XML. All five named Smoke cases are also recorded as passed in the packaged full EditMode report.
- `git diff --check`: **passed** for the implementation candidate.

An earlier focused attempt (`6dd9e43fa3f24787954e7c058ae5b5c4`) reached and completed the Core Arrest action but placed the final census read before disposing the enclosing daily P12 operation scope. The assertion correctly received `OperationInProgress`. The test was corrected to close the scope before reading the census; the subsequent focused and full runs passed. This was a fixture sequencing correction, not a production behavior change.

## Public evidence package

The approved Unity evidence packager produced and verified one privacy-safe full EditMode package. Filtered reports were not altered or manually normalized.

- Manifest: `coref-guard-arrest-20261101.manifest.json`
- Archive: `coref-guard-arrest-20261101.zip`
- Archive SHA-256: `68183c13e814ef15d9abc423613b7cbbf6fe03557975e3a698539455b544a442`
- Manifest SHA-256: `5F7CC2B8EFA12B6DFD68E8C05252B3EA481960934FE6CC15C6AEA771E176515B`
- Packager verification: `VALID: runs=1 entries=1` with the archive hash above.
- The full-run input snapshot explicitly binds the Unity project file inventory and hashes. Its captured repository identity is the candidate's parent (`fb38325cde4c25f5fab2e758f336fbff109316e6`, tree `88b9ecd31bb88e7c105b7f0aafc787058c0e8c97`); the two changed implementation/test files in that snapshot have the exact SHA-256 values listed above, matching the committed candidate. The implementation commit adds only those two files relative to that parent.
- The snapshot also recorded pre-existing local changes to `TesteSimulacao.cs` and three ProjectSettings files. Those files were left untouched and excluded from the candidate commit; their captured hashes remain in the package manifest for source-context traceability.

The original raw reports and run logs remain in protected private evidence storage. The public package contains only the normalized full EditMode XML; it does not replace or reinterpret the original reports.

## Review and promotion status

This record documents validation only. Independent exact-tip review is pending. The candidate has not been promoted to Core canonical, and CORE-F remains incomplete.