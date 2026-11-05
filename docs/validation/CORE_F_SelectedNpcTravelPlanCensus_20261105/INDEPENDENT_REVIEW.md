# CORE-F selected NPC TravelPlan census — independent review

**Disposition: VALIDATED_CANDIDATE**

## Exact identities

- Review branch: `codex/core/CORE-F-TravelPlan-census-review-record-90e8ec92`
- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Candidate implementation commit: `90e8ec927bfe308ad43f7b493b7fab734e9d9ef7`
- Candidate implementation tree: `73d5d221db18276b98ae133a709e2b953a8e7012`
- Exact source base: `48403233484a191c567c7b44a3745ade991e3b85`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- The candidate branch's local and remote tips matched. The candidate is not Core canonical and this review does not promote it.

## Review scope and findings

The independent reviewer inspected the complete five-file delta, current CORE-F/P12 contracts, and exact-tip validation manifests. The change makes the selected census witness identify the authoritative `DailyV1NpcTravelPlanOwner` and its revision. `NpcTravelPlanRuntime` remains the Unity compatibility and P12 admission endpoint, bound to the same selected owner. P12 capture rejects a missing or divergent selected projection before accepting the Core-owned row. NPCs without a selected Core owner retain the existing Unity plan identity.

The tests continue to assert exact identities: selected NPCs map to the Core TravelPlan; compatibility NPCs retain their existing plan identity. Mixed-roster fallback retains all selected and compatibility rows, and P12 root capture preserves the established whole-roster behavior. No identity assertion was removed or weakened.

**Independent review verdict: PASS.** No actionable findings.

## Validation evidence

All Unity results below use the official evidence runner on the exact candidate source and test inputs. The independent review confirmed the five changed-file hashes in the full EditMode manifest match the reviewed commit.

- Focused selected-profile startup/owner-thread case is included in the full EditMode run.
- Mixed-roster P12 compatibility fallback: 1/1 passed, run `758a572dc5a74ecebd408bd763adc431`, stable input.
- P12 NPC root-owner snapshot suite: 33/33 passed, run `5fc95fbb9b72409b94067250d339ee7a`, stable input.
- Full EditMode: 3299/3299 passed, run `a9886b16c2ea490fb019e3b3de12e599`, stable input.
- GMConsole smoke fixture: 2/2 passed in EditMode, run `49ea68add8c348cb9a9d57501d8814f4`, stable input. An empty PlayMode filter result was excluded.
- CoreValidation: 1017/1017 cold and warm passed on source base `48403233484a191c567c7b44a3745ade991e3b85`, run `7b1c3de4953c4298b236b8f8f1a2f91a`. This candidate changes only Unity host adapters and Unity tests; Core sources and Core tests are unchanged from that validated base.
- `git diff --check` passed for the exact implementation commit.

The validation manifests, original XML reports, and logs remain in their protected E: evidence directories and were not modified by this review.

## Integration boundary

This is a bounded selected-owner census and P12 capture integration checkpoint only. It does not complete CORE-F, authorize canonical promotion, or establish detached .NET continuation. Recompute dependencies before any later integration decision.
