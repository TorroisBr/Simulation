# P12-B Selected-Profile SettlementPopulation Lifecycle Implementation Review

**Verdict:** `VALIDATED_CANDIDATE` — PASS on exact code tip.

| Item | Exact identity |
|---|---|
| Actual candidate base | `147cf2b08e8cae2715bded228d953440c2355b22` |
| Reviewed code commit | `f6e9b1ca14e9bfbb9e8f19622272610abdf2cc01` |
| Reviewed code tree | `e1bb56f97b0988247a092f96e9757a8bdd0e8380` |
| Candidate evidence tip at review | `3357cad24e2c50fe48c07766865e4c9064798388` |
| P12 canonical at review | `a6572ab3d4330d81edb334ae8b4c84ca5e6b173e` |
| Architecture baseline | `ffd75652d89d862b83d634868c560f8540869b89` |
| Current P17 canonical in the candidate base | `b3f26d541fb1a7f7c5c9809877b4c5b937a53aee` |

**Independent reviewer:** Luna-first reviewer `/root/p12_population_code_review`; exact-tip review completed 2026-10-05. The reviewer did not edit the candidate. The review covered the complete base-to-code diff, the current architecture and both accepted alignment records, the P12 Brief/State and reviewed technical design, exact validation artifacts, owner cardinality/revision behavior, operation boundaries, failure/compensation paths, determinism, and exclusions.

## Review finding and resolution

The initial review of code `78a43fcca1d2a96e95f45a2909945882c7be7487` returned `NEEDS_CHANGES`: `NpcRuntime.TryApplyInjury` rejected calls while P12-bound without an explicit documented contract or regression. The candidate was corrected at `f6e9b1c`. It now states that injury severity has no owner section or operation in this bounded P12 profile, returns `false` before mutation while bound, and retains normal behavior while unbound. `P12BoundRuntimeRejectsUnwitnessedNpcInjuryWithoutMutation` verifies injury severity, NPC life/residence revisions, and mutation epoch remain unchanged. No injury census or operation was added.

The reviewer re-read the complete `147cf2b..f6e9b1c` diff and returned `PASS` with no remaining scoped correctness findings. The corrected boundary is within the reviewed exclusions; it does not widen this population lifecycle slice.

## Validation reviewed

All results correspond to the reviewed code tree `e1bb56f97b0988247a092f96e9757a8bdd0e8380`, using Unity `6000.3.9f1`:

| Gate | Result | XML SHA-256 | Log SHA-256 |
|---|---:|---|---|
| `P12PopulationLifecycleInvalidationTests` | 9/9 PASS | `3E5F08AC5819453E092DE0AFB09C5DF6B0757A559E493A777787321EEEFDE901` | `38ABCDBDB3A5B4DF130894EE16703003961959128CAA83917AC372746404F65F` |
| ALL EditMode | 2344/2344 PASS | `8C08EDCBCD85DD948B864D78C0A9464F17AD73A425265696F9D0BA65EC0798F3` | `B56847243BD70948A422F28B9691CEABB615357412BCC97F059C933551FFB223` |
| Official Smoke | 5/5 PASS | `AB021BE2B16357F3CCBB33E55ABBBCEC7103C9F1779AE033799EBA48130B77EB` | `1E382A7607AFA57F02539C5068000BE456BA6334A5BEE8C0B858E491B48094E0` |
| `git diff --check 147cf2b..f6e9b1c` | PASS | — | — |

The XMLs and compressed original logs are retained under `docs/validation/P12B/population-lifecycle-20261005/`; `injury-review-fix-logs.zip` SHA-256 is `DA297F05207F0366717B57EBA73C87A3F5DFB6090E7490CB9D86EC1E5956427F`. Exact artifact identities are in the candidate manifest.

## Scope and integration limits

This is a bounded SettlementPopulation/person/NPC lifecycle owner-invalidation capability. It does not complete P12-B or establish complete profile owner/write coverage, complete shared-epoch coverage, global quiescence, capture eligibility, export, hydration, P12-A readiness, P13 readiness, or Phase 12 closure. The target P12 canonical was `a6572ab3d4330d81edb334ae8b4c84ca5e6b173e` at review; the candidate is a descendant and must pass refreshed promotion preflight.