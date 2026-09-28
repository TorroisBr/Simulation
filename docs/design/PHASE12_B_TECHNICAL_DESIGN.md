# P12-B — Profile Admission and Completed-Boundary Eligibility

**Status:** Independent technical design review PASS. This documentation-only
design is based on planning commit
`36618a801607656e339110b683bc6a666a26aeeb`; this document defines a technical
boundary within that scope. It does not deliver code, start P12-B
implementation, or establish P12-A readiness. Its listed composition,
serialized-runtime, and complete mutation-census dependencies still block
P12-B implementation.

**Canonical references checked after remote refresh on 2026-09-28:** architecture
`c285466c355103d3637ac165246591b72eb7bda0`; P8 `470667d37863384edadb3d93ef64d8004aff46a3`;
P9 `82396ae7ffaf407fda278928da456b06dc5394d4` (P9-B code integration
`d9a62d7c6bea242653c2d68cc0a70911bb5ed1bf`); P11
`308e24d0744112e8f2b741521b8b3e4acb51ebbf` / code
`0cd4281804ecc6a2d110352d1a238959e93867f0`; P14
`4caecbbfb0464c965811402b3c11d8717605114a`; P18 canonical code
`9e790c59e14ca7f7ed195c0e6267e10f3cd039d7` (its State still has stale
wording that the promoted D1/D2 prerequisites are missing); P20
`7a81cc0ecbc511dd36c248ec62c7b20f7e477f53`; and both current architecture
alignment records. The P12 planning base is `36618a8`; P12-A remains
`WAIT_DEPENDENCY` and P12-C through P12-G remain downstream.

## 1. Purpose and boundary

P12-B establishes two things for the accepted `UnityBootstrap-Daily-v1`
profile:

1. Whether a runtime is the exact supported bootstrap/provider composition
   admitted by this profile.
2. Whether a particular capture request refers to the latest successfully
   completed daily boundary, on the owning simulation thread, while the
   runtime remains quiescent, healthy, and unchanged since that boundary.

P12-B does not serialize owner state, create an envelope or save catalog,
export or hydrate domain stores, validate the complete restored graph, or
publish a restored runtime. Those capabilities belong to P12-C through P12-G
and the final P12-A integration. A P12-B admission result is not proof that
every owner is covered.

## 2. Current composition evidence and blocking mismatch

The selected Unity scene is `Assets/Scenes/SampleScene.unity`; its serialized
`TesteSimulacao` reference selects `Simulation-GeneralTest.asset` (asset GUID
`ba87bf49ee034da6bda3daeef8e40c3f`). The asset contains authored module,
City, NPC, action, job, status, inventory, and other bootstrap inputs. The
asset's module flags are inputs only: `EffectiveSimulationConfiguration`
after normal resolution determines the daily policies and providers.

In the inspected `TesteSimulacao.InitializeSimulation` path, initialization
resolves calendar/configuration, creates `SimulationTime`, the deterministic
random provider, runtime-ID allocator and record sequence, stores/recorders,
legacy spatial network, Cities, sites/routes and NPCs, then builds the
configuration-selected systems, bootstraps initial Knowledge/justice state,
and constructs `SimulationRuntime`. These source facts identify where P12-B
must obtain composition evidence; they do not by themselves prove an accepted
P9-B/P11 combined profile.

**Candidate prerequisite, not current live evidence:** the fresh source audit
reports that P9-B authored-geography code `00395ef` runs the P9 genesis path,
while P11 code `0cd4281` composes `ActorChoiceStore` without P9 genesis; neither
is an ancestor of the other, and their common base is P8 canonical `470667d`.
An additive application-level composition candidate is being validated to
combine these two existing capabilities as the normal accepted P12 profile.
Until that candidate and its bootstrap tests pass, no branch is evidence of a
live combined composition and the manifest's exact required provider/section
set cannot be finalized. This design treats the composition candidate as a
hard dependency, not as an already delivered or promoted runtime.

The accepted P12-A profile remains unchanged. Do not conditionally accept
either half as `UnityBootstrap-Daily-v1`, synthesize an ActorChoiceStore, omit
P9-B provenance, or add an external `WorldCommand` queue. After candidate
validation, refresh the live composition/provider inventory against the exact
combined source and record whether the existing P11 store is composed. P12-A's
P11 causal-state rule applies where that selected runtime composes
ActorChoiceStore; external queue composition remains excluded. Admission must
reject until the combined candidate is validated and its exact composition is
known.

## 3. Admission manifest and owner boundary

The admission result is an immutable in-memory profile manifest assembled from
bootstrap-owned evidence after the selected bootstrap finishes composition
and before a capture token may be issued. It contains stable values, not Unity
object references or the mutable asset objects themselves. The manifest is
bound to the runtime instance and rejects unsupported or ambiguous composition
before it can be labeled eligible.

Required manifest inputs:

- Profile ID/revision and envelope/profile schema identity for
  `UnityBootstrap-Daily-v1`.
- Selected entry path and authored configuration identity: scene/config asset
  identity plus deterministic content fingerprint for every behavior-bearing
  selected definition reachable from the profile (Cities, sites, NPCs,
  actions, jobs, statuses, item definitions, and configured directives as
  applicable). The authored asset's name or Unity object reference alone is
  insufficient.
- Simulation build identity, Unity/runtime identity, and the accepted
  current-host numeric execution profile.
- Resolved immutable `EffectiveSimulationConfiguration` identity and
  `SimulationCalendar` identity/version. Hash the resolved values using the
  accepted canonical length-delimited encoding; do not use only requested
  module flags or a preset label.
- Actual instantiated built-in provider/system identity and compatible
  version set, derived from the completed runtime composition. Include the
  selected P9-B profile/schema identity and genesis-profile mapping as
  admission evidence, without exporting or regenerating its facts in P12-B.
- Versioned live section-census evidence supplied by each actual owner:
  stable section/owner contract identity, schema/version, runtime owner
  instance identity, required/explicit-empty/excluded role, exact current
  cardinality including an explicit zero, and that owner's current mutation
  revision. This is admission/emptiness evidence, not exact owner export or
  hydration.
- A current composition contract revision so a changed bootstrap/provider
  graph cannot be treated as the old profile by matching the asset alone.

The bootstrap/composition owner supplies the provider graph. Each actual
domain owner supplies the versioned live section census from its own authority;
the coordinator seals both into an immutable manifest and boundary witness.
Every section is declared `Required`, `ExplicitlyEmpty`, or `Excluded`. For an
excluded section, the owning store must report a known exact cardinality of
zero at the admitted boundary. A missing section/owner report, unknown owner
revision, unknown cardinality, or unsupported census version fails closed.
At each candidate boundary, owners provide a fresh census or an owner-issued
unchanged-revision witness; the coordinator rechecks the same evidence after
the later P12-G collector runs. A changed revision, increased excluded count,
or absent report rejects and discards that capture candidate. These census
records do not contain owner truth and cannot stand in for P12-C through
P12-F export/hydration.

The profile-admission service compares the values against the accepted profile
contract; it must not discover providers by reflection, guess from asset
flags, or infer that a store is present from a diagnostic projection. Where
the exact provider graph, required section set, or live owner census is not
demonstrated by the refreshed inventory, the result is
`UnsupportedOrUnverifiedComposition`, not a partial manifest.

P12-B admission explicitly rejects a different profile/configuration,
P9-A-only genesis profile, incompatible build/runtime/numeric profile,
unknown or injected provider composition, and external WorldCommand service or
queue. An allocated canonical P8-B through P8-E store is not by itself
incompatible; its owner must report an exact zero cardinality and current
revision for the corresponding excluded section. A populated or unverified
excluded section rejects admission. Likewise P10, P14-A, P18, P19, and P20
state is outside this profile; an owner census must prove excluded state
absent or admission rejects. This evidence is only a fail-closed census;
P12-G still validates complete owner truth and the staged graph. Generated-
world content, P13 reconstruction/fork guarantees, and cross-host/migration
behavior stay outside the manifest contract.

### Proposed narrow API seam

Names are illustrative and remain subject to repository naming conventions:

- `IContinuationProfileAdmissionSource` (owned by the bootstrap/composition)
  produces one immutable `BootstrapProfileEvidence` from the actual selected
  config, resolved effective config/calendar, genesis profile evidence, and
  constructed runtime provider set. Each store/provider owner also registers
  an `OwnerSectionCensusProvider` with an explicit schema version and
  role/cardinality contract.
- `ContinuationProfileAdmission.TryAdmit(evidence, supportedProfile, out
  manifest, out failure)` performs exact comparison and returns either a
  sealed immutable `AdmittedProfileManifest` or a typed rejection. It has no
  mutation authority over the world.
- The runtime-bound `DailyCaptureEligibility` coordinator owns the per-day
  successful-core sequence, outer-call state, mutation epoch, owner-operation
  registry, and census completeness certificate. It publishes an ephemeral
  token only after the complete outer advance call succeeds. Validation
  compares runtime identity, manifest fingerprint, absolute day, sequence,
  mutation epoch, owner thread, mutation health, active-operation state, and
  every registered owner census/revision.
- `SimulationOperationScope` (or an equivalent owner-approved seam) binds
  the actual simulation owner thread during profile bootstrap and tracks
  active synchronous bootstrap, advance, supported command, and transaction
  scopes. It is an operation-accounting contract, not a lock or cross-thread
  synchronization promise. Its caller inventory must be explicit and
  exhaustive for the admitted profile.

No serialized token is proposed. The eventual P12-G envelope records the
completed day/sequence as causal boundary data; P12-B's in-memory runtime
identity only prevents a token from being reused with another live runtime.

## 4. Completed-day token lifecycle

Each composed runtime has a fresh opaque runtime-instance identity, a
monotonic completed-core sequence, a capture-mutation epoch, and an outer
advance-call state. The sequence is scoped to that runtime lifetime and is
incremented once each time the private daily core returns normally. The
sequence advances per completed core even inside a multi-day request, but no
capture token is visible while the outer public `TryAdvanceDays` call is
active. The epoch is advanced after every supported authoritative mutation
that commits after a boundary. Neither counter is allocated from or
substituted for a semantic entity ID.

State transition:

1. **Before an outer call begins:** after argument/overflow preflight and
   acquisition of the per-runtime advance lease, invalidate the old token and
   mark the outer call active. A zero-day no-op does not create a boundary or
   invalidate an otherwise-current token. A rejected input that fails before
   advance begins likewise makes no truth change. No time increment alone
   constitutes completion.
2. **During an outer call:** the prior token is unavailable. `TryAdvanceDay`
   executes one daily core under the lease. `TryAdvanceDays(N)` acquires the
   lease once and calls a private daily core for each requested day; it does
   not publish intermediate tokens or reenter a public wrapper. Each private
   core that returns normally increments `completedCoreSequence` by one.
3. **Whole-call success:** after every requested core completes normally and
   the public outer method is about to return success, publish exactly one
   token for the final `SimulationTime.AbsoluteDay`, the final completed-core
   sequence, current mutation epoch, profile fingerprint and runtime
   identity. Token publication is the final non-failing step while still
   inside the lease; release the lease and return success without a gap that
   permits an intervening write. A single-day call follows the same rule with
   one core.
4. **Partial failure:** if `TryAdvanceDays(N)` completes `k < N` daily cores
   and the next core returns a typed failure or throws, sequence remains
   incremented for those `k` normally completed cores only. The simulation
   clock and truth retain whatever the failed core actually changed; P12-B
   performs no rollback. The old token remains invalid, no token is published
   for this outer call, and the lease is released through its failure path.
   The next successful outer call may publish a new token bound to the actual
   absolute day and monotonic completed-core sequence; the token does not
   imply `absoluteDay == sequence` or that every intervening attempted core
   succeeded. If mutation health is faulted, capture remains rejected until a
   new supported healthy runtime exists. No token is published for an
   exception, even when the clock already advanced.
5. **After a supported authoritative write:** the committing owner notifies
   the eligibility coordinator, which increments the mutation epoch and
   invalidates any current token. Failed preflight that made no mutation does
   not invalidate. A later fully successful outer daily call issues one new
   token.
6. **Capture eligibility check:** accept only on the bound simulation owner
   thread, when no outer advance or registered owner operation is active, the
   mutation guard is healthy, the same manifest is admitted, every required
   owner census is registered/current, every excluded section has a current
   explicit-zero census, and the token's runtime/day/sequence/epoch match.
   P12-G rechecks token and owner revisions after it gathers owner sections;
   any change discards its candidate. P12-B does not collect or seal those
   sections.

The inspected `SimulationRuntime` currently exposes mutation health through
`AuthoritativeMutationGuard`, but no owner-thread identity or general active
operation registry was found. That guard is a health/fault latch, not a busy
flag, mutex, thread verifier, or complete mutation revision source. The
reviewed P18-D2 lease separately tracks only its guarded advance calls. P12-B
must therefore bind the actual bootstrap thread and add narrow operation-scope
accounting for every supported synchronous entrypoint found in the refreshed
profile inventory. The scope counter answers only whether an admitted runtime
operation is in progress; it must not be described as making stores
thread-safe. Off-thread work is outside the profile and cannot be accepted.
If the supported command/transaction entrypoints cannot all be identified or
wrapped, capture eligibility fails closed.

Do not overload `AuthoritativeMutationGuard` health state to mean capture
eligibility. Use a separate eligibility epoch/coordinator. B's reviewed API
contract establishes an expected owner-section set from the live inventory,
and its token validator fails closed until every expected P12-C/D/E/F owner
registers its versioned census and committed-mutation notification. The
invalidation API is B's prerequisite contract; owner-specific notification
delivery belongs to the checkpoint that owns those stores. Therefore B does
not claim complete all-owner stale-token rejection before those integrations.
The dependency is non-circular: C/D/E/F may prepare isolated adapters against
the exact reviewed B API contract; canonical integration uses B's promoted
kernel/API, and end-to-end eligibility evidence is recorded only after all
owner adapters exist and P12-G validates the whole capture. A missing adapter
means `OwnerCoverageIncomplete`, never a permissive token. Diagnostics-only
reads do not invalidate a token.

## 5. P18-D2 shared `SimulationRuntime` hotspot

P18-D2 technical design review passed at `6f82bf4`; its implementation
`ea7b3e7` and the economy receipt/prepared-install prerequisite are promoted
at P18 canonical code tip `9e790c5`. P18-D consumer integration is active on
the unpromoted feature candidate `a9428a3` and currently owns the shared
runtime hotspot. D2's scope is the per-runtime non-reentrant
lease around the currently composed legacy `TryAdvanceDay` and
`TryAdvanceDays` APIs and the narrow future owner seam. It does not compose the
P18 timeline, boundary chronology, subphases, or successful P18-C handoff;
those remain P18-D integration obligations. The lease is not a general
thread-safety guarantee. The promoted D2 API owns the advance-lease seam; the
active P18-D consumer integration owns the current `SimulationRuntime.cs`
editing window.

Therefore:

- P12-B design introduces no second lease, lock, universal busy framework, or
  P18 timeline composition.
- Do not edit `SimulationRuntime.cs`, its public advance wrappers, or the
  lease seam while P18-D consumer integration has the exclusive editing
  window. This P12 branch is documentation-only and makes no code change.
- Before P12-B implementation touches the runtime, P18-D must finish its
  current consumer integration and explicitly hand over the exclusive runtime
  hotspot. D2 already passed review/validation and is canonical at `9e790c5`;
  revalidate this design against that promoted API. P12-B then uses that same lease to ensure capture cannot
  observe an in-progress legacy advance; it must not nest or wrap the lease
  in a way that changes `TryAdvanceDays` behavior.
- P12-B's success-boundary callback and mutation-epoch seam are separate
  responsibilities from the D2 lease. Coordinate their insertion at the
  same `SimulationRuntime` boundary owner after D2; do not release the lease
  between daily advancement and a boundary callback that forms part of the
  admitted daily step.

P18-D2's lease currently protects only the legacy calls. If P12-B is integrated later
with a chronological P18 runtime, P18-D must extend its lease continuously
through the outer timeline advance, continuation barriers, all due boundary
subphases/work, and successful P18-C handoff; P12-B does not add that path.
This is a hotspot serialization/implementation-order constraint. It does not
create a new semantic dependency from P12 to P18 or claim P18-D2 is promoted.

## 6. Files, ownership, and integration order

**P12-B design/implementation ownership after the blockers clear:**

- `Assets/_Project/Scripts/TesteSimulacao.cs`: provide selected bootstrap
  evidence only after normal initialization has resolved configuration and
  constructed the actual providers. No P9 generation or P11 state is
  synthesized here.
- A new narrowly scoped admission/eligibility implementation under a P12
  continuation namespace (exact path to follow repository conventions):
  immutable manifest/evidence values, typed admission results, runtime-bound
  eligibility state, token validation and mutation notification interface.
  This is lifecycle admission, not the P12 envelope/store serializer.
- `Assets/_Project/Scripts/SimulationRuntime.cs`: one success-boundary hook
  and delegation to the eligibility coordinator, only after P18-D consumer
  integration completes or explicitly hands over this hotspot. Keep existing `AdvanceDay` result,
  error, order and `TryAdvanceDays` behavior.
- Existing included mutation owners: integrate notifications after successful
  authoritative commits. The exact owner/file list is blocked on validation
  of the P9-B/P11 composition candidate and a refreshed live profile/provider
  inventory; do not infer a complete list from guard bindings alone.
- Focused EditMode tests beside bootstrap/profile admission and runtime
  orchestration tests; no diagnostics snapshot becomes a save contract.

Integration sequence:

1. Validate the additive P9-B/P11 composition candidate (`00395ef` plus
   `0cd4281`, common P8 base `470667d`) and its bootstrap tests. Then refresh
   the live read-only composition inventory against that exact candidate tip.
   Without a passed candidate and inventory, the admission manifest cannot be
   finalized and P12-B implementation is blocked.
2. P18-D2 design and implementation are promoted at `6f82bf4` / `9e790c5`.
   P18-D consumer integration currently owns the runtime hotspot; require its
   candidate validation and explicit owner handoff before opening the P12-B
   runtime owner window. Revalidate P12-B seams against the promoted D2 API.
3. Implement immutable admission values/source and rejection behavior without
   serializing owner state. Implement the runtime boundary/token hook using
   the single D2 lease contract. Instrument mutation paths only after the
   live inventory assigns their owner checkpoint; the eligibility kernel
   remains fail-closed until every expected owner adapter is present.
4. Independently review the full candidate, especially profile mismatch and
   fail-closed behavior, hidden mutation paths, advance ordering, and shared
   hotspot scope. Run focused and required regressions after implementation;
   this technical design makes no test-result claim.
5. Integrate/publish P12-B only through the normal human promotion gate. P12-C
   may use its manifest/token contract once promoted; P12-D/E/F remain
   responsible for complete owner exports/hydrators and mutation notifications
   for their owners. P12-G validates token stability over a complete capture.

P12-B may not claim profile readiness from its own tests. P12-A remains blocked
until exact included-owner coverage, current profile inventory, and its
separate implementation authorization are complete.

## 7. Validation design

The tests below are implementation requirements, not executed results.

**Profile admission tests:**

- Admit only the selected SampleScene/config/profile combination after
  composition evidence matches. Reject wrong asset/profile revision,
  incompatible build/runtime/numeric identity, changed behavior-bearing
  content, effective configuration or calendar, P9-A-only profile, unknown
  provider/version, externally injected provider, and mismatched genesis
  profile/schema.
- Test exact module flags versus resolved `EffectiveSimulationConfiguration`
  so requested module lists are never treated as the effective provider set.
- Reject external `WorldCommand` queue/service. For excluded P8-B..E,
  P10/P14-A/P18/P19/P20 state, reject any populated or unverified section;
  a known-empty default service is not itself a rejection. Reject an ambiguous
  or incomplete owner/provider inventory rather than admitting a partial
  profile.
- Explicitly test the known P9-B/P11 composition gap: until the additive
  source candidate has passed its bootstrap tests and its exact provider graph
  has been inventoried, incomplete variants return
  `UnsupportedOrUnverifiedComposition`. Do not create a test that silently
  selects one half as the supported profile or treats the unvalidated merge as
  current live composition.

**Boundary and token tests:**

- A successful one-day outer call increments the private daily-core sequence
  once and publishes one token for its final day. `TryAdvanceDays(N)` holds one
  outer lease; each normally completed daily core increments the sequence,
  but no token is observable while the outer call is in progress. Only after
  all N cores succeed does the outer call publish one token for the final day
  and sequence. Test that a reentrant eligibility check from any daily
  callback cannot observe an intermediate token. A zero-day no-op does not
  create a boundary or invalidate an otherwise-current token.
- Invalidate the prior token once a valid outer advance begins. On partial
  `TryAdvanceDays(N)` failure after k normally completed cores, the sequence
  advances by exactly k; the clock remains at its actual resulting absolute
  day (which may include the failed core's early clock increment); no token is
  published and the lease is released. Assert that sequence is not inferred
  from absolute-day difference. A later successful call may issue one token
  bound to the then-current day and monotonic sequence. A throw after clock
  increment behaves the same with respect to token withholding; a faulted
  guard continues to reject. Failed preflight before an outer call starts and
  zero-day no-op do not increment the sequence.
- Test direct `TryAdvanceDay`, the `AdvanceDay` throwing wrapper, and
  `TryAdvanceDays` against the D2 lease so nested wrappers never double-count
  a completed core. Test that every success/failure/exception path releases
  the single outer lease.
- After a token is issued, successful direct supported authoritative mutation
  invalidates it; a failed preflight with no write does not. Enumerate all
  mutation families from the refreshed live profile inventory; every included
  owner must call the committed-mutation notifier. Missing an owner adapter
  leaves the coverage certificate incomplete and returns
  `OwnerCoverageIncomplete`, rather than granting eligibility.
- For every live section census provider, test schema/revision matching,
  concrete owner identity, exact positive counts for populated required
  sections, and explicit zero counts for empty/excluded sections. Missing,
  unsupported, stale, duplicate, or contradictory census evidence rejects.
  Populate an excluded section after an empty witness and verify eligibility
  fails before envelope collection; mutate a required owner and verify the
  boundary witness becomes stale. Test census revalidation after the eventual
  collector completes.
- Bind owner-thread identity when profile bootstrap completes. Accept capture
  only on that exact thread. D2's advance lease is not the active-operation
  authority for non-advance work: wrap every supported command, transaction,
  and bootstrap operation in its registered operation scope. A capture
  attempt during any such scope or a reentrant callback rejects. Missing or
  unknown operation coverage fails closed; do not assert thread safety.
- Reject capture from a different runtime, changed manifest, wrong absolute
  day, stale sequence, stale mutation epoch, faulted runtime, active outer
  advance lease, active registered operation, or incomplete owner census.
  Capture/revalidation with no intervening write remains eligible only when
  every expected owner has supplied its current witness.
- Diagnostic and read-only queries leave the token valid. Repeated
  invalidation is monotonic and cannot resurrect a prior token; only a later
  fully successful outer daily call can publish a fresh token.
- Validate token fields and sequence are runtime-lifetime causal identity,
  not entity IDs or a persistent save record. P12-B does not test save/load
  parity or claim reconstruction closure.

## 8. Reconstruction sensitivity and exclusions

The admitted profile manifest and boundary identify the exact runtime
composition, content/provider/config/calendar compatibility, and completed
daily boundary at which later P12 owner sections are captured. The eventual
continuation must preserve absolute day, completed advance sequence, effective
configuration/calendar, profile and provider identities, and all causal owner
state. P12-B holds only the live eligibility token and invalidation epoch;
they are not sufficient reconstruction state and do not replace P12-C's IDs,
genesis provenance, random state, or P12-D/E/F owner sections.

P12-B does not add P18 intraday state, P19 module/loader state, P20 shared
activities, P13 replay/fork behavior, generated P9/P10 content, unconfigured
P14-A material flow, P8-B..E state, or external WorldCommand queues. P8-A and
P9-B evidence enters only as exact selected profile admission identity and is
exported/hydrated under P12-C. Legacy spatial/site state belongs to P12-D.

## 9. Readiness and unresolved gates

This design is bounded to accepted P12-B scope, but P12-B is **not**
`READY_FOR_IMPLEMENTATION` on this evidence:

1. Validate the additive P9-B/P11 composition candidate and its bootstrap
   tests; then refresh the live profile/provider/owner inventory against the
   exact passing tip. Until then this design does not identify an already-live
   combined runtime.
2. P18-D2 technical review passed at `6f82bf4` and implementation is
   underway. Its accepted scope guards only the currently composed legacy
   `TryAdvanceDay`/`TryAdvanceDays` calls. Require its candidate validation,
   canonical promotion and explicit hotspot handoff before P12-B touches
   `SimulationRuntime`; P18-D still owns the future chronological
   continuation/subphase/handoff window.
3. Demonstrate the complete versioned owner-section census and
   mutation-notification inventory. P12-B's eligibility kernel must fail
   closed until P12-C/D/E/F owners register their required adapters; P12-G
   provides whole-capture validation. If any supported included mutation can
   bypass invalidation, eligibility cannot be claimed complete.
4. Inventory the actual bootstrap thread and all supported active operation
   entrypoints, then prove capture rejects a different thread or any
   in-flight scope. D2's advance lease alone is not sufficient evidence.

P12-C through P12-G remain downstream. P12-A final profile integration remains
`WAIT_DEPENDENCY`; accepted P12-A scope and P12-B technical design are not
implementation authorization, delivery, canonical promotion, or proof of
save/load.
