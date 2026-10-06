# P12-B Property/Estate owner mutation-epoch candidate

**Status:** Implemented, validated candidate. P12-B remains incomplete.

## Exact base and design

- Canonical base: `82735cb0ac7878fda0efd7d9e6a3029fe8a501f7`.
- Reviewed technical design: `19d2e6c92ccd224b8289d2095dc33103e2858287`.
- Independent design review: `codex/phase12/P12BPropertyEstateMutationEpochDesignReview` at `eb0270ae189684198d31ade91a6e8ebb53909293` — PASS against the exact design and canonical base.
- Original code candidate: `955dc087e931d9204f6fca33b8beb5fb45f7e211`, tree `ca2c6bd5ccf44a0441f9b31391eb9bb82a97524d`.
- Architecture §92A revalidation code candidate: `codex/phase12/P12BPropertyEstateEpochImplementation` at `167a488c09fc7a2dc51517e1886250c43303bc20`, tree `7527439309841a8f68302e7c7630ddcecbc36b21`; only focused admission tests changed after the original implementation.

## Bounded changes

The selected `UnityBootstrap-Daily-v1` runtime now requires the three existing Property/Estate census sections. They bind to the exact installed `PropertyOwnershipStore` and `EstateStore`, require schema v1, and require their initial cardinalities to be zero. The ten-NPC/two-City selected profile inventory is 242 sections, up from 239. `Simulation-GeneralTest.asset` retains P10-A Ruin/LocalTopology as a separate proving profile.

Two operation IDs were registered:

- `p12.property.owner-commit` for ownership registration, property transfer, and the property transfer inside estate succession.
- `p12.estate.owner-commit` for explicit estate opening.

Each selected runtime facade validates owner thread, unchanged affected-section baselines, and mutation-epoch capacity before the domain commit. Successful commits report only their changed sections. Both Property sections report the same store identity and local revision; succession does not change EstateStore. Existing domain rejection results are preserved. P12 admission refusal for estate succession uses the appended `EstateSuccessionFailureCode.RuntimeFaulted = 17`; existing values 0–16 are unchanged.

The tests cover initial exact owners and cardinalities, successful registration/transfer/opening/succession, duplicate and same-owner rejection, stale succession rejection without another owner or epoch change, epoch deltas, registered-operation quiescence, and pre-commit owner-thread refusal. After current Architecture §92A was revalidated, focused negative proofs were added for prepopulated Daily-v1 Property ownership plus transfer history and for a pre-existing Estate. Both are rejected during runtime-admission construction before a runtime can be returned to bootstrap publication.

## Validation

After the §92A negative proofs were added, focused suites passed 51/51 total: `PropertyEstateMutationEpochTests` 5/5, `PropertyOwnershipCensusTests` 2/2, `EstateCensusTests` 1/1, `SuccessionIntegrationTests` 21/21, and `SimulationBootstrapCompositionTests` 22/22. ALL EditMode passed 2415/2415, official Smoke passed 5/5, and `git diff --check` passed. Exact XML/log paths and SHA-256 values are in [`P12BPropertyEstateEpoch/VALIDATION.md`](../validation/P12BPropertyEstateEpoch/VALIDATION.md).

## Limits

This is a bounded P12-B operation/invalidation slice. It does not establish complete profile owner coverage, complete shared-epoch coverage, global owner-thread or quiescence proof, capture eligibility, export, hydration, P12-A readiness, P12-B completion, P13 readiness, or Phase closure. Direct calls to exposed stores/static systems outside the selected runtime facades remain outside this slice. Phase 12 remains open; P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked.
