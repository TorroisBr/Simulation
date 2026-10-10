# P12-G exact-profile and retained-reference validation

## Candidate identity

- Canonical base: `codex/phase12/canonical` at `3c1575530a1d44c3932767b6e8879be2e6672dc8`.
- Candidate code tip: `b9b9ed0d9c32e5c3564fa830fa5e04aba3c3e201`.
- Candidate Git tree: `bb66d465fd29edf28fe26ef32820bdc449fe5a5f`.
- Candidate `Assets` tree: `c324e787ed987e00b359f82e9efb60551526be00`.
- Unity Editor: `6000.3.9f1`.
- The focused runs were executed against the exact tracked code later committed at the candidate tip; ALL EditMode was rerun after commit. No code changed between validation and commit.

## Scope exercised

The source guard now requires the selected Daily-v1 `p12f.expeditions` row to be Required, identify the exact live `ExpeditionStore`, match schema and local revision, and have cardinality zero before a staging attempt or staged roots. The target guard applies the same exact-zero check to the privately composed candidate after its target census and before allocator/vector/admission checks. It compares actual revisions; revision zero is not assumed.

Allocator high-water validation separately inspects `ActorChoiceDisposition.DecisionRecordId` and retained TravelParty/Expedition `OriginDecisionId` references. Only canonical values emitted by the decision allocator (`decision-` plus its `D6` numeric form) constrain the decision counter. Repeated references are allowed; arbitrary opaque strings are preserved, not looked up through `NpcDecisionStore`, and do not become identity rows.

Regressions cover the exact required Expedition owner and identity, nonempty rejection, an empty owner at revision 2, source/private-target rejection behavior, duplicate and opaque decision-reference handling, stale high-water rejection for ActorChoice and TravelParty references, source-session integrity, and valid retry. The existing broader restored-boundary tests also ran in the full suite.

## Validation results

All XML files report `Passed` with zero failed, skipped, or inconclusive tests. XML SHA-256 values:

| Gate | Result | XML | SHA-256 |
|---|---:|---|---|
| Focused `SimulationRuntimeAdmissionTests` | 97/97 | `Focused-SimulationRuntimeAdmissionTests.xml` | `A6AA962CC16AB31C0F9E4A0DD675875F9AD3FC3BA91B29F0C765C1F3BED3813C` |
| Focused `P12GDailyV1OwnerVectorTests` | 10/10 | `Focused-P12GDailyV1OwnerVectorTests.xml` | `D5C3077B5F3AB3A4487F95C7C503D3B0317B02CFE2BB8F2F6084A3670DBDA17A` |
| ALL EditMode | 2771/2771 | `AllEditMode.xml` | `32D1D2735541E549AD081DAD71D2EC4489C9B8C8D5E4655020CE172D627DDE87` |
| Official Smoke (`-testFilter Smoke`) | 5/5 | `OfficialSmoke.xml` | `A6E71C4354830DB7B72712FD0F634C3A2DA9F8C0963B2B452BE5C35C093618B4` |
| `git diff --check` | PASS | `3c1575530a1d44c3932767b6e8879be2e6672dc8..b9b9ed0d9c32e5c3564fa830fa5e04aba3c3e201` | — |

Unity logs are in `UnityLogs.zip`, SHA-256 `60E82C16405F755360DDEDEFA92F06B43355E878FC7C7CB59D6CA26CA36F9668`.

## Preserved limits

This is a P12-G implementation candidate, not a completion or promotion record. P12-G still has outstanding whole-graph rejection/atomicity and deterministic-parity obligations from its technical design; P12-A remains `WAIT_DEPENDENCY`, P13 remains `BLOCKED`, and Phase 12 remains `OPEN`. No capture eligibility, export, hydration, or downstream readiness is implied.

Unrelated `ProjectSettings/EditorBuildSettings.asset`, `ProjectSettings/ShaderGraphSettings.asset`, and untracked `.meta` files were excluded. Their pre- and post-validation SHA-256 values matched. The unrelated line-ending-only status on `P12EDailyV1OwnerPackage.cs` was not staged.
