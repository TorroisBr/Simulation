# P12-F — Knowledge, Directives, Actor Choices, and Active Commitments

**Status:** Documentation-only technical proposal for independent exact-tip
review. This file defines no implementation readiness and does not deliver
owner export or staged hydration. P12-A remains `WAIT_DEPENDENCY`; its separate
profile implementation authorization remains outstanding.

**Design base:** P12 planning commit `4c384ab916b44e4df8eb576fb98a88c3fac526b2`.
The proposal follows the accepted P12-B–G decomposition and P12-A
`UnityBootstrap-Daily-v1` contract. P12-C identity/genesis/deterministic roots,
P12-D factual roots, and P12-E selected core/daily owners are prerequisites to
F. Their designs are inputs, not evidence that their capabilities have been
implemented or promoted.

**Related reviewed design inputs:** P12-C refresh
`f30a73fd7d88721dcb1774e8f6615d59fd70c671` (owner census
`99739fd0d190cd61acda8d3c91b25acab7405379`; independent review pending at
proposal time); P12-D `796eadc1a0bd6b44b646f27f5eeb63de71f6b14`; P12-E
`51718c7bd38e9582433d1fa38ff4011ef9cf20c5`. P12-C/D/E design review passes
do not substitute for delivery of those capabilities. The design also applies
the current intraday/extensibility and multi-participant alignment records:
P18/P19/P20 constraints remain explicit while their state is excluded from
this daily profile.

## 1. Boundary

P12-F owns exact causal Knowledge, scheduled directives, P11 actor-choice
terminal history, and only those active commitment facts proven present in the
accepted profile's refreshed live owner inventory. Every section is an
immutable value export from its domain authority and has a private staged
hydrator. F does not define a save envelope, capture eligibility, whole-graph
publication, or continuation parity coordinator; P12-B and P12-G own those
boundaries.

The selected profile is the normal `UnityBootstrap-Daily-v1` composition.
P12-F does not add an external `WorldCommand` service or queue. The default
P11 `ActorChoiceStore` is included because the selected P11 runtime composition
constructs it. A selected profile with an external command queue is unsupported
and must be rejected by admission, not represented as an empty F section.

P12-F must not duplicate facts owned by C, D, or E. In particular, the single
`NpcRuntime` value projection spans D factual identity/state, E-provider-written
outcomes, and F Knowledge/commitment values. Its owner takes one immutable
snapshot and exposes disjoint semantic slices under the same identity and
revision; slices are merged before one private hydrator reconstructs that
runtime. Do not add a second NPC hydrator or independently recapture the owner
for F.

## 2. Owner sections

The section list below is a contract for the refreshed profile inventory, not
a claim that every named authority is currently populated, complete, or
implemented for continuation. Each present owner reports exact identity,
schema, revision, cardinality (including zero), and immutable values. A required
owner with zero facts has an explicit empty section. Missing or unknown owner
coverage is not empty state and blocks F completeness.

### 2.1 Knowledge

Capture all Knowledge authorities actually composed by the admitted profile,
including `PoliticalKnowledgeStore`, commercial Knowledge-sharing state, and
the Knowledge-bearing fields in the shared `NpcRuntime` projection (spatial,
commercial, and exploration observations where present). The refreshed
inventory determines the exact owner set; do not infer completeness from one
Knowledge store or a diagnostic snapshot.

Each store-owned observation preserves its stable holder/subject identities,
typed observed fact, source and provenance, observation/receipt boundary,
freshness/staleness inputs, and owner-defined causal revision. NPC Knowledge
values remain in the one `NpcRuntime` snapshot assigned to F; overlapping
provider outcomes or facts assigned to E are not copied into a second section.
Capture observations as they existed at T. Do not regenerate them from current
truth, current definitions, or current Knowledge policy.

Private hydration restores the exact values and owner revisions, rebuilding
only indexes explicitly derived by that owner. It validates references to C/D
identity and world roots and returns unresolved cross-section bindings to G
when the target belongs to E. A malformed, unsupported, dangling, duplicated,
or incompatible observation rejects the staged F candidate; it is never
silently dropped or refreshed.

### 2.2 Scheduled directives

`ScheduledDirectiveStore` is a required F owner because the selected bootstrap
composes it. Its immutable section preserves each directive's stable ID,
target day, mode/operation, actor and action-definition references, causal
ordering/sequence owned by the directive authority, processing state and
terminal disposition, plus the exact owner revision and cardinality.

Restore directive records without invoking `ScheduledDirectiveSystem`,
advancing time, dispatching an action, or consuming an already processed
directive. Validate directive IDs and sequence/order invariants, actor/action
references against staged roots and admitted content, and state/disposition
consistency. Definitions and executable services come from the admitted
compatible build/content; serialized data does not carry delegates or service
objects. Empty and populated stores both round-trip. Unknown states or
unsupported definitions reject before publication.

### 2.3 P11 ActorChoiceStore

Preserve the complete `ActorChoiceStore` history, not only pending inputs or a
diagnostic projection. Each value includes the `ActorChoiceInputId`, original
`WorldCommandId`, input sequence, `PersonId`, action definition, origin,
authority mode, capture day, final status, and the ordered immutable
disposition history with transition ordinal, day, actor-turn roster ordinal,
optional opaque `DecisionRecordId` correlation, deferral/failure reason, and
attempt outcome or returned result status as applicable. Preserve the exact next input sequence
and the store's complete command-ID idempotency set (which must agree exactly
with retained input history).

`DecisionRecordId` is an optional retained correlation string, not a foreign
key. Preserve its exact nullable value; do not require a matching
`NpcDecisionStore` row or validate its existence during hydration. The
ActorChoice owner permits null, and runtime does not resolve this value.

Capturable states are the terminal `Rejected`, `AttemptReturned`, and
`AttemptThrew` records, including their complete preceding dispositions. A
thrown attempt can be captured only at a later successful daily boundary while
the runtime remains healthy, as required by P11/P12-A. Reject `Pending`
(including deferred choices) and `ConsumedAwaitingTerminalAttempt`; neither
may be resumed, retried, or completed by hydration. Preserve terminal records
without calling the actor-choice processor or replaying the original command.

The private staged factory rebuilds only owner indexes (`InputId` lookup and
the duplicate-command-ID set) from validated immutable records and restores
the exact next sequence without allocating a new input. It verifies unique
input and command IDs, strictly increasing positive input sequences,
next-sequence greater than all retained inputs, contiguous disposition
ordinals, legal lifecycle transitions, nondecreasing causal boundaries,
terminal-status consistency, and valid C/D identity references for actual
typed IDs such as `PersonId`. Optional opaque decision correlations are
preserved as values and do not add a graph edge. Any `Pending`/in-flight
record, inconsistent duplicate index, or unsupported enum rejects the entire
staged ActorChoice section. P11's
trusted normal game/UI input contract remains unchanged; F introduces no
control grants, ownership checks, anti-cheat boundary, or adversarial-command
model.

### 2.4 Active commitments, inventory-driven

The candidate owner inventory identifies legacy `TravelPartyStore`,
`ExpeditionStore`, travel/expedition progress and services, plus current action
and merchant-plan commitment values held by `NpcRuntime`. `MerchantSystem`
behavior belongs to E, while its active plan values belong to F. These are
conditional owner sections: before implementation, refresh the accepted
profile's actual composition and map each active commitment fact to exactly
one existing owner. Include only proven-in-profile owner instances; do not
create new stores or assume every listed service retains mutable continuation
state.

For each included owner, export the exact stable IDs and owner revision,
actor/party/member bindings, current lifecycle/progress, incurred or reserved
cost facts owned there, target/site/market references, and reciprocal links
required to resume that existing commitment. Preserve explicit empty sections
for composed owners with zero commitment records. Where an owner is not
composed, record its typed absent/disabled status in admission evidence; where
composition or cardinality is unknown, reject profile admission rather than
omitting it.

Do not serialize derived plans as if they were domain authority. Include
plan/commitment values only where their current owner retains them as causal
state (including the F slice of `NpcRuntime`); reconstruct only indexes or
service references expressly derived by that owner. Preserve any optional
`OriginDecisionId` as an exact nullable opaque correlation string; it is not a
foreign key and does not require a corresponding decision row. Validate local identity,
membership/cardinality, lifecycle and reciprocal references against C/D roots,
and emit E-owned unresolved market/provider bindings for P12-G. Hydration
restores the exact current commitment without choosing, replanning,
rescheduling, charging, transferring, moving, consuming, or applying its
effects again. Existing domain owners remain responsible for later execution
and current-truth validation.

P18 timeline/work/availability/receipt state and P20 shared activity instances,
participant commitments, reservations, and cardinality are outside this daily
profile and must remain absent. Do not map a future P20 shared commitment into
an NPC-owned F field or infer one-actor cardinality for a shared activity.

## 3. Staging and graph order

P12-G owns the complete private composition and final publication. F supplies
owner-local immutable packages and private owner candidates in the following
dependency order:

1. Require a P12-B admitted profile/boundary witness and C identity,
   deterministic-root, and shared-sequence packages; require D staged Person,
   NPC, City, and legacy spatial roots; require E staged core/provider roots
   needed by any F cross-reference. F does not recreate these roots.
2. Validate every F section's presence/absence against the refreshed live
   inventory, exact owner identity/schema/revision/cardinality, profile
   compatibility, and stable typed IDs before constructing private owner
   candidates.
3. Stage Knowledge values against C/D identities and available D/E facts;
   unresolved E references are reported as typed bindings for G. Do not
   derive Knowledge from those facts.
4. Stage `ScheduledDirectiveStore` records against the staged actors and
   admitted action definitions, without processing due directives.
5. Stage terminal ActorChoice records after Person roots exist, validating
   identity/history and shared causal sequence references without dispatching
   a choice.
6. Stage each inventoried active commitment owner after its actor/party/
   expedition roots and any relevant D/E target roots exist. Validate local
   reciprocity now and return remaining cross-section bindings for G.
7. Return a typed F package and validation evidence. P12-G resolves all D/E/F
   bindings, checks global identity/sequence/cardinality invariants and
   explicit empty/excluded sections, validates the complete graph, then
   publishes once. Any F failure discards the private F candidates and leaves
   the active runtime and all its owners unchanged.

The concrete constructor order follows actual owner references, not this
semantic list when an owner dependency requires a different sequence. No
candidate is bound to the active mutation guard or published by F.

### 3.1 Omitted noncausal read models

`NpcDecisionStore` and `DomainEventStore` are classified as
`OmittedNonCausalReadModel` for this profile: populated decision/event history
is not included in continuation state. `HistoryStore` is a subset of
event/history data and is omitted with it; `NpcChronicle` is derived. P12-F
does not promise history or chronicle UI parity. These classifications do not
remove causal state owned by F: ActorChoice terminal receipts and directive or
commitment owner truth remain included according to their contracts. An
optional opaque correlation string is preserved as-is and is not required to
resolve into an omitted read-model row.

## 4. Rejection and no-replay rules

Reject missing required owners, unknown owner/schema versions, unknown
nonempty sections, unsupported populated owners, incomplete owner inventory,
duplicate or dangling IDs, wrong-kind references, contradictory reciprocal
links, invalid revisions/cardinalities, unsupported content/provider identity,
or a section whose exact export cannot be made coherent at the admitted
boundary. Explicit zero is different from missing evidence.

Reject external `WorldCommand` queue/service composition, pending/deferred or
`ConsumedAwaitingTerminalAttempt` actor choices, and any P18/P20 state in this
profile. Do not include P10 topology, P14 productive-material state, P19
module state, P13 fork guarantees, or later gameplay. Diagnostic snapshots,
event/history records, transaction rollback clones, Unity asset serialization,
and runtime clones are not substitutes for owner exports/hydrators.

Successful hydration performs data reconstruction only. It must not replay
commands or directives; retry actor attempts; regenerate Knowledge; run travel,
merchant, expedition, or other plans; recreate activity lifecycle; apply
domain effects; or allocate replacement IDs/sequences. Normal domain
authorities execute future work after P12-G publishes the restored runtime.

## 5. Readiness and completion criteria

This design is a proposal only. F implementation may be scheduled only after
this exact contract passes independent review and P12-C/D/E dependencies are
delivered/promoted or a reviewed, stable owner interface is available for an
explicitly isolated implementation. The owner inventory must be refreshed
against the accepted profile's live canonical composition and prove every
included/conditional F owner, exact cardinality, revision source, and supported
mutation path. It must resolve the shared `NpcRuntime` D/E/F snapshot and
single-hydrator seam. An unenumerated owner or writable bypass is a blocker,
not authority to add a guessed schema.

F owner implementation is complete only when each included authority has an
exact immutable export, a private staged hydrator, complete owner-local
invariants and mutation/revision coverage, empty and populated round trips,
corruption/rejection tests that leave source state unchanged, and evidence
that no work or effects are replayed. Cross-owner unresolved bindings must be
consumed by G's whole-graph validation. This is a prerequisite capability
completion criterion, not P12-A readiness or Phase 12 closure.

## 6. Required implementation tests (future; none run for this proposal)

- Knowledge: empty and populated owner round trips preserve provenance,
  observation boundaries, freshness and revisions; missing/duplicate/dangling
  references reject; changed current truth does not regenerate saved
  observations.
- Directives: empty/populated pending and terminal-state round trips preserve
  IDs/order/disposition and next processing behavior; hydration invokes no
  directive action; missing action/actor, malformed lifecycle and unsupported
  schema reject with source and staged target unchanged.
- ActorChoice: preserve each allowed terminal status and all disposition
  variants, exact order, command-ID history, next sequence, and identical
  duplicate-command handling after restore. Reject every in-flight state,
  duplicate/missing IDs, non-increasing or nonpositive input sequences,
  disposition transition ordinals that are not contiguous from one, illegal
  transitions, invalid terminal state, a next input sequence that does not
  exceed all retained inputs, overflow, and broken typed Person references
  without changing the source. Opaque optional `DecisionRecordId` values,
  including null and strings without a retained decision row, round-trip
  exactly and do not create a relationship-validation failure. Input sequence
  values must be strictly increasing and unique, but need not be gap-free.
- Commitments: for each owner proven in the refreshed profile, empty and
  active fixtures round-trip stable identities, progress, costs and reciprocal
  links; next normal domain execution matches uninterrupted execution without
  replayed planning or effects. Optional opaque `OriginDecisionId` values,
  including null and strings without a retained decision row, round-trip
  exactly without target-existence validation. Corrupt/dangling/duplicate/
  contradictory typed references reject. Do not substitute P20 activity tests
  or infer universal participant counts.
- Shared NPC owner: D/E/F values are exported from one immutable snapshot and
  revision, merged once, and hydrated by one staged NpcRuntime factory;
  duplicated fields, mismatched snapshot IDs/revisions, or mutation during
  capture reject.
- Composition/rejection: absent versus explicit-empty distinction is tested;
  missing/unknown included owners, external WorldCommand queue, excluded
  P18/P20 state, unsupported content/provider versions, and incomplete live
  inventory reject before publication. Failures preserve the active runtime.
- On implementation integration, run relevant Knowledge, directives,
  ActorChoice, travel/party/expedition, merchant and affected D/E suites, ALL
  EditMode, the complete official Smoke suite, and `git diff --check` under the
  repository validation policy. P12-G owns whole-profile continuation parity.

No tests or implementation validation are claimed by this document.

## 7. Explicit exclusions

P12-F does not implement P12-A save/load integration, storage/envelope or
migration format, P12-G graph publication/parity, external command queues,
actor control grants/security, P18 intraday continuation, P19 loader/module
state, P20 shared activities, P13 history/fork guarantees, P10 topology, P14
material flow, new gameplay, or a new commitment/planning framework. The
alignment constraints on extensibility and shared activity identity/cardinality
remain review constraints without adding those deferred capabilities to this
profile.
