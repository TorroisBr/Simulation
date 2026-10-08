# P12-D — Current-base revalidation: private owner package and D-graph assembly

**Classification:** bounded current-base technical-design addendum; submitted for independent exact-tip design review. It is not implementation authorization or a promotion record.

## Baseline and authority

- P12 canonical base: `a4ce0abcf261226f4b52fbacc8df9ef8f67a0de2`.
- Architecture baseline: commit `c285466c355103d3637ac165246591b72eb7bda0`; `docs/SIMULATION_ARCHITECTURE.md` blob `4a3c73c4428ba7bc43c28f617e243e4cd54078fa`.
- `docs/ROADMAP.md` blob: `f9bb445880948b5e493fbf7f5682c38a33d18589`.
- P12 Brief blob: `31d9e1b4df41fc994f0a747274e35c4ef40b6e3a`.
- P12 State blob: `700a1fad188049f23819c0beae23ea4e34e1ad1f`.
- Accepted D design: [`PHASE12_D_TECHNICAL_DESIGN.md`](PHASE12_D_TECHNICAL_DESIGN.md), blob `a6f72aabc26057b46ec8896738c1006c016d880e`.
- Current G design: [`PHASE12_G_TECHNICAL_DESIGN.md`](PHASE12_G_TECHNICAL_DESIGN.md), blob `f19b3a316627c4a0842a2b740c776e6f71f6d68e`.
- Promoted NPC D/F owner snapshot Assets tree: `d7e95170c31947fb611a7461133b60ce75ad1e4d`.

This is an addendum to the accepted D design, not a replacement. It resolves only the next bounded current-base design boundary: assemble the already-promoted Daily-v1 D owners into one private D package and validate their cross-owner graph. Existing schemas, accepted product scope, and the single-owner capture model remain in force. It does not amend architecture, create another checkpoint identity, or authorize code changes.

## Current-base findings and precise supersessions

The current D design remains semantically compatible on City fields, NPC D/F field ownership, owner-local revisions, typed references, the exact-empty Daily-v1 site boundary, and the split between D-owned staging and G-owned final publication. The promoted City, Person, Genealogy, legacy SpatialNetwork, ExplorableSite, exact-zero receipt census, City/NPC relation-order assembly, and NPC D/F snapshot APIs can be composed without replacing that work.

Two ordering/ownership statements need a narrow correction for the next implementation boundary:

1. D design §4 step 4 currently places Person restoration after NPC staging. Current [`PersonStore`](../../Assets/_Project/Scripts/Person/PersonStore.cs) exposes an exact private `TryCreateFromOwnerSnapshot` factory (around line 180), and the promoted [`P12DNpcRootOwnerSnapshot`](../../Assets/_Project/Scripts/P12DNpcRootOwnerSnapshot.cs) staging API (around line 756) already requires the staged `PersonStore` before constructing its NPC roster and validating reciprocal materialization links. The package sequence must therefore stage PersonStore first, stage NPCs once against it, and validate both directions and orphan conditions after the NPC roster exists. This preserves Person rows that are not materialized and does not make PersonStore depend on NPC construction.
2. G design §2 item 4 and §3 step 4 still describe an “E City”/merged D/E City projection for the selected profile. That wording is stale for current `UnityBootstrap-Daily-v1`: accepted D design §2 and the current field crosswalk assign all selected City, Market, custody/account, stock, population, ordered NPC-membership, and associated revision/receipt facts to D. E contributes its provider-owned state only; its provider operations write the D-owned City roots and do not create an E factual City projection. The current profile constructs one City from D facts. This addendum supersedes those G phrases only for Daily-v1; it does not define how a future profile could compose a separately reviewed additional owner.

The ordering correction is grounded in current code, not a product-semantic change. Relevant existing APIs include:

- [`P12DCityRootOwnerSnapshot.TryCaptureForStaging` and `StagingCaptureEnvelope`](../../Assets/_Project/Scripts/P12DCityRootOwnerSnapshot.cs), which capture/stage each City once and return its `P12DCityMembershipLinker`;
- [`P12DCityNpcRelationAssembler.TryFillMembershipsOnce`](../../Assets/_Project/Scripts/P12DCityRootOwnerSnapshot.cs), which validates all captured membership IDs and City/NPC/location reciprocity before filling private ordered membership lists;
- [`P12DNpcRootOwnerSnapshot.TryCapture` / `TryStage`](../../Assets/_Project/Scripts/P12DNpcRootOwnerSnapshot.cs), which bind the split NPC projections to the same token, capture stamp, and exact owner-section vector, stage each merged NPC once, and accept the staged PersonStore and City linkers;
- [`PersonStore.CaptureOwnerSnapshot` / `TryCreateFromOwnerSnapshot`](../../Assets/_Project/Scripts/Person/PersonStore.cs), which preserve the private Person rows, materialized-NPC IDs, local revision, and derived index;
- [`GenealogyStore.CaptureOwnerSnapshot` / `TryCreateFromOwnerSnapshot`](../../Assets/_Project/Scripts/Genealogy/GenealogyStore.cs), whose private factory checks its local graph; PersonId endpoint membership remains an outer D-graph check;
- `SpatialNetworkRuntime.CaptureOwnerSnapshot` / `TryCreateFromOwnerSnapshot` in [`SpatialRuntime.cs`](../../Assets/_Project/Scripts/SpatialRuntime.cs), which reconstruct exact legacy locations/routes and derived outgoing-route indexes;
- the B-owned `SimulationRuntime.TryValidateCompletedDailyCaptureToken`, which D reuses as a stale-boundary check rather than replacing with a D-specific lock or epoch.

## Smallest D package boundary

Add one internal Daily-v1 D package assembly entry point in a D-owned adapter/coordinator. Its contract should be no broader than:

```text
TryStageDailyV1DPackage(
    completedBoundaryToken,
    captureStamp,
    exactOwnerSectionVector,
    alreadyCapturedDAndMergedNpcInputs,
    stagedP8P9RootsAndAdmittedDefinitions,
    out opaquePrivateDPackage,
    out failure)
```

Names and concrete parameter types are implementation details. Prefer existing owner snapshot and factory types; do not add a public serialization API, generic restore framework, second capture lock, runtime publication hook, or replacement owner adapter. The result is an opaque, unpublished package containing the exact staged D owners, the already-reviewed single D/F-merged NPC instances required for D relation validation, and typed links to the supplied B/C roots. It does not absorb F's semantic ownership or duplicate its exported fields.

The entry point consumes one completed-boundary token, one transient stamp, and the exact `token.OwnerSections` vector object. It must check that those identities agree at package entry and again before returning. Every owner section must match the accepted Daily-v1 identity, schema, role, cardinality, and local revision in that vector. Keep source-local revisions and receipts exactly; do not mint an aggregate City/NPC revision. For owner snapshots whose existing API has no token fields, the D coordinator binds the immutable copy to this exact boundary evidence and validates the same B token after copying. The owner-section census is admission evidence, never a substitute for copying authoritative owner values.

## Required private staging order

All steps operate only on a private candidate; any failure discards it.

1. **Consume roots and boundary evidence.** Use the already accepted B token/quiescence contract and promoted C identity, allocator, sequence, deterministic roots, provenance, and P8-A geography. D does not allocate replacement IDs, mint a competing epoch, or capture after the token is stale.
2. **Copy exact owner inputs under that boundary.** Capture the already-promoted City and NPC D/F projections using their existing shared capture identity. Bind the Person, Genealogy, legacy spatial, and exact-empty site owner snapshots to the same token/stamp/exact section-vector evidence in the D coordinator. Revalidate the B token after copying. Preserve each owner's order and local revision; do not reconstruct values from diagnostics or gameplay APIs.
3. **Stage legacy spatial facts and PersonStore privately.** Rebuild exact legacy location/route identities and owner-derived indexes against the supplied registry, rejecting collisions and dangling endpoints. Build PersonStore from its exact snapshot before NPC construction; preserve dormant/unmaterialized Person rows, residence, and pending materialized-NPC IDs. Do not resolve a materialization link yet.
4. **Stage each D-owned City once.** Resolve each City definition and legacy-location reference against the already staged roots. Construct one City per captured City row from D-owned values, including its exact market/custody/stock/population facts, receipts/revisions, and ordered pending `ImportantNpcs` IDs/revision. Retain the existing membership linker. Do not construct a shell and replace it, or run an economy/provider operation during hydration.
5. **Merge and stage NPCs once.** Use the already-promoted D/F capture envelope: both disjoint projections for an NPC must name the same completed token, transient stamp, and exact owner-section vector/component revisions. Pass the staged PersonStore and staged City membership linkers to the existing NPC exact-value staging path. Resolve typed root links without `SetCurrentPresence`, `AddImportantNpc`, travel/action dispatch, Knowledge writes, plan regeneration, or other gameplay mutation. P18 LocalObservation and MerchantTradeState remain exact-zero census evidence only; never copy or replay their receipt data.
6. **Resolve relations and validate the complete D graph.** Resolve ordered City pending NPC IDs through the staged NPC map using the existing all-or-nothing relation assembler. Only after all IDs, owners, and reciprocal links are valid may it fill the private City lists once, without callbacks or revision changes. Then validate Person materialization in both directions against the now-complete Person/NPC sets; validate Person residence references, City/NPC current-City and current-legacy-location reciprocity, genealogy endpoints against staged Persons, and every legacy route endpoint against staged locations. Preserve City order and `ImportantNpcRevision`. Reject duplicate, dangling, cross-owner, nonreciprocal, or orphaned relations instead of repairing or inferring them.
7. **Preserve exact exclusions and close the boundary.** Require the selected `p12d.explorable-sites` witness to match its installed owner and B token and remain `ExplicitlyEmpty` at zero cardinality and revision. Reject populated site rows. Daily-v1 P10 LocalTopology remains `NOT_COMPOSED` under its existing typed absence witness; do not instantiate it or infer a site from a Location. Revalidate the same completed-boundary token and all package-level identities before returning the opaque private package.

### Relation and root semantics

The package validates only relationships required by current owner contracts:

- each captured City ordered NPC ID resolves once to the exact staged NPC; that NPC points back to the exact staged City and its exact legacy current location;
- each NPC that has a current City is present exactly once in that City's ordered membership; membership does not derive from NPC roster order;
- each materialized Person and NPC agree on the exact reciprocal `PersonId`/`NpcRuntimeId` binding; a Person with no materialized NPC is valid, and an NPC with no Person binding remains valid where existing runtime semantics permit it;
- Person residence and NPC residence/current/destination references resolve to the correct staged owner type; do not conflate Person residence with the NPC field;
- Genealogy edges have endpoints in the staged PersonStore, supplementing the GenealogyStore factory's own local validation;
- City and route references resolve to supplied P8/C roots or exact legacy SpatialNetwork objects according to their declared type. Legacy `SpatialLocationRuntime` identity never substitutes for P8 `LocationId`.

Do not add a blanket unique-residence, one-City, one-Item, or cross-typed-ID constraint. Enforce only the owner and relation invariants supported by the current schema/design.

## Ownership and final publication boundary

P12-D owns the selected Daily-v1 factual package: City and nested market/custody/account/stock/price/population facts, ordered City membership, Person rows/materialization relations, Genealogy edges, legacy spatial records, exact-empty site evidence, and validation of these D relations. E contributes provider state only. The merged D/F NPC value object remains one concrete NPC reconstruction: D and F retain disjoint semantic field ownership and their single capture envelope; neither makes a second NPC.

The D package is private and unpublished. It performs no mutation of the live runtime, mutation guard binding, active-runtime reference swap, whole-profile envelope validation, whole B–F graph validation, or continuation parity claim. P12-G remains the owner of composing all included B–F packages, validating the whole profile and its cross-package graph/parity, binding the fresh guard, and performing the one final runtime publication. A D graph pass is necessary but not sufficient for G's whole-profile pass. The daily profile's separate admitted site-empty and P10 absence facts stay as typed inputs; D does not widen P12-A, P13, or the P10 profile.

Failure at any stage returns no package and leaves the source runtime, source revisions, active runtime, and mutation guard unchanged. City membership's existing two-pass rule remains: prove every linker/map/relation before the first private membership fill; if any later D check fails, discard the whole private package. G's final publication remains the only active-runtime publication point.

## Hotspots and validation plan

The implementation should add the smallest D-only coordinator/package file and focused tests. Reuse the existing owner adapters. Expected read/integration hotspots are `P12DCityRootOwnerSnapshot.cs`, `P12DNpcRootOwnerSnapshot.cs`, `PersonStore.cs`, `GenealogyStore.cs`, `SpatialNetworkRuntime.cs`, the exact-empty site census provider, and the P12-B token validator. Do not modify `SimulationRuntime` capture/publication code or bootstrap composition as part of this D-only slice; do not overlap another writer on the City/NPC adapter files. P12-G consumes the returned D package in its own later integration boundary.

Focused evidence must cover: successful one-package assembly; exact token/stamp/vector binding and stale-boundary rejection before output; Person-first staging with dormant/materialized/NPC-only cases; ordered City membership and preserved local revisions; D/F projection identity; City/NPC/current-location reciprocity and duplicate/dangling/cross-owner/orphan rejection; Person residence/materialization and Genealogy endpoint rejection; legacy route/root identity checks; exact-zero site owner and Daily-v1 LocalTopology absence; and failure atomicity/source immutability when any later relation fails. No action, travel, plan, charge, provider effect, or Knowledge write may run during reconstruction.

At implementation time, retain inspectable focused XML/logs and source/tree hashes, then run the relevant promoted D-owner regressions, ALL EditMode, official Smoke, and `git diff --check` in the repository's serialized Unity validation slot. These are proposed obligations, not validation performed by this docs-only addendum.

## Scope limits and decision

This addendum claims only a current-base design boundary for private P12-D Daily-v1 package assembly and D-owned graph validation. It does not claim implementation readiness before independent exact-tip review. It does not complete P12-D, implement G publication, make P12-A or P13 ready, close Phase 12, add P10 LocalTopology or site rows, add P18 receipts, introduce new gameplay, or broaden the accepted profile. No unresolved product or canonical architecture decision is introduced; the two corrections above follow the promoted current APIs and accepted ownership contracts.
