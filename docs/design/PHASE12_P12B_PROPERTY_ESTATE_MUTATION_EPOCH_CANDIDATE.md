# P12-B Property/Estate owner mutation-epoch candidate

**Status:** Implemented, validated candidate. P12-B remains incomplete.

## Exact base and design

- Canonical base: `82735cb0ac7878fda0efd7d9e6a3029fe8a501f7`.
- Reviewed technical design: `19d2e6c92ccd224b8289d2095dc33103e2858287`.
- Independent design review: `codex/phase12/P12BPropertyEstateMutationEpochDesignReview` at `eb0270ae189684198d31ade91a6e8ebb53909293` — PASS against the exact design and canonical base.
- Code candidate: `codex/phase12/P12BPropertyEstateEpochImplementation` at `955dc087e931d9204f6fca33b8beb5fb45f7e211`.
- Code tree: `ca2c6bd5ccf44a0441f9b31391eb9bb82a97524d`.

## Bounded changes

The selected `UnityBootstrap-Daily-v1` runtime now requires the three existing Property/Estate census sections. They bind to the exact installed `PropertyOwnershipStore` and `EstateStore`, require schema v1, and require their initial cardinalities to be zero. The ten-NPC/two-City selected profile inventory is 242 sections, up from 239. `Simulation-GeneralTest.asset` retains P10-A Ruin/LocalTopology as a separate proving profile.

Two operation IDs were registered:

- `p12.property.owner-commit` for ownership registration, property transfer, and the property transfer inside estate succession.
- `p12.estate.owner-commit` for explicit estate opening.

Each selected runtime facade validates owner thread, unchanged affected-section baselines, and mutation-epoch capacity before the domain commit. Successful commits report only their changed sections. Both Property sections report the same store identity and local revision; succession does not change EstateStore. Existing domain rejection results are preserved. P12 admission refusal for estate succession uses the appended `EstateSuccessionFailureCode.RuntimeFaulted = 17`; existing values 0–16 are unchanged.

The tests cover initial exact owners and cardinalities, successful registration/transfer/opening/succession, duplicate and same-owner rejection, stale succession rejection without another owner or epoch change, epoch deltas, registered-operation quiescence, and pre-commit owner-thread refusal.

## Validation

Focused suites passed 49/49 total: `PropertyEstateMutationEpochTests` 3/3, `PropertyOwnershipCensusTests` 2/2, `EstateCensusTests` 1/1, `SuccessionIntegrationTests` 21/21, and `SimulationBootstrapCompositionTests` 22/22. ALL EditMode passed 2413/2413, official Smoke passed 5/5, and `git diff --check` passed. Exact XML/log paths and SHA-256 values are in [`P12BPropertyEstateEpoch/VALIDATION.md`](../validation/P12BPropertyEstateEpoch/VALIDATION.md).

## Limits

This is a bounded P12-B operation/invalidation slice. It does not establish complete profile owner coverage, complete shared-epoch coverage, global owner-thread or quiescence proof, capture eligibility, export, hydration, P12-A readiness, P12-B completion, P13 readiness, or Phase closure. Direct calls to exposed stores/static systems outside the selected runtime facades remain outside this slice. Phase 12 remains open; P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked.
