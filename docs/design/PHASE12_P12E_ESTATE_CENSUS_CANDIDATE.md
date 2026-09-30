# P12-E Estate Census Candidate

**Status:** Implementation, validation, and independent exact-tip review
complete. P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`.

**Branch:** `codex/phase12/P12EEstateCensus`.

**Code-bearing candidate:** `ceb0940d51049cc7f44170f5d3c2ea27d5d9df60`.

**Reviewed design:** proposal `70b2de9caf3a190c81c3416448a992c9192902c3`;
independent review PASS is recorded at design-review commit
`2389c8137fc577b6d213a75ceeccb91112328797`.

**Implementation review:** PASS on exact code tip `ceb0940` against reviewed
base `2389c81`. Durable record: `PHASE12_P12E_ESTATE_CENSUS_REVIEW.md`.

## Delivered boundary

The bootstrap publishes fixed schema-v1 section `p12e.estate.records` over
the exact runtime-installed `Runtime.EstateStore`. It reports that owner
identity, `EstateStore.Count` (one retained record per EstateId), and its
existing local revision. The deceased-Person dictionary is only a secondary
index and is not counted as additional Estate rows.

The selected authored profile reports exact-zero count/revision and stable
owner identity. A runtime test explicitly opens an Estate for a registered,
factually deceased Person and observes count/revision advance from 0/0 to 1/1.
The retained Estate record and deceased-Person lookup resolve to the same
record. Duplicate EstateId with a different Person, a second Estate for the
same deceased Person, an unknown Person, and a living Person all leave the
witness unchanged. Runtime construction's internal `TryRegister` use only
populates a newly cloned store; the provider adds no live writer or
death-triggered opening behavior.

## Validation

| Gate | Result | Evidence |
|---|---:|---|
| `EstateCensusTests` | 1/1 | `Temp/ValidationResults/EditMode-20260930-013001-225f9db5ef67445e9f62777b3604861a.xml` |
| `SimulationBootstrapCompositionTests` | 14/14 | `Temp/ValidationResults/EditMode-20260930-013021-d5cde0f3a4574319aa111952ab38503b.xml` |
| `SuccessionIntegrationTests` | 21/21 | `Temp/ValidationResults/EditMode-20260930-013038-76c0b72fc0d6407e9aceacb8f5722c6c.xml` |
| `EstatePropertyFoundationTests` | 9/9 | `Temp/ValidationResults/EditMode-20260930-013107-353909729bfb47259302f6afb74ac9ba.xml` |
| `EstateInstitutionRuntimeIntegrationTests` | 5/5 | `Temp/ValidationResults/EditMode-20260930-013123-9e4d2d7b33744a00836d6902ba139a3e.xml` |
| ALL EditMode | 1981/1981 | `Temp/ValidationResults/EditMode-20260930-013228-7d076ea1d9f24f21bf2d697608a588b7.xml` |
| Official complete Smoke (`-TestFilter Smoke`) | 5/5 | `Temp/ValidationResults/EditMode-20260930-013311-3161e238791b4f2090321ffd035e2cc8.xml` |
| `git diff --check` | PASS | Candidate tree |

## Limits retained

The census does not add estate opening, succession, inheritance, or property
transfer behavior. It does not provide Estate export or staged hydration,
complete owner registration, shared invalidation, owner-thread/quiescence,
capture eligibility, or restore. P12-B remains incomplete and P12-A remains
`WAIT_DEPENDENCY`; P12-A implementation is not authorized by this candidate.
