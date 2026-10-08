# P12-E Current-Base Technical Design Review

**Review ID:** `P12E-CURRENT-BASE-DESIGN-0735103`
**Verdict:** `PASS` for the bounded P12-E design and corrected P12-D/E/F owner map.
**Implementation readiness:** `NOT READY` for P12-E owner implementation.
**Review mode:** Independent, read-only review of the exact documentation candidate. No code or tests were changed or run.

## Exact references

| Reference | Exact revision |
|---|---|
| Candidate reviewed | `0735103009b0161b3175349e9c78db241913de74` (`codex/phase12/P12DECurrentRevalidation`) |
| Candidate parent | `66cc39a244bda0afe20854897dc4a50757e5bc4f` |
| P12 canonical baseline at review | `0e786db8e6ed5ed937ff62e3f63258d8b73fd93c` (`origin/codex/phase12/canonical`) |
| Architecture canonical baseline | `47eff220c7ce00f6e7c759bdc2b76780bb46f628` (`origin/codex/architecture/world-identity-projection`) |

The exact candidate diff from P12 canonical is documentation-only: it adds the D and E technical-design candidates and updates the owner-coverage inventory. The inventory correction changes the bounded current B/C status and D/E/F owner assignment; it does not alter source or runtime behavior.

### Exact reviewed blobs

| Artifact | Blob SHA |
|---|---|
| `docs/design/PHASE12_D_TECHNICAL_DESIGN.md` | `18eece609c8d387d5407d188e5a9b4635526dccb` |
| `docs/design/PHASE12_E_TECHNICAL_DESIGN.md` | `2e172f17e61e264f1129a00714ae3afeff5bd365` |
| `docs/design/PHASE12_OWNER_COVERAGE_INVENTORY.md` | `bdeb78dbc4a7bede2c7a4c6252a14a55b497f61c` |
| `docs/phases/PHASE12_BRIEF.md` | `31d9e1b4df41fc994f0a747274e35c4ef40b6e3a` |
| `docs/PHASE12_STATE.md` | `b8ea8875b99f5fcc78943fc790ed86889eec6531` |
| `docs/design/PHASE12_CAPABILITY_CHECKPOINT_DECOMPOSITION.md` | `a1ce40a53f66b3a2248a616ae30375141aede7cb` |
| `docs/design/PHASE12_F_TECHNICAL_DESIGN.md` | `916310bc45e35822042fa94b0ddf191f17e930c6` |
| `docs/design/PHASE12_TECHNICAL_DESIGN.md` | `28c617b056bf2a1d1f2f0ed3075b2db007cc62b3` |
| Architecture `docs/SIMULATION_ARCHITECTURE.md` | `25843842688239cdc3b80988b2e28dbaa16b4987` |
| Architecture `docs/EXECUTION_MODEL.md` | `fcd49f34ec8b2f0321cc444bf7b3e612ffd963b2` |
| Architecture `docs/ROADMAP.md` | `d03e144544ab25371b71db64538c0de47ae8381c` |
| Alignment `docs/architecture/INTRADAY_EXTENSIBILITY_ALIGNMENT.md` | `a231a2a014bf58be5ce382c48a55f3654df89a61` |
| Alignment `docs/architecture/MULTIPARTICIPANT_ACTIVITY_ALIGNMENT.md` | `4ed6fcc60b348461e3201d4f3d480c21a3154ec3` |
| `Assets/Scenes/SampleScene.unity` at P12 base | `ae0a1696ba5ac13ae45f4c6d1319e1d6e3670008` |
| `Simulation-DailyV1.asset` at P12 base | `cadf254a0a4276a6bc32deb986929f42d7b170f1` |
| `Simulation-GeneralTest.asset` at P12 base | `6f5bd0bc0da6419bae268b7e57a1b669aa57cc76` |
| `SimulationRuntime.cs` at P12 base | `0e86af631c869c36e12450e1fcd537b1419e5fd4` |
| `TesteSimulacao.cs` at P12 base | `d1ba7d5f03578328d898dc55cda8b7cc56160fcf` |
| `PersistentConflictWarBattleContracts.cs` at P12 base | `30cbc501eea24796a7d3bac47515d7fba08fedc1` |
| `PersistentConflictWarBattleStores.cs` at P12 base | `901d278c24f3a96e419a1e55c7355bb67fb4e0e2` |
| `P17ARuntimeTests.cs` at P12 base | `ad6e872ed4e2025e2e9aa2cff6580beead28db86` |
| `SimulationConfigData.cs` at P12 base | `4756953bfd0cf7fc4fc9ac359bae81640876a00a` |

## Findings

### P12-B/C status is current and supersedes older inventory prose

The current State header and refreshed dependency DAG mark P12-B and P12-C `COMPLETE/PROMOTED` within their accepted bounded contracts. The B contract supplies admission and the completed-boundary capture token; C supplies the private identity/genesis/random roots, including the existing `WorldId`. The State explicitly says D/E capability dependencies are satisfied and require current-boundary revalidation before implementation readiness. The corrected inventory at lines 285–301 labels earlier baseline readiness and City assignments historical. Older State rows below the latest State header do not reopen B or C.

### Selected profile and effective-provider boundary

SampleScene references the asset whose GUID resolves to `Simulation-DailyV1.asset`. `Simulation-GeneralTest.asset` remains the separate P10-A Ruin/LocalTopology profile and is rejected by the selected Daily admission. Both assets request the same declared module set (Economy, Merchant, GuardCrime, Crime); those flags are configuration inputs, not proof of the effective instantiated provider graph. The E design correctly makes effective configuration and instantiated services/providers authoritative and requires exact provider identity/version and write-path inventory before any E adapter is implemented.

### Corrected D/E/F owner boundary

- **D owns all selected Daily-v1 City values:** City/definition/location roots; population aggregate, revision, and committed population-operation receipts/revision; ordered `ImportantNpcs` membership/revision; ordered market-item identity, amount, desired amount, and current price plus market revision; counterparty and population-economy identities/modes; and account identities, balances, and revisions. Economy provider effects are captured through these D roots. Current E emits no City section. P18 City daily-continuation receipts and P14 finite-source/`LastMaterialFlow` state remain excluded.
- **E owns distinct provider/core-domain facts only:** justice/crime/appraisal/guard authority facts; core political, institutional, property, force, Conflict, War, and Battle authorities; and any separately proven retained merchant/provider causal state. Commercial-sharing provider behavior/configuration belongs to E, but its observations, holder/provenance/freshness/revision records are Knowledge and belong to F. E does not duplicate D City fields, F Knowledge, active plans, or NPC fields.
- **F owns Knowledge and commitments:** `PoliticalKnowledgeStore`; commercial, spatial, and exploration observations where composed; directives; P11 terminal ActorChoice history; travel/expedition state; and active merchant/NPC action/travel commitment payloads. The shared `NpcRuntime` is captured once under the same B token/component revision vector; disjoint D/F projections merge before one reconstruction.

This allocation is consistent between the corrected inventory, the D candidate, the E candidate, and the F proposal.

### P17-A and the shared War owner

`P17AWarStrategicSection` is optional state on records owned by the existing `PersistentWarStore`; P17-A does not introduce a second War owner. The normal/selected Daily composition rejects populated P17-A War state, and the current negative test covers Standard and selected Daily compositions. E therefore covers the existing P12 War owner contract for this profile while admission fails closed on P17-A state. Census count/revision is not a serialization contract and cannot be counted as export coverage.

### Architecture and alignment compatibility

The design preserves the current `WorldId` through the C root and does not mint identities, rerun genesis, introduce a second capture lock, or claim P13 history/fork behavior. It keeps the bounded daily profile separate from P18 intraday continuation and P20 shared-activity commitments, consistent with architecture §§91A, 91B, 92, 92A and the current intraday and multi-participant alignment records. The design does not imply that P18/P20 state is empty merely because it is absent from E; excluded-state admission must reject unexpected populated state.

## Implementation readiness: NOT READY

No E owner slice becomes implementation-ready from this documentation correction or from B/C promotion. Before implementing an E slice, the owner-specific evidence remains required:

1. Exact current effective provider/owner identity, schema, conditional composition, and cardinality for the supported Daily-v1 runtime.
2. Exact mutable field inventory and typed cross-owner links, including separation of E-owned facts from D roots, F Knowledge/commitments, and derived/service state.
3. Every supported mutation path (direct APIs, transactional paths, and daily provider writes), with owner-authoritative revision/capture identity and evidence that no mutable-reference bypass can evade it.
4. A detached immutable owner export and exact private staged reconstruction/hydrator that preserve values, IDs, revisions, order, and terminal state without replay or reapplication.
5. Owner-local and cross-owner relation checks, explicit empty/disabled/excluded witnesses, and rejection fixtures for unsupported providers/state and malformed references.
6. A safe handoff for shared owners and hotspots. City is D-only; `NpcRuntime` remains a single D/F capture and reconstruction point; `SimulationRuntime.cs`/bootstrap integration remains serialized.

The prior P12-E Battle, Conflict, Estate, Institution/Office, Manpower/Spatial, Property, and War census designs/reviews are passive census evidence, not these export/hydration adapters. `MerchantSystem` retained causal state is unproven and needs an owner audit. P12-E remains open until all selected owners are covered and integrated; P12-A still needs complete owner coverage, P12-G validation/publication/parity, a validated live-profile inventory, and separate authorization.

## Required orchestrator follow-up finding

The candidate’s new D/E designs, current Brief, State, and profile code consistently select `Simulation-DailyV1.asset`. The older umbrella `docs/design/PHASE12_TECHNICAL_DESIGN.md` still says in its header and early body that `Simulation-GeneralTest.asset` is the selected Daily profile. Preserve this as a documentation mismatch: update the stale profile statement or add a conspicuous supersession note before treating that umbrella design as an implementation handoff. This stale text does not change the current P12 canonical profile or invalidate the bounded E/F split above, but must not be silently relied on.

## Review limits

This review validates the bounded design and source/profile facts listed above; it does not claim exhaustive owner/writer coverage or implementation readiness. No Unity or other tests were run, as this was a read-only design review. The review does not modify the candidate branch or P12 canonical.
