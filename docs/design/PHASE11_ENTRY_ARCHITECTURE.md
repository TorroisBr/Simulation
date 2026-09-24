# Phase 11 Entry Architecture Proposal

> **Status: product scope selected; architecture/technical entry design still
> pending.** The user-selected first consumer is not implementation approval.
> This proposal does not amend the Simulation Architecture, Roadmap, Phase
> Brief, or Phase State. No Phase 11 checkpoint IDs are approved or assigned.

## Purpose and evidence boundary

Phase 11's Brief describes a bounded actor-control and external-command slice.
The user selected the first consumer: **one actor-scoped choice of a supported
action replaces that actor's autonomous choice, while ordinary domain
execution still revalidates current World Truth.** This selection does not
grant hidden knowledge, outcome authority, broader ongoing control, or GM /
external command scope. The actor's eligibility, controller authority,
visible candidate/target set, application boundary, and exact action contract
remain unresolved. The Brief marks the phase `ENTRY_ARCHITECTURE_READY`; this
does not make implementation schedulable.

This proposal is based on assigned baseline `1f4651e99db2c357dd3be3c6b9284d104379f706`
in the isolated `codex/phase11/ActorCommandsEntryArchitecture` worktree. The
Phase 5, 6, and 7 State documents report those phases closed. There is no
Phase 11 State document or approved Phase 11 checkpoint decomposition in this
checkout. Phase 8's Brief describes an approved spatial/travel scope but says
its implementation has not started; Phase 11 must not assume that a travel
capability exists just because spatial contracts or legacy travel code exist.

Relevant authorities read: `AGENTS.md`, `docs/SIMULATION_ARCHITECTURE.md`,
`docs/ROADMAP.md`, `docs/EXECUTION_MODEL.md`, `docs/PHASE5_STATE.md`,
`docs/PHASE6_STATE.md`, `docs/PHASE7_STATE.md`, and the Phase 8–13 Briefs.
No separate ADR directory or ADR referenced by the Phase 11 Brief is present
in this checkout. Relevant executable surfaces inspected include
`WorldCommandFoundation.cs`, `CoreWorldCommandHandlers.cs`,
`NaturalLanguageWorldCommandContracts.cs`,
`DeterministicWorldCommandNaturalLanguageTranslator.cs`, `GMConsolePanel.cs`,
`WorldObserverDemoBootstrap.cs`, `NpcDecisionSystem.cs`, `NpcActionRuntime.cs`,
`NpcRuntime.cs`, `PersonRuntime.cs`, `PersonMaterialization.cs`,
`PoliticalKnowledge.cs`, `SpatialKnowledge.cs`, `LocalTopologyKnowledge.cs`,
and `SimulationRuntime.cs`, plus their command, translator, and mutation-guard
EditMode tests. Tests were inspected as evidence; none were run for this
documentation-only task.

## Current facts from canonical code and contracts

### Semantic contracts

- World Truth, Knowledge, interpretation, decision, execution, outcome, event,
  and history are separate layers. Planning/decision uses the relevant
  actor's Knowledge; execution revalidates current World Truth.
- A human's authority to choose an actor's decision does not grant omniscient
  information or authority to decide the resulting domain outcome. An in-world
  divinity, a GM, and an external controller are distinct roles.
- A command or explicit request is not autonomous behavior. Availability,
  domain enablement, autonomous policy, and provider/service registration are
  distinct. Explicit input still needs an available, supported domain path.
- `PersonId` is the durable individual identity. `PersonRuntime` currently
  carries identity, life dates, residence, and a materialization link; rich
  behavior and several legacy per-NPC knowledge stores are on `NpcRuntime`.
  A Person may exist without a materialized `NpcRuntime`. Existing command
  payloads that name an NPC commonly use `NpcRuntimeId`; they do not establish
  a general actor identity or control relationship.
- There is no universal Knowledge holder abstraction. The architecture
  explicitly leaves room for Person, Institution, and Faction knowledge
  without requiring one generic base class. Current political knowledge uses
  typed holders and stable Person/Institution/Faction IDs; spatial and local
  topology Knowledge are legacy NpcRuntime-owned models.
- Decisions, supported execution, and authoritative mutation must remain
  distinct. A preview or candidate-generation step cannot mutate, consume
  authoritative randomness, allocate causal identity, or choose on behalf of
  an ambiguous input.

### Existing autonomous and requested-action paths

`NpcDecisionSystem.ChooseAction` filters action definitions by current status,
creates available runtime actions/providers, applies autonomous-action policy,
calculates utilities, and chooses using a contextual authoritative random
source. The active decision is recorded as `Autonomous` by
`SimulationRuntime.EvaluateAction`.

`NpcDecisionSystem.CreateRequestedAction` can construct a requested action
after checking that the NPC is alive and its required status is present. It
does not itself establish who may request the action, a controller-to-actor
mapping, an actor-scoped information view, or a replayable input. The runtime
also has scheduled `RequestAction` and a narrow `ForceOutcome` directive path;
these are not a general live actor-input queue or the `WorldCommand` service.
No external command queue is currently composed into the `AdvanceDay` input
boundary. The relative ordering of future actor input, scheduled directives,
and autonomous choice therefore remains to be designed.

### Existing external command path

`WorldCommand` is a typed envelope with `Kind`, `Origin`, `Authority`, and a
typed payload. `WorldCommandService` registers one handler per kind, exposes a
preview, executes a handler, and records a result. Core handlers revalidate on
execution and delegate to domain-owned services/stores; the command layer is
not itself a general-purpose mutation authority.

Current core command kinds are `RelocateNpc`, `DeclareStackResource`,
`DeclareNotableItem`, `AddLocalPlace`, `AddLocalConnection`,
`GrantSiteKnowledge`, `GrantAdventureIntel`, `ResolveConflict`, and
`PlaceOpposition`. Current core authority rules are command-specific:

| Command kinds | Current accepted execution authority | Current meaning in the implementation |
|---|---|---|
| All listed kinds except the last two | `Declare` | The other modes are rejected by these handlers. This does not authorize a new command kind or bypass its domain checks. |
| `ResolveConflict`, `PlaceOpposition` | `Request`, `Declare`, or `ForceOutcome` | `Request` and unconstrained `Declare` use normal conflict resolution. Forced winner/outcome/participant constraints require `ForceOutcome`; structural identity, participant, and domain invariants remain enforced. |
| All kinds | `Suggest` for preview only | It is not executable authority. |

The `Request` distinction is important: it is a request through normal domain
rules, not an instruction to accept. `Declare` is only supported where an
existing handler defines a canonical declared operation. `ForceOutcome` is
currently limited to the supported conflict outcome constraints. Do not
generalize any of these into universal command modes.

The command ID allocator is an in-memory sequence. `WorldCommandRecord`
currently retains ID, absolute day, origin, authority, kind, success,
affected/created runtime IDs, event IDs, and diagnostic. It does **not** retain
the command payload, an actor/controller identity, submission/receipt order, or
a logical boundary ordinal. The record store is an in-memory guarded audit
store, not a persistence or replay contract. A result/record is not itself a
Domain Event or World Truth.

The deterministic natural-language translator returns a typed command or a
structured ambiguous/missing/unsupported result. It does not mutate; the GM
console requires explicit preview and then executes the original typed command
so the handler revalidates. The current GM console is an observer/GM surface
with access to the supplied world NPC/entity lists. It is not an actor-limited
client or an authorization boundary. `Origin` and `Authority` are supplied by
the command caller; current handlers do not authenticate a submitter or prove
that the submitter may control the referenced actor.

## Boundary map

The following describes current authority boundaries and the missing Phase 11
edges. `(?)` marks semantics not selected by this proposal.

```text
CURRENT AUTONOMOUS PATH
Actor Knowledge + current actor state
    → NpcDecisionSystem chooses an autonomous NpcActionRuntime
    → existing action/domain execution revalidates World Truth
    → domain outcome → Domain Event / History / UI

SELECTED HUMAN ACTOR-CHOICE SCOPE (technical path not yet defined)
Controller (?) + controlled actor identity (?)
    → actor-scoped decision context built from that actor's Knowledge
    → explicit choice/request at a defined logical boundary (?)
    → the same supported action/domain execution boundary
    → current World Truth validation → domain outcome → downstream records

CURRENT GM / EXTERNAL COMMAND PATH
Structured input or deterministic translation
    → typed WorldCommand → optional read-only Preview
    → Execute → per-kind authority check + current validation
    → existing domain-owned authority → outcome / Knowledge / Truth
    → command audit record and domain events where applicable
```

The selected actor path replaces the actor's autonomous *choice*. It must not
inject hidden facts into the choice context, directly write an outcome, or
skip the selected domain's execution checks. The GM/external path remains a
separate authority surface and is not part of this selected first-consumer
scope. Whether a later command track shares an envelope, adapter, or queue is
an implementation choice; it must not erase the semantic distinction.

## Selected first consumer and adjacent options

The selected first-consumer product scope is one actor-scoped choice of one
supported action in place of that actor's autonomous choice. Normal domain
execution remains authoritative and revalidates current World Truth. The
choice does not yet define who may submit it, which actor representations are
eligible, what candidates or targets can be seen, or when it is applied.

| Candidate | What it could prove | Constraints and dependencies |
|---|---|---|
| **Selected:** one actor-scoped choice of a supported action | Replaces that actor's autonomous choice for one decision while ordinary domain execution revalidates current World Truth. Existing `NpcActionData`, `NpcDecisionSystem`, and decision records are nearby seams, not an approved action contract. | Actor eligibility/materialization, controller authority, visible candidates/targets, the one-shot application boundary, and the exact action payload/contract remain open. `CreateRequestedAction` alone supplies none of the ingress, identity, information, ordering, or input-record contract. |
| GM/external `Request` through `ResolveConflict` or `PlaceOpposition` | Exercises the existing normal-resolver command path and its distinction from supported forced outcomes. | **Not selected** as the first consumer and not included in the selected actor-choice scope. Existing command support does not imply additional command, actor-control, or ForceOutcome permissions. |
| GM `Declare` through a supported existing command | Exercises a typed declared operation through its existing domain authority; Knowledge grants can change Knowledge without asserting underlying Truth. | **Not selected** as the first consumer or part of the selected actor-choice scope. Each command has distinct truth/knowledge semantics and capability; there is no blanket `Declare` support. |
| Travel/route intent chosen using actor Knowledge | Could later join actor choice, spatial perspective, and domain execution. | Not selected. Civil Travel is a soft ordering for Phase 11, not a hard dependency. Any future travel consumer waits for the exact promoted spatial/travel capability it uses. |

The first-consumer product choice is set. Architecture/technical entry design
must now define the bounded actor and action contract, preserve the actor's
Knowledge perspective, and bind execution to an available domain authority.
This selection does not authorize an implementation wave.

## Actor-limited information and authority implications

1. **Separate controller, actor, and authority.** A human/controller identity
   (if the product needs one), the in-world actor (`PersonId` where the actor is
   a Person), and the operation's domain authority are distinct. A command's
   `Origin` label is provenance, not proof of authorization. A GM may have an
   external authority that an in-world actor does not.
2. **Build choice context from actor Knowledge.** An actor's unknown or stale
   information stays unknown or stale. A controller may see more in an
   observer/GM surface only if product scope allows it; that observer view
   cannot silently become the actor's decision input. Knowledge gained from an
   explicit GM action remains a Knowledge mutation, not automatic factual
   discovery by the actor.
3. **Keep Person identity durable.** Any future persisted actor command should
   identify an individual through `PersonId` when that identity exists and
   specify the required materialized representation at execution. The design
   must resolve whether an unmaterialized Person can be controlled; it cannot
   treat lack of `NpcRuntime` as loss of Person identity or silently
   materialize/activate the actor as a side effect of input.
4. **Replace a decision, not execution truth.** Explicit choice may supply a
   bounded action/intent. Domain code still checks life/state, policy,
   capability, current targets, costs, and other operation preconditions at
   execution. If facts changed since preview/choice, reject or return the
   domain's supported result; do not expose hidden Truth as an automatic
   explanation or update to Knowledge.
5. **Do not widen command modes.** Preserve `Suggest` as preview-only,
   `Request` as subject to normal rules, `Declare` as a supported declared
   operation, and `ForceOutcome` only for constraints a domain explicitly
   supports. External/GM authority must not introduce arbitrary client
   mutation, generic force flags, or a second outcome resolver.
6. **Keep the decision record separate from the outcome record.** An input
   should be explainable as an input/decision with explicit acceptance or
   rejection semantics. The domain event/history remains downstream of a
   successful domain mutation and is not a substitute for capturing the
   causal input.

## Causal and replay-sensitive inputs

Authoritative determinism depends on the same initial state, effective
configuration/calendar/content, deterministic randomness, and the same
logically ordered external command sequence. A Phase 11 implementation that
claims durable continuation or later reconstruction must therefore make the
following causal facts recoverable in principle:

- the normalized typed input/payload and selected target(s), not only the
  human text or a command kind;
- stable actor/subject identity and any controller/principal provenance needed
  to interpret authority;
- origin, requested authority mode, command/decision identity, and semantic
  order at the application boundary;
- which logical day/boundary received, considered, applied, rejected, or
  deferred the input, and the defined acceptance/rejection result;
- the versioned action/content/definitions and domain policy needed to
  interpret the input, plus the authoritative facts and causal random context
  the supported domain execution consumed;
- any Knowledge observations or explicit knowledge injections relevant to
  the choice, including provenance and observed/received time where that
  domain stores them.

The present `WorldCommandRecord` is useful audit metadata but lacks payload and
logical queue order. The current allocator's local sequence does not alone
prove ordering across a save/load, fork, multiple ingress sources, or hosts.
Neither the command record store, diagnostics snapshots, events, nor History
currently provides Phase 12 save coverage or Phase 13 replay/fork coverage.
Exact storage, versioning, raw-input retention, and compatibility policy remain
open design choices; a Phase 11 proposal must not claim to solve those later
phases by adding a generic event log.

## Candidate checkpoint decomposition — UNAPPROVED

The units below are candidate design/implementation boundaries only. They have
no Phase 11 IDs, approval, or implementation authorization.

| Candidate unit (UNAPPROVED) | Candidate closure evidence | Dependencies / ordering |
|---|---|---|
| Actor eligibility, perspective, and one-choice contract | Defines actor eligibility/materialization, controller authority, visible candidate/target projection, exact supported-action contract, and explicit exclusions for the selected scope. | The first-consumer product choice is set; these technical and remaining product details must be resolved before implementation. |
| Logical input boundary and envelope contract | Defines capture/application boundary, deterministic order, stale-state behavior, identity/provenance, and accepted/rejected semantics without granting new domain mutation power. | Depends on the selected consumer and its actor identity. Requires explicit coexistence semantics with scheduled directives, autonomous selection, and any external command source. |
| Consumer adapter through existing domain authority | A bounded request/choice reaches one selected domain path, revalidates current World Truth, and records no fabricated result. | Requires the exact promoted domain capability and a contract for actor Knowledge access. No blanket P8/P9 or command-handler dependency. |
| Causal-input recording and integration | Captures enough normalized causal input and logical application order for its stated guarantee, with deterministic behavior across equivalent runs. | Integrates command/input capture, runtime boundary, domain records, and relevant diagnostics. Must be explicit about what Phase 12/13 still need. |
| Phase-specific regression and acceptance review | Tests privacy-of-perspective, command authority, stale truth, deterministic ordering, and domain invariants for the approved consumer. | Follows implementation and independent review; test scope depends on whether daily-loop ordering or long-horizon behavior changes. |

Potential parallelism is limited to isolated discovery/design of consumer
contracts. Any implementation touching `WorldCommandFoundation`, command
handlers, Knowledge authorities, `SimulationRuntime`/`AdvanceDay`, or shared
domain stores requires explicit ownership and isolated worktrees. The selected
command/runtime integration should be ordered deliberately; it is not a
reason to introduce a generic concurrency lock or universal actor framework.

## Dependencies and scheduling implications

- Phase 11 entry design can proceed independently of Phase 8 world generation.
  An implementation can proceed only when its selected consumer's actual
  domain authority is available and promoted; registering a provider or
  handler is not enough.
- Phase 11 has hard semantic dependencies on Person/Knowledge/decision versus
  execution contracts and on the existing `Request`/`Declare`/supported
  `ForceOutcome` distinctions. These contracts do not establish a universal
  actor model or command permission matrix.
- Any actor-information query depends on the specific Knowledge model being
  consumed. There is no current general actor Knowledge projection covering
  all domains.
- Any live input integration depends on a selected logical application
  boundary that coexists with autonomous and scheduled processing. If
  `AdvanceDay` ordering or long-horizon effects change, the corresponding
  runtime regression and long-run validation gates apply.
- Phase 12 continuation benefits from stable causal input semantics, but
  Phase 11 is not a substitute for Phase 12's full authoritative save scope.
  Phase 13 needs both continuation and reconstructible inputs/mutations; a
  Phase 11 command audit record alone cannot satisfy it.
- Civil travel is a possible later consumer, not a blanket prerequisite. A
  selected travel slice waits for the relevant promoted Phase 8 authority only.

## Unresolved choices for review

### Product choices

- Which actor is eligible for the selected one-action choice, and can an
  unmaterialized Person be eligible?
- Which action candidates and targets may the actor-scoped view expose? Is any
  GM/observer assistance allowed while preserving the actor's Knowledge
  boundary?
- Who may submit the choice for that actor, and what product-level controller
  authority is intended? No ongoing actor-control policy was selected.
- GM/external `WorldCommand` operations are not in the selected first-consumer
  scope. Whether a separate command track belongs in a later Phase 11 slice
  remains undecided; no existing command kind is implicitly exposed.

### Architecture and technical choices

- How does a controller identity bind to durable actor identity, and which
  layer authenticates it? How are conflicting controllers or stale control
  grants handled if those concepts are in product scope?
- Is actor choice represented as a separate input type, a constrained
  `WorldCommand`, or an adapter to a domain-specific request? The answer must
  preserve actor choice versus GM declaration and domain outcome.
- What is the exact supported-action contract: which stable action identity,
  selected target/arguments, and actor-scoped candidate source are captured?
  Existing `NpcActionRuntime` / `CreateRequestedAction` is not assumed to be
  the contract.
- At what logical boundary are inputs accepted and ordered relative to
  scheduled directives and autonomous action choice, and how is this one-shot
  choice consumed? What happens to late, duplicate, stale, rejected, or
  deferred inputs?
- Which normalized fields and principal/order data are included in a future
  causal record, and what guarantee belongs to Phase 11 versus Phase 12/13?
  How are failed requests and replay-sensitive rejection semantics captured?
- Does any new `Request` or `ForceOutcome` support actually have a domain-owned
  implementation? The default answer is no until the selected domain defines
  and validates that support.
- What actor Knowledge projection does the first consumer require, and can it
  avoid exposing world registries/world snapshots to the decision layer?

These questions must be resolved or explicitly scoped out before a
track-specific technical design is accepted. No semantic answer or product
permission is inferred by this proposal.
