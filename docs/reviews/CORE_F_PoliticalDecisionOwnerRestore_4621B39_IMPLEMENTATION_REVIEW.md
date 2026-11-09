# CORE-F Candidate 4621B39 — PoliticalDecision Owner Restore Review

**Verdict:** `VALIDATED_CANDIDATE` — independent exact-tip review PASS; no actionable findings.
**Review type:** Implementation and validation review of exact candidate commit `4621b39a0449a045b5bffeb54bf491ee8b5fa9a4`.
**Candidate branch:** `codex/core/CORE-F-restore-stage-transaction`.
**Exact base:** `5119f4160044598943ac02fa951b64cb454aaa40`.
**Candidate Git tree:** `78bb33fa64c2ae6a18a6cb1456186212ebfa4243`.
**Candidate `Assets` tree:** `a45905cb15af9869831bb54821228d12f3500eaf`.
**Current Core canonical at review:** `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea` (`codex/core/canonical`).

## Reviewed scope

The candidate carries the exact P12-E `PoliticalDecisionStore` through the selected Core owner graph and detached restore composition. It binds that store to the exact P12-D `PersonStore`, includes it in the Core owner mutation-guard graph, validates its Required census witness, and lets Unity project the same owner without replacing its identity. The diff also improves restore-publication diagnostics and adds regression coverage.

The review checked these changed files:

- `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.ImageRestore.cs`
- `Assets/_Project/Scripts/SimulationRuntime.cs`
- `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs`
- `Packages/com.simulation.core/Runtime/Application/DailyV1GenesisComposer.cs`
- `Packages/com.simulation.core/Runtime/Application/DailyV1RestoredExecutionBoundaryComposer.cs`
- `Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs`
- `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedRuntimeOwnerGraph.cs`
- `Packages/com.simulation.core/Runtime/Political/PoliticalDecision.cs`
- `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs`
- `tests/Simulation.Core.Tests/DailyV1GenesisComposerTests.cs`
- `tests/Simulation.Core.Tests/DailyV1SelectedRuntimeOwnerGraphTests.cs`

## Contract and implementation findings

1. **Exact P12-E owner binding is preserved.** Genesis creates one empty decision store and binds it to the selected PersonStore. Restore requires the staged P12-E store, checks its compatibility with the exact staged P12-D PersonStore, and binds that same object to the graph. The graph rejects replacement by another store. A graph without a decision owner or with an owner not bound to its PersonStore is rejected before an execution boundary is returned.

2. **Census identity, cardinality, and local revision are checked.** The Core census helper requires one unique Required section with the established P12-E section ID and schema, the exact graph-owned store identity, and cardinality/revision equal to its live census witness. It emits the same exact owner identity and live values into the normalized owner vector. Both daily recapture and restored-boundary recapture use this helper. Missing, duplicated, wrong-identity, wrong-schema, wrong-cardinality, or stale-revision rows fail closed.

3. **Unity retains the same authoritative store.** When a Core-bound selected graph supplies the P12-E owner, `SimulationRuntime` rejects a different supplied store, validates record relationships and revision through a scratch copy, then retains the exact Core store instance. It also checks the prebound graph identity and includes the owner in mutation-guard binding. The scratch validation copy is not installed as an authority.

4. **Detached restoration remains staged and unpublished until existing commit.** The P12 image coordinator requires the staged P12-E decision owner and forwards it to Core composition. The owner-stage path checks the P12-E/P12-D shared PersonStore and passes the staged decision store through the restore composer. No host-created replacement owner is accepted. The composition APIs return a candidate; the existing application publication gate remains responsible for the atomic session exchange.

5. **Regression coverage exercises identity and failure boundaries.** Tests cover genesis owner presence and binding, refusal to replace the graph owner, exact census rebinding, foreign identity and revision rejection, omission of the P12-E owner, and the normal successful owner-passing path. Additional restore-token assertions verify the rejected stale-source attempt does not invalidate or mutate the candidate. Existing assertions are retained; none were weakened to fit the implementation.

The changes do not alter the P12-E section ID, schema, role, payload, or wire format. They do not alter Justice Records/P18 receipt identity. That separate host-neutral Justice decision remains governed by its existing architecture review. No concern was found with host-neutral .NET composition: the exact store and PersonStore are Core objects, and no Unity type is required by these bindings.

## Validation evidence

The runs were captured against the validated working source immediately before the candidate commit. The changed production and test file bytes were compared with the official run manifests and match candidate `4621b39a0449a045b5bffeb54bf491ee8b5fa9a4`. The Unity snapshot reports stable input; Core cold/warm input and generated-dependency snapshots are stable.

| Gate | Run identity | Result | Result artifact SHA-256 |
|---|---|---:|---|
| CoreValidation cold | `e827f34f78024ce98a769ea3d68104d1` / TRX run `adb43743-6881-4c23-917a-30322f0fa2e5` | 1081/1081 passed; 0 failed, skipped, or inconclusive | `CoreTests-cold.trx`: `1ec88ff98d3c715cbd1385a59a28460ea7bd4dc077752ed68b82c6ea08a8e791` |
| CoreValidation warm | same validation run / TRX run `c807397c-2898-4492-9186-ab8586f32282` | 1081/1081 passed; 0 failed, skipped, or inconclusive | `CoreTests-warm.trx`: `0aecd3346649e8191cf6b7b99c6f5faf193ae8add2f6abddf1fecc0d3aee06de` |
| Unity ALL EditMode | `d3a007d205f54a42918c2129ece270b4` | 3303/3303 passed; 0 failed, skipped, or inconclusive; input stable | XML: `cf057364aa93e06d6f0c68bcc550153593492028026596364e4ece62e7774d7c`; log: `6e949ebbc224b87f62abd37437be4436d33bbd4d0ef9ad1ab64475a235a3ce22` |
| Official EditMode Smoke | `88b4a81b41c949f387eb1d965ab3374f` (`Smoke` filter) | 5/5 passed; 0 failed, skipped, or inconclusive; input stable | XML: `8f4eb2c6f4eb832acf06bf5f1077b50e286da793346d8feb189b5a5704bd78de`; log: `35fbc54347ad9b590e4b4197da5fa9b31b8ee5a90c5da9f5f1bad27e2bd3dbcb` |
| Diff hygiene | candidate source diff | PASS | `git diff --check` |

CoreValidation’s recorded working-input digest is `98ebe217ccbe0bf71210145489e1c1587457b778c4e2fb98c2a584eefe391a0e`. Unity’s full-run input digest is `c7a6a29039d7e95f6bbde2d83f142171fd58dfd6ca74f24b2f438bb5123932a2`. The private original reports and manifests remain in the protected evidence store; no workstation paths, logs, or private evidence were added to this review commit.

## Review boundary and promotion verdict

This review validates only the exact candidate and bounded P12-E restore-owner integration described above. It does not claim P12-B completion, P12-A readiness, P13 readiness, full CORE-F completion, Phase closure, or canonical promotion.

**Canonical promotion: NOT performed or approved by this review.** Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
