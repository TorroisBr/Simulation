# Phase 9 Technical Design Proposal — UNAPPROVED

**Status:** technical-design proposal; this text awaits independent re-review.
It assigns no checkpoint IDs and authorizes no implementation. The current
Phase 9 Brief and accepted checkpoint scope remain controlling.

**Historical independent technical-design review:** PASS on commit
`89dfd7329c1b1cb836b77ff5277b198c20dbe7c5`; that review applies to the earlier
proposal revision. The P8-E promotion refresh passed targeted independent
review at `4b3e651137824124cdb770a15bf23f51c618e4f7`. This revision clarifies
the boundary between existing scheduled directives and P18 activity state.
Independent re-review of the refreshed document set passed at
`4895cf91b8205d3ba75db8f2d9e3da8e76804fde`. No checkpoint IDs or implementation
authorization are added.

**Baseline:** `codex/phase8/canonical` at
`c5b2e06b534f4b2af38f10e6510b10800aa8b28c`, containing the architecture and
roadmap refresh at `c285466c355103d3637ac165246591b72eb7bda0`; refreshed Phase 9
entry proposal at `05ba224da8cead8221d12fb1b9dc64da5a3b61d2`; and the Phase 8
State at that historical canonical baseline. The architecture alignment records are
`architecture/INTRADAY_EXTENSIBILITY_ALIGNMENT.md` and
`architecture/MULTIPARTICIPANT_ACTIVITY_ALIGNMENT.md`.

**Targeted P8-E promotion revalidation:** checked against Phase 8 canonical
tip `77f3e1a` (P8-E promotion `d95b60d174cb0b17df09e2775b3cbd134c74b21f`),
with architecture baseline still `c285466c355103d3637ac165246591b72eb7bda0`
and both alignment records current. P8-E is now canonical. Its civil-travel
capability is not a blanket dependency for the approved authored
Unity-bootstrap-only first profile; it is conditional only if a selected
profile output consumes P8-E travel/route-plan facts. This revalidation does
not add travel facts or change the first profile.

## 1. Recommendation and bounded first profile

Implement genesis as a deterministic pre-start pipeline over a resolved,
explicit profile. The approved first delivery uses the existing authored Unity
bootstrap world as a proving profile for generic deterministic genesis, backed
by its `SimulationConfigData` and the domain facts this path actually composes.
Its world and population extent come from authored configuration. It promises
no procedurally generated terrain, settlements, population, or pre-simulation
backstory. Keep initial facts in existing domain authorities and preserve the
pipeline seam for later procedural consumers. Scope the proving profile to an
explicit daily-execution profile, with compatible simulation/content versions,
effective configuration, effective calendar, authored initial inputs, and an
explicit reproducible root seed. Do not promise support for arbitrary
hand-assembled `SimulationRuntime` instances or optional injected providers
that the normal bootstrap did not select; this design does not promise an
arbitrary-provider API or P19 loader/public surface.

The product boundary for this first delivery is approved; a later checkpoint
must still enumerate the included authorities and prove profile completeness
against their invariants before implementation begins. The proposal does not
add local generation, route plans/Knowledge, initial timed activities, shared
participation, a content catalog, or new gameplay. P8 capabilities are needed
only when selected initial facts consume them: P8-A for authored factual
geography; P8-B for passages; P8-C for City/Site anchors or Person positions;
P8-D is canonical and available for route plans or route Knowledge if selected.
P8-E is now canonical, but is not a blanket prerequisite; it is conditional
only when selected initial outputs consume its travel/route-plan facts. P18 is
conditional on selected timed-activity/lifecycle or participant-availability
facts; existing scheduled directives retain their current domain semantics.
P20 applies only to selected shared activity/participation facts. P19's loader
and public API remain deferred.

The inspected `TesteSimulacao.InitializeSimulation` path constructs time,
randomness and runtime identity infrastructure, creates runtime objects from
authored data, resolves effective configuration, and assembles systems in
`RebuildSystems`. `RuntimeIdAllocator` and `RuntimeIdentityRegistry` provide
runtime representation identity; they do not establish semantic `PersonId`,
`HexId`, or `LocationId` for generated facts. `SimulationConfigurationResolver`
resolves defaults, preset, world overrides and content overrides into
`EffectiveSimulationConfiguration`; the effective calendar is a separate
authoritative input. These boundaries support the proposed profile but do not
themselves implement or guarantee deterministic genesis.

## 2. Pipeline model and contracts

The pipeline is a resolved immutable plan plus isolated candidate execution.
Every selected stage/contributor declares:

- stable semantic stage and contributor IDs, independent of registration order;
- compatible version/revision and provenance identity;
- typed input and output contracts, including the domain authority that owns
  each authoritative output;
- explicit required stage/output dependencies and compatibility requirements;
- conflict policy for overlapping writes, or a declaration that writes are
  disjoint; and
- any purpose-specific random context it consumes.

The plan is validated before execution. Reject duplicate identities, missing or
incompatible dependencies, undeclared input consumption, cycles, ambiguous
ordering, and unresolved conflicting writes. Dependencies form a DAG. Execute
in deterministic topological order; among simultaneously eligible stages use
stable semantic IDs as the tie-break. Contributions targeting the same domain
fact must be disjoint or use an explicitly declared deterministic merge rule.
Never resolve conflicts by incidental collection/registration order, runtime
ID allocation order, host scheduling, or “last writer wins” without a named
contract.

```text
authored inputs + selected contributors
  → resolve profile, content compatibility, effective configuration/calendar
  → validate stage graph and declared input/output contracts
  → execute dependency-ordered stages against unpublished candidate authorities
  → route proposed facts through their owning domain authorities
  → validate complete profile World Truth and cross-domain references
  → publish one complete initial composition
  → establish first actually simulated boundary
```

Stages may propose or construct candidate facts, but may not maintain parallel
generator-owned truth. Domain authorities remain the owners and validators of
their facts. Candidate authorities must be isolated from the live runtime
composition until every stage succeeds and the complete profile passes
validation. A failure discards the candidate result; no partial world becomes
visible. Publication is a single composition handoff/seal before the first
simulated boundary, not a series of live mutations. Concrete types and host
composition API require implementation design after this proposal is accepted.

Any future generated backstory may explain initial facts but is not simulated
history; the approved first proving profile includes no backstory promise. No
simulated boundary exists until the complete initial profile is published and
startup declares the first actual boundary. After that point, changes use
ordinary domain mutation authorities; genesis privilege is closed.

## 3. Identity, provenance, compatibility, and randomness

Authoritative generated facts require semantic IDs stable for the compatible
profile and causal inputs. The exact generation algorithm is deliberately not
chosen here. A later checkpoint must select and review it, including collision
behavior and version compatibility. Runtime IDs remain local representation
handles and may neither define semantic IDs nor determine generated identity
through registration/allocation order. An ID used by causal facts or
reconstruction must be part of the recoverable semantic result.

The published result records sufficient provenance to identify the compatible
simulation version, selected profile, effective configuration values/revision,
effective calendar, content/definition identities and compatible versions,
root seed/random algorithm, and every selected stage/contributor ID and
version. For each contribution it must preserve the declared dependency and
causal input identity needed to distinguish materially different initial
worlds. Asset names or a preset name alone are insufficient if their values may
change. Exact storage representation is deferred to the owning implementation
and persistence designs.

Randomness is purpose-scoped. Derive a stage's deterministic random context
from stable causal inputs: root seed, compatible profile/version, stage and
contributor semantic identities/versions, and explicitly declared upstream
input identities. Separate purposes or independently named draws so unrelated
random consumption and unrelated stage additions do not shift existing
outcomes. A stage's purpose key and derivation version are part of its
compatibility/provenance contract. Do not use one shared sequential stream
whose output depends on execution/registration order. The exact derivation and
draw algorithms remain a checkpoint decision.

## 4. Moddability and extension boundary

Extensibility is a constraint on the seams designed now: generation logic is
separable from Unity presentation, stages consume declared semantic inputs,
outputs route through domain ownership, and compatible contributors can be
composed deterministically without rewriting core concepts. Design against
real consumers and avoid speculative universal registries or generic rules
frameworks.

This Phase 9 design does not build P19's code-mod loader, public API, packaging,
module lifecycle, compatibility resolver, or runtime extension installation.
The initial profile may select built-in contributors through the host's
composition root. Future P19 adapters can bind compatible contributors to the
same contracts after real extension consumers and public contracts are
reviewed. No adversarial mod/player security architecture is introduced;
normal domain coherence and complete-state validation remain required.

## 5. Existing-world retrofit is separate

Installing or enabling a contributor never reruns genesis, adds facts to old
worlds implicitly, or changes historical settlement placement. Existing-world
retrofit is an optional, explicit later domain/module migration at a declared
post-start boundary. It needs its own selected contributor/version, compatible
input boundary, domain-owned mutation and validation, and recoverable causal
provenance. It is not part of the first-world profile or this implementation
proposal, and must not be smuggled into a “refresh” operation. Runtime world
expansion likewise retains its own post-start authority and is out of scope.

## 6. Dependency and readiness effects

This proposal consumes stable P8 contracts and the promoted capabilities
actually needed by the chosen profile. At historical baseline `c5b2e06`,
P8-A/B/C/D were canonical and P8-E was design-approved with
implementation/promotion pending. At current revalidated canonical tip
`77f3e1a`, P8-A through P8-E are canonical. P8-D is relevant to this proposal
only if the accepted profile selects route plans or route Knowledge; P8-E is
relevant only if selected outputs consume its civil-travel/route-plan facts.
Neither capability is a blanket P9 dependency. If the
profile is daily-only and has no P18 activity/participant state, P18 is not a
gate; existing scheduled-directive commitments retain their current domain
contract. If it initializes timed activities, it must consume the relevant
accepted and promoted P18 contracts/capabilities. If it initializes shared
activity/participation facts, it additionally needs the applicable P20
contracts/capabilities. Neither is a blanket genesis prerequisite, and P18
must not be made to wait on P20.

P10 can design against the P9 pipeline contract, but local generation enters
only when its specific P8/P9 contracts and capabilities are accepted. P12 may
inventory continuation in parallel; a save claim must include the initial
World Truth and provenance in its selected support profile. P13 additionally
requires recoverable initial and changed state. Neither downstream phase
retroactively supplies missing genesis causality.

Implementation remains unschedulable until a separate checkpoint contract
accepts the first-profile authority inventory (consistent with the approved
authored-bootstrap proving profile), exact stage/input/output and compatibility
set, semantic identity and provenance strategy, deterministic conflict/RNG
rules, complete validation/publication boundary, and any spatial capabilities
not already promoted that the profile requires. This proposal assigns no IDs
and does not satisfy that gate.

## 7. Validation obligations for an implementation candidate

The implementation contract should require tests proving:

1. identical compatible profile, authored inputs, effective configuration and
   calendar, content versions, and root seed produce semantically identical
   authoritative initial facts across supported hosts;
2. changing an unrelated contributor or its random consumption does not shift
   unaffected stage outcomes, while declared upstream factual changes can;
3. stage order is deterministic under reordered source collections, and
   missing/duplicate/incompatible/cyclic declarations or unresolved write
   conflicts fail before publication;
4. stage failure and whole-world invariant failure leave no partially visible
   runtime world;
5. generated semantic IDs do not depend on runtime registration order, and
   all cross-domain references resolve to owning authorities;
6. provenance identifies compatible content/configuration/calendar and
   contributor versions needed to reproduce the same initial profile; and
7. initial generation creates no simulated history before the first boundary,
   with normal runtime mutation authority taking over after publication.

Run relevant domain suites and required repository regression gates only after
an implementation checkpoint is approved. This document-only proposal has no
Unity test requirement; validate its diff with `git diff --check` and
independent architecture/design review.

## 8. Open gates

- Independent review and acceptance of this technical design.
- Checkpoint contract enumerating the authorities and optional spatial facts
  used by the approved authored-bootstrap proving profile; no additional
  gameplay/content scope is inferred here.
- Selection/review of semantic ID and purpose-scoped random derivation
  algorithms as part of the accepted checkpoint contract.
- P8 capabilities for selected outputs; P8-A through P8-E are canonical at
  `77f3e1a`. P8-D route planning and P8-E civil travel are conditional only
  when the selected profile includes their corresponding facts; neither is a
  blanket prerequisite for the authored-only first profile.
- Separate implementation authorization, followed by candidate review and
  integration validation. No capability is promoted by this proposal.
