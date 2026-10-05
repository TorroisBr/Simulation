# P12-B CrimeSocialAppraisal selected-profile mutation invalidation

**Status:** Draft bounded technical design; independent review pending.
**Checkpoint:** Existing accepted P12-B prerequisite-capability work; no new checkpoint ID.
**P12 canonical base:** `a00cba49f642c9b3203df838f9ba27d675f560b2`.
**Architecture:** `ffd75652d89d862b83d634868c560f8540869b89`.
**Intraday/extensibility alignment:** `4b6dd1d38cffeaf3cc1ac3effea0f8ede8771194`.
**Multi-participant activity alignment:** `c285466c355103d3637ac165246591b72eb7bda0`.
**Related promoted work:** P12-B Crime/Justice status and Justice invalidation implementation `92be026fea6515094be7140230194ba626eb090a`, exact tree `e3f844fe67e38df7c96f3db88d792c176c1b9dfd`.

## Contract and current evidence

The selected `UnityBootstrap-Daily-v1` runtime composes exactly one `CrimeSocialAppraisalWorldState` under `SimulationRuntime`. It owns one exact `TheftOutcomeStore`, `CrimeKnowledgeStore`, and `SocialReactionStore`, plus their existing `CrimeSocialAppraisalIntegration`. The child stores share the same `PersonStore` and `SimulationTime`. The authored sample profile begins with no Persons and its legacy NPCs have no `PersonId`, but that baseline is not immutable: current `SimulationRuntime.TryRegisterPerson` can register a Person after publication through the existing P12 membership/lifecycle path. Therefore a permanent explicit-empty fence would not safely cover all admitted P12 state transitions.

The stores expose direct result-bearing mutations. `CrimeSystem.TryExecuteSteal` uses the integration as its theft sink: it validates Person identity and calls `CanAcceptTheftOutcome` before transferring money, then calls `TryAcceptTheftOutcome`; if that write fails, existing code attempts a monetary compensation. The integration adds a TheftOutcome, then CrimeKnowledge and derived SocialReactions, and rolls back earlier store changes if a later step fails. `TryRecordKnowledgeAndAppraise` is another composite writer. The stores currently have no local census revision or P12 owner/epoch hook. Their standalone/runtime fault guards remain separate from P12 invalidation.

## Bounded P12-B contract

Track three exact singleton owner sections, each bound by reference to its child store under the one admitted `CrimeSocialAppraisalWorldState`:

- `p12.crime-social-appraisal.outcomes`: `TheftOutcomeStore`; rows keyed by the existing stable `TheftOutcomeId` from perpetrator, victim, absolute day, and stable occurrence key.
- `p12.crime-social-appraisal.knowledge`: `CrimeKnowledgeStore`; current rows keyed by evaluator `PersonId` plus `TheftOutcomeId`, with existing observation ordering/replacement semantics.
- `p12.crime-social-appraisal.reactions`: `SocialReactionStore`; historical reaction rows keyed by existing `SocialReactionId`, retaining source, target, and supersession links.

Each provider reports exactly one owner, current row count, and a monotone store-local revision. Identity and ordering remain the existing domain contract; no new ID allocator, random input, or temporal precision is introduced. Each successful row commit, replacement, or successful compensation changes the store-local revision before notifying the exact owner section. A rejected/no-op write that changes no stored row does not notify. Revision overflow and P12 epoch-capacity exhaustion fail before the affected write.

Bind owner mutation callbacks to every direct store mutation and to the existing composite integration. A direct single-store commit reserves/preflights its exact owner section and one shared-epoch increment before mutation, then reports after commit. Each composite operation reserves sufficient local-revision and shared-epoch capacity for its maximum successful writes and compensations before its first child-store mutation. The actual call graph bounds a full theft acceptance to at most two committed changes for Outcomes (add/remove), two for Knowledge (write/restore-or-remove), and four for Reactions (two appends/two removals), at most eight shared-epoch increments total. Knowledge-and-appraise without a new outcome needs at most six. Nested store callbacks consume the active composite reservation; they do not reserve twice. Unused reservation capacity is released at scope close.

On partial failure, preserve the existing compensation order and return values. Every committed forward or compensating row change advances its local revision and consumes one reserved epoch increment, even when final semantic rows equal the starting rows. Capacity exhaustion must be detected before the first composite write so a later compensation cannot be prevented by the P12 epoch limit. No callback may be delayed past an unrelated owner write. All scopes remain synchronous and exact-store-bound.

For the theft action, the existing `CanAcceptTheftOutcome` semantic check remains before the account transfer. P12 capacity is reserved when `TryAcceptTheftOutcome` starts after the transfer; if reservation fails before any CrimeSocialAppraisal write, the existing CrimeSystem compensation path restores money. Do not hold a CrimeSocialAppraisal reservation across `EconomyTransactionService` or other owner callbacks. Preserve standalone behavior when the stores are not bound to the selected P12 protocol.

## Implementation seams

1. Add stable schema-v1 census providers for the three existing store references and register/bind them as part of the selected-profile runtime protocol before publication. Admission requires one exact child store of each kind and a readable baseline; the accepted SampleScene starts each count at zero.
2. Add monotone local revisions to the three stores and pre-mutation callbacks for every successful public write plus internal compensation writes. Bind the callbacks only once to the exact P12 runtime; retain existing runtime fault guards.
3. Add one narrowly scoped composite reservation around `TryAcceptTheftOutcome` and one around `TryRecordKnowledgeAndAppraise`. Reserve maximum local and shared capacity from the bounded call graph above; direct store APIs use a one-write path. Reuse P12's existing owner-section/epoch primitives; do not create a general transaction framework.
4. Bind the exact `CrimeSocialAppraisalWorldState` in `SimulationRuntime` and verify each registered provider points to its current child store. Do not add a separate runtime instance or alter bootstrap content.
5. Keep `CrimeSystem`'s existing money-before-appraisal order and compensation behavior. The P12 hook must not change robbery eligibility, money amounts, witness/knowledge rules, appraisal outcomes, reaction identity, or standalone unbound behavior.

## Required validation

Focused coverage must verify exact store identity/cardinality and initial counts; post-bind Person registration followed by each direct writer; local revision and shared-epoch deltas for outcome add, knowledge replacement, reaction append, and every compensation path; single-write and composite saturation preflight with no partial owner mutation; no duplicate notifications for nested composite store calls; and continued unbound integration/compensation semantics. Add an action regression proving P12 capacity refusal restores the existing money transfer and leaves CrimeSocialAppraisal rows unchanged. Then run the affected Crime/Justice, appraisal, economy, runtime admission/orchestration, bootstrap composition and P12 mutation suites, ALL EditMode, official Smoke, and `git diff --check`.

`SimulationRuntime`/P12 protocol is the serialized integration hotspot. Implement only after independent exact-design review; refresh current canonical immediately beforehand. Implementation requires a separate exact-tip code review and bounded-promotion preflight. Documentation-only design commits do not replace code validation.

## Exclusions and limits

This is owner identity, census, mutation invalidation, and bounded epoch accounting for already-composed CrimeSocialAppraisal state. It does not add Person creation semantics, enable Person-backed NPC actions, add theft or appraisal gameplay, change the domain's current results, provide P12-A export/hydration, complete P12-B, establish complete owner/operation/shared-epoch coverage, prove global quiescence or capture eligibility, enable P13, or close Phase 12. It does not add P18 receipts, P19 extension surface, or a generalized transaction framework.