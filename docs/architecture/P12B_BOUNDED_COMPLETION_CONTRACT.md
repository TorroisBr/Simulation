# P12-B bounded completion contract — Daily-v1

**Decision:** A — one bounded completion slice within the already accepted
P12-B checkpoint. No new numbered checkpoint, writer feature or Phase.
**Status:** TECHNICAL_DESIGN_IN_PROGRESS pending independent review.
**Date:** 2026-10-07.
**Implementation source base:** `origin/codex/phase12/canonical` at
`94551b08be8cc9347de35eae5051b8e578ea4c1e`.
**Architecture base:** `a29ddd1271fff8fc45abb3270229b43bfe89f2a9`.
This document prepares a completion workflow; it changes no canonical code,
Brief, State, architecture semantics or supported profile.

## 1. Current evidence and smallest path

The selected dedicated `Simulation-DailyV1.asset`/P9-B composition has
275 census sections at N=10, U=10, P=0, matching `65 + 20*N + U + P`.
The canonical effective-owner ledger and registered-operation matrix map 23
known routes. The Master/user and independent audits report no missing
registered family, cardinality mismatch or bypass among those routes.
This is retained positive evidence, not yet an exhaustive supported-reachability
or runtime-wide quiescence certificate. Older 253/258/260/268 State entries
describe their dated snapshots; use the latest 275 source evidence.

Exact retained sources at the implementation base:

- `docs/design/PHASE12_B_DAILY_V1_EFFECTIVE_OWNER_COMMIT_LEDGER.md`
- `docs/design/PHASE12_B_DAILY_V1_REGISTERED_OPERATION_MATRIX.md`
- `docs/design/PHASE12_B_DAILY_V1_OWNER_THREAD_QUIESCENCE_EVIDENCE.md`
- Their independent review records and `PHASE12_DAILY_INGRESS_P14C_PROFILE_REVALIDATION_REVIEW.md`
- `PHASE12_B_TECHNICAL_DESIGN.md`, accepted capability decomposition, Brief/State.

Production already has owner census/revision and mutation epoch, owner-thread
binding, sealed operation scopes, advance lease and direct-clock routing.
`IFactualReadRuntimeState.TryReadCompletedLogicalBoundary` currently returns
CurrentDay after profile/thread/idleness checks. It supplies no successful-core
sequence or capture token and must not be repurposed to imply eligibility.

The remaining work is exactly:
(1) bounded exhaustive reachability evidence; (2) runtime-wide admitted-operation
thread/quiescence evidence; (3) completed-boundary token lifecycle.
No new currently uncovered writer checkpoint is justified. C (evidence only)
cannot close the absent token; D is unnecessary because its accepted runtime
prerequisites exist. B would split one existing coherence guarantee without
a distinct capability dependency. A still has ordered internal gates.

## 2. Exact meaning of exhaustive

Exhaustive is a closed proof over the **supported, admitted Daily-v1 composition
and execution surface at this exact compatible source/configuration**, not over
every public method, arbitrary program, hostile caller or possible object graph.

Use two independent inventories, then reconcile them:

1. **Composition inventory:** enumerate from the explicit profile constructor/
   genesis output graph all authoritative owners and mutable child owners
   actually installed, including empty stores, dormant/unmaterialized identities,
   allocators, receipts, plans and existing NPC Knowledge views. Classify each
   as Required with its exact census contract, ExplicitlyEmpty/excluded with
   tested zero/rejection, immutable/derived with a causal justification, or
   unused deterministic-root state assigned to C with no supported writer.
   Public invisibility or zero at day zero alone proves none of these.
2. **Execution inventory:** enumerate supported root entrypoints and traverse
   all configuration-enabled callees, delegate bindings, owner callbacks,
   event handlers and branches to mutation sites. Record every root's enclosing
   operation, thread gate, changed owner/section union, notification/epoch
   completion, compensation/failure behavior and scope lifetime.
   Start from entrypoints, not from the registered-operation list, so an
   unregistered reachable mutation cannot disappear from the audit.

Close the proof by induction, not enumeration of infinitely many worlds:
the admitted initial graph satisfies the census invariant; every supported
transition either rejects unchanged, commits the complete mapped owner changes
and reconciles dynamic sections before its scope exits, or faults admission
closed after any partial write. For all states reachable by those transitions,
the owners, semantic section IDs/roles, local revisions, and current
`65 + 20*N + U + P` relation remain covered. Include registration/removal,
Person registration/materialization/binding/unbinding, residence/death,
empty/nonempty inventory rows and family reinstallation as supported.
Multiple sections sharing one owner/revision are explicit views, not missing
owners. Fixed 275 is a baseline assertion, not an invariant for every roster.

Each branch/owner is discharged by source-linked reasoning plus representative
boundary/negative tests, including dynamic cardinality and exceptional branches.
Passing a long run, checking a total, or finding no callsite by search alone
does not close the proof. No unresolved 'unknown', 'probably unreachable' or
missing owner/ingress row may remain when production token issuance is enabled.
A missing row discovered inside this boundary requires a bounded correction
under the current contract; it cannot be dismissed as unsupported after discovery.

## 3. Authoritative reachability boundary

The supported boundary is the approved composition and its explicitly
supported runtime operations, subordinate to architecture §§2, 69/D7G, 85,
91–92A and P12's accepted Brief. It is not inferred from mutable Scene contents,
from method visibility or from the formula/registration set itself.

| Root/category | Required classification |
|---|---|
| Dedicated Daily-v1 bootstrap/genesis | Synchronous pre-runtime creation establishes initial truth; after the census baseline, validation/publication tail is scoped through callbacks/finalization. Failed genesis never publishes healthy WorldId or runtime. |
| TesteSimulacao.Start/Update/Simulate and runtime advance APIs | Supported selected-host roots. Direct owned SimulationTime advance dispatches through the same runtime outer operation; no second clock boundary. |
| Explicit supported runtime facades already in the 23-route contract | Include solo travel, travel-party and the supported membership, population, money/market, institution/office, Faction, political claim/support, property/estate and domain-operation forms even if the automatic daily loop does not call them. Each variant and its transitive commits must be mapped. |
| Callbacks/events installed by this profile | Include every synchronously reachable writer, compensation and post-commit notification. A late callback is part of the originating operation until it finishes. No arbitrary new handler/provider is silently admitted. |
| Raw store methods, retained legacy mutable references | Their normal internal use is included when reachable from a supported root. Arbitrary external mutation through an alias is unsupported out-of-bound behavior per D7G, not promised memory isolation. Public API visibility alone does not add an ingress. |
| WorldCommand/ActorChoice UI console and new callbacks/providers | The external queue and separate WorldObserver console are not composed. Reserved public declarations do not create a supported root. Preserve existing composed ActorChoice census/history checks; no stale ActorChoice branch replay or new command ingress. |
| Expedition facade/system | Constructed/exposed but no admitted producer or autonomy root. Prove that from fixed caller/configuration wiring and that supported transitions cannot create Expedition state indirectly; do not activate, register its missing operation or add Knowledge/Expedition export here. It remains P12-F work. |
| P8-B/C/E mutators and P8-D observation/history producers; P10 generation; P14-A/B/C source profiles; military; P18/P20 temporal consumers; mods | Preserve exact-zero witnesses for P8-B/C and P8-D observation/history, and no selected producer for P8-E. P10 generated-site and P14-A/B/current P14-C mixed-source configurations reject before WorldId/runtime construction. Admitted legacy solo/party travel, ExogenousDaily economy and merchant writers remain included. Preserve military zero and temporal-profile rejection. Separate proving profiles or arbitrary external calls are not Daily-v1 roots; populated unsupported state rejects, never gets omitted. |

Source reasoning must document exclusions, not merely rename an unaccounted
supported writer. Existing census detects unexpected changes only to what
its contracts observe; do not promise detection of arbitrary unsupported
unrevisioned alias writes. The host/callback graph and built-in configuration
are closed under the accepted compatibility contract. New root/provider/
feature configuration requires explicit profile revalidation before eligibility;
no generic reflection discovery, security layer or ownership registry.

**Eleven allocator kinds:** retain their explicit no-supported-writer
classification in the reachability report, verifying callers/configuration and
any dynamic creation branch. Full allocator value/export/hydration belongs to
P12-C. Preserve current Event/Decision/TravelParty census notifications.
If evidence actually discovers another admitted allocator writer, its P12-B
invalidation coverage must be addressed before eligibility; absent that
evidence, no extra registration or allocator-state subsystem is justified.

## 4. Runtime-wide quiescence for this profile

Runtime-wide means all operations reachable within §3, not all arbitrary code
that can access the process. Prove:

- The selected host captures the actual Start thread reference AND managed ID
  before genesis. Runtime/protocol construction binds to that captured thread;
  every admitted runtime ingress checks it before its first post-baseline write.
  Pre-runtime genesis writes stay within the private synchronous genesis graph.
- Each §2 execution root remains inside the existing operation accounting
  until its complete write set, deferred within-call callbacks, compensation,
  revision notifications and epoch flush finish. Nested scopes compose;
  scope exit cannot make a still-writing enclosing operation look idle.
- No admitted asynchronous worker, cross-thread mutation, deferred callback
  after scope exit, unscoped domain transaction or pending epoch reservation
  exists outside that accounting. An unknown installed callback is unverified
  composition; it cannot receive a completeness certificate.
- At validation there is no bootstrap publication, advance lease, active owner
  operation/context (including membership/merchant/reserved operation contexts),
  pending epoch reservation or active reentrant capture attempt. Wrong-thread,
  disposal/accounting failure, unobserved revision or protocol fault rejects.
- The healthy AuthoritativeMutationGuard remains the domain integrity latch.
  The P12 protocol's fail-closed status remains admission health; do not use
  the guard as a busy flag, redefine normal rejection as catastrophic fault,
  or add a mutex, global scheduler or second simulation authority.

Reuse ContinuationCensusProtocol and SimulationRuntime's existing lease/context
checks. Add only a narrow read-only pending-reservation/health check if the
current protocol cannot report that condition; do not create a second counter
of domain operations. Tests inspect callbacks from every mapped operation,
not just the day wrapper. Deterministic queries and diagnostics do not commit
or change epochs.

## 5. Token owner, fields and validation

SimulationRuntime owns a small runtime-bound daily eligibility component (or
equivalent private fields). It observes the existing SimulationTime, guard,
admission context and ContinuationCensusProtocol. The protocol remains sole
mutation-epoch and owner-operation accounting authority; no second epoch or
writer registry. The component owns only successful-core sequence and the
ephemeral current token/outer-advance lifecycle.

A token is an immutable in-memory, internally issued capability, not a save,
WorldId, entity ID or domain snapshot. Bind it to:

- an opaque live runtime-instance identity and this admitted profile/composition
  evidence identity; include published WorldId where available, never a new ID;
- final AbsoluteDay and checked monotonic completedCoreSequence;
- the existing committed mutationEpoch;
- immutable current census witness vector, in ordinal section-ID order:
  section/schema/role, exact installed owner identity, cardinality and local
  revision, including dynamic families and required zero witnesses.

Use exact runtime/composition identity and value comparison; no new digest,
hash algorithm or serialization format is required by this token. Reuse the
accepted effective config/calendar/provider/genesis evidence. Domain values
are not exported in B. A narrow internal protocol witness-read method may copy
already validated census values; no Store or mutable dictionary exposure.

Validation returns a typed rejection unless runtime/profile/WorldId publication,
owner thread, guard and protocol health, all operation contexts/reservations,
current day/sequence/epoch and full census vector agree. Re-assess current
inventory/roles, including dynamic membership, rather than accepting 275 by
count. No new token is created by validation, idle polling, factual export,
query or bootstrap. If the epoch changes after a committed supported mutation,
any retained token is immediately logically invalid; a lazy pointer clear is
safe only because every validation rechecks epoch and witnesses. An idle state
after such mutation cannot reissue eligibility.

Do not strengthen FR-B's initial-world coherent factual read into a P12 token
gate: WI-A/FR-B/FR-C/WX-D reads must retain existing supported genesis/day-zero
behavior. Add a distinct P12 eligibility API; existing
TryReadCompletedLogicalBoundary is neither renamed into evidence nor used to
mint a token. Factual read and capture have different coverage obligations.

## 6. Completed advance lifecycle and exact finalization order

This refines the existing P12-B §4 contract for current operation accounting;
it adds no temporal profile.

1. **Initial:** even after healthy genesis/publication, no token and sequence=0.
   Day-zero idle is not a successfully simulated completed day.
2. **Admission:** retain existing argument/clock/profile/thread/lease preflight.
   A zero-day no-op or proven rejection before an advance begins creates no
   token/sequence and does not invalidate an older still-valid token, as the
   accepted lifecycle already specifies. Such token remains attributable only
   to its earlier successful call; a failed return carries no new token.
   Actual protocol/guard faults invalidate eligibility through health, even
   before lease admission. At this source, a wrong-thread positive advance
   calls IsRuntimeAdmissionOwnerThreadCurrent, which faults the census protocol;
   eligibility therefore fails without writing token fields from that thread.
   A pure pre-admission rejection with no state/health change preserves the
   earlier token. A wrong-thread capture check always rejects.
3. **Accepted positive outer call:** acquire one existing advance lease,
   invalidate previous token, and enter runtime.advance-day. No intermediate
   eligibility is observable. Reentrant advances retain existing rejection.
4. **Core success:** increment completedCoreSequence exactly once for each
   private daily core that returns normally/successfully, never per wrapper,
   clock tick or attempted day. Direct SimulationTime dispatch follows the
   same path exactly once. Preflight checked sequence capacity before any new
   core write; overflow rejects without wrap or fabricated boundary.
5. **Batch:** TryAdvanceDays(N>0) has one outer call/lease and N private cores.
   k successful cores followed by false/throw retain those factual completed
   cores and sequence increments; any failed core's early clock/mutations
   remain as actually committed. No rollback and no token for that call.
   Sequence is not inferred from day difference and need not equal AbsoluteDay.
6. **Success finalization:** after ALL requested cores return successfully,
   close/flush the nested and outer protocol scopes FIRST. Keep the existing
   advance lease held. Recheck protocol/guard health, Start thread, complete
   current census, mutation epoch, published profile and all contexts/reservations.
   Protocol active-operation count must be zero. Only this private finalizer
   may disregard its OWN still-held advance lease; every public token/read
   check continues to reject that lease.
   Prebuild the token/witness vector before publication. Publish by the final
   no-fail private assignment inside the lease, then release the existing
   no-callback/no-fail lease and return success. No event, callback, mutation,
   allocation or fallible operation may occur after publication. This avoids
   a read/notification gap without a new synchronization framework.
7. **Failure finalization:** if any core, scope disposal or final assessment
   fails/faults, publish nothing, keep old token revoked and release the lease.
   No token may survive a finally-path after an exception. A failed post-core
   coherence assessment faults P12 admission closed and reports existing
   RuntimeFaulted failure; it does not reinterpret completed truth or set a
   catastrophic domain guard fault. Preserve the original exception on throws.
8. **Later changes:** committed supported writes invalidate via existing epoch.
   Rejected no-write operations/read-only queries do not. Only another fully
   successful positive outer advance can issue a replacement token. A faulted
   runtime/protocol cannot recover by resetting flags or merely becoming idle.

P18 timeline remains excluded. Do not issue this daily token from a P18 logical
tick or change SimulationTime's TimelineProjectionOwnsClock rule. A future
intraday profile must use its own exact completed temporal boundary, due-work,
sealed input ordering and causal state, not infer completion from a day count.

## 7. Reconstruction and downstream ownership

The completed day/sequence and its interpretation must be available for later
C/G continuation boundary evidence; sequence cannot be reconstructed from
AbsoluteDay after partial failure. Preserve configured time/calendar,
WorldId and same-continuation identity/provenance, causal order, current
allocator/record/RNG owner contracts and actual committed state. P13 historical
fork uses new WorldId + provenance and its own reconstructible history gate.

The live token, managed thread, owner object references, operation scopes and
local mutation epoch are admission machinery, not serialized bearer authority.
C–G will privately hydrate compatible semantic boundary/sequence facts and
fresh local census/epoch baselines into a new runtime identity; no old token
becomes valid there. Restored-publication eligibility is outside B and must
be separately proven in G/A. No storage/export/hydration API or save schema
is added to this completion slice.

Passive census for already included Knowledge/ActorChoice/directive owners is
retained and validated. Their complete causal export/hydration, deeper
Knowledge integration and Expedition remain F. P12-C full allocators/RNG/
identity export remains C; B completion does not prove full Save, C readiness
by implementation presence, P12-A parity or P13 reconstruction.

## 8. Implementation surface and ordered gates

**Gate 1 — evidence closure, before enabling production token issuance.**
Master produces a source-linked reachability certificate containing the exact
canonical/config/build baseline; complete installed-owner graph; supported
root/branch/callback closure; 23-operation-to-commit/notification map; dynamic
formula induction; explicit exclusions/eleven allocator classification; and
thread/scope completion proof. Independently review it. A Boolean manually set
by a worker or hardcoded count is not a certificate. Code may assert the
reviewed immutable composition contract; no generic proof engine or registry.
If this audit finds a genuine supported hole, correct it within this bounded
completion before proceeding or report the exact incompatible scope.

**Gate 2 — narrow completion lifecycle.**
Use current source, not stale ActorChoice branches. Expected production files:
SimulationRuntime.cs; a small DailyCaptureEligibility/token value file if
needed; ContinuationCensusProtocol.cs for immutable census/reservation
assessment only; TesteSimulacao.cs only for explicit admission/publication
evidence already required. SimulationTime.cs only if needed to preserve the
existing dispatcher, with no new time authority. Changes to 23 domain
wrappers are unwarranted unless Gate 1 identifies a specific correction.

A concrete internal API can be TryGetCompletedDailyCaptureToken and
TryValidateCompletedDailyCaptureToken with typed failures, private begin/
core-success/finalize helpers and a copied witness vector. No save collector,
thread-marshalling service, global lock or runtime owner registry.
Runtime/clock hooks and protocol finalization are one serialized P12-B hotspot
window; parallel evidence/test preparation may use isolated worktrees.

**Gate 3 — validation and exact-tip implementation review.**
Do not claim B complete from a reviewed design, green existing tests or the
23-operation list. Review full base-to-candidate diff, token ordering and all
failure/reentrancy paths, admission versus guard faults and reconstruction
data. Domain canonical promotion retains its normal human gate.

## 9. Required validation and closure

Focused tests:

- Exact selected composition at initial N=10/U=10/P=0 has 275 sections;
  dynamic register/unregister/materialization and Person lifecycle preserve
  exact provider identity/keys and formula; verify real section rows, not
  just totals. Unsupported populated/excluded state rejects.
- Source certificate independently covers every supported root/branch and
  owner. For all 23 admitted operation routes, exercise successful commit,
  no-write rejection and a mid-scope eligibility probe; prove thread admission,
  nested context lifetime and epoch notification through final flush.
  Use domain-specific representative fixtures, not a new gameplay scenario.
- Bootstrap/idle/day-zero never mint a token; successful day mints exactly one;
  direct owned-clock dispatch does not double count; zero/negative preflight
  cannot manufacture a boundary. Distinguish preserved older token.
- Positive multi-day call exposes no intermediate token; successful batch has
  per-core sequence and one final token. False/throw after k cores, after early
  clock write, late callback/notification or scope close yields no token.
  Test final census failure after cores and checked day/sequence/epoch overflow.
- Wrong-thread admission/disposal, reentrant advance/eligibility, pending
  reservation/context, unregistered operation/provider, missing/changed census
  and guard/protocol fault fail closed. Include a protocol scope that faults
  during Dispose after a successful core; publication cannot precede it.
- Supported same-day owner mutation invalidates old token; no-write rejection
  and diagnostics preserve it; idle never resurrects it. Cross-runtime/
  profile/day/sequence/epoch/owner-revision tampering rejects.
- Capture checks during each stage of finalization reject until success
  returns; all callbacks precede final no-fail publication/lease release.
- Day-zero WI-A/FR-B/FR-C/WX-D remains valid under its existing coherent read
  contract; P18 clock/advance tests preserve excluded-profile behavior.
  Verify sequence independent of day, no entity ID/RNG consumption by tokens.

Required regression gate: affected census/operation/owner suites,
SimulationBootstrapComposition, SimulationRuntimeAdmission, runtime
orchestration, FactualRead and P18 compatibility tests; ALL EditMode and the
complete official Smoke suite. Because advance lifecycle/control flow changes,
run the official runtime LongRun gate and compare authoritative daily outcomes
against the unchanged source fixture. Retain exact source/tree and XML/log
hashes, no skipped relevant cases, independent exact-tip review and diff-check.
No Unity tests are run for this documentation-only preparation.

B closes only when Gate 1 evidence has no unresolved supported rows,
runtime-wide quiescence is proven for that same graph, Gate 2 token code passes
Gate 3 on its exact integrated base, and the normal approved promotion/State
record names P12-B complete. A 275/23 count, pure idle query, manual token or
incomplete-writer certificate is insufficient. P12-A remains WAIT_DEPENDENCY
on C–G exact export/hydration/parity; P13 remains dependency-blocked; no closed
Phase is reopened. B completion unlocks C's capability edge, not full Save.

## 10. Readiness and genuine decisions

Accepted P12-B scope and prerequisite implementation authority already cover
this workflow. After independent technical PASS, it is READY_FOR_IMPLEMENTATION
as one bounded completion candidate with Gate 1 first; it does not assert that
exhaustive evidence or runtime delivery already passed. No new architecture/
product decision or dependency on C/F/P18-D is required. If the supported
reachability graph cannot be closed within the existing profile, report the
specific row/contract conflict instead of silently broadening scope.
