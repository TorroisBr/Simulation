# P12-E technical-design review — LocalTopology absence correction

**Review ID:** P12E-DESIGN-EXACT-TIP-68AD469-R1  
**Outcome:** `PASS` for the exact technical-design document and the correction to finding E1. This is design review only; it does not validate code, promote a capability, or close P12-E/Phase 12.  
**Readiness:** No P12-E owner slice is `READY_FOR_IMPLEMENTATION`. The design’s owner-specific evidence requirements remain unmet.

## Exact revisions reviewed

- Candidate branch: `codex/phase12/P12ELocalTopologyCompositionDesignCorrection`.
- Candidate commit: `68ad4697117c72ba42718de0db8ab7307436c007`; tree `e001a61b22a90e351f06e6ad2268dd7862fafca6`.
- P12 canonical base: `da2a73896bc405ae6f11c536a5fbe8d471b00c21`.
- Architecture canonical: `47eff220c7ce00f6e7c759bdc2b76780bb46f628`.
- Candidate changes only `docs/design/PHASE12_E_TECHNICAL_DESIGN.md` (blob `15aaee09d3295cff81a48e166b620c89f5156346`) relative to the P12 base; no code, inventory, State, or test files changed.
- The prior finding is `P12E-DESIGN-CURRENT-DA2A738-R1`, recorded at `76ec4f0de74a48e03d1d0125976cfcc816d18bb8`.
- Current profile inventory blob at the base: `0757cd0c39e7c1e53ae0c0ae99fe191151ef5dc3`. Current State continues to say no E owner slice is implementation-ready.

## Finding E1 resolution

The prior design incorrectly required an instantiated empty `LocalTopologyStore` for the accepted Daily-v1 profile. Candidate §4 (`PHASE12_E_TECHNICAL_DESIGN.md:222-237`) now says that the store is `NOT_COMPOSED`; requires a typed absence/provider-absence witness bound to the exact effective profile and provider inventory; distinguishes that witness from a composed-empty owner section; rejects unexpected composition/injection or populated P10 state; and explicitly forbids requiring an empty store section or instantiating a store to satisfy the witness.

This matches the current canonical evidence:

- The owner inventory says `LocalTopologyStore` is not composed and must be treated as `NOT_COMPOSED`, not empty (`PHASE12_OWNER_COVERAGE_INVENTORY.md:807-814`).
- The selected Daily-v1 composition tests assert `runtime.LocalTopologyStore` is null at lines 1668 and 1749 (`Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs`).
- `SimulationRuntime` accepts the owner as optional and resolves it only from an explicit argument or an ArmedForce spatial owner (`SimulationRuntime.cs:1232-1233`); the selected P10 path is separately gated from the Daily-v1 profile.
- The design’s §2 already separates composed required-empty, not-composed/disabled, and excluded sections. The corrected §4 now applies those distinctions consistently.

The correction preserves P10 as a separate proving profile. It does not add Ruin/LocalTopology to Daily-v1 or weaken P10 admission rules. The reference in §7 to excluded-or-empty state is now resolved by the specific §4 disposition.

## Remaining contract review

The rest of the exact design remains compatible with current architecture, accepted P12 scope, and the current D design:

1. P12-E consumes the promoted P12-B completed-boundary token and exact owner-section identity/cardinality/schema/revision evidence. It adds no capture lock or persisted token. It consumes P12-C identity, genesis/provenance, sequence, and random roots without rerunning genesis, minting IDs, or claiming P13 history/fork semantics.
2. Current Daily-v1 City fields remain D-owned. Shared `CityRuntime` and `NpcRuntime` values are captured once with the same token and component revision vector, split into disjoint D/E/F projections, merged, and reconstructed once. Knowledge and active commitments remain F-owned; P12-G owns whole-graph validation, reference resolution, parity, and publication.
3. Each E authority still requires exact retained fields and links, all supported writer paths, exact-once revision/capture identity, immutable detached export, private staged construction, rejection cases, and a safe owner handoff. Hydration restores recorded owner truth without replaying daily systems/events or reapplying terminal effects.
4. P18 intraday/activity state, P19 loader state, P20 activity state, P13 history/fork guarantees, P14 material flow, P10 Ruin/LocalTopology, and P12-F/G responsibilities remain outside E. The design adds no Mod API or loader work and remains compatible with both current architecture alignment records.

This review does not claim the corrected typed absence witness is already emitted by current code. Any implementation must encode and validate that disposition at the accepted profile/provider-manifest boundary; it must not silently reinterpret absence as an empty owner or imply new P12-B scope.

## Implementation readiness and limits

No owner slice is ready. P12 State records E owner fields, supported writes/revisions, and staged reconstruction evidence as outstanding. Candidate §3/§7 likewise says the inventory is not exhaustive and requires owner-specific evidence before implementation. Current startup emptiness and census witnesses do not establish an exact export or staged hydrator. This review approves the design correction only; P12-A remains `WAIT_DEPENDENCY`, and P12-E and Phase 12 remain open.

No tests were run: the reviewed delta is documentation-only.
