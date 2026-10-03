# Phase 19 — Public Extension Surface v1

**Status:** bounded documentation design proposal; not independently reviewed. Phase 19 remains deferred for platform implementation, and no P19 checkpoint IDs are approved. This document defines neither loader/package technology nor implementation readiness.

**Design baseline:** canonical architecture tip da34d50bd7831ac3eefab31e925492ede8dded5c, branch codex/phase19/P19PublicExtensionSurfaceDesign.

## 1. Purpose and scope

This proposal outlines a presentation-independent public surface for real extension contributions already motivated by the promoted P9/P10 generation contracts and P18/P20 temporal and participant contracts. It defines the API shape, contribution registration and execution boundaries, compatibility identity, and the P12 profile boundary.

The proposed surface has two semantic contribution families:

1. Dependency-declared pre-publication generation stages, grounded in the P9 genesis pipeline and P10 authored geography/local-topology consumers.
2. Activity definitions and activity-specific participation/effect policies, grounded in P18 lifecycle and P20 participant contracts, plus current TravelParty and Expedition consumers of typed participant contexts.

These are consumer-backed interface families, not selected implementation checkpoints. No general plugin runtime, arbitrary method hooks, universal effects/workflow engine, new gameplay, package format, transport, renderer integration, retrofit behavior, or security boundary is designed here. Scope for a future loader checkpoint still requires its own product decision and checkpoint split.

## 2. Consumer evidence and current limits

| Consumer evidence in the canonical tree | Current seam and limit | Public-surface implication |
|---|---|---|
| P9-A/P9-B genesis and the P10-A Ruin authoring profile | SimulationGenesisPipeline exposes fixed built-in stage IDs and dependency order; TesteSimulacao.InitializeSimulation dispatches that order before publishing the completed bootstrap. P9-B contributes its one authored geography stage. P10-A consumes the produced Location through the P8/P10 owners. There is no extension-stage registry. | A generation contribution should be a named, versioned stage with declared inputs, outputs, and dependencies, executed before initial-world publication and publishing facts through their existing domain owners. New-world generation and explicit retrofit stay distinct. |
| P18 activity lifecycle and P20 participant contract | ActivityDefinition carries definition ID/version; ActivityLifecycleStore owns instances, participant relations, commitments, revisions, due work, and transition receipts. Its injected start validator is a narrow existing seam, not a general extension API. P20-A proves a synthetic shared operation; its promoted proof does not establish a universal activity system or complete mod API. | A public contribution should describe an activity definition and its role/count requirements and selected policy/effect handlers while leaving instance lifecycle and participant commitments with the promoted owners. |
| Existing multi-participant operations | ActionExecutionContext, ActionParticipationRequirements, and ActionExecutionValidator provide typed roles/count bounds. TravelPartySystem and ExpeditionSystem consume and validate these contexts under their own domain rules. Their C# types are runtime seams, not automatically stable public contracts. | Reuse the semantic distinction between definition, instance, participant and role. Do not expose the concrete Unity runtime classes or make one existing consumer the universal activity owner. |
| P12 accepted daily profile | P12 technical design admits one selected Unity bootstrap profile with authored P9-B geography. It explicitly excludes P19 modules/retrofit, P9/P10 generated-world content, P18 temporal state, and P20 shared activities. P12-B admission is still incomplete. | A P19 registration surface must be detectable at composition and must not silently enter UnityBootstrap-Daily-v1. Design intent alone is not profile-admission evidence. |

Relevant executable files are Assets/_Project/Scripts/SimulationGenesisPipeline.cs, TesteSimulacao.cs, ActivityLifecycle.cs, ActionExecution.cs, TravelParty.cs, and ExpeditionSystem.cs. Current code has no P19 registration surface.

## 3. Proposed public contract shape

The public contract is a small set of typed, semantic contribution contracts over immutable descriptors. Conceptually, an ISimulationExtension entry point describes a ModuleManifest and registers through an IExtensionContributionBuilder. The builder exposes AddGenerationStage(descriptor, handler), AddActivity(descriptor, policy/effect handlers), and conditional AddStateOwner(descriptor, owner) operations. Each operation accepts an immutable descriptor; runtime handlers receive only the corresponding typed context and owner-scoped ports. Names below describe responsibilities; these are illustrative and do not freeze final CLR names, ABI, serialization, or loader technology.

| Contract | Required information and behavior |
|---|---|
| Extension manifest | Stable module ID; module release/content identity; supported public API contract version; required module dependencies; declared contribution IDs and capability families. IDs are module-namespaced and stable across runs. |
| Module entry point | Describes metadata and contributes to a host-owned registration builder during composition. Registration is declarative: it cannot mutate a world, run a generation stage, or retain a live world/runtime reference. |
| Generation-stage contribution | Stable stage ID and revision; declared typed input/output kinds; explicit stage dependencies; deterministic execution entry receiving only the selected immutable inputs, effective configuration, logical generation context, and owner-scoped staging ports. Output enters existing domain authorities before world validation/publication. |
| Activity contribution | Stable definition ID/revision; supported participant roles and count/requirement policy; explicit policy/effect handlers for supported semantic boundaries. Invocation receives a read-only, boundary-specific context and returns a validated prepared operation for the owning domain authority to commit. Lifecycle, time, identity allocation, commitments, and transition receipts remain P18-owned; participant relation and coordinated requirements follow the supported P20 contract. |
| Extension-owned state owner, if a selected consumer needs it | Stable owner and section IDs, schema version, exact immutable semantic snapshot, staged hydration/relationship-validation seams, and declared causal inputs/order. This is an owner contract, not an opaque untyped blob service. It is conditional on a future profile that explicitly supports that owner. |

The host validates all registrations and freezes an immutable contribution manifest before world creation. Duplicate IDs, missing dependencies, incompatible API versions, unsupported contribution families, invalid dependency graphs, or ambiguous composition fail before the world is published. Stage execution follows declared dependency order; independent stages use a stable semantic tie-break based on module and contribution IDs. A consumer defines any permitted merge rule explicitly. Incidental file, assembly, collection, or discovery order never decides causal order.

For activity execution, the host calls a registered handler only at the supported decision, start, completion, or other explicitly exposed domain boundary. There is no per-frame polling or universal callback on arbitrary methods. Handlers cannot reenter or mutate the lifecycle owner while it is dispatching. A rejected, stale, or failed prepared operation leaves the owner write set unchanged under that consumer's normal atomicity contract. Any random context is deterministic and namespaced by module, contribution, logical boundary, and causal operation; unrelated modules cannot perturb it through shared global RNG consumption.

## 4. Module and contribution lifecycle

The minimal proposed lifecycle is:

1. **Discovered:** host reads module identity and declared public API compatibility without enabling world behavior.
2. **Selected and validated:** the user-selected set is checked for dependencies, duplicate IDs, API compatibility, and supported contribution families. Loading a module is not equivalent to enabling every contribution.
3. **Registered and frozen:** modules contribute descriptors; the host validates ordering and freezes the exact manifest. No registration changes are accepted after composition.
4. **Composed for a new world:** generation stages run in the validated order and publish through domain owners. The exact module/contribution manifest and effective behavior-bearing configuration become part of that world's compatibility identity.
5. **Active for that world:** activity and other selected contributions run only at their documented semantic boundaries. Continuation requires a compatible module set and execution identity.

The v1 proposal does not hot-reload, enable, disable, or replace code inside an existing world's history. Installing a module does not rerun prior generation. Existing-world support, when selected later, is an explicit retrofit/migration operation with its own state, causal-boundary, and compatibility contract; it is not an implicit consequence of discovery. A module missing from a world that depends on it makes continuation unsupported and must be rejected or routed through a separately designed migration.

## 5. Version and compatibility approach

Compatibility has distinct version dimensions:

- **Public API contract:** explicit major/minor contract version. A module declares the versions it supports; incompatible major contracts reject composition.
- **Module and contribution identity:** stable module ID, module release/content identity, contribution IDs/revisions, and declared dependencies. IDs are never reassigned to different semantics.
- **State schema:** each extension-owned authoritative owner versions its semantic section independently. A module release is not a substitute for a state-schema version.
- **Effective world manifest:** exact enabled module/contribution set, dependency-resolved order, behavior-bearing configuration, generation inputs/provenance, and any causal random context needed for reconstruction.

A world records or can recover the exact compatibility identity that produced it. Continuing a world requires the required module code/content and compatible effective manifest. Missing modules, unknown causal sections, mismatched revisions, or unsupported API/state versions fail closed before publication or continuation; they are never replaced with defaults or silently dropped. Any migration is an explicit, versioned owner transformation with staged validation and its own causal record. This proposal does not define a cross-release migration format or promise cross-version replay.

The existing SimulationGenesisPipeline fingerprint includes its current built-in configuration and legacy EnabledModules values. That legacy setting is not canonical P19 enablement authority and the existing fingerprint is not a complete extension manifest. A future supported extension profile must add exact extension identity under its own reviewed profile/schema contract rather than treating the legacy module set as proof of extension compatibility.

## 6. Authority and mutation boundaries

- Extensions may read only the documented immutable semantic views and may request writes through typed domain/application operations exposed for their contribution family.
- Existing domain owners remain the sole authorities for their facts. Generation and activity contributions do not create parallel World Truth, write directly to internal stores, or treat Unity objects, diagnostics, event text, or caches as authority.
- A module that owns genuinely new authoritative facts must declare one semantic state owner. Its retained facts, identity, relations, commit boundaries, deterministic order, effective content/configuration, and reconstruction inputs must be explicit; derived indexes remain rebuildable. No universal WorldEntity or central extension key/value store is introduced.
- A generation stage writes only during pre-publication composition through owner-scoped staging operations. The complete generated state and references validate before publication. Reinstalling or changing a generation module does not recreate prior facts or backstory.
- Activity lifecycle and participant availability/commitments stay in the existing temporal and participant owners. An extension handler proposes domain effects through owner commands; it does not replace the clock, scheduler, availability authority, command validation, or mutation guards.
- Extensions are player-selected code in a local single-player world. Domain validation and atomicity preserve coherence for supported operations; they are not an adversarial mod sandbox or anti-cheat system.

## 7. Boundary with UnityBootstrap-Daily-v1

The accepted P12 profile remains frozen. It contains the selected authored bootstrap, selected P9-B authored geography facts, and its explicitly inventoried daily owners. It excludes P19 module/retrofit state, P9/P10 generated-world content, P18 temporal state, and P20 shared-activity state. P19 does not expand or redefine this profile.

For that profile, admission must reject any non-base P19 module activation or contribution registration, extension-owned state owner, unsupported custom provider, or world carrying extension retrofit provenance—even when its current state section is empty. Rejection is based on the selected composition/manifest and owner inventory, not a zero-row export or the old EnabledModules field. The accepted P9-B authored geography stage is an explicit base-profile exception with its existing contract identity; it is not permission for other registered generation contributors.

P12-B admission is not yet complete, so this document records the required policy, not delivered enforcement. Before an extension-owned domain checkpoint can be promoted, it must either be excluded from the selected profile by construction or have a verified fail-closed admission/inventory hook and negative rejection validation integrated serially with the P12-B owner. A design statement or empty census alone does not prove exclusion. Any later profile that admits extensions requires its own exact owner exports, staged hydration, module/code/content compatibility, causal inputs/order, and deterministic continuation evidence; a generic P12-closed prerequisite is not imposed on stateless or new-world-only extension work.

## 8. Open gates and conclusion

No unresolved canonical semantic or architecture decision was found for these contribution boundaries: modules are player-selected local code; generation is pre-publication and owner-published; lifecycle and participant authorities remain domain-owned; deterministic identities/order and compatible causal context are required; existing-world retrofit is explicit; and P12 profile admission is exact or fail-closed.

The exact first product scope/checkpoint split, loader and package technology, public ABI details, and any retrofit or saved-mod-world support remain future gates. The Phase 19 Brief leaves loader technology open and approves no checkpoint IDs. This design therefore records a bounded surface proposal only; it does not authorize a loader/runtime, claim independent review, mark any checkpoint READY_FOR_IMPLEMENTATION, or promote Phase 19.

## Sources checked

- docs/SIMULATION_ARCHITECTURE.md §§2, 11–12, 91–93
- docs/ROADMAP.md and docs/EXECUTION_MODEL.md
- docs/phases/PHASE19_BRIEF.md
- docs/architecture/P12_CAPABILITY_DAG_AUDIT.md and docs/architecture/P12_CAPABILITY_DAG_MASTER_HANDOFF.md
- docs/design/PHASE12_TECHNICAL_DESIGN.md and docs/PHASE12_STATE.md
- docs/phases/PHASE9_BRIEF.md, docs/phases/PHASE10_BRIEF.md, docs/phases/PHASE18_BRIEF.md, docs/phases/PHASE20_BRIEF.md, docs/PHASE9_STATE.md, and docs/PHASE18_STATE.md
- Assets/_Project/Scripts/SimulationGenesisPipeline.cs, TesteSimulacao.cs, ActivityLifecycle.cs, ActionExecution.cs, TravelParty.cs, and ExpeditionSystem.cs
