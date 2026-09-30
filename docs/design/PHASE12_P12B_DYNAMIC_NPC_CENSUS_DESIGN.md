# P12-B Dynamic NPC SpatialKnowledge Census Design

**Status:** Proposed bounded P12-B technical design; pending independent review.

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
unregistration plus Person-to-NPC materialization and its rollback path.
It does not make any other P12 owner inventory complete.

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

Reconciliation is an atomic set replacement performed only by the outermost
successful roster membership transaction, while that transaction's
registered operation scope is still active. It enumerates the full current
roster, constructs and validates both sections for each NPC, then stages the
complete family delta before replacing protocol state:

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
6. Advance the existing mutation epoch once for a non-empty add, remove, or
   replacement delta, including a new owner whose two cardinalities are zero.
   Leave it unchanged for a failed/rolled-back operation or a no-op roster
   operation.

All candidate witnesses and the resulting key set are validated before any
protocol dictionary, expected-section set, provider reference, baseline, or
epoch changes. Failure leaves protocol state unchanged and faults or rejects
the outer P12 operation according to the existing fail-closed runtime rule.

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
transaction. Calls nested by `PersonMaterializationSystem.TryMaterializePerson`
join that outer transaction and only mark its roster set dirty; they do not
publish a provider delta. Materialization reconciles once after the optional
starting-City binding succeeds. If materialization rolls back successfully,
the original roster and census inventory remain unchanged. If rollback cannot
restore the original world state, the runtime is faulted and no census
assessment may succeed.

Death and emigration do not remove an NPC from `SimulationRuntime.NpcRuntimes`
and therefore do not remove its pair. An unregistration rejected because an
NPC remains Person-bound or resident leaves the family unchanged. Existing
`RuntimeId` values remain the section identity; a successful explicit owner
replacement is represented by the atomic family delta above rather than an
implicit change to an already-live section.

This transaction boundary tracks roster membership only. It does not imply
that every other runtime mutation enters a P12 operation scope or notifies the
shared epoch. The broader committed-write map and global owner-thread and
quiescence proofs remain separate P12-B blockers.

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
- a materialization failure after nested roster registration restores the
  original census set and does not advance the epoch;
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
checkpoint acceptance is required; independent technical review is the next
gate. Implementation must use an isolated branch based on the then-current
P12 canonical tip and serialize the `SimulationRuntime` / materialization
hotspot with other writers.
