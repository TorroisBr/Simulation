# P12-B selected-profile legacy Crime/Justice invalidation — technical design

**Design state:** TECHNICAL_DESIGN_IN_PROGRESS; independent review is pending. This is a proposed P12-B prerequisite sub-slice, not a new checkpoint ID and not implementation authorization beyond the already accepted P12-B–G capability work.

**Exact P12 base:** 39e275f39e1602d3fa109d3a1bb9acd60585a3f2
**Architecture authority:** codex/architecture/world-identity-projection at ffd75652d89d862b83d634868c560f8540869b89
**Alignment authorities:** intraday/extensibility at 4b6dd1d38cffeaf3cc1ac3effea0f8ede8771194; multi-participant activities at c285466c355103d3637ac165246591b72eb7bda0

## Purpose and scope

After the population lifecycle promotion, the Phase 12 State and blocker matrix still name direct NPC MoneyAccount/Inventory invalidation as the next work. That statement is stale. Code commit c49f957e45c3059231e9ec66e4010a7c3a389988 is an ancestor of the current P12 canonical and its code tree is 627af2fbd7f93e0025106ee5ba87e72bae6c4ed2. The older remote implementation ref 766137aea8535c1d8f6e5529856228db6890e718 has the same tree, so it is retained historical evidence and must not be re-delivered.

The highest-impact remaining selected-profile gap supported by the current source map is the legacy daily Crime/Justice mutation path. GeneralTest enables Crime and GuardCrime, and the runtime advances already-established hidden state and Justice records every day. This design covers only the existing selected-profile owners and mutations described below. It adds no crime, arrest, escape, or other gameplay rule.

P12-B remains INCOMPLETE, P12-A remains WAIT_DEPENDENCY, P13 remains BLOCKED, and Phase 12 remains open. No capture eligibility, complete owner or operation coverage, global quiescence, export, hydration, or P12-A/P13 readiness follows from this design or a future implementation of it.

## Current source contract

The accepted UnityBootstrap-Daily-v1 profile resolves the GeneralTest configuration with Economy, Merchant, GuardCrime, and Crime enabled. The published runtime contains one JusticeSystem and the configured CrimeSystem and GuardSystem providers. The selected profile does not compose the P18 timeline.

At the daily runtime boundary, SimulationRuntime calls these legacy methods in this exact order:

1. JusticeSystem.BeginDay()
2. CrimeSystem.AdvanceHiddenStatuses(npcRuntimes)
3. JusticeSystem.AdvanceSentences(npcRuntimes)
4. JusticeSystem.SyncWantedStatuses(npcRuntimes)

The selected path does not call the receipt-backed P18 prepared-step commit methods for those effects. Their P18 step revisions and receipts are separate state and cannot substitute for P12 invalidation of these legacy calls.

JusticeSystem owns two mutable, variable-size collections: wanted records and prison sentences. The selected implementation must observe the actual current contents and mutations. It must not assume one warrant or one sentence per NPC, or assume that startup emptiness bounds evolved state. Each retained row must continue to reference its actual target NPC and City owner; counts may be zero or greater than one over an NPC's history.

NpcRuntime owns the hidden-day counter and the current-status collection. The live roster has exactly one NpcRuntime per registered RuntimeId at each census boundary; its total cardinality follows the already-promoted roster reconciliation. CurrentStatus is a variable-size ordered list of status-definition references. Preserve its actual count and order, and do not infer uniqueness from AddStatus, because constructor/bootstrap paths use AddRange. Hidden-day state is one scalar per registered RuntimeId.

Production source does not mutate CurrentStatus through the public list from outside NpcRuntime; normal writes use AddStatus and RemoveStatus. The implementation will expose CurrentStatus as IReadOnlyList backed by a cached read-only wrapper, update the few consumers that require List to consume IReadOnlyList, and route test/setup writes through NpcRuntime methods. This removes the current bypass without adding a security boundary; P19 has not delivered a public mod API.

CrimeSystem and JusticeSystem also contain P18 receipt-backed step state. Under this P12 profile that state must remain explicitly empty; a nonempty receipt set or nonzero step revision is unsupported profile state and must not be silently treated as captured by this slice. The P18 temporal state contract remains separate.

CrimeSocialAppraisalWorldState and its TheftOutcomeStore, CrimeKnowledgeStore, and SocialReactionStore are composed but are not mutated by the four daily calls above. They remain uncovered by this sub-slice. Theft, guard arrest, escape, failed-escape, and other actor-action writer families require a later source-specific operation review; this design does not claim those paths are integrated as one P12 operation.

## Owner and cardinality contract

| Selected fact | Exact owner identity | Cardinality assumption | Local change evidence |
|---|---|---|---|
| NPC hidden-day counter and current status list | Exact NpcRuntime selected by RuntimeId | One NpcRuntime per current registered RuntimeId; dynamic roster count is reconciled at the existing census boundary. Status rows are zero-or-more and retain actual order/count. | Add p12b.npc-status-crime-state/<RuntimeId>, schema 1, with a local revision on NpcRuntime. This is an invalidation witness, not a second export DTO; P12-D/E later assigns each serialized NPC fact exactly once. Do not alias it to the existing life-state, residence, MoneyAccount, or Inventory section. |
| Wanted records and prison sentences | Exact JusticeSystem instance published by this SimulationRuntime | Exactly one JusticeSystem per selected runtime. Each internal collection is zero-or-more; there is no one-row-per-NPC or one-row-per-City rule. | Add p12b.justice-records, schema 1, with one Justice-owned local revision covering every successful legacy mutation of either collection and each retained row's mutable fields. The provider verifies exact owner reference and reports both actual collection counts; null rows or target/City links that do not resolve to the current selected owners fail census validation. |
| P18 receipt-backed Crime/Justice step state | Exact composed CrimeSystem and JusticeSystem instances | One of each when their selected effective configuration composes them; P18 receipt dictionaries/revisions remain empty in this P12 profile. | Add p12b.crime-p18-receipts and p12b.justice-p18-receipts, schema 1, each cardinality one and exact-owner-bound; reject nonempty receipts or nonzero P18 step revisions. A later P18 receipt write is not admitted as P12 coverage by this design. |

The Justice record rows are members of one singleton JusticeSystem owner in this P12-B witness; they are not separate owners with per-NPC cardinality. The P12-B witness checks dynamic list counts, row validity and local revision. It does not invent row identity or provide an export DTO. P12-E must define stable identities and the sentence-to-warrant relation encoding before Justice state can be exported or hydrated. Dynamic row counts are sampled values, not permanent cardinality promises.

## Bounded implementation contract after design review

1. Add the exact per-NPC status/hidden and singleton Justice census sections in the table. Add p12b.npc-status-crime-state/<RuntimeId> local revisions on NpcRuntime and p12b.justice-records local revision on JusticeSystem. Preflight capacity before each supported commit; do not increment on rejected calls or semantic no-ops. Make CurrentStatus a read-only view backed by a wrapper so production code cannot mutate the live List outside owner methods.
2. Bind exact providers to the selected runtime composition. Verify reference identity against the published NpcRuntime roster, JusticeSystem, and CrimeSystem; reconcile roster-derived NPC sections through the existing census path.
3. Add required explicit-empty receipt sections p12b.crime-p18-receipts and p12b.justice-p18-receipts, each cardinality one and bound to the exact owner. Reject the selected P12 profile if either owner has nonempty P18 receipts or a nonzero P18 step revision.
4. Add narrow P12 operation scopes for the four ordered legacy daily calls. Before each call, validate the required owner sections, current baselines, owner thread, and one epoch increment capacity. Use the actual changed-section set and notify once when that call closes. Preserve the existing call order, repeated SyncWantedStatuses call, and normal logging.
5. Preserve existing partial-commit behavior. If a legacy call commits some row/NPC changes and then throws, close its scope with exactly the sections that did commit, then rethrow the original domain exception. If P12 notification fails after a commit, fault the runtime according to the existing protocol. Do not add rollback or change Justice/Crime behavior.
6. Use current mutation guards and operation batching where available. Do not introduce a generic operation framework. Do not duplicate notifications already emitted by a nested supported owner operation.

This first operation coverage is the daily legacy sequence only. Existing actor-action paths stay individually classified until their exact owner sets, success/failure commit points, event/record-sequence behavior, and nested operation scopes receive a separate review. Owner-local revisions added here may make those writes observable, but that alone does not prove their P12 operation boundaries or full changed-owner sets.

## Dependency-ordered implementation sequence

- **CJ-1 — owner census and local revisions:** exact per-NPC condition/status witness, exact singleton Justice witness, explicit-empty P18 receipt evidence, and local revisions at the underlying committed mutations. Tests establish real roster and row cardinalities, including multiple historical rows for the same NPC/City where the model permits them.
- **CJ-2 — selected daily integration:** bind the exact providers and cover the four existing runtime calls in order using bounded P12 operation scopes and committed changed-section sets.
- **CJ-3 — later writer-family review:** audit theft/social-appraisal, GuardCrime arrest, escape and failed-escape, and generic status effects. Do not begin this family until CJ-1/CJ-2 integration releases SimulationRuntime and JusticeSystem hotspots.

CJ-1 and CJ-2 share NpcRuntime, JusticeSystem, ContinuationCensusProtocol, SimulationRuntime, and bootstrap-composition ownership. They are sequential sub-slices, not parallel writers. Read-only review of the source matrix is safe in parallel.

## Required validation for an implementation candidate

- Focused tests for current-roster membership/cardinality, exact Justice owner identity, zero/many wanted and sentence rows, stable target/City resolution, NPC status order/count, read-only status access, and empty versus populated P18 receipts.
- Focused tests for each of the four daily calls: changed owner revisions, unchanged/no-op behavior, exact changed sections, one reserved epoch per bounded call, owner-thread and stale-baseline rejection before writes, revision/epoch exhaustion, and partial commit followed by exception.
- Regression tests for Population lifecycle invalidation, actor status semantics, Crime hidden-day timing, Justice sentence/warrant ordering, and existing P18 receipt-backed steps.
- ALL EditMode, official Smoke, and git diff --check on the exact code tree; retain logs/XML and hashes. Re-run integration validation if shared runtime/provider composition changes.
- Independent exact-tip code review after implementation; include the exact architecture and alignment refs and check that the review tree matches the validation tree.

## Exclusions and remaining gates

This design does not add concrete Crime/Justice gameplay, new arrest or crime rules, new actor-control/security boundaries, historical replay, P12 export/hydration, P12-A readiness, complete P12-B owner or operation coverage, a complete shared epoch, global quiescence, P13 readiness, or Phase 12 closure. It does not implement P18 temporal steps, P20 activities, P19 loader/API work, Lab UI, or a combined P10/P14 bootstrap profile.

P12-B work remains inside the accepted prerequisite capability authority recorded by the P12 Brief. After independent review, implementation may proceed only if the reviewer confirms the exact owner/cardinality and partial-commit design against current architecture, State, and source. If review finds a missing semantic choice rather than a technical gap, keep this track blocked and identify that exact decision; independent Phase work can continue.
