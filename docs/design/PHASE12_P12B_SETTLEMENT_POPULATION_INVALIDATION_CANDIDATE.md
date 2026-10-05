# P12-B Selected-Profile SettlementPopulation Lifecycle Candidate

**Status:** `SUBMITTED` for independent exact-tip implementation review. The
bounded design review passed and the implementation is authorized under the
accepted P12-B prerequisite-capability scope.

- **Code commit:** `78a43fcca1d2a96e95f45a2909945882c7be7487`
- **Code tree:** `e7335734cc72274a2f9da208517de40333f4c00d`
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
bearing Person death, conflict injury, enabled daily demographic writers,
restore compensation, and unclassified external writers remain outside this
slice. Phase 12 remains open.

## Exact-tree validation

All validation below ran against code tree
`e7335734cc72274a2f9da208517de40333f4c00d` with Unity `6000.3.9f1` and the
repository `Tools/UnityValidation/Invoke-UnityValidation.ps1` harness.

| Gate | Result | XML SHA-256 | Log SHA-256 |
|---|---:|---|---|
| `P12PopulationLifecycleInvalidationTests` | 8/8 | `BB6C3419F4371CD4F35C36A50B57F5B8222F34988FAF80751645DD483BFB7807` | `FE8EF4D4A32586AF33F88D60C9F54BD89D6A890162792A9179D977787029396F` |
| ALL EditMode | 2343/2343 | `B6A3B5C8FB287C524E5AD3B5D632652B3ACF18A652788269E20DE38CD5B2936C` | `F9075FAE241C7C0B9583BFADD53CDE2BC896857470D114A2722C1E9833F20D3A` |
| Official Smoke (`EditMode -TestFilter Smoke`) | 5/5 | `98C9A89A75479087499282EAF2E4EAE7FCC2D4AFCCA10A22220A8D2D2DB905B6` | `15DBC99CD4BA71D0FA19163CF8F4F434BE809BD907AF86CD3A98595EC6916112` |
| `git diff --check` | PASS | — | — |

The matching artifacts are retained at:

- `docs/validation/P12B/population-lifecycle-20261005/focused-after-direct-gates/`
- `docs/validation/P12B/population-lifecycle-20261005/all-final/`
- `docs/validation/P12B/population-lifecycle-20261005/smoke-final/`
- Original Unity logs are stored in `docs/validation/P12B/population-lifecycle-20261005/logs-final.zip` (SHA-256 `AAD357900B7736BA24912995601F81829EE47D18A29C3912B4AE848D2CF47306`); the table records each extracted log's SHA-256.

The unrelated ProjectSettings edits and pre-existing untracked `.meta` files
were excluded from the candidate.
