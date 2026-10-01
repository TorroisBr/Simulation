# P12-B post-MoneyAccount blocker refresh

**Status:** Audit candidate; exact source baseline is P12 canonical f913dd088f71b74cfab7c1bd8a1a79b7ce9a29ea (2026-10-01). Documentation only; this does not promote or close a checkpoint.

## Result

P12-B remains **INCOMPLETE**; P12-A remains **WAIT_DEPENDENCY**; P13 remains **BLOCKED**. The per-City Market stock-row census at 533c1e5 (code 3b2a9c2, review 9dbae5b) and per-NPC MoneyAccount identity/cardinality/local-revision census at 9f615d8 (code 2bdd099, review f79cc55) are canonical.

The highest-value remaining gap is **operation/epoch coherence**, rather than another census provider. The selected profile has a 142-section partial census kernel plus many other passive owner witnesses exposed by composition. The kernel neither registers the full effective-profile owner set nor observes every supported commit path. No supported cross-owner operation currently has complete shared-epoch coverage, and the runtime exposes no capture-eligibility token. The next blocker is **DESIGN_REQUIRED**: refresh the owner-commit and outer-operation boundary contract against the current owner matrix. The bounded design is in [PHASE12_P12B_POST_MONEY_ACCOUNT_INVALIDATION_DESIGN.md](PHASE12_P12B_POST_MONEY_ACCOUNT_INVALIDATION_DESIGN.md).

## Baseline and recovery

- Fetch/prune completed. origin/codex/phase12/canonical and this checkout both resolve to f913dd088f71b74cfab7c1bd8a1a79b7ce9a29ea.
- The executable delta since operation-footprint audit base 70bc1e5 adds the Market and MoneyAccount witnesses/family wiring. The previous method-family map remains applicable; this report adds those owner deltas and rechecks their protocol relationship.
- The runtime-adapter review record remains durable at origin/codex/phase12/P12BRuntimeAdmissionAdapterReviewRecord (bd62a1e); its implementation is already canonical.
- Local Expedition and NPC-Knowledge worktrees contain committed test-only follow-ups (8be5be3, 3d05a1f, 425c217) on older source bases. Their unique local diffs are tests; promoted source behavior is already canonical. They are retained unreviewed follow-ups and do not change readiness. The old census-owner composition worktree at 3ab131d is an out-of-date snapshot of already-promoted owner-family work. The Market worktree at 91c83cd is an older documentation/review snapshot. No candidate is discarded or used as current-base readiness proof.
- Unrelated worktree edits in ProjectSettings and the two untracked ArmedForceSpatialPosition meta files are untouched and excluded.

## Owner and effective-profile census matrix

Status terms describe census evidence only. **COVERED** means exact sections are registered in the current partial protocol; it does not mean P12-B is complete. **PARTIALLY_COVERED** means an owner witness or bounded live observation exists but is not part of a complete sealed profile inventory, or a composite owner still has uncovered mutable facts. A local revision is not a shared epoch.

### Registered partial protocol: 142 selected-profile sections

SimulationRuntime.InitializeNpcRosterCensusProtocol seals two fixed PersonStore sections, then four roster-following families. The authored day-zero profile has ten NPCs:

| Status | Owner identity and section/cardinality | Revision and provider | Day-zero evidence; gap |
|---|---|---|---|
| COVERED (partial kernel) | Two required sections on the installed PersonStore: membership and Person-to-NPC materialization bindings; exact counts including zero. | PersonStore census providers; one store-local revision. | 2 sections. Several PersonStore/lifecycle writers do not enter the membership scope. |
| COVERED (partial kernel) | Two sections per installed NPC, keyed by RuntimeId: known legacy locations and known routes, bound to that NPC's SpatialKnowledgeRuntime. | SpatialKnowledge roster family; each owner's local revision. | 20 sections; initial total 20 known locations and 10 routes. |
| COVERED (partial kernel) | One section per NPC, keyed by RuntimeId and bound to its InventoryRuntime; item-row cardinality and local revision. | Inventory roster family. | 10 sections; initial Inventory rows total 8. Direct writes do not notify the shared epoch. |
| COVERED (partial kernel) | One required section per NPC, keyed by RuntimeId and bound to its exact MoneyAccountRuntime; owner-presence cardinality exactly 1 and account-local revision. | MoneyAccount roster family, schema v1; promoted at 9f615d8. | 10 sections. Successful debit/credit revise the account but do not notify the protocol epoch. |
| COVERED (partial kernel) | Ten separately identified NPC-Knowledge owner sections per NPC, each bound to its installed Knowledge authority and count/revision. | NPC-Knowledge roster family; per-owner local revisions. | 100 sections; registered, but commits have no general epoch callback. |

The current protocol therefore registers 2 + 20 + 10 + 10 + 100 = **142 sections** for the initial ten-NPC roster. Family cardinality follows supported roster reconciliation; 142 is not a maximum, complete owner total, or evolved-boundary census. The protocol registers runtime.npc-membership generally, plus runtime.bootstrap-publication and runtime.advance-day in the selected P12 profile.

### Other canonical witnesses and effective-profile roles

These providers are composed or owner-exposed but are **not registered as a complete P12 profile inventory**. Exact section definitions and promotion records remain in PHASE12_STATE.md and their linked candidate records.

| Owner family | Evidence and exact owner boundary | Classification |
|---|---|---|
| RuntimeIdentityRegistry | Eight typed identity-index sections on the shared registry and its revision. Day zero: 10 NPC, 2 City, 2 legacy Location, 2 legacy Route identities; zero of the other typed identities. | PARTIALLY_COVERED; passive census, not sealed protocol. |
| RuntimeIdAllocator | Fourteen per-kind next-value sections on one allocator with per-counter census revisions. Day-zero NPC/City/legacy-Location/legacy-Route next values are 11/3/3/3; others are 1. | PARTIALLY_COVERED; passive census, not export/hydration or complete registration. |
| SimulationRecordSequence | One causal next-sequence witness and revision; initial next value 1. | PARTIALLY_COVERED; passive C-root witness, not registered. |
| Deterministic random and P9 provenance | Selected profile uses one seed-zero keyed provider with three pure keyed call families and no mutable stream; P9-B provenance is an immutable manifest. | PARTIALLY_COVERED; exact manifest still needs provider/configuration identity. No mutable stream is evidenced for this profile. |
| P8-A geography | Three sections on the installed spatial authority: one Hex, one anchored Location, one scale context; selected profile requires these populated facts at revision 1. | PARTIALLY_COVERED; positive witnesses, not registered or export/hydration. |
| P8-B, P8-C, P8-D | Two B sections, two C sections and two D sections are owner-backed exact-zero witnesses at selected day zero. P8-E coordinator is not a retained owner section. | PARTIALLY_COVERED; exact-zero witnesses not registered. |
| Per-City NPC presence | One section per installed City; exact projection count, City identity, local membership revision, uniqueness and reciprocal roster check. | PARTIALLY_COVERED; projection only, not composite City/NPC revision or protocol section. |
| Per-City Market | One section per installed City, keyed by City RuntimeId and bound to that City's distinct Market owner; stock-row count and Market-local revision. | PARTIALLY_COVERED; promoted at 533c1e5, not registered or a City/economy epoch. |
| SettlementPopulation; Person/Genealogy | Population providers expose aggregate owner count/revision; PersonStore has the two sections above; Genealogy exposes records/count evidence. | PARTIALLY_COVERED; no composite population/person/residence/lifecycle boundary. |
| Legacy SpatialNetwork and ExplorableSite | Two network sections (locations/routes) share the installed network revision; day-zero values 2/2 at revision 4. ExplorableSite reports installed owner, count 0, revision 0. | PARTIALLY_COVERED; passive evidence, not protocol sections. |
| Estate/Property; Institution/Office | Separate sections bind owner identity, cardinality and local revision; Office evidence distinguishes offices, incumbencies and tenures. | PARTIALLY_COVERED; empty bootstrap is not evolved-state proof or shared invalidation. |
| ArmedForce/manpower/position; Conflict/War/Battle | Separate store-bound sections for force/contingent/reference facts, manpower, positions, conflicts, wars and battles, with local counts/revisions where exposed. | PARTIALLY_COVERED; no common epoch for multi-owner installs. |
| Political/organization claims, recognition, faction/support and decisions | Several composed stores expose individual counts/revisions, but the complete live set and revision boundaries are not represented by census providers in the protocol. | PARTIALLY_COVERED; no exact complete owner registration or shared-epoch participation. |
| ActorChoice and ScheduledDirective | Two ActorChoice sections share one store/revision; ScheduledDirective has one store-backed section/revision. Both are empty at day zero. | PARTIALLY_COVERED; causal owners but not registered. |
| TravelParty and Expedition | One owner-backed section each; day-zero count/revision both zero. Expedition transient autonomy reservation/failure state is not represented by its witness. | PARTIALLY_COVERED; no outer-operation or epoch registration. |
| Conditional receipt owners | NpcDecisionRecorder occurrenceReceipts and EconomyTransactionService keyedSaleReceipts each have exact owner/count/revision witnesses at zero for the selected legacy daily profile. | PARTIALLY_COVERED; exact zero only. P18-D keyed consumer is not in this profile. |
| Non-NPC MoneyAccount holders | `PopulationEconomyRuntime` owns an account only for account-backed population consumption; `MarketCounterpartyRuntime` owns an account only for account-backed liquidity. These identities/revisions are not part of the promoted per-NPC roster family. The selected profile must prove the configured absent/present branches exactly before admission; an instantiated non-NPC account needs its own section if the profile composes it. | Conditional owner gaps; do not count the NPC family as all MoneyAccountRuntime instances. Account-backed City operations are outside the current registered 142-section kernel. |
| City/NPC composite mutable state | CityRuntime exposes its Cities backing list and has no whole-City revision. NpcRuntime has no composite revision and exposes mutable status, plans and child-owner references. | MISSING as a complete live composite census/invalidation boundary; some child sections are separate witnesses. |
| Selected Justice/Crime/social appraisal and remaining economy | Effective-config-selected daily owners can change NPC, sentence, wanted/hidden status, outcome, Knowledge, appraisal, Market, account, Inventory, transaction, receipt and plan facts across authorities. No complete live owner tuple or callback covers these paths. | MISSING/PARTIALLY_COVERED by individual receipts and child revisions. |
| Decision/event/history/Chronicle read models | NpcDecisionStore and DomainEventStore are read models; History is a selected event subset and Chronicle is derived. | NOT_REQUIRED as serialized authoritative state or mutation-epoch owners. This does not replace the causal SimulationRecordSequence. |
| P10 topology, P14 flow, P18 temporal state, P19 module state, P20 activities, P13 fork guarantee | Outside UnityBootstrap-Daily-v1. P10/P14 are not configured; P18 timeline composition is rejected by the P12 adapter; other scopes remain separate Phases. | DEFERRED_WITH_JUSTIFICATION for this profile. A future profile must refresh its own owner set. Common daily Economy/Merchant/Commercial-Knowledge owners remain included. |
| External WorldCommand service/queue | Not composed; the accepted profile rejects an external queue. | NOT_REQUIRED for this profile. |

These categories are not summed into a purported complete owner count. The 142 registered sections are the only countable sealed protocol set. The remaining witnesses do not yet bind one complete effective provider graph, roles, counts, revisions and exact-zero exclusions.

## Operation-footprint matrix

The method-level source map remains in PHASE12_B_BLOCKER_RESOLUTION.md. This refresh adds Market and MoneyAccount and distinguishes active-operation accounting from invalidation.

| Operation | Owners it can commit | Current scope/local revision | Epoch and quiescence |
|---|---|---|---|
| Selected bootstrap (Start → genesis → publication) | Genesis/profile authorities, IDs, identity registry, authored City/NPC/site/network facts and runtime composition. | runtime.bootstrap-publication starts at p9.genesis.validate-profile after runtime/protocol creation; earlier genesis is outside. | PARTIAL: the tail is counted and failure revokes/faults publication, but commits are not fully inventoried/notified. |
| NPC membership (TryRegisterNpc, TryUnregisterNpc, TryMaterializePerson, TryBindExistingNpcToPerson) | NPC registry/roster, PersonStore membership/materialization, per-NPC Knowledge/Inventory/MoneyAccount families and sometimes City presence. | runtime.npc-membership checks PersonStore revision delta and reconciles registered fixed/dynamic sections. | PARTIAL: only production notification path found; other PersonStore writers and City side writes remain outside. |
| Other Person/lifecycle (registration, birth, death, residence, migration, immigration/emigration, adoption, genealogy) | Person, NPC, Genealogy, population, residence/binding and City projection. | Public lifecycle/system/store entrypoints may bypass membership scope; some operations compensate commits. | MISSING except named membership subset. |
| Selected TryAdvanceDay / nonzero TryAdvanceDays | Clock, configured daily placement/demography, Justice/Crime, directives, economy/merchant, Knowledge, ActorChoice/actions, travel/party, Expedition and event/decision sequence. | runtime.advance-day spans the selected day/batch under the per-runtime advance lease. | PARTIAL: scope suppresses eligibility during this call but does not lock owners or notify all commits; a later failed stage does not roll back prior domains. |
| City daily production/consumption/pricing and Market mutations | City facts/projections, Market stock rows, receipts, and sometimes Inventory/accounts via commerce. | Market has an owner row-count/local-revision witness per City, not whole-City revision or protocol registration. | MISSING shared epoch; direct City/Market methods bypass runtime scopes. |
| MoneyAccount debit/credit and Inventory writes | Per-NPC account and Inventory; transaction callers may also change Market, receipts, travel, plans and Knowledge. `NpcRuntime.AddMoney/TrySpendMoney` are additional direct account wrappers. | MoneyAccount and Inventory families are passive protocol sections; direct methods commit to public owners. NPC-account prepared installs are found only in excluded P18-D keyed sale; City prepared account installs belong to the excluded P18 timeline daily-economy step. | MISSING shared epoch: selected-profile debit/credit and Inventory add/remove do not notify. Later revision drift can fault partial assessment but is not epoch update. Non-NPC account instances are not covered by the per-NPC family. |
| Economy transaction and Merchant trade | Account, Inventory, Market, transaction/merchant receipts, City/NPC plans and Commercial Knowledge by path. `TryExecuteNpcTrade` mutates two rostered accounts and two rostered Inventories, with compensating account writes on later failure. Other market/population/travel operations span their own owners. | P12-relevant service methods use direct debit/credit, Inventory, Market and caller-owned writes; normal daily calls may nest in `runtime.advance-day`, while direct service/owner entrypoints remain. The two `MoneyAccountRuntime.InstallPrepared` call sites belong to excluded P18-D keyed sale and excluded P18 timeline daily economy. | MISSING complete outer boundary and changed-owner notifications. P18-D keyed receipt remains exact-zero only in this profile. The NPC-trade four-owner transaction is the smallest already-registered candidate cluster; MerchantSystem plan completion and other effects remain outside it. |
| Justice, Crime, wanted/sentence and appraisal | NPC status, Justice/Crime, Knowledge, appraisal/reaction and event sequence. | Some prepared receipts/local revisions; daily work is nested, direct action paths may not be. | MISSING complete set, outer boundary and shared notification. |
| Political, institution, estate/property, force/manpower, Conflict/War/Battle | Multiple local stores; Battle apply spans terminal state, manpower/source population and force mirrors. | Passive census/revisions exist for several stores; local transaction gates are operation-specific. | MISSING profile-wide epoch for a complete logical operation. |
| ActorChoice/directives | Store state and sequence; effects may additionally touch NPC, economy, Knowledge or events. | Passive witnesses; normal daily path nested, direct calls remain. | MISSING; containment is not a changed-section notification. |
| Individual travel and TravelParty start/advance | NPC transit/presence, City projections, account/cost, party, SpatialKnowledge and sequence. | Local revisions and compensation; TravelParty is witnessed but not registered; party advance can commit partial member progress. | MISSING for direct operations; no common scope/epoch. |
| Expedition start/autonomy/exploration/effects/return/reconciliation | Expedition, TravelParty, NPC/City, account, Inventory/place content, Knowledge, Conflict/Battle and sequence, plus transient autonomy reservations. | Normal daily path is inside outer day scope; store reservations/revisions are local. | MISSING complete Expedition boundary; distinct entrypoints can commit partial progress outside daily scope. |
| P18 intraday/P18-D; P20; P10/P14 additions | New causal owners if selected by a future profile. | Excluded from this profile; P12 adapter rejects P18 timeline composition. | NOT_APPLICABLE to this admitted profile; future profiles need a new inventory. |
| External commands | Would mutate input and domain owners. | No WorldCommand service/queue is composed; profile rejects it. | NOT_APPLICABLE. |

No cross-owner selected-profile operation is **COMPLETE** for shared-epoch coverage. NPC membership is PARTIAL; bootstrap and daily advance have partial operation accounting but incomplete owner notification; other included operation families are MISSING. Explicit exclusions are NOT_APPLICABLE to this profile.

## Shared mutation epoch

ContinuationCensusProtocol exposes NotifyCommittedMutation(s) and a monotonically increasing partial mutationEpoch. The only production invocation is through NPC-membership reconciliation: it checks the PersonStore revision delta, reconciles registered NPC families, then notifies those changed section IDs. No production callers were found for generic notification from Market, MoneyAccount, Inventory, City, Justice/Crime, TravelParty, Expedition, or political/military writes.

A local revision proves only that its own owner changed. It does not put Market + account + Inventory + transaction/merchant receipts, or Expedition + TravelParty + NPC/City + Knowledge, into one coherent boundary. A successful compensation is itself a committed owner change. The daily scope prevents capture during its named runtime call, but completion does not refresh all owner baselines or establish whole-operation epoch coverage. Unnotified changes may fault a later partial assessment; no capture token exists to invalidate.

## Admission and quiescence

The promoted adapter establishes only these bounded facts:

- Selected-profile TesteSimulacao.Start captures the actual Thread and managed-thread ID; the runtime verifies them and wrong-thread admission calls fault closed.
- The selected operation inventory contains runtime.bootstrap-publication, runtime.advance-day and runtime.npc-membership. Bootstrap accounting begins only at profile validation, after earlier genesis stages.
- Nonzero day/batch calls use the outer runtime path and existing per-runtime advance lease; runtime-owned selected clock dispatch uses that path. Exceptions in admitted bootstrap/day work fault partial admission.
- TryAssessNpcRosterCensus assesses only registered sections. Operation counts are caller registration, not locks or owner synchronization.
- The protocol is not generally thread-safe. Owner-thread binding cannot observe a same-thread direct owner write outside a registered scope; AuthoritativeMutationGuard health is not a capture epoch.

Admission/quiescence is PARTIAL, not coherent-capture proof. This report does not broaden the adapter's exact reviewed scope.

## Capture-eligibility gap

The accepted P12-B design requires all of the following for a completed-day token:

1. Exact supported profile, build/runtime/numeric identity, effective configuration/calendar/content and provider composition admitted by a sealed manifest.
2. A live versioned witness for every included owner: exact identity, role, cardinality (including required positive or explicit zero), revision; no missing, duplicate, unknown or unregistered section.
3. Current owner thread and healthy mutation guard.
4. Successful complete daily core and outer call; no token during a batch or after failure/throw.
5. No active outer operation, and every supported direct/composite mutator accounted for. Scopes do not imply locks.
6. Every committed authoritative owner change reaches the shared epoch after commit; token has no unreported changed owner.
7. Runtime/profile/day/completed-core sequence/epoch and owner identities still match at validation.
8. P12-G rechecks witnesses after collection and rejects if identity, count, revision, composition or epoch changed.

No CaptureEligible/capture-token API exists. The adapter and 142-section assessment cover only parts of predicates 2–5. Full manifest, owner commit coverage, boundary token and post-collection proof remain missing.

## Other blockers and dependency boundary

- P12-B has not sealed the complete live profile owner manifest or shown that every supported commit path either invalidates the same epoch or is unreachable under this profile.
- The P12-B completed-boundary token/lifecycle is not implemented.
- P12-C deterministic roots/identity exports, P12-D factual exports, P12-E configured-domain exports, P12-F Knowledge/commitment exports, and P12-G staged whole-graph validation/publication/parity remain downstream behind documented edges. Census is not export/hydration.
- P12-A stays WAIT_DEPENDENCY until each included owner has exact export/staged hydration, live profile inventory is validated, and separate implementation authorization is recorded.
- P13 remains dependency-gated. Same-continuation WorldId preservation and future fork-created WorldId/provenance remain architecture constraints; this report implements neither.
- P10 topology, P14 material-flow, P18 temporal, P19 module and P20 activity facts stay outside this profile.

## Next blocker and status

**Selected next blocker:** design the owner-commit/outer-operation integration contract for shared P12-B invalidation against this current matrix. **Status: DESIGN_REQUIRED.**

This dominates a new passive census provider because Market and MoneyAccount now expose exact owner-local witnesses, and the protocol already has an epoch API, while successful daily, transaction, TravelParty and Expedition operations still span owners without complete scopes or changed-section notifications. Another count would not make those operations capture-safe.

The first review of candidate `5372048860412f617250ade1e9be3b49479525d3` returned NEEDS_CHANGES: it conflated an NPC-owned account witness with a transaction boundary and did not state how the account's prepared installs related to the profile. Source revalidation found two prepared-install call sites: P18-D keyed sale and P18 timeline daily economy; both are explicitly excluded from `UnityBootstrap-Daily-v1`. The revised design therefore does not start with an account-only adapter. It proposes a bounded NPC-to-NPC trade operation over the already-registered buyer/seller account and Inventory sections, with the existing protocol's named operation scope and one exact notification for every sequential commit/compensation. It explicitly leaves later MerchantSystem plan completion, direct owner entrypoints and the remaining economy methods uncovered. The revised artifact still needs independent exact-tip design review; it does not start implementation or make any scope/P12 status newly ready.

The design uses existing ownership: map supported operations to exact owner sections; place scopes around supported multi-owner boundaries; notify only after actual owner commits with exact changed section IDs; include successful compensating writes; preserve domain result semantics if continuation bookkeeping faults; fail admission closed for an unaccounted writer; and order implementation by dependencies/hotspots, beginning with a bounded owner cluster already registered with exact witnesses. Do not add a universal lock/event bus, gameplay behavior, generic rollback, security policy or new Phase/checkpoint ID.

The accepted P12-B–P12-G prerequisite authorization was recorded on 2026-09-27. This audit identifies no new product/canonical architecture decision and no new checkpoint-acceptance gate. The 2026-09-27 acceptance already authorizes prerequisite implementation within its accepted P12 scope. Independent design review is the next technical gate; it adds no new authority or scope. Canonical promotion and P12-A implementation authorization remain separate human gates.

## Work and validation

Documentation-only. No Unity tests were run or needed. Run git diff --check and exact-tip independent design/documentation review before presenting this candidate for canonical promotion.
