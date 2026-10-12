# P12-G Faction post-guard recapture witness

## Scope

This candidate adds one EditMode test for the existing P12-G CandidateGuardBound hook. The test uses the supported SimulationRuntime.TryRegisterFaction path to add a Faction after target-owner checks and before final target-owner recapture. It verifies that recapture rejects the changed target vector before admission/publication, leaves the source session and token retryable, permits a clean retry, and preserves next-boundary parity.

No production script changed relative to canonical base e1190fb127cc74a98169f1c07f357cd8a487e186. The test source blob is 0376885edf6dc3083e3c6f0ef8ee4d065cae1cc0. This remains a bounded P12-G witness; it does not claim P12-G readiness/completion, complete owner coverage, or P12-A/P13 readiness.

## Validation

Unity Editor 6000.3.9f1:

| Suite | Result |
|---|---:|
| Focused DailyV1RestoreRejectsFactionAddedAfterTargetChecksAndKeepsSourceRetryable | 1/1 passed |
| SimulationRuntimeAdmissionTests | 200/200 passed |
| ALL EditMode | 2879/2879 passed |
| Official Smoke | 5/5 passed |
| git diff --check | PASS |

All reports show zero failed, skipped, or inconclusive tests. The focused and regression suites ran before artifact path sanitation. Sanitization changed only incidental path/user labels in report XML and logs; the test source blob and production scripts remained unchanged. XML result projections are recorded in SANITIZATION.md.

## Boundaries retained

This witness does not close P12-G and does not establish full deterministic continuation parity, all failure-boundary guarantees, integrated restored-graph validation, full owner/epoch coverage, P12-A readiness, or P13 readiness. The existing Phase 12 State remains authoritative for the remaining obligations.