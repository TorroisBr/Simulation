# P18-A Additive Boundary Subphase Extension — Technical Design Proposal

**Design base:** `18ecc6e56d3c6303edfaf8a38257355d262a6ea5` (`codex/phase18/canonical`)
**Architecture baseline:** `c285466c355103d3637ac165246591b72eb7bda0`
**Authority:** `docs/design/PHASE18_A_TECHNICAL_DESIGN.md`, the intraday/extensibility and multi-participant alignment records, `docs/EXECUTION_MODEL.md`, and `docs/phases/PHASE18_BRIEF.md`.
**Status:** Proposed, additive, unaccepted prerequisite to P18-D implementation. This document does not change the promoted P18-A implementation, create a State/closure record, or authorize P18-D implementation.

## 1. Purpose and compatibility boundary

P18-A already defines an atomic typed day-boundary owner operation: the owner commits its effects together with the occurrence identity, and retry resolves that same occurrence. Its current `IDayBoundaryCommit` returns only success/failure. It has no contract for an atomic boundary activation that freezes a multi-step manifest, a separate resumable continuation, or a barrier around ordinary work at that instant. P18-D's daily consumer needs those additive semantics because it coordinates independently owned daily effects without a world-wide transaction.

This proposal preserves the original boundary operation and its identity. A compatible owner may opt into the extension; existing atomic boundary owners and P18-A callers retain their current behavior. The extension is domain-scoped to a selected boundary owner and its declared operation descriptors. It does not generalize transactions across authorities or add a universal workflow/job framework.

## 2. Identities and frozen activation manifest

Boundary activation uses the existing P18-A identity:

`BoundaryOccurrenceId = Encode(WorldId, ProfileId, AbsoluteDay)`

In the same owner transaction, activation commits (a) the boundary effects, (b) that occurrence as consumed exactly once, and (c) either no continuation or a frozen continuation manifest. The original occurrence is never left pending while continuation steps commit. Once activation commits, retrying that boundary resolves the committed occurrence and its exact continuation state; it does not activate again.

The continuation has a distinct stable identity, derived injectively from the boundary occurrence and the selected subphase kind/version, for example:

`ContinuationId = Encode(BoundaryOccurrenceId, SubphaseKind, SubphaseVersion)`

The continuation identity is not the boundary occurrence identity and does not replace it. Each manifest descriptor also carries its own stable step identity, owner identity, operation kind/version, and frozen ordinal. These identities must use the repository's injective length-prefixed encoding (`SpatialStableKey.Encode` or an equivalent versioned encoding), with canonical numeric formatting. Delimiter concatenation, hash codes, collection indexes alone, host object identity, and regenerated random IDs are not valid identity encodings.

The immutable manifest captures all inputs that can change which steps run or their order: ordered typed descriptors; stable owner/domain IDs; compatible owner revisions and operation versions; world/profile and effective configuration/content identity; the relevant PersonId roster and its canonical order where actor work is selected; and each candidate's frozen disposition (included, explicitly skipped under declared profile semantics, or otherwise terminal). Descriptor payloads are data-only and sufficient to resolve the exact owner operation. No callback or delegate is the only representation. A manifest is never recomputed from partially mutated world state.

The roster is an ordered set of stable `PersonId` values only where the chosen domain semantics require person-specific steps. The timeline's scheduled subject remains a domain-owned activity/work identity, not an implicit PersonId. Activity instance identity remains independent of participants; one-participant fixtures do not impose one-to-one Activity/Person cardinality. If the supported operation is not actor-scoped, no synthetic PersonId is introduced.

## 3. Resumable continuation and barrier

The continuation is a separate owner-held, reconstructible state machine keyed by `ContinuationId`. It records the frozen manifest/version, the next unresolved ordinal or equivalent per-step receipts, and its terminal completion state. Each step is attempted in manifest order. An uncommitted failure leaves that step unresolved and leaves the continuation active; retry resolves the same manifest and existing receipts, skips already committed steps, and never rebuilds from current partially changed facts. A committed completion is idempotent and cannot run its effect again.

The timeline processes a due boundary in this order: sealed inputs at the exact boundary instant; atomic boundary activation; the continuation's ordered steps; ordinary due work at that instant; then later instants. When activation yields a continuation, the continuation barrier blocks ordinary same-instant due work, a successful advance beyond that instant, and any P18-C post-advance handoff until the continuation is complete. The advance reports a typed pending/incomplete result (or equivalent resumable status) while retaining `now` at the boundary and retaining undispatched ordinary due work. It must not pretend the advance reached its target. An equal-target retry resumes the same continuation and, after it completes, may drain ordinary work at that instant. No second timeline or reentrant advance is created.

Each owner step commits its domain effect, a durable step receipt/idempotency identity, and the step's source signals atomically within that owner's existing mutation boundary. The stable step identity is based on continuation plus manifest step identity (including frozen ordinal where the owner contract requires it), so a retry cannot duplicate an applied effect or mistake another actor/operation's receipt for this one. Owner ID and compatible revision are checked against the frozen descriptor. A stale or incompatible owner fails closed as a domain result; the manifest is not silently rewritten.

This protocol does not require a global rollback. Earlier committed steps remain committed if a later step fails; receipts make that partial progress explicit and resumable. The per-step atomic write set is limited to the owning domain's effect, receipt, and emitted source signals. Cross-owner orchestration state is the continuation's receipt/progress record, not a claim that all owner stores share one transaction.

## 4. Returned facts and publication boundary

Committed steps may produce timeline facts or source signals. Their effects and owner receipts remain committed during resumable progress, but generated signals are retained durably with their step receipts and are not lost or duplicated on retry. They do not become visible as timeline due-work facts while the outer `AdvanceTo` attempt is incomplete.

The boundary owner/continuation adapter accumulates the complete returned-fact set from committed steps using stable source identities. Only after all continuation steps and all work required by the outer advance through its target succeed may the timeline validate the complete set, allocate provisional causal sequence values, and publish the facts atomically. If validation or the outer advance fails, no partial returned-fact publication or sequence drift occurs; retained signals are available to the next retry. Already committed step effects are not repeated. An implementation may use a P18-A returned-facts/provisional-sequence seam, but must preserve P18-A's rule that sequence allocation is provisional until publication commits.

No owner or step handler may reentrantly register/drain work or recursively advance the timeline. Publication occurs at the outer timeline boundary after successful advance processing, not from inside owner mutation. Ordinary same-instant work remains behind the continuation barrier until completion and then observes the continuation's committed effects and published returned facts according to the declared causal ordering.

## 5. Reconstruction and temporal/cardinality obligations

Reconstruction must recover the exact logical instant and immutable calendar/configuration versions; the pending or committed `BoundaryOccurrenceId`; the distinct `ContinuationId`; the complete frozen ordered manifest and compatible descriptor/owner revisions; roster and frozen dispositions; each committed step receipt and unresolved step; retained source signals and their source-step identities; ordinary due-work references still waiting behind the barrier; accepted/sealed input sequence state; and causal sequence allocator state. Rebuilding an index is permitted, but it must reproduce the same order and barrier.

Temporal identity is the absolute day boundary at the exact `LogicalTick`, combined with stable world/profile identity. A retry at that instant resumes the same continuation; later time cannot pass it. A second activation cannot alias the first day, another profile, or another world. Cardinality is explicit: an owner operation can have zero, one, or many person-specific descriptors, and the frozen manifest records the actual roster/order for this execution. Neither a single-actor example nor a PersonId-bearing step changes ActivityInstanceId's independent identity or imposes a universal participant count. Any fact whose meaning depends on order, roster, disposition, configuration, or already-applied effect is causal reconstruction state, not a derived query.

## 6. Extension seam and non-goals

The additive seam may be expressed through typed optional boundary activation/continuation contracts (or equivalent internal P18-A interfaces): activation returns a prepared atomic commit plus a frozen continuation descriptor; the timeline persists/resolves continuation identity and ordered progress; owners prepare/commit individual steps and return data-only signals; and the outer advance validates/publishes returned facts atomically after success. Exact API names and storage layout remain implementation design, but the atomicity, identity, barrier, and reconstruction semantics above are requirements.

This does not add a generic world transaction, arbitrary plugin callbacks, a public mod API/loader, a universal recurring-process model, a general workflow engine, or gameplay beyond the explicitly selected P18-D consumer. Moddability/extensibility remains a present review constraint: typed semantic descriptors and stable identities must be compatible with future extension without exposing a public registry in P18-A. Phase 19's public extension surface remains deferred.

## 7. Acceptance and dependency

Before P18-D implementation, this additive contract must be accepted through the repository execution model and implemented/reviewed in P18-A (or an explicitly approved compatible integration). The implementation must demonstrate activation retry idempotency; distinct occurrence/continuation/step identities including delimiter-bearing IDs; frozen order/roster/disposition through reconstruction; failure and retry after one or more committed steps; atomic effect-plus-receipt-plus-signal semantics; no ordinary same-instant dispatch, advance beyond the instant, or P18-C handoff while incomplete; complete publication after successful outer advance with no sequence drift on failure; and no reentrant publication. Targeted temporal identity/cardinality tests must include zero/one/multiple actor descriptors where applicable and assert that ActivityInstanceId is not conflated with PersonId. Independent review must compare against the promoted P18-A implementation and both alignment records. Unity validation is required for code changes under the normal P18 gates; this proposal itself is documentation-only.

The proposal is additive and does not invalidate promoted P18-A/B/C. It is a hard technical prerequisite to P18-D implementation, not a prerequisite to P18-D design review or unrelated P20 work. P18-D remains responsible for its selected profile's daily-effect inventory, ordering, consumer semantics, and any separate shared-hotspot ownership window.
