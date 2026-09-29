# P12-F — Knowledge, Directives, Actor Choices, and Active Commitments

**Status:** Documentation-only technical proposal. This design does not
deliver a capability, owner export, or staged hydration and establishes no
implementation readiness. P12-A remains `WAIT_DEPENDENCY`; its separate
profile implementation authorization remains outstanding. Review evidence is
tracked outside this design file.

**Design base:** P12 planning commit `4c384ab916b44e4df8eb576fb98a88c3fac526b2`.
The proposal follows the accepted P12-B–G decomposition and P12-A
`UnityBootstrap-Daily-v1` contract. P12-C identity/genesis/deterministic roots,
P12-D factual roots, and P12-E selected core/daily owners are prerequisites to
F. Their designs are inputs, not evidence that their capabilities have been
implemented or promoted.

**Current evidence revalidation (2026-09-29):** this F scope preserves the
spatial-boundary correction at `d409549be5aee048ab8a90dc271af80288fa4e01`
and is checked against the current owner/profile inventory
`16611d89be3e9b8deae595d61ea8f8870c88e5ff`, P12-B design/reference refresh
`ef8c72cd388445e25ce9360bb1e689fc0a07c639`, P12-C identity/genesis design
`edc51571559a9ba4b1a025963de2e25b23c66fd3`, P12-D design
`e8b83d75e34f8456555065e24bfe67bb30366baa`, and P12-E design
`104c21cbd53c7bba8855bcac076eddc84bab947e`. P12-C passed exact-tip
independent review at `edc5157`. P12-D `e8b83d75e34f8456555065e24bfe67bb30366baa`
and P12-E `104c21cbd53c7bba8855bcac076eddc84bab947e` each passed independent
exact-content review, recorded separately in
`codex/phase12/P12DEIndependentReview` at
`3d5d7a8ce41f34d1fb55864897f9d508f50d8fcf`. The records limit their verdicts
to the designs and explicitly claim no delivered or promoted capability or
P12-A readiness. P12-B remains blocked on the complete live
owner/mutation census, committed-mutation invalidation evidence, composed
runtime validation, and the P18-D capture/handoff evidence recorded in the
inventory and B design. C implementation waits on B delivery; D/E implementation
waits on the named owner roots and interfaces; F remains downstream of C/D/E.
Earlier review hashes in the preceding F version apply only to their exact
reviewed contents.

**Current P18-D branch evidence (2026-09-29):** canonical prerequisites are
promoted at `9e790c5`. The current owner inventory `16611d8` records actor
bridge source `4016a73` with execution record `924cfee9b41c77274141795f0f7ddcd117819f89`
(exact-tip review PASS; focused 5/5) and merchant trade-state owner
`b05feafd4f95b1a3a559e6d58de339334df57365` (independent exact-tip PASS;
Merchant 8/8 and local observation 6/6). The `43363dd` SellGoods/local-
observation composite contains an evolved actor bridge; the separate
`4016a73`/`924cfee` review does not cover that composite bridge, which still
needs exact-tip integration review. The alternate bridge `10dcfda` is not the
selected composite source. Demography owner `ddcac0b` has independent exact-tip
review and focused owner validation, but remains an isolated candidate pending
integration. The optional-profile P18-D consumer candidate at runtime
implementation `3ddf847` (based on P14 integration merge `a2a8edd`) now
composes chronological advance, successful P18-C handoff, and the bounded
SellGoods consumer. It rejects P14-A local-material-flow Cities before
mutation. Test-only follow-up `0887d18` adds the consumer replay regression;
comment-accuracy follow-up `6a4d971` has fresh validation and corrects the
scenario wording. Current State evidence tip `f1cfed3` accurately describes
post-terminal replay, and exact-tip P18 review passed. Canonical promotion and
explicit `SimulationRuntime` hotspot handoff remain pending. This candidate
does not add P18 temporal state
to `UnityBootstrap-Daily-v1`, and does not establish P12 owner export or staged
hydration.

Revalidation uses architecture baseline `c285466c355103d3637ac165246591b72eb7bda0`,
the intraday/extensibility alignment record `4b6dd1d38cffeaf3cc1ac3effea0f8ede8771194`,
and the multi-participant activity alignment incorporated into the current
architecture. The P12-A daily profile still excludes P18 temporal state, P19
module state, and P20 shared activities; those identities/cardinality
constraints remain explicit if a later supported profile includes them.

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

The legacy NPC-owned `SpatialKnowledgeRuntime` and
`ExplorableSiteKnowledgeRuntime` values are included only when the admitted
profile inventory proves they are composed. They are distinct from canonical
P8-D `SpatialRouteKnowledgeStore` and `PersonRoutePlanStore` state, which this
profile excludes. P8-B, P8-C, P8-D, and P8-E each require an explicit empty
section; missing or unknown evidence is not empty, and populated state rejects
the profile. In particular, do not place P8-D route observations/plans, P8-C
`PersonSpatialPositionStore` facts, or P8-E position, transit, and civil-travel
state in the legacy NPC Knowledge section.

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

These are supported legacy commitment facts only when present in the selected
profile inventory; they do not include or stand in for canonical P8-E position,
transit, or civil-travel state. P8-D `SpatialRouteKnowledgeStore` and
`PersonRoutePlanStore` remain excluded as well. The complete canonical P8-B,
P8-C, P8-D, and P8-E sections require explicit empty evidence and reject
populated or unverified state.

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
   identity/history, strictly increasing input sequence values, contiguous
   local disposition ordinals, and local lifecycle invariants without
   dispatching a choice. Preserve optional `DecisionRecordId` strings as
   opaque values; do not compare them with the shared C record counter or
   require target decision rows.
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
material flow, canonical P8-B/C/D/E state, new gameplay, or a new
commitment/planning framework. In particular, do not serialize canonical P8-D
`SpatialRouteKnowledgeStore` or `PersonRoutePlanStore`, P8-C
`PersonSpatialPositionStore`, or P8-E position/travel state, as a substitute
for the explicitly supported legacy NPC Knowledge and commitment owners. P8-B,
P8-C, P8-D, and P8-E remain explicit-empty sections, with populated state
rejected. The optional-profile P18-D consumer candidate `3ddf847`, based on
P14 merge `a2a8edd`, composes chronological advance, successful P18-C handoff,
and bounded SellGoods; its P14-A local-material-flow City exclusion occurs
before mutation. It remains noncanonical pending canonical promotion and
explicit runtime-hotspot handoff; the exact-tip implementation review of code
`6a4d971` and State `f1cfed3` passed. Test-only `0887d18` adds the replay
regression, with comment-only follow-up `6a4d971`; current State evidence tip
`f1cfed3` records post-terminal replay and passing validation. Candidate composition neither changes the
accepted `UnityBootstrap-Daily-v1` profile nor satisfies P12 owner export,
staged hydration, or B's live census and handoff gates. It adds no P18 temporal
state to this P12 daily profile.
The alignment constraints on extensibility and shared-activity identity and
cardinality remain review constraints without adding those deferred capabilities
to this profile.
