# Independent Exact-Tip Review — Selected Core FleeCity Dispatch

## Verdict

**VALIDATED_CANDIDATE — PASS. No actionable correctness or test-integrity findings.**

## Identity

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Candidate: `b21c11f5f8dc31773232b6082db711a386222578`
- Candidate tree: `40f2b7e7910fc6b656006645bdb0edbd9584cdbd`
- Immediate parent and review base: `4c99e7ef82869fd4349b28f511fd868862076185`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

## Review

The candidate diff contains only `SimulationRuntime.cs` and `SimulationRuntimeAdmissionTests.cs`. The exact target-city resolver rejects duplicate or foreign projections, requires the exact graph City owner and the same SpatialLocation object, and the reconstructed action view must bind to the exact current action payload. Dispatch requires the live selected admission context on its owning thread. A present stale action projection still fails closed.

FleeCity's existing adapter rechecks the exact target City before dispatch. Core remains authoritative for eligibility, deterministic action roll, route and cost planning, TravelPlan owner commit, and its single P12 mutation notification. Projection reconstruction uses `notifyP12MutationBoundary: false`; it does not create an extra owner revision or census epoch. The committed travel-plan state is published to Unity as a compatibility view, without moving the actor or debiting MoneyAccount.

The new regression covers admitted projection recovery, rejection when admission is absent, unchanged action revision and mutation epoch during recovery, exact destination identity, deterministic action roll, exact pending TravelPlan owner/projection state, unchanged Travel state and account balance/revision, and final census success. Assertions were not weakened. Existing tests continue to cover Core FleeCity dispatch and owner commit semantics.

The reviewer verified that candidate source/test blobs match the protected run-manifest input hashes and that focused 1/1, full EditMode 3287/3287, and official Smoke 5/5 runs pass with stable inputs. The sanitized package archive and one-entry count match its manifest; `git diff --check` passes. Validation results are recorded in [`VALIDATION.md`](VALIDATION.md). No tests were rerun during review.

## Non-blocking coverage note

The new Unity integration test does not directly exercise a stale target-City projection or failed FleeCity action roll. The exact City boundary rejects identity mismatches before the roll or TravelPlan commit, and adjacent coverage checks stale selected projections and Core FleeCity dispatch. This is not a blocker for this bounded candidate.

## Integration limits

This is a candidate review, not Core canonical promotion or CORE-F completion. Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`. The existing `CrimeSystem` provider and `TravelSystem` store remain required Unity host adapters; detached-host readiness is not claimed.
