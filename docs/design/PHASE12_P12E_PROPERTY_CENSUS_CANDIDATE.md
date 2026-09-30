# P12-E Property Census Candidate

**Status:** Implementation reviewed and validated; promoted at cumulative
canonical tip `b889b47`.

**Branch:** `codex/phase12/P12EPropertyCensus`

**Code tip:** `b3cdeaefe0a65bfc6e9712a3b7cfadbecbf295ce`.

**Design review record:** `7ca5db3`.

**Implementation base:** Estate census candidate
`da611028c4fd9066fef740e2eda84838c7362cd2`.

## Delivered surface

The candidate publishes two passive schema-v1 census sections over the
exact installed `Runtime.PropertyOwnershipStore`:

- `p12e.property.ownership` — `Count`.
- `p12e.property.transfer-history` — `TransferHistory.Count`.

Both sections report the store's existing local `Revision`, keep distinct
cardinalities, and are `Required` profile sections when the selected census
contract is assembled. The authored selected profile currently reports exact
zero for both sections, with stable identity on repeated reads. Runtime
tests exercise registration (1/0, revision 1), transfer (1/1, revision 2),
rejected duplicate registration and same-owner transfer with no witness
change, and a populated runtime clone preserving records/history/revision
without changing its source.

No property or transfer behavior is added. This is a live cardinality witness
only. It does not implement property export/staged hydration, a connected
shared mutation epoch, owner-thread binding, general operation quiescence,
capture eligibility, or restore. It neither completes P12-B nor makes
P12-A `READY`; the recorded blockers remain.

## Validation

All runs used `Tools/UnityValidation/Invoke-UnityValidation.ps1` in this
worktree:

| Gate | Result | XML |
|---|---:|---|
| `PropertyOwnershipCensusTests` | 2/2 | `EditMode-20260930-014544-c158ece0ea944c039c80ee19d9a9dca7.xml` |
| Selected authored profile bootstrap | 1/1 | `EditMode-20260930-014608-c10c9156f1cc446498e3ffd9fec18164.xml` |
| `PropertyTransferFoundationTests` | 5/5 | `EditMode-20260930-014634-a6acd491286b43c3a0628aabd1c8d5dc.xml` |
| `EstatePropertyFoundationTests` | 9/9 | `EditMode-20260930-014650-7a9c60e156884ecc98b69abb10a69642.xml` |
| `EstateInstitutionRuntimeIntegrationTests` | 5/5 | `EditMode-20260930-014706-cb422d29a60e497797988b3da34c6026.xml` |
| ALL EditMode | 1983/1983 | `EditMode-20260930-014728-c506e7f99ee74b58a9e8d74e1d6b8aa4.xml` |
| Official complete `Smoke` filter | 5/5 | `EditMode-20260930-014807-737a5ade59ec4c8190a9e340624d8762.xml` |
| `git diff --check` | PASS | clean |

Independent exact-tip implementation review passed at
`b3cdeaefe0a65bfc6e9712a3b7cfadbecbf295ce` with no findings; see
`PHASE12_P12E_PROPERTY_CENSUS_REVIEW.md`.
