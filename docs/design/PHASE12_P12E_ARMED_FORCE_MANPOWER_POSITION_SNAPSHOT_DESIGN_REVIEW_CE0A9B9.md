# P12-E ArmedForce/Manpower/Position Design Review

**Result: NEEDS_CHANGES**

## Exact review target

- Candidate branch: `codex/phase12/P12EArmedForceManpowerPositionSnapshotDesign`
- Candidate: `ce0a9b9aab3ec9d5d969706a781bdd3a6042c956`
- Candidate document blob: `e243325025c057f89b14f8fec2f85a3553d3335d`
- Required P12 canonical base: `04e7c3f49a7c9690ebc091fcfc50f05ef67009d6` (verified current at review)
- Architecture authority: `47eff220c7ce00f6e7c759bdc2b76780bb46f628`
- Current general P12-E design: `docs/design/PHASE12_E_TECHNICAL_DESIGN.md`, blob `15aaee09d3295cff81a48e166b620c89f5156346`; latest correction commit `030dc1b13956bdb946f1a63869397d3e464850e4`
- Current candidate review checkout was clean and at the exact candidate. Candidate is a docs-only child of the required canonical base.
- This review does not reuse the earlier R1 NEEDS_CHANGES record for a stale general-design blob.

## Finding E1 — missing exact witness for manpower source-provider absence (blocks design approval)

The proposal correctly distinguishes a null/empty manpower source registry from absent `LocalTopologyStore`, and correctly says source-bound data must fail closed when the admitted authority cannot be resolved. However, it also requires an exact source-absence/profile witness and leaves its creation/binding to future P12-B evidence, while the current P12-B census does not provide that witness. This is a missing prerequisite for the design's claimed source-empty Daily-v1 composition, not evidence that a source provider is absent.

Evidence:

- `Assets/_Project/Scripts/P12EMilitaryOwnerCensusProviders.cs`: `ContingentManpowerCensusProvider.GetCurrentCensus()` emits only `SectionId`, schema, the installed `ContingentManpowerStateStore` object identity, `owner.States.Count`, and `owner.Revision`. It does not witness `SourceProvider`, the selected runtime's settlement registrations, or registry identity/absence.
- `Assets/_Project/Scripts/SimulationRuntime.cs` around construction of the manpower provider: `SettlementManpowerSourceRegistry.TryCreate` returns null for null/empty registrations; provider resolution then chooses the supplied `manpowerSourceProvider` or `contingentManpowerStateStore.SourceProvider` fallback. Thus “no registrations” alone does not prove the resolved provider is null.
- `Assets/_Project/Scripts/MilitaryManpowerFoundation.cs`: the store retains `sourceProvider` in a readonly field and exposes it through `SourceProvider`; this is not part of its passive census witness.
- `docs/design/PHASE12_OWNER_COVERAGE_INVENTORY.md` calls the Daily-v1 settlement registry empty and distinguishes it from uncomposed LocalTopology, but explicitly says startup construction is not a live exact-zero witness.
- The proposal's §4 acknowledges that P12-B must bind the no-registration/null-provider condition, yet names no current token/vector field or existing exact runtime-admission witness that does so.

Fail-closed behavior for a source-bound row is necessary, but not sufficient to satisfy the design's own current-profile admission contract: it would reject a populated binding after reading the document, while leaving the selected live composition's provider identity/absence unproven at capture. Empty manpower rows/cardinality likewise do not prove provider absence.

### Required bounded correction

Revise the proposal to identify a concrete exact witness available at the completed boundary for both (a) the effective registration set and (b) the resolved provider attached to the installed manpower owner. It must be bound to the same admitted profile/token and reject an unexpected non-null provider or registration before capture. Reuse an existing exact admission/token witness if source inspection proves one; otherwise mark that provider-absence witness as a prerequisite owned by P12-B and keep this sub-slice design blocked until it is delivered/reviewed. Do not infer it from the static asset, empty state rows, or owner count. Do not silently add a new census section or P12-B runtime behavior under this P12-E design.

This finding does not require changing gameplay semantics, adding source registration, serializing provider objects/capacities, or expanding the checkpoint.

## Verified design content

The following bounded contracts are consistent with the current general P12-E design and inspected owner code:

- The five value sections and their local owner/revision/cardinality binding are explicit; the three force projections share one exact `ArmedForceStore` identity/revision, while manpower and position use their own owner-local revisions.
- DTO fields cover the inspected force, contingent, relevant-person, cohort/source-binding, and typed-position records; detached values avoid serializing owner/provider/runtime objects.
- Source writes remain owned by `ContingentManpowerStateStore`; `Amount` mirrors `LivingRosterAmount`. The proposal preserves the existing source/cohort validation, checked amounts, revision semantics, and Battle prepared-batch rollback, including exact row/revision restoration. It does not treat local revisions as epochs.
- Private staging order (force/Person links, manpower bound to staged force/source authority, then position bound to staged P8 spatial authority) and no-public-write-replay construction are compatible with existing ownership.
- P16-A supply/receipt and P17-A War state remain explicit Daily-v1 rejection cases; P10 remains separate.
- The current general P12-E design's corrected Daily-v1 boundary is respected: `LocalTopologyStore` is typed `NOT_COMPOSED`, not an empty composed store. This proposal preserves the explicit P10 rejection and does not weaken that boundary.
- The candidate adds no P12-B changes, new P12 checkpoint identity, P12-A readiness claim, or P12-E implementation claim.

## Readiness and next step

This is a documentation/design candidate only; no code or tests were changed or run. The proposal is **not design-approved** because E1 leaves a required current-profile provider-absence witness unspecified. It is **not implementation-ready**. The smallest next action is an additive design correction that either points to and binds an already-existing exact P12-B admission witness or records the missing witness as a P12-B dependency with a precise contract owner and fail-closed rule. Afterward, obtain a fresh exact-content review. P12-A remains `WAIT_DEPENDENCY`; no P12-E delivery or Phase 12 completion is implied.
