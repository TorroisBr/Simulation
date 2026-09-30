# P12-B Dynamic NPC SpatialKnowledge Census Design

**Status:** Independent technical review and revalidation passed at exact
design tip `f3c7edee28968b6af0020c09a08fc0ab8740fe64`; implementation may
proceed under the previously accepted P12-B prerequisite authority.

- **Design base:** P12 canonical `0a37e9f053b482d80d0815c95352e3d96b56ed8f`.
- **Accepted authority:** P12-B in
  [`PHASE12_CAPABILITY_CHECKPOINT_DECOMPOSITION.md`](PHASE12_CAPABILITY_CHECKPOINT_DECOMPOSITION.md).
- **Current partial witness:**
  [`PHASE12_P12B_SPATIAL_KNOWLEDGE_CENSUS_CANDIDATE.md`](PHASE12_P12B_SPATIAL_KNOWLEDGE_CENSUS_CANDIDATE.md)
  was promoted at `55e2f232b02cd5c6df76014d325a8caeb7020408`.

## Problem

The promoted SpatialKnowledge providers are constructed once from the
bootstrap `Runtime.NpcRuntimes` roster. The resulting read-only array remains
fixed after composition. `SimulationRuntime.TryRegisterNpc` and
`TryUnregisterNpc` can change the live roster later, so the passive sections
can omit a newly registered NPC or retain a removed NPC owner. A live list of
provider objects by itself does not close this gap: the current
`ContinuationCensusProtocol` seals expected sections and providers before
owner-thread binding and has no post-seal inventory reconciliation.

Person materialization makes the membership edge transactional. It binds the
Person, calls `TryRegisterNpc`, and may then bind the City; failure can roll
back the Person binding and unregister the just-added NPC. A census observed
between registration and final City binding would report a partial materialized
graph. The roster census must therefore follow the successful outer operation,
not the nested registration call.

## Bounded scope

This checkpoint makes the existing passive P12-B SpatialKnowledge witness
family follow the currently installed NPC roster at completed roster
membership boundaries. It covers successful direct NPC registration and
unregistration plus Person-to-NPC materialization/adoption and their rollback
paths. It also accounts for the already witnessed `PersonStore` sections when
those operations change its shared revision. It does not make any other P12
owner inventory complete.

For every NPC in the live roster, the family contains exactly two schema-v1
sections:

- `p12f.spatial-knowledge.locations/{RuntimeId}`;
- `p12f.spatial-knowledge.routes/{RuntimeId}`.

Each section is bound to that NPC's exact `SpatialKnowledgeRuntime`. The pair
reports separate location and route counts against the same owner revision.
Section membership and ordering are derived from the complete live roster and
ordered by ordinal `RuntimeId`. The existing ten-NPC day-zero witness remains
unchanged.

## Recommended reconciliation contract

Declare one closed dynamic section family before census inventories are
sealed. The family source is the world-owned `SimulationRuntime.NpcRuntimes`
roster; it is not an arbitrary caller-supplied provider list. The census
protocol continues to reject general registration after setup. It gains one
owner-thread reconciliation operation for this previously declared family.

Reconciliation is one atomic outer-operation commit, performed while the
runtime-owned transaction context is still active and before its registered
operation scope exits. It enumerates the full current roster, constructs and
validates both sections for each NPC, and stages the complete family delta
together with every changed fixed census section before replacing protocol
state:

1. Reject null NPCs, blank or duplicate RuntimeIds, absent or mismatched
   SpatialKnowledge owners, duplicate section ids, invalid witnesses, and
   pre-existing owner revision drift not included in this roster operation.
2. Keep unchanged sections and their baselines when both section id and exact
   owner instance remain the same.
3. Add both sections for each new owner and seed their baseline from the
   completed roster state.
4. Remove both sections for each owner no longer in the roster, releasing the
   protocol's references to that detached NPC and owner.
5. If one RuntimeId remains in the final roster but now refers to a different
   NPC/SpatialKnowledge object after an explicit successful roster operation,
   treat it as an owner replacement in the same atomic delta and seed the new
   pair. A changed owner reference without this explicit family reconciliation
   remains a fail-closed identity mismatch.
6. Include both fixed `PersonStore` sections whenever that store's revision
   changed during the outer operation, even if rollback restored its
   materialization-binding cardinality. Both
   `p12d.person.membership` and
   `p12d.person.materialization-binding` share that revision and must receive
   the same commit boundary.
7. Advance the existing mutation epoch at most once for the union of changed
   fixed sections and dynamic-family additions, removals, or replacements.
   A newly added owner with two zero counts still changes the family. A fully
   compensated operation may leave the family unchanged yet still require one
   epoch advance because an included fixed owner's revision advanced. Leave
   the epoch unchanged only when no included owner revision or family
   membership changed.

The protocol needs one combined commit operation for this boundary. It must
not call dynamic-family reconciliation and
`NotifyCommittedMutations(PersonStoreSections)` separately, since that would
double-advance the epoch for one outer operation. All candidate witnesses and
the resulting key set are validated before any protocol dictionary,
expected-section set, provider reference, baseline, or epoch changes. Failure
leaves protocol state unchanged and faults or rejects the outer P12 operation
according to the existing fail-closed runtime rule.

At every owner-section assessment, the protocol verifies that the current
family enumeration still matches the reconciled section ids and owner
instances. A missed roster reconciliation therefore returns
`OwnerCoverageIncomplete`; it cannot silently admit a stale provider array.
During the outer operation, assessment returns `OperationInProgress` before
reading the owner census. Reconciliation publishes the new set only after the
domain operation has completed successfully and before the operation scope
exits.

## Runtime transaction boundary

`TryRegisterNpc` and `TryUnregisterNpc` keep their existing gameplay acceptance
and rejection rules. A direct successful call is one outer membership
transaction. A runtime-owned nesting context, separate from the protocol's
active-operation count, associates nested roster calls with their outer
materialization/adoption operation; nested calls only mark membership dirty
and cannot reconcile early. A direct registration or unregistration creates
its own outer context. This is necessary because `SimulationOperationScope`
tracks only a count, not operation identity.

On successful new Person materialization, the final commit includes the two
`PersonStore` census sections and the added SpatialKnowledge pair in one epoch
advance. If materialization fails after `TryBindMaterializedNpc` and its
compensation succeeds, the binding count returns to its prior value but
`PersonStore.Revision` advances once for the bind and once for the rollback.
The final commit therefore includes both `PersonStore` sections and advances
the epoch once; the dynamic family remains unchanged. Failures before any
PersonStore write do not advance the epoch. A failed compensation faults the
runtime/protocol and blocks all census assessment; it does not publish a
partial provider delta or claim a successful commit.

`TryBindExistingNpcToPerson` and its compensation use the same fixed-section
accounting path but do not change the NPC SpatialKnowledge family. This keeps
the shared PersonStore revision observable without widening this checkpoint
to Person export or hydration. The design's commit helper must accept one
changed-section set and one optional dynamic-family delta, then update all
baselines and advance the epoch no more than once.

Death and emigration do not remove an NPC from `SimulationRuntime.NpcRuntimes`
and therefore do not remove its pair. An unregistration rejected because an
NPC remains Person-bound or resident leaves the family unchanged. Existing
`RuntimeId` values remain the section identity; a successful explicit owner
replacement is represented by the atomic family delta above rather than an
implicit change to an already-live section.

The materialization path may also change a City `ImportantNpcs` projection;
that projection has no complete owner witness in the current inventory and
remains a separate P12-B gap. This checkpoint must not claim complete outer
write coverage or capture readiness until that and the other committed-write
gaps are resolved. The combined commit accounts for all currently admitted
fixed sections affected by the operation plus the dynamic family, without
double-advancing the epoch. Other PersonStore mutations such as birth/death
remain in the broader writer map; if they change its revision without a
registered combined commit, assessment must fail closed. Global
owner-thread/quiescence proof remains a separate P12-B blocker.

## Required implementation evidence

The later implementation must prove:

- the selected bootstrap still reports the same ten NPC owners and twenty
  sections with exact counts, owner identities, shared revisions, and
  deterministic order;
- a successful standalone registration adds exactly one pair and one epoch
  transition, while a no-op or rejected registration changes neither;
- permitted unregistration removes exactly one pair and releases its owner;
  Person-bound/resident rejection, death, and emigration retain the pair;
- successful Person materialization adds the pair only after all materialized
  bindings complete;
- successful materialization updates both fixed `PersonStore` section
  revisions and the new SpatialKnowledge pair with one shared epoch advance;
- if a supported path can fail after PersonStore binding and compensate, that
  compensation restores the materialization count, leaves the dynamic family
  unchanged, reports both `PersonStore` sections, and advances the shared
  epoch exactly once; current synchronous APIs expose no deterministic
  post-bind materialization failure input, so do not add a synthetic failure
  seam or new failure semantics;
- successful legacy adoption updates both `PersonStore` sections once without
  changing the dynamic family; current `TryBindExistingNpcToPerson` has no
  reachable post-bind failure because `PersonRuntime.TrySetResidenceSettlementRuntimeId`
  unconditionally succeeds once reached. Test its supported pre-bind
  rejections without revision change. Do not add a synthetic failure seam or
  new failure semantics. If a later contract makes post-bind adoption fallible,
  revalidate rollback accounting before including that path;
- a failure before any included owner changes advances no epoch;
- an incomplete rollback faults the runtime and blocks assessment;
- census assessment during an active membership operation reports
  `OperationInProgress` without reading a partial owner set;
- successful same-RuntimeId owner replacement, if exercised by the existing
  API, atomically replaces both exact owner identities; replacement cannot
  occur as an unannounced per-section identity change;
- an injected missed or malformed reconciliation fails closed and causes no
  partially published expected/provider inventory; and
- all current day-zero and census suites, full EditMode, official Smoke, and
  `git diff --check` pass before any later canonical promotion.

## Explicit exclusions and readiness

This design does not add general census-provider registration, P12-A capture
eligibility, a complete profile inventory, notification wiring for
SpatialKnowledge discovery or other owners, a complete owner-thread or
quiescence proof, export/hydration, P12-C/D/E/F/G work, or a change to NPC
population/materialization semantics. It does not make P12-B complete or
P12-A ready. P12-A remains `WAIT_DEPENDENCY` until all accepted owner exports
and staged hydrators, the complete live profile inventory, and separate P12-A
implementation authorization are established.

The P12-B–P12-G prerequisite implementation authority already accepted in
`PHASE12_BRIEF.md` covers this bounded owner-census work. No additional
checkpoint acceptance is required. Implementation must use an isolated branch
based on the then-current P12 canonical tip and serialize the `SimulationRuntime`
/ materialization hotspot with other writers.

## Implementation revalidation note

At canonical `0a37e9f053b482d80d0815c95352e3d96b56ed8f`,
`PersonRuntime.TrySetResidenceSettlementRuntimeId` only assigns the requested
value and returns true. After materialization's PersonStore bind, the remaining
roster registration checks repeat pre-bind checks on the same synchronous
runtime, and starting-City presence uses that City's own Location and a fresh
alive, non-traveling candidate. Thus neither adoption nor materialization has a
deterministic supported post-bind failure input in the current API. The
implementation must cover successful paths and pre-bind rejection, harden any
existing compensation result so a future failed rollback faults/blocks the
census, and preserve fail-closed accounting if a future supported post-bind
compensation path is introduced. Do not add synthetic failure seams or new
failure semantics. This narrows test evidence to reachable behavior; it adds no
product or failure semantics.
