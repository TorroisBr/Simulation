# Phase 11 Entry Architecture Proposal

> **Status: product scope resolved; entry proposal independently reviewed
> PASS; technical design remains.** The user-selected first consumer and
> trusted local caller are not implementation approval. This proposal does
> not amend the Simulation Architecture, Roadmap, Phase Brief, or Phase State.
> No Phase 11 checkpoint IDs are approved or assigned.

## Purpose and evidence boundary

Phase 11's Brief describes a bounded actor-control and external-command slice.
The user selected the first consumer: **one actor-scoped choice of a local
`SellGoods` action, with candidate planning based on that actor's
`CommercialKnowledge` and existing domain execution revalidating current
world state.** The choice replaces that actor's autonomous action choice for
one decision. The selected slice is the actor's existing local-market
`SellGoods` behavior at its current city. It does not include active-plan
sales, which can use current state from other NPCs during candidate planning;
remote trade travel, a new sale outcome path, hidden knowledge, broader
ongoing control, and additional GM/external interventions are also excluded. The
user selected a trusted single-player game/UI caller, with no player-to-actor
ownership, control grants, authentication, anti-cheat, anti-tamper, or security
validation layer. The supported actor is a living, materialized,
Person-backed merchant `NpcRuntime` with a current city and no active
`MerchantTradePlanRuntime`, matching the existing `CommercialKnowledge` owner
and local-market `SellGoods` path. The UI chooses the supported `SellGoods`
action only; the existing merchant planner selects its candidate and quantity
from that actor's Knowledge and inventory. Current market facts remain
execution-time validation. These choices bound the entry
contract; routine type names and storage/API shapes remain technical design
work. The Brief marks the phase `ENTRY_ARCHITECTURE_READY`; this update does
not itself authorize implementation.

This proposal is based on assigned baseline `1f4651e99db2c357dd3be3c6b9284d104379f706`
in the isolated `codex/phase11/ActorCommandsEntryArchitecture` worktree. The
Phase 5, 6, and 7 State documents report those phases closed. There is no
Phase 11 State document or approved Phase 11 checkpoint decomposition in this
checkout. Current `docs/PHASE8_STATE.md` at `codex/phase8/canonical` commit
`ed7a40a86a6a16e9f4fda75703470c38135fda0e` records P8-A through P8-C as
canonical, P8-D as ready to implement, and P8-E waiting on D. This local-market
sale needs no travel capability.

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
- A trusted game/UI caller's ability to select an actor action does not grant
  omniscient information or authority to decide the resulting domain outcome.
  An in-world actor, the local single-player UI, a GM, and an external
  controller remain distinct roles. This first consumer adds no controller
  identity or authorization model.
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
does not itself provide the selected action-choice input or its causal record.
The selected local UI is trusted; no controller-to-actor grant or security
check is added. The runtime's scheduled `RequestAction` and narrow
`ForceOutcome` directive path are separate from this actor-choice input. The
trusted UI choice is a distinct semantic input from GM/external declarations,
but its capture enters through a dedicated typed `WorldCommand`/domain
boundary as required by the Phase 11 Brief; it queues a choice rather than
applying a sale. In current `SimulationRuntime` order, travel and
reserved-activity checks and scheduled directives can bypass ordinary
autonomous evaluation. A queued actor choice is consumed only at the same
normal `EvaluateAction` boundary, immediately before autonomous
`ChooseAction`, so those existing higher-priority paths keep their behavior.

For the selected consumer, the existing local-sale path provides a concrete
domain seam. `MerchantSystem.CreateSellGoodsAction` builds a local sale from
the merchant's inventory and current-city `CommercialKnowledge` observation,
including remembered price/freshness, then selects a sale quantity. This
Knowledge-bounded path applies when the actor has no active trade plan. An
active plan may select a local NPC buyer by reading other NPCs' current state
and funds during planning, so that mode is outside this first consumer. The
ordinary market execution path delegates to the existing transaction service
against the actor's current-city market; a stale candidate can fail or
partially fill against current market truth. The current provider constructs a
best sale, not a general actor-facing list of typed sale candidates, and its
runtime action contains content/runtime references. This is evidence for the
consumer, not an approved candidate-list or input-payload contract. This
current-city market sale requires no travel capability.

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

SELECTED ACTOR-CHOICE SCOPE
Trusted local game/UI caller
    → dedicated typed WorldCommand payload (PersonId + action DefinitionId)
    → capture the choice in the actor-input owner at the domain boundary
    → at that actor's next normal EvaluateAction boundary, construct the
      existing local sale using that actor's inventory and CommercialKnowledge
    → existing MerchantSystem selects a local-market candidate and amount
    → current market World Truth validation
    → transaction result / records

CURRENT GM / EXTERNAL COMMAND PATH
Structured input or deterministic translation
    → typed WorldCommand → optional read-only Preview
    → Execute → per-kind authority check + current validation
    → existing domain-owned authority → outcome / Knowledge / Truth
    → command audit record and domain events where applicable
```

The selected actor path replaces the actor's autonomous *choice*. It uses the
actor's `CommercialKnowledge` and existing merchant planner to build one local
sale action; the human selects the action, not an item, quantity, price, or
transaction result. It must not present current market truth as remembered
knowledge, directly write an outcome, or skip the normal action and
domain checks. The GM/external path remains a separate authority surface and is not
part of this selected first-consumer scope. The shared `WorldCommand` ingress
does not give this actor choice GM authority or authorize a world mutation;
its dedicated payload records a request that the existing actor/domain path
may apply at the next decision boundary. No authentication or defensive
validation for forged inputs is added; invalid inputs outside the normal UI
flow are outside the supported contract. Its command identity remains
separate from actor-action acceptance and domain outcome records.

## Selected first consumer and adjacent options

The selected first-consumer product scope is one actor-scoped choice of the
existing local-market `SellGoods` action in place of that actor's autonomous
choice. Candidate planning uses the actor's `CommercialKnowledge`; ordinary
merchant execution remains authoritative and revalidates current market
truth. An actor with an active `MerchantTradePlanRuntime` is outside this
consumer, because its existing planner may use other NPCs' current state and
funds. This scope does not include remote trade travel or mutation by the input
layer. The supported actor is a living, Person-backed, materialized merchant
with a current city and no active trade plan. The trusted single-player UI
submits only the selected supported action identity. Existing merchant
planning chooses its local-market candidate and quantity; no current market
truth is added to the actor-facing view. The choice is consumed at the actor's
next normal `EvaluateAction` boundary, before autonomous selection; existing
travel/reservation and scheduled-directive precedence remains intact.

| Candidate | What it could prove | Constraints and dependencies |
|---|---|---|
| **Selected:** one actor-scoped local-market `SellGoods` choice | Replaces that actor's autonomous action choice for one decision. The trusted local UI selects the action for a living Person-backed materialized merchant with a current city and no active trade plan. Existing `MerchantSystem` plans the local-market sale from that actor's inventory and `CommercialKnowledge`; execution revalidates current market truth. | The input identifies `PersonId` and the supported action definition, not item/quantity or outcome. It is consumed at the next normal `EvaluateAction` boundary, after existing scheduled directives and activity exclusions. No controller grant/auth/security layer or travel capability is added. `CreateRequestedAction` alone still supplies no causal input record. |
| GM/external `Request` through `ResolveConflict` or `PlaceOpposition` | Exercises the existing normal-resolver command path and its distinction from supported forced outcomes. | **Not selected** as the first consumer and not included in the selected actor-choice scope. Existing command support does not imply additional command, actor-control, or ForceOutcome permissions. |
| GM `Declare` through a supported existing command | Exercises a typed declared operation through its existing domain authority; Knowledge grants can change Knowledge without asserting underlying Truth. | **Not selected** as the first consumer or part of the selected actor-choice scope. Each command has distinct truth/knowledge semantics and capability; there is no blanket `Declare` support. |
| Remote trade, travel, or route intent | Could later join actor choice, spatial perspective, and domain execution. | Not part of the selected local-market sale. Civil Travel is a soft ordering for Phase 11, not a dependency for this consumer; a later travel consumer waits for the exact promoted spatial/travel capability it uses. |

The first-consumer action, trusted caller, eligible actor shape, actor-limited
view, and normal-turn application point are now bounded. A technical design
must map this contract onto a small causal input representation and existing
runtime/action seams. This selection does not authorize a security framework,
a GM command expansion, or any new market outcome authority.

## Actor-limited information and authority implications

1. **Separate caller, actor, and domain authority.** The trusted game/UI caller,
   the in-world actor (`PersonId`), and the operation's domain authority are
   distinct. The caller is trusted by the single-player product contract; there
   is no controller principal, actor ownership grant, or authentication layer.
   A command's `Origin` label is provenance, not proof of authorization. A GM
   may have an external authority that an in-world actor does not.
2. **Build choice context from actor Knowledge.** An actor's unknown or stale
   information stays unknown or stale. For the selected sale, remembered
   prices, stock, and liquidity estimates come from that merchant's
   `CommercialKnowledge`; current market state is consulted for domain
   execution, not substituted into the candidate's Knowledge view. The
   actor-facing choice does not include observer/GM assistance or market-truth
   enrichment. Knowledge gained from an explicit GM action remains a Knowledge
   mutation, not automatic factual discovery by the actor.
3. **Keep Person identity durable.** The choice identifies the actor through
   `PersonId` and resolves the currently materialized `NpcRuntime` at the
   normal decision boundary. An unmaterialized Person is not eligible for this
   `SellGoods` consumer because it has no current merchant runtime or
   `CommercialKnowledge`; the input must not materialize or activate a Person.
4. **Replace a decision, not execution truth.** Explicit choice supplies only
   the supported `SellGoods` action identity. The existing merchant planner
   selects its sale from the actor's inventory and Knowledge. Domain code still
   checks life/state,
   policy, capability, seller inventory, current price and market capacity,
   counterparty funds for account-backed markets or the existing
   explicit-source money path for open markets, and other operation
   preconditions at execution. If
   facts changed since action choice, return the transaction's supported
   result; do not expose hidden Truth as an automatic explanation or update to
   Knowledge. Do not add validation whose purpose is to defend against a
   manually forged input outside the supported game/UI flow.
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

- the normalized typed actor choice: stable `PersonId` and supported action
  definition identity, not only human text or a command kind;
- no item, quantity, price, or market outcome is selected by the UI. Existing
  `MerchantSystem` chooses the local sale from inventory and Knowledge, and
  the normal transaction result records the resulting material/money effect;
- the trusted local game/UI input origin and the logical application boundary;
  there is no controller/principal identity or authorization grant in scope;
- deterministic actor-turn order at application. The existing daily roster
  iteration and scheduled-directive/activity precedence remain the ordering
  authority; a choice does not reorder other actors' transactions;
- which logical day/boundary received, considered, applied, rejected, or
  deferred the input, and the defined acceptance/rejection result;
- the versioned action/content/definitions and domain policy needed to
  interpret the input, plus the authoritative facts and causal random context
  the supported domain execution consumed;
- any Knowledge observations or explicit knowledge injections relevant to
  the choice, including provenance and observed/received time where that
  domain stores them.

The present `WorldCommandRecord` is useful audit metadata but lacks payload and
logical queue order. The actor choice remains semantically distinct from
GM/external authority operations, while its trusted UI ingress uses the
required `WorldCommand`/domain boundary with a dedicated typed payload. Its
normalized selection, queue order, application boundary, and result must be
recoverable without treating diagnostic/history output as authority. The
current allocator's local sequence does not alone prove ordering across a
save/load or fork. Exact schema/versioning remains Phase 12/13 work; Phase 11
must not claim to solve those phases by adding a generic event log.

## Candidate checkpoint decomposition — UNAPPROVED

The units below are candidate design/implementation boundaries only. They have
no Phase 11 IDs, approval, or implementation authorization.

| Candidate unit (UNAPPROVED) | Candidate closure evidence | Dependencies / ordering |
|---|---|---|
| Actor eligibility, perspective, and one-choice contract | Use the supported configured `SellGoods` action for a living Person-backed materialized `NpcRuntime` with a current city and no active trade plan; actor knowledge remains owned by that runtime. The UI chooses the action, while MerchantSystem chooses the local-market candidate and quantity. | Product scope is selected. Preserve the existing current-city merchant/action requirements; do not add a broader actor model or controller grants. |
| Logical input boundary and envelope contract | Capture a normalized `PersonId` plus `NpcActionData.DefinitionId` through a dedicated typed `WorldCommand`/domain ingress; consume it at the next ordinary `EvaluateAction` before autonomous choice. Existing scheduled directives and activity exclusions retain their current precedence. | Use the normal actor-turn loop order. A choice unavailable at application is recorded as rejected and autonomous selection proceeds; a created action is attempted once and its normal domain result is returned. The new payload does not reuse GM `Declare` or `ForceOutcome` authority. |
| Consumer adapter through existing domain authority | A bounded choice reaches the existing local-market `SellGoods` path through `MerchantSystem` and the current market transaction service, revalidates current market truth, and records no fabricated result. | Requires the local merchant/action composition and a contract for actor `CommercialKnowledge` access. No travel capability, blanket P8/P9 dependency, or new GM command authority is selected. |
| Causal-input recording and integration | Captures the selected actor/action, decision boundary, and applied/rejected disposition separately from the sale outcome. The pending choice is authoritative future input. | Integrates the actor-choice input owner, runtime decision boundary, decision records and deterministic diagnostics. Phase 12/13 still own full continuation/reconstruction formats. |
| Phase-specific regression and acceptance review | Tests Knowledge-bounded action planning, ordinary current-truth revalidation, one-shot use, scheduled-directive precedence, deterministic actor order, and unchanged GM command semantics. | Follows implementation and independent review; long-run validation is required only if daily-loop or long-horizon behavior changes. |

Implementation touching `WorldCommandFoundation`, `NpcDecisionSystem`,
`SimulationRuntime`/`EvaluateAction`, decision records, Knowledge, or merchant
transaction composition needs explicit file ownership and an isolated
worktree. The actor-choice payload/handler remains semantically distinct from
GM command handlers and authority modes; it does not justify a generic
concurrency lock or universal actor framework.

## Dependencies and scheduling implications

- Phase 11 entry design can proceed independently of Phase 8 world generation.
  The selected current-city local sale uses the existing merchant and
  transaction execution paths and does not require travel. Registering a
  provider or handler alone still does not establish domain availability or
  enablement.
- Phase 11 has hard semantic dependencies on Person/Knowledge/decision versus
  execution contracts and on the existing `Request`/`Declare`/supported
  `ForceOutcome` distinctions. These contracts do not establish a universal
  actor model or command permission matrix.
- Any actor-information query depends on the specific Knowledge model being
  consumed. The selected sale uses `CommercialKnowledge`, currently owned by
  the `NpcRuntime` representation; there is no current general actor Knowledge
  projection covering all domains.
- Any live input integration depends on a selected logical application
  boundary that coexists with autonomous and scheduled processing. If
  `AdvanceDay` ordering or long-horizon effects change, the corresponding
  runtime regression and long-run validation gates apply.
- Phase 12 continuation benefits from stable causal input semantics, but
  Phase 11 is not a substitute for Phase 12's full authoritative save scope.
  Phase 13 needs both continuation and reconstructible inputs/mutations; a
  Phase 11 command audit record alone cannot satisfy it.
- Civil travel is not a dependency for this local sale. Any later travel or
  remote-trade consumer waits for the relevant promoted Phase 8 authority only.

## Resolved entry boundary and remaining technical work

The first actor-choice consumer has no remaining product gate. Its supported
contract is:

- The trusted local single-player UI submits one supported action choice for a
  living, Person-backed, materialized actor currently eligible for the
  configured local-market `SellGoods` path and with no active trade plan. There is no controller identity,
  ownership grant, authentication, anti-cheat, anti-tamper, or security layer.
- The normalized choice carries the actor's `PersonId` and the selected
  `NpcActionData.DefinitionId`. The user chooses the action only; the existing
  `MerchantSystem` plans the local-market candidate and quantity from the
  actor's inventory and `CommercialKnowledge`.
- The choice is consumed at that actor's next ordinary `EvaluateAction`
  boundary immediately before autonomous `ChooseAction`. Travel/reservation
  exclusions and scheduled-directive precedence remain unchanged. Existing
  actor iteration order continues to determine ordering between actors.
- If the selected action is no longer supported by normal gameplay state when
  applied, including when the actor has an active trade plan, record its
  rejection and let autonomous selection proceed. If the action is
  constructed, attempt it once through the existing action and transaction
  path; stale market truth produces the ordinary domain result, with no
  automatic retry and no Knowledge rewrite.
- GM/external `WorldCommand` behavior remains on its existing validated path
  and is not widened by this actor-choice consumer. Phase 12/13 own later
  save-schema and full historical reconstruction guarantees.

The next work is technical: implement the small normalized input owner and
runtime seam, include pending and applied/rejected input in deterministic
diagnostics, and add focused ordering, Knowledge-perspective, stale-market,
and no-regression coverage. Type/member naming and internal store shape are
ordinary implementation choices; they do not reopen the selected semantics.
