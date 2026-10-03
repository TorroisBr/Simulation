# P12-B RuntimeIdAllocator Decision-counter invalidation candidate

**Status:** independently reviewed `VALIDATED_CANDIDATE`; full required validation passed on the exact code tree. Review is recorded in [`PHASE12_P12B_RUNTIME_ID_DECISION_COUNTER_INVALIDATION_IMPLEMENTATION_REVIEW.md`](PHASE12_P12B_RUNTIME_ID_DECISION_COUNTER_INVALIDATION_IMPLEMENTATION_REVIEW.md). Canonical promotion, P12-B completion, P12-A readiness, and P13 readiness are not claimed.

**Canonical base:** `codex/phase12/canonical` at `22525cb5f96eb9eed2e168b7e6a23fdc1e420304`.
**Reviewed design:** `1d265aa411292393b693914e8552ad23dac7ce14`; design review record `f7406787c6280a9191f5111fe796ca636e4a4994`.
**Candidate branch:** `codex/phase12/P12BRuntimeIdDecisionCounterInvalidationImplementation`.
**Implementation commit:** `52154053219e30e679bc400adf55c2f577bd8106`.
**Exact implementation tree:** `ddd3684daf264a15af9c217c8edb4e390e82602d` (matches the staged `git write-tree` captured before commit).

## Bounded change

The selected `UnityBootstrap-Daily-v1` protocol now registers the existing schema-v1 `p12c.runtime-id-allocator.decisions` witness as required, cardinality one, on the exact shared `RuntimeIdAllocator`. Successful `AllocateDecisionId()` calls preflight the selected owner thread, unchanged Decision-counter baseline, and mutation-epoch capacity before incrementing the cursor; post-commit notification reports only the Decisions section. Existing Merchant batching collects Decision and record-sequence changes into the enclosing epoch step.

Bootstrap composition compares the Decision provider with the runtime-bound provider by section, schema, cardinality, opaque owner identity, and current revision. Tests cover exact and different owners, successful allocation, unchanged sibling Event counter, stale baseline, wrong thread, allocator exhaustion, epoch saturation/final representable step, later record-sequence exhaustion after a committed Decision ID, nested Merchant batching, and non-P12 output compatibility.

The recorder's existing order and consumed-ID behavior are preserved. If later sequence allocation fails, the already allocated Decision ID stays consumed and its revision remains visible. No record sequence, decision-store, occurrence-receipt, ActorChoice, other allocator-counter, operation, export/hydration, or broader coverage semantics were added.

Changed code/test paths are limited to:

- `Assets/_Project/Scripts/RuntimeIdAllocatorCensusProviders.cs`
- `Assets/_Project/Scripts/RuntimeIdentity.cs`
- `Assets/_Project/Scripts/SimulationBootstrapComposition.cs`
- `Assets/_Project/Scripts/SimulationRuntime.cs`
- `Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs`
- `Assets/_Project/Tests/EditMode/Editor/SimulationRecordSequenceP12InvalidationTests.cs`
- `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs`

## Exact-tree validation

All Unity results below are retained under `Library/ValidationResults/P12BDecisionCounter/`. Each XML and log hash is SHA-256.

| Gate | Result | XML | XML SHA-256 | Log | Log SHA-256 |
|---|---:|---|---|---|---|
| `SimulationRecordSequenceP12InvalidationTests` | 15/15 | `EditMode-20261003-193224-8c3db9c74c744483b293f46bde566eeb.xml` | `C961326E81B0D4BB7489E9FF588555BBB7257A0BB112E134E2F3C45ED00130A3` | `EditMode-20261003-193224-8c3db9c74c744483b293f46bde566eeb.log` | `5803BC46D77A476BED30AB59892658F44F8EA9241B2AB24DD66B0AE450CDC181` |
| `SimulationRuntimeAdmissionTests` | 31/31 | `EditMode-20261003-193238-d45eb5bfdb284ff69b8e6c00d99d692c.xml` | `F201F09F1165F03786059318B351F8E5534ACCB70AA46D35C5D6F7CFAEAEF90E` | `EditMode-20261003-193238-d45eb5bfdb284ff69b8e6c00d99d692c.log` | `141B4158A102FC4E7B08766AC8D7AD294E0049003FA1B9BBA4154F8E6EEE805B` |
| `SimulationBootstrapCompositionTests` | 21/21 | `EditMode-20261003-193254-acf4a516ead34e4bbb36a89f59bdf399.xml` | `6E7CB64A6C1909648BE69D83EB124D71F2E3C55807035A733937CA321C944FE4` | `EditMode-20261003-193254-acf4a516ead34e4bbb36a89f59bdf399.log` | `C0C9DC884132EC14E8468A70EB9899A8DB4FF0BF3457D81B0F3089951B55EB0E` |
| `CoreRuntimeTests` | 14/14 | `EditMode-20261003-193308-6bcffbb2d73e48a0aad70c395de3f6de.xml` | `9E60BCD32F1CD2006F53DA5BEBB88B2FC150D72DA31152E6F29CFF770DEE00E6` | `EditMode-20261003-193308-6bcffbb2d73e48a0aad70c395de3f6de.log` | `CEAF0A7E8CBE2A17D5612E3A890ADF25CFCE8D86554B6B2A89AB3AEC4E2E94D5` |
| `RuntimeIdAllocatorCensusTests` | 3/3 | `EditMode-20261003-193322-36ec6fb4dc1c4a6999f8ec623e69692b.xml` | `B21C97D566AD9BBBBF47B4CB9F0158DAABC512AAFF2BA65FD9661AE4FFC3C389` | `EditMode-20261003-193322-36ec6fb4dc1c4a6999f8ec623e69692b.log` | `599459D4491F48BF1444B0BA55CB187A60663E2CEFBE1E0EB648D5296C3BBCF7` |
| ALL EditMode | 2227/2227 | `EditMode-20261003-193336-efd0b09400034db3921ffda7dfc0d4aa.xml` | `E9AABB65A20595BD7FF35542AF9A3D3359C2D469BE2DD1649CA415FB6F8AA8D1` | `EditMode-20261003-193336-efd0b09400034db3921ffda7dfc0d4aa.log` | `48C821133747D7B71850B57D49939700E338A5DB871F31C9CB803473B251109C` |
| Official EditMode `Smoke` filter | 5/5 | `EditMode-20261003-193410-67e9ee08c2704a8ca44a944361e506e4.xml` | `5FFE7DB2FE471D3B305F4D4000D2893D7E4F21775BCD45CBA0C511105419DD66` | `EditMode-20261003-193410-67e9ee08c2704a8ca44a944361e506e4.log` | `89A1DEA2AE901BD18BAC2B57825C99727D1018A4D174BCF15355FEFA6888F74C` |

`git diff --check` passed on the implementation tree before commit. The official Smoke gate is the five-test EditMode `-TestFilter Smoke`; the zero-test PlayMode probe is excluded.

## Evidence boundary

This candidate covers only invalidation for successful Decision-ID cursor writes through the selected P12 runtime and their existing nested batching behavior. It does not establish complete allocator-root or owner coverage, complete shared-epoch coverage, global quiescence, capture eligibility, export, hydration, P12-A readiness, P12-B completion, P13 readiness, or Phase 12 closure. P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked.
