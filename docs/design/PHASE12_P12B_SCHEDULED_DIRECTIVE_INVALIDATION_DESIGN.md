# P12-B ScheduledDirective owner invalidation — technical design

**Status:** Bounded design proposal; independent technical review is required
before implementation.

**Checkpoint:** Accepted P12-B prerequisite capability scope; this proposal
adds no checkpoint ID and does not change P12-A.

**Canonical base:** `54fc23b89cb13598dd184a2b5b6bacf9dc23b0a7`.

## 1. Purpose and contract

The selected `UnityBootstrap-Daily-v1` composition already owns a
`ScheduledDirectiveStore`, its local revision/count witness, and a
`ScheduledDirectiveCensusProvider` exposed by
`SimulationBootstrapComposition`. The selected P12 runtime currently does
not register that provider with its census protocol or bind supported store
commits to the partial mutation epoch. This design connects the existing
owner to the existing P12 protocol without rebuilding the passive census or
adding a directive operation or gameplay behavior.

The required owner section is the existing
`p12f.scheduled-directives`, schema 1, owned by the exact store installed in
the published composition. Cardinality counts every stored row, including
terminal rows; local revision advances once per successful Add or terminal
state commit. P12 reports each committed owner revision through the current
mutation-section dispatcher. A standalone commit notifies the protocol
immediately. If an existing TravelParty, Merchant, or solo-travel operation
batch is active, the changed section is coalesced into that operation and
accounted at its outer boundary. This does not claim that the whole directive
action or daily advance is one atomic P12 operation.

## 2. Source-grounded mutation boundary

`TesteSimulacao` creates scheduled directives during authored genesis, creates
their `ScheduledDirectiveSystem`, and passes that system to
`SimulationRuntime`. The same store is later installed in
`SimulationBootstrapComposition`. Genesis Add operations occur before the
runtime's initial census baseline; their final count/revision is part of that
baseline and must not emit synthetic post-baseline notifications.

After the runtime baseline is established, supported store commits are:

- `ScheduledDirectiveStore.Add`, including its existing single-commit path
  that inserts a row and may immediately mark it Skipped for an invalid or
  overdue schedule; and
- stored-row `MarkSucceeded`, `MarkFailed`, or `MarkSkipped`, including
  transitions made by `PrepareDay` for duplicate targets/unresolved actors
  and transitions made by normal actor-turn processing.

`TryTakeDirective` changes only the system's transient actor lookup and does
not change the stored row count or local revision. Preparation/query paths
with no successful status transition also remain non-mutating. Preserve all
existing schedule selection, action execution, terminal status, reason,
logging, return, and exception behavior.

## 3. Runtime registration and exact ownership

During selected-profile P12 census initialization, obtain the store from the
installed `ScheduledDirectiveSystem` (a read-only internal `Store` property is
the smallest exposure). Construct or reuse a provider for that exact store,
validate section ID/schema, nonnegative dynamic cardinality, owner reference,
and revision, then register `p12f.scheduled-directives` as Required in the
sealed protocol. A missing system/store or mismatched witness faults selected
P12 admission closed. Non-P12 runtimes retain their existing behavior.

`SimulationBootstrapComposition` already creates a provider from its
`directives` argument. Add a runtime ownership check, patterned after the
existing record-sequence and allocator checks, so the published composition
proves its provider samples the exact store registered by the runtime. Do not
create a second store or discover ownership through reflection.

When the selected P12 runtime binds owner mutation callbacks, bind admission
and post-commit notification to the same exact store. Reuse the existing
`CanCommitP12MutationSections` and `NotifyP12MutationSections` paths, which
already route a changed section into an active TravelParty, Merchant, or
solo-travel batch where applicable, and otherwise notify the protocol
directly.

## 4. Commit protocol

Keep local revision/cardinality changes under the existing
`ScheduledDirectiveStore` owner monitor. Add a P12 mutation-boundary callback
pair on the store, bound once to the selected runtime, following the current
store-level admission/committed callback pattern.

For each supported write:

1. Run the existing guard, identity/ownership, duplicate/state/day, and local
   revision-capacity checks without changing owner-visible state. For Add,
   defer binding the incoming directive to the guard/store until after P12
   admission, so a rejected preflight leaves even that incoming object
   untouched.
2. If P12 callbacks are bound, preflight owner thread, exact store/provider
   identity, current section count/revision, unchanged-section baseline, and
   mutation-epoch capacity before changing membership or row state. A failed
   P12 preflight faults the protocol and must propagate as an
   `InvalidOperationException` before any directive binding or owner commit.
   This propagation is required because current `PrepareDay` and actor-turn
   callers ignore the boolean result of `MarkSkipped`/`MarkSucceeded`/
   `MarkFailed`; returning `false` alone could let the daily advance report
   success. The runtime's existing selected-P12 advance catch faults and
   rethrows the failure. Do not catch or convert it to an ordinary directive
   rejection. With no P12 callback bound, preserve the existing non-P12
   boolean behavior.
3. Apply the existing owner commit. For Add-with-immediate-Skipped, insertion
   and initial status remain one store commit and one revision increment.
4. After the revision increments, verify the exact live witness and notify
   `p12f.scheduled-directives` through the current P12 dispatcher. A
   notification failure after commit faults the runtime and throws, matching
   existing committed-owner notification behavior.

Before runtime callback binding, Add continues to build genesis state and its
final witness becomes the initial baseline. After binding, a supported Add
uses the same preflight/commit/notify sequence. Every successful owner commit
advances the local revision once. A standalone commit is notified immediately;
commits inside an existing batch are coalesced by changed section and accounted
once when that batch closes. Repeated, rejected, exhausted, or transient lookup
paths do not notify. A P12 preflight denial is exceptional and propagates; it
is not converted to a normal `false` result.

If terminal-state admission fails after the directive action or other daily
work has already committed, those earlier domain effects are not rolled back.
The selected P12 advance faults and aborts by propagating the admission
exception; this slice does not add an action-plus-directive transaction or
claim daily atomicity.

The store monitor protects its own local census coherence. It is not a new
runtime lock, owner-thread guarantee, or quiescence proof. Existing P12 owner
thread and protocol admission checks remain the authority for the callback.

## 5. Intended files and integration order

Expected executable changes are limited to:

- `Assets/_Project/Scripts/ScheduledDirectives.cs` — expose the installed
  system store and bind P12 admission/commit callbacks at the existing owner
  commit boundary;
- `Assets/_Project/Scripts/SimulationRuntime.cs` — register/validate the
  required exact provider, bind the store, and implement focused preflight,
  owner revalidation, and post-commit notification helpers;
- `Assets/_Project/Scripts/SimulationBootstrapComposition.cs` — assert the
  published provider matches the runtime-registered store;
- focused ScheduledDirective/P12 EditMode tests.

Reuse the canonical store monitor/revision/provider implementation and
current runtime callback patterns. Do not cherry-pick or restart the old
store-local census branch; its store-local implementation is already in this
base. Keep P18 temporal ActorChoice, `SimulationRuntime` temporal execution,
and unrelated operation owners untouched. The main hotspot is
`SimulationRuntime.cs`; serialize its implementation/integration work.

## 6. Required validation and completion boundary

Focused tests should prove exact runtime/store identity, required-section
registration for the selected P12 profile, genesis baseline after authored
Adds, one preflight and one revision change per successful post-bind Add or
terminal transition, Add-with-immediate-Skipped as one commit, and unchanged
count/revision/epoch for failed/repeated operations. Verify that standalone
commits notify immediately and that commits within an existing operation batch
are coalesced at its outer boundary. Cover `PrepareDay` conflict and
unresolved-actor skips, normal success/failure terminal paths, and
`TryTakeDirective` remaining transient. Exercise stale owner/revision and
epoch-capacity rejection before any directive binding or store mutation;
assert that rejected `PrepareDay` and actor-turn terminal commits propagate
through selected-P12 daily advance instead of reporting success. Also cover
Add denial, post-commit notification failure faulting the runtime, and
unchanged non-P12 boolean behavior.

Then run affected runtime/admission and bootstrap suites, ALL EditMode,
official Smoke, and `git diff --check` on the exact code tree. Record artifact
hashes and obtain independent exact-tip implementation review before
autonomous promotion preflight.

## 7. Exclusions and limitations

This is only selected-profile ScheduledDirective owner invalidation in P12-B.
It does not add directive types or change gameplay; group directive effects
into a new operation; cover unrelated NPC/action owner writes; bind the P18
temporal profile; establish complete owner or shared-epoch coverage, global
quiescence, or capture eligibility; add export/hydration; make P12-A ready;
complete P12-B; unblock P13; or close Phase 12.
