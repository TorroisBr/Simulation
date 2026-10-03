# P12-B RuntimeIdAllocator Decision-counter implementation review

**Verdict: `VALIDATED_CANDIDATE`**

- Canonical base: `22525cb5f96eb9eed2e168b7e6a23fdc1e420304`
- Accepted design: `1d265aa411292393b693914e8552ad23dac7ce14`
- Design review record: `f7406787c6280a9191f5111fe796ca636e4a4994`
- Candidate branch: `codex/phase12/P12BRuntimeIdDecisionCounterInvalidationImplementation`
- Exact candidate tip reviewed: `42b61a6fd41785461d6137098c8277acfcf00146`
- Reviewed code commit: `52154053219e30e679bc400adf55c2f577bd8106`
- Reviewed code tree: `ddd3684daf264a15af9c217c8edb4e390e82602d`
- Reviewer: independent; no candidate files were edited during review.

## Findings

The implementation matches the bounded design. It registers the existing schema-v1 `p12c.runtime-id-allocator.decisions` section as required, with cardinality one, the exact shared `RuntimeIdAllocator` identity, and local revision `nextDecisionSequence - 1`. The runtime checks the owner thread, section baseline, and shared epoch capacity before advancing the cursor. After allocation, it reports only the Decisions section. Existing TravelParty, Merchant, and solo-travel contexts retain their changed-section batching behavior.

Bootstrap composition compares the runtime and composition Decision witnesses by section, schema, cardinality, owner identity, and current revision, rejecting mismatches. `AllocateDecisionId()` preserves exhaustion ordering and the legacy non-P12 `decision-000001` format. The recorder's existing Decision-ID-before-record-sequence order remains intact; if later sequence allocation fails, the consumed Decision ID remains committed and its revision and epoch invalidation are observable.

Focused coverage verifies exact/different allocator owners, successful and rejected allocations, stale baseline, wrong thread, allocator exhaustion, epoch saturation and final representable step, later sequence exhaustion, nested Merchant batching, and non-P12 compatibility. `git diff --check` passes. The implementation changes only the allocator provider, allocator, runtime, bootstrap composition, and three focused test files.

## Exact-tree validation

The following retained artifacts were independently checked for SHA-256, XML result, and test counts. Each XML reports `Passed`, zero failures, and zero skipped tests. Artifact filenames and paths are listed in the candidate record [`PHASE12_P12B_RUNTIME_ID_DECISION_COUNTER_INVALIDATION_CANDIDATE.md`](PHASE12_P12B_RUNTIME_ID_DECISION_COUNTER_INVALIDATION_CANDIDATE.md).

| Gate | Result | XML SHA-256 | Log SHA-256 |
|---|---:|---|---|
| `SimulationRecordSequenceP12InvalidationTests` | 15/15 | `C961326E81B0D4BB7489E9FF588555BBB7257A0BB112E134E2F3C45ED00130A3` | `5803BC46D77A476BED30AB59892658F44F8EA9241B2AB24DD66B0AE450CDC181` |
| `SimulationRuntimeAdmissionTests` | 31/31 | `F201F09F1165F03786059318B351F8E5534ACCB70AA46D35C5D6F7CFAEAEF90E` | `141B4158A102FC4E7B08766AC8D7AD294E0049003FA1B9BBA4154F8E6EEE805B` |
| `SimulationBootstrapCompositionTests` | 21/21 | `6E7CB64A6C1909648BE69D83EB124D71F2E3C55807035A733937CA321C944FE4` | `C0C9DC884132EC14E8468A70EB9899A8DB4FF0BF3457D81B0F3089951B55EB0E` |
| `CoreRuntimeTests` | 14/14 | `9E60BCD32F1CD2006F53DA5BEBB88B2FC150D72DA31152E6F29CFF770DEE00E6` | `CEAF0A7E8CBE2A17D5612E3A890ADF25CFCE8D86554B6B2A89AB3AEC4E2E94D5` |
| `RuntimeIdAllocatorCensusTests` | 3/3 | `B21C97D566AD9BBBBF47B4CB9F0158DAABC512AAFF2BA65FD9661AE4FFC3C389` | `599459D4491F48BF1444B0BA55CB187A60663E2CEFBE1E0EB648D5296C3BBCF7` |
| ALL EditMode | 2227/2227 | `E9AABB65A20595BD7FF35542AF9A3D3359C2D469BE2DD1649CA415FB6F8AA8D1` | `48C821133747D7B71850B57D49939700E338A5DB871F31C9CB803473B251109C` |
| Official EditMode Smoke | 5/5 | `5FFE7DB2FE471D3B305F4D4000D2893D7E4F21775BCD45CBA0C511105419DD66` | `89A1DEA2AE901BD18BAC2B57825C99727D1018A4D174BCF15355FEFA6888F74C` |

## Scope and status

This candidate covers only Decision-ID cursor invalidation for successful allocations through the selected P12 runtime and existing nested batching. It does not add other allocator counters, alter decision or record ordering, cover DecisionStore/receipt/ActorChoice mutation, or provide export/hydration. It makes no complete owner/epoch coverage, global quiescence, capture eligibility, P12-A readiness, P12-B completion, P13 readiness, or Phase closure claim.

P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked. This review validates the candidate only and does not approve canonical promotion.
