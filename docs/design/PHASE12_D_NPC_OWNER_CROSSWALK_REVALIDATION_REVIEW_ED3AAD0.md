# P12-D NPC owner crosswalk current-base revalidation exact-tip review

**Verdict:** VALIDATED_CANDIDATE<br>
**Readiness disposition:** READY_FOR_IMPLEMENTATION for the bounded NPC D/F value snapshot, projection merge, and private staged reconstruction slice only, after this independent review. This does not promote code, complete P12-D, or authorize P12-A.

## Exact refs

- Repository: `TorroisBr/Simulation`
- Current P12 canonical and candidate base: `ed3aad0bcf98fc1b709b6bc632452689448b8803`
- Candidate branch: `codex/phase12/P12DNpcOwnerCrosswalkRevalidationEd3aad0`
- Exact candidate tip: `945d219a5910389f6faec8977bcec75c52de4048`
- Candidate tree: `14470a7bf714c687c49e9245d2dc6b2ef947540c`
- Reviewed document blob: `4a25891122a1b97f99f3b7274daf9b559e6b7134`
- Architecture authority: `47eff220c7ce00f6e7c759bdc2b76780bb46f628`

Remote refs matched these SHAs during review. The candidate is based on current P12 canonical. Its cumulative diff adds only `docs/design/PHASE12_D_NPC_OWNER_CROSSWALK_REVALIDATION_ED3AAD0.md`; `git diff --check` passes.

## Review result

I reviewed the complete candidate document against the current P12 Brief/State, the accepted P12-D technical design, the prior exact-tip NPC source crosswalk and its review, the P12-D City/NPC assembly revalidation and implementation review, the exact-zero receipt-owner review, and current source at the stated canonical base.

The document's source delta is accurate. The prior crosswalk was audited against P12 `ef0cafb5848cadcf0ac91a3e1af7ff3faaab1367`; the current base is 29 commits later. In the changed NPC source, the new relevant seams are the non-lazy `ExistingLocalKnowledgeObservationRuntime` and `ExistingMerchantTradeStateRuntime` accessors and the private staged-presence factory. The latter assigns direct staged City/Location references without invoking gameplay presence or City-membership mutations. The receipt-provider/protocol delta adds the exact-zero child-owner witnesses described in the revalidation; it does not change the D/F value assignment or introduce another NPC snapshot.

The reviewed field matrix covers the serialized NPC value surface and its exact owner boundary:

- D retains NPC runtime/definition identity for the loaded projection, factual life/status/hidden-day state, current/residence/destination roots, inventory/account values, and the current action definition. Person binding and bound residence remain owned by the reciprocal PersonStore relation and Person state.
- F retains the mutable action runtime, travel and plan commitments, and admitted Knowledge values. Typed links refer to D roots; action-definition identity must agree with D. No action or travel is replayed during stage.
- Inventory, account, lifecycle, status, action, travel, plan, and Knowledge rows use the named current census families and their existing local owner revisions/cardinalities. Variable collection lengths remain value cardinalities inside their owner rows.
- `p12d.explorable-sites` remains required-empty for Daily-v1; P10 LocalTopology remains typed `NOT_COMPOSED`. The two P18 receipt owners remain excluded from D/F payloads and must be proven exactly empty.
- The D/F split follows the current technical design: capture one concrete NPC once under the completed-boundary token and exact component-revision vector; bind both disjoint projections to the same token, stamp, and vector; merge them before constructing the NPC once. No synthetic aggregate NPC revision or second projection is added.

The receipt evidence is concrete in current code. For each exact roster NPC, the provider creates two distinct schema-v1 rows keyed by RuntimeId: `p12d.npc-local-observation-receipts/{RuntimeId}` and `p12d.npc-merchant-trade-state-receipts/{RuntimeId}`. It rejects duplicate/aliased NPC or child-owner objects, missing/replaced owners, and any nonzero cardinality or revision. Owner reads bypass lazy list normalization. The protocol registers those required rows in the existing owner vector and rejects stale tokens after receipt writes. The exact-tip receipt-owner review records the tested stale-token and populated-roster-add rejection cases. This is a fail-closed empty witness, not receipt export or replay.

The E/F boundary remains single-owner. The P12-D design assigns the NPC values to D/F even when a provider in another checkpoint writes them; a census prefix such as `p12e.npc-money-account` does not transfer value ownership or create a duplicate NPC projection. No promoted P12-E/F exact owner snapshot covers an NPC row. The existing diagnostic `WorldStateNpcSnapshot` is a diagnostic view, not a detached persistence export or private hydration factory. PersonStore remains authoritative for PersonId↔NpcRuntimeId materialization; this also preserves the architecture rule that NpcRuntimeId is a loaded/runtime projection identity, not historical Person identity.

The P12 State at the reviewed base records City/NPC assembly validation of City 17/17, receipt-owner 13/13, ALL EditMode 2592/2592, official Smoke 5/5, and cumulative diff-check PASS. The exact code/evidence records remain tied to their stated code trees. No Unity tests were rerun because this candidate is documentation-only and does not change code or evidence.

## Readiness and limits

The technical review passes the current-base revalidation. Under the accepted P12-D prerequisite capability scope, the bounded NPC D/F value snapshot/private-stage owner slice is now `READY_FOR_IMPLEMENTATION`. The implementation must still meet the accepted design's detached immutable capture, token/stamp/vector consistency, one construction per owner, exact staged values and revisions, typed-reference validation, and fail-before-publication requirements.

This readiness applies only to the NPC D/F owner slice. It does not establish City/NPC whole-profile runtime/bootstrap integration, whole-D graph validation, all remaining D owner coverage, P12-D completion, P12-A readiness, P13 readiness, profile-wide export/hydration, or Phase 12 closure. P12-D remains IN PROGRESS; P12-A remains WAIT_DEPENDENCY; P13 remains BLOCKED. No product or canonical architecture decision remains unresolved for this bounded slice.
