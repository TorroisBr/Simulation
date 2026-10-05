# P12-B Selected-Profile SettlementPopulation Lifecycle Candidate

**Status:** `VALIDATED_CANDIDATE` — independent exact-tip implementation
review PASS is recorded in
[`PHASE12_P12B_SETTLEMENT_POPULATION_INVALIDATION_IMPLEMENTATION_REVIEW.md`](PHASE12_P12B_SETTLEMENT_POPULATION_INVALIDATION_IMPLEMENTATION_REVIEW.md).
The bounded design review passed and the implementation is authorized under
the accepted P12-B prerequisite-capability scope.

- **Code commit:** `f6e9b1ca14e9bfbb9e8f19622272610abdf2cc01`
- **Code tree:** `e1bb56f97b0988247a092f96e9757a8bdd0e8380`
- **Independent exact-tip code review:** PASS (recorded in the linked review artifact)
- **Candidate base:** `147cf2b08e8cae2715bded228d953440c2355b22`
- **P12 canonical at refresh:** `a6572ab3d4330d81edb334ae8b4c84ca5e6b173e`
- **Current P17 runtime base included by the candidate base:** `b3f26d541fb1a7f7c5c9809877b4c5b937a53aee`
- **Architecture:** `ffd75652d89d862b83d634868c560f8540869b89`

The implementation consumes the reviewed contract in
[`PHASE12_P12B_SETTLEMENT_POPULATION_INVALIDATION_DESIGN.md`](PHASE12_P12B_SETTLEMENT_POPULATION_INVALIDATION_DESIGN.md)
and its independent review in
[`PHASE12_P12B_SETTLEMENT_POPULATION_INVALIDATION_DESIGN_REVIEW.md`](PHASE12_P12B_SETTLEMENT_POPULATION_INVALIDATION_DESIGN_REVIEW.md).

## Delivered boundary

- Registers exact per-City population aggregate and receipt witnesses, plus
  singleton Person life/residence and NPC life/residence witnesses, in the
  selected `UnityBootstrap-Daily-v1` owner inventory.
- Requires the selected profile's effective NaturalMortality and
  AggregateDemography writers to remain disabled; admission fails if either is
  enabled.
- Reserves one shared mutation-epoch increment and each participating local
  owner revision before supported writes. Immigration, emigration, resident
  death, receipt-free Person death, Person residence binding, and paired
  residence migration notify once after the complete successful operation.
- Reconciles lifecycle witnesses during existing Person/NPC membership and
  materialization operations. Unbound NPC residence and Person-owned residence
  use distinct witnesses.
- Rejects P12-bound named birth and keyed/receipt-bearing Person death before
  mutation. Direct static population, Person residence, and NPC residence
  membership writes report failure when no admitted runtime scope exists.

## Exclusions and status

This is a bounded owner/operation invalidation capability only. It does not
complete P12-B or claim complete owner/shared-epoch coverage, global
quiescence, capture eligibility, P12-A readiness, P13 readiness, export, or
hydration. Named birth remains deferred to its P12-D owner contract. Receipt-
bearing Person death, all NPC injury-severity writes while P12-bound (including
conflict injury), enabled daily demographic writers, restore compensation, and
unclassified external writers remain outside this slice.

While bound, `NpcRuntime.TryApplyInjury` returns `false` before mutation because
no injury owner section or operation is registered; unbound runtimes keep the
existing behavior. Phase 12 remains open.

## Review correction and exact-tree validation

The first independent code review requested an explicit boundary and regression for injury writes. This candidate now states that every NPC injury-severity write fails closed while its NPC is bound to the selected P12 profile, and proves the injury value, NPC life/residence revisions, and mutation epoch remain unchanged. No injury owner or operation was added.

All validation below ran against code tree `e1bb56f97b0988247a092f96e9757a8bdd0e8380` with Unity `6000.3.9f1` and the repository `Tools/UnityValidation/Invoke-UnityValidation.ps1` harness.

| Gate | Result | XML SHA-256 | Log SHA-256 |
|---|---:|---|---|
| `P12PopulationLifecycleInvalidationTests` | 9/9 | `3E5F08AC5819453E092DE0AFB09C5DF6B0757A559E493A777787321EEEFDE901` | `38ABCDBDB3A5B4DF130894EE16703003961959128CAA83917AC372746404F65F` |
| ALL EditMode | 2344/2344 | `8C08EDCBCD85DD948B864D78C0A9464F17AD73A425265696F9D0BA65EC0798F3` | `B56847243BD70948A422F28B9691CEABB615357412BCC97F059C933551FFB223` |
| Official Smoke (`EditMode -TestFilter Smoke`) | 5/5 | `AB021BE2B16357F3CCBB33E55ABBBCEC7103C9F1779AE033799EBA48130B77EB` | `1E382A7607AFA57F02539C5068000BE456BA6334A5BEE8C0B858E491B48094E0` |
| `git diff --check 147cf2b..f6e9b1c` | PASS | — | — |

XML results remain under `docs/validation/P12B/population-lifecycle-20261005/{injury-review-fix-focused,all-after-injury-fix,smoke-after-injury-fix}/`. The three original logs are retained in `docs/validation/P12B/population-lifecycle-20261005/injury-review-fix-logs.zip` (SHA-256 `DA297F05207F0366717B57EBA73C87A3F5DFB6090E7490CB9D86EC1E5956427F`); the table records each extracted log hash.
The unrelated ProjectSettings edits and pre-existing untracked `.meta` files
were excluded from the candidate.
