# P12-C Identity and Sequence Snapshot Slice — Current-Base Revalidation

**Status:** Current-base contract revalidation for a bounded P12-C slice.
This record preserves the old candidate and authorizes only additive
recomposition of its allocator/sequence snapshot work after independent review.

## Current authority and dependency

- Architecture: `codex/architecture/world-identity-projection` at
  `47eff220c7ce00f6e7c759bdc2b76780bb46f628`, including the intraday and
  extensibility alignment and the multi-participant activity alignment.
- P12 canonical before this candidate: `f23fe5a1c70dce8cb32a4ca6aa088820b3ad7279`.
- P8 canonical: `470667d37863384edadb3d93ef64d8004aff46a3`.
- P9 canonical: `82396ae7ffaf407fda278928da456b06dc5394d4`.
- P11 canonical: `308e24d0744112e8f2b741521b8b3e4acb51ebbf`.
- P18 canonical: `8ac2d7885ea1f00d544d88a64bf918a411934f7f`.
- P20 canonical: `00d36d5c5603cc0bffcce354f05b40fc3d4e6d88`.

P12-B was promoted at `0e9ed16823035d9449c96f9fc9fc8f95cf41616f` and its
State record was refreshed at `f23fe5a1c70dce8cb32a4ca6aa088820b3ad7279`.
The P12-B dependency is therefore satisfied. The accepted P12-C scope remains
the one in `PHASE12_BRIEF.md` and
`PHASE12_CAPABILITY_CHECKPOINT_DECOMPOSITION.md`.

## Preserved candidate and integration rule

Preserve `codex/phase12/P12CIdentityRuntimeSnapshot` at
`531d835f01a9070df42d54291ffde32387fb4358`. It is based on
`8db8cfc0096c0a2bc05981e7577da048441f65e4`, is not an ancestor of current
canonical, and must not be fast-forwarded or copied wholesale. Its snapshot
DTOs, exact-family validators, staged constructors, and tests are reusable.
The candidate's old `DecisionRecords.cs` and `RuntimeIdentity.cs` also remove
current P12 mutation hooks, census identity/revision behavior, P11
`ActorChoice`, and P18-D occurrence-receipt ownership. Recomposition must add
only the snapshot seams to the current owner implementations and retain every
current behavior.

## Bounded slice contract

This slice adds immutable snapshots and private staged construction for the
fourteen existing typed `RuntimeIdAllocator` next-value counters and the shared
`SimulationRecordSequence` next value. Preserve exact values and gaps; reject
unknown schemas, wrong family sets, duplicate/missing/unknown counter families,
and values below 1 or at exhaustion. Rejection must not mutate a live owner or
allocate replacement identity. A staged owner receives a fresh in-memory
census identity, derives local revision as `next value - 1`, and remains
unbound until the normal runtime composition binds its P12 mutation boundary.

The current allocator has fourteen matching counter fields and fourteen
owner-census providers. Event, Decision, and TravelParty writes have existing
P12 admission/commit hooks. `SimulationRecordSequence` has its own census
identity/revision and P12 mutation boundary. Preserve those hooks and provider
contracts so a restored owner can be bound by `SimulationRuntime` and later
writes still invalidate the shared epoch.

The sequence is shared by `DomainEventRecorder` and `NpcDecisionRecorder`,
including `TryRecordOccurrenceOnce` receipt behavior. Preserve the current P11
`NpcDecisionOrigin.ActorChoice` value and P18-D occurrence receipts/census.
No decision/event records or receipt contents are exported by this slice.

## Current causal and profile audit

The current P12 Brief and State select the dedicated P9-B-only
`UnityBootstrap-Daily-v1` profile. `Simulation-GeneralTest.asset` remains the
separate P10-A proving profile. The stale P12-A technical-design text and old
candidate documents do not change that accepted profile boundary.

The selected bootstrap builds one `DeterministicRandomSource` from the
effective authored seed (or zero) and passes it to the runtime, decision
system, and Crime system. The source is stateless per `(seed, stream key, draw
index)` call. Current source keys include `npc-decision|<NpcRuntimeId>|<day>`,
`action-success|<actor>|<decision-or-action>|<action>|<day>`, and
`crime-steal|<thief>|<day>`; direct keyed calls use draw index zero. Mutable
`DeterministicRandomStream` draw cursors are created by
`SeededConflictRandomSource`, which is not constructed by the selected Daily-v1
bootstrap. The shared seed/config/provenance and any stream state actually
used by an admitted profile remain P12-C obligations; this identity/sequence
slice neither captures nor excludes them.

P9-B genesis manifest/provenance, exact P8-A Hex/Location/scale facts,
deterministic provider roots, global identity/reference validation, and
profile-wide export/hydration remain separate P12-C/P12-A obligations. This
slice is partial P12-C delivery only and does not complete P12-C, P12-A, or
Phase 12.

## Required validation and review

Before promotion, current-base validation must cover the retained exact-value,
gap, immutability, malformed-snapshot, and non-mutating-rejection cases, plus
current P12 runtime rebinding of restored owners. The integration tests must
confirm fresh owner identities and `next - 1` revisions; successful Event,
Decision, TravelParty, and record-sequence writes still advance their local
revision and P12 epoch; rejected admission consumes no next value; and
ActorChoice/occurrence-receipt behavior remains intact. Run the affected
identity, record-sequence, ActorChoice, and P18-D receipt suites, ALL EditMode,
official Smoke, applicable LongRun validation, and `git diff --check` on the
exact integrated tree. Obtain fresh independent exact-tip implementation
review after integration; no old candidate validation or review substitutes
for it.

No unresolved product or canonical architecture decision is identified. This
revalidation authorizes the existing accepted P12-C snapshot sub-scope only;
it does not authorize a broader P12-C claim or Phase closure.
