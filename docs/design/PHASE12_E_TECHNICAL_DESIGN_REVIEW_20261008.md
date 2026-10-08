# P12-E technical-design review — current canonical content

**Review ID:** P12E-DESIGN-CURRENT-DA2A738-R1  
**Outcome:** `NEEDS_CHANGES` — one exact-profile inconsistency blocks approval of the design. The remaining reviewed contract is aligned with current P12 dependencies, owner boundaries, and stated exclusions.  
**Readiness:** No P12-E owner slice is `READY_FOR_IMPLEMENTATION`. This review authorizes no implementation, canonical promotion, P12-A integration, or Phase closure.

## Exact content reviewed

- P12 canonical: `codex/phase12/canonical` at `da2a73896bc405ae6f11c536a5fbe8d471b00c21` (tree `03d14f8bac395b932b23ec3a107653cefd70cf4d`).
- Architecture canonical: `codex/architecture/world-identity-projection` at `47eff220c7ce00f6e7c759bdc2b76780bb46f628`.
- Exact design blob: `docs/design/PHASE12_E_TECHNICAL_DESIGN.md` — `2e172f17e61e264f1129a00714ae3afeff5bd365`.
- Exact owner inventory blob: `docs/design/PHASE12_OWNER_COVERAGE_INVENTORY.md` — `0757cd0c39e7c1e53ae0c0ae99fe191151ef5dc3`.
- Current P12 State and Brief blobs: `35205ed70f3d0bd4c0a3bda52c92d6b570ead395` and `31d9e1b4df41fc994f0a747274e35c4ef40b6e3a`.
- Architecture source blobs consulted: `SIMULATION_ARCHITECTURE.md` `25843842688239cdc3b80988b2e28dbaa16b4987`; `EXECUTION_MODEL.md` `9d007aa93e602a2f8242000d3c60864614b049f3`; `ROADMAP.md` `d03e144544ab25371b71db64538c0de47ae8381c`.
- The architecture’s intraday/extensibility and multi-participant alignment records at that architecture tip are blobs `a231a2a014bf58be5ce382c48a55f3654df89a61` and `4ed6fcc60b348461e3201d4f3d480c21a3154ec3`.

This review targets the design and evidence at the stated current canonical, not its older prior review or any implementation candidate. No code or test execution was performed.

## Finding that blocks approval

**E1 — Daily-v1 LocalTopology presence contract contradicts current profile composition.** `PHASE12_E_TECHNICAL_DESIGN.md:220-224` says excluded P10 state includes “the instantiated `LocalTopologyStore` which must be witnessed empty.” But the current owner inventory at `PHASE12_OWNER_COVERAGE_INVENTORY.md:807-814` says `LocalTopologyStore` is not composed in the selected P9-B-only Daily-v1 profile and must be represented as `NOT_COMPOSED`, not as an empty store. The selected Daily-v1 composition tests assert `runtime.LocalTopologyStore` is null at lines 1668 and 1749 of `Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs`. `SimulationRuntime` allows this optional owner to be absent (`SimulationRuntime.cs:1232-1233`). The design itself correctly distinguishes “required populated or empty,” “not composed/disabled,” and “excluded” in §2 (`lines 49-58`), so §4 is internally inconsistent as well as inconsistent with live evidence.

This cannot be satisfied by demanding an empty owner section: that changes the accepted profile from absence to composition and risks admitting P10 state into the P12-E profile. Correct §4 to require the manifest’s typed `NOT_COMPOSED`/provider-absence witness for Daily-v1 and to reject unexpected instance injection or populated P10 state. Keep P10 excluded; do not instantiate a store merely to satisfy this design. Then obtain a fresh exact-content design review.

## Other reviewed contract areas

1. **P12-B and P12-C dependencies:** The design consumes the promoted P12-B successful completed-boundary token and owner-section identity/cardinality/schema/revision vector. It creates no second capture lock and does not serialize that transient token. It consumes P12-C’s existing world identity, genesis/provenance, ID/sequence, and random roots without rerunning genesis, minting IDs, or assigning P13 fork/history behavior. These boundaries agree with current P12 State and the accepted Brief.

2. **P12-D/E/F partition and current D proposal:** The design assigns current Daily-v1 City roots to D and distinct official-provider facts to E, forbidding duplicate City/NPC fields. Nested `CityRuntime` and `NpcRuntime` values are captured once under the same P12-B token and exact component revision vector, split into disjoint sections, merged, and reconstructed once. This aligns with the current D technical design (`PHASE12_D_TECHNICAL_DESIGN.md:42-49,117-125`) and preserves D/E/F ownership. P12-F keeps Knowledge and active commitments; P12-G owns graph validation, reference resolution, parity, and publication. The reviewed design does not claim that a census witness or transaction rollback snapshot is an export/hydrator.

3. **Owner contracts, revisions, staging, and replay sensitivity:** The design requires detached immutable values, an owner-controlled exact revision or capture identity, enumeration of every supported writer including direct and daily paths, and failure when mutable exposure or a writer bypasses the snapshot boundary. Hydration occurs into private candidates in dependency order and restores recorded state; it does not rerun daily systems, replay events, reapply terminal outcomes, or publish the active runtime. Failure must leave live owner truth, revisions, balances, bindings, and random state unchanged. These requirements fit the existing deterministic-continuation and owner-authority constraints.

4. **Architecture alignments and exclusions:** The design keeps P18 temporal/activity/continuation state, P19 loader/mod-owned state, P20 shared activity state, P13 history/fork guarantees, P10 Ruin/LocalTopology, P14-A material flow, and P12-F/G responsibilities outside this E slice. It treats moddability as a current review constraint without adding a Mod API or loader. No conflict was found with the current intraday/extensibility or multi-participant alignment records.

## Implementation readiness

No owner slice is ready. The design’s §8 permits implementation only after fresh exact-tip design review **and** a field-complete owner slice with current provider evidence and safe handoff. Its §3/§7 explicitly say the provider inventory is not exhaustive and require exact retained fields, cross-links, owner revision/capture identity, every mutation path, detached export shape, exact private factory, and rejection fixtures for each slice. Current State also records that no E slice is implementation-ready. The inventory identifies existing gaps: for example, `InstitutionStore` and `OfficeStore` have no local revision, while other candidate owners expose only selective revisions; the current inventory says no listed E authority has a complete immutable export plus staged hydration path. Empty startup state, census coverage, and passing unrelated tests do not close these gaps.

## Smallest next step

First correct the §4 `LocalTopologyStore` statement to match the accepted `NOT_COMPOSED` profile contract and request a new exact-content design review. After that passes, the smallest implementation-readiness task is an owner-specific source evidence sheet for one isolated E authority: effective provider identity; complete retained field/link set; every supported writer and exact-once revision behavior; immutable detached export; private staged factory; rejection cases; and any D-root or shared-hotspot handoff. Do not mark a slice ready until that evidence is complete.
