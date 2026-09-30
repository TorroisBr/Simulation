# P12-E Institution and Office Census Candidate

**Status:** Implemented, validated, and independently reviewed; canonical
promotion has not occurred.

**Branch:** `codex/phase12/P12EPropertyCensus`.

**Code tip:** `65ebc7f7ab6dd834e2326ff426600483a74c162c`.

**Exact candidate tip:** `35ec9887add812922908d1f402d02d5db3500d44`.
Independent integration review passed on that exact tip against canonical
base `69f456d5e3c6d6f7e4b85b36e98968ced0549bf3`; the only commit after the
code tip is documentation-only. The review found no code, scope, or
revalidation findings. It did not rerun Unity tests; the exact code tree's
validation results are listed below.

**Design:** `PHASE12_P12E_INSTITUTION_OFFICE_CENSUS_DESIGN.md`, proposed at
`e22b8b3` and design-reviewed at `db36087`.

## Delivered surface

The selected `UnityBootstrap-Daily-v1` profile publishes four required,
passive schema-v1 sections over the exact installed owners:

| Section | Owner | Cardinality |
|---|---|---|
| `p12e.institution.records` | installed `InstitutionStore` | institution records |
| `p12e.office.records` | installed `OfficeStore` | office records |
| `p12e.office.incumbencies` | installed `OfficeStore` | active incumbencies |
| `p12e.office.tenures` | installed `OfficeStore` | all retained open and closed tenures |

`InstitutionStore` and `OfficeStore` now expose read-only counts and local
revisions. Successful register, assign, vacancy, and clone-time historical
tenure commits advance their owner revision once; revision capacity is
preflighted before mutation. Vacancy advances the Office revision even though
the closed tenure replaces an open row and the tenure count stays constant.
All three Office sections share the installed Office owner identity and
revision. Runtime construction continues to replay existing owner operations;
clone revisions are local post-composition baselines, not copied source
stamps.

The focused tests cover normal commits and rejected operations, same-count
vacancy invalidation, populated clone identity/source preservation, and a
stale recognition transition. The stale transition is proposed for the first
incumbent, then applied after vacancy and successor assignment; the rejected
`StaleIncumbency` result leaves all four cardinalities and both owner
revisions unchanged.

## Validation

Validation used `Tools/UnityValidation/Invoke-UnityValidation.ps1` in the
candidate worktree. The test tree is identical to code tip `65ebc7f`.

| Gate | Result | XML |
|---|---:|---|
| `InstitutionOfficeCensusTests` | 2/2 | `EditMode-20260930-022307-3917efac45264928b65ad10a316c6ddd.xml` |
| ALL EditMode | 1985/1985 | `EditMode-20260930-022332-fac9c5647dd44107a69f3ccb03adc111.xml` |
| Official complete `Smoke` filter | 5/5 | `EditMode-20260930-022408-ed87098a111341b4bb989c67538c5346.xml` |
| `git diff --check` | PASS | clean |

The prior validation of the same implementation tree also passed the
`Institution` filter 46/46, `PoliticalSuccessionIntegrationTests` 13/13,
selected-profile bootstrap 1/1, and `Property` filter 23/23. The stale-apply
case was subsequently added and is included in the focused and full-suite
runs above.

## Limits

This candidate supplies passive owner/cardinality and local revision evidence
only. It does not connect commits to the shared mutation epoch, prove
owner-thread/quiescence, register a complete profile census, provide export
or staged hydration, complete P12-B, or make P12-A `READY`. It adds no
institutional gameplay semantics and does not change the accepted P12-A
authorization gate. The candidate has not been promoted to
`codex/phase12/canonical`.
