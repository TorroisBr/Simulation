# Independent review — CORE-F Runtime Owner Section Contracts

**Verdict: NEEDS_CHANGES**
**Reviewed tip:** 2121f562a322e8118253649970d666a030959f28
**Base:** 8770bbb43353cf9106b5598f6c4c09b4f1bd7343
**Core canonical:** a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
**Independent reviewer:** operation_inventory_review
**Review date:** 2026-10-27

## Review result

The code/test scope is coherent overall. Core stores the expected section contract identity inventory and its sorted projection; the adapter still owns provider instances and existing per-family validation. The update validates a staged map before replacing state under the runtime lock. Standalone census behavior remains local. The review found no weakened assertions, unrelated files, or changes to protected ProjectSettings.

The exact EditMode package was independently verified with the official packager: archive SHA-256 8cb44f8125c272520174c069195aed3baeac17c126852fb73528fdc9b3345241; 3,271/3,271 passed, zero failures/skips/inconclusive. The package manifest and changed Unity source/test hashes match the recorded inputs; no local workstation paths were found.

## Actionable findings

1. **P1 — bind dynamic catalogue replacement to the owning operation.** DailyV1ExecutionBoundaryState.TryReplaceExpectedOwnerSectionContractInventoryDuringRuntimeOperation checks only that some runtime protocol operation is active. That permits any registered operation to replace the entire sealed section identity catalogue. The accepted P12 owner/commit ledger assigns dynamic NPC roster and Person membership/materialization transitions to the exact runtime.npc-membership operation. Require that operation ID to be active. Nested demographic operations remain compatible because the membership operation stays active around the transition. Add a rejection test while a different operation (for example DailyAdvance or MarketSale) is active.
2. **P2 — exercise shared Core/adaptor agreement during a real roster transition.** Existing dynamic transition tests exercise standalone protocol state, while the new shared test checks only bootstrap inventory. Add a shared-protocol roster/materialization transition and assert the Core inventory's sorted identities, cardinality, and revision match the provider-facing projection after the transition.

These are implementation/test findings supported by the current approved contracts; no product or architecture decision is needed.

## Evidence limitations

CoreValidation (888/888 cold and warm) and the filtered Smoke invocation (5/5) passed in protected local evidence. Their complete report packages are not durable in the candidate: the approved .NET TRX adapter does not accept the CoreValidation manifest schema, and the approved Unity packager rejects the filtered Smoke XML because its parent suite advertises full discovery counts. The ALL EditMode package contains the same five Smoke case results but does not replace the separate filtered invocation. These are separate evidence-policy blockers; do not infer canonical promotion readiness from this review.
