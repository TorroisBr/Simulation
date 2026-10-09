## P12-G current State-update record — promoted 2026-10-09

P12 canonical advanced by clean fast-forward from `21c4e545addbc631fb2259ae85ade724615f2555` to `f21cbc70f508a7fb60bb9cf5ed711685eb5f339b`. This documentation-only promotion records the current-tip C–F interface revalidation in State. The underlying State update is `2cf02189d1f3c3b672ff83990d848e1601c1a0d8`; its independent exact-tip PASS is durably recorded in [`design/PHASE12_G_CURRENT_STATE_UPDATE_REVIEW_2CF0218.md`](design/PHASE12_G_CURRENT_STATE_UPDATE_REVIEW_2CF0218.md). The `Assets` tree remains `1b90b4f77586f69c04330b564a32e0a9475808d4`; focused 26/26, ALL EditMode 2732/2732, and official Smoke 5/5 remain tied to that tree. Unity was not rerun for this docs-only promotion; `git diff --check` passed.

P12-B through P12-F remain promoted within their recorded scopes. P12-G and P12-A remain `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains `OPEN`. C–F interface revalidation is closed, while current owner/operation/epoch reconciliation, remaining target witnesses, restored-boundary admission, single-session publication, and whole-graph proof remain open. This promotion does not assert P12-G or P12-A implementation readiness. Unrelated ProjectSettings edits and untracked `.meta` files remain untouched.
## P12-G current-tip C–F interface revalidation — promoted 2026-10-09

`codex/phase12/canonical` advanced by clean fast-forward from `dfeb79818d40390cc981ceeaa198eb6318b03535` to `21c4e545addbc631fb2259ae85ade724615f2555`. The promoted, documentation-only revalidation is [`design/PHASE12_G_CURRENT_TIP_CF_PACKAGE_INTERFACE_REVALIDATION_DFEB798.md`](design/PHASE12_G_CURRENT_TIP_CF_PACKAGE_INTERFACE_REVALIDATION_DFEB798.md); its independent exact-tip PASS is recorded in [`design/PHASE12_G_CURRENT_TIP_CF_PACKAGE_INTERFACE_REVALIDATION_REVIEW_D87D4A0.md`](design/PHASE12_G_CURRENT_TIP_CF_PACKAGE_INTERFACE_REVALIDATION_REVIEW_D87D4A0.md). The review checked implementation source, exact code/tree identities, dependency order, the current validation report, and the corrected D/F `NpcFRows` ownership seam. The `Assets` tree remains `1b90b4f77586f69c04330b564a32e0a9475808d4`; focused 26/26, ALL EditMode 2732/2732, and official Smoke 5/5 remain tied to this tree. Unity was not rerun for this documentation-only revalidation; `git diff --check` passed.

The production C/D/E/F package source tree is unchanged from the prior interface audit baseline. Current APIs and the retained aggregate test support the local order: begin the attempt, capture F from the source boundary, stage C, stage D, stage E, then stage the retained F capture against those packages under the same attempt. D receives staged `WorldId` and a separately supplied staged `RuntimeIdentityRegistry`; it exports detached `NpcFRows` for F to stage. This revalidation closes only the stale current-tip package-interface item in P12-G §7. It does not deliver a G coordinator, target checks, restored-boundary admission, single-session publication, or whole-graph proof.

P12-B through P12-F remain promoted within their recorded scopes. P12-G and P12-A remain `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains `OPEN`. The remaining P12-G inventory, operation/quiescence, target-witness, admission/publication, and whole-graph gates in the preceding State record remain open. Unrelated ProjectSettings edits and untracked `.meta` files remain untouched.
## P12-G owner/operation/publication reconciliation — promoted 2026-10-09

`codex/phase12/canonical` advanced by clean fast-forward from `564553f92737ddd9d190bc16ac84c4894e35913a` to `f799f4e2db8266be30e2f675b760d0b9911da42f`. The promotion contains the source-linked [Daily-v1 owner/operation/publication reconciliation](design/PHASE12_G_DAILY_V1_OWNER_OPERATION_PUBLICATION_RECONCILIATION_564553F.md) and its independent [exact-tip review record](design/PHASE12_G_DAILY_V1_OWNER_OPERATION_PUBLICATION_RECONCILIATION_REVIEW_10C1302.md). The reviewed ledger content is commit `10c130259bd48b9ff2fa57609188a1c6fa9911d8`, blob `305acd56f32873932b56b1f063288ccef68d974a`. Independent exact-tip review PASS; the full diff is documentation-only, `Assets` is unchanged, and `git diff --check` passes. Unity tests were not applicable to this documentation-only reconciliation.

The refreshed manifest arithmetic is `61 + 22N + (N-M) + P + 4C`, yielding 299 rows for the accepted authored fixture. The previous 275/278 row counts and 20-per-NPC equation are historical; the old 23-operation statement is superseded by the current 24-ID matrix, whose stale 278-row closing sentence is explicitly identified. The new ledger maps current owner families and supported operations to C–F consumers, records non-vector composed objects and publication aliases, and reconciles Expedition using the later reviewed supported-ingress record.

This evidence does **not** certify an exhaustive live graph or make P12-G implementation-ready. Remaining P12-G gates are: complete live owner/cardinality coverage across supported transitions; exact Crime/Social and any supported direct-path operation/epoch mapping; current-tip B–F interface revalidation; fresh target checks for P8-C/D, global receipt caches, per-NPC P18 receipts, Crime/Justice sentinels, and ActorChoice temporal state; fresh restored-boundary admission; the single active-session publication root; and whole-graph rejection, failure atomicity, no-replay, and continuation parity.

P12-B through P12-F remain promoted within their recorded scopes. P12-G remains `WAIT_DEPENDENCY`; P12-A remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains `OPEN`. The separate P12-A implementation authorization is unchanged. The user's unrelated ProjectSettings edits and untracked `.meta` files remain unstaged and untouched.

## P12-G selected-profile inventory evidence promotion — 2026-10-09

P12 canonical advanced by clean fast-forward from 02009f9063dd252bd4b177fd6aef1e74dcd947f5 to 05942670d52c03ea3e7bd6d13f86468549a53071. The promoted Assets tree is 1b90b4f77586f69c04330b564a32e0a9475808d4. The independent exact-tip review is recorded in design/PHASE12_G_TRAVELPARTY_ALLOCATOR_IDENTITY_REVIEW_C65F251.md.

The bounded test-evidence change asserts that the bootstrap-exposed P12-C TravelParty allocator-counter provider and the runtime-registered provider identify the same exact owner instance. The earlier current-inventory evidence in this candidate also reconciles the independent 299-section manifest with protocol expected/registered sets, selected roster and Person-materialization transitions, and identified non-vector objects. Exact validation results and artifact hashes are in validation/P12GCurrentCanonicalInventory/VALIDATION.md: focused 26/26, ALL EditMode 2732/2732, official Smoke 5/5, and git diff --check PASS.

This promotion adds selected-profile inventory evidence only. It does not establish the complete per-section supported-writer/operation/epoch/publication crosswalk, exhaustive evolved or conditional-owner coverage, fresh target-owner validation, restored-boundary admission, single-session publication, whole-graph continuation parity, or P12-G implementation readiness.

### Refreshed P12 dependency status

- P12-B through P12-F remain promoted within their recorded bounded scopes. P12-C is complete within its accepted typed identity, P9-B provenance, deterministic-root, and P8-A continuation scope.
- P12-G remains WAIT_DEPENDENCY. Its next evidence gate is a current source-linked reconciliation joining the 299 expected section/provider identities and revision/cardinality sources to the current 24-operation matrix, successful commits or explicit no-supported-writer dispositions, publication consumers, and B-F payload or target-zero consumers. The older 275-row owner ledger and 23-operation wording must be reconciled against the current records.
- P12-G also retains target-owner exact-zero obligations for P8-C/D, global receipt caches, per-NPC P18 receipts, Crime/Justice sentinels, and the target ActorChoice temporal provider; reviewed source APIs exist, but no G assembler performs these checks.
- P12-G restored-boundary admission and the TesteSimulacao single active-session publication boundary remain unimplemented. Whole-graph rejection, failure atomicity, no-replay, and continuation-parity validation remain later implementation obligations.
- P12-A remains WAIT_DEPENDENCY pending complete included-owner export/private hydration, validated live inventory, and separate implementation authorization. P13 remains BLOCKED. Phase 12 remains OPEN.

Unrelated ProjectSettings edits and the three untracked .meta files in the working tree were not staged or changed.

## Current P12 canonical checkpoint state — 2026-10-09

P12 canonical advanced by clean fast-forward from `0619a33cd4287d89bad80fbe546763aff8f2a75b` to the bounded P12-F owner-package candidate `c4977af1c6566bd5987b3fbdb64bbc7389245d3f`. The candidate was independently reviewed against the exact implementation tree and its validation evidence; this State update records the promotion and refreshed dependencies.

- P12-B through P12-F are PROMOTED within their individually recorded checkpoint scopes. P12-E's completion is limited to its selected Daily-v1 owner-package/export/private-staging boundary.
- P12-G remains WAIT_DEPENDENCY on the required complete B-through-F composition and validated live-profile inventory. P12-A remains WAIT_DEPENDENCY on complete included-owner export/private hydration, validated inventory, and its separate authorization. P13 remains BLOCKED on its supported reconstruction prerequisites. Phase 12 remains OPEN.
- Older State entries below retain their historical statuses at the time each record was written; this current summary supersedes them without changing checkpoint identities or prior promotion history.

## Latest canonical promotion — P12-F Daily-v1 owner package — 2026-10-09

The P12-F implementation candidate `codex/phase12/P12FImplementation` was promoted at `c4977af1c6566bd5987b3fbdb64bbc7389245d3f`, a clean fast-forward from P12 canonical `0619a33cd4287d89bad80fbe546763aff8f2a75b`. Its owner-package implementation commit is `8809be743cbc94155cd85a417e58ec55bd60965d`; aggregate C/D/E/F staging-test commit is `807f175fab5aa267c766f3f10452cdbcf3d5138e`; exact reviewed `Assets` tree is `a9a7c1015a5fa3cacfdb6219b18f2f593c863174`. The final candidate tip adds only review and evidence documents after that unchanged code tree.

Independent exact-tip review PASS is recorded in [`design/PHASE12_P12F_DAILY_V1_OWNER_PACKAGE_IMPLEMENTATION_REVIEW_807F175.md`](design/PHASE12_P12F_DAILY_V1_OWNER_PACKAGE_IMPLEMENTATION_REVIEW_807F175.md). Exact-tree validation passed P12-F 24/24, P12-E regression 6/6, ALL EditMode 2732/2732, official Smoke 5/5, and `git diff --check`; XML/log hashes and run identities are in [`validation/P12FDailyV1OwnerPackage/VALIDATION.md`](validation/P12FDailyV1OwnerPackage/VALIDATION.md), `runs.csv`, and `SHA256SUMS.txt`. All artifact hashes were recomputed locally at preflight.

This promotes detached schema-v1 capture and private staged reconstruction for the five selected Daily-v1 F authorities: PoliticalKnowledgeStore, ScheduledDirectiveStore, ActorChoiceStore, TravelPartyStore, and ExpeditionStore. It retains the shared completed-boundary token, exact owner-section vector, and same temporary staging attempt across C/D/E/F. The test proves aggregate F staging after private C, D, and E staging on that attempt. The package preserves explicit empty owners and detached NPC F rows, and performs no command dispatch, ActorChoice replay, travel replanning, or repeated effects.

P12-F does not add save/load, whole-graph publication, continuation parity, capture eligibility, active-runtime swap, P12-G completion, P12-A/P13 readiness, or Phase 12 closure. P12-G remains blocked on complete profile composition and the validated live-profile inventory. P12-A remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`.

## Latest canonical promotion — P12-E effective-provider and owner source crosswalk — 2026-10-09

P12 canonical advanced by a clean fast-forward from `a2e8b696054089378748354703dd2e0f50245769` to the reviewed documentation candidate `fe490b0368fa50c1bc5196b628e20151323edcc9` on `codex/phase12/canonical`. The original crosswalk candidate is `687eaf3a73e7651005192c6e7a739c738e1c0b2d`, tree `1cb5e4cae1af6eff5b75d3e0f4da3536d614a95e`; its exact crosswalk blob is `5d2280c957deb7df9bb78183cc2d472e62966429`. Independent exact-tip content review PASS is recorded in [`design/PHASE12_E_EFFECTIVE_PROVIDER_OWNER_CROSSWALK_REVIEW_A2E8B69.md`](design/PHASE12_E_EFFECTIVE_PROVIDER_OWNER_CROSSWALK_REVIEW_A2E8B69.md), durably added at the promoted tip. The only change after the reviewed candidate was that review record; final preflight confirmed the three reviewed document blobs were unchanged, ancestry was fast-forward-only, the diff contained documentation only, and `git diff --check` passed. No Unity tests were applicable or run for this documentation-only slice.

The promoted crosswalk maps the selected Daily-v1 effective Economy, Merchant, Justice, Crime, and Guard composition to D/E/F owners, records the legacy demography call and disabled selected policies accurately, and classifies the two P18-only Merchant receipt caches as unreachable in normal Daily-v1 composition. This is source-level composition evidence, not a runtime provider manifest, export/hydration capability, or readiness proof.

### Refreshed P12 dependency DAG

- P12-B and P12-C remain COMPLETE/PROMOTED within their bounded contracts. P12-D remains COMPLETE/PROMOTED within its accepted Daily-v1 factual-root and staged-owner scope.
- P12-E remains IN PROGRESS. The source crosswalk resolved the selected profile's effective-provider/owner mapping for the audited branches and found no additional populated E value owner ready for immediate snapshot implementation. The accepted E contract still requires exact detached exports and private staged hydrators for every included owner, complete provider/owner dispositions, and integrated owner-complete validation. Existing owner snapshot promotions and their limits remain as recorded below; this documentation slice does not satisfy those obligations or add runtime composition.
- P12-F remains WAIT_DEPENDENCY on P12-E. P12-G remains WAIT_DEPENDENCY on P12-B through P12-F and validated live-profile inventory. P12-A remains WAIT_DEPENDENCY pending every included owner's reviewed export/staged hydrator, validated live inventory, and its separate implementation authorization. P13 remains BLOCKED; Phase 12 remains OPEN.
- This promotion makes no P12-A/P13 readiness, capture eligibility, global quiescence, profile-wide continuation, or Phase-closure claim. It does not reopen or change closed phases.
## P12-E current-profile provider source crosswalk — 2026-10-09

A read-only source crosswalk at P12 canonical `a2e8b696054089378748354703dd2e0f50245769` now maps the selected Daily-v1 effective Economy, Merchant, Justice, Crime, and Guard composition to its D/E/F owner boundaries and snapshot dispositions. It also establishes that the Merchant plan-urgency and commercial-sharing receipt caches are P18-only and remain empty/unreachable in the normal Daily-v1 composition. Details and source anchors are in [`design/PHASE12_E_EFFECTIVE_PROVIDER_OWNER_CROSSWALK_A2E8B69.md`](design/PHASE12_E_EFFECTIVE_PROVIDER_OWNER_CROSSWALK_A2E8B69.md).

No additional populated E value owner was found for immediate implementation. The crosswalk is source-level inventory evidence only; it does not supply a runtime provider manifest or complete exports/staged hydrators. P12-E remains IN PROGRESS; P12-F/G and P12-A retain their WAIT_DEPENDENCY gates; P13 remains BLOCKED. No readiness or closure status changes.
## Latest canonical promotion — P12-E Justice records owner snapshot (2026-10-09)

`codex/phase12/canonical` was fast-forwarded from `565f6b5817e0126c2f89e0a1452b09a8fb03cc19` to `52d1c5d2ef4149111bf3c794e8984715ca9cca74`. The reviewed implementation/test tip is `e0b464a23b0db339f7a13e6fb2f8c20607dcd9fd`, tree `ed209e7c9d707f396a93de06cae2e07f0e00bd4e`; the promoted candidate retains the validation report/evidence and exact-tip review record [`design/PHASE12_P12E_JUSTICE_RECORDS_OWNER_SNAPSHOT_IMPLEMENTATION_REVIEW_E0B464A.md`](design/PHASE12_P12E_JUSTICE_RECORDS_OWNER_SNAPSHOT_IMPLEMENTATION_REVIEW_E0B464A.md) at `52d1c5d`. Remote candidate and canonical refs synchronized at the promotion SHA. Preflight confirmed clean fast-forward ancestry, unchanged reviewed code tree, matching validation artifacts, and `git diff --check` PASS.

The bounded schema-v1 adapter captures and privately stages ordered Justice wanted-record and prison-sentence facts for the selected Daily-v1 owner. It preserves exact local revision, NPC/nullable Person and City references, list order/multiplicity, and sentence-to-warrant relation; capture consumes the existing exact-zero Justice P18 receipt witness without serializing transient receipts. The independently reviewed final code tree passed Justice snapshot 9/9, Justice invalidation 12/12, NPC-root snapshot 26/26, NPC receipt census 13/13, runtime admission 70/70, ALL EditMode 2709/2709, official Smoke 5/5, and `git diff --check`; XML/log hashes are recorded in [`validation/P12EJusticeRecordsOwnerSnapshot/VALIDATION.md`](validation/P12EJusticeRecordsOwnerSnapshot/VALIDATION.md).

### Refreshed P12 dependency DAG

- P12-B and P12-C remain COMPLETE/PROMOTED within their recorded contracts; P12-D remains COMPLETE/PROMOTED within its bootstrap factual-root and Person/population boundary.
- P12-E remains IN PROGRESS. Justice wanted-record and prison-sentence export/private staging is now promoted. Earlier core and configured-owner snapshots remain as recorded. The accepted P12-E checkpoint still requires current effective-provider/owner inventory and exact export/staged hydration for every included owner; this slice does not satisfy or claim complete P12-E coverage or integration.
- The current read-only owner audit found no additional discrete populated P12-E snapshot owner supported for immediate implementation. Merchant commitment/Knowledge state is assigned to P12-F; transient Merchant/P18 receipts remain excluded; P12-D owns City/market/account/inventory facts. This finding does not close the effective provider inventory or P12-E.
- P12-F remains WAIT_DEPENDENCY on P12-E. P12-G remains WAIT_DEPENDENCY on P12-B through P12-F and a validated live-profile inventory.
- P12-A remains WAIT_DEPENDENCY until every included owner has exact export and staged hydration, the live profile inventory is validated, and its separate implementation authorization is recorded. P13 remains BLOCKED. Phase 12 remains OPEN.

This promotion does not add runtime/bootstrap composition, P12-G publication, global quiescence, capture eligibility, profile-wide export/hydration, P12-A or P13 readiness, or Phase closure.
## Latest canonical promotion — P12-E Crime/Social Appraisal owner snapshot (2026-10-09)

P12 canonical advanced by clean fast-forward from `29f719f428e29cc452ffa8435c0d601c2bed4787` to the exact-tip reviewed candidate `a998541f24841cbd0b1d6f0359a60aaa7b49ae90`. The candidate code commit/tree are `58a25ed8c6ea40d9767594ad2ee3800d13caa70c` / `951aeccd8f71a017729a06ed438b13f63f3a8378`; its full Git tree is `f57a889c8ffca4e5217d633514bc9e3eed053cb1`. The candidate and evidence were pushed on `codex/phase12/P12ECrimeSocialAppraisalOwnerSnapshotImplementation`, then promoted to `codex/phase12/canonical`.

The exact-tip implementation review is VALIDATED_CANDIDATE at review branch commit fd3770d936b33b4cf410aadbc92f0c3a4dd5a253, with the durable follow-up in docs/design/PHASE12_P12E_CRIME_SOCIAL_APPRAISAL_OWNER_SNAPSHOT_REVIEW_FOLLOWUP_A998541.md. It resolves the prior docs-only NEEDS_CHANGES at 67740afbdfc4fed98830e13c539094a73e1567c0. The design review PASS is recorded at 3fe1bb522907797383e62559a2a62495bba1528e.

Exact-code-tree validation passed the focused owner snapshot suite 6/6; Crime/Social Appraisal integration 12/12; Crime/Social invalidation 11/11; Crime/Justice invalidation 12/12; continuation census protocol 24/24; Institution/Office snapshot 11/11; Political Claim snapshot 9/9; Political Decision snapshot 5/5; Political Support snapshot 6/6; Runtime Admission 70/70; ALL EditMode 2700/2700; official Smoke 5/5; and git diff --check. The final focused, full-suite, and Smoke artifacts are tied to code tree 951aeccd; the Political Decision, Political Support, and Runtime Admission focused runs predate only test-file additions in this checkpoint, and the final ALL EditMode run covers them on the final source tree. Raw XML/logs and hashes are retained under docs/validation/P12ECrimeSocialAppraisalOwnerSnapshot/.

This promotes detached schema-v1 snapshot capture and private staged reconstruction for the three existing TheftOutcomeStore, CrimeKnowledgeStore, and SocialReactionStore owners. It preserves exact independent cardinality/revision, stable IDs and typed endpoints, opaque provenance, current crime knowledge, historical reaction supersession, and binding to the supplied staged PersonStore, InstitutionStore, and SimulationTime roots. Capture uses the existing completed-boundary token and exact P12-B owner witnesses.

No runtime/bootstrap composition, mutation-epoch wiring, gameplay behavior, P12-B completion, P12-A readiness, P13 readiness, P12-G publication, profile-wide export/hydration, or Phase 12 closure is added or claimed. Existing supported Crime/Social writes and P12-B behavior remain unchanged.

### Refreshed P12 dependency DAG

- P12-B and P12-C remain COMPLETE/PROMOTED within their recorded contracts; P12-D remains COMPLETE/PROMOTED within its Daily-v1 owner-package boundary.
- P12-E remains IN PROGRESS. Crime/Social Appraisal owner snapshot capture/staging is now promoted; other composed E authorities remain subject to the accepted owner inventory and require their own bounded evidence or implementation.
- P12-F waits on P12-E. P12-G waits on P12-B through P12-F and a validated live-profile inventory.
- P12-A remains WAIT_DEPENDENCY pending complete included-owner export/staged hydration, live-profile inventory validation, and its separate implementation authorization. P13 remains BLOCKED.
- Phase 12 remains OPEN. This slice does not establish complete P12-E owner coverage or broader continuation readiness.

## Latest canonical promotion — P12-E PoliticalDecision owner snapshot (2026-10-09)

P12 canonical advanced by a clean fast-forward from `c9d2d8f9ff7176d4d36c5e0a007d2f9ddc7210f8` to reviewed candidate `df1daf8979079abf3cd1619b4a3992765988426e`. The candidate preserves the exact reviewed code commit/tree `8b0bbcb15e864fa0ebb4d26dbc6b205038a2f921` / `4496b67ed9f59d9cb95bc4e16bf0b90bd3ae7e2b` (Assets tree `04b8f0248dfcd4f164d7ba317ac34a159983a134`). The code candidate `293ad422153a6ef5b11c7e755757107d6cfba5ed` passed independent exact-tip review; the durable review is [`design/PHASE12_P12E_POLITICAL_DECISION_OWNER_SNAPSHOT_IMPLEMENTATION_REVIEW_8B0BBCB.md`](design/PHASE12_P12E_POLITICAL_DECISION_OWNER_SNAPSHOT_IMPLEMENTATION_REVIEW_8B0BBCB.md), recorded in the promoted candidate. Its design review is `d617aab13db2af4db0385e7c4d5b7eaa0375fe09`; the reviewed design is [`design/PHASE12_P12E_POLITICAL_DECISION_OWNER_SNAPSHOT_DESIGN.md`](design/PHASE12_P12E_POLITICAL_DECISION_OWNER_SNAPSHOT_DESIGN.md).

Exact-tree validation passed the focused owner snapshot suite 5/5, affected regressions 28/28, ALL EditMode 2694/2694, official Smoke 5/5, and `git diff --check`. XML/log hashes and run identities are recorded in [`validation/P12EPoliticalDecisionOwnerSnapshot/VALIDATION.md`](validation/P12EPoliticalDecisionOwnerSnapshot/VALIDATION.md), `runs.csv`, and `SHA256SUMS.txt`; the artifact hashes were checked before promotion.

This promotes detached schema-v1 export and private staged reconstruction for the existing PoliticalDecision owner, preserving typed records, stable identity/order, references and local append-only revision. It proves exact empty-owner live capture and detached non-empty staging. The current Daily-v1 admission remains exact-zero for this owner; live non-empty capture is unsupported until a separately authorized P12-B boundary change. No runtime/bootstrap composition, P12-B census or mutation wiring, P12-C sequencing, P12-G publication, profile-wide coverage, P12-A/P13 readiness, or Phase 12 closure is added or claimed.

### Refreshed P12 dependency DAG

- P12-B and P12-C remain COMPLETE/PROMOTED within their bounded contracts; P12-D remains COMPLETE/PROMOTED within its Daily-v1 owner-package boundary.
- P12-E remains IN PROGRESS. Conflict, Battle, ArmedForce/manpower/position, Institution/Office, War, Property/Estate, PoliticalClaim/recognition, Faction/affiliation, PoliticalSupport, and PoliticalDecision owner snapshots are promoted. Other composed E authorities remain incomplete under the accepted owner inventory and require their own bounded current-base work.
- P12-F waits on P12-E. P12-G waits on P12-B through P12-F and a validated live-profile inventory.
- P12-A remains WAIT_DEPENDENCY until every included owner has exact export and staged hydration, the live profile inventory is validated, and separate implementation authorization is recorded. P13 remains BLOCKED.
- Phase 12 remains open. This checkpoint does not imply profile-wide export/hydration, global quiescence, complete P12-E coverage, or P12-A/P13 readiness.
## Latest canonical promotion — P12-E PoliticalSupport owner snapshot (2026-10-09)

P12 canonical was refreshed at `4fb8af28780e492c75010ed87bbe21ae6c3b816d`. The bounded candidate `codex/phase12/P12EPoliticalSupportOwnerSnapshotImplementation` was independently reviewed and promoted by clean fast-forward through candidate tip `70df06acac19b6c909e5148cc57a64c0847d1e18`; this State update records the resulting canonical promotion. Its implementation and focused-test commit is `9da6c882c035870ca0d67612288e1b1d76ccf56e`, code tree `768db39ebf3720c93d86250cefd370922df61e77`, and Assets tree `45f959b73528924a296bba6a9404a7878b7f6013`. Candidate tip `728bd9c43b0c606c7eb454c6398e8674b2b52249` retains validation after the code commit; the exact-tip independent review is recorded in [`design/PHASE12_P12E_POLITICAL_SUPPORT_OWNER_SNAPSHOT_IMPLEMENTATION_REVIEW_9DA6C88.md`](design/PHASE12_P12E_POLITICAL_SUPPORT_OWNER_SNAPSHOT_IMPLEMENTATION_REVIEW_9DA6C88.md).

Validation on the exact Assets tree passed five focused suites (39/39), ALL EditMode (2689/2689), official Smoke (5/5), and `git diff --check`. Exact XML/log hashes and results are recorded in [`validation/P12EPoliticalSupportOwnerSnapshot/VALIDATION.md`](validation/P12EPoliticalSupportOwnerSnapshot/VALIDATION.md), `runs.csv`, and `SHA256SUMS.txt`.

This adds detached schema-v1 export and private staged reconstruction for the existing PoliticalSupport owner. It preserves the exact completed Daily-v1 token and Required owner-section identity/cardinality/revision witness, all active and ended typed relation facts, captured-day bounds, deterministic ordering, local revision, typed references, and the derived active-pair index. No runtime/bootstrap composition, P12-B census or mutation wiring, `PoliticalWorldRevision` snapshot, shared persistence coordinator, P12-G publication, P12-A/P13 readiness, global quiescence, complete P12-E coverage, or Phase 12 closure is added or claimed.

### Refreshed P12 dependency DAG

- P12-B and P12-C remain COMPLETE/PROMOTED within their bounded contracts; P12-D remains COMPLETE/PROMOTED within its Daily-v1 owner-package boundary.
- P12-E remains IN PROGRESS. Conflict, Battle, ArmedForce/manpower/position, Institution/Office, War, Property/Estate, PoliticalClaim/recognition, Faction/affiliation, and PoliticalSupport owner snapshots are promoted. Other composed E authorities remain incomplete under the accepted owner inventory and require their own bounded current-base work.
- P12-F waits on P12-E. P12-G waits on P12-B through P12-F and validated live-profile inventory.
- P12-A remains WAIT_DEPENDENCY until every included owner has exact export and staged hydration, the live profile inventory is validated, and its separate implementation authorization is recorded. P13 remains BLOCKED.
- Phase 12 remains open. This checkpoint does not imply profile-wide export/hydration, global quiescence, or P12-A/P13 readiness.
## Latest canonical promotion — P12-E PoliticalClaim and Faction owner snapshots (2026-10-09)

P12 canonical was refreshed at `a768f2d9eca161f5cff059a782737412f43b2861`.
Immediately before promotion, the canonical, candidate, review, code tree, artifact hashes, protected-file hashes, ancestry, and diff checks were revalidated. `codex/phase12/canonical` advanced by clean fast-forward from `a768f2d9eca161f5cff059a782737412f43b2861` to the promotion State commit `9c4874bae64224a68844c503be423955a3bef987`; local and remote refs were verified synchronized at that SHA.

The bounded candidate combines PoliticalClaim/recognition and Faction/affiliation
owner snapshots. Its implementation commit is
`1e2d81b7e18d7290ef3adf90cb44160bf3aba1a5`; the exact reviewed `Assets` tree
is `803ace9ead6ca5e6cbd3ca95b4574e5d03a8393f`. Candidate tip
`62cb4eedcc990a6f57e59d2c6916f9d334871d33` adds only durable validation
evidence after the code commit. Independent exact-tip implementation review
PASS is recorded at `b0bc51a06afaae9785f100e881128d0807e19252` on
`codex/review/phase12/P12EOwnerSnapshotsIntegratedReview1E2D81B`, in
[`design/PHASE12_P12E_OWNER_SNAPSHOTS_IMPLEMENTATION_REVIEW_1E2D81B.md`](design/PHASE12_P12E_OWNER_SNAPSHOTS_IMPLEMENTATION_REVIEW_1E2D81B.md).
The review binds to the unchanged code commit/tree and checked the docs-only
validation follow-up. The owner designs and prior review evidence remain in
[`design/PHASE12_P12E_POLITICAL_CLAIM_OWNER_SNAPSHOT_DESIGN_0709EEC.md`](design/PHASE12_P12E_POLITICAL_CLAIM_OWNER_SNAPSHOT_DESIGN_0709EEC.md),
[`design/PHASE12_P12E_POLITICAL_CLAIM_OWNER_SNAPSHOT_DESIGN_REVIEW_94CE311.md`](design/PHASE12_P12E_POLITICAL_CLAIM_OWNER_SNAPSHOT_DESIGN_REVIEW_94CE311.md),
[`design/PHASE12_P12E_FACTION_OWNER_SNAPSHOT_DESIGN_A768F2D.md`](design/PHASE12_P12E_FACTION_OWNER_SNAPSHOT_DESIGN_A768F2D.md),
and [`design/PHASE12_P12E_FACTION_OWNER_SNAPSHOT_DESIGN_REVIEW.md`](design/PHASE12_P12E_FACTION_OWNER_SNAPSHOT_DESIGN_REVIEW.md).

Exact-tree validation passed 14 focused suites (183/183), ALL EditMode
(2683/2683), official Smoke (5/5), and `git diff --check`. Durable results
and hashes are recorded in
[`validation/P12EOwnerSnapshotsIntegration/VALIDATION.md`](validation/P12EOwnerSnapshotsIntegration/VALIDATION.md),
[`validation/P12EOwnerSnapshotsIntegration/runs.csv`](validation/P12EOwnerSnapshotsIntegration/runs.csv),
and [`validation/P12EOwnerSnapshotsIntegration/SHA256SUMS.txt`](validation/P12EOwnerSnapshotsIntegration/SHA256SUMS.txt).
All 49 committed XML/compressed-log artifact hashes were rechecked. The
preflight-confirmed 657 existing `.meta` files and 26 `ProjectSettings` files
remain byte-identical; three Unity-generated untracked `.meta` files remain
unmodified and uncommitted.

This promotes detached schema-v1 export and private staged reconstruction for
the selected Daily-v1 PoliticalClaim/recognition and Faction/affiliation
owners. Exact identity, local revisions, typed references, order/history, and
all-or-nothing private staging are preserved. It adds no runtime/bootstrap
composition, mutation/epoch wiring, global quiescence, profile-wide capture
eligibility, P12-G publication, or Phase 12 closure. P12-E remains IN PROGRESS.

### Refreshed P12 dependency DAG

- P12-B and P12-C remain COMPLETE/PROMOTED within their accepted bounded
  contracts; P12-D remains COMPLETE/PROMOTED within its Daily-v1 owner-package
  boundary.
- P12-E remains IN PROGRESS. Conflict, Battle, ArmedForce/manpower/position,
  Institution/Office, War, Property/Estate, PoliticalClaim/recognition, and
  Faction/affiliation owner snapshots are promoted. PoliticalSupport remains
  an accepted but incomplete P12-E owner; its bounded snapshot design and
  independent review are the next owner-specific work. Other configured E
  authorities remain incomplete as recorded in the live owner inventory.
- P12-F waits on P12-E. P12-G waits on P12-B through P12-F and validated
  live-profile inventory.
- P12-A remains WAIT_DEPENDENCY until all included owners have exact export
  and staged hydration, the live profile inventory is validated, and its
  separate implementation authorization is recorded. P13 remains BLOCKED.
- Phase 12 remains open. This promotion does not imply profile-wide
  export/hydration, global quiescence, or P12-A/P13 readiness.
# Phase 12 State — Save & Deterministic Continuation

**Status:** PHASE 12 IN PROGRESS — P12-B COMPLETE/PROMOTED within its bounded
profile-admission and completed-boundary lifecycle contract; P12-C
COMPLETE/PROMOTED within its accepted identity, genesis-provenance, and
deterministic-root continuation scope; P12-A remains `WAIT_DEPENDENCY`. P13
remains `BLOCKED`.

## Latest canonical promotion — P12-E Property/Estate owner snapshots (2026-10-08)

After refreshing P12 canonical at `0709eec1244f791e81b1d05f669bb4a577a9fdb6`,
the bounded current-base candidate `codex/phase12/P12EPropertyEstateOwnerSnapshotWarBaseFix`
was promoted through candidate tip `f473c194af5dc19b25fca7682e6434e6cc4c5b80`
and exact-tip review record `2440f29b5f09531a75a96108f0dc0c526973d4f7`.
Its implementation/fix commit is
`4490fd51a83e0ebd6b79801d9b18674655ec6adf`; the reviewed Git tree is
`43efc164fe1668ca8fd19dba93c0dadf582b1e97` and tested `Assets` tree is
`4ee9e00f3aaec28225fdebd2a38d8db78b2061e6`.

The independent exact-tip review is recorded in
[`design/PHASE12_P12E_PROPERTY_ESTATE_OWNER_SNAPSHOT_IMPLEMENTATION_REVIEW_F473C19.md`](design/PHASE12_P12E_PROPERTY_ESTATE_OWNER_SNAPSHOT_IMPLEMENTATION_REVIEW_F473C19.md).
It confirms the earlier null transfer-row defect is fixed and the required
malformed-row, reference, day-bound, capture-stamp, paired-output, and
source-immutability regressions are present. The review verified all 20 raw
artifact hashes/XML results and reports zero failures. Exact-tree validation
passed focused snapshot 11/11, Property census 2/2, Estate census 1/1,
Property/Estate foundation 9/9, transfer 5/5, succession 21/21, mutation epoch
5/5, Person staging 5/5, ALL EditMode 2662/2662, official Smoke 5/5, and
`git diff --check`. Evidence is retained in
[`validation/P12EPropertyEstateOwnerSnapshot/ReviewFix/VALIDATION.md`](validation/P12EPropertyEstateOwnerSnapshot/ReviewFix/VALIDATION.md).

This adds detached schema-v1 capture and private staged reconstruction for
the selected Daily-v1 Property ownership, transfer-history, and Estate-record
sections. Failed staging yields no partial Property/Estate pair. The slice
does not add runtime/bootstrap integration, operation or epoch wiring, global
quiescence, profile-wide capture eligibility, P12-G publication, or Phase 12
closure. P12-E remains IN PROGRESS.

### Refreshed P12 dependency DAG

- P12-B and P12-C remain COMPLETE/PROMOTED within their accepted bounded
  contracts; P12-D remains COMPLETE/PROMOTED within its Daily-v1 owner-package
  boundary.
- P12-E remains IN PROGRESS. Conflict, Battle, ArmedForce/manpower/position,
  Institution/Office, War, and Property/Estate owner snapshots are promoted.
  A PoliticalClaim/recognition owner-specific design candidate is available at
  `c286bc7b41c1c5b499d98b55f192be784ee43faa`; implementation awaits its
  independent exact-tip design review. Other accepted owner families remain
  incomplete.
- P12-F waits on P12-E. P12-G waits on P12-B through P12-F plus validated
  live-profile inventory.
- P12-A remains WAIT_DEPENDENCY until all included owners have exact
  export/staged hydration, the live profile inventory is validated, and its
  separate implementation authorization is recorded. P13 remains BLOCKED.
- Phase 12 remains open. This owner slice does not imply profile-wide
  export/hydration or Phase closure.

## Previous canonical promotion — P12-E War owner snapshot (2026-10-08)

After refreshing P12 canonical at `710874b06b3045bb75acb28feeb063d63a83c31e`,
the bounded current-base War candidate `codex/phase12/P12EWarOwnerSnapshotIntegration`
was promoted at `8222476cf63ba4126b1a1c00370618a28629bdb7`. Its code commit
`ba2f8af44c91cc49cb424b959cbc4c88ab440c8e` is a direct child of the prior
canonical. The exact reviewed Git tree is
`cc5f95f8099391e587d24c360184f7fc65fecbea`; its validated combined `Assets`
tree is `d844aa0f09587b91b5582bc91aff2974a0059c57`.

Independent exact-tip current-base integration review PASS is durably recorded
at `8dc4105caff22af08085eb65d4c4cddf7ce4cb5c` on
`codex/review/phase12/P12EWarOwnerSnapshotIntegrationReview8222476` in
[`design/PHASE12_P12E_WAR_OWNER_SNAPSHOT_INTEGRATION_REVIEW_8222476.md`](design/PHASE12_P12E_WAR_OWNER_SNAPSHOT_INTEGRATION_REVIEW_8222476.md).
The review confirms all five War implementation/test/metadata blobs match the
previously reviewed code and finds no Institution/Office overlap. Current-base
validation passed War 5/5, Conflict/War 9/9, Battle 6/6, ArmedForce 10/10,
P17 10/10, Institution/Office 11/11, ALL EditMode 2651/2651, official Smoke
5/5, and `git diff --check`. Validation artifacts and hashes are recorded in
[`validation/P12EWarOwnerSnapshotIntegration-20261009.md`](validation/P12EWarOwnerSnapshotIntegration-20261009.md).

The bounded War owner snapshot preserves exact ArmedForce-to-Conflict staged
references, optional Conflict resolution, local War revision restoration,
private all-or-nothing staging, and rejection of P17-A War data in Daily-v1.
It adds no runtime/bootstrap integration or mutation/epoch wiring and does not
claim complete P12-E coverage, global quiescence, capture eligibility, P12-A
or P13 readiness, whole-profile export/hydration, P12-G publication, or Phase
12 closure.

### Refreshed P12 dependency DAG

- P12-B and P12-C remain COMPLETE/PROMOTED within their accepted bounded
  contracts; P12-D remains COMPLETE/PROMOTED within its Daily-v1 owner-package
  boundary.
- P12-E remains IN PROGRESS. Conflict, Battle, ArmedForce/manpower/position,
  Institution/Office, and War owner snapshots are promoted. Property/Estate
  remains a separate implementation track and is being corrected and
  re-integrated on this canonical base.
- P12-F waits on P12-E. P12-G waits on P12-B through P12-F plus validated
  live-profile inventory.
- P12-A remains WAIT_DEPENDENCY until all included owners have exact
  export/staged hydration, the live profile inventory is validated, and its
  separate implementation authorization is recorded. P13 remains BLOCKED.
- Phase 12 remains open. This owner slice does not imply profile-wide
  export/hydration or Phase closure.

## Previous canonical promotion — P12-E Institution/Office owner snapshots (2026-10-08)

After refreshing P12 canonical at `77135b3e0ca8df83c6852f2c234ff9098a833468`,
the bounded Institution/Office candidate was promoted through implementation
tip `4ae340ad1bea69d53d0635ba3eeb8a71b4f268a7` and its exact-tip review record
`1cbb69aec73f6c287db87a22918140264192d206`. The reviewed `Assets` tree is
`e1e1000774d57c49d98b8ac23f5d1c0319cc45ab`; the promotion preserves that tree.
Independent review returned `VALIDATED_CANDIDATE` with no blocking findings.

The slice adds detached schema-v1 capture and private staged reconstruction
for the four Required Daily-v1 Institution/Office owner sections. Capture
binds to the exact installed owners and token/component vector, preserves
owner revisions and repeated equal closed-tenure occurrences in mutation
order, and validates typed references and open/closed tenure consistency
before returning the private staged pair. It does not publish runtime state.

Exact-tree validation passed: focused 11/11, selected regressions 89/89, ALL
EditMode 2646/2646, official Smoke 5/5, and `git diff --check`. Durable XML,
logs, and hashes are recorded with the candidate integration evidence.

This promotes only the bounded Institution/Office owner snapshot capability.
P12-E remains in progress pending the other accepted effective-profile owner
snapshots and their integration. It adds no runtime/bootstrap integration,
operation or epoch wiring, global quiescence, profile-wide capture eligibility,
P12-G publication, or Phase closure. P12-B and P12-C remain complete within
their recorded bounded contracts; P12-D remains complete within its accepted
Daily-v1 private owner-package boundary. P12-A remains `WAIT_DEPENDENCY`; P13
remains `BLOCKED`.

### Refreshed P12 dependency DAG

- P12-B and P12-C remain COMPLETE/PROMOTED; P12-D remains COMPLETE/PROMOTED
  within its accepted Daily-v1 private owner-package boundary.
- P12-E remains IN PROGRESS. Conflict and Institution/Office owner snapshots
  are promoted; Property/Estate and War owner snapshot work remain separate
  tracks, with their own current-base validation and review gates.
- P12-F waits on P12-E; P12-G waits on P12-B through P12-F and a validated
  live-profile inventory.
- P12-A remains WAIT_DEPENDENCY until all included owners have exact
  export/staged hydration, the live profile inventory is validated, and its
  separate implementation authorization is recorded. P13 remains BLOCKED.
- Phase 12 remains open; no profile-wide export/hydration or Phase closure is
  implied by this owner slice.

## P12-D accepted Daily-v1 owner package complete (2026-10-08)

P12 canonical advanced from `a4ce0abcf261226f4b52fbacc8df9ef8f67a0de2` through the reviewed owner-package candidate and exact-tip review record to `329c75acc6f8b244912f9c2550c021859f9b2cc3`. The implementation candidate was `6c43f18e76e6d7c80307890c64a30e7c3e541da0`; its reviewed Git tree is `346dbe8a387753b4b5bfb7cc0a968d0e73bdcf50` and its `Assets` tree is `86df24ff56a945160da517b6f62325ba1f8af8dc`. Production code is `aaf727b0f32cedf93aa895aa93461ae6a6a2d9e9`; integrated coverage is `d1244b200305e494cfbc38d53e1418a1c8007110`. Independent exact-tip review PASS is recorded in [`design/PHASE12_D_OWNER_PACKAGE_ASSEMBLY_IMPLEMENTATION_REVIEW_6C43F18.md`](design/PHASE12_D_OWNER_PACKAGE_ASSEMBLY_IMPLEMENTATION_REVIEW_6C43F18.md) at canonical tip `329c75a`.

The accepted Daily-v1 P12-D owner and relation graph is now assembled into one private staged package. It includes the selected City and nested market/account/stock/population facts, the merged NPC D/F projections, Person, Genealogy, legacy SpatialNetwork, the exact-empty ExplorableSite boundary, and the nested receipt-owner census. The coordinator binds snapshots to the same completed-boundary token, stamp, and exact owner-section vector; validates cross-owner identity, references, ordering, and reciprocal relations; and returns no partial package when staging fails. The package remains private and unpublished.

The exact reviewed tree passed P12-D focused coverage 26/26, ALL EditMode 2628/2628, official Smoke 5/5, and `git diff --check`. Hashes and retained validation artifacts are recorded in [`validation/P12DOwnerPackageAssembly/VALIDATION.md`](validation/P12DOwnerPackageAssembly/VALIDATION.md). The current-base design review and architecture freshness revalidation remain recorded at `8a68a003601d9197e63fe4fe5fc567eab6a4b58e` and `c34e1de4c902f18665207865f483784ca1b3d9ce`.

**P12-D is COMPLETE/PROMOTED within this accepted Daily-v1 owner-export and private staged-hydration boundary.** This does not deliver whole B–F graph composition, runtime/bootstrap publication, final guard binding, continuation parity, or P12-G atomic publication; those remain P12-G obligations. Daily-v1 still requires the selected ExplorableSite section to be exactly empty. Populated site export is outside this profile. Earlier State text saying that D package assembly, whole-D integration, or other accepted D owner coverage remains outstanding is superseded by this promotion record.

## Previous canonical promotion — P12-E Conflict owner snapshot (2026-10-08)

After refreshed preflight, `codex/phase12/canonical` advanced by clean
fast-forward from `f5d99cb7008023d14a0ed16ea2149a7d7c18def1` to
`ff733409b4f38b4078f21d81f845e62b66ba3392`. The integrated candidate is
`codex/phase12/P12EConflictCurrentBaseIntegration`; its reviewed Git tree is
`3e0904b93a374b474d9d78866d00f763ca936a3c` and its tested `Assets` tree is
`f503667082098813a87380b4c7f1c84de986970e`. Exact-tip independent review
returned `VALIDATED_CANDIDATE` at `b3ada0be866b789b52fb4ea23d6698cb27374c91`
on `codex/review/phase12/P12EConflictOwnerSnapshotImplementationReviewFF733R2`.
The candidate's production Conflict snapshot code is unchanged from the
current-base integrated implementation; this final candidate adds the two
review-requested regressions and exact-tree validation artifacts.

The bounded slice adds detached schema-v1 export and private staged
reconstruction for the Required Daily-v1 `p12e.conflicts` owner. It preserves
Conflict identity, lifecycle, sides, participant bindings, typed ArmedForce
references, and exact local revision; capture is bound to the existing
completed-boundary token and owner-section witness. Staging returns a private
Conflict store or no candidate. It does not add runtime/bootstrap integration,
operation wiring, shared-epoch or global-quiescence claims, whole P12-E package
composition, P12-G publication, or profile-wide capture eligibility.

Exact-tree validation passed: seven focused suites 63/63, ALL EditMode
2635/2635, official Smoke 5/5, and `git diff --check`. XML, compressed-log,
and decompressed raw-log hashes are in
[`validation/P12EConflictOwnerSnapshot/ReviewFix-20261008/VALIDATION.md`](validation/P12EConflictOwnerSnapshot/ReviewFix-20261008/VALIDATION.md).
The final preflight verified the clean fast-forward base, unchanged tested
`Assets` tree, exact review, and canonical synchronization.

### P12 dependency DAG after the Conflict promotion

- P12-B and P12-C remain COMPLETE/PROMOTED within their accepted bounded
  contracts; P12-D remains COMPLETE/PROMOTED within its accepted Daily-v1
  owner-export/private-staging boundary.
- P12-E remains IN PROGRESS. Battle, ArmedForce/manpower/position, and Conflict
  owner snapshots are promoted. Property/Estate and Institution/Office snapshot
  designs have independent exact-content review PASS and their bounded owner
  implementations are proceeding in separate isolated tracks. War snapshot
  implementation remains ordered after the Conflict owner API is canonical.
- P12-F waits on P12-E; P12-G waits on P12-B through P12-F and validated
  live-profile inventory.
- P12-A remains WAIT_DEPENDENCY; P13 remains BLOCKED; Phase 12 remains open.

This promotion does not claim full P12-E owner coverage, profile-wide
export/hydration, global quiescence, P12-A/P13 readiness, or Phase 12 closure.

### Historical P12-D dependency DAG (before the Conflict promotion)

- P12-B and P12-C remain COMPLETE/PROMOTED within their accepted bounded contracts.
- P12-D is COMPLETE/PROMOTED within the boundary above.
- P12-E remains IN PROGRESS. The Battle and ArmedForce/manpower/position slices remain promoted. The existing Conflict owner-snapshot candidate was based on prior canonical `a4ce0ab`; its focused 7/7 result is exploratory evidence only. Recompose it on `329c75a`, revalidate current-base design and integrated code, then complete required validation and exact-tip review before promotion.
- P12-F waits on P12-E; its P12-C and P12-D dependencies are satisfied.
- P12-G waits on P12-B through P12-F and validated live-profile inventory.
- P12-A remains WAIT_DEPENDENCY pending complete included-owner export/staged hydration, validated live inventory, and separate implementation authorization. P13 remains BLOCKED on its explicit continuation prerequisites. Phase 12 remains open.

This update does not claim profile-wide export/hydration, capture eligibility beyond the accepted B boundary contract, P12-A/P13 readiness, or Phase 12 closure.
## P12-D NPC D/F root owner snapshot — bounded checkpoint promotion record (2026-10-08)

The refreshed P12 canonical base for this candidate bundle is
`5acc3fff94f74fcb718610825caadf645423d672`. The bounded implementation is
candidate `7fb74bbc36bbc954e7ba0160b32657fbf9de2890` on
`codex/phase12/P12DNpcRootSnapshotImplementation`; its reviewed code tree is
`fcb19211f9e407fd4fe0553355abe293ae993780`, with exact `Assets` subtree
`d7e95170c31947fb611a7461133b60ce75ad1e4d`. Independent exact-tip review
returned `PASS — VALIDATED_CANDIDATE` at review commit
`e3628b2986c633ef0a147dcc4fb0b967339fe36c`, recorded in
[`design/PHASE12_D_NPC_ROOT_OWNER_SNAPSHOT_IMPLEMENTATION_REVIEW_7FB74.md`](design/PHASE12_D_NPC_ROOT_OWNER_SNAPSHOT_IMPLEMENTATION_REVIEW_7FB74.md).
The reviewed implementation candidate, exact-tip review record, and this
State entry form one bounded checkpoint promotion bundle. This State update
does not change source code or the `Assets` tree.

After final preflight, `codex/phase12/canonical` advanced by clean fast-forward
from `5acc3fff94f74fcb718610825caadf645423d672` to the promoted bundle tip
`4fc1e37246dc96da99638fe87f6e825e05d2dd2e`. The remote canonical ref was
verified synchronized at that SHA. The reviewed code tip, Git tree, and exact
`Assets` subtree above are unchanged; the retained exact-tip review and
validation remain applicable, so this documentation-only State finalization
does not rerun Unity.

The bounded P12-D slice captures NPC D/F values once under the same completed
Daily-v1 boundary token, stamp, and component revision vector; merges disjoint
D/F projections; and privately stages exact owner values and typed references
without replaying gameplay actions or writers. P18 LocalObservation and
MerchantTradeState are exact-zero census evidence only; their receipt data is
not exported or replayed. Daily-v1 continues to exclude ExplorableSite and
LocalTopology. The slice does not implement whole-D graph/runtime/bootstrap
publication or P12-G atomic publication.

Exact-tree validation passed: NPC D/F snapshot 22/22, City-root regression
17/17, receipt-owner regression 13/13, ALL EditMode 2624/2624, official Smoke
5/5, and cumulative `git diff --check`. XML, compressed-log, and decompressed
raw-log hashes are recorded and cross-checked in
[`validation/P12DNpcRootOwnerSnapshot/VALIDATION.md`](validation/P12DNpcRootOwnerSnapshot/VALIDATION.md).

### Refreshed P12 dependency DAG

- P12-B and P12-C remain COMPLETE/PROMOTED within their accepted bounded
  contracts.
- P12-D remains IN PROGRESS. Its promoted Genealogy, SpatialNetwork, Person,
  ExplorableSite, City, exact-zero receipt census, and City/NPC relation-order
  assembly remain intact. This bounded promotion delivers the NPC D/F value
  snapshot, projection merge, and private staged reconstruction. Whole-D
  package/graph integration and runtime/bootstrap publication remain
  outstanding.
- P12-E remains IN PROGRESS; Battle and the ArmedForce/manpower/position owner
  snapshot are promoted, while wider accepted owner coverage and integration
  remain outstanding.
- P12-F waits for P12-C, P12-D, and P12-E.
- P12-G waits for P12-B through P12-F and a validated live-profile inventory.
- P12-A remains WAIT_DEPENDENCY pending complete included-owner export and
  staged hydration, validated live inventory, and separate implementation
  authorization. P13 remains BLOCKED on its explicit continuation
  prerequisites.

Before the next D package-integration checkpoint, obtain a separate
current-base readiness review. It must resolve the Person staging-order
discrepancy: the D technical design describes restoring Persons after NPC
staging, while the current-base crosswalk and implementation API stage and bind
Persons before NPC staging. It must also reconcile stale P12-G City ownership
wording that describes merged D/E City ownership with the current D design's
single D-owned City package. This candidate does not resolve those issues or
claim whole-D integration.

P12-D remains IN PROGRESS, P12-A remains WAIT_DEPENDENCY, P13 remains BLOCKED,
and Phase 12 remains open. This candidate does not claim capture eligibility,
profile-wide export/hydration, P12-A or P13 readiness, or Phase closure.

## Latest canonical promotion — P12-E ArmedForce/manpower/position owner snapshot (2026-10-08)

After refreshed preflight, `codex/phase12/canonical` advanced from
`ed3aad0bcf98fc1b709b6bc632452689448b8803` through the exact-tip reviewed
P12-E candidate bundle on `codex/phase12/P12ECurrentBasePromotionIntegrationE161093`.
The candidate is `codex/phase12/P12ECurrentBaseRevalidation` at
`e161093a56a0308314895eebe7e89b251eea7092`; its tested code/test tip is
`99302cffec8cda35945ed191bd3dba98beb7bd57`, implementation commit
`d57120cc8e7876910caec6f2a134231a8a23bfee`, and tested/final `Assets` tree
`811f8018c5722a6cf5a0bbf54ec04a9f73f77397`. P12 canonical was the candidate's
clean fast-forward base. The two independent exact-tip review records are
`b70894dae5cb13eeabbe55e8affeeab113985dd5` and
`414cf855cf75de5930d58485582236323d028396`; both returned
`VALIDATED_CANDIDATE` on the unchanged tree. The promotion bundle adds those
review records and this State update without changing code.

The bounded slice adds detached value export and private staged reconstruction
for the accepted Daily-v1 `ArmedForceStore`, `ContingentManpowerStateStore`,
and `ArmedForceSpatialStateStore` owner group. It consumes the existing five
required P12-B owner sections and one completed-boundary token/vector; it adds
no mutation wiring, epoch behavior, bootstrap/runtime integration, or new census
provider. It preserves exact owner-local revisions, force/contingent/person
references, manpower cohorts, and typed P8 positions. Existing owner
invariants are validated before staged owners are returned.

Exact-tree validation passed: P12-E focus 10/10, Continuation Protocol 24/24,
P12-D receipt-owner 13/13, City root 17/17, bootstrap composition 26/26,
ALL EditMode 2602/2602, official Smoke 5/5, and `git diff --check`. The
manifest and XML/log hashes are in
[`validation/P12ECurrentBaseRevalidation/VALIDATION.md`](validation/P12ECurrentBaseRevalidation/VALIDATION.md).
The only `.meta` changes are the paired Unity metadata files for the new
snapshot source and its Editor tests; no ProjectSettings or existing/unrelated
metadata files changed.

P12-E remains IN PROGRESS. This is one ArmedForce/manpower/position owner slice;
it does not complete P12-E, establish full owner or shared-epoch coverage,
prove global quiescence or capture eligibility, provide profile-wide export/
hydration or P12-G publication, make P12-A/P13 ready, or close Phase 12.
P12-D remains IN PROGRESS; its promoted City/NPC relation-order assembly is
preserved, while whole-D graph integration, runtime/bootstrap publication, and
remaining D owner coverage remain outstanding. The isolated NPC D/F snapshot
work is still under implementation and is not part of this promotion.

### Refreshed P12 dependency DAG

- P12-B and P12-C remain COMPLETE/PROMOTED within their accepted bounded
  contracts.
- P12-D remains IN PROGRESS: promoted Genealogy, SpatialNetwork, Person,
  ExplorableSite, City, receipt-owner witnesses, and City/NPC assembly; NPC
  owner snapshot/private staging, whole-D graph integration, and runtime
  publication remain outstanding.
- P12-E remains IN PROGRESS: Battle and this ArmedForce/manpower/position owner
  snapshot are promoted; other accepted core/daily-domain owner coverage and
  integration remain outstanding.
- P12-F waits for P12-C, P12-D, and P12-E.
- P12-G waits for P12-B through P12-F and a validated live-profile inventory.
- P12-A remains WAIT_DEPENDENCY pending complete included-owner export and
  staged hydration, validated live inventory, and separate implementation
  authorization. P13 remains BLOCKED on its explicit continuation prerequisites.

Phase 12 remains open. This bounded promotion does not imply P12-A/P13
readiness, profile-wide save/hydration, or Phase closure.

## Latest canonical promotion — P12-D City/NPC relation-order assembly (2026-10-08)

After final preflight, `codex/phase12/canonical` was advanced from
`1c7b906c172d9e47020996888db64bd2516b451a` through the reviewed candidate
`483edb791f523d797e9b52e88eda00fc34eaa5ff`. Its implementation code commit
is `5aceb2b7ce49ffe009489627731cd6689fe2d200`, reviewed code-commit tree
`bdc866e9a87091d029909aaa3767c44d1120337f`, and reviewed `Assets` tree
`e6c0776052dbfdd8a83ce51eb15fd15cf9d89b2c`. The candidate branch is
`codex/phase12/P12DCityNpcAssemblyImplementation`.

The exact-tip independent review is PASS at review commit
`28a4f060c84ad568bb9f3d21faabb6581af2d0bb`, recorded in
[`design/PHASE12_D_CITY_NPC_ASSEMBLY_IMPLEMENTATION_REVIEW_483EDB.md`](design/PHASE12_D_CITY_NPC_ASSEMBLY_IMPLEMENTATION_REVIEW_483EDB.md).
The earlier review of candidate `92ad87f` and its superseding NEEDS_CHANGES
addendum are retained in
[`design/PHASE12_D_CITY_NPC_ASSEMBLY_IMPLEMENTATION_REVIEW_92AD87F.md`](design/PHASE12_D_CITY_NPC_ASSEMBLY_IMPLEMENTATION_REVIEW_92AD87F.md)
and
[`design/PHASE12_D_CITY_NPC_ASSEMBLY_IMPLEMENTATION_REVIEW_ADDENDUM_92AD87F.md`](design/PHASE12_D_CITY_NPC_ASSEMBLY_IMPLEMENTATION_REVIEW_ADDENDUM_92AD87F.md).
The addendum supersedes that earlier PASS due to a separable snapshot/evidence
pairing hole; the accepted candidate replaces it with a privately constructed
capture envelope binding City values to their token, stamp, and owner-vector
identity.

Validation on the unchanged code tree passed the City assembly suite 17/17,
receipt-owner suite 13/13, ALL EditMode 2592/2592, official Smoke 5/5, and
cumulative `git diff --check`. Exact XML/log/source hashes are in
[`validation/P12DCityNpcAssembly/VALIDATION-FOLLOWUP-CAPTURE-ENVELOPE-5ACEB2B.md`](validation/P12DCityNpcAssembly/VALIDATION-FOLLOWUP-CAPTURE-ENVELOPE-5ACEB2B.md).

This bounded slice stages City/NPC presence and validates ordered reciprocal
City/NPC/location relations before any membership fill. It preserves captured
ordering and local revision, rejects mismatched City/D/F capture identity and
exact-zero receipt-owner evidence gaps, and performs no gameplay mutation
during staging. It does not implement runtime/bootstrap publication, broader
P12-D owner coverage, profile-wide export/hydration, P12-A/P13 behavior, or
Phase closure.

P12-D remains IN PROGRESS. City/NPC staged assembly and its reciprocal
relation validation are now promoted; runtime/bootstrap publication, whole-D
graph integration, and remaining accepted D owner coverage remain outstanding.
P12-E remains IN PROGRESS; its ArmedForceManpowerPosition snapshot candidate
must be recomposed and revalidated on this new canonical base before
integration. P12-F still waits for P12-C/D/E; P12-G waits for P12-B through
P12-F plus validated live-profile inventory. P12-A remains WAIT_DEPENDENCY and
P13 remains BLOCKED. Phase 12 remains open.

This promotion does not imply P12-D completion, complete owner/shared-epoch
coverage, capture eligibility, profile-wide export/hydration, P12-A/P13
readiness, or Phase closure.

## Latest canonical promotion — P12-D exact-zero receipt-owner census (2026-10-08)

After a final remote preflight, `codex/phase12/canonical` was advanced from
`04e7c3f49a7c9690ebc091fcfc50f05ef67009d6` through reviewed candidate
`cc9f11471887c71f2a9c4834e1bcc1edeb3025fe`. Its reviewed code/test commit is
`4947ec926b3a48427e9c07de5d722d45014cf1bf`, code/test tree
`dd0cda707693237a56f5664b2eabc8aa6821b100`, and reviewed `Assets` tree
`b239758a62c9c670f7400af2567515308e138a4f`. The independent exact-tip
implementation review is PASS at
`85acfead11d84a8940fe6da87ace6fab243ffc56`, recorded in
[`design/PHASE12_D_CITY_NPC_RECEIPT_ZERO_WITNESS_IMPLEMENTATION_REVIEW_R3.md`](design/PHASE12_D_CITY_NPC_RECEIPT_ZERO_WITNESS_IMPLEMENTATION_REVIEW_R3.md).
The accepted design and its independent review remain
[`design/PHASE12_D_CITY_NPC_RECEIPT_ZERO_WITNESS_DESIGN.md`](design/PHASE12_D_CITY_NPC_RECEIPT_ZERO_WITNESS_DESIGN.md)
and
[`design/PHASE12_D_CITY_NPC_RECEIPT_ZERO_WITNESS_DESIGN_REVIEW.md`](design/PHASE12_D_CITY_NPC_RECEIPT_ZERO_WITNESS_DESIGN_REVIEW.md).

Validation on that unchanged code tree passed the receipt-owner focused suite
13/13, ALL EditMode 2582/2582, official Smoke 5/5, and `git diff --check`.
The exact XML/log hashes and committed/worktree source-byte correspondence
are recorded in
[`validation/P12DCityNpcReceiptOwnerCensus/VALIDATION-FOLLOWUP-50C9206.md`](validation/P12DCityNpcReceiptOwnerCensus/VALIDATION-FOLLOWUP-50C9206.md).
The source-hash correction changes documentation only; the reviewed `Assets`
tree is unchanged.

This slice adds the selected Daily-v1 exact-zero identity/cardinality/revision
witnesses for the two excluded P18 receipt owners under each applicable NPC:
LocalObservation and MerchantTradeState. It closes the prior evidence gap
where absence from the census vector did not prove exact empty state. It adds
no P18 receipt export or replay, no new P12-B operation or mutation semantics,
and no broader owner/shared-epoch, capture-eligibility, export, or hydration
claim.

P12-D remains IN PROGRESS. The missing receipt-owner evidence is supplied, so
the existing City/NPC assembly design may now be revalidated against this
canonical tip. City/NPC shared assembly, exact merged-graph validation,
runtime integration, and remaining accepted D owner coverage remain
outstanding; this promotion does not complete P12-D.

### Refreshed P12 checkpoint DAG

- P12-B: COMPLETE/PROMOTED within its bounded Daily-v1 admission and
  completed-boundary lifecycle contract.
- P12-C: COMPLETE/PROMOTED within its accepted identity, genesis-provenance,
  and deterministic-root continuation scope.
- P12-D: IN PROGRESS. Genealogy, SpatialNetwork, Person, ExplorableSite,
  isolated City, and the two exact-zero P18 receipt-owner census witnesses
  are promoted. Revalidate the existing City/NPC assembly design; shared
  assembly, merged-graph validation, runtime integration, and remaining D
  owner coverage remain.
- P12-E: IN PROGRESS. Battle owner snapshot is promoted. The bounded
  ArmedForceManpowerPosition snapshot candidate is independently reviewed
  and validated on its prior P12 base; it must be revalidated/recomposed after
  this D promotion before integration.
- P12-F: waits for P12-C, P12-D, and P12-E.
- P12-G: waits for P12-B through P12-F plus a validated live-profile
  inventory.
- P12-A: remains WAIT_DEPENDENCY until all included owners have exact export
  and staged hydration, live inventory is validated, and separate
  implementation authorization is recorded.
- P13: remains BLOCKED on its explicit continuation prerequisites.

Phase 12 remains open. This promotion does not imply P12-D completion,
P12-A/P13 readiness, profile-wide export/hydration, or Phase closure.

This current record supersedes the readiness snapshot in the earlier
P12-E Battle promotion section below; that section is retained as history.

## Earlier bounded owner promotion — P12-E Battle snapshot (2026-10-08)

After refreshing `origin/codex/phase12/canonical` at
`ef0cafb5848cadcf0ac91a3e1af7ff3faaab1367`, the bounded Battle owner snapshot
was integrated from candidate branch `codex/phase12/P12EBattleOwnerSnapshotImplementation`.
Its code-bearing commit is `2f78244c5a4dc96d3ac45e87335b341912b81947`, based
directly on that canonical tip, with reviewed `Assets` tree
`7a92f17b81dea315ef9cf4330421749a41f08767`. The integration candidate before
this State refresh was `111fb37` (docs and validation records after the code
commit do not change its `Assets` tree).

The bounded technical design is
[`design/PHASE12_P12E_BATTLE_OWNER_SNAPSHOT_DESIGN.md`](design/PHASE12_P12E_BATTLE_OWNER_SNAPSHOT_DESIGN.md),
with independent design review PASS in
[`design/PHASE12_P12E_BATTLE_OWNER_SNAPSHOT_DESIGN_REVIEW_9B1B1A.md`](design/PHASE12_P12E_BATTLE_OWNER_SNAPSHOT_DESIGN_REVIEW_9B1B1A.md).
Independent exact-tip implementation review PASS, with no findings, is recorded
in
[`design/PHASE12_P12E_BATTLE_OWNER_SNAPSHOT_IMPLEMENTATION_REVIEW.md`](design/PHASE12_P12E_BATTLE_OWNER_SNAPSHOT_IMPLEMENTATION_REVIEW.md).
Validation on the unchanged code tree passed the five focused suites 52/52,
ALL EditMode 2569/2569, official Smoke 5/5, and `git diff --check`; exact XML,
compressed/raw log hashes, and source blob IDs are in
[`validation/P12EBattleOwnerSnapshot/VALIDATION.md`](validation/P12EBattleOwnerSnapshot/VALIDATION.md).

This slice adds detached schema-v1 export and private staged reconstruction
for the existing `PersistentBattleStore` in the accepted Daily-v1 profile. It
consumes the exact successful P12-B token/vector and `p12e.battles` owner
witness; it reconstructs Battle rows against staged Force, Conflict, War, and
spatial parents, preserves the exact local revision, and rejects LocalTopology
because it remains `NOT_COMPOSED` in Daily-v1. It does not add runtime/bootstrap
publication or P12-G graph publication.

### Refreshed P12-D/E readiness and owner DAG

- P12-B remains COMPLETE/PROMOTED within its bounded admission and
  completed-boundary lifecycle contract. This slice does not broaden or reopen
  it.
- P12-C remains COMPLETE/PROMOTED within its accepted identity,
  genesis-provenance, and deterministic-root continuation scope. No further
  P12-C obligation is missing inside that accepted checkpoint. This does not
  deliver a save envelope, active-runtime publication, P12-G validation,
  copied-save branching, P13 history, or fork semantics.
- P12-D remains IN PROGRESS. Genealogy, SpatialNetwork, Person, ExplorableSite,
  and isolated City root snapshots remain promoted. City/NPC shared assembly,
  exact merged-graph validation, runtime integration, and remaining D owner
  coverage remain outstanding. The current-source crosswalk and exact-tip
  review are recorded in
  [`design/PHASE12_D_CITY_NPC_CURRENT_BASE_FIELD_CROSSWALK.md`](design/PHASE12_D_CITY_NPC_CURRENT_BASE_FIELD_CROSSWALK.md)
  and
  [`design/PHASE12_D_CITY_NPC_CURRENT_BASE_FIELD_CROSSWALK_REVIEW_3B1C850.md`](design/PHASE12_D_CITY_NPC_CURRENT_BASE_FIELD_CROSSWALK_REVIEW_3B1C850.md).
  It identifies two nested P18 receipt owners without a P12 owner-section
  identity/cardinality/revision witness. Their absence from the census vector
  is not proof of exact empty state; City/NPC implementation stays blocked
  until that evidence is supplied under the accepted profile boundary.
- P12-E is IN PROGRESS. The Battle owner snapshot/private staging capability is
  promoted as one owner slice. Other selected core and daily-domain authorities
  still need owner-specific field/writer/revision coverage, detached export,
  private staged reconstruction, and cross-owner checks; this promotion does
  not complete P12-E.
- P12-F remains dependent on P12-C, P12-D, and P12-E.
- P12-G remains dependent on P12-B through P12-F plus a validated live-profile
  inventory.
- P12-A remains WAIT_DEPENDENCY until all included owners have exact export
  and staged hydration, live inventory is validated, and separate
  implementation authorization is recorded. P13 remains BLOCKED on its
  explicit continuation prerequisites. Phase 12 remains open.

No complete P12-D/E export coverage, P12-A readiness, P13 readiness, or Phase
closure is implied.
## Latest canonical promotion — P12-D City root owner snapshot (2026-10-08)

After refreshing P12 canonical at aa1a40f2e6da53a1a7388601bd13052f1a535545, the
bounded City owner snapshot was promoted through reviewed candidate
22f20b1ab0cf9ba4fc36155c645554c98bff9c6e and exact-tip review record
14afa99c802305d75761e589c47e7997276c80c0. The implementation commit is
58c142329d034fbed18ece25ee017d0d69f4d62f; its reviewed Assets tree is
e83151063141cb1395e1d53d30e850c6f81ed778. The durable implementation review is
[PHASE12_P12D_CITY_ROOT_SNAPSHOT_IMPLEMENTATION_REVIEW_22F20B1.md](design/PHASE12_P12D_CITY_ROOT_SNAPSHOT_IMPLEMENTATION_REVIEW_22F20B1.md);
source and validation hashes are in
[P12DCityRootOwnerSnapshot/VALIDATION.md](validation/P12DCityRootOwnerSnapshot/VALIDATION.md).

Validation passed the focused City snapshot suite 7/7, ALL EditMode 2563/2563,
official Smoke 5/5, and git diff --check. Review verified all five committed
source-blob hashes and retained result/log artifact hashes. The only post-code
candidate change corrected three source hashes in documentation; no executable
tree changed after validation.

This slice provides detached capture and private exact-value staging for the
selected City root, Market rows, Market counterparty, PopulationEconomy, and
SettlementPopulation, including ordered City membership, stored market prices,
local revisions, and retained population operation receipts. Capture consumes
the exact Daily-v1 completed-boundary token, shared stamp, and four matching
City owner sections. It admits only Open/Free account-free City economy and
rejects P18 City daily receipts or P14 material-flow state.

P12-D remains IN PROGRESS. This promotion does not add NPC serialization,
shared City/NPC graph construction, runtime/bootstrap integration, whole-D
cross-owner validation, P12-G publication, profile-wide export/hydration,
P12-A readiness, P13 readiness, or Phase closure. City/NPC construction and
relation assembly remain a serialized integration hotspot.

### Refreshed P12 checkpoint DAG

- P12-B: COMPLETE/PROMOTED within its bounded profile-admission and completed-
  boundary lifecycle contract.
- P12-C: COMPLETE/PROMOTED within its accepted identity, genesis-provenance,
  and deterministic-root continuation scope.
- P12-D: IN PROGRESS. Genealogy, SpatialNetwork, Person, ExplorableSite, and
  now the isolated City root owner snapshots are promoted. Shared City/NPC
  assembly, exact cross-owner graph validation, runtime integration, and
  remaining accepted D owner coverage are outstanding.
- P12-E: no owner export/staged-hydration slice is currently
  READY_FOR_IMPLEMENTATION. Existing passive census witnesses do not provide
  exact detached export or private staged reconstruction. Re-evaluate after
  owner-specific design and current-source evidence.
- P12-F: waits for P12-C, P12-D, and P12-E.
- P12-G: waits for P12-B through P12-F plus validated live-profile inventory.
- P12-A: remains WAIT_DEPENDENCY until every included owner has exact export
  and staged hydration, the live profile inventory is validated, and its
  separate implementation authorization is recorded.
- P13: remains BLOCKED on its explicit continuation and recoverable-history
  prerequisites.

Phase 12 remains open. No complete P12-D/E export coverage, P12-A readiness,
P13 readiness, or Phase closure is implied.

## Current P12-D status — Genealogy, SpatialNetwork and Person owner snapshots promoted (2026-10-08)

`codex/phase12/canonical` was fast-forwarded from
`0e786db8e6ed5ed937ff62e3f63258d8b73fd93c` to promotion tip
`66ea303f8a49f0784130c79400f07dc30f54a63d`. The bounded Genealogy owner
implementation is code `2b3de7cd13e6f35d35d5dece56eefb64c1fd972a`, with validated `Assets`
tree `127bc9dc10c0e69312ae98748f04becbe2d8ceda`. Its exact-tip
implementation review is PASS at review commit
`9d101c50b8c6d1c161d741bcd71dd158008820f0`; design review of the current D
contract is PASS at `b9a0fd1b5941f0b7615a72acec39fd22e6c9ee0e`, recorded in
[`design/PHASE12_D_TECHNICAL_DESIGN_REVIEW_0735103.md`](design/PHASE12_D_TECHNICAL_DESIGN_REVIEW_0735103.md).
Focused Genealogy tests passed 24/24, all Genealogy tests 52/52, birth lifecycle
15/15, ALL EditMode 2540/2540, official Smoke 5/5, and `git diff --check`.
Exact source and validation artifact hashes are in
[`validation/P12DGenealogyOwnerSnapshot/VALIDATION.md`](validation/P12DGenealogyOwnerSnapshot/VALIDATION.md).

The slice adds detached immutable schema-v1 parentage records and an
unpublished staged `GenealogyStore` factory that preserves the exact local
revision and rebuilds local graph indexes. It validates local edge shape,
uniqueness, self-edges, cycles, and revision bounds. The outer D graph must
still bind capture to the P12-B completed-boundary token/revision vector and
validate both endpoints against staged `PersonStore`; neither is implemented
by this owner slice. No runtime/bootstrap publication, whole-D graph
validation, profile-wide export/hydration, P12-A readiness, P13 readiness, or
Phase closure is claimed.

The canonical branch was then fast-forwarded from
`ad4b20c42a25c9fd453c98695a79ce59490bf4fe` to
`f6f5ff2507200d72e5c4194eae60cbc21559e65c`. This promotes the isolated
`SpatialNetworkRuntime` owner snapshot: code `39ad61d60907154c4af52ea1bf01bfe94322d90f`,
validated `Assets` tree `1e00c94447bf202acf7517b14a5aed6043ae4eec`, exact-tip
review PASS recorded at `de3ed27be5d03aa58f49397ed80a9875abf8c5c7`.
Validation passed the focused census suite 11/11, ALL EditMode 2544/2544,
official Smoke 5/5, and `git diff --check`. The validation manifest's source
hashes were corrected against the committed Git blobs; that documentation-only
correction did not change the validated `Assets` tree. See
[`validation/P12DSpatialNetworkOwnerSnapshot/VALIDATION.md`](validation/P12DSpatialNetworkOwnerSnapshot/VALIDATION.md)
and
[`design/PHASE12_P12D_SPATIAL_NETWORK_OWNER_SNAPSHOT_IMPLEMENTATION_REVIEW.md`](design/PHASE12_P12D_SPATIAL_NETWORK_OWNER_SNAPSHOT_IMPLEMENTATION_REVIEW.md).

This slice exports legacy location/route identity and ordered values with the
owner's exact local revision, stages them against the existing C identity
registry, and rebuilds the derived outgoing-route index. Parallel routes and
stored durations remain distinct. It adds no `SimulationRuntime` or bootstrap
integration, P12-B capture binding, whole-D validation, profile-wide
export/hydration, P12-A readiness, P13 readiness, or Phase closure.

The promoted Person owner snapshot implementation is code
6c872c5ca6e997844d01c18bfee8909c16317455 with reviewed Assets tree
b84164f70736c1d5c7643e107f06178798238ca0. Independent exact-tip review PASS
is recorded at 77d0383b92a5368ff93a04fc254cf4c529480b11 and included in the
integration candidate. Focused Person tests passed 182/182; integrated ALL
EditMode passed 2549/2549; official Smoke passed 5/5; git diff --check passed.
Artifact hashes and the scope boundary are in
[validation/P12DPersonSnapshotIntegration/VALIDATION.md](validation/P12DPersonSnapshotIntegration/VALIDATION.md).
Following final preflight, codex/phase12/canonical was fast-forwarded from
da2a73896bc405ae6f11c536a5fbe8d471b00c21 to promotion tip
69882ed2ef9f15893a27897c04ba6242f9f86aa4. This promotes only the bounded
local Person owner snapshot and private staging capability.

Current design revalidation uses architecture canonical 47eff220c7ce00f6e7c759bdc2b76780bb46f628, including the intraday/extensibility and multi-participant activity alignment records. P12-B and P12-C remain complete within their accepted bounded contracts; the latest P12 dependency graph is recorded below. The numbered-phase execution order continues to follow documented edges rather than phase numbers.
## Latest canonical promotion — P12-D ExplorableSite owner snapshot (2026-10-08)

After refreshing P12 canonical at dbba3e9a227f66da0381e3e042e826518d63c240,
the bounded Site owner snapshot was promoted by fast-forward to
366dc6cb2a605c6d1fc2b9c11518d6d2972488cc. The promoted tip includes integration
candidate f005be6f9a46c35c1cc7c467914073bd264b4dec and its exact-tip review
record. Implementation code remains f4f0f5d6e54c87638ce00261fb4d0add0803c5ef
with validated Assets tree 71d917e5bd4090368f5be1536a6cbb2e789bed64.
The final integration review passed as BASE_DRIFT_ONLY; the earlier current-base
review assessed a prior integration tip and is retained as historical evidence.

The promotion adds only the isolated ExplorableSite owner snapshot/private staging
factory and the selected Daily-v1 exact-empty proof. P12-D remains IN PROGRESS:
it does not add B token/vector binding, whole-D graph validation, runtime
publication, or City/NPC composition. P12-E has no implementation-ready
export/hydration slice; P12-F still waits for D and E, and P12-G waits for B-F
plus validated live-profile inventory. P12-A remains WAIT_DEPENDENCY, P13
remains BLOCKED, and Phase 12 remains open.
### Refreshed P12 dependency and owner DAG

- **P12-B and P12-C:** remain COMPLETE/PROMOTED within their recorded bounded
  contracts; the current P12-D slice does not reopen them.
- **P12-D:** IN PROGRESS. Genealogy, SpatialNetwork, and Person local-owner
  snapshots are promoted. The isolated Site implementation f4f0f5d6e54c87638ce00261fb4d0add0803c5ef is
promoted at P12 canonical promotion tip 366dc6cb2a605c6d1fc2b9c11518d6d2972488cc.
Its reviewed Assets tree is 71d917e5bd4090368f5be1536a6cbb2e789bed64. Exact-tip
implementation review
  PASS is recorded at `8290a9a9`; current-base revalidation PASS
  (`BASE_DRIFT_ONLY`) is recorded at `16dce50d` against P12 canonical
  `dbba3e9`. Final exact-tip integration review PASS is recorded at 366dc6cb2a605c6d1fc2b9c11518d6d2972488cc, reviewing candidate f005be6f9a46c35c1cc7c467914073bd264b4dec against dbba3e9; Site census 12/12, selected Daily-v1 exact-empty profile 1/1, ALL
  EditMode 2556/2556, official Smoke 5/5, and `git diff --check` pass; artifacts
  and hashes are in
  [`validation/P12DExplorableSiteOwnerSnapshot/VALIDATION.md`](validation/P12DExplorableSiteOwnerSnapshot/VALIDATION.md).
  The live `AddCore` path is unchanged; capture rejects duplicate
  `SiteInstanceId` state that cannot round-trip. Current integration still
  provides only the isolated owner factory, without P12-B token/vector binding,
  whole-D cross-owner validation, runtime publication, or P12-A integration.
  Independent City/NPC design review of original candidate `4d1b7d32` found
  profile-boundary issues at `b08acd1e`; corrected candidate `9e0dcce1` passed
  exact-content review at `c65499d3`. The correction preserves Daily-v1's
  `p12d.explorable-sites` required-empty section, consumes P12-E's typed
  `NOT_COMPOSED` LocalTopology witness, and defers generic populated-site rows
  to a future profile. It grants no City/NPC implementation readiness because
  field-complete owner evidence is still absent. P10-A remains a separate
  proving profile. City/population and the D/E/F NPC adapter remain serialized
  shared-owner work; whole-D staging remains outstanding.
- **P12-E:** The LocalTopology NOT_COMPOSED correction passed exact-content
  design review at cdea870ef9eeb27603d021bac97ec56a81856617 (durable review
  branch tip d40e222affe67690287887195dfdc425798cd7e1). Current-base
  revalidation against P12 63cb5e7 and architecture 47eff220 is PASS and
  classifies the Person promotion as BASE_DRIFT_ONLY. The corrected design
  requires typed provider absence for Daily-v1 and keeps P10 separate. The
  docs-only correction and review records were promoted from 63cb5e7 to P12
  canonical `dbba3e9`; the correction adds no capability and the readiness DAG
  is unchanged. No E owner slice is READY_FOR_IMPLEMENTATION: complete owner
  fields, writes and revisions, detached export, and staged reconstruction
  evidence remain outstanding.
- **P12-F:** waits for P12-D and P12-E. Expedition remains in its accepted
  P12-F scope and is not advanced by this D slice.
- **P12-G:** waits for P12-B through P12-F and a validated live-profile
  inventory.
- **P12-A:** remains `WAIT_DEPENDENCY` until every included owner has exact
  export/staged hydration, the live inventory is validated, and its separate
  authorization is recorded.
- **P13:** remains `BLOCKED` on its explicit continuation prerequisites.

Phase 12 remains open. These owner promotions do not establish complete
P12-D, P12-A readiness, P13 readiness, or Phase closure.

## Current canonical status — P12-C private root composition promoted (2026-10-08)

After exact-tip review and validation, `codex/phase12/canonical` was
fast-forwarded from `6886f5876c756a7abb86c541f6d783da885941d0` through
candidate `27962a0ce64f74295704719d4258150c6e83e1d0`. The reviewed code
implementation is `f32f5d895deedff176c09dbcc19ed622dd5226ce`, code tree
`fdf3d684af1b471643a7f940e128a90ad7445a9c`. The promotion branch tip carries
the unchanged reviewed code plus the exact-tip review, result XML, and durable
raw-log archive. Independent implementation review is recorded in
[`design/PHASE12_P12C_PRIVATE_ROOT_COMPOSITION_IMPLEMENTATION_REVIEW.md`](design/PHASE12_P12C_PRIVATE_ROOT_COMPOSITION_IMPLEMENTATION_REVIEW.md);
source hashes and validation evidence are in
[`validation/P12CPrivateRootComposition/VALIDATION.md`](validation/P12CPrivateRootComposition/VALIDATION.md).

The private all-or-none Daily-v1 root now composes the promoted allocator and
record-sequence snapshots, P8-A geography, P9-B manifest/provenance,
deterministic-random root, and the already-published `WorldId`. Cross-owner
checks retain the exact P8 facts and P9 provenance, validate the selected
profile/schema and effective seed, and reject missing, malformed, duplicate,
or contradictory reserved provenance tags. Validation on the unchanged code
tree passed the new composition suite 51/51, seven affected owner/integration
suites 67/67, ALL EditMode 2535/2535, official Smoke 5/5,
`SimulationRuntimeLongRunTests` 7/7, and `git diff --check`.

This completes P12-C's accepted private identity/genesis-provenance and
deterministic-root composition capability. It adds no serialized save
envelope, capture hook, active-runtime publication, P12-G whole-graph
validation, copied-save branching, or P13 fork semantics. It does not make
P12-A ready or close Phase 12.

### Refreshed P12 dependency DAG

- **P12-B:** COMPLETE/PROMOTED within the bounded profile-admission and
  completed-boundary lifecycle contract. This P12-C promotion does not reopen
  or broaden it.
- **P12-C:** COMPLETE/PROMOTED within the accepted identity, genesis
  provenance, and deterministic-root continuation scope.
- **P12-D and P12-E:** their P12-B and P12-C capability dependencies are now
  satisfied. Revalidate their accepted technical boundaries against current
  canonical architecture, profile inventory, and owner code before assigning
  implementation readiness; keep their shared runtime/bootstrap integration
  serialized.
- **P12-F:** waits on P12-C, P12-D, and P12-E. Expedition remains deferred to
  its documented P12-F scope.
- **P12-G:** waits on P12-B through P12-F and a validated live-profile
  inventory.
- **P12-A:** remains `WAIT_DEPENDENCY` until every included owner has exact
  export and staged hydration, the live profile inventory is validated, and
  its separate implementation authorization is recorded.
- **P13:** remains `BLOCKED` on its documented continuation and recoverable
  causal-history prerequisites.

Phase 12 remains open. This promotion establishes no profile-wide
export/hydration, P12-A readiness, P13 readiness, or Phase closure.

## Previous canonical status — P12-C owner-continuation composition slice promoted (2026-10-08)

After refreshing `origin/codex/phase12/canonical`, the branch was fast-forwarded
from `82125b8e20ca997226ede0069bc875472cf90430` to the reviewed composition
candidate `4fb2721dc4bee8fd3d7543260a74252f9687d7a1`. Its implementation code
tip is `4001c2471df9088e2e51e7a1420bfa0e9b385b88`, full tree
`dc54747a3c64ab9189c1425e0a6d92eba38f33b7`, and validated `Assets` subtree
`52b91d11785c2adc32d09abb21deb2e61a820fc2`. The exact-tip independent review
is `VALIDATED_CANDIDATE` at `058bbbd2757ec9b464081a7a3e5fdacf7f89ce22` on
`codex/phase12/P12COwnerContinuationIntegrationReviewRecord-4fb2721`; the
candidate and review record are published on their named remote branches.

The promoted slice carries the accepted P8-A geography snapshot/private
reconstruction, P9-B genesis-manifest provenance snapshot/reconstruction, and
the selected deterministic-random provider root. The prior promoted P12-C
identity/sequence snapshot remains in history. The P9 manifest's complete
`OutputOwners` list and the producer's existing canonical provenance records
remain separate and unchanged; this slice does not alter P9 fingerprint
generation.

Validation remains bound to code tree `dc54747a3c64ab9189c1425e0a6d92eba38f33b7`:
focused suites passed (spatial snapshot 7/7, P9 manifest 3/3, random root 7/7,
bootstrap composition 26/26), ALL EditMode `2484/2484`, official Smoke `5/5`,
SimulationRuntime LongRun `7/7`, and `git diff --check`. Exact source, XML,
and log hashes are in
[`PHASE12_P12C_OWNER_CONTINUATION_COMPOSITION_EVIDENCE.md`](design/PHASE12_P12C_OWNER_CONTINUATION_COMPOSITION_EVIDENCE.md).

P12-B is already `COMPLETE/PROMOTED` within its accepted contract. The current
canonical code and State include the reviewed evidence closure, supported
owner/ingress and committed-write reconciliation, runtime-wide owner-thread/
quiescence, and the token issued only after a successful completed boundary;
the exact review and validation are linked below. This P12-C promotion does
not reopen or broaden P12-B.

### Refreshed dependency DAG

- **P12-B:** COMPLETE/PROMOTED within its bounded profile-admission and
  completed-boundary lifecycle contract.
- **P12-C:** IN PROGRESS; the identity/sequence slice and the P8-A/P9-B/
  deterministic-root composition slice are promoted. This candidate is partial
  P12-C work and does not itself satisfy the complete P12-C dependency edge.
- **P12-D and P12-E:** remain blocked until P12-C is complete; do not infer
  readiness from this partial promotion.
- **P12-F:** waits on P12-C, P12-D, and P12-E. Expedition remains deferred to
  its documented P12-F scope.
- **P12-G:** waits on P12-B through P12-F and a validated live-profile
  inventory.
- **P12-A:** remains `WAIT_DEPENDENCY` until every included owner has exact
  export and staged hydration, the live profile inventory is validated, and
  its separate implementation authorization is recorded.
- **P13:** remains `BLOCKED` on its documented continuation and recoverable
  causal-history prerequisites.

Phase 12 remains open. This promotion establishes no profile-wide
export/hydration, P12-A readiness, P13 readiness, or Phase closure.

## Previous canonical status — P12-C identity and sequence snapshot slice promoted (2026-10-07)

`codex/phase12/canonical` was fast-forwarded from
`f23fe5a1c70dce8cb32a4ca6aa088820b3ad7279` to reviewed candidate tip
`9c0920731b2603986d7c7b4bc51a3eb030535b65`. The candidate's code tip is
`d9ce4502b6b1601660f2c44629d6e0f34c72036d`, tree
`5222c38f4c4e56313efa1a7caa7542e830d644ba`, with `Assets` tree
`4e41a3dac9162f632f14daa9f9a978308e959371`. The exact-tip independent
implementation review is PASS in
[`PHASE12_P12C_IDENTITY_SEQUENCE_SNAPSHOT_IMPLEMENTATION_REVIEW.md`](design/PHASE12_P12C_IDENTITY_SEQUENCE_SNAPSHOT_IMPLEMENTATION_REVIEW.md);
code and validation evidence are recorded in
[`P12CIdentitySequenceSnapshotCurrentBase/VALIDATION.md`](validation/P12CIdentitySequenceSnapshotCurrentBase/VALIDATION.md).

P12-B remains complete within its accepted bounded contract. Its selected
Daily-v1 supported owner/ingress and committed-write evidence is reconciled;
runtime-wide owner-thread/quiescence and exact owner-coherence are enforced at
successful outer advance boundaries; and an ephemeral completed-boundary token
is published only after successful boundary work. Failed, partial, faulted, or
incoherent advances do not issue a new token. The selected-profile census is
`68 + 20*N + U + P`, or 278 at the authored baseline (`N=10`, `U=10`, `P=0`).
The P12-B review and validation remain in
[`PHASE12_P12B_BOUNDED_COMPLETION_IMPLEMENTATION_REVIEW_R2.md`](design/PHASE12_P12B_BOUNDED_COMPLETION_IMPLEMENTATION_REVIEW_R2.md)
and [`P12BBoundedCompletionGate2R3/VALIDATION.md`](validation/P12BBoundedCompletionGate2R3/VALIDATION.md).

The promoted P12-C sub-slice adds immutable snapshots and strict private staged
reconstruction for the fourteen existing `RuntimeIdAllocator` counters and the
shared `SimulationRecordSequence`. It preserves exact values/gaps, rejects
malformed snapshots without mutating live owners, gives reconstructed owners
fresh census identities with local revision `next value - 1`, and leaves
mutation hooks unbound until normal runtime composition binds them. Existing
P12 mutation hooks, runtime rebinding, P11 ActorChoice behavior, and P18-D
occurrence receipts remain intact.

Exact-tree validation remains attached to code tip `d9ce450`: focused suites
passed (IdentitySequenceSnapshot 4/4, RuntimeIdAllocatorCensus 3/3,
SimulationRecordSequenceP12Invalidation 17/17, ActorChoiceCensus 9/9,
ActorChoiceRuntime 11/11, P18DConsumerIntegration 10/10); ALL EditMode
`2466/2466`; official Smoke `5/5`; SimulationRuntime LongRun `7/7`; and
`git diff --check` passed. The manifest hashes raw committed Git blobs and
records the Windows line-ending normalization used by the validation worktree.
The independent reviewer verified all source, XML, and archived-log hashes
against the exact candidate.

### Refreshed dependency DAG

- **P12-B:** COMPLETE/PROMOTED within its accepted bounded contract.
- **P12-C:** IN PROGRESS; the allocator/sequence snapshot sub-slice above is
  promoted, but P12-C is not complete. Remaining accepted obligations include
  P9-B genesis provenance, exact selected-profile P8-A geography facts, and
  continuation-relevant deterministic-random roots. A read-only audit
  identified exact P8-A geography snapshot/staged reconstruction as the
  narrowest next bounded slice; it needs its own technical design review
  before implementation. The selected Daily-v1 facts are the authored Hex
  and terrain/revision, its anchored Location, coordinate convention/order,
  and authored scale context. A separate RNG-root slice is
  `READY_FOR_DESIGN`; the Daily-v1 path uses a stateless keyed provider and
  does not require Conflict demo stream cursors.
- **P12-D and P12-E:** remain blocked on completion of P12-C, in addition to
  their satisfied P12-B edge.
- **P12-F:** waits on P12-C, P12-D, and P12-E. Expedition remains deferred to
  its documented P12-F scope.
- **P12-G:** waits on P12-B through P12-F and a validated live-profile
  inventory.
- **P12-A:** remains `WAIT_DEPENDENCY` until every included owner has exact
  export and staged hydration, the live profile inventory is validated, and
  its separate implementation authorization is recorded.
- **P13:** remains `BLOCKED` on its documented continuation and recoverable
  causal-history prerequisites.

Phase 12 remains open. This promotion provides no profile-wide
export/hydration, P12-A readiness, P13 readiness, or formal Phase 12 closure.
The older P12-E snapshot below is retained as historical evidence.
## Historical canonical snapshot — P12-E military census registration (2026-10-06)

The code-bearing P12-E promotion tip is cda5a55ff6e3d9c884c95eabd0d11f3f4f4ee004.
It was fast-forwarded from 88d476729715aa82578cb2a204e32a69263e6402 after
exact-tip review and validation preflight. This State and matrix refresh is a
docs-only descendant of that promotion.

The promoted code is 5b055be864afa0ace56d56381eb00fe4e993ed86, tree
c9e763e2ebc2f63d9772701a0c03e35c271392f7. Independent implementation review
passed for that exact code/tree and is recorded in
[PHASE12_P12B_P12E_MILITARY_OWNER_REGISTRATION_IMPLEMENTATION_REVIEW.md](design/PHASE12_P12B_P12E_MILITARY_OWNER_REGISTRATION_IMPLEMENTATION_REVIEW.md).
The selected Daily-v1 admission inventory is now 268 sections, up from 260.
This adds exactly eight existing schema-v1 P12-E owner sections as Required:
three ArmedForce projections sharing one owner/revision, contingent manpower,
armed-force positions, and Conflict, War, and Battle.

Validation on the unchanged code tree passed the focused composition suite
24/24, five owner-provider suites 5/5 total, Property/Estate 5/5, ALL EditMode
2434/2434, official Smoke 5/5, and git diff --check. Exact XML/log hashes and
artifact names are in
[the validation manifest](validation/P12BP12EMilitaryOwnerCensusRegistration_VALIDATION.md).
No tests were rerun for the later docs-only review-whitespace correction.

The selected profile remains the dedicated P9-B-only UnityBootstrap-Daily-v1;
P10-A Ruin/LocalTopology remains a separate proving profile. A current-source
call-site audit found no normal Daily-v1 writer for the newly registered
P12-E owners. The military movement and War mutation paths are gated by the
separate P16-A/P17-A composition profiles. This census promotion adds no
operation, writer, mutation callback, shared-epoch notification, P17 state,
or gameplay.

P12-B remains INCOMPLETE; P12-A remains WAIT_DEPENDENCY; P13 remains BLOCKED.
The change does not establish complete owner/cardinality coverage, complete
shared-epoch coverage, global quiescence, capture eligibility, export,
hydration, downstream readiness, or Phase 12 closure. P12-F Expedition stays
deferred behind P12-C/D/E.

### Refreshed numbered-phase DAG and readiness

Remote canonical States were refreshed after the promotion. P8 and P9 are
closed in their recorded scopes; P10-A/B are promoted and Phase 10 remains
open; P11 is closed in its bounded scope; P14-A/B, P15-A, P16-A, P17-A, and
P20-A/B are promoted while their Phases remain open; P18 is formally closed
within its recorded scope. P19 public Mod API/loader implementation remains
deferred to its documented entry gates. No dependent checkpoint becomes ready
merely from P12-E census registration.

The active dependency chain remains P12-B incomplete; P12-C waits on B;
P12-D and P12-E wait on B and C; P12-F waits on C/D/E; P12-G waits on B
through F plus a validated live-profile inventory; P12-A waits for complete
included-owner export/staged hydration, validated live inventory, and its
separate implementation authorization. P13 remains blocked on supported P12
continuation and recoverable causal history. Phase numbers do not change these
documented dependency edges.

### Next P12-B blocker

The current Daily-v1 day-path crosswalk remains applicable: comparing
SimulationRuntime.cs from the prior crosswalk baseline 62e12f9 to this
promotion shows only four lines registering the P8-D and P12-E fixed owners;
the daily loop itself did not change. The refreshed audit records that no
newly supported in-profile write path was found. Do not add a military
operation or infer shared-epoch coverage from the Required census sections.

The remaining high-value P12-B work is source-linked proof of complete
effective owner/cardinality coverage and runtime-wide owner-thread/quiescence,
followed by the completed-boundary lifecycle. The current FR-B
TryReadCompletedLogicalBoundary check verifies the Daily-v1 profile, owner
thread, and idle operation state, but returns the current day without a
successful-advance sequence or P12 capture token. Do not issue or claim a
capture token until the supported owner/write and invalidation matrix is
complete. The detailed current audit is appended to
[PHASE12_B_BLOCKER_RESOLUTION.md](design/PHASE12_B_BLOCKER_RESOLUTION.md).

## Historical canonical status — P8-D exact-zero admission promoted (baseline 77030ff)

The following record preserves the P8-D promotion-time snapshot. The current
canonical status and later P12-E evidence are recorded above.

The remote `codex/phase12/canonical` branch is at
`77030ff8cd09d1c7999c63f46e8fea511c60336b` (tree
`b2404474c4f285511e10a62444c0026efb2c9505`). This is the promoted P8-D
exact-zero admission candidate, code `7e827b3fe4b8effefd682838c6be575d25eab501`
(code tree `127edf616d99bca0614041b54f72ae5addeb26b2`). The exact-tip
implementation review passed and is recorded in
[`design/PHASE12_P12B_P8D_EXACT_ZERO_REGISTRATION_IMPLEMENTATION_REVIEW.md`](design/PHASE12_P12B_P8D_EXACT_ZERO_REGISTRATION_IMPLEMENTATION_REVIEW.md).
The accepted slice registers the existing P8-D route-observation and
route-plan-history owners as explicit-empty schema-v1 sections, bound to the
exact installed runtime clones. The selected P9-B-only Daily-v1 partial
inventory is now 260 sections. The focused composition passed 24/24, ALL
EditMode passed 2434/2434, official Smoke passed 5/5, and `git diff --check`
passed. The selected Daily-v1 admission/profile-separation revalidation
passed 1/1 on the pre-implementation canonical code tree; detailed Unity
outputs, hashes, and the archived first failed plus final passing full-suite
run are recorded in
[`validation/P12P8DExactZeroAdmission/VALIDATION.md`](validation/P12P8DExactZeroAdmission/VALIDATION.md).

This is partial census evidence only. It adds no route operation, route
feature, shared-epoch notification, capture eligibility, export, hydration,
P12-A readiness, P12-B completion, P13 readiness, or Phase closure. P12-A
remains `WAIT_DEPENDENCY`; P12-B remains incomplete; P13 remains blocked; and
P12-F Expedition remains deferred behind P12-C/D/E. The historical P8-D gap
section and earlier 258-section snapshots below describe their original
baselines and are superseded for current status by this record.

The current source audit identified the next bounded P12-B inventory gap:
eight already-composed P12-E ArmedForce/manpower/position/Conflict/War/Battle
sections have owner-issued providers and selected-profile day-zero evidence,
but are not yet registered in the sealed protocol. Their integration boundary
and exclusions are recorded in
[`design/PHASE12_B_BLOCKER_RESOLUTION.md`](design/PHASE12_B_BLOCKER_RESOLUTION.md).
No implementation or P12-B readiness change is implied by this audit.

**Current P12-B canonical refresh — 2026-10-06:** canonical was fast-forwarded
from `69a5a41ca879fb66ce62efbe0d62e31f7298675b` to
`b0ab1ae2d8dc9205e58c33a8a8c7658de27bbec1`. The bounded City-roster
read-only-view implementation is code `256c443903fd3e33ed05cfe4d86a11b3467176a2`,
tree `42ea8bfda3d95a57965192f550c58be226bf0004`; its independent exact-tip
review PASS is recorded in
`docs/design/PHASE12_P12B_CITY_ROSTER_READ_ONLY_VIEW_IMPLEMENTATION_REVIEW.md`.
The selected P9-B-only Daily-v1 composition continues to register 253
sections. The focused composition passed 24/24, including the exact
Daily-v1 253-section owner/cardinality inventory and the new rejected-City-list
mutation assertions; `SimulationRuntimeAdmissionTests` passed 50/50; ALL
EditMode passed 2417/2417; official Smoke passed 5/5; and `git diff --check`
passed. Hashes and both Unity-output/Git-blob evidence are in
`docs/validation/P12BCityRosterReadOnlyView/VALIDATION.md`.

The change closes an accidental mutable alias through `SimulationRuntime.Cities`:
the API now returns one retained `AsReadOnly()` view over the privately copied
and sorted City roster. It introduces no City creation/removal operation,
revision, or shared-epoch claim. P12-B remains incomplete; P12-A remains
`WAIT_DEPENDENCY`; P13 remains blocked.

## P12-B Runtime Identity/Spatial Census promotion record — 2026-10-06

This remains bounded census registration plus the already accepted NPC
membership invalidation path. Daily-v1 still uses the dedicated P9-B profile;
P10-A Ruin/LocalTopology and P10-B generated content remain separate. The
promotion does not establish complete owner coverage, complete shared-epoch
coverage, global quiescence, capture eligibility, export, hydration, P12-A
readiness, P12-B completion, or P13 readiness.

**Previously promoted cumulative P12-B owner-witness candidate tip:** passive
census stack `codex/phase12/P12EPropertyCensus` at
`b889b4747738d933fe48311ef89fc33a40e3dfa0`, fast-forwarded from canonical
`69f456d5e3c6d6f7e4b85b36e98968ced0549bf3`. Its code-bearing tree is
`65ebc7f7ab6dd834e2326ff426600483a74c162c`; exact integration review passed
at `35ec9887add812922908d1f402d02d5db3500d44`, and the final docs-only tip
check passed at `b889b47`. It retains the separately promoted
`RuntimeIdentityRegistry` witness at `033854452c1167e053f57076803819ffe3a16840`
and the earlier P8-A witness at
`644bdae8ded1d8a938ec380370966ca6c235b881`.

## Cumulative census promotion and current readiness — 2026-09-30

The approved cumulative P12-B passive owner-census candidate
`codex/phase12/P12BCensusOwnersCumulativeIntegration` was fast-forwarded to
P12 canonical at `81435f9f17816a3fb35b59d8ab374ed1cd719444`, from previous
canonical `676196bcd807603deb9d01bd2855342a7d47a01e`. This promoted code tree
is `b0be75370d32679d0745ed15d29ce359dada0bb6`; exact implementation review
passed, required focused suites passed, ALL EditMode passed `2008/2008`, the
complete official Smoke filter passed `5/5`, and `git diff --check` passed.
The following State-only commit records the promotion. The promoted candidate
includes the approved P12-D Genealogy saturated-rollback correction at
`5ef2615bb7d3de6280a2f7a6943a1669ead9002c`; the correction remains limited to
named-birth compensation at revision saturation. It does not complete P12-D.

Its code-bearing tip is `44fc3ab94c9666f656149f346fb2cc553d3cb689` and tree
`b0be75370d32679d0745ed15d29ce359dada0bb6`, based on prior canonical
`676196b`. It adds published PersonStore, ExplorableSite, and per-City
SettlementPopulation census providers while retaining the promoted Genealogy
provider. Exact-tip implementation and integration review passed; final
documentation revalidation passed at candidate branch tip `81435f9`.

This remains a partial passive owner-census foundation. P12-B is incomplete
and P12-A remains `WAIT_DEPENDENCY`. The promotion does not provide a complete
effective-profile inventory, shared mutation-epoch coverage,
owner-thread/quiescence proof, capture eligibility, immutable exports, or
staged hydration. P12-D remains blocked on P12-B and P12-C. The reviewed
legacy `SpatialNetworkRuntime` location/route census is now promoted as
recorded below. Continue with the remaining owner and invalidation gaps in
[`PHASE12_B_BLOCKER_RESOLUTION.md`](design/PHASE12_B_BLOCKER_RESOLUTION.md);
do not treat this witness as complete profile coverage or a P12-B readiness
change.

**Architecture baseline:** `451340c56e9b676bf6ea43412bcb856b9ccde3de`,
including the approved WorldId and factual-projection contracts in §§91A–91B.
The intraday/extensibility alignment remains `4b6dd1d38cffeaf3cc1ac3effea0f8ede8771194`;
the multi-participant activity alignment remains `c285466c355103d3637ac165246591b72eb7bda0`.

**Planning authority:** `docs/phases/PHASE12_BRIEF.md` and the accepted
P12-B–P12-G capability decomposition in
`docs/design/PHASE12_CAPABILITY_CHECKPOINT_DECOMPOSITION.md`.

### Historical checkpoint status at the P12-E registration snapshot (2026-10-06)

| Checkpoint | Status | Current evidence and limits |
|---|---|---|
| P12-A — `UnityBootstrap-Daily-v1` profile integration | `WAIT_DEPENDENCY` | Scope accepted. No included-owner export plus staged-hydration coverage or validated complete live profile inventory exists yet. Its separate implementation authorization remains outstanding. |
| P12-B — profile admission and completed-boundary lifecycle | INCOMPLETE — PARTIAL FOUNDATION PROMOTED | Current canonical is cda5a55; code 5b055be/tree c9e763e registers eight P12-E Required owner sections and raises the tested Daily-v1 inventory to 268. The exact-tip review, validation manifest, current-source audit, and remaining limits are recorded above and in docs/design/PHASE12_B_BLOCKER_RESOLUTION.md. No complete owner/cardinality or shared-epoch coverage, global quiescence proof, or capture token is claimed. |
| P12-C — identity, provenance, deterministic roots | `BLOCKED_ON_P12-B` | The `RuntimeIdAllocator` passive census and record-sequence witness promoted at `b889b47` are inventory evidence only; they do not provide C exports/hydration, deterministic-root state, or provenance. Preserve `codex/phase12/P12CIdentityRuntimeSnapshot` at `531d835` for selective reintegration only after B readiness and revalidation. |
| P12-D — factual roots and Person/population relations | `BLOCKED_ON_P12-B_AND_C` | Owner inventory and accepted scope remain; no complete export/hydration capability is claimed. |
| P12-E — core and official daily-domain owners | `BLOCKED_ON_P12-B_AND_C` | Owner inventory and accepted scope remain; effective-profile provider coverage and exact owner exports are incomplete. |
| P12-F — Knowledge, directives, choices, commitments | `BLOCKED_ON_P12-C_D_E` | Accepted scope remains dependency-gated; no complete export/hydration capability is claimed. |
| P12-G — staged restore, graph validation, publication, parity | `BLOCKED_ON_P12-B_THROUGH_F` | No whole-graph staged restore or continuation-parity capability is claimed. |

Phase 12 remains open. P13 remains dependency-gated. This State does not claim
save/load support, P12-A readiness, P12-B readiness, Phase closure, or a P13
historical fork guarantee.

**Market-operation invalidation — promoted on current P12 canonical:** the
bounded candidate is exact-tip reviewed PASS at
`b577312c2edc6d3124c6d2b9dd3a1201dc9055ae` (tree
`88ec452a979a7439b825858a6b1b271f8bc6c948`) against canonical base
`b8a7da54864bee3fb9b8916793240e91fbce0955`. With approval, its reviewed
bundle was fast-forwarded to `codex/phase12/canonical` at
`b77e154e86b510c9f47ea9042fe5a1edb39749b0`. It extends only Open-market
purchase/sale owner commits, direct Market commits, and selected daily
production/Free-consumption/price-refresh paths. Review, validation, and the
refreshed owner/operation/epoch matrix are linked in
`docs/design/PHASE12_P12B_MARKET_OPERATION_INVALIDATION_REVIEW.md`,
`docs/design/PHASE12_P12B_MARKET_OPERATION_INVALIDATION_CANDIDATE.md`, and
`docs/design/PHASE12_B_BLOCKER_RESOLUTION.md`.

## P12-B Market operation invalidation promotion — 2026-10-01

With explicit approval, the reviewed candidate bundle was fast-forwarded
from canonical `b8a7da54864bee3fb9b8916793240e91fbce0955` to
`b77e154e86b510c9f47ea9042fe5a1edb39749b0` on
`codex/phase12/canonical`. This records promotion only; P12-B remains
incomplete and Phase 12 remains open.

Independent exact-tip code review PASSed on candidate
`b577312c2edc6d3124c6d2b9dd3a1201dc9055ae`, tree
`88ec452a979a7439b825858a6b1b271f8bc6c948`, based on canonical
`b8a7da54864bee3fb9b8916793240e91fbce0955`. Durable review evidence is
`docs/design/PHASE12_P12B_MARKET_OPERATION_INVALIDATION_REVIEW.md`; validation
results and artifact hashes are in
`docs/design/PHASE12_P12B_MARKET_OPERATION_INVALIDATION_CANDIDATE.md`.

The bounded slice registers the exact composed City Market owners; scopes
Open-market purchase/sale over the exact NPC account, Inventory, and Market
sections; reports committed owner revisions and successful compensation; and
reports direct Market stock/changed-price commits plus selected daily City
production, Free consumption, and price refresh. Direct Market mutations are
owner-thread/baseline guarded and notify their Market section, but do not
require an active named operation scope. Other transaction families and
public owner writers remain uncovered.

This candidate does not complete P12-B or establish complete owner coverage,
complete shared-epoch coverage, capture eligibility, export, or hydration.
P12-A remains `WAIT_DEPENDENCY`, P13 remains blocked, and Phase 12 remains
open. Canonical promotion is complete for this bounded slice; future candidate
promotions remain separate human gates.

## P12-B SpatialNetwork census promotion — 2026-09-30

With explicit approval, candidate
`codex/phase12/P12BSpatialNetworkCensus` at
`46c457fe12d2b287d55553e606fea471255d292d` was fast-forwarded to
`codex/phase12/canonical` from `eec3fbeecffb52c633ffe0945b3dfa39743ae064`.
The code-bearing commit is `555594ed899f2e191640f758599058a198c53ee7`,
tree `da7edc43dcbe7c56fedf52730d5fb808ff7db6d3`. Independent exact-tip
implementation review passed; the durable review record is
`codex/phase12/P12BSpatialNetworkCensusReviewRecord` at
`dc10d7376e22af1a7027ea8bdb238f2e56dfe390`. The candidate and review records
are linked in
[`PHASE12_P12B_SPATIAL_NETWORK_CENSUS_CANDIDATE.md`](design/PHASE12_P12B_SPATIAL_NETWORK_CENSUS_CANDIDATE.md)
and
[`PHASE12_P12B_SPATIAL_NETWORK_CENSUS_IMPLEMENTATION_REVIEW.md`](design/PHASE12_P12B_SPATIAL_NETWORK_CENSUS_IMPLEMENTATION_REVIEW.md).

It publishes passive schema-v1 owner witnesses for legacy runtime network
locations and routes. Both bind to the exact installed `SpatialNetworkRuntime`
and share its monotone revision. The selected authored bootstrap profile
reports two locations, two routes, and revision four. Validation on the code
tree passed `SpatialNetworkCensusTests` 7/7, ALL EditMode 2015/2015, complete
official Smoke 5/5, and `git diff --check`; retained XML evidence is under
`Library/ValidationResults/P12BSpatialNetwork` in the candidate worktree.

This remains unsynchronized passive census evidence. It does not connect
network or direct `RuntimeIdentityRegistry` writes to the shared P12-B epoch,
complete the selected-profile owner inventory, prove owner-thread/quiescence,
grant capture eligibility, or provide export/hydration. P12-B remains
incomplete; P12-A remains `WAIT_DEPENDENCY`; P12-C through P12-G remain
blocked on their documented prerequisites; Phase 12 remains open.

## P12-B SpatialKnowledge census promotion — 2026-09-30

With explicit approval, candidate
`codex/phase12/P12BSpatialKnowledgeCensus` at
`55e2f232b02cd5c6df76014d325a8caeb7020408` was fast-forwarded to
`codex/phase12/canonical` from `81ddfe4bd0620b1b61a0a52aa074ef2ed57c2833`.
The code-bearing commit is `de49358cd8b0baa5df2c12206d3e405dbf31261a`.
Independent exact-tip implementation review passed after the retained result
paths were verified; the reviewed candidate tip changes only the evidence
record. The selected bootstrap profile reports 20 per-NPC sections: ten
installed NPC owners, each with two known locations and one known route at
shared revision three. Providers bind to each exact installed
`SpatialKnowledgeRuntime` and are ordered by ordinal `RuntimeId`. The runtime
now returns live read-only views and rejects new discoveries before mutation
at revision saturation.

Validation on the code-bearing tip passed `SpatialKnowledgeCensusTests` 3/3,
`SimulationBootstrapCompositionTests` 14/14, ALL EditMode 2018/2018, and the
complete official Smoke filter 5/5. Exact XML evidence is retained under
`Library/ValidationResults/P12BSpatialKnowledgeReview` in the candidate
worktree and linked from
[`PHASE12_P12B_SPATIAL_KNOWLEDGE_CENSUS_CANDIDATE.md`](design/PHASE12_P12B_SPATIAL_KNOWLEDGE_CENSUS_CANDIDATE.md).

This is a fixed day-zero per-NPC witness set. Roster-driven dynamic
SpatialKnowledge section membership is now covered by the separate promotion
record below. Other dynamic NPC owner coverage, discovery-write invalidation,
shared mutation-epoch coverage for all supported writes, global
owner-thread/quiescence, complete profile owner coverage, capture eligibility,
and export/hydration remain unresolved. P12-B remains incomplete; P12-A
remains `WAIT_DEPENDENCY`; Phase 12 remains open.

## P12-B dynamic NPC SpatialKnowledge census promotion — 2026-09-30

With explicit approval, candidate branch
`codex/phase12/P12BDynamicNpcCensusImplementation` at
`0021b0aa13fb6ae6d5f27c129c67ba452dbacb4e` was fast-forwarded to the local
`codex/phase12/canonical` branch from
`0a37e9f053b482d80d0815c95352e3d96b56ed8f`.
The code-bearing commit is `2c782a7a08abfc1c8b2ba9201efad12f4ac279ae`.
Independent exact-tip implementation review passed against that base; the
durable record is
[`PHASE12_P12B_DYNAMIC_NPC_CENSUS_IMPLEMENTATION_REVIEW.md`](design/PHASE12_P12B_DYNAMIC_NPC_CENSUS_IMPLEMENTATION_REVIEW.md)
at the promoted candidate tip.

The partial protocol now reconciles two schema-v1 SpatialKnowledge sections
for every currently registered NPC, ordered by ordinal RuntimeId and bound to
the exact NPC and SpatialKnowledge owner objects. NPC register/unregister and
Person materialization/adoption use a runtime-owned outer membership context;
affected PersonStore sections and the dynamic family publish as one delta.
The local context records its owning thread; a mismatched nested entry faults
this partial census and cannot join its active context or publish its family
or epoch. This does not prove global runtime thread affinity or quiescence.

Validation on the code-bearing tip passed `SpatialKnowledgeCensusTests` 16/16,
`ContinuationCensusProtocolTests` 22/22, ALL EditMode 2035/2035, the complete
official Smoke filter 5/5, and `git diff --check`. Exact results are retained
under `Library/ValidationResults/P12BDynamicNpcCensus/` and linked from
[`PHASE12_P12B_DYNAMIC_NPC_CENSUS_IMPLEMENTATION_CANDIDATE.md`](design/PHASE12_P12B_DYNAMIC_NPC_CENSUS_IMPLEMENTATION_CANDIDATE.md).

This promotes only the roster-following SpatialKnowledge/Person partial
census. The passive City `ImportantNpcs` projection witness is promoted
separately below; broader City/NPC composite coverage, shared-epoch wiring,
direct Inventory write invalidation, unrelated PersonStore writers,
SpatialKnowledge discovery invalidation, complete profile inventory, global owner-thread/quiescence,
capture eligibility, and export/hydration remain open. P12-B remains
incomplete and P12-A remains `WAIT_DEPENDENCY`.

## Promoted RuntimeIdentityRegistry passive census witnesses

The reviewed candidate `codex/phase12/P12BRuntimeIdentityWitness` at
`033854452c1167e053f57076803819ffe3a16840` was approved and fast-forwarded to
`codex/phase12/canonical` on 2026-09-29 from canonical
`1ada62b031e738e2bdd5d3d623e028a114961d6e`. The independent exact-tip
implementation review record is `codex/phase12/P12BRuntimeIdentityWitnessReviewRecord`
at `c113f52`.

It adds eight schema-v1 passive sections for the existing typed runtime
identity indexes. Each section reports its live index count and shares the
installed registry's identity and monotone registration revision. The normal
bootstrap publishes the fixed provider collection without exposing the raw
registry or a general registration hook. Validation on the candidate passed:
`RuntimeIdentityCensusTests` 6/6, the selected authored bootstrap profile
1/1, ALL EditMode 1963/1963, complete official Smoke 5/5, and
`git diff --check`. XML evidence is recorded in
`docs/design/PHASE12_RUNTIME_IDENTITY_CENSUS_CANDIDATE.md`.

This remains a passive witness for one P12-C causal identity owner. It does
not add the RuntimeIdAllocator counters, shared mutation-epoch wiring,
owner-thread/quiescence enforcement, capture eligibility, or export and
hydration. P12-B remains incomplete and P12-A remains `WAIT_DEPENDENCY`.

## Promoted P12-B partial-foundation evidence

The candidate branch `codex/phase12/P12BCoordinatorReviewFix` was based on
the previous canonical SHA `04105d31e88fca97888dddb8e974236a7f4b6804`.
Its code-bearing tree was `a67beacf5aad9da11a070eae48a45fcd50ffb44b`; the
reviewed promotion tip is `9da25b47ab0bc0f6a1a10032dfceaad05f6316e6`.
Canonical promotion was approved and completed at `9da25b47ab0bc0f6a1a10032dfceaad05f6316e6`. It includes:

- a versioned owner-section census protocol with exact section role, owner
  identity, schema, cardinality, revision/snapshot stamp, fail-closed owner
  coverage, serialized owner-thread binding, operation accounting, atomic
  changed-owner batch validation, and a monotonically advancing mutation epoch;
- exact owner-issued census witnesses for the conditional P18 decision
  occurrence-receipt and economy keyed-sale-receipt ledgers, including retained
  terminal/preflight-failure receipts, same-cardinality replacement,
  replay, and collision behavior;
- the selected-profile day-zero census evidence and the bounded post-promotion
  P8-C witness-adapter work package in the blocker-resolution plan.

Independent implementation review passed on code tip
`dbe3db08c54c7380f26c89b7ee07e0742730d95b` against the previous canonical
base. Independent review of the P8-C dependency update passed on exact tip
`a67beacf5aad9da11a070eae48a45fcd50ffb44b`; the final State and test-plan
wording were also reviewed on the promoted exact tip. These reviews retain the
limitations listed above.

Validation on the exact code-bearing candidate tree `a67beacf5aad9da11a070eae48a45fcd50ffb44b` passed: ALL EditMode `1952/1952`
(`Temp/ValidationResults/EditMode-20260929-191720-bf896e633b224549a5bc349eed4e2908.xml`),
official complete Smoke `5/5`
(`Temp/ValidationResults/EditMode-20260929-191810-0384b2eb5dcf452ca01f484050aebe82.xml`),
and `git diff --check`. The tested code-bearing tree and promoted tip differ
only by reviewed documentation commits. This is a reviewed, promoted
non-admitting foundation, not a completed P12-B checkpoint.

## Promoted P8-A populated geography census witnesses

The P8-A passive witness candidate was based on canonical
`06145c7cbc258c56cc1be1a24adaa1751d32bc01` and, with approval, fast-forwarded
to `codex/phase12/canonical` at `644bdae8ded1d8a938ec380370966ca6c235b881`.
Its code-bearing tip is `9efba61`; independent exact-tip implementation
review passed on `644bdae`. The durable review record is branch
`codex/phase12/P12BP8APopulatedReviewRecord` at `a401860`.

It adds three schema-v1 passive witnesses backed by the installed runtime
`SpatialAuthorityStore`: `p8a.hexes` reports `HexCount`, `p8a.locations`
reports `LocationCount`, and `p8a.scale-context` reports `HasGeography ? 1 : 0`.
Each reports the store's shared `Revision`. The selected profile verifies the
populated day-zero values 1/1/1 at revision 1 and the installed owner identity.
A separate temporal test confirms that successful barrier registration
advances the shared revision from 1 to 2 while these cardinalities remain
7/1/1 in its multi-Hex fixture.

Validation on the code-bearing tree passed: `SpatialGeographyTests` 15/15,
`SimulationBootstrapCompositionTests` 14/14, ALL EditMode 1957/1957, official
complete Smoke 5/5, and `git diff --check`. Exact result paths and scope limits
are recorded in `docs/design/PHASE12_P8A_CENSUS_CANDIDATE.md`; independent
review is recorded in `docs/design/PHASE12_P8A_CENSUS_REVIEW.md` on its review
branch.

These unsynchronized providers remain passive. They are not registered in a
complete profile census, do not connect spatial writes to the shared mutation
epoch, do not prove owner-thread/quiescence, and do not grant capture
eligibility. P12-B remains incomplete and P12-A remains `WAIT_DEPENDENCY`.

## Promoted P8-C passive census witnesses

The P8-C passive witness candidate was based on canonical
`a43316858b7006c624ed1a950097210ae55e95f7` and promoted to
`codex/phase12/canonical` at `481358d1f8967d1c0199370601597c329fce69b2`.
Independent exact-tip review passed. It adds schema-v1 owner witnesses for
`p8c.city-site-location-bindings` and `p8c.person-positions`, using the
published runtime's installed `LegacySpatialAnchorBindingStore` and
`PersonSpatialPositionStore` references as identity and their exact local
count/revision values.

Focused `PersonSpatialPresenceTests` passed 9/9 and
`SimulationBootstrapCompositionTests` passed 14/14. ALL EditMode passed
1953/1953 and official complete Smoke passed 5/5. Result XMLs are recorded in
`docs/design/PHASE12_P8C_CENSUS_CANDIDATE.md` and retained in the candidate
worktree under `Library/ValidationResults/P12BP8C`.

These unsynchronized providers remain passive. They are not registered in the
incomplete profile census, do not connect writes to the shared mutation epoch,
do not prove owner-thread/quiescence, and do not grant capture eligibility.
P12-B remains incomplete and P12-A remains `WAIT_DEPENDENCY`.

## Promoted P8-B passive census witnesses

The P8-B passage and crossing candidate was based on canonical
`4d48a5f88145d748c32c8dc42ab251dcb04f81e4` and promoted to
`codex/phase12/canonical` at `04d39b23b8509609dcd96990a214922dc0220e8b`.
Independent exact-tip review passed. It adds schema-v1 witnesses for
`p8b.passage-option-barrier-state` and `p8b.crossings`. The passage witness
uses the installed `SpatialPassageAuthority` child as identity, checked
`Options.Count + Barriers.Count` as cardinality, and its parent's revision as
the conservative stamp. The crossing witness uses the installed
`SpatialAuthorityStore` identity, `CrossingCount`, and that same revision.
Crossings projected into `OptionStates` are not double-counted as passage
membership.

Validation on the code-bearing commit `e77d671df7240c71c885004110fa096a9ed67802`
passed: `SpatialPassageAuthorityTests` 13/13,
`SimulationBootstrapCompositionTests` 14/14, ALL EditMode 1954/1954, official
complete Smoke 5/5, and `git diff --check`. XML paths and scope limits are
recorded in `docs/design/PHASE12_P8B_CENSUS_CANDIDATE.md`.

These unsynchronized providers remain passive. They are not registered in the
profile census, do not connect writes to the shared mutation epoch, do not
prove owner-thread/quiescence, and do not grant capture eligibility. P12-B
remains incomplete and P12-A remains `WAIT_DEPENDENCY`.

## Promoted P8-D passive census witnesses

The P8-D route-owner witness candidate was based on canonical
`c7ff9fd9f4245c3f8968b9196a4c20de19dd0fb2` and promoted with approval to
`codex/phase12/canonical` at candidate tip
`d92fdfb6b5ceb517c210be7cea5faab52ebb5641`. Its code-bearing commit is
`f0575ef43a77898aae8fb8565d4b709b850a46d8`. Independent exact-tip
implementation review passed on candidate tip `d92fdfb`; the durable review
record is branch `codex/phase12/P12BP8DReviewRecord` at
`934741142cd74e06d2c284af45a62a068fa3d9de`.

It adds schema-v1 passive witnesses for:

- `p8d.spatial-route-observations`, using the installed runtime
  `SpatialRouteKnowledgeStore`, `ObservationCount`, and local `Revision`;
- `p8d.person-route-plan-history`, using the installed runtime
  `PersonRoutePlanStore`, `PlanCount` for all retained rows, and local
  `Revision`.

Validation on the code-bearing tree passed: `SpatialRoutePlanningTests`
21/21, `SimulationBootstrapCompositionTests` 14/14, ALL EditMode 1955/1955,
official complete Smoke 5/5, and `git diff --check`. Exact XML paths are
recorded in `docs/design/PHASE12_P8D_CENSUS_CANDIDATE.md` and remain in the
candidate worktree under `Library/ValidationResults/P12BP8D`.

The profile test confirms day-zero exact-zero values and installed-owner
identity. Mutation tests confirm new observations, replay/conflict behavior,
plan-history append/stale rejection, and P8-E status changes that advance the
plan revision without adding a history row. These reads remain unsynchronized
and passive. They are not registered in a complete profile census, do not
connect writes to the shared mutation epoch, do not prove owner-thread or
quiescence, and do not grant capture eligibility. P12-B remains incomplete;
P12-A remains `WAIT_DEPENDENCY`.

## Promoted cumulative passive owner-witness extension

The cumulative owner-census candidate was approved and fast-forwarded from
P12 canonical `69f456d5e3c6d6f7e4b85b36e98968ced0549bf3` to
`b889b4747738d933fe48311ef89fc33a40e3dfa0`. Its code-bearing tree is
`65ebc7f7ab6dd834e2326ff426600483a74c162c`; the exact integration review
passed at `35ec9887add812922908d1f402d02d5db3500d44`, and the docs-only final
tip check passed at `b889b47`.

The promoted stack includes passive owner witnesses for:

- `SimulationRecordSequence`, `ActorChoiceStore`, and `RuntimeIdAllocator`;
- `ArmedForceStore`, contingent manpower, and armed-force spatial position;
- persistent Conflict, War, and Battle stores;
- Estate and Property ownership/transfer history; and
- Institution, Office, active incumbency, and retained tenure history.

Focused evidence remains in the linked candidate records under
`docs/design/PHASE12_*_CENSUS_CANDIDATE.md`. The final code tree passed ALL
EditMode `1985/1985`, the complete official `Smoke` filter `5/5`, and
`git diff --check`. This integrates the reviewed slices without closing P12-B
or changing the P12-C/D/E/F/G dependencies.

These are still passive per-owner witnesses. They do not register a complete
effective-profile census, prove every supported committed write reaches the
shared epoch, establish owner-thread/quiescence, issue capture eligibility,
or provide immutable exports and staged hydration. P12-B remains incomplete;
P12-A remains `WAIT_DEPENDENCY`.

## Promoted P12-D Genealogy parentage witness

The reviewed candidate branch `codex/phase12/P12DGenealogyCensusWitness` was
based on canonical `d0c2733994aaf51e417b7c9f49f2b3489c4c49c3` and approved for
promotion at branch tip `bde930477b7614a7fb1baed01497dbd1fe063927`. Canonical
was fast-forwarded to that tip. The code-bearing commit is
`3afbc593fad8648e5a2ae20b7ec1a9d988769fb9`; the intervening and final commits
only record the candidate design and validation evidence.

The promoted schema-v1 `p12d.genealogy.parentage` witness is bound to the
installed runtime `GenealogyStore` and counts direct parentage edges. Ordinary
public add/remove commits advance its local revision once and preflight
revision overflow before changing edges. The internal named-birth compensation
path may remove an edge at saturation without advancing the revision; this
strictly decreases cardinality while ordinary mutations are closed. Clone
construction retains the existing deterministic replay behavior and exposes
only the installed clone through the bootstrap composition.

Independent implementation review passed on the exact code tip and the
docs-only candidate update was reviewed. Validation passed: focused
`GenealogyCensusTests` 3/3, ALL EditMode 1988/1988, and the complete official
`Smoke` filter 5/5. Exact evidence paths and the detailed test boundary are in
`docs/design/PHASE12_P12D_GENEALOGY_CENSUS_CANDIDATE.md`.

This remains one passive, unsynchronized owner witness. It is not registered
in the complete P12-B profile inventory, does not connect writes to the shared
mutation epoch, and proves neither owner-thread/quiescence nor capture
eligibility. It adds no genealogy semantics, export, hydration, or P12-D
completion. P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`; P12-D
remains blocked on P12-B and P12-C.

The subsequent P12-D multi-owner commit-graph audit found a revision-saturation
rollback defect in the promoted code. The bounded correction's code commit is
`f631de8a9209956cf61d0f901867cca244befcaf` (tree
`ae029655824dd3ec6a73bc3c17a9bb2708014401`); it was independently reviewed
against exact parent `8f04a62bf2e66995efe8f311f5d62cff191acb11` (based on
P12 canonical `543196a`) and validated (focused named-birth 15/15, Genealogy
census 4/4, ALL EditMode 1990/1990, complete official Smoke 5/5, diff-check).
The reviewed correction candidate at `5ef2615bb7d3de6280a2f7a6943a1669ead9002c`
was approved and fast-forwarded to P12 canonical. The promoted Genealogy slice
now includes the saturated named-birth rollback repair. This does not change
P12-B/P12-A readiness or close P12-D.

## P12-B City NPC-presence projection census promotion — 2026-09-30

With explicit approval, candidate
`codex/phase12/P12BNpcOwnerCensusImplementation` was fast-forwarded from prior
canonical `f7a66963ff6f44ee6116c0b37fd7374bf34ace5a` to candidate tip
`10fb58d088e515d76bb86de2d7381c9ea9cb7483`. The code-bearing implementation
is `aafa81ea6a2cf6b8963d15ee9ed8d26577b368a2`. Independent exact-tip
implementation and candidate-document reviews passed.

The promoted slice publishes one schema-v1 passive witness per installed City,
bound to the exact `CityRuntime` and reporting the City-owned
`ImportantNpcs` cardinality/revision. Before issuing evidence, it validates
unique projection membership and reciprocal exact City/Location references
against the live installed NPC roster. City-owned mutations, the live
read-only view, and revision-capacity preflights cover cross-City presence,
single travel, party start/rollback, and final-day party arrival.

Validation on the code-bearing tree passed `GeneralizedSpatialTravelTests`
18/18, `GroupTravelTests` 32/32, `SimulationBootstrapCompositionTests` 14/14,
ALL EditMode 2042/2042, the complete official `Smoke` filter 5/5, and
`git diff --check`. Persistent XML evidence and the precise scope are recorded
in
[`PHASE12_P12B_CITY_NPC_PRESENCE_CENSUS_CANDIDATE.md`](design/PHASE12_P12B_CITY_NPC_PRESENCE_CENSUS_CANDIDATE.md).

This remains a passive City projection witness. It does not wire City writes
to the shared mutation epoch, prove owner-thread/quiescence, provide
export/hydration, or grant capture eligibility. It does not supply global
owner coverage. It continues to rely on the
existing `SimulationRuntime.Cities` boundary; City composition sealing and
drift detection remain unsolved. P12-B remains incomplete and P12-A remains
`WAIT_DEPENDENCY`.

## P12-B dynamic per-NPC Inventory census promotion — 2026-09-30

With explicit approval, candidate
`codex/phase12/P12BNpcInventoryCensus` was fast-forwarded from canonical
`f961477380508647d2975272da72d96892944ed9` to `15e5a543f3896c02f9dbd9c36c9ba75bc90dac2b`.
The code-bearing changes add one schema-v1 `p12f.inventory/<RuntimeId>`
witness per currently rostered NPC, ordered by ordinal RuntimeId and bound to
the exact installed `NpcRuntime` and `InventoryRuntime`. The witness reports
the item-row count and existing local Inventory revision without triggering
lazy owner or item-storage creation. Inventory family reconciliation joins
the existing NPC membership operation with the dynamic SpatialKnowledge and
fixed PersonStore sections. Same-roster owner replacement fails census closed
and retains the previously published provider snapshot.

Independent exact-tip implementation review passed against the base/current
canonical tip `f961477`; the durable review record is
`codex/phase12/P12BNpcInventoryCensusReviewRecord` at
`f98c5d4f3ddc3721b4b31de1a40f063e2b333831`. Validation on candidate tip
`15e5a54` passed `NpcInventoryCensusTests` 7/7, bootstrap composition 14/14,
ALL EditMode 2049/2049, complete official Smoke 5/5, and `git diff --check`.
Exact XML evidence is retained in
`Library/ValidationResults/P12BNpcInventoryCensus` in the candidate worktree
and linked from
[`PHASE12_P12B_NPC_INVENTORY_CENSUS_CANDIDATE.md`](design/PHASE12_P12B_NPC_INVENTORY_CENSUS_CANDIDATE.md).

This remains a passive Inventory owner witness. Direct Inventory writes still
do not notify the shared P12-B epoch. The witness does not seal City
composition or cover an NPC `StartingCity` outside the composed City list; that
is a separate full-profile admission blocker. This promotion adds no global
owner-thread/quiescence proof, capture eligibility, export, or hydration.
P12-B remains incomplete and P12-A remains `WAIT_DEPENDENCY`.

## P12-B TravelParty census promotion — 2026-09-30

With explicit approval, `codex/phase12/P12BTravelPartyCensusImplementation`
was fast-forwarded to `codex/phase12/canonical` at
`b78a271f9c552b40bade1a45168388eafa670f59` from canonical
`e9ced8e451f42e80ed2132ce494cd5c26e439894`. Its code/test tip is
`260a688f7ea1a9f86e9b589788cda2c32357e170` (tree
`34571165d55af36a60f64650dce1968802cfd500`). Independent exact-tip review
passed; the durable PASS record is
`codex/phase12/P12BTravelPartyCensusImplementationReview2` at
`e8910587fdd894a5090c4208b30c5e834acb528f`. The previous HOLD record is
preserved separately.

The candidate publishes the exact installed `TravelPartyStore` through a
schema-v1 passive census provider reporting active party-instance count and
owner-local revision. Party identity remains distinct from member NPC
identities; cardinality counts party instances, not participants. The bounded
compensation test uses a deterministic stepwise sequence through real store
and lifecycle operations; it does not claim to pause the complete
`ExpeditionSystem.TryBeginReturn` coordinator. The reviewer verified the
production coordinator's matching mutation-window boundary and found this
proof sufficient without a production-only test hook.

Validation on the code/test tip passed `TravelPartyCensus` 10/10,
`GroupTravel` 32/32, `Expedition` 14/14, bootstrap composition 14/14, ALL
EditMode 2061/2061, complete official Smoke 5/5, and `git diff --check`.
All six XML/log pairs were independently inspected against their recorded
SHA-256 values in
[`PHASE12_P12B_TRAVEL_PARTY_CENSUS_CANDIDATE.md`](design/PHASE12_P12B_TRAVEL_PARTY_CENSUS_CANDIDATE.md).

This is a passive owner witness only. It does not prove member-level census
coverage, global committed-write invalidation, complete profile inventory,
owner-thread/quiescence, capture eligibility, export, or hydration. It adds
no P18 timeline/handoff behavior and no permanent Activity-to-Actor or
NPC-owned authority rule. P12-B remains incomplete; P12-A remains
`WAIT_DEPENDENCY`.

## P12-B ScheduledDirective, Expedition, and NPC Knowledge composition promotion — 2026-09-30

With explicit approval, candidate
`codex/phase12/P12BCensusOwnersCompositionIntegration` was fast-forwarded
from canonical `19d0373d6a71b63536248ecc9091e66c9b3a708b` to code-bearing tip
`72239ad1013dad5d507bad9737c358b1cadfe752`, tree
`c9fc250195bb5fda11935c687294f8ae5738bd9a`. The source owner candidates
remain in the integration history. The candidate composes the reviewed
ScheduledDirective, Expedition, and per-NPC Knowledge census providers through
the normal runtime/bootstrap boundary. Knowledge roster reconciliation is
staged before publication and preserves the Inventory family on success and
failed reconciliation.

Independent exact-tip integration review passed against canonical
`19d0373`; the durable record is
`codex/phase12/P12BCensusOwnersCompositionIntegrationReviewRecord` at
`b4fe15f0b32acea82c7ca4da608efde9346e8221`. The candidate contract and
evidence record is
`codex/phase12/P12BCensusOwnersCompositionCandidateRecord` at
`3b60dd61c7d84e08cfda587e83fb18a69decb957`.

The exact code tree's completed validation record reports Knowledge census
17/17, bootstrap composition 14/14, ScheduledDirective census 6/6, ALL
EditMode 2099/2099, official Smoke 5/5, and clean `git diff --check`. The
reviewer independently verified the retained Smoke XML/log hashes recorded in
the candidate file; the focused-suite and full EditMode XML/log files were
absent from the migrated checkout. The Smoke XML records 5/5 and its assembly
metadata lists 2,099 test cases; it is not itself evidence that all 2,099 ran.

This promotes only partial passive census composition. It does not complete
the effective-profile owner inventory or P12-B, connect every supported owner
write to the shared epoch, prove runtime-wide owner-thread/quiescence, grant
capture eligibility, or provide immutable exports, staged hydration, or
restoration. P12-B remains incomplete; P12-A remains
`WAIT_DEPENDENCY`; downstream P12 checkpoints remain blocked as recorded;
Phase 12 remains open.

## Remaining dependency-ordered P12-B blockers

The static successful-writer inventory is recorded; proving every included
commit reaches the shared invalidation epoch remains open. The selected live
day-zero test covers only a subset of owners and does not prove evolved
cardinality, owner-thread identity, or quiescence. Causal C roots, factual D,
official E, and commitment F owners still need exact witness providers and
supported-writer coverage.

P8-A through P8-D owner witnesses and the RuntimeIdentityRegistry witness are
now promoted. This adds positive day-zero P8-A geography cardinalities while
P8-B/C/D and registry witnesses cover their separate sections. The promoted
TravelParty witness covers only party-instance count/revision. Remaining
causal C roots, factual D, official E, and commitment F owners still need exact
witness providers and supported-writer coverage. The remaining committed-write
invalidation and owner-thread/quiescence blockers are unchanged. Phase 12
remains open and no P12-A implementation authorization is implied.

## P12-B bounded runtime admission/quiescence adapter promotion — 2026-10-01

With explicit approval, candidate `codex/phase12/P12BRuntimeQuiescenceAdapterIntegration`
was fast-forwarded from P12 canonical `f538a096bf4b2558566518483bc60f0129718a3b`
to `f60d8e65bea5f0b3f8964eef85957cf84451d6ba`. Its runtime implementation
commit is `9d4b035bc484286cfb58d66cca07809844c30254` (tree
`911d7cc9ff4e9c0205ae3305df4757099a461b0a`). The accepted technical design
`e5a32b8ec226204e751e1da41dfcf2546a760718` and its independent design PASS
remain linked from the implementation candidate record.

The selected `UnityBootstrap-Daily-v1` profile now carries the Unity `Start`
thread identity into the runtime and its partial census protocol. Its named
bootstrap-publication and daily-advance scopes are admitted on that captured
thread; direct runtime-owned daily clock calls route through the same daily
operation. The adapter rejects combination with a P18 timeline profile and
preserves P18 timeline clock ownership. Failure in an admitted bootstrap or
daily operation faults partial admission; selected-profile publication is
revoked and failed Start is latched. The candidate adds a selected-profile
ExplorableSite witness assertion for exact installed-owner identity and stable
day-zero count/revision zero.

Independent exact-tip implementation and final-delta review passed. The
review record is `codex/phase12/P12BRuntimeAdmissionAdapterReviewRecord` at
`bd62a1e7e1d5a4da0b5778fec99dee12bf62f9d9`. Validation passed runtime
admission 8/8, P18D compatibility 26/26, selected-profile census 1/1, ALL
EditMode 2107/2107, the complete official Smoke filter 5/5, and
`git diff --check`. Exact validation artifact hashes are retained in
`docs/design/PHASE12_P12B_RUNTIME_ADMISSION_ADAPTER_CANDIDATE.md` and the
review record.

This promotion supplies only the bounded admission/quiescence adapter and
related census evidence. It does not establish a complete effective-profile
census, full supported-write coverage of the shared mutation epoch, global
owner-thread/quiescence coverage, capture eligibility, immutable export, or
staged hydration. P12-B remains incomplete; P12-A remains
`WAIT_DEPENDENCY`; Phase 12 remains open.

## P12-B operation-footprint refresh candidate — 2026-10-01

The docs-only operation-footprint refresh was prepared on this canonical base
`70bc1e50a7107a1489614a62f5f34694b6b52498` as
`codex/phase12/P12BOperationFootprintRefresh`. Audit commit
`abf7246cd472e522139dd15867c38f6b6e7afd2a` corrects stale statements from the
older `81ddfe4` audit and separates the selected daily, membership, Travel,
TravelParty, and Expedition outer paths. Independent exact-tip review passed;
the review record is `docs/design/PHASE12_P12B_OPERATION_FOOTPRINT_REFRESH_REVIEW.md`.

The refreshed evidence confirms that the promoted adapter registers only the
partial owner and operation inventories described above. It does not supply
the complete effective-profile owner/cardinality set or map every supported
commit to the shared epoch. In particular, the Expedition start, daily
autonomy, direct exploration/effects, return, and travel-reconciliation paths
have different owner commits and bypasses; this evidence does not authorize a
new runtime operation or callback. This candidate changes no executable code
and records no new checkpoint acceptance.

P12-B remains incomplete, P12-A remains `WAIT_DEPENDENCY`, P13 remains
dependency-gated, and Phase 12 remains open. No complete census, shared-epoch
coverage, capture eligibility, export, or hydration is inferred.

## P12-B operation-footprint refresh promotion — 2026-10-01

With explicit approval, `codex/phase12/canonical` was fast-forwarded from
`70bc1e50a7107a1489614a62f5f34694b6b52498` to the reviewed, documentation-only
candidate `4f125864290113a257bc4f926e5e19d9a3b48da6`. This promotes the
operation-footprint revalidation and its State/review evidence only; it changes
no executable code and adds no implementation authorization.

The audit confirms that the selected profile has partial bootstrap, daily, and
NPC-membership scopes. It does not establish a complete owner/cardinality
inventory, supported-write coverage of the mutation epoch, or a single
Expedition operation boundary. The complete owner-section and supported-
operation matrix remains the prerequisite to further runtime wiring.

P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`; P13 remains
dependency-gated; Phase 12 remains open. Capture eligibility and
export/hydration are not provided by this promotion.

## P12-B per-City Market stock census promotion — 2026-10-01

With explicit approval, `codex/phase12/canonical` was fast-forwarded from
`7be88ca7816b14c934729bffec56178ce3eb7e5a` through the exact-tip-reviewed
candidate `533c1e54f362218f222bf567dc1cacb8fdf68600` and its durable review
record commit `9dbae5b6e20caccfabfabf63e27ac5d75dc15008`. The implementation
commit is `3b2a9c2807ac95c6c929fe1104fcb078f364b96f` (tree
`1bb0a2b9e271d8070fe1012a0fecf7b37b0763e5`); the exact candidate tree is
`014ada28ba228be3e6faa104a876f521dd2a5835`. The review record is
`docs/design/PHASE12_P12B_MARKET_STOCK_CENSUS_IMPLEMENTATION_REVIEW.md`.

The promoted addition is only a passive per-City Market stock-row census
witness: each installed Market reports its row count and that same owner's
existing local revision. Exact-tip review verified the full candidate diff,
reviewed candidate SHA, and validation artifacts. The recorded validation
passed Market census 1/1, bootstrap composition 14/14, ALL EditMode
2108/2108, official EditMode Smoke 5/5, and `git diff --check`; detailed
artifact hashes remain in
`docs/design/PHASE12_P12B_MARKET_STOCK_CENSUS_CANDIDATE.md`.

This does not complete P12-B or establish runtime operation wiring, shared
mutation-epoch coverage, global owner-thread/quiescence, capture eligibility,
export, or hydration. P12-B remains incomplete, P12-A remains
`WAIT_DEPENDENCY`, P13 remains blocked, and Phase 12 remains open.

## P12-B per-NPC MoneyAccount census promotion — 2026-10-01

With approval, the reviewed candidate at `9f615d84c80b797397b85ea1fac2e32361081370` was fast-forwarded to `codex/phase12/canonical` from `43dba1b5d16cba1558c4c39239f3cd0d4c669958`. Its code-bearing commit is `2bdd0990acc2bdc2d6073b0863fc1ae94a209c4d` (tree `58f0e76525a0ee6dc9b7f73f34dcbf71db944a09`). Independent exact-tip implementation review passed; the durable record is `docs/design/PHASE12_P12B_NPC_MONEY_ACCOUNT_CENSUS_IMPLEMENTATION_REVIEW.md`.

The census publishes one passive schema-v1 witness per currently rostered NPC, ordered by RuntimeId and bound to that exact NPC's installed `MoneyAccountRuntime`. It reports cardinality one and the owner's existing local revision. Roster membership changes reconcile the family; positive debit/credit revision forwarding, roster add/remove and re-registration, aliases, missing owners, and replacement behavior are covered by focused tests.

Validation on the code-bearing tree passed focused census 10/10, selected bootstrap composition 14/14, ALL EditMode 2118/2118, official Smoke 5/5, and `git diff --check`. Exact XML/log names and SHA-256 values are recorded in the candidate document.

This remains passive local-revision evidence. Direct account writes are not wired to the protocol mutation epoch; the promotion makes no shared-epoch completeness, capture-eligibility, export/hydration, or full-profile census claim. P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked; Phase 12 remains open.

## Historical P12-B post-MoneyAccount blocker snapshot — 2026-10-01

At this historical snapshot based on canonical f913dd088f71b74cfab7c1bd8a1a79b7ce9a29ea, the partial protocol sealed 142 selected-profile sections at the authored ten-NPC day-zero roster: two PersonStore sections, two per-NPC SpatialKnowledge sections, one per-NPC Inventory section, one per-NPC MoneyAccount section, and ten per-NPC Knowledge sections. This is not the complete effective-profile owner inventory.

Other promoted passive witnesses include RuntimeIdentityRegistry, RuntimeIdAllocator, SimulationRecordSequence, ActorChoice, City presence/Market/SettlementPopulation, Genealogy, P8-A–D, legacy SpatialNetwork, ExplorableSite, Estate/Property, Institution/Office, ArmedForce/manpower/position, Conflict/War/Battle, ScheduledDirective, TravelParty, Expedition, and conditional receipts. They are not all registered in the sealed P12 protocol. City/NPC composite revisions, remaining Justice/Crime/economy census facts, and other mutable direct owners remain incomplete.

The preceding operation-matrix sentence was a historical snapshot before the
NPC-trade and Market invalidation promotions. Current canonical also notifies
the partial shared mutation epoch for the reviewed NPC-to-NPC trade and
Open-market purchase/sale operations, direct Market stock/changed-price
commits, and selected daily production, Free-consumption, and price-refresh
commits. Bootstrap publication, daily advance, and membership retain their
bounded scopes. Direct per-NPC MoneyAccount and Inventory writes and other
operation families remain uncovered; no selected cross-owner operation set
is complete for shared-epoch coverage. No CaptureEligible/token API exists.
P12-B remains INCOMPLETE; P12-A remains WAIT_DEPENDENCY; P13 remains BLOCKED.

The post-Market highest-value remaining P12-B operation/epoch gap is
`DESIGN_REQUIRED`: successful direct writes to the already-censused per-NPC
MoneyAccount and Inventory owners advance only their local revisions. The next
bounded contract must connect those committed writes to the partial shared
epoch, retain exact owner identity and baseline checks, and prevent duplicate
notifications when the same writers execute inside the already-instrumented
NPC-trade or Open-market purchase/sale operations. Other writer families,
complete owner coverage, and capture eligibility remain outside that slice.
This design work does not create a P12-B readiness claim. P12-A remains
`WAIT_DEPENDENCY`; P13 remains blocked.

### Design review correction — 2026-10-01

Independent review of the first docs-only blocker-refresh candidate at
`5372048860412f617250ade1e9be3b49479525d3` returned NEEDS_CHANGES. It found
that an account-local callback was not an enclosing multi-owner boundary and
that the design did not classify prepared account installs. Source review
confirmed that `MoneyAccountRuntime.InstallPrepared` is called from P18-D keyed
sale and P18 timeline daily economy, both outside `UnityBootstrap-Daily-v1`.
The candidate audit/design were revised to make the first bounded operation
NPC-to-NPC trade, covering only its already-registered buyer/seller account and
Inventory sections and every successful compensation under one named outer
scope. Later MerchantSystem plan completion, direct writer paths, other economy
methods and non-NPC account owners remain explicit gaps. This revised design
requires a fresh exact-tip independent review before implementation. P12-B
remains incomplete, P12-A remains `WAIT_DEPENDENCY`, and P13 remains blocked.

The selected-profile asset cross-check confirms the two `Simulation-GeneralTest`
City assets use CityData's Open-liquidity/Free-consumption defaults, so no
non-NPC MoneyAccountRuntime is instantiated for `UnityBootstrap-Daily-v1`.
Account-backed City owners remain out of this profile and require separate
sections if selected by a future profile.

## P12-B NPC trade owner-commit implementation candidate — 2026-10-01

The bounded NPC-to-NPC trade slice has been implemented on
`codex/phase12/P12BPostMoneyAccountBlockerRefresh`, based on current P12
canonical `f913dd088f71b74cfab7c1bd8a1a79b7ce9a29ea`. Code commit
`9c75311ee4920f6b45552361cb20e6152252a1ec` has tree
`8745ee9bf03aca9cdb17308f32cbf3abafea3332`. The implementation registers
`runtime.economy.npc-trade`, binds the shared transaction service before
bootstrap publication, validates participant section identity and unchanged
baselines before the first commit, and notifies each committed account or
Inventory revision—including successful compensation writes—inside one
scoped operation. Continuation bookkeeping failure faults P12 admission closed
without changing the established domain result.

Focused validation passed SimulationRuntimeAdmission 12/12,
NpcMoneyAccountCensus 10/10, NpcInventoryCensus 7/7,
ContinuationCensusProtocol 22/22, and EconomyTransaction 45/45. ALL EditMode
passed 2122/2122, official EditMode Smoke passed 5/5, and `git diff --check`
passed. Exact artifact paths and XML/log hashes are in
`docs/design/PHASE12_P12B_NPC_TRADE_INVALIDATION_CANDIDATE.md`.

The implementation remains limited to `TryExecuteNpcTrade` and its four
rostered NPC account/Inventory sections. MerchantSystem plan completion,
direct owner writes, other transaction families, complete profile census,
capture eligibility, export, and hydration remain uncovered. Independent
exact-tip implementation review and canonical promotion are pending. This
candidate does not complete P12-B, make P12-A ready, or unblock P13; Phase 12
remains open.

### Exact-tip review correction — 2026-10-01

Independent review of the first code candidate tip `5d42cef6a9ceec4eb0af49689e1f67c07d571eaf`
returned NEEDS_CHANGES: a failed bound P12 precommit admission disabled
notifications but still allowed the domain trade to commit. The correction at
`5ab42a9880b866f9f8d94b9365b2c5fb51c15ff7` returns the existing
`TransactionCommitFailed` result before any owner write when participant,
owner-thread, baseline, or operation admission fails. A service without a bound
P12 runtime retains its established path; bookkeeping failure after a committed
owner still preserves the trade result and faults P12 admission closed. Its
code tree is `6f8bab112ef0f7c97bfde31aa3dbd637db4f8a33`.

The revised code tree passed focused SimulationRuntimeAdmission 15/15,
NpcMoneyAccountCensus 10/10, NpcInventoryCensus 7/7,
ContinuationCensusProtocol 22/22, EconomyTransaction 45/45, ALL EditMode
2125/2125, official EditMode Smoke 5/5, and `git diff --check`. Exact filenames
and XML/log hashes are recorded in
`docs/design/PHASE12_P12B_NPC_TRADE_INVALIDATION_CANDIDATE.md`. Independent exact-tip code re-review passed against candidate tip
`49b32c548e4b8c666657c031105401246cff2563`. The durable record is
`docs/design/PHASE12_P12B_NPC_TRADE_INVALIDATION_REVIEW.md`, committed at
`0279c8e40b5c51bdf5cd6dd03247832766a726de`. Canonical promotion is recorded below.
P12-B remains incomplete, P12-A remains `WAIT_DEPENDENCY`, and
P13 remains blocked.

## P12-B NPC trade invalidation promotion — 2026-10-01

With approval, `codex/phase12/canonical` was fast-forwarded from
`f913dd088f71b74cfab7c1bd8a1a79b7ce9a29ea` to
`522cf9158d9f650675eccfb6bcec4144dbaa32e2`. The promoted bounded slice is
NPC-to-NPC trade owner-commit invalidation. The reviewed code candidate is
`49b32c548e4b8c666657c031105401246cff2563`; independent exact-tip review
passed and its durable record is
`docs/design/PHASE12_P12B_NPC_TRADE_INVALIDATION_REVIEW.md` at
`0279c8e40b5c51bdf5cd6dd03247832766a726de`.

The trade boundary covers only the exact rostered buyer/seller MoneyAccount
and Inventory sections, with per-commit notifications through success and
compensation. Failed bound precommit admission rejects before owner writes;
postcommit bookkeeping failure preserves the established domain result while
faulting P12 admission closed. Validation and artifact hashes remain in the
candidate record.

This does not complete P12-B or establish complete owner coverage, complete
shared-epoch coverage, capture eligibility, export, or hydration. P12-A remains
`WAIT_DEPENDENCY`; P13 remains blocked; Phase 12 remains open.


## P12-B direct NPC owner invalidation candidate

The direct per-NPC MoneyAccount and Inventory committed-write invalidation
candidate is now validated and independently reviewed. Its P12 canonical base
is 2f7c7422812de40aa1223e8310dcbd9f5d8ca474; exact code candidate
c49f957e45c3059231e9ec66e4010a7c3a389988 has tested tree
627af2fbd7f93e0025106ee5ba87e72bae6c4ed2. The durable exact-tip review and
validation record are
docs/design/PHASE12_P12B_DIRECT_NPC_OWNER_INVALIDATION_REVIEW.md and
docs/design/PHASE12_P12B_DIRECT_NPC_OWNER_INVALIDATION_CANDIDATE.md.

Validation passed the focused NPC owner invalidation, economy, MoneyAccount,
NPC census, Inventory census, Expedition, and runtime-admission suites; ALL
EditMode passed 2149/2149, official Smoke passed 5/5, and git diff --check
passed. The recorded Unity XML paths and SHA-256 hashes are in the candidate
record. This integration State is prepared on the candidate branch; canonical
promotion remains an explicit human gate.

The direct-owner slice connects only already-censused per-NPC MoneyAccount
and Inventory owner writes to the partial shared epoch. It does not complete
P12-B, complete the selected-profile owner set or shared-epoch coverage, prove
global owner-thread/quiescence, enable capture, or add export/hydration.
P12-A remains WAIT_DEPENDENCY and P13 remains blocked.

The P12-B operation/epoch matrix addendum records the updated blocker
classification. In particular, the earlier post-Market direct account and
Inventory writer gap is now a validated candidate but remains outside
canonical until this separate promotion gate is completed. The reviewed NPC
money-transfer operation design at commit
30c00d78fcb68c2969ac2eac3ed694423c55783e, with durable review record
884aa1b27f1de3a3d330eb75053bab01ffec7cc2 on
codex/phase12/P12BMoneyTransferOperationDesign, remains WAIT_DEPENDENCY until
the direct-owner capability is promoted. Do not infer transfer-operation
coverage from this candidate.

Architecture promotion 451340c56e9b676bf6ea43412bcb856b9ccde3de adds the
approved WorldId and factual-reader contracts; it does not reopen P9/P18 or
claim P12 capability. The candidate still requires serial integration and
revalidation at the shared SimulationRuntime window with WI-A and any later
runtime owner.


## Current canonical refresh — 2026-10-02

P12 canonical now includes the approved direct NPC owner-write invalidation
slice. The code candidate is `c49f957e45c3059231e9ec66e4010a7c3a389988`
(tree `627af2fbd7f93e0025106ee5ba87e72bae6c4ed2`), based on
`2f7c7422812de40aa1223e8310dcbd9f5d8ca474`. Its reviewed State/matrix
integration bundle is canonical at `9d1474b4299d8e888dd387e02e9018d9e8627f84`.
Exact-tip implementation review passed; the P12 candidate and review records
retain the focused suite results (NPC owner invalidation 13/13, economy
transaction 45/45, MoneyAccount census 27/27, NPC census 10/10, Inventory
census 7/7, Expedition 32/32, Runtime Admission 25/25), ALL EditMode 2149/2149,
official Smoke 5/5, and `git diff --check` PASS.

This slice links direct committed writes to already-censused per-NPC
MoneyAccount and Inventory owners with exact identity, owner-thread, local
revision, and protocol-baseline checks. It reports a successful direct
debit/credit or inventory add/remove once after commit, suppresses duplicate
notifications inside already-scoped NPC trade and Open-market operations, and
faults closed when supported roster replacement invalidates the owner binding.
It does not add owners or operation families.

The promotion supersedes the older candidate-status addenda below that say
this capability is waiting for promotion. It remains a bounded partial epoch:
P12-B is INCOMPLETE; P12-A is WAIT_DEPENDENCY; P13 is BLOCKED. It does not
prove complete owner census, complete shared-epoch coverage, global runtime
quiescence, capture eligibility, export, hydration, or phase closure.

WI-A implementation `3b39e0d89858dce517ad72cbb76da621eb954bad` was
reviewed against this P12 integration base and promoted with its exact-tip
review record at `codex/wia/canonical` tip
`93b6f0e8cedcb63437cbf5fa5de3461853c81462`. The targeted combined
`SimulationRuntime`/bootstrap revalidation passed at exact WI-A code tip
`3b39e0d89858dce517ad72cbb76da621eb954bad`; durable review evidence is
`codex/phase12/P12BWIARuntimeHotspotRevalidation` at
`313095ecec87b11928708607821c6b9ad9b5e275`, record
`docs/design/PHASE12_P12B_WIA_RUNTIME_HOTSPOT_REVALIDATION.md`. It confirms
the P12 owner hooks and operation scopes remain intact and WI-A closes the
existing bootstrap publication scope before exposing the world. The review
examined retained tests; it did not rerun them. The bounded FR-B factual-read
core was later promoted at P12 code tip
`0ad19ecd9633e01d358a9ff826ca3ca5f8e3627c` (tree
`09c3243ccb5f21a559dbcba3fa1dc6c4050ff516`), with exact-tip review and
validation recorded in
`docs/design/FRB_FACTUAL_READ_FOUNDATION_CANDIDATE.md`. It adds the core
contracts, admission, coordinator, and Faction/Person store guards only. FR-B
still needs production runtime composition and an exact selected-profile
Faction/Person read-cut proof. The partial P12 epoch is not a global coherence
boundary; no capture token or P12-B completion is implied.

The P12 operation gap identified at this refresh was the bounded NPC-to-NPC
money-transfer boundary. Its refreshed design was based on P12 canonical
`ed14e8dd9575a56461f7648d7ff786e114224785`, with exact design tip
`ea1d5b58ab8c5204c7559d13be10034e3a8732eb` and independent PASS review
record `74dc9b5360fb356eca58874878942ee90b786fd8` on
`codex/phase12/P12BMoneyTransferOperationDesignRefresh`. The contract covers
only the two exact rostered NPC MoneyAccount sections, uses promoted owner
hooks for each commit, and keeps one named scope across transfer and any
successful source compensation. The P12-bound raw-account overload rejects
before writes; finite zero transfer between distinct valid accounts retains
success without revisions or epoch advancement. The accepted P12-B
prerequisite authorization and exact design review made this slice
READY_FOR_IMPLEMENTATION. Its reviewed implementation is now promoted; the
remaining operation matrix must be reevaluated against the new canonical tip.

## P12-B NPC money-transfer operation promotion — 2026-10-02

With explicit approval, the implementation candidate
`codex/phase12/P12BMoneyTransferImplementation` was fast-forwarded from P12
canonical `53989ee940e0dc0b22492873ebaffb2cd02f8f58` to
`2f2b731866aea86eb52ef2b51eb687c88bf91bc4`. Its code-bearing commit is
`66415aefe6834fc73e9e6c3b22fe0ee3058cfa11` (tree
`862bceb6164bf5ddeed49ca8007d9be3ed4670d6`). Independent exact-tip review
passed with no actionable findings and is durably recorded at
`09f9f49ef85faf5c24eae57acba4426ca0bc39f8` on
`codex/phase12/P12BMoneyTransferImplementationReview`.

The candidate registers and scopes only
`runtime.economy.money-transfer` for a transfer between exact rostered NPC
MoneyAccount owners, including committed debit, credit, compensation and
result selection. Promoted owner hooks remain the commit notification path;
the P12-bound raw-account overload rejects before writes. The validation
record at the candidate contains matching retained XML/log hashes: admission
28/28, economy transaction 45/45, crime integration 12/12, ALL EditMode
2152/2152, official Smoke 5/5, and `git diff --check` PASS.

This promotion does not complete P12-B or establish complete owner or
operation coverage, universal shared-epoch coverage, runtime-wide
owner-thread/quiescence, capture eligibility, export/hydration, or P12-A
readiness. P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`; P13
remains blocked; Phase 12 remains open.

Current checkpoint status remains: P12-A WAIT_DEPENDENCY; P12-B INCOMPLETE;
P12-C through P12-G blocked by their documented prerequisites; P13 blocked.

## P12-B selected daily Merchant operation promotion — 2026-10-02

With explicit approval, `codex/phase12/canonical` was fast-forwarded from
`a66c215d1e8e9db34ea559a8bf2a0803fbdbf4ab` to the exact reviewed candidate
`5e643b7e4535b5bb699eb021d3b313243bf45658`. The code-bearing test commit is
`3ed113bf35546964cd4a56f424578dc1213f41fa` (tree
`876e810757c55b098d805e6e9ac012da7e6cab4d`). The candidate evidence is
`docs/design/PHASE12_P12B_MERCHANT_DAILY_OPERATION_CANDIDATE.md`; independent
exact-tip review passed and is durably recorded at
`5f4fd440720ff0d9e5461edab06a299a9a1a6830` on
`codex/phase12/P12BMerchantDailyOperationImplementationReview`.

The promoted boundary is only the selected daily Merchant operation
`runtime.merchant.advance-npc-trade-state`. It admits and batches mutations for
the reviewed Merchant-owned Knowledge and plan owners during the existing
normal call; direct supported owner-thread writes outside that nested operation
refresh the corresponding baselines and invalidate the changed plan owner.
The global decision allocator, record sequence, and decision read model remain
outside this slice.

Exact-tree validation passed: focused suites `SimulationRuntimeAdmissionTests`
31/31, `NpcPlanCensusTests` 5/5, `NpcKnowledgeCensusTests` 17/17,
`ContinuationCensusProtocolTests` 22/22, `NpcOwnerCommitInvalidationTests`
13/13, `P18DLocalKnowledgeObservationTests` 6/6,
`P18DConsumerIntegrationTests` 9/9, `MerchantLiquidityTests` 11/11,
`SimulationBootstrapCompositionTests` 14/14, and
`SimulationRuntimeLongRunTests` 7/7; ALL EditMode 2160/2160; official Smoke
5/5; and `git diff --check` PASS. The retained XML/log hashes are recorded in
the candidate evidence file.

This promotion does not establish P12-B completion, complete owner or
operation coverage, complete shared-epoch coverage, capture eligibility,
export, hydration, or P12-A readiness. P12-B remains INCOMPLETE; P12-A remains
`WAIT_DEPENDENCY`; P13 remains BLOCKED; Phase 12 remains open. The residual
owner and operation gaps are classified in the latest post-promotion matrix at
`docs/design/PHASE12_B_BLOCKER_RESOLUTION.md`; the matrix remains partial and
does not make P12-B ready.

## P12-B bounded FR-B factual-read core promotion — 2026-10-02

With explicit approval, P12 canonical was fast-forwarded from
`d6e52dcdbf5fe2a36de92c6516485040d54e4279` to reviewed branch tip
`451d06e62b7c95d7b600b77f10b0510049d62961`. The code-bearing commit is
`0ad19ecd9633e01d358a9ff826ca3ca5f8e3627c` (tree
`09c3243ccb5f21a559dbcba3fa1dc6c4050ff516`); the durable candidate and
exact-tip review evidence is
`docs/design/FRB_FACTUAL_READ_FOUNDATION_CANDIDATE.md`.

Focused `FactualReadFoundationTests` passed 8/8, ALL EditMode passed
2168/2168, the official Smoke filter passed 5/5, and `git diff --check`
passed. Independent exact-tip code review passed against P12 base `d6e52dc`
and architecture authority `c285466c355103d3637ac165246591b72eb7bda0`.

This promotes only the bounded core contracts, admission, coordinator, and
FactionStore/PersonStore read-time mutation guards. It does not compose live
readers into `SimulationRuntime`/bootstrap or prove the selected-profile
Faction/Person read cut. Readers must provide immutable copied fact values;
the generic result wrapper does not deep-copy references. This does not
establish complete owner/epoch coverage, capture eligibility, export,
hydration, P12-A readiness, P12-B completion, or P13 readiness. P12-B remains
INCOMPLETE; P12-A remains `WAIT_DEPENDENCY`; P13 remains BLOCKED; Phase 12
remains open.

## P12 WI-A current-base integration and revalidation promotion — 2026-10-02

With the user's explicit approval, `codex/phase12/canonical` was fast-forwarded
from `66f91c68d367e03703ab014046d5e62c3f89ebbe` to
`ae0a0b4fed921dfb3fd7e173a4787309120bece2`. The code-bearing commit is
`242ae6bf81c2f4da832be1e7948bbaac004a620b`, tree
`284e7a9a229671419c4ea0c545c02b4513e41717`, directly based on the prior P12
canonical tip. The promoted tip adds the durable exact-tip review and
revalidation record at
`docs/design/PHASE12_P12B_WIA_POST_FRB_REVALIDATION.md`; no source or test code
changed after validation/review.

Independent exact-tip review passed on code `242ae6b` and tree `284e7a9` against
P12 base `66f91c6`, architecture `c285466c355103d3637ac165246591b72eb7bda0`,
and WI-A canonical baseline `534d2dd1b6ed8cdc168c0c90328adc6b6c1eedf5`.
Validation on that exact code tree passed four focused suites (105/105), ALL
EditMode (2174/2174), official Smoke (5/5), and `git diff --check`. The
review/validation record includes the exact per-suite XML/log SHA-256 values;
local artifacts are retained under
`Library/ValidationResults/WIA-P12-PostFRB-20261002/`.

This promotes only the bounded WI-A World Identity integration/revalidation
with the current P12 bootstrap/admission composition. It does not provide P12
persistence, save/load, P13 fork behavior, FR-B live read composition or read
cut, a Faction projection, or a World Exchange producer. It does not complete
P12-B or make P12-A ready. P12-B remains INCOMPLETE; P12-A remains
`WAIT_DEPENDENCY`; P13 remains BLOCKED; Phase 12 remains open.

## P12-B record-sequence invalidation candidate review — 2026-10-02

The P12-B `SimulationRecordSequence` mutation-invalidation candidate is
independently reviewed and validated for canonical consideration. Its exact
canonical base was `1ac675cc558aa919a749167647c10506c11303fc`; exact code tip is
`cd7ca4498d2c1d3591c227bd9011429f9bd06d8f` (tree
`5282d65fbc4311bb6b907770a4e6fa363ad633df`). The pushed candidate/evidence tip
at review time was `c4acda50aee93da1511139d03437d006d9156319` on
`codex/phase12/P12BRecordSequenceInvalidationNestedScopeFix`. The durable
design review is `6c46dfc`; the implementation review is recorded in
`docs/design/PHASE12_P12B_RECORD_SEQUENCE_INVALIDATION_IMPLEMENTATION_REVIEW.md`.

The initial exact-tip implementation review requested a test for one sequence
allocation under nested registered operation scopes. The candidate adds that
focused assertion without production-code changes. Exact-code validation
passed focused `SimulationRecordSequenceP12InvalidationTests` 6/6, ALL
EditMode 2180/2180, official Smoke 5/5, and `git diff --check`; the exact XML
and log paths and SHA-256 hashes are recorded in the candidate and review
documents.

This State entry records a reviewed candidate, not canonical delivery or
promotion. The bounded scope is only invalidation for the one selected-profile
record-sequence owner. Event, Decision, occurrence-receipt sections and
preceding domain mutations remain uncovered. No complete owner/operation or
shared-epoch coverage, global quiescence, capture eligibility, export,
hydration, or P12-B completion is claimed. P12-B remains INCOMPLETE; P12-A
remains `WAIT_DEPENDENCY`; P13 remains BLOCKED; Phase 12 remains open.

## P12-B selected-profile record-sequence invalidation promotion — 2026-10-02

With explicit approval, `codex/phase12/canonical` was fast-forwarded from
`1ac675cc558aa919a749167647c10506c11303fc` to
`e64caf08e7ada24a0f6b8c193207a6242018896d`. The promoted code tip is
`cd7ca4498d2c1d3591c227bd9011429f9bd06d8f`, with unchanged reviewed tree
`5282d65fbc4311bb6b907770a4e6fa363ad633df`. The final preflight confirmed
fast-forward ancestry, exact candidate identity, unchanged code tree, and
matching review/validation evidence before promotion. Independent exact-tip
implementation review is recorded in
`docs/design/PHASE12_P12B_RECORD_SEQUENCE_INVALIDATION_IMPLEMENTATION_REVIEW.md`.

The bounded addition connects only the selected-profile
`SimulationRecordSequence` owner to P12 invalidation after successful
`Allocate()` writes on the reviewed production paths. Nested registered
operation scopes produce one owner revision and one shared-epoch increment
for the allocation. Exact-tree validation passed focused
`SimulationRecordSequenceP12InvalidationTests` 6/6, ALL EditMode 2180/2180,
official Smoke 5/5, and `git diff --check`; artifact names and SHA-256 values
are recorded in
`docs/design/PHASE12_P12B_RECORD_SEQUENCE_INVALIDATION_CANDIDATE.md`.

Event, Decision, occurrence-receipt, and preceding domain-mutation sections
remain outside this slice. This does not establish complete owner coverage,
complete shared-epoch coverage, global quiescence, capture eligibility,
export, hydration, P12-A readiness, P12-B completion, or P13 readiness. P12-B
remains INCOMPLETE; P12-A remains `WAIT_DEPENDENCY`; P13 remains BLOCKED; Phase
12 remains open. The next refresh must use canonical tip `e64caf0` and consume
the queued FR-B live-integration handoff only after recomposition and required
current-base revalidation.

## P12-B FR-B selected-profile live integration promotion — 2026-10-02

With explicit approval, `codex/phase12/canonical` was fast-forwarded from
`1dce6d54a33ac1b1778b1a44a7794512a58416e8` to
`28d33a2c8f10ddb724819893a0b5f2804a6f5b0b`. The promoted code tip is
`aeb76c687d00a49f505ab264a58a508a20e4923b`, tree
`862eb904c6679a66d4dd2a2e2ad7f174ec8479c2`. The exact-tip independent
implementation review and promotion record are durably included at the
promoted tip; the review commit adds only
`docs/design/FRB_LIVE_INTEGRATION_P12_CURRENT_REVIEW.md` after the reviewed
code commit. The earlier old-base FR-B candidate and review remain preserved.

The bounded integration exposes a `FactualReadCoordinator` on each composed
runtime, but only `UnityBootstrap-Daily-v1` binds its exact composed
`FactionStore`/`PersonStore` pair. That surface becomes available after
successful world publication and healthy bootstrap-operation closure. Reads
require the bound owner thread and healthy idle runtime, and bracket the
synchronous read cut with the selected profile's logical day and exact store
revisions while supported owner mutations are guarded. Other profiles remain
unbound/unavailable and the reader set remains empty, so requested capabilities
are unsupported. The partial P12 mutation epoch is a health signal only, not a
whole-world coherence boundary.

Exact-tree validation passed `SimulationBootstrapCompositionTests` 21/21,
ALL EditMode 2182/2182, official EditMode Smoke 5/5, and
`git diff --check`. The retained XML/log hashes and independent exact-tip
review are recorded in
`docs/design/FRB_LIVE_INTEGRATION_P12_CURRENT_REVIEW.md`.

This does not establish P12-B completion, complete owner or shared-epoch
coverage, runtime-wide quiescence, capture eligibility, export, hydration,
P12-A readiness, or P13 readiness. Day-zero factual reads allowed by FR-B are
not P12-A completed-day capture eligibility. P12-B remains INCOMPLETE; P12-A
remains `WAIT_DEPENDENCY`; P13 remains BLOCKED; Phase 12 remains open. The
post-promotion DAG adds no newly READY numbered-phase implementation checkpoint:
P12-A and P12-C through P12-G retain their documented prerequisite edges,
and P13 remains blocked on continuation plus recoverable causal inputs.

## P12-B Faction factual-read (FR-C) integration promotion — 2026-10-02

With human approval and final preflight, `codex/phase12/canonical` was fast-forwarded from reviewed current base `0f36331d84ad3139171d36f980dfe6fa635ae30c` to the FR-C integration handoff tip `9a6f78c43e7ad0a8f73366055151a7710e24a759`. The reviewed code commit is `ec042b30b1c0a390f611c47cb22b75631cfb9556`, tree `fabb182e79bf7c36035b791736edcb42785056ac`. Candidate documentation commit: `4a083e30efffd3761128ff5bdfb7b606397c74b0`. Independent exact-tip review record: `360dcdf76f69dadb4498dcfab06379621e7ac18d`; review result PASS against the exact base and code tree. The candidate, review, and handoff records are retained at `docs/design/FRC_FACTUAL_READER_CANDIDATE.md`, `docs/design/FRC_FACTUAL_READER_CURRENT_BASE_REVIEW.md`, and `docs/design/FRC_FACTUAL_READER_CURRENT_BASE_HANDOFF.md`.

The preflight confirmed that canonical still equaled the reviewed base, the candidate was a clean fast-forward descendant, the reviewed code/tree were unchanged, the candidate carried the exact review record, and `git diff --check` passed. No newer canonical changes or semantic hotspot conflicts were present. Validation remains the exact-code evidence in the candidate record: FactualReadFoundationTests 9/9, FactionFactualReaderTests 7/7, SimulationBootstrapCompositionTests 21/21, ALL EditMode 2190/2190, official Smoke 5/5, and `git diff --check` PASS. Tests were not rerun because the code tree was unchanged.

The promoted addition is the immutable `simulation.faction-truth/v1` reader and its narrow registration through `SimulationRuntime`, with corresponding bootstrap coverage. Facts are copied and deterministically ordered; active and ended affiliation handling, Person endpoints, logical-boundary/revision checks, and fail-closed diagnostics follow the reviewed contract. Knowledge, support, and direct mutable Store exposure remain excluded.

The numbered-phase DAG was refreshed after promotion. FR-C promotion does not complete P12-B or change checkpoint readiness: P12-A remains `WAIT_DEPENDENCY`; P12-B remains `INCOMPLETE`; P12-C through P12-G remain blocked by their documented prerequisites; P13 remains blocked; Phase 12 remains open. No new numbered-phase implementation checkpoint became READY. Continue the independent P12-B owner/operation coverage work against the new canonical tip. This promotion does not claim broader FactualRead completeness, save/load, capture eligibility, P12-A or P13 readiness, World Exchange production, or External collection coverage.


## P12-B bounded `TravelPartySystem.AdvanceParties` operation/invalidation promotion — 2026-10-02

With explicit approval, `codex/phase12/canonical` was fast-forwarded from
`6b30d86c3214a98603bea809154e2dc06047d6a3` to
`4a9a6977d7b0a2b4a7258559fe127946337d17fc`. The exact reviewed code tip is
`ac0bcffe4d345c81d77bfa56b19e3591a9ebb46c` (tree
`14e2f4e485a83791af43b781546bd6f90b3913f5`); the promoted tip adds the
candidate/review evidence only after that code. Independent exact-tip review
PASS is recorded in
`docs/design/PHASE12_P12B_TRAVEL_PARTY_ADVANCE_IMPLEMENTATION_REVIEW.md`.

The bounded selected-daily-profile operation wraps the existing
`TravelPartySystem.AdvanceParties` call inside `runtime.advance-day`. It
accounts for the reviewed TravelParty store, per-NPC travel progress, City
presence, SpatialKnowledge, and record-sequence commits as one nested
invalidation boundary, preserving the existing arrival order, partial
progress, and event-failure behavior. It adds no P18 path or gameplay rule.

Retained exact-code validation passed all 9 focused suites, ALL EditMode
2200/2200, official Smoke 5/5, and `git diff --check`; artifact paths and
hashes are recorded in
`docs/design/PHASE12_P12B_TRAVEL_PARTY_ADVANCE_CANDIDATE.md`. The reviewed code
tree is unchanged by the promotion record, so tests were not rerun for
documentation-only bookkeeping.

The refreshed owner/operation/epoch matrix is in
`docs/design/PHASE12_B_BLOCKER_RESOLUTION.md`. The next supported high-impact
gap is the selected allocator's Event counter: its passive witness exists but
successful `AllocateEventId()` writes are not yet connected to the partial
shared epoch. This is a bounded invalidation target, not a global allocator
coverage claim. The solo travel-start outer operation remains a subsequent
candidate after that owner hook is reviewed and integrated.

This promotion does not establish P12-B completion, complete owner or
operation coverage, complete shared-epoch coverage, global quiescence, capture
eligibility, export, hydration, P12-A readiness, or P13 readiness. The
numbered-phase DAG was refreshed: P12-A remains `WAIT_DEPENDENCY`, P12-B
remains `INCOMPLETE`, P12-C remains blocked on P12-B, P12-D/E on P12-B and
P12-C, P12-F on P12-C/D/E, P12-G on P12-B through P12-F plus a validated live
profile inventory, and P13 remains blocked on continuation plus recoverable
causal inputs. Phase 12 remains open.

## P12-B RuntimeIdAllocator Event-counter invalidation promotion — 2026-10-03

After final preflight, P12 canonical was fast-forwarded from
`aa8f0305bea9f10c15045e07400d8785c2bd9e23` to
`55ac2ebdec4bdbcda6085668188730b7bcb9cd5a`. The reviewed code is
`a573e5120951f8ac10c2da5b6ad79e066991a57a`, tree
`8e3e2966601d834c2c23429e253d02a9d1a7bb8c`. Exact-tip independent review is
PASS in
`docs/design/PHASE12_P12B_RUNTIME_ID_EVENT_COUNTER_INVALIDATION_IMPLEMENTATION_REVIEW.md`;
candidate identity and retained validation hashes are in
`docs/design/PHASE12_P12B_RUNTIME_ID_EVENT_COUNTER_INVALIDATION_CANDIDATE.md`.

The selected `UnityBootstrap-Daily-v1` runtime now registers and binds only
the existing cardinality-one Event counter witness. Successful
`AllocateEventId()` writes invalidate that owner and the partial shared epoch;
allocations join an active TravelParty or Merchant batch. Exhaustion and
rejected preflight do not advance the counter. A later event-construction or
storage failure does not roll back an already consumed ID. The exact legacy
exhaustion message is preserved.

Exact-tree validation passed the focused record-sequence invalidation suite
11/11, TravelParty advance 10/10, bootstrap composition 21/21, ALL EditMode
2205/2205, official Smoke 5/5, and `git diff --check`. XML/log names and
SHA-256 values are retained in the candidate evidence document.

The refreshed P12-B matrix is in
`docs/design/PHASE12_B_BLOCKER_RESOLUTION.md`. It reclassifies Event allocation
as covered only for this selected owner and identifies solo travel-start as
the next bounded cross-owner operation to audit. Other allocator counters,
unrelated Event/Decision/read-model writes, and preceding domain commits
remain outside this slice.

### Numbered-phase DAG refresh

This promotion changes no dependency edge or checkpoint readiness. P12-A
remains `WAIT_DEPENDENCY` pending complete included-owner export and staged
hydration, a validated complete live-profile inventory, and its separate
implementation authorization. P12-B remains `INCOMPLETE`. P12-C remains
blocked on P12-B; P12-D and P12-E remain blocked on P12-B/P12-C; P12-F remains
blocked on P12-C/P12-D/P12-E; P12-G remains blocked on P12-B through P12-F
and the validated live inventory. P13 remains blocked on P12 continuation and
recoverable causal inputs/history. Phases 9 and 18 retain their recorded
closure scopes; no new numbered-phase implementation checkpoint became
`READY`, and Phase 12 is not ready for closure.

P12-B still lacks complete live owner/cardinality coverage, complete
committed-write/shared-epoch coverage, runtime-wide owner-thread/quiescence
proof, and capture eligibility. No export, hydration, P12-A readiness, P13
readiness, or Phase closure is claimed.


## P12-B selected-profile solo travel-start operation promotion  2026-10-03

With the standing bounded-promotion authorization and successful final preflight, `codex/phase12/canonical` advanced from `e76e50bfefc68b827d54ceb74e0b25293e0ad7b8` to `26346b51a9c591b30bdf5e70c43520a1d9ac563f`. The reviewed executable code is `fe0e0be03403e92001173deae1fafe58dfe432d2`, tree `d72e84d6a442440ab82bacf6a0eb32165a9d7055`. Candidate evidence and independent implementation review are retained in `docs/design/PHASE12_P12B_SOLO_TRAVEL_START_OPERATION_CANDIDATE.md` and `docs/design/PHASE12_P12B_SOLO_TRAVEL_START_OPERATION_IMPLEMENTATION_REVIEW_R2.md`.

The bounded selected-profile operation enters around the existing bound `TravelActionProvider` only for the selected Travel action. The source-City presence section is included only if that City reciprocally contains the NPC, matching the mutation in `NpcRuntime.StartTravel`. Exact-tree validation passed the focused solo-travel suite 11/11, affected economy/spatial/runtime suites, ALL EditMode 2216/2216, official Smoke 5/5, and `git diff --check`. The evidence document records the superseded 10/11 fixture attempt and corrected 11/11 rerun. No Travel scheduling API or unsupported Travel directive was added.

This promotion covers only this bounded operation/invalidation slice. It does not establish complete live-owner or operation coverage, complete shared-epoch coverage, global quiescence, capture eligibility, export, hydration, P12-A readiness, P12-B completion, P13 readiness, or Phase 12 closure.

### Numbered-phase status after solo travel promotion

No dependency edge or phase readiness changed. P12-B remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains open. The P12 owner/operation/epoch matrix will be refreshed after the queued WX-D candidate has been recomposed and integrated, as directed by the Phase Master. WX-D is outside P12 scope and is not included as P12 continuation capability.
## WX-D factual producer handoff after P12 solo-travel promotion — 2026-10-03

With the standing bounded-promotion authorization and successful exact-tip preflight, `codex/phase12/canonical` was fast-forwarded from `4d9f48fceea4e0742e7ebc2c5199df5163051b7c` to the reviewed WX-D integration tip `55a93ba58b572dafb70647f387148c0a4bde97c3`. This integrates the solo-travel promotion already at `4d9f48f` with the additive World Exchange v2 producer handoff. WX-D executable code is `aab725b89366e65fd839c47b8ae36ad91cd560d5`, tree `d248892c37bac86142863d034e041384768553eb`; the tested combined candidate is `2d23eb8a05615be98ec12da1c7b90d6317e23131`, tree `3b9dd4738f40b64d778c49a7d15189374d88b363`. Fresh exact-tip independent integration review is recorded in `docs/design/WXD_V2_POST_SOLO_TRAVEL_INTEGRATION_REVIEW.md` and the validation evidence/hashes in `docs/design/WXD_V2_POST_SOLO_TRAVEL_INTEGRATION.md`.

The source and replayed WX-D implementation patches have identical stable patch IDs and 22 changed paths, with no overlap against solo-travel files. The integration classification is `BASE_DRIFT_ONLY`; no WX-D code adaptation was needed. Focused producer 7/7, FR-B 9/9, FR-C 7/7, bootstrap composition 21/21, ALL EditMode 2223/2223, official Smoke 5/5, and `git diff --check` passed on the exact executable tree. Review verified all retained artifact hashes and that Simulation-External `main` remains `0ce8403ba05f778db6850f566a974a4c56cf4edb` with schema blob `5619013647c31d969a7cd49e0563ff68c78cdde3`.

The promoted WX-D producer remains bounded to WI-A WorldId and FR-C Faction factual projection through World Exchange v2, truthful `collectionCoverage`, deterministic artifact/file generation, and fail-closed factual-read behavior. It does not claim whole-World projection, save/load, IPC/live sync, write-back, P19 integration, or any P12 continuation capability; it is a cross-track producer handoff, not P12-B delivery.

### Numbered-phase DAG refresh

The canonical move adds no dependency edge and no new Phase 12 readiness. The P12-B blocker matrix was re-read against this canonical tip and refreshed in `docs/design/PHASE12_B_BLOCKER_RESOLUTION.md`. P12-B remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P12-C remains blocked on B; P12-D/E remain blocked on B and C; P12-F remains blocked on C/D/E; P12-G remains blocked on B through F and a validated live-profile inventory; P13 remains blocked on continuation and recoverable causal inputs/history. Phase 12 remains open. No capture eligibility, complete owner or shared-epoch coverage, global quiescence, export, hydration, P12-A readiness, or P13 readiness is inferred.

## P12-B selected-profile RuntimeIdAllocator Decision-counter invalidation promotion — 2026-10-03

After the final refreshed preflight, `codex/phase12/canonical` advanced from
`22525cb5f96eb9eed2e168b7e6a23fdc1e420304` to
`22e5e51ac1383f91c30ea9d1c15351020dd93b38` by clean fast-forward. The reviewed
implementation is `52154053219e30e679bc400adf55c2f577bd8106`, exact tree
`ddd3684daf264a15af9c217c8edb4e390e82602d`. Independent exact-tip review
passed for documentation candidate `42b61a6fd41785461d6137098c8277acfcf00146`
and the unchanged code tree; its durable final review record is the tip
`22e5e51ac1383f91c30ea9d1c15351020dd93b38`.

The selected `UnityBootstrap-Daily-v1` protocol registers and binds only the
existing required, cardinality-one Decision counter witness on the exact
`RuntimeIdAllocator`. A successful `AllocateDecisionId()` preflights owner
thread, baseline revision, and epoch capacity before incrementing, then
reports only the Decision section. Existing TravelParty/Merchant nested
batching is preserved. The consumed ID remains committed if later record
sequence allocation fails. No other allocator counters, DecisionStore,
occurrence receipts, ActorChoice, or new operation semantics are included.

Retained exact-tree validation passed the five focused suites (15/15, 31/31,
21/21, 14/14, 3/3), ALL EditMode 2227/2227, official EditMode Smoke 5/5, and
`git diff --check`. The candidate and implementation-review records contain
artifact paths and SHA-256 hashes. This is one bounded owner invalidation
slice only: P12-B remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P13
remains `BLOCKED`; Phase 12 remains open. No complete owner/operation/shared-
epoch coverage, global quiescence, capture eligibility, export, hydration,
P12-A readiness, or P13 readiness is inferred.

### Numbered-phase DAG refresh

No dependency edge or readiness label changes: P12-A remains
`WAIT_DEPENDENCY`; P12-B remains `INCOMPLETE`; P12-C waits on B; P12-D/E wait
on B and C; P12-F waits on C/D/E; P12-G waits on B through F plus validated
live-profile inventory; P13 remains blocked on P12 continuation and
recoverable causal inputs/history. Phase 12 is not ready for closure.

## P12-B selected-profile ActorChoice owner-invalidation promotion — 2026-10-03

After refreshed exact-tip preflight, `codex/phase12/canonical` advanced by
clean fast-forward from `f1ec63ea7fa0592b3a280e138a80023e3cacc6b7` to
`3b25852bfc678095dd97327aecfaa2559b151bc1`. The reviewed implementation is
`0bb87c89662397857c7e55267bbc60f32ce0676a`, tree
`aa570c943299eafe52c9a5a9b05a9487bdfd5add`; the exact-tip implementation
review and retained validation are recorded in
`docs/design/PHASE12_P12B_ACTOR_CHOICE_INVALIDATION_IMPLEMENTATION_REVIEW.md`
and `docs/design/PHASE12_P12B_ACTOR_CHOICE_INVALIDATION_CANDIDATE.md`.

For the selected `UnityBootstrap-Daily-v1` profile, P11 ActorChoice is now
registered against the exact runtime-owned store. Supported successful
capture/disposition commits preflight the owner-thread, section baseline, and
epoch capacity before mutation, then notify after commit through the existing
P12 operation batching/direct path. This does not register or bind the P18
temporal section. Exact-tree validation passed ActorChoice census 9/9, the
ActorChoice suite 49/49, runtime admission 31/31, bootstrap composition
21/21, ALL EditMode 2231/2231, official Smoke 5/5, and `git diff --check`.

This is one bounded P11 owner-invalidation slice. P12-B remains `INCOMPLETE`;
P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked. It does not establish
complete owner/operation/shared-epoch coverage, global quiescence, capture
eligibility, export, hydration, or Phase closure.

The refreshed blocker matrix selects ScheduledDirective committed-state
invalidation as the next bounded source/design task. The selected runtime
composes its store and system; `AdvanceDayAfterClockAdvance` prepares due
directives and the actor-turn path marks supported terminal outcomes. The
store-local revision/witness is already canonical at
`03ffa1031c3a7f125d1d8cff00f72d22749816bb`, and its provider is exposed by
the bootstrap composition at `72239ad`. The current P12 runtime does not
register/bind this owner section. The next task is that missing live
owner-invalidation boundary, not a census-only delivery; preserve the
existing owner witness rather than rebuilding it.

This State and the corrected blocker matrix are current through canonical
docs-only tip `a6470ad1fea3134d08219c43b41fac6ebf0a44ab`; the ActorChoice
implementation promotion itself remains at `3b25852`.

### Numbered-phase DAG refresh

No dependency edge or readiness label changes: P12-A remains
`WAIT_DEPENDENCY`; P12-B remains `INCOMPLETE`; P12-C waits on B; P12-D/E wait
on B and C; P12-F waits on C/D/E; P12-G waits on B through F plus validated
live-profile inventory; P13 remains blocked on P12 continuation and
recoverable causal inputs/history. Phase 12 remains open and is not ready for
closure.

## P12-B ScheduledDirective selected-profile invalidation promotion — 2026-10-03

Under the standing `AUTONOMOUS_BOUNDED_PROMOTION` policy, `codex/phase12/canonical` fast-forwarded from `54fc23b89cb13598dd184a2b5b6bacf9dc23b0a7` to reviewed candidate `f23cbd9b6959771249e8f4446808b8402fbd8b9f`. The implementation code is `b8dced9666438d736c8bd2b417390d52988f3c78`, exact tree `248abaacad1538a40a4b0a7af4e1898749993128`. Independent exact-tip review PASS is recorded in [`PHASE12_P12B_SCHEDULED_DIRECTIVE_INVALIDATION_IMPLEMENTATION_REVIEW.md`](design/PHASE12_P12B_SCHEDULED_DIRECTIVE_INVALIDATION_IMPLEMENTATION_REVIEW.md); candidate and retained validation artifact hashes are in [`PHASE12_P12B_SCHEDULED_DIRECTIVE_INVALIDATION_CANDIDATE.md`](design/PHASE12_P12B_SCHEDULED_DIRECTIVE_INVALIDATION_CANDIDATE.md).

The bounded adapter registers the exact selected-profile `ScheduledDirectiveStore` witness, preserves optional omission by standalone runtimes, and requires exact store/provider identity at published bootstrap composition. Successful supported post-bind Adds and terminal transitions preflight before commit and report after commit, including duplicate/unresolved skips from `PrepareDay`; transient `TryTakeDirective` remains outside the authoritative section. The integration uses the existing direct and nested-operation invalidation paths.

Exact-tree evidence: `ScheduledDirectiveCensusTests` 16/16; `SimulationRuntimeAdmissionTests` 31/31; `SimulationBootstrapCompositionTests` 21/21; `SimulationRuntimeOrchestrationTests` 12/12; ALL EditMode 2241/2241; official EditMode Smoke 5/5; `git diff --check` PASS. The independent reviewer verified all six retained XML/log hash pairs. Tests were not rerun for this promotion because the reviewed executable tree did not change.

This promotion covers only selected-profile ScheduledDirective owner invalidation. P12-B remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P12-C through P12-G retain their dependency gates; P13 historical reconstruction/fork remains blocked on continuation and recoverable causal history. No complete owner or shared-epoch coverage, global quiescence, capture eligibility, export, hydration, P12-A/P13 readiness, or Phase closure is claimed.

### Architecture refresh and numbered-phase DAG

This promotion was revalidated against current architecture `3bf09249b7dd9e255c3493aacfd75c96080a31e3`. Its P15-A/P16-A planning additions are upstream-irrelevant to this P12 code tree; `UnityBootstrap-Daily-v1` remains unchanged. P15-A and P16-A are independently implementation-ready handoffs and are being developed in isolated candidates. Before P15 promotion, prove the new `StructureStore` is excluded from the daily profile. Before P16 promotion, prove the new carried-supply/receipt state cannot be silently omitted from P12; its extension of the ArmedForce spatial owner requires serial P12 integration and a negative daily-profile rejection test.

P13 retention/causal-input work remains design-only; authoritative fork implementation is dependency-blocked. P19 public extension design is ready, while loader/runtime work stays deferred. P10/P14/P20 follow-ons remain `READY_FOR_PRODUCT_SCOPE_DECISION`; P17 remains deferred. This promotion changes no numbered-phase dependency edge. Phase 12 remains open.

## P12-B selected-profile SettlementPopulation/person/NPC lifecycle invalidation promotion — 2026-10-05

With the standing authorization for routine bounded checkpoint promotion and a successful exact-tip preflight, `codex/phase12/canonical` advanced by clean fast-forward from `a6572ab3d4330d81edb334ae8b4c84ca5e6b173e` to `16d09ece1958c73152bf3f04c82ac4a0d177bfbe`. The reviewed code is `f6e9b1ca14e9bfbb9e8f19622272610abdf2cc01`, tree `e1bb56f97b0988247a092f96e9757a8bdd0e8380`. Candidate evidence is `docs/design/PHASE12_P12B_SETTLEMENT_POPULATION_INVALIDATION_CANDIDATE.md`; exact-tip independent implementation review PASS is `docs/design/PHASE12_P12B_SETTLEMENT_POPULATION_INVALIDATION_IMPLEMENTATION_REVIEW.md` and is committed in the promoted tip.

For the selected `UnityBootstrap-Daily-v1` profile, this slice registers exact per-City SettlementPopulation aggregate/receipt owners and singleton Person/NPC life-residence owners. It admits only the reviewed lifecycle, residence, and paired-migration operations, reserves participating local revisions and one shared-epoch increment before writes, then notifies once after the complete operation. Natural mortality and aggregate demography remain required to resolve disabled at admission. P12-bound injury writes fail before mutation because the selected profile has no injury owner or operation; unbound behavior is unchanged. Named birth and receipt-bearing Person death remain excluded as documented.

Retained exact-tree validation passed `P12PopulationLifecycleInvalidationTests` 9/9, ALL EditMode 2344/2344, official Smoke 5/5, and `git diff --check`. Exact XML/log SHA-256 values and the validation-log archive hash are recorded in the candidate and review artifacts. The implementation does not establish complete owner/write or shared-epoch coverage, global quiescence, capture eligibility, export, hydration, P12-A readiness, P13 readiness, P12-B completion, or Phase 12 closure.

### Readiness after promotion

P12-B remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P12-C waits on B; P12-D and P12-E wait on B and C; P12-F waits on C/D/E; P12-G waits on B through F and validated live-profile inventory; P13 remains blocked on P12 continuation and recoverable causal inputs/history. The promoted slice changes no checkpoint dependency edge. The latest blocker matrix is appended to `docs/design/PHASE12_B_BLOCKER_RESOLUTION.md`; it identifies direct NPC MoneyAccount/Inventory committed-write invalidation as the next bounded revalidation target, not as completed coverage.

## Current canonical and blocker refresh — 2026-10-05

Remote `codex/phase12/canonical` is `39e275f39e1602d3fa109d3a1bb9acd60585a3f2`. The selected-profile Population lifecycle invalidation code is `f6e9b1ca14e9bfbb9e8f19622272610abdf2cc01`, tree `e1bb56f97b0988247a092f96e9757a8bdd0e8380`; its exact-tip review and validation remain retained in the prior promotion record. Direct NPC MoneyAccount/Inventory invalidation code `c49f957e45c3059231e9ec66e4010a7c3a389988` is also already in current canonical. The old State sentence immediately above is preserved as a historical record; the current blocker matrix records its correction and the matching tree evidence.

The next P12-B technical design is the existing selected-profile legacy Crime/Justice daily mutation path, as recorded in `docs/design/PHASE12_P12B_CRIME_JUSTICE_INVALIDATION_DESIGN.md`. The revised exact-tip review passed at `452426687a57c1cfebb3f007511c5e23a21273a2`, tree `e970567ae059dc8c537739469cfff65c0d67fa6b`; the durable review record is `docs/design/PHASE12_P12B_CRIME_JUSTICE_INVALIDATION_DESIGN_REVIEW.md`. Under the previously accepted P12-B prerequisite-capability authority, this bounded sub-slice is ready for implementation. No implementation is started or claimed by this State refresh.

The current architecture authority remains `codex/architecture/world-identity-projection` at `ffd75652d89d862b83d634868c560f8540869b89`, including the intraday/extensibility and multi-participant alignment records. The approved bounded P10 Ruin and P14 City profiles remain separate because P8 retains one top-level owner per Location; no combined bootstrap profile is implied.

P12-B remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P12-C waits on B; P12-D/E wait on B and C; P12-F waits on C/D/E; P12-G waits on B through F plus validated live-profile inventory; P13 remains blocked on continuation and recoverable causal inputs. No complete owner/operation/shared-epoch coverage, global quiescence, capture eligibility, export, hydration, downstream readiness, or Phase closure is claimed.

## P12-B Crime/Justice same-day writer correction — 2026-10-05

Current remote P12 canonical remains `39e275f39e1602d3fa109d3a1bb9acd60585a3f2`. The earlier exact-tip design review at `5d5539dd198d74324eb7ecfb296819f4f38d83e9` covered only the four legacy calls in `BeginSimulationDay`. A source audit showed that `TryAdvanceDay` keeps the same `runtime.advance-day` operation active through the later actor-turn loop, where Crime/Guard providers, failure handlers, successful actor/target status changes, and forced Escape directives can mutate those same owners.

The current design revision at `codex/phase12/P12BCrimeJusticeInvalidationDesign` extends the bounded selected-profile contract to those same-day NPC status/hidden and Justice facts. The four legacy calls retain sequential per-call epoch reservations. Action paths use owner-specific admission scopes and immediate preflight/notification for each changed owner leaf so independent Account, Event/RecordSequence, allocator, Travel, Merchant, ActorChoice, and directive callbacks continue normally. The revision also routes mutable WantedRecordRuntime and PrisonSentenceRuntime rows through the exact Justice owner callback, blocks unsupported direct writes to P12-bound status/Justice owners, and rejects P18 receipt commits under the selected P12 Daily profile. CrimeSocialAppraisal stores remain explicit uncovered owners.

The earlier review does not cover these additions. The revised design passed fresh independent exact-tip review at `452426687a57c1cfebb3f007511c5e23a21273a2`, tree `e970567ae059dc8c537739469cfff65c0d67fa6b`; its durable review is `docs/design/PHASE12_P12B_CRIME_JUSTICE_INVALIDATION_DESIGN_REVIEW.md`. Under previously accepted P12-B prerequisite-capability authority, implementation may proceed within this reviewed boundary. Implementation and validation have not started. `git diff --check` passes for the documentation revision. P12-B remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P12-C waits on B; P12-D/E wait on B and C; P12-F waits on C/D/E; P12-G waits on B through F plus validated live-profile inventory; P13 remains blocked. This revision does not claim complete Crime/Justice coverage, complete owner/operation/epoch coverage, capture eligibility, export, hydration, P12-A/P13 readiness, or Phase closure.

## P12-B selected-profile Crime/Justice invalidation promotion — 2026-10-05

After refreshed preflight, `codex/phase12/canonical` advanced by clean fast-forward from `39e275f39e1602d3fa109d3a1bb9acd60585a3f2` through reviewed candidate tip `37371317d7c8a27eb795a10fb78676a4a6946173`. The code-bearing implementation is `92be026fea6515094be7140230194ba626eb090a`, tree `e3f844fe67e38df7c96f3db88d792c176c1b9dfd`. Independent exact-tip review returned `VALIDATED_CANDIDATE` for corrected candidate tip `f74fbc9f2fbaf61b82b74a88c82cf9d50c481b0c`; its durable review record is `docs/design/PHASE12_P12B_CRIME_JUSTICE_INVALIDATION_IMPLEMENTATION_REVIEW.md`, committed in the promotion bundle. The review-record and subsequent promotion bundle contain documentation only; the executable code tree is unchanged.

Exact-tree validation on the code-bearing implementation passed the seven focused suites (Crime/Justice invalidation 12/12, boundary owner 27/27, runtime guard 4/4, runtime orchestration 12/12, bootstrap composition 21/21, population lifecycle 9/9, CrimeSocial appraisal integration 12/12), ALL EditMode 2356/2356, official Smoke 5/5, and `git diff --check`. The retained archive `docs/validation/P12BCrimeJustice-validation-20261005.zip` has SHA-256 `3301FE714E6C22482824FCD0779DC60538F75BFBC5B416A4C15FB247288B822A`.

The bounded selected-profile slice covers legacy daily Crime/Justice calls and same-day actor/directive paths for NPC status/hidden and Justice facts, with sequential per-call reservations in the daily path, immediate per-leaf action notifications, exact mutable-row bindings, and rejection of P18 receipt-backed commits under `UnityBootstrap-Daily-v1`. It does not cover the separate CrimeSocialAppraisal stores or establish full Crime/Justice, owner, operation, or shared-epoch coverage. P12-B remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked; Phase 12 remains open. No global quiescence, capture eligibility, export, hydration, or downstream readiness is claimed.

The refreshed next P12-B source target is the already-composed `CrimeSocialAppraisalWorldState` owner family, to be design-bounded from current profile evidence. The accepted daily profile has an empty PersonStore and legacy NPCs without `PersonId`; theft outcome, CrimeKnowledge, and SocialReaction mutations require registered Person endpoints, and the ordinary theft path checks Person identity before money transfer. The next audit must confirm the exact store identity/cardinality, current empty baseline, all post-bind mutation entry points, and whether the existing profile can only support explicit-empty admission/fail-closed writes. This is not yet a readiness or implementation claim.
## CrimeSocialAppraisal live-writer correction — 2026-10-05

A follow-up source audit found that `SimulationRuntime.TryRegisterPerson` supports a live PersonStore registration through the existing P12 membership path. Thus the accepted profile's initial empty PersonStore and PersonId-free authored NPCs do not make the three composed CrimeSocialAppraisal stores immutable-empty after publication. The previously recorded “verify explicit-empty only” option is superseded; do not fence these owners permanently empty or assume PersonStore cardinality stays zero. The bounded design now targets dynamic row census and mutation invalidation for the exact singleton TheftOutcome, CrimeKnowledge, and SocialReaction stores, including the existing multi-store compensation paths. The design is `docs/design/PHASE12_P12B_CRIME_SOCIAL_APPRAISAL_INVALIDATION_DESIGN.md`, reviewed at exact design tip `7770119d7ae4dd69186ff6394946168db1473527` on P12 source base `a00cba49f642c9b3203df838f9ba27d675f560b2`; independent design review PASS is recorded in `docs/design/PHASE12_P12B_CRIME_SOCIAL_APPRAISAL_INVALIDATION_DESIGN_REVIEW.md`. Under the previously accepted P12-B capability authorization, implementation may proceed within that bounded design. No P12-B completion, P12-A readiness, or downstream readiness follows.

## P12-B selected-profile Crime/Social Appraisal invalidation promotion — 2026-10-05

Remote `codex/phase12/canonical` advanced by the already authorized clean fast-forward from `a00cba49f642c9b3203df838f9ba27d675f560b2` to `1a073df83050da9bed5f7e3e48a27b9952cf62eb`. The promoted code is `ec956c7247c39f8984f7c5a6bca3d813091a0047`, tree `61956354146f3437396d93d91c4df246da8df89d`. The exact-tip independent implementation review PASS is `docs/design/PHASE12_P12B_CRIME_SOCIAL_APPRAISAL_INVALIDATION_IMPLEMENTATION_REVIEW_R1.md`; candidate and validation evidence are recorded in `docs/design/PHASE12_P12B_CRIME_SOCIAL_APPRAISAL_INVALIDATION_CANDIDATE.md`. The corrected source tree did not change after review or validation; promotion added only documentation and retained evidence.

For the selected `UnityBootstrap-Daily-v1` profile, the bounded adapter registers the exact singleton TheftOutcome, CrimeKnowledge, and SocialReaction owners, tracks local cardinality/revision, and connects the reviewed direct and composite mutation paths to P12's shared epoch. Nested Knowledge/Appraisal is included in TheftAcceptance. The correction preserves a committed theft result when the post-commit epoch notification faults, so CrimeSystem does not reverse money while leaving outcome/knowledge/reaction rows committed.

Exact-tree evidence for `61956354146f3437396d93d91c4df246da8df89d`: focused `P12CrimeSocialAppraisalInvalidationTests` 11/11; ALL EditMode 2367/2367; official Smoke 5/5; `git diff --check` PASS. The retained validation archive SHA-256 is `7c13183315486d6753879b36002f42ffa962fea15e6c651ccb94f0e8f35e253f`; exact XML/log hashes are in the candidate manifest and R1 review.

This promotes only those three Crime/Social owners and reviewed writes. It adds no gameplay behavior and does not establish complete P12-B owner/operation/shared-epoch coverage, global quiescence, capture eligibility, export, hydration, P12-A readiness, P13 readiness, or Phase closure. P12-B remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked; Phase 12 remains open.

### Current dependency state

P12-B → P12-C → P12-D/P12-E → P12-F; P12-G still requires B through F plus a validated live-profile inventory; P12-A remains blocked until complete included-owner export and staged hydration, inventory validation, and its separate implementation authorization. P13 remains blocked on the required P12 continuation and recoverable causal history. This promotion changes no edge. The approved P10 Ruin and P14 City bootstrap profiles remain separate under P8's one-top-level-owner-per-Location invariant.

The current architecture baseline remains `ffd75652d89d862b83d634868c560f8540869b89`; the intraday/extensibility and multi-participant alignment records remain `4b6dd1d38cffeaf3cc1ac3effea0f8ede8771194` and `c285466c355103d3637ac165246591b72eb7bda0` respectively.

### Promotion-policy record

The current `docs/EXECUTION_MODEL.md` still contains legacy wording that routine canonical promotion requires explicit human approval. The user's standing authorization for this run permits bounded numbered-phase promotions after all documented review, validation, ancestry, and exact-tree checks pass. This State follows that authorization; it makes no architecture-policy change.

## P12-B selected-profile TravelParty start operation promotion — 2026-10-05

After refreshed exact-tip preflight, `codex/phase12/canonical` advanced by
clean fast-forward from `29162cd0cf63e31f9542e612023a7256ac36ca4c` to
`9395c43e8ad8ae516a2fba554dc8e48d0b5895a5`. The bounded implementation is
code-bearing commit `2bc6d3264c76347eed21b70dcfcde98533f7aa66`, tree
`9043a8da8718a364f602eee56aaf48f83f06ad51`. The State update is
documentation-only and records no additional implementation scope.

Independent exact-tip implementation review passed with no remaining
actionable findings. The review and candidate evidence are
[PHASE12_P12B_TRAVEL_PARTY_START_OPERATION_IMPLEMENTATION_REVIEW.md](design/PHASE12_P12B_TRAVEL_PARTY_START_OPERATION_IMPLEMENTATION_REVIEW.md)
and
[PHASE12_P12B_TRAVEL_PARTY_START_OPERATION_CANDIDATE.md](design/PHASE12_P12B_TRAVEL_PARTY_START_OPERATION_CANDIDATE.md).
Exact-tree validation passed P12TravelPartyStartOperationTests 16/16,
ContinuationCensusProtocolTests 22/22, NpcOwnerCommitInvalidationTests 13/13,
ALL EditMode 2383/2383, official Smoke 5/5, and `git diff --check`. The
compact retained archive is
`docs/validation/P12BTravelPartyStart-notify-fix/P12BTravelPartyStart-final-validation.zip`,
SHA-256 `F3EF32E29C32B335684F130D60DFD3DC1B1352122B2342008A56EEE39CDF6A65`.
The earlier pre-fix 15/16 result remains diagnostic only.

The selected-profile normal group-start path now opens
`runtime.travel-party.start` after preparation and before TravelParty ID
allocation; it validates the exact TravelParty allocator witness and owner
revision headroom, batches successful committed-owner notifications into one
epoch, and closes the operation after a post-commit notification fault.
P12-bound direct unwrapped starts still reject before domain writes; unbound
runtime behavior and existing transaction/event/compensation semantics are
preserved.

This promotion covers only that normal group-start operation. Direct
Expedition start/return, every TravelParty API, arbitrary standalone writers,
complete P12-B owner/operation/shared-epoch coverage, global quiescence,
capture eligibility, export, hydration, P12-A readiness, P13 readiness, and
Phase 12 closure remain unclaimed. P12-B remains INCOMPLETE; P12-A remains
WAIT_DEPENDENCY; P13 remains BLOCKED.

The dependency DAG is unchanged. The current P12-B blocker remains the
source-driven Daily-v1 owner/operation/epoch delta audit across effective
writers. The promoted group-start path is now closed within its exact
boundary; the audit must continue over remaining supported paths and may
select another implementation slice only when it identifies the exact owner
set, cardinality/identity, effective ingress, committed-write boundary, and
epoch behavior. Expedition work remains deferred to P12-F.

## Daily-v1 profile separation — 2026-10-06

The user resolved the P10-A profile mismatch in favor of preserving the
accepted P12 scope. `SampleScene.unity` now selects the dedicated
`Simulation-DailyV1.asset`, which contains the authored P9-B geography and the
same selected City/NPC inputs but no P10-A Ruin. `Simulation-GeneralTest.asset`
is unchanged and remains the separate P10-A Ruin/LocalTopology proving
profile. A Daily-v1 bootstrap now rejects any P10-A authored Ruin before
WorldId allocation, runtime-owner construction, or genesis publication.

The refreshed selected-profile census runs under
`UnityBootstrap-Daily-v1` and confirms the profile identity and P9-B
fingerprint match, P10-A's stage is absent, and the exact runtime-identity
cardinalities are 10 NPCs, 2 Cities, 2 Locations, 2 Routes, and zero
ExplorableSites, LocalPlaces, LocalConnections, and NotableItems. The
SpatialAuthority contains one P8-A Hex, one anchored Location, and one scale
context. The full inventory test verifies each registered provider against
its exact owner instance, section, schema, cardinality, and revision. See
`docs/validation/P12DailyProfileSeparation/VALIDATION.md` for the focused and
full Unity evidence.

The correction is prepared as P12 candidate
`75ca59af90d54f3fb307382740ce0ba3fa4e00fa`, based on canonical
`b9fcb54840ae7c4e68d5f4d1812e5591bb36a948`, with code tree
`e3fbbd53689a4dd582e086e8c1677935d21ef78f`. Exact-tree validation passed the
selected-profile inventory test 1/1, ALL EditMode 2384/2384, official Smoke
5/5, and `git diff --check`; artifacts and hashes are recorded in the linked
validation manifest. Independent exact-tip review passed for the code commit
and tree above; the durable review record is
`docs/validation/P12DailyProfileSeparation/REVIEW.md`. The code candidate is
validated; at the time of this entry, it awaited canonical promotion preflight.

This resolves only the configuration/profile mismatch and validates the
current day-zero owner/cardinality inventory. It does not close the
source-driven P12-B owner/operation/shared-epoch audit, establish complete
owner or epoch coverage, or provide export/hydration. P12-B remains
INCOMPLETE; P12-A remains WAIT_DEPENDENCY; P13 remains BLOCKED. P12-F
Expedition remains deferred until P12-C/D/E.

## P12 Daily-v1 profile separation promotion — 2026-10-06

The validated profile-correction candidate was fast-forwarded to
`codex/phase12/canonical` at `eb1f8041d291376744c6bbaf3a9aa235addd3830`,
from canonical `b9fcb54840ae7c4e68d5f4d1812e5591bb36a948`. The reviewed code
remains `75ca59af90d54f3fb307382740ce0ba3fa4e00fa`, tree
`e3fbbd53689a4dd582e086e8c1677935d21ef78f`. Exact-tip independent review
passed and is recorded in
`docs/validation/P12DailyProfileSeparation/REVIEW.md`; exact-tree validation
and hashes are in `VALIDATION.md`, with raw test results in its hash-pinned
archive. Local and remote `codex/phase12/canonical` are synchronized at the
promotion SHA.

This promotion makes SampleScene's P9-B-only Daily-v1 profile selection and
pre-publication rejection of P10-A/P10-B Ruin configurations canonical.
`Simulation-GeneralTest.asset` remains the independent P10-A
Ruin/LocalTopology proving profile. The correction does not exclude Ruins from
future deliberately composed continuation profiles.

The profile mismatch and selected day-zero identity/cardinality question are
resolved. P12-B remains INCOMPLETE while source-driven owner, committed-write,
and shared-epoch coverage remains outstanding. P12-A remains
WAIT_DEPENDENCY; P13 remains BLOCKED. No complete owner or epoch coverage,
global quiescence, capture eligibility, export, or hydration is implied.
Continue the P12-B current-profile owner/operation/epoch audit from this
corrected canonical state.

## P12 live-profile inventory correction — P10-A authored Ruin — 2026-10-05

The source audit
[PHASE12_DAILY_V1_P10A_PROFILE_RECONCILIATION_AUDIT.md](design/PHASE12_DAILY_V1_P10A_PROFILE_RECONCILIATION_AUDIT.md)
found a mismatch between the accepted P12 profile description and the actual
SampleScene configuration. The scene selects
`UnityBootstrapDailyV1` and the GeneralTest asset; that asset enables the
P10-A authored Ruin/LocalTopology profile. `SimulationConfigData` resolves
its P10-A profile identity, and the bootstrap stage composes LocalTopology.
The P10-B-only Daily admission rejection does not reject this P10-A
composition. The selected-profile composition test does not set the Daily-v1
runtime-admission context, so its P10 cardinalities do not prove the P12
profile boundary.

This supersedes any implication that the actual SampleScene Daily-v1
composition has P10 LocalTopology absent. It does not change the P12-A
exclusion of P10-A in the Brief or widen P12 scope. P12-B remains
INCOMPLETE; P12-A remains WAIT_DEPENDENCY; P13 remains BLOCKED. The P12
TravelParty-start promotion remains valid within its exact reviewed owner set.

## P12-B current-action owner/invalidation promotion — 2026-10-06

P12 canonical advanced by clean fast-forward from
`90a8a2aef212bec2c68d7224739ce512d44c9fe3` to
`1b7ae19a9400fb34cb44e763191531fc72d79d79`. The promoted code commit is
`f4a235c17d65841585ae9f6c56f65ca8eb310ca6`, tree
`92a59220bdefc8e17e7fea6490f87f15646b9d6f`; exact-tip independent review and
validation evidence are retained in
`docs/design/PHASE12_P12B_CURRENT_ACTION_INVALIDATION_REVIEW.md` and
`docs/design/PHASE12_P12B_CURRENT_ACTION_INVALIDATION_CANDIDATE.md`. Local and
remote `codex/phase12/canonical` were synchronized at the promoted review
record tip before this State update.

The slice adds a per-rostered-NPC current-action section (cardinality 0/1,
exact owner identity and non-serialized local revision), admitted installed
action writes, shared-epoch notifications for covered successful changes, and
action clearing inside the covered Person/population/NPC lifecycle paths. The
exact P9-B-only `UnityBootstrap-Daily-v1` profile was revalidated: runtime
admission/owner inventory 37/37 and bootstrap composition/profile separation
22/22. Focused current-action/lifecycle/ActorChoice 20/20, Crime/Justice 12/12,
solo travel 11/11, ALL EditMode 2395/2395, Official Smoke 5/5, and
`git diff --check` passed on the reviewed code tree. Raw logs and hashes are
retained in the candidate evidence and its validation archive.

This is a bounded owner/invalidation slice only. It does not complete the
Daily-v1 owner census, all supported committed-write/shared-epoch coverage,
global owner-thread quiescence, capture eligibility, export, or hydration.
P12-B remains INCOMPLETE; P12-A remains WAIT_DEPENDENCY; P13 remains BLOCKED.
P10-A Ruin/LocalTopology remains a separate proving profile and is excluded
from Daily-v1. P12-F Expedition remains deferred to P12-C/D/E.

The architecture reference for this refresh is
`codex/architecture/world-identity-projection` at
`e16796014d348e3b59da7ed848101c4c03926ba5`; the intraday/extensibility and
multi-participant activity alignment records remain
`4b6dd1d38cffeaf3cc1ac3effea0f8ede8771194` and
`c285466c355103d3637ac165246591b72eb7bda0`, respectively. The current
Daily-v1 profile correction and its 37/37 admission and 22/22 composition
evidence supersede the historical P10-A mismatch section above without
changing the accepted P12 scope.

## P12-B Daily-v1 exact-zero receipt owner admission promotion — 2026-10-06

P12 canonical advanced by a clean fast-forward from `ea4decdaffa26e80e76ee72135ead0a7d673f358` to the reviewed candidate `88f71f758673111e1ea67b88947485dd9c5d57b5`; local and remote `codex/phase12/canonical` are synchronized at that promotion tip. The promoted implementation is code commit `c8f1689d195218351cca0b45ef431883860b3d7d`, tree `078ca8a17ea095895c897638178262ca8bb932b6`. Independent exact-tip review is recorded in `docs/design/PHASE12_P12B_DAILY_EXACT_ZERO_RECEIPT_IMPLEMENTATION_REVIEW.md`; the corrected technical design and its independent PASS are recorded in `docs/design/PHASE12_P12B_DAILY_EXACT_ZERO_RECEIPTS_DESIGN_REVIEW.md`.

The selected `UnityBootstrap-Daily-v1` census now binds the existing `NpcDecisionRecorder.OccurrenceReceiptSectionId` and `EconomyTransactionService.KeyedSaleReceiptSectionId` providers as fixed `ExplicitlyEmpty` sections. On the exact base, the actual selected profile had 233 registered sections; adding these two sections yields the verified 235-section composition. The full-profile requirement applies to the real Daily-v1 bootstrap, and missing/malformed/populated providers or later unnotified cardinality/revision changes fail closed through the existing census admission and assessment behavior. The P10-A Ruin remains in its separate `Simulation-GeneralTest.asset` proving profile.

The exact-base Daily-v1 baseline count probe passed 1/1. Profile/configuration and owner-thread admission revalidation each passed 1/1 on source base `ea4dec`. Candidate receipt admission passed 50/50; selected profile/cardinality inventory passed 1/1; ALL EditMode passed 2408/2408; official EditMode Smoke passed 5/5; `git diff --check` passed. XML hashes, raw-log archive hashes, probe source, and baseline evidence are retained under `docs/validation/P12DailyProfileRevalidation/final/` and `docs/validation/P12ExactZeroReceiptBaseline/`.

This promotes only exact-zero receipt-owner admission for the selected partial census. It does not enable P18-D receipt writers or establish complete owner coverage, complete shared-epoch coverage, global quiescence, capture eligibility, export, hydration, P12-A readiness, P13 readiness, or Phase 12 closure. P12-B remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked. The dependency edges are unchanged: P12-C waits on B; P12-D/E wait on B and C; P12-F waits on C/D/E; P12-G waits on B–F plus validated live-profile inventory. Expedition remains deferred to P12-F.

The blocker matrix in `docs/design/PHASE12_B_BLOCKER_RESOLUTION.md` now records this owner-family promotion. Continue the current-source Daily-v1 owner/operation/epoch delta audit and choose the highest-impact remaining supported committed-write gap; this promotion does not itself make another checkpoint READY.

## P12-B recovery and current-source audit — 2026-10-06

At the start of this audit, refreshed remote `codex/phase12/canonical` was
`bba9f89b6a95c01ec71db47d498c65dddbe9d99f`. The dedicated Daily-v1 profile
correction at code `75ca59af90d54f3fb307382740ce0ba3fa4e00fa`, tree
`e3fbbd53689a4dd582e086e8c1677935d21ef78f`, and its promotion record remain
in canonical history. The later exact-zero receipt implementation at code
`c8f1689d195218351cca0b45ef431883860b3d7d`, tree
`078ca8a17ea095895c897638178262ca8bb932b6`, is the current executable tree;
subsequent canonical commits add review, validation, and State evidence only.

The requested profile revalidation is retained on these exact trees. The
profile-separation validation records the selected Daily-v1 owner/cardinality
inventory 1/1, ALL EditMode 2384/2384, and official Smoke 5/5 on the profile
correction. The later receipt candidate reran the selected profile inventory
1/1 on the updated tree and verified all 235 registered sections; its ALL
EditMode result is 2408/2408 and official Smoke is 5/5. The live profile is
the dedicated P9-B `Simulation-DailyV1.asset` with 10 NPCs, 2 Cities, 2
Locations, 2 Routes, one P8-A Hex, one anchored Location, and one scale
context; ExplorableSite, LocalPlace, LocalConnection, and NotableItem
cardinalities are zero. `Simulation-GeneralTest.asset` remains the separate
P10-A Ruin/LocalTopology proving profile. No executable change after the
reviewed receipt tree requires another rerun for this profile question.

A current-source Daily-v1 delta review after the receipt promotion found no
additional normal supported committed-write path with a source-proven owner
gap suitable for a new bounded implementation slice. The traced
`TravelSystem.AdvanceTravels` leaves already notify the exact per-NPC travel
owner and applicable City presence; theft remains its existing sequence of
independently admitted economy, Crime/Social, and Justice commits, with no
evidence for a theft-wide operation; `NpcDecisionStore` is a read model while
`SimulationRecordSequence` is the causal owner. No P12-F Expedition operation
is pulled forward.

This is a negative delta finding, not a completion proof. The 235-section
test checks the section set currently registered by the selected runtime; it
does not prove that the set exhausts every authoritative owner in the
effective object graph. Complete owner-set reconciliation, full supported
commit-to-epoch coverage, runtime-wide owner-thread/quiescence proof, and
capture eligibility remain open. P12-B remains `INCOMPLETE`; P12-A remains
`WAIT_DEPENDENCY`; P13 remains blocked; P12-F remains dependency-gated on
P12-C/D/E.

The next P12-B evidence task is to reconcile each owner in the current
Daily-v1 runtime graph against its exact registered section family, identity,
schema, cardinality/revision source, and supported mutation operations. A new
implementation slice is justified only by a concrete unmatched owner or
normal supported commit boundary from that reconciliation. This preserves the
accepted profile scope and avoids repeating already-promoted census and
invalidation work.

The first source crosswalk follow-up is recorded in
`docs/design/PHASE12_B_BLOCKER_RESOLUTION.md`: the sealed 235-section protocol
inventory omits ten separate bootstrap-published RuntimeIdentityRegistry and
SpatialNetwork witness sections, and public post-genesis site/location
registration APIs need supported-profile reachability classification. This
is an owner reconciliation obligation, not a P12-B readiness or implementation
claim. Daily-v1 remains P9-B-only; P10-A stays in its separate proving profile.

## P12-B Institution/Office mutation invalidation promotion - 2026-10-06

After refreshed exact-tip preflight, codex/phase12/canonical advanced by
fast-forward from 0daa72addc1f186d23713adf75f6c2f83a5aff9b to
d9aee58065e5bab710bc973617221d45b036e16b. The implementation code remains
13a4ff503d336d34ed008f27571cce82c019dfe4, tree
85ad013074b727ffe8727c2d90b079a45e0ca5c0. The candidate final tree is
0e8030f11a18acc424f19437d2bb7ad43bf1529d; its docs/evidence commits do not
change the reviewed code tree. The exact-tip independent implementation
review is durably recorded at a85aea5980a578d874227816bc967baeac1eb598 on
codex/phase12/P12BInstitutionOfficeEpochImplementationReview.

The selected P9-B-only UnityBootstrap-Daily-v1 partial protocol now contains
239 registered sections, including the four existing P12-E Institution and
Office census sections. The admitted p12.institution-office.owner-commit
scope covers Institution registration, Office registration, both incumbent
assignment overloads, explicit vacancy, and institutional vacancy
recognition. Institution commits notify their one section; Office commits
notify records, incumbencies, and tenures together because they share one
OfficeStore revision. The operation tracker returns to zero after successful
and rejected calls, with selected-profile off-owner-thread rejection before
mutation. The P10-A Ruin remains separate from Daily-v1.

Exact-tree validation on code 13a4ff5 passed focused
InstitutionOfficeCensusTests 4/4, ALL EditMode 2410/2410, official Smoke
5/5, and git diff --check. XML and raw-log archive hashes are recorded in
docs/validation/P12BInstitutionOfficeEpoch/VALIDATION.md; the independent
review checked those artifacts against the exact code tree.

This is a bounded Institution/Office invalidation slice. It does not establish
complete owner coverage, complete shared-epoch coverage, global quiescence,
capture eligibility, export, hydration, P12-A readiness, P13 readiness, P12-B
completion, or Phase 12 closure. P12-B remains INCOMPLETE; P12-A remains
WAIT_DEPENDENCY; P13 remains blocked. The refreshed owner/operation matrix
records the next source-supported gap in the existing Property/Estate
authorities; it remains design/audit work until its bounded operation
contract is independently reviewed.

## P12-B Property/Estate mutation-epoch promotion — 2026-10-06

After exact-tip revalidation and final preflight, codex/phase12/canonical advanced by a clean fast-forward from 82735cb0ac7878fda0efd7d9e6a3029fe8a501f7 to 0c05223079a2036ba3b87ea19194efd1be25e7c9. The promoted implementation code is 167a488c09fc7a2dc51517e1886250c43303bc20, tree 7527439309841a8f68302e7c7630ddcecbc36b21. Its exact-tip independent implementation review, refreshed against Architecture e16796014d348e3b59da7ed848101c4c03926ba5, is durably recorded at ad097b1b1f088ffaf17272669ab709a12e63c6f9 on codex/phase12/P12BPropertyEstateEpochImplementationReview. The technical design is 19d2e6c92ccd224b8289d2095dc33103e2858287, with independent design review PASS eb0270ae189684198d31ade91a6e8ebb53909293.

The selected P9-B-only UnityBootstrap-Daily-v1 partial census now verifies 242 sections, up from 239. Three Required schema-v1 sections bind to the exact installed PropertyOwnershipStore and EstateStore: Property ownership, Property transfer history, and Estate records. Initial Property/Estate cardinalities must be zero. The bounded p12.property.owner-commit and p12.estate.owner-commit operations cover existing runtime property registration and transfer, succession's nested property transfer, and explicit estate opening. They preflight owner thread, section baselines, and epoch capacity before the existing domain commit, then notify the affected sections after success. Existing domain rejection behavior is preserved; estate RuntimeFaulted is appended as result code 17 without renumbering prior codes.

Architecture §92A revalidation added two negative admission proofs. A selected Daily-v1 composition with prepopulated Property ownership plus transfer history, and a separate composition with a pre-existing Estate, each fail runtime admission before a SimulationRuntime is returned for bootstrap publication; source stores retain their rows. This leaves the separate P10-A Simulation-GeneralTest.asset Ruin/LocalTopology proving profile unchanged.

Exact-tree validation on code 167a488 passed five focused suites, 51/51 total (PropertyEstateMutationEpochTests 5/5, PropertyOwnershipCensusTests 2/2, EstateCensusTests 1/1, SuccessionIntegrationTests 21/21, SimulationBootstrapCompositionTests 22/22); ALL EditMode passed 2415/2415; official Smoke passed 5/5; git diff --check passed. XML, logs, source hashes, and the raw-log archive hashes are retained in docs/validation/P12BPropertyEstateEpoch/VALIDATION.md and the review record.

This promotion closes only the listed Property/Estate owner and runtime mutation paths. It does not establish complete owner coverage, complete shared-epoch coverage, global quiescence, capture eligibility, export, hydration, P12-A readiness, P12-B completion, P13 readiness, or Phase 12 closure. P12-B remains INCOMPLETE; P12-A remains WAIT_DEPENDENCY; P13 remains blocked. P12-C waits on B; P12-D/E wait on B and C; P12-F waits on C/D/E; P12-G waits on B-F plus validated live-profile inventory. Daily-v1 remains P9-B-only and P10-A remains a separate proving profile.

The refreshed highest-value P12-B obligation is the current-source owner/protocol crosswalk: reconcile the eight bootstrap-published RuntimeIdentityRegistry witness sections and two SpatialNetworkRuntime witness sections against the now-242-section sealed inventory, then classify supported post-publication reachability of SpatialNetwork.RegisterLocation/RegisterRoute and ExplorableSites.Add. Public visibility alone does not establish that those writes are part of the selected Daily-v1 gameplay contract. No new implementation checkpoint or readiness follows until source evidence identifies the exact supported boundary.

## P12-B City roster read-only view promotion — 2026-10-06

After final preflight, `codex/phase12/canonical` advanced by fast-forward from
`69a5a41ca879fb66ce62efbe0d62e31f7298675b` to
`b0ab1ae2d8dc9205e58c33a8a8c7658de27bbec1`. The implementation code is
`256c443903fd3e33ed05cfe4d86a11b3467176a2`, tree
`42ea8bfda3d95a57965192f550c58be226bf0004`; its independent exact-tip review
PASS is recorded in
`docs/design/PHASE12_P12B_CITY_ROSTER_READ_ONLY_VIEW_IMPLEMENTATION_REVIEW.md`.
The reviewed design and independent design PASS are retained at
`24522619b4418b9f506f9a59aec21eb188848560` and
`a39039eecdc95e4ea9e2160d72dd57df1ab3b3c0`, respectively.

`SimulationRuntime.Cities` now returns one retained `AsReadOnly()` wrapper over
the runtime's private constructor copy, preserving its sorted order and exact
City objects while preventing an `IList<CityRuntime>` cast from adding,
replacing, or clearing roster entries. The selected Daily-v1 test asserts the
two City owners, rejected mutation attempts, unchanged roster identity/order,
and unchanged RuntimeIdentity City owner/cardinality/revision. Exact-tree
validation passed: `SimulationBootstrapCompositionTests` 24/24 (including the
253-section Daily-v1 inventory), `SimulationRuntimeAdmissionTests` 50/50,
ALL EditMode 2417/2417, official Smoke 5/5, and `git diff --check`. Artifact
hashes and the Unity-output/Git-blob distinction are documented in
`docs/validation/P12BCityRosterReadOnlyView/VALIDATION.md`.

This closes only the accidental mutable roster alias. It adds no City
creation/removal operation, census section, revision, shared-epoch hook, or
export/hydration behavior. Daily-v1 remains the P9-B-only profile, and the
P10-A Ruin/LocalTopology proving profile remains separate. P12-B remains
`INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked. No complete
owner coverage, complete shared-epoch coverage, global quiescence, capture
eligibility, P12-A/P13 readiness, or Phase 12 closure is claimed.

### Refreshed P12 dependency DAG and next blocker

No numbered-phase dependency edge changes: P12-C waits on B; P12-D and P12-E
wait on B and C; P12-F waits on C/D/E; P12-G waits on B through F plus a
validated live-profile inventory; P13 remains blocked on supported P12
continuation and recoverable causal history. Expedition remains deferred to
P12-F. The next P12-B work is the source-driven reconciliation of remaining
supported commits against their exact registered owner sections and shared
epoch, alongside runtime-wide owner-thread/quiescence evidence. The City-list
fix does not establish implementation readiness for another owner family.

## P12-B FactionStore owner mutation promotion — 2026-10-06

After refreshed preflight, `codex/phase12/canonical` advanced by fast-forward
from `54e95a325812baa8db2fe233d3dd566be93f7fa2` to
`0290aa30b202ed50e67d037ef1db3319403ff982`. The promoted code commit is
`8bb6c61e8bd636b4b99a85d7bc96ad379f10c04d`, tree
`286743c7ac4c45fc0cea812749ce5d1e14e0151b`; the promoted tip tree is
`42b557e7b5bbb45c1f914b5a0a89c7c2992ba995`. Independent exact-tip review
PASS is recorded in
[`design/PHASE12_P12B_FACTION_OWNER_MUTATION_IMPLEMENTATION_REVIEW.md`](design/PHASE12_P12B_FACTION_OWNER_MUTATION_IMPLEMENTATION_REVIEW.md).
Validation artifacts and hashes are retained in
[`validation/P12BFactionStoreOwnerMutation/VALIDATION.md`](validation/P12BFactionStoreOwnerMutation/VALIDATION.md).

The selected P9-B-only Daily-v1 protocol adds two Required sections, moving
the tested partial inventory from 253 to 255. They bind separate faction and
affiliation row counts to the exact installed `FactionStore` and its shared
local revision. The bounded `p12.faction.owner-commit` wrapper covers only
`TryRegisterFaction`, `TryApplyFactionAffiliation`, and
`TryApplyFactionAffiliationEnd`; successful existing commits refresh both
sections in one mutation epoch. Existing rejection semantics are retained.

Exact-tree validation passed the five focused suites (100/100 total), ALL
EditMode 2422/2422, official Smoke 5/5, and `git diff --check`. The seven XML
files and archived logs were verified by the independent reviewer against the
manifest and exact code tree. The code/tree, review, validation, and remote
canonical promotion were all confirmed synchronized.

This remains one bounded owner and facade-write slice. P12-B remains
`INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked. Do not
infer complete owner/writer coverage, complete shared-epoch coverage, global
quiescence, capture eligibility, export, hydration, P12-A/P13 readiness, or
Phase 12 closure. Daily-v1 remains P9-B-only, and the P10-A proving profile
remains separate.
## Architecture revalidation after development-artifact authority update — 2026-10-06

The latest Architecture General line available is
`origin/codex/architecture/world-identity-projection` at
`e16796014d348e3b59da7ed848101c4c03926ba5`. Its documentation update
consolidates §2's rule that Unity Scenes and general configuration assets do
not silently redefine an approved profile, and §92A's fail-closed entry rule
for a genuinely new owner. It does not invalidate the already approved and
promoted P9-B-only Daily-v1 profile: the profile contract remains explicit and
its live composed owners are checked. The FactionStore slice added census and
invalidation for an owner already instantiated inside `SimulationRuntime`; it
did not add a domain owner or change the profile. This revalidation does not
merge or promote the separate Architecture branch and changes no Phase scope.
## P12-B PoliticalClaimStore owner mutation promotion — 2026-10-06

Under the standing `AUTONOMOUS_BOUNDED_PROMOTION` policy, the refreshed
`codex/phase12/canonical` branch was fast-forwarded from
`4d015062c28061148eb9926a6d23799681531afe` to the exact-reviewed integration
bundle `3a0191057bcc600101b55199cbbf9f905610fc5d`. The promoted implementation
code is `e3ae99b1756227f3af0d8d379f9a0f7778f854e5`, tree
`2cca4e2d91e18eed50bf08a110db3016ea7a7adf`. Independent exact-tip review PASS
is recorded in
[`design/PHASE12_P12B_POLITICAL_CLAIM_OWNER_MUTATION_IMPLEMENTATION_REVIEW.md`](design/PHASE12_P12B_POLITICAL_CLAIM_OWNER_MUTATION_IMPLEMENTATION_REVIEW.md);
the exact validation artifacts and hashes are in
[`validation/P12BPoliticalClaimOwnerMutation/VALIDATION.md`](validation/P12BPoliticalClaimOwnerMutation/VALIDATION.md).

This slice adds two Required selected Daily-v1 census sections for separate
PoliticalClaim and recognition cardinalities on the exact installed
`PoliticalClaimStore`, both using its shared local revision. The validated
inventory now contains 257 sections. One `p12.political-claim.owner-commit`
operation covers only the existing `TryRegisterPoliticalClaim`,
`TryApplyPoliticalClaimRecognition`, and `TryApplyPoliticalClaimResolution`
runtime facades; successful commits notify both sections once in one mutation
epoch and retain the existing PoliticalWorldRevision increment. The review
and focused tests cover recognition replacement without cardinality change,
claim resolution, rejected writes, and local revision overflow.

P12-B remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P13 remains
blocked; Phase 12 remains open. This does not establish complete P12-B owner
or writer coverage, complete shared-epoch coverage, global quiescence, capture
eligibility, export, hydration, or P12-A/P13 readiness. Daily-v1 remains the
P9-B-only profile, and the separate P10-A Ruin/LocalTopology proving profile
is unchanged.

The numbered-phase DAG is unchanged: P12-C waits on B; P12-D and P12-E wait
on B and C; P12-F waits on C/D/E; P12-G waits on B through F plus validated
live-profile inventory; P13 remains blocked on supported continuation and
recoverable causal history. The separate PoliticalSupportStore owner and its
existing runtime write paths remain outside this promotion and require their
own source/contract reconciliation before further P12-B wiring.

## P12-B PoliticalSupportStore owner mutation promotion — 2026-10-07

Under the standing bounded-promotion authorization, `codex/phase12/canonical`
advanced by fast-forward from `2760fe199909708f22ea61d3eb2dd929b542521b` to
`a145c60b8b81bcff23ae86142b6192e4718ac87d`. The promoted implementation
code is `3b0e73824e600b5754cfd32d5bcf1ec37d78142c`, tree
`d4c3d901f4cd4a7b23da11edbe5c8a05f215136a`. The exact candidate evidence tip
is `6f09476793d3736579c1e7484d531860fde69c8a`; the independent exact-tip
implementation review record is
[`PHASE12_P12B_POLITICAL_SUPPORT_OWNER_MUTATION_IMPLEMENTATION_REVIEW.md`](design/PHASE12_P12B_POLITICAL_SUPPORT_OWNER_MUTATION_IMPLEMENTATION_REVIEW.md),
published with the promotion record at `a145c60`. The candidate's reviewed
code tree and validation artifacts were unchanged at final preflight.

The selected Daily-v1 inventory now contains 258-section partial inventory. This
slice adds the exact installed PoliticalSupport relation-row count/local
revision section and a bounded `p12.political-support.owner-commit` operation
over the existing runtime registration, add, and end-transition commit
facades. Successful commits publish the relevant revision once; existing
PoliticalWorldRevision and rejection behavior remain intact. The current
bootstrap boundary is exact owner identity with zero rows and revision zero.

Validation recorded for the unchanged reviewed tree: eight focused suites
109/109, ALL EditMode 2434/2434, official Smoke 5/5, and
`git diff --check` PASS. Exact XML/log hashes are retained in
[`P12BPoliticalSupport/VALIDATION.md`](validation/P12BPoliticalSupport/VALIDATION.md).

This closes only that owner census and its three existing facade writes.
P12-B remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P13 remains
blocked; Phase 12 remains open. It does not establish complete owner/writer
or shared-epoch coverage, global quiescence, capture eligibility, export,
hydration, downstream readiness, or Phase closure. Daily-v1 remains the
P9-B-only profile, and P10-A remains a separate proving profile.

The numbered-phase DAG is unchanged: P12-C waits on B; P12-D and P12-E wait
on B and C; P12-F waits on C/D/E; P12-G waits on B through F plus a validated
live-profile inventory; P13 remains dependency-gated. The next blocker task is
a current-canonical, source-linked refresh of the selected-profile
owner/operation/epoch crosswalk. The existing operation-footprint refresh is
based on canonical `70bc1e50` and predates multiple promoted operations,
including the current PoliticalSupport slice; it cannot select the next
implementation by itself. Keep P12-F Expedition work deferred until its
documented P12-C/D/E prerequisites are met.

## P12-B Daily-v1 source crosswalk refresh — 2026-10-06

**Baseline:** `codex/phase12/canonical` at `62e12f998b5264bb915c879150c9d1fb7461bd43`, tree `1d4760c874e4a793e76512578bde7e6219c3f82c`.

The accepted profile correction remains in canonical: `SampleScene.unity` selects the dedicated P9-B-only `Simulation-DailyV1.asset`; `Simulation-GeneralTest.asset` retains P10-A Ruin/LocalTopology. The exact profile-separation inventory evidence at code `75ca59a` remains retained in `docs/validation/P12DailyProfileSeparation/`. The later PoliticalSupport validation at code tree `d4c3d90` reran the selected bootstrap composition and runtime-admission suites against the 258-section partial inventory (focused 109/109, composition 24/24, admission 50/50, ALL EditMode 2434/2434, Smoke 5/5). These results are retained evidence; this source audit did not rerun Unity.

A source-linked crosswalk of the selected Daily-v1 outer day path is recorded in `docs/design/PHASE12_B_BLOCKER_RESOLUTION.md` under the same heading. It found no uncovered successful writer in the supported daily path. The selected-profile public clock dispatcher enters the full runtime day operation, which remains active while the clock and daily core advance; the FR-B read cut rejects while that operation is active. The P12-B design treats `SimulationTime.AbsoluteDay` as boundary identity rather than a post-boundary owner write, so a separate clock mutation-epoch notification is not required. The P12 completed-boundary token, sequence, and publication/invalidation lifecycle remain design obligations and are not implemented at this baseline.

This closes the current Daily-v1 day-path and sealed-operation-to-entry mapping. The 23 currently registered operation IDs each have an explicit scope entry or bound admission callback. This checks registration-to-scope consistency; it does not prove that the operation list exhausts every supported production caller or that every live owner/write is represented. The trusted local UI ActorActionChoice handler is not a current Daily-v1 path: the accepted P12 Brief explicitly rejects an external `WorldCommand` service/queue for this profile, and the reserved handler registration has no production caller in this repository. Do not add a Daily-v1 ActorChoice-capture operation scope for that excluded queue. The remaining work is the complete effective owner/cardinality and successful-commit matrix, followed by runtime-wide owner-thread/quiescence and the undelivered P12 completed-boundary eligibility coordinator. P12-B remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked. No complete owner or shared-epoch coverage, global quiescence, capture eligibility, export, hydration, or downstream readiness is implied. P12-F Expedition work remains deferred until its documented P12-C/D/E prerequisites are met. Independent exact-tip review of the documentation candidate passed; the durable record is [`design/PHASE12_P12B_DAILY_V1_SOURCE_CROSSWALK_REVIEW.md`](design/PHASE12_P12B_DAILY_V1_SOURCE_CROSSWALK_REVIEW.md).

## P12-B P8-D exact-zero census gap — 2026-10-06

Current canonical is `e5405cf897c30224f86ce605a9efe6777f93749a`. The selected
Daily-v1 profile already has passive providers for exact-zero P8-D route
Knowledge and plan-history owners, but they are not registered in its sealed
258-section protocol. Independent source audit confirms that P12 technical
design requires these composed excluded owners to be explicitly empty; the
registered owner inventory therefore remains partial. The two owner facades
have no production Daily-v1 call sites, so no route operation is added. The
bounded next slice is reviewed registration of the installed providers as
`ExplicitlyEmpty`, exact-zero admission, and selected-profile revalidation at
260 sections. Design: [`design/PHASE12_P12B_P8D_EXACT_ZERO_REGISTRATION_DESIGN.md`](design/PHASE12_P12B_P8D_EXACT_ZERO_REGISTRATION_DESIGN.md).

This is a proposal/source finding only: it adds no code, operation, epoch
notification, route behavior, or capture capability. P12-B remains
`INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked. P12-C
waits on B; P12-D/E wait on B and C; P12-F waits on C/D/E; P12-G waits on
B–F plus a validated live-profile inventory. The independent design review
passed at `2a89e34`; its durable record is
[`design/PHASE12_P12B_P8D_EXACT_ZERO_REGISTRATION_DESIGN_REVIEW.md`](design/PHASE12_P12B_P8D_EXACT_ZERO_REGISTRATION_DESIGN_REVIEW.md).

## P12-B Daily-v1 spatial inventory reconciliation and admission design review — 2026-10-06

The older P8-D blocker entry above records a 258-section inventory at
`e5405cf`. It is historical relative to the current canonical code: P12-E
military owner registration is integrated, and the exact current selected
profile test at code tree `c9e763e2ebc2f63d9772701a0c03e35c271392f7`
asserts 268 sections, including P8-D exact-zero owners and all eight P12-E
sections. The executable Assets tree is unchanged from code-bearing tip
`5b055be864afa0ace56d56381eb00fe4e993ed86` and current P12 canonical
`d8e6c9919d9359003dfd370fbd38a47424256b26`.

The accepted profile split remains in force: SampleScene selects the dedicated
P9-B-only `Simulation-DailyV1.asset`; `Simulation-GeneralTest.asset` remains
the separate P10-A Ruin/LocalTopology proving profile. Retained exact-tree
P12-E validation includes selected-profile composition 24/24, five owner
provider suites 5/5, ALL EditMode 2434/2434, official Smoke 5/5, and
`git diff --check` PASS. The earlier exact profile-selection record remains
available in `P12DailyProfileSeparation/VALIDATION.md`.

The current 268-section admission protocol still omits the seven existing
P8-A/B/C providers. The bounded design
[`PHASE12_P12B_SPATIAL_PROFILE_CARDINALITY_ADMISSION_DESIGN.md`](design/PHASE12_P12B_SPATIAL_PROFILE_CARDINALITY_ADMISSION_DESIGN.md)
passed independent technical design review at exact tip
`ec73a0f95ca3bec0d50bd61dbe15a930d8a5635d`; its durable record is
[`PHASE12_P12B_SPATIAL_PROFILE_CARDINALITY_ADMISSION_DESIGN_REVIEW.md`](design/PHASE12_P12B_SPATIAL_PROFILE_CARDINALITY_ADMISSION_DESIGN_REVIEW.md).
It covers only those seven registrations and exact initial Required-section
cardinality admission, with a 275-section target. Implementation and
validation remain outstanding. This correction supersedes the stale 258/
P8-D-next wording; it does not assert complete owner coverage, mutation/epoch
coverage, global quiescence, capture eligibility, P12-B completion, P12-A
readiness, P13 readiness, export, hydration, or Phase 12 closure. P12-B
remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked.

## P12-B Daily-v1 spatial profile admission promotion — 2026-10-07

`codex/phase12/canonical` was fast-forwarded from `d8e6c9919d9359003dfd370fbd38a47424256b26` to reviewed candidate `14a44656b0a0b4211023d68ed39333d83ccc3481`. The promoted implementation code is `c2a21d8eb544291d3a46ef0d65d4f4c226feefbe`, tree `24f2c317ec28a8123b86d558ae0889505ba1aff6`. The exact-tip independent implementation review is `VALIDATED_CANDIDATE`, recorded in [`PHASE12_P12B_SPATIAL_PROFILE_CARDINALITY_ADMISSION_IMPLEMENTATION_REVIEW.md`](design/PHASE12_P12B_SPATIAL_PROFILE_CARDINALITY_ADMISSION_IMPLEMENTATION_REVIEW.md). The reviewed code tree remained unchanged through final preflight; validation and review records were the only post-code additions.

The selected Daily-v1 admission inventory is now 275 sections, up from 268. The slice registers the seven existing P8-A/B/C census providers against their exact installed owners and enforces the reviewed initial Required cardinalities: P8-A Hex/Location/scale `1/1/1`, RuntimeIdentity NPC/City/Location/Route `10/2/2/2`, and legacy spatial-network Location/Route `2/2`. P8-B passage/crossings and P8-C site/person-position sections remain `ExplicitlyEmpty` at zero. Initial NPC cardinality remains an admission condition only; the existing identity lifecycle remains `10 → 11 → 11`.

Validation on the reviewed code tree passed focused composition 24/24, runtime admission 58/58, Property/Estate 5/5, World Exchange 7/7, ALL EditMode 2442/2442, official Smoke 5/5, and `git diff --check`. Exact result XML, compressed logs, original/compressed hashes, and commands/results are retained in [`P12BSpatialProfileCardinalityAdmission/VALIDATION.md`](validation/P12BSpatialProfileCardinalityAdmission/VALIDATION.md). The user-approved profile split remains intact: SampleScene selects the dedicated P9-B-only `Simulation-DailyV1.asset`; `Simulation-GeneralTest.asset` remains the separate P10-A Ruin/LocalTopology proving profile.

This promotion adds census registration and initial cardinality admission only. It adds no P8-B/C writer, operation ID, mutation callback, or shared-epoch claim. P12-B remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains open. The promotion does not establish complete owner/cardinality or successful-write coverage, complete shared-epoch coverage, global quiescence, capture eligibility, export, hydration, P12-A/P13 readiness, or Phase closure.

### Refreshed P12 blocker DAG and next evidence task

The P12 dependency edges are unchanged: P12-C waits on B; P12-D/E wait on B and C; P12-F waits on C/D/E; P12-G waits on B–F plus a validated live-profile inventory; P12-A waits on complete included-owner export/staged hydration, validated live inventory, and its separate authorization; P13 remains blocked on supported continuation and recoverable causal history. P12-F Expedition remains deferred until its documented P12-C/D/E prerequisites are met.

The latest source-linked owner/operation/epoch crosswalk predates this promotion and records the earlier 268-section inventory. It does not prove exhaustive successful-write-to-epoch coverage; registration-to-scope consistency is not proof that every supported producer is represented. The refreshed read-only audit did not prove a concrete missing operation/epoch edge. The next bounded task is therefore an evidence-only, current-tip callsite matrix: enumerate the selected Daily-v1 live owners and cardinalities, locate every supported successful commit, map its exact sections/local revision, sealed operation scope and shared-epoch notification, and distinguish production reachability from registered-but-unused operations. Treat any unproven edge as an evidence gap, not as permission to add speculative mutation wiring. Revisit runtime-wide owner-thread/quiescence and completed-boundary lifecycle only after that current matrix is established.

### P8-A/B/C source reconciliation — audited canonical `2d61e19`

The source-linked P8-A/B/C admission and current Daily-v1 callsite delta is
recorded in [`PHASE12_B_BLOCKER_RESOLUTION.md`](design/PHASE12_B_BLOCKER_RESOLUTION.md)
and the updated [`PHASE12_OWNER_COVERAGE_INVENTORY.md`](design/PHASE12_OWNER_COVERAGE_INVENTORY.md).
The 275-section inventory now has exact P8-A `1/1/1` Required counts and
P8-B/C exact-zero admission against installed owner identity. Authored P8-A
geography is established before runtime construction and the P12 baseline.
At this audited canonical, P10 A/B and P14-B are rejected before WorldId;
P14-A reaches the P8-C exact-zero census rejection before publication. The
follow-up P12 candidate adds explicit P14-A rejection before WorldId. No
selected Daily-v1 P8-B/C position/passage/crossing writer was found. The
P8-specific delta therefore proves no missing operation or shared-
epoch edge. It adds no operation/callback and makes no broader completeness
claim.

The remaining matrix is still incomplete across the wider 275-section profile:
registration-to-operation consistency does not prove every supported ingress
or successful commit is mapped to its changed sections and shared epoch. The
read-only source audit found no concrete remaining missing writer/epoch edge,
so no new implementation checkpoint is justified from this evidence alone.
Continue the current-tip owner-family and successful-write reconciliation;
then prove runtime-wide owner-thread/quiescence and revalidate the completed-
boundary lifecycle against that evidence. Preserve P12-B `INCOMPLETE`, P12-A
`WAIT_DEPENDENCY`, and P13 `BLOCKED`.

### P14-A Daily-v1 early rejection correction candidate — 2026-10-07

The source audit found that promoted P12 code rejected P14-B finite sources
before WorldId allocation, but P14-A authored ExogenousDaily material flow
could pass the same early check and reach City-anchor composition. The existing
P8-C `ExplicitlyEmpty` witness then rejects it before publication. The accepted
P12 profile contract keeps P14-A outside selected Daily-v1, so candidate
`65315888a080d2df3fcddd7f977fe8136566a9bb` adds an early P14-A profile
rejection before WorldId allocation. Its `Assets` tree is
`ea356e4e1241b82395cfeb3821705589252d87a4`; the focused and full validation is
recorded in [`P12DailyP14Admission/VALIDATION.md`](validation/P12DailyP14Admission/VALIDATION.md).
This remains a candidate pending exact-tip review and canonical integration.
Standalone unscoped P14-A behavior remains covered. This candidate adds no
census section, operation, mutation callback, or shared-epoch claim, and it
does not change P12-B/P12-A/P13 readiness.

### P14-A Daily-v1 early-rejection correction — exact-tip review

Independent exact-tip implementation review passed for candidate `c1d37a8c8658beee87d682d27952a7f7d4970fc1` against P12 canonical `2d61e19e8c82bfc729d613d3462a087dfba8ac8f`. Reviewed code remains `65315888a080d2df3fcddd7f977fe8136566a9bb`, tree `ea356e4e1241b82395cfeb3821705589252d87a4`; later branch content is documentation/evidence only. The reviewer found no issues. The durable exact-tip findings are in [`PHASE12_P12B_DAILY_P14_ADMISSION_IMPLEMENTATION_REVIEW.md`](design/PHASE12_P12B_DAILY_P14_ADMISSION_IMPLEMENTATION_REVIEW.md).

The retained validation remains exact-tree: runtime admission 59/59, composition 24/24, selected Daily-v1 inventory 1/1, ALL EditMode 2443/2443, official Smoke 5/5, and `git diff --check` PASS. This candidate remains limited to the early rejection of P14-A authored material-flow configuration under selected Daily-v1; P10-A remains separate and standalone P14-A remains covered. P12-B remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked. Canonical promotion is the next orchestration action under the standing bounded-promotion authorization.

## P12-B Daily-v1 P14-A profile-boundary correction promotion — 2026-10-07

`codex/phase12/canonical` was fast-forwarded from `2d61e19e8c82bfc729d613d3462a087dfba8ac8f` to reviewed candidate `ed2e6a6c6375debfa0a739b7a886183997bb2871`. The promoted code commit is `65315888a080d2df3fcddd7f977fe8136566a9bb`, with `Assets` tree `ea356e4e1241b82395cfeb3821705589252d87a4`. The exact-tip independent review record is [`PHASE12_P12B_DAILY_P14_ADMISSION_IMPLEMENTATION_REVIEW.md`](design/PHASE12_P12B_DAILY_P14_ADMISSION_IMPLEMENTATION_REVIEW.md); the validation manifest is [`P12DailyP14Admission/VALIDATION.md`](validation/P12DailyP14Admission/VALIDATION.md). The post-review branch additions and this promotion record are docs-only; the executable tree remains the reviewed tree.

The selected `UnityBootstrap-Daily-v1` profile now rejects authored P14-A ExogenousDaily material-flow configuration before WorldId allocation and runtime/owner construction. Standalone unscoped P14-A remains covered, and P10-A Ruin/LocalTopology remains a separate proving profile. Validation remains focused admission 59/59, composition 24/24, exact selected-profile inventory 1/1, ALL EditMode 2443/2443, official Smoke 5/5, and `git diff --check` PASS.

This is only a profile-boundary correction. P12-B remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked. It makes no complete owner/shared-epoch, global quiescence, capture eligibility, export, hydration, downstream readiness, or Phase 12 closure claim. The wider owner/successful-write-to-operation-and-epoch matrix remains the next P12-B evidence obligation.


### P12-B selected Daily-v1 temporal owner/cardinality witness — 2026-10-07

Code candidate `cb299879b35dc01d8ec65dec36fd258f3d7f85b1` (commit tree `5f0201661e5e781597bc884ca7d290ce08a43266`, Assets tree `4d00528482829fcb3bcde58ff1bcbe4ba69f116b`) adds test-only temporal assertions to the selected Daily-v1 roster lifecycle test. At 10 → 11 → 10 → 11 NPC roster cardinalities it checks section IDs, exact owner identity, cardinality and local revision for MoneyAccount, Inventory, both SpatialKnowledge sections, and ten Knowledge sections per live NPC. The existing identity-registry assertions remain in place across unregister/re-registration.

Post-profile-separation validation is recorded in [`P12DailyTemporalCensus/VALIDATION.md`](validation/P12DailyTemporalCensus/VALIDATION.md): exact Daily admission rejection 1/1; exact selected-profile geography and owner/cardinality inventory 1/1; temporal owner-family roster test 1/1; ALL EditMode 2443/2443; official Smoke 5/5; `git diff --check` PASS. P10-A remains a separate proving profile.

This is a bounded temporal witness, not complete owner coverage. It adds no runtime behavior and establishes no complete commit/operation/epoch mapping, shared-epoch coverage, quiescence, capture eligibility, export, hydration, or phase readiness. The broader P12-B owner/operation/epoch matrix remains incomplete. P12-B remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains open. Independent exact-tip review validated candidate `54d3aea92e51b99d28fc2832468f9e8dc0eb5075`; the durable record is [`PHASE12_P12B_DAILY_TEMPORAL_CENSUS_REVIEW.md`](design/PHASE12_P12B_DAILY_TEMPORAL_CENSUS_REVIEW.md). The reviewed code and Assets trees remain unchanged.

## P12-B Daily-v1 temporal owner/cardinality witness promotion — 2026-10-07

`codex/phase12/canonical` was fast-forwarded from `d8deeb3f25e4523668e25ff5579c527a5189511e` to `a4db2868489e70a5f69816541f2221435ae3c8fc`. The promoted code commit is `cb299879b35dc01d8ec65dec36fd258f3d7f85b1`, commit tree `5f0201661e5e781597bc884ca7d290ce08a43266`, `Assets` tree `4d00528482829fcb3bcde58ff1bcbe4ba69f116b`. Independent exact-tip review is `VALIDATED_CANDIDATE` in [`PHASE12_P12B_DAILY_TEMPORAL_CENSUS_REVIEW.md`](design/PHASE12_P12B_DAILY_TEMPORAL_CENSUS_REVIEW.md); validation evidence is in [`P12DailyTemporalCensus/VALIDATION.md`](validation/P12DailyTemporalCensus/VALIDATION.md).

The selected Daily-v1 roster lifecycle test now checks exact owner-family section IDs, identities, cardinalities and local revisions at 10 → 11 → 10 → 11 roster states for MoneyAccount, Inventory, SpatialKnowledge and ten Knowledge sections per live NPC. Exact Daily admission still rejects authored P14-A before identity/owner construction, and exact selected-profile owner/cardinality admission passes. P10-A Ruin/LocalTopology remains a separate proving profile. This is partial temporal census evidence only.

The refreshed source audit found no specific supported Daily-v1 successful writer missing a section or epoch edge. The highest-value remaining evidence task is a current-tip effective-owner ledger for all 275 selected-profile sections (including dynamic roster expansions), paired with a supported-entrypoint → successful commit → changed sections/local revision → enclosing operation scope → shared-epoch notification matrix. Distinguish pre-runtime genesis writes and public/unused APIs from supported runtime ingress. Use that matrix to prove owner-thread coverage and quiescence at capture, then validate a completed-boundary token tied to successful advance sequence; the existing `CurrentDay` read alone is not that token.

This promotion adds no production owner, writer, operation, epoch behavior, quiescence, capture eligibility, export or hydration. It does not complete census coverage or P12-B. P12-B remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains open. No P12-A or P13 readiness is implied.

## P12-B Daily-v1 effective owner/cardinality census evidence promotion — 2026-10-07

After refreshing P12 canonical at `89b2368e9756069b2f52cd7cf17c26735f7c103f`, `codex/phase12/canonical` was fast-forwarded to the reviewed candidate `84cf1fc56851a0d2c845b3b165fd4a18de796da2`. Its code-bearing commit is `c50c4237d1e1023567f5ca0b24376a23d84bd4b7`, commit tree `d26ad1c0235bcc78104930ce9a2fce6883558603`, and Assets tree `70e7a06d1f7adfc59e3f07e8c9a5448567750863`. Independent exact-tip review passed with no findings; the durable result is [`PHASE12_P12B_DAILY_OWNER_MATRIX_IMPLEMENTATION_REVIEW.md`](design/PHASE12_P12B_DAILY_OWNER_MATRIX_IMPLEMENTATION_REVIEW.md). The validation manifest and sanitized XML are in [`P12DailyOwnerMatrix/VALIDATION.md`](validation/P12DailyOwnerMatrix/VALIDATION.md).

This test-only slice extends the selected Daily-v1 owner/cardinality assertions to NPC TravelState, merchant/travel plans, NPC life/residence, CrimeJustice status, current action, and Person life/residence after normal registration/materialization. It asserts the sealed count formula `65 + 20*N + U + P`; at the authored 10-NPC baseline this remains 275 sections. SampleScene continues to use the dedicated P9-B-only Daily-v1 profile, and P10-A Ruin/LocalTopology remains the separate GeneralTest proving profile. No runtime behavior, mutation scope, epoch wiring, or profile configuration changed.

Validation passed the temporal roster test 1/1, Person registration/materialization test 1/1, Daily admission 1/1, selected-profile inventory 1/1, ALL EditMode 2444/2444, official Smoke 5/5, and `git diff --check`. This is partial exact owner/cardinality evidence; it does not certify every effective section through all state changes or every supported successful writer.

A refreshed read-only source audit mapped all 23 registered Daily-v1 operation IDs to their known runtime entrypoints, owner/revision families, operation scopes, epoch notifications, and focused tests. It found no specific additional supported writer or missing epoch edge. The remaining P12-B work is to complete the source-linked census row and supported-ingress/commit matrix, then prove runtime-wide owner-thread/quiescence and validate completed-boundary eligibility against the successful advance sequence. The audit does not make registration-to-scope mapping exhaustive.

P12-B remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains open. P12-C still waits on B; P12-D/E wait on B and C; P12-F waits on C/D/E; P12-G waits on B–F plus a validated live-profile inventory. No complete owner/shared-epoch coverage, global quiescence, capture eligibility, export, hydration, or downstream readiness is implied.

## P12-B selected Daily-v1 registered-operation source matrix — 2026-10-07

The current-tip source crosswalk is recorded in [`PHASE12_B_DAILY_V1_REGISTERED_OPERATION_MATRIX.md`](design/PHASE12_B_DAILY_V1_REGISTERED_OPERATION_MATRIX.md). Independent document review passed against the current production source; its exact document hash and findings are in [`PHASE12_B_DAILY_V1_REGISTERED_OPERATION_MATRIX_REVIEW.md`](design/PHASE12_B_DAILY_V1_REGISTERED_OPERATION_MATRIX_REVIEW.md). The audit accounts for all 23 registered operation IDs, their known runtime entrypoints, owner/revision families, invalidation boundaries, and focused tests. No specific additional supported Daily-v1 writer or missing epoch edge was identified.

This closes the registration-to-scope crosswalk only. It does not prove that the 23 IDs exhaust all supported ingress, or that every effective owner/cardinality row and successful commit path has been enumerated. The next bounded evidence task is the exact 275-section effective owner/provider/revision ledger paired with supported-ingress and committed-write coverage; after that, prove runtime-wide owner-thread/quiescence and validate completed-boundary eligibility against the successful advance sequence. No speculative mutation or epoch wiring is justified by the current audit.

P12-B remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains open. P12-C waits on B; P12-D/E wait on B and C; P12-F waits on C/D/E; P12-G waits on B–F and a validated live-profile inventory. No owner/shared-epoch completeness, quiescence, capture eligibility, export, hydration, or downstream readiness is implied.

## Current architecture and Daily-v1 profile revalidation — 2026-10-07

The architecture branch `codex/architecture/world-identity-projection` is
current at `e16796014d348e3b59da7ed848101c4c03926ba5`. This refresh incorporates
architecture §2's development-artifact boundary, §§91A–91B's WorldId and
factual-reader contracts, §92A's continuation-aware owner requirements, the
2026-09-26 intraday/extensibility alignment (`4b6dd1d`), and the
multi-participant activity alignment (`c285466`). The updated profile rule was
revalidated against current P12 canonical `c7c8bbf01c599312971d80c99020965e7cfb924b`.

**Classification: `REVALIDATE` — PASS for the selected Daily-v1 composition.**
The P12 Brief explicitly selects `SampleScene` → `Simulation-DailyV1.asset` as
the approved P9-B-only profile input. The asset has no authored P10-A Ruin;
`Simulation-GeneralTest.asset` remains the separate P10-A proving profile, and
Daily-v1 admission rejects that profile before identity allocation. The exact
composition test checks the admitted selected profile and its 275-section
partial census; retained temporal roster and Person-materialization tests
recheck the supported dynamic owner/cardinality cases. Validation on code
`c50c4237d1e1023567f5ca0b24376a23d84bd4b7` / tree
`d26ad1c0235bcc78104930ce9a2fce6883558603` remains applicable: exact Daily
admission 1/1, selected-profile inventory 1/1, temporal roster 1/1, Person
registration/materialization 1/1, ALL EditMode 2444/2444, official Smoke 5/5,
and `git diff --check` PASS. The code has not changed since that validation.

This targeted revalidation does not certify that the full 275-row effective
owner/commit/epoch matrix is exhaustive. It preserves the accepted profile and
does not infer a new owner, complete owner/shared-epoch coverage, runtime-wide
quiescence, capture eligibility, export, hydration, P12-A readiness, P12-B
completion, P13 readiness, or Phase closure. The P12-B next evidence task
remains the complete source-linked owner/cardinality and successful-commit
matrix, followed by runtime-wide owner-thread/quiescence proof and a
completed-boundary token tied to successful advance sequence.

Independent exact-tip documentation review passed for candidate
`4cb2559ddbf7c95d84c446ab76e172493e59aa31` against base
`c7c8bbf01c599312971d80c99020965e7cfb924b`; findings and retained evidence
are recorded in [`PHASE12_DAILY_PROFILE_ARCHITECTURE_REVALIDATION_REVIEW.md`](PHASE12_DAILY_PROFILE_ARCHITECTURE_REVALIDATION_REVIEW.md).

The architecture branch's current Execution Model also carries older
human-approval wording for routine canonical promotion. The active run retains
the user's explicit standing authority for bounded promotions after all exact-
tip review, validation, ancestry, tree, and scope conditions pass; this
governance wording mismatch does not change technical readiness or authorize
Phase closure.

## P12-B Daily-v1 source-linked owner/commit ledger — 2026-10-07

The bounded effective-owner and known-commit ledger is recorded in
[`PHASE12_B_DAILY_V1_EFFECTIVE_OWNER_COMMIT_LEDGER.md`](design/PHASE12_B_DAILY_V1_EFFECTIVE_OWNER_COMMIT_LEDGER.md).
Candidate `1963cb6a7fab8a874b3c2c55af685cbe0cd4649a` was independently
reviewed against P12 canonical base `405f70e58a7a1dd8be255798b795faff095f44b4`;
the candidate tree is `28532e5abeb18f016cfceb890d15b8ff35549b0a`. The exact-tip
review is recorded in
[`PHASE12_B_DAILY_V1_EFFECTIVE_OWNER_COMMIT_LEDGER_REVIEW.md`](design/PHASE12_B_DAILY_V1_EFFECTIVE_OWNER_COMMIT_LEDGER_REVIEW.md).
The documentation-only candidate and review record are promoted together at
this State's commit; the ledger's reviewed tree is unchanged.

The ledger consolidates the selected-profile `65 + 20*N + U + P` inventory
(275 rows at the authored baseline) with source-linked owner identity,
cardinality, local revision, and known successful-commit boundaries. It
reconciles the 23 registered operation IDs and separately audits the eleven
unregistered RuntimeIdAllocator counters. It found no concrete new supported
Daily-v1 writer or missing shared-epoch edge. The ledger explicitly does not
prove exhaustive reachable-state or supported-ingress coverage; those eleven
counter kinds remain an open continuation/reconstruction boundary rather than
being declared irrelevant.

This closes the source-linked ledger evidence task only. The remaining P12-B
obligations are to resolve the effective-owner/supported-ingress exhaustiveness
boundary, prove runtime-wide owner-thread and quiescence coverage, and validate
a completed-boundary token tied to a successful advance sequence. No production
behavior or census section changes. P12-B remains `INCOMPLETE`; P12-A remains
`WAIT_DEPENDENCY`; P13 remains `BLOCKED`. No complete owner/shared-epoch
coverage, global quiescence, capture eligibility, export, hydration, downstream
readiness, or Phase 12 closure is claimed.

## P12-B current Daily-v1 ingress and P14-C profile revalidation — 2026-10-07

The selected-flow audit compared current P12 canonical `f191f87fe548469ba3f329fa083be5adc49656a2`, architecture `e16796014d348e3b59da7ed848101c4c03926ba5`, and P14 canonical `09fcbe46ffb5a7d80377185692d550310debc110`. The source-linked owner/commit ledger and owner-thread/quiescence evidence now record the current selected call graph and the P14-C profile boundary. The audit found no currently supported Daily-v1 ingress or successful commit omitted from the existing 23-operation crosswalk.

`SampleScene` selects the dedicated `Simulation-DailyV1.asset`; its normal path captures/checks the Unity owner thread and routes the Space action through `Simulate` to `runtime.advance-day`. The selected asset has no scheduled directives or external command queue. The separate WorldObserver command-console demo is not part of this profile. The public Expedition facade remains a call-graph caveat without a selected Daily-v1 caller and remains deferred to P12-F dependencies. This closes only the bounded current-source reconciliation. It does not prove every possible ingress, every reachable owner state, or all future callbacks/direct store-reference access.

P14-A/B/C remain separate material-flow proving profiles. At current P12 canonical, `ValidateP14SourceAdmission` applies the generic `HasAuthoredMaterialFlowCity` rejection before WorldId allocation; current P14 canonical additionally has an explicit mixed-source P14-C diagnostic and the `UnityBootstrapDailyRejectsMixedP14CSourcesBeforeIdentityOrOwnerConstruction` test. P12 Daily-v1 gains no owner, operation, or profile scope from this revalidation; the 275-row authored baseline is unchanged. P14-C's retained validation remains tied to code `6d9498d31bff1dc75e7071fe88ce2f80c37a7ebf` / tree `76142065bdcfcd01c2f58e7ba4dd0cdfef01ea92`: focused 9/9, ALL EditMode 2455/2455, official Smoke 5/5, SimulationRuntimeLongRun 7/7, and `git diff --check` PASS.

P12-B remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`. Remaining blockers are runtime-wide owner-thread/quiescence proof and a completed-boundary token tied to a successful advance sequence, alongside the unresolved effective-owner/reachable-state completeness boundary. No complete owner/shared-epoch coverage, capture eligibility, export, hydration, downstream readiness, or Phase 12 closure is implied.

### Promotion record

The docs-only revalidation candidate `cb1191c182f347dd11ffb1768fc5228ad2ee7696` (tree `ffae914890a091298a8d6488267d622973b1bad9`) passed exact-tip independent review and was fast-forwarded from P12 canonical `f191f87fe548469ba3f329fa083be5adc49656a2` to `57a50449090f873b81d5600ac7ef0aadbbbe55e8`. The durable review is [`PHASE12_DAILY_INGRESS_P14C_PROFILE_REVALIDATION_REVIEW.md`](design/PHASE12_DAILY_INGRESS_P14C_PROFILE_REVALIDATION_REVIEW.md). Remote canonical and the candidate branch synchronized at `57a5044`; `git diff --check` passed. This promotion changes documentation only and leaves the status and limitations above unchanged.

## P12-E Daily-v1 owner-package composition promotion — 2026-10-09

`codex/phase12/canonical` was fast-forwarded from `12339dd423bcab787ef5d10fad4c6e2b1597603d` to the exact-tip reviewed candidate `2e25c4e152ce435c12d1cddee696d6e5fe37e33a`. The code implementation is `e329951680fc690f3bcf97d97c07f00703893786`; its exact reviewed `Assets` tree is `7d2d1a949d9b9c836ada8889314828c171d01aa7`. The full candidate evidence tip before the review record was `0a84c701f1e9d1bb3b7d464f47e5427c02f15746` (tree `69479a7f0d6b5f6957cbca63d95fbd453945b329`). The review record is [`design/PHASE12_P12E_DAILY_V1_OWNER_PACKAGE_IMPLEMENTATION_REVIEW_E329951.md`](design/PHASE12_P12E_DAILY_V1_OWNER_PACKAGE_IMPLEMENTATION_REVIEW_E329951.md), durably pushed with the candidate before promotion. Remote canonical and candidate were verified synchronized at `2e25c4e` immediately after promotion.

The promoted private assembler captures and stages the complete accepted P12-E owner set for `UnityBootstrap-Daily-v1`, using one exact P12-B completed-boundary token, owner-section vector, and staging-attempt identity shared with the P12-C roots and P12-D package. Exact identity/cardinality/revision, required-empty owners, and cross-owner staged references are validated; the populated Institution → Office → Claim → Support path is covered. Typed P12-F Knowledge bindings remain unresolved evidence for P12-G. Validation remains tied to the exact `Assets` tree: P12-E package 6/6, P12-E focused 74/74, persistent-owner regressions 33/33, P12-C private-root composition 51/51, ALL EditMode 2715/2715, official Smoke 5/5, and `git diff --check` PASS. XML, archived log, and archive hashes are retained in [`validation/P12EOwnerSetComposition/VALIDATION.md`](validation/P12EOwnerSetComposition/VALIDATION.md); the final preflight independently rechecked all six XML/log hashes and the archive hash.

This completes P12-E only within its accepted selected-profile owner-set export/private-staging contract. It does not add publication, P12-G, P12-A integration/readiness, P13 readiness, global quiescence, or Phase 12 closure. Phase 12 remains OPEN.

### Refreshed P12 dependency DAG

- P12-B, P12-C, P12-D, and P12-E are COMPLETE/PROMOTED within their recorded bounded scopes.
- P12-F's documented dependency on P12-C/D/E is now satisfied. Its existing technical design remains a proposal and requires current-base owner-inventory and shared-`NpcRuntime` seam revalidation plus independent design review before implementation. This promotion does not claim P12-F readiness or implementation.
- P12-G remains WAIT_DEPENDENCY on P12-B through P12-F and the validated live-profile inventory. P12-A remains WAIT_DEPENDENCY on complete included-owner exports/hydration, validated live inventory, and its separate implementation authorization. P13 remains BLOCKED on its supported continuation/recoverable-causal-history requirements.
- No other numbered-phase dependency edge changes. No closed Phase is reopened.
