# P20-B joint civil travel — implementation progress

**Status:** focused implementation progress pushed; not a complete integration candidate, not independently reviewed, and not promoted.

**Implementation commit:** `e93731c847dfd5e6973d8df0fae78419a46c74ea`

**Code tree:** `92ba3b27c65d18bb0c5786b8e99860116c94d22b`

**Parent candidate:** `b2ca724c2da2bb89b943f6caf9c141bdee843718`

**P20 design handoff:** `d80ec06f48500a0ee80d6e05136ad50a6978a670`

**Architecture baseline:** `f6924e63d8e5731da1d33021d0361e7defe6dad7`

This additive commit retains the existing bounded P20-B implementation and adds causal identities/order for consent and `AbortAfterLeg`, derives public lifecycle state from P18, stages proposal and P20 owner installation in one timeline commit, and strengthens stale-start, current-passage, terminal-arrival, and clone-identity tests. It does not modify `SimulationRuntime`, bootstrap, or P12 admission.

## Focused validation

The initial focused run exposed test-fixture issues: unsealed timeline input windows and stale-write tests attempting initial-position registration after a Person already had a position. The fixtures were corrected to seal each advanced tick and to create valid intervening P8 mutations. The final exact code tree then passed:

| Suite | Result |
|---|---:|
| `P20JointCivilTravel` | 14/14 |
| `ActivityLifecycleTests` | 17/17 |
| `LogicalTimelineTests` | 38/38 |
| `P20SyntheticOperationTests` | 11/11 |
| `GeneralizedSpatialTravelTests` | 18/18 |
| `SpatialRoutePlanningTests` | 21/21 |
| `git diff --check` | PASS |

Raw Unity XML and log files for the passing runs are archived in `docs/validation/P20B/P20B-focused-20261004-e93731c.zip` (SHA-256 `7F2C43F12B0C80DF216B3B1923A3A00060785BB42B8C51675FA3828FD1181675`). The focused suites ran on the committed executable tree; the later change was documentation-only.

## Remaining boundary

P20-B still requires the selected `UnityBootstrap-Daily-v1` fail-closed admission/inventory hook and its negative test before a complete P20 integration candidate can be claimed. That hook shares the bootstrap/admission hotspot being handled by P10; this progress commit deliberately leaves it untouched pending that integration boundary. ALL EditMode and official Smoke have therefore not been run for this incomplete integration candidate. No P20-B independent exact-tip implementation review or canonical promotion is claimed.

P20-B remains limited to the reviewed two-Person, one supported civil-segment proof and its P18/P8 ownership boundaries. This commit does not add Party/Group semantics, automatic progression, save/load, or broader P12 readiness.
