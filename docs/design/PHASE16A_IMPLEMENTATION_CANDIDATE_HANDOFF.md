# P16-A current-base implementation candidate handoff

**Checkpoint:** P16-A, One Passage Military Movement with Finite Supply. **Status:** current-base recomposed implementation candidate; full validation and independent exact-tip review passed. It remains noncanonical while the reviewed P15/P16 architecture-planning candidate awaits its separately required canonical promotion. The earlier P16-only candidate `d31d91a6ee44d2b678fcf393db57c315ec284746` (tree `d4dc93f9a14af1659a891252b0cdfc5034456528`) was integrated against the new P14 canonical tip. **Current integrated code commit:** `98f80648a226212cd13c37bce34d0e2d6c68574a`; executable code tree: `1abb2f83817659fa9bce8e66826f79fde34765b8`. **Candidate evidence tip:** `b1127a34cdcc1cd214f25f23e91f1118eb6f9cf5`. **Integrated authorities:** P15 canonical `5054211ad883d14fc6727416c313f1f1824679f4`, P14 canonical `06e9c30101a74bd618d3651885c489c79fe866bb`, P10 canonical `e53252de5277fd5af46bbacb8eda5ee6e74aff08`, and P12 canonical `a6572ab3d4330d81edb334ae8b4c84ca5e6b173e`. **Architecture authority:** `f6924e63d8e5731da1d33021d0361e7defe6dad7`.

## Current-base revalidation after P14-B promotion

The P16-A branch was recomposed with the current P14 canonical tip after P14-B promotion. The merge base was P12 canonical `a6572ab3d4330d81edb334ae8b4c84ca5e6b173e`; the P14/P10 updates merged cleanly into the P15/P16 candidate. The changed production files are disjoint; `SimulationBootstrapCompositionTests.cs` was the only shared source file and merged automatically. Classification: **BASE_DRIFT_ONLY**. Focused admission/bootstrap suites then revalidated the P16 negative-profile contract alongside P14/P10 admission behavior. No P14 City/P10 Ruin combined proving profile was introduced; that profile remains rejected before allocation/publication.

Validation ran on code commit `98f80648a226212cd13c37bce34d0e2d6c68574a`, tree `1abb2f83817659fa9bce8e66826f79fde34765b8`, with Unity `6000.3.9f1`:

| Suite | Result |
|---|---:|
| `P16AMilitaryMovementTests` | 20/20 PASS |
| `SimulationRuntimeAdmissionTests` | 37/37 PASS |
| `SimulationBootstrapCompositionTests` | 21/21 PASS |
| ALL EditMode | 2323/2323 PASS |
| Official Smoke | 5/5 PASS |
| `git diff --check` | PASS |

Raw XML/log hashes and the exact-tree archive are recorded in `docs/validation/P16A/P16A-current-base-revalidation-1abb2f8.md`; archive SHA-256 is `5C7C6D878EB880A5879A558AD52370144C782413F05E8F3904097E6FCDD09BFE`. Independent exact-tip review passed with no actionable findings; see `docs/design/PHASE16A_IMPLEMENTATION_REVIEW.md`. The earlier review and result table below apply only to the previous P16-only tree.

## Delivered boundary

- `ArmedForceSpatialStateStore` remains the sole P16 position owner. Its selected P16-A record combines the one-force binding, factual position, one compatible item with finite carried quantity, and the one-shot successful crossing receipt. The low-level crossing mutation is internal; callers use `SimulationRuntime.TryExecuteP16AMilitaryCrossing`.
- The explicit `P16AOneHopMilitary` composition requires the selected P16 profile and existing force/spatial authorities. Before publication, that profile binds one nonnegative target day. The operation is accepted only when `CurrentDay` equals that target; the receipt records the target boundary and sole allowed invocation order `0`. It revalidates the force, current source, registered adjacent Hexes, passage, revisions, fixed `P16A-MilitaryOneHop/v1` context, supply, and one-shot receipt before replacing the owner record once.
- Authored initial carried quantity is nonnegative, including zero; the crossing debit remains strictly positive. A zero-stock profile is valid state and a crossing rejects it as insufficient supply without mutation.
- The runtime captures its construction-thread identity for this profile, rejects off-owner-thread operations and day advancement, and uses the existing non-reentrant advance/operation lease. The lease is reentrancy protection under the current single-writer model, not a general cross-thread lock. Rejection leaves position, supply, receipt, and revision unchanged.
- This is a one-boundary daily proving operation. The P16-A composition rejects a P18 intraday profile because the current receipt records an absolute day and fixed operation order, not a sub-day logical instant/causal sequence. Timed military movement remains outside this checkpoint.
- `UnityBootstrap-Daily-v1` rejects both unmoved populated P16 state and state after a committed crossing before P12 admission can publish it. Tests assert the exact position, quantity, receipt reference, and owner revision remain unchanged; the empty daily P16 profile still composes.
- P16-A is not wired into a production Unity bootstrap/gameplay world. No new content, order UI, route planning, replenishment, automatic travel, Battle/War/occupation effects, or P12 serialization is included.

## Previous P16-only exact-tree validation (historical)

The following results ran after the final P16 source/test edits, on the prior P16-only tree `d4dc93f9a14af1659a891252b0cdfc5034456528`. They are retained as historical evidence and are superseded for current-base integration by the full revalidation above.

| Gate | Result | Unity-output XML SHA-256 | Git-stored XML blob SHA-256 | Log SHA-256 |
|---|---:|---|---|---|
| `P16AMilitaryMovementTests` | 20/20 | `AE50F89F4CDB985CDBC29A9777EDBF23C6C6C57E1E88DA029FA8C0D091FAD892` | `2C5A754D72E1FB5A8A499E80AF06338D96FFFF435DE6A1899EAF8504DAD60805` | `7134737B10D968C1E69D693010E6A51F63C32070EFB1241737E1EBA7F3E8996A` |
| ALL EditMode | 2271/2271 | `F5AF5345BD8DF80E1C03A245E1092E81F84407CD0BBC8BF60D1049FA2A597F5B` | `DCF5923CCBEA869482B93617B14DAB9C76C803000F74C996ED9A5C7076930FF4` | `2037FDFD01E5B23C61286EE78BE64239E8D21DC54ABF4B8939E73E2C9D21E918` |
| Official Smoke (`EditMode -TestFilter Smoke`) | 5/5 | `8E555F2E7805BE02092451578A012C2DB3CE426965EFFB94CFD9F911D160C5AD` | `8164DC24AFF81D4823CE0ABF23746DD42DE3C2D4D95BC26E46078E210AEA526E` | `956BADDF9E0C3A0CCE76093EF1CE1252B0CC47FF6C9089E96293610E8FB01E2D` |
| `git diff --check` | PASS | — | — |

The result artifacts are retained under `docs/validation/P16A/`. Git's text normalization changes XML line endings when storing the reports, so the Unity-output XML hashes and Git blob hashes intentionally differ. The raw Unity XML and log pairs are also retained byte-for-byte in `P16A-validation-artifacts-d4dc93f.zip` (SHA-256 `F4807D4E3D3699DD999A9A18CFF42C84C3B3E24A193B11F78992E103F702D9B3`). That archive contains all six files listed below; its extracted XML and log hashes match the Unity-output and log hashes in this table. The older `P16A-validation-logs-d4dc93f.zip` remains historical evidence and contains the three raw logs (SHA-256 `58554DE61AC0C6A335419E6CF5CDA4C43E9EEC519013159E5FA5470CFCF11305`).

- `EditMode-20261004-031836-8df50b8c57a7414097f789197c9a2c5e.xml` — focused P16-A suite.
- `EditMode-20261004-031859-8b6a796694414fb291cd52cc9b935c27.xml` — ALL EditMode.
- `EditMode-20261004-031948-0991ccc4d7c446b993b786a4041a594e.xml` — Official Smoke.

Each log is archived under its original filename inside `P16A-validation-logs-d4dc93f.zip`; extracting reproduces the exact raw logs represented by the table's hashes.

The full EditMode run includes the affected P7/P8/P12/P15 regressions. No daily-loop behavior changed, so a long-run gate is not implied.

## Limits and review gate

This candidate does not claim P12-B completion, complete owner or shared-epoch coverage, global quiescence, capture eligibility, export/hydration, P12-A readiness, or P13 readiness. P12-B remains incomplete, P12-A remains `WAIT_DEPENDENCY`, and P13 remains blocked. P16 remains open after P16-A until its Phase objective and any later checkpoints are handled separately.

The latest recomposed code candidate (`98f8064`, tree `1abb2f8`) is independently reviewed and validated, but is not canonical. The architecture candidate `codex/architecture/p15-p16-p13-p19-design` at `a2788e6400251b5ce3cf8d2269ea8a5b5fdfd4a8` explicitly makes canonical architecture promotion the next human gate before P15/P16 implementation handoff. Its architecture review record is `docs/architecture/P15_P16_NEXT_SLICES_REVIEW_RECORD.md` on that candidate branch. Preserve this implementation candidate unchanged while that gate is pending; after it clears, refresh P16 prerequisites and apply the bounded promotion workflow to the exact reviewed tree. No Phase 16 closure is claimed.
