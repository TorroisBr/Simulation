# Phase 13 — Historical Reconstruction Entry Proposal

**Status: PROPOSED, UNACCEPTED.** This is bounded entry/design exploration only. It
does not create a checkpoint, approve a P13 State, authorize implementation, or
change the Phase 13 Brief's `WAIT_DEPENDENCY` readiness. It is subordinate to
`SIMULATION_ARCHITECTURE.md`, the Phase 13 Brief, and actual promoted capabilities.

**Current-base revalidation (2026-09-27):** P9 canonical is `82396ae` (closed
within P9-A/P9-B; P9-B's selected authored geography is exactly one P8-A Hex
and one anchored Location with provenance); P10 canonical is `95f9882` (P10-A
promoted, Phase 10 open, with no universal Ruin/topology assumption);
P12's accepted scope record is `4a1d364` (scope only, still `WAIT_DEPENDENCY`;
latest docs-only owner-inventory refresh candidate `18f1cbb`); P14 canonical is
`4caecbb`. P18 canonical is `99cac77` with its A/B/C core promoted; additive
P18-A contract `2175bf2` was accepted, with acceptance recorded in implementation
branch commit `9de70ae`; its implementation remains in progress and unpromoted.
P18-C adapter `405c5c0` is reviewed but unpromoted, and P18-D design `aa5f182`
remains implementation-blocked. The current P20-A checkpoint proposal is `2a03eda`
(proposed/unaccepted, acceptance pending); technical design `6a0d164` does not
establish a delivered P20 capability. Architecture baseline `c285466` and both
alignment records remain active constraints. These status updates do not
narrow P13's full boundary guarantee or satisfy its P12 hard edge.

## 1. Purpose and guarantee

Phase 13's product guarantee remains: reconstruct the authoritative world at
**any actually simulated boundary from the first simulated boundary onward**,
then independently continue a fork under compatible semantics. This includes
boundaries before, during, and after any supported causal mutation. It excludes
boundaries inside generated pre-simulation backstory. The guarantee is not
limited here to save points, dates, daily boundaries, known checkpoint cadence,
or a proposed storage profile. Any proposal to narrow which simulated
boundaries are supported is a product decision for the user.

The fork starts with the boundary's authoritative truth and the causal inputs
and compatible execution context needed for future behavior. It must neither
apply effects twice nor omit, invent, or move a fact across its actual boundary.
Reconstruction is judged by owner-authorized world state and future behavior
under compatible inputs/semantics, not by diagnostic byte equality alone.

## 2. Semantic constraints

- `SAVE != REPLAY != HISTORY`. Save/continuation is recovery of a current
  boundary; replay is one possible reconstruction mechanism; History is a
  downstream record/projection. None implies either of the others.
- There is no event-sourcing mandate or universal event log. A mechanism may
  combine owner snapshots, retained inputs, deterministic recomputation, or
  other reviewed means, provided it satisfies the full boundary guarantee.
- Diagnostic snapshots, diffs, canonical exports, UI projections, event
  records, and selective histories are not authoritative truth and cannot
  stand in for an owner's exact export/hydration contract.
- Domain stores retain mutation authority. A reconstructed state must be
  publishable through validated owner hydration/composition, not by mutating a
  diagnostic projection or rebuilding truth in a parallel shadow model.
- Only compatible code, content, configuration, calendar, and effective
  semantics can be assumed. The guarantee does not certify arbitrary
  incompatible versions/mod combinations.
- Generated initial outputs are historical world truth at the first simulated
  boundary. Installation or retrofit later in a run is a mutation at its own
  boundary; it does not rerun prior generation stages.

## 3. Candidate decomposition

This proposal separates three concerns without prescribing storage technology:

1. **Boundary truth:** exact recoverable state for every authoritative owner
   whose facts exist at the requested boundary, including stable semantic IDs,
   allocator/high-water state, owner revisions and cross-owner relationships.
2. **Causal inputs and execution context:** accepted external inputs and their
   payload, authority, exact logical boundary, stable ordering/sequence,
   deterministic random context where consumed, plus compatible effective
   configuration/content/calendar/code and required module versions/state.
3. **Reconstruction and publication:** restore or derive candidate owner state
   without observable partial publication; validate owner invariants and
   cross-references; bind runtime mutation guards/derived indexes only after
   validation; publish a coherent world boundary once; then allow normal
   continuation from that boundary.

The precise split between persisted boundary snapshots and replayed mutations,
their cadence/retention, indexes, compression, and file/database layout is
deferred. Any chosen mechanism must still cover all actual simulated
boundaries. A checkpoint interval may be an optimization; it cannot redefine
which boundaries the product supports.

## 4. Capability-specific dependencies

### Hard implementation prerequisites

- **P12 continuation capability for the included world:** exact immutable
  exports and validated staged hydration for every included authoritative
  owner, plus coherent runtime composition/publication. Continuation alone is
  not P13, but P13 cannot reconstruct/fork a world whose owners cannot be
  captured and restored.
- **Recoverable causal inputs and initial-world/mutation semantics:** each
  mutation/input needed to distinguish two possible histories must have
  recoverable authority, payload, boundary, order/idempotency identity and
  compatible semantics, or be unambiguously derivable from retained facts.
  Inputs cannot be inferred from event/history projections after their causal
  content has been discarded.
- **Compatible execution context:** the effective code/content/configuration,
  calendar, deterministic random state/context actually consumed, and domain
  version/migration semantics needed to continue the selected branch.
- **Relevant temporal capability only for supported intraday histories:**
  P18 state/order/input boundaries and applicable promoted consumer integration
  are needed when claiming those intraday boundaries. Do not invent historical
  intraday actions for a history that ran only under daily semantics.
- **Relevant shared-activity capability only when supported:** P20 state and
  input semantics are needed for boundaries containing that activity's
  formation, participant decisions, reservations, start/abort or effects.

P12-A scope was accepted at `codex/phase12/ContinuationDesignP9BRevalidation`
tip `4a1d364`, but that is scope acceptance only. The latest owner-inventory
refresh candidate `18f1cbb` still reports no profile-included owner
group with demonstrated complete exact immutable export plus staged
hydration. It identifies missing envelope/admission, identity allocator
restoration, owner DTOs and hydrators, causal inputs/commitments,
mutation-health/quiescence and atomic staged publication. The refreshed P12
record also retains `WAIT_DEPENDENCY` and no implementation authorization.
This is evidence of the hard P12 blocker, not a P12 capability. Therefore P13
implementation remains `WAIT_DEPENDENCY`.

### Conditional module and extension coverage

For modded histories, reconstruct the module/code identity and versions,
module-owned state, compatibility and migration/retrofit facts needed at the
requested boundary. An installation or explicit retrofit is represented at
its actual boundary. A fork before it excludes facts introduced by it; a fork
after it retains its results without rerunning historical generation. P19's
loader is not a blanket prerequisite for unmodded worlds; only the applicable
module/version/state/migration capability is required for histories that use
it. Modding remains player-owned, not an adversarial security boundary.

### Edges that are not blanket prerequisites

Do not impose all of P18-D, P19, and P20, nor phase-number order, as universal
P13 gates. Daily-profile P13 coverage can be separately designed and validated
where its actual owners and inputs are recoverable, but it cannot claim
complete intraday coverage. P18-D is required only for the selected migrated
consumers whose behavior is in the covered history. P19 is conditional on
modded histories. P20 is conditional on shared-activity histories. These are
capability-specific hard edges for the corresponding claimed coverage, not
blanket phase locks.

Soft test ordering may prefer proving a bounded daily continuation/reconstruction
slice before intraday, modded, or shared-activity slices because it reduces
diagnostic complexity. This ordering is not readiness dependency and does not
alter the full product guarantee. Generated-world fixtures are useful evidence,
but a manually authored initial world may also define the first simulated
boundary.

## 5. Boundary-sensitive state inventory

The eventual supported-world inventory must be derived from the actual
composition and owners, not from diagnostic field lists. At minimum it must
account for:

- the first simulated boundary, initial authored/generated outputs and their
  compatible provenance/inputs;
- all authoritative owner facts and stable identities, including allocator,
  sequence, revisions, relations, commitments and mutation disposition needed
  to preserve exact boundary state;
- external commands and other accepted inputs, including authority, payload,
  exact logical instant/day, order and idempotency/terminal disposition;
- deterministic execution context and random state/context only where consumed;
- temporal time/calendar, pending owner work, activity lifecycle and causal
  ordering where the history actually used P18. P18 canonical is `99cac77`;
  the additive P18-A returned-facts/subphase contract `2175bf2` was accepted
  (acceptance record `9de70ae`) but is not yet implemented/promoted, and P18-D
  remains blocked. Do not invent
  intraday facts absent from the execution history;
- P20 `ActivityInstanceId` as identity distinct from `PersonId` and participant
  identities, plus the boundary-specific participant/role set;
  independent agreements/withdrawals, reservations, scheduled/validated start,
  cancellation/abort and already-applied participant/shared effects where used;
- module identities/versions, module-owned authoritative state, migration or
  explicit retrofit inputs/results at their actual installation boundaries;
- compatibility metadata sufficient to reject unsupported execution rather
  than silently reinterpret it.

For P20, an earlier boundary preserves its then-current pending or executing
instance and participant set. It must never receive a later final roster
retroactively. General participant cardinality is one or more; one- or
two-person fixtures are bounded proofs, not a cardinality rule. `ActivityInstanceId`
never aliases a `PersonId` or participant identity. For P18, reconstruction
consumes supported recorded temporal state/order and inputs; it cannot
manufacture intraday detail absent from the historical execution. Indexes may
be rebuilt only when their ordering and validity derive completely from
retained authoritative facts.

## 6. Proposed technical proof obligations

Before any future bounded implementation design is accepted, it should define
and test:

- how the requested boundary is identified unambiguously, including exact
  intraday logical instant where supported;
- how every owner contributes immutable state and validates hydration, and how
  a fresh composed world is published atomically only after all required
  owners/cross-references pass;
- how captured inputs are sealed, ordered, deduplicated and applied at their
  actual boundaries without retroactive mutation;
- how forks at adjacent boundaries differ exactly by intervening committed
  causes, and continuation does not double-apply effects;
- how fork branches advance independently and deterministically under the same
  compatible inputs/semantics, including stable identities and random context;
- how unavailable/incompatible owner, module, input or version state fails
  admission deterministically without partial publication;
- how branch-specific P18/P20 state, membership/roster changes and module
  retrofit boundaries are preserved; and
- how correctness is evaluated through authoritative owner state and
  continuation behavior, with diagnostics used only as supporting comparison.

Exact serialization DTOs, snapshot/replay API, storage schema, log taxonomy,
retention policy, compaction, branch database layout, user-facing history UI,
and a universal event catalog remain deferred until owner contracts and a
concrete consumer make those choices necessary.

## 7. Open questions and decision ownership

The following remain technical design questions unless resolving them would
change the product guarantee:

- Which owner exports/hydrators and composition stages form the minimal first
  supported profile, and what exact admission manifest binds them?
- Which boundary token represents a completed/quiescent mutation/input wave,
  especially while a scheduler or multi-owner transaction is processing?
- Which facts are stored as boundary snapshots versus reconstructed from
  retained causal inputs, and what validation oracle proves equivalent truth?
- How are compatible code/content/module versions located and migration paths
  selected without retroactively applying a migration or retrofit?
- Which deterministic random generators expose state, and which operations
  instead retain explicit random outcomes/inputs?
- What failure/retention policy applies when a required historical input or
  compatible module is unavailable?

These should be resolved with the owners and the selected implementation
profile, not guessed in this entry proposal. **Any decision to narrow the
supported boundary guarantee** (including omitting dates/instants, classes of
mutations, owners, or historical intervals from a profile while claiming P13
closure) is a user product decision and requires explicit user direction. The
proposal does not select such a narrowing.

## 8. Candidate documents consulted

These are planning/design candidates, not promoted capability evidence:

- `docs/design/PHASE18_A_TECHNICAL_DESIGN.md` and
  `docs/design/PHASE18_B_TECHNICAL_DESIGN.md`: scheduler/owner due-work and
  activity identity/lifecycle boundaries; useful only for actual supported P18
  histories. Phase 18 State/code determine delivered capability.
- `docs/design/PHASE18_C_TECHNICAL_DESIGN.md`: availability/input-boundary
  decisions; candidate design is subordinate to current promoted code/State.
- `docs/phases/PHASE18_BRIEF.md`: temporal objective and conditional P13 edge.
- `docs/phases/PHASE20_BRIEF.md`, current scope proposal
  `codex/phase20/P20FirstCheckpointProposal` at `2a03eda` (proposed/unaccepted),
  and technical design `6a0d164`: boundary-specific shared-instance and atomic
  effect requirements. The exact-two fixture is not general cardinality and
  candidate text does not prove runtime capability.
- `docs/architecture/INTRADAY_EXTENSIBILITY_ALIGNMENT.md` and
  `docs/architecture/MULTIPARTICIPANT_ACTIVITY_ALIGNMENT.md`: accepted
  cross-phase constraints, including no blanket P18-D/P19/P20 edge.
- P12 owner inventory refresh candidate `codex/phase12/ContinuationDesignP9BRevalidation`
  at `18f1cbb` (accepted scope remains `4a1d364`): evidence of current export/hydration gaps, not a completed
  continuation contract or implementation authorization.

The implementation dependency graph must be refreshed against canonical
Phase 12/18/19/20 States and code before scheduling. This proposal creates no
checkpoint IDs and does not certify readiness.
