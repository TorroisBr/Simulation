# P12-B ScheduledDirective Passive Census — Technical Proposal

**Status:** Bounded technical proposal, documentation only. It proposes one
owner-issued passive census capability within accepted P12-B scope. It does
not implement the capability, complete the live owner inventory, establish
P12-B readiness, or change P12-A `WAIT_DEPENDENCY`.

**Base:** P12 canonical `e9ced8e451f42e80ed2132ce494cd5c26e439894`.

## 1. Purpose and scope

The selected `UnityBootstrap-Daily-v1` profile composes one
`ScheduledDirectiveStore` and currently observes that its directive list is
empty at day zero. The store has no owner revision, so this observation cannot
prove that the same owner remains empty or report its live state at a later
capture boundary. This proposal adds a fixed, passive section witness for the
exact composed store.

The proposed section is:

| Field | Contract |
|---|---|
| Section ID | `p12f.scheduled-directives` |
| Schema | `1` |
| Role | Required owner section; cardinality may be zero or positive |
| Owner | The exact `ScheduledDirectiveStore` installed in and exposed by `SimulationBootstrapComposition.ScheduledDirectives` |
| Cardinality | Number of stored directives, including terminal Succeeded, Failed, and Skipped directives |
| Revision | Store-local monotonic revision, initially zero, advanced once for each successful owner commit that changes stored membership or a stored directive's processing state |

This is a live count/revision/identity witness. It is not an export of
directive fields, a global mutation epoch, an operation/quiescence proof, or
a claim that every P12 profile owner has a census adapter.

## 2. Source-grounded owner and mutation surface

`ScheduledDirectiveStore` owns a private ordered `List<ScheduledDirective>`
and a private ID dictionary. Its `Directives` property exposes a read-only
wrapper over that list. `Add` rejects mutation-guard failure, null values,
cross-runtime guard ownership, and duplicate `DirectiveId`; on acceptance it
binds the directive to the store's authoritative mutation guard when one is
present, then inserts it into both collections.

The successful Add path can immediately mark the new row Skipped. Existing
semantics do so when `AbsoluteDay < 1` or when it is earlier than the current
`SimulationTime.AbsoluteDay`. The stored membership and initial terminal
state are one successful owner operation and must produce one revision step,
not two. The check order and skip reasons remain unchanged.

Each stored directive also exposes public `MarkSucceeded`, `MarkFailed`, and
`MarkSkipped` methods. They mutate a Pending directive only when its bound
`AuthoritativeMutationGuard` permits mutation, the current day is nonnegative,
and the requested state is terminal. A failed or repeated transition returns
false and leaves the row unchanged. These direct methods are supported
writers because runtime processing and callers invoke them on the stored
object; routing only through a new store API would miss existing writes.

`ScheduledDirectiveSystem.PrepareDay` reads pending rows and may mark due
directives Skipped for duplicate actor targets or an unresolved actor. The
successful status transitions are owner writes even though preparation as a
whole is not a store mutation. `TryTakeDirective` removes an actor-to-directive
entry from `directivesByActorForCurrentDay`, a transient system-owned lookup;
it does not remove or change the stored directive and must not increment the
store revision. A mere prepare/query with no successful status transition
also does not increment it.

## 3. Proposed owner commit contract

The implementation should keep the owner-local revision and stored-list
cardinality coherent through every supported mutation path:

1. Add a private owner monitor and a `long` revision to
   `ScheduledDirectiveStore`. Bootstrap revision is zero, including when
   genesis adds no directives.
2. Run supported store writes under that monitor. Census reads acquire the
   same monitor and capture the list count and revision together. A provider
   sample can therefore never combine the count from one committed state
   with the revision from another.
3. Once a directive is accepted into the store, bind its terminal-state
   transition callback to that store, in addition to its existing mutation
   guard binding. The public `MarkSucceeded/Failed/Skipped` methods retain
   their current signatures and return behavior, but delegate the state
   transition through the owning store. The store takes its monitor, checks
   guard/state/day/terminal-state/revision preconditions, applies the state
   change, and increments revision once before returning success. A stored
   directive cannot change terminal state without updating this owner's
   revision.
4. For an accepted Add, preflight revision capacity and existing duplicate,
   guard-binding, and directive-ownership rules before changing stored
   membership. Append to both indexes and, if current source rules require
   immediate skip, apply that initial status with a private owner-only
   transition primitive inside the same monitor scope (not through the public
   callback path). Increment revision once after the complete
   membership/status commit.
   A failure before commit leaves list, dictionary, directive state, and
   revision unchanged. Existing log messages and schedule semantics remain
   intact.
5. Before any commit that would increment a saturated `long` revision, reject
   without effects. For Add, perform this check before binding the directive
   or inserting it. For a terminal transition, return false with the row
   still Pending. Do not wrap, reset, or saturate the revision silently.
6. No-op paths—null/guard rejection, duplicate IDs, failed ownership binding,
   invalid status preconditions, repeated terminal marking, and transient
   `TryTakeDirective` consumption—leave revision unchanged. Successful Add
   and successful terminal transition each add exactly one.

The owner callback is needed because `ScheduledDirective`'s current public
state-transition methods are direct mutation entrypoints. The callback should
be bound only after an Add has passed all rejection checks and revision
preflight. A directive rejected by Add remains unattached to this owner. The
implementation must preserve the current guard semantics for both standalone
directives and stored directives. Because `ScheduledDirective` is marked
`[Serializable]`, any store callback binding should be runtime-only and
re-established by normal store ownership; it must not become serialized
delegate state.

### Concurrency boundary

The monitor establishes local coherence between this store's supported Add,
stored status transitions, and census samples. It does not make
`ScheduledDirectiveStore`, `ScheduledDirective`, `ScheduledDirectiveSystem`,
or the simulation runtime generally thread-safe. Existing consumers of
`Directives` must not be treated as receiving a thread-safe snapshot merely
because the census provider locks while sampling. P12-B still requires its
separate owner-thread and quiescence proof before capture; this proposal does
not supply that proof.

## 4. Census provider and selected-profile wiring

Implement one fixed provider (suggested name
`ScheduledDirectiveCensusProvider`) following the current passive provider
pattern:

- retain the exact `ScheduledDirectiveStore` passed to the provider;
- expose `p12f.scheduled-directives`, schema version 1;
- return `OwnerSectionCensusWitness(sectionId, schemaVersion, owner,
  cardinality, revision)` sampled atomically by the store;
- do not enumerate directive values or call any mutator during reads.

`SimulationBootstrapComposition` should create the provider from the same
`ScheduledDirectiveStore` assigned to `ScheduledDirectives` and expose it as
a fixed property. This wiring makes the published witness owner identical
(`ReferenceEquals`) to the selected runtime's installed store. Do not discover
the provider by reflection, synthesize another store, or register this
section into a generic/dynamic census protocol. Keep the provider passive and
outside export/hydration.

## 5. Boundary and exclusions

The eventual capture caller must request this witness only at the admitted
P12-B completed, quiescent boundary and must revalidate its revision after
collection. Until P12-B's runtime-wide owner-thread and in-flight-operation
fences exist, this adapter cannot certify that a capture is eligible or that
no concurrent writer can run before or after its sample.

This proposal excludes:

- directive serialization, import, export, staged hydration, and restored
  graph validation;
- global mutation-epoch notification or registration in a complete
  `UnityBootstrap-Daily-v1` census;
- runtime-wide locking, owner-thread binding, capture eligibility, or
  quiescence claims;
- changes to directive selection, schedule validation, terminal-state
  meaning, actor resolution, or `TryTakeDirective` behavior;
- new directive types, APIs intended to change game behavior, or generic
  census protocol changes;
- any inference that this one witness completes P12-B, P12-A, or another
  P12-B owner obligation.

## 6. Focused implementation test matrix

These are proposed tests; none was run for this document.

### Owner identity and initial state

- Provider reports section ID/schema version, exact store owner reference,
  count zero and revision zero for a newly composed empty store.
- Two providers over different stores report their respective owner
  identities; a provider cannot accidentally report a separately constructed
  store.
- Adding authored directives through normal bootstrap leaves census count
  equal to stored list count and reflects the final state after genesis.

### Successful commits

- Successful valid Add increments count and revision once.
- Successful Add whose existing schedule rules immediately mark it Skipped
  increments count and revision once total; state, processed day, and reason
  remain the existing values.
- Successful direct `MarkSucceeded`, `MarkFailed`, and `MarkSkipped` calls on
  stored rows each increment revision once and preserve current state fields.
- `PrepareDay` conflict skipping and unresolved-actor skipping route through
  the owner and each successful row transition increments the revision once.
- Census count includes Pending and all terminal rows; state transition does
  not change cardinality.

### Rejection, no-op, and transient state

- Duplicate ID, null Add, mutation-guard rejection, failed/cross-runtime
  directive binding, and failed capacity preflight leave membership and
  revision unchanged.
- Invalid day/state preconditions and repeated Mark calls leave row fields
  and revision unchanged.
- Repeated `PrepareDay` for the same day and preparation with no successful
  skips do not increment revision.
- Successful `TryTakeDirective` and a second unsuccessful take do not change
  store count or revision; the pending row remains stored and its status can
  still be sampled.
- `Add` at `long.MaxValue` revision and terminal transition at
  `long.MaxValue` fail closed before any visible owner or directive change.

### Coherence

- A census sampled before a successful Add/terminal transition remains a
  self-consistent historical count/revision pair; a later sample observes the
  new pair.
- A coordinated writer/read test proves census count and revision are read
  under the same owner monitor, without asserting general runtime thread
  safety or concurrent safety for direct list consumers.

## 7. Dependencies and completion limits

This is a bounded owner witness proposal for an already accepted P12-B
capability boundary. Implementation still follows the repository's normal
checkpoint review and validation workflow. The P12-B blocker record currently
lists directives among F owners missing a count/revision witness and states
that their raw day-zero empty-list observation does not establish exact-zero
owner evidence.

Even after implementation and promotion, P12-B remains incomplete until all
included owners have exact witnesses, successful writes are covered by the
shared invalidation contract, the owner-thread/quiescence evidence is complete,
and the complete profile composition is revalidated. P12-A remains
`WAIT_DEPENDENCY` until full owner export/hydration, live profile inventory,
and its separate authorization requirements are met.
