# CORE-F Justice receipt exact-zero validation

**Purpose:** validate the bounded P12-E selected Daily-v1 receipt sentinel enforcement on candidate base `61e8923b902eb96a8095ebf197c9ffb54622920b`. This does not promote Core canonical or claim CORE-F completion.

## Change under validation

- `DailyV1SelectedRuntimeOwnerCensusSource.TryCreateCoreOwnedJusticeSections` and `TryValidateCoreOwnedJusticeSections` now require the selected P18 receipt census revision to be exactly zero, matching the accepted UnityBootstrap-Daily-v1 exact-empty sentinel contract.
- The Core session-gate regression rejects receipt revision 1 and preserves the single shared opaque projection identity.
- The generic complete owner-vector fixture supplies revision zero for both exact-empty Crime and Justice receipt rows; other owner revisions remain unchanged.
- No P12 row identity, serialization, Justice owner identity, Unity compatibility identity, or host contract changed. The current Unity adapter continues to bind the selected Core Justice owner and its receipt slot to the same `JusticeSystem` object. A detached host must provide one transient identity for the same paired rows and bind it to its exact owner/receipt slot.

The architecture review `docs/reviews/CORE_F_JusticeCensusHostNeutralIdentity_ArchitectureReview_cbacf617.md` concluded PASS: the shared identity contract supports host-neutral execution without P12 reconciliation. Exact-tip implementation review PASSED for `b31970333a618900afb76c169a92c7f095ab8ffb` / tree `996d7722d753ffaaf4e19fb3ad2292672a6dfcaf`; the durable independent review is in `INDEPENDENT_REVIEW.md`. This does not represent canonical promotion or full CORE-F acceptance.

## Source identity

- Candidate base commit: `61e8923b902eb96a8095ebf197c9ffb54622920b`.
- Candidate base tree: `b142f921a2bb7fcd90c149ae1d326666146c829a`.
- CoreValidation recorded input digest: `f754776288bf9892852005a7cd77c0ab56a294ed4f34420569ec8cf992c78843`.
- Unity EditMode and Smoke both recorded the identical stable input digest `16d4f84954d7924468cbe2e689356359dbec8f2dc445662021b519912e8f1a24` before and after execution.
- Tested source SHA-256:
  - `Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs` — `e8d2354a47929edd4f4c365871d1f2171d1198062c662452ebf280d181574cef`.
  - `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs` — `e97e65477cdd8f11aa2a76a677b0c67dbbbaaa42f17e18a1e05d4a2d6d471c7b`.
  - `tests/Simulation.Core.Tests/DailyV1ExecutionBoundaryStateTests.cs` — `ad7aea7d60593627d5c18359f99c74ce53ab3cbe1aa3ec5676d4c321987d1d81`.
- The Unity source inventory records the unchanged local `ProjectSettings/ProjectSettings.asset` input hash `f0771a23c3ebcb50d651c13515ee66e6e56d321c6115f54a2193bf35dd994633`. It is user-owned, excluded from this candidate, and was not staged or changed by this work.

## Validation results

| Gate | Run identity | Result | Original result SHA-256 |
|---|---|---:|---|
| CoreValidation cold | `7ce36c45-fd2b-401a-b0af-8f258793839c` | 1081/1081 passed; zero failures/skips | `e537af5974c2cb43d0ce558e73c3c192149cd658915525d87d97644c6ca6a1f5` |
| CoreValidation warm | `32b1848a-3978-457e-86b8-ddc094df4c36` | 1081/1081 passed; zero failures/skips | `e3a8066c450e68f64ee548410c590bc6dc0dc9f2abb58ec62928e0e5e4f86a93` |
| Full Unity EditMode | `55f81753d3d84387a2afb1110f118e39` | 3303/3303 passed; zero failures/skips | `42ce09f4f043d5835666f90a6dd2c0ffaa6e23c8b7457b38db086694c77c2580` |
| Official Unity EditMode Smoke filter | `b10c31f828ca49b69da27c67f0f55e69` | 5/5 passed; zero failures/skips | `0562717853d291f62bdfa7ce079b1dc47e45be5e1a79c192c84f45740093d060` |

The protected CoreValidation runner manifest SHA-256 is `a0bd0d4f96a9685992a3d9bfd84da195389f7cd68a1dcea187d586485014f678`. CoreValidation cold/warm TRX and logs remain in its owner-only private retention and are not included here. The protected Unity manifests and full raw logs also remain private and unmodified.

The approved Unity result-only packager produced and verified the complete EditMode package against its protected original. Package manifest SHA-256: `07189f055711cb64df0757f95b85e3b25eddf50336a6e7951b121fec94fa9f62`. ZIP SHA-256: `162d1e52ce80d83f593527b0a13dd42798f3fef1a2ac24bea07a392a0f31a1ff` (174,621 bytes). The archived complete EditMode result contains all five Smoke case identities as Passed.

A separate attempt to package only the filtered Smoke report was rejected by the packager (`Suite 'Simulation' testcasecount does not match its descendants`). No result was rewritten or published from that attempt. The original Smoke XML remains protected; the complete EditMode package retains all five corresponding Smoke results.

`git diff --check` passed. This record plus the package is a recoverable candidate checkpoint, not final CORE-F promotion evidence. The private .NET TRX remains retained for current review; any later canonical promotion must satisfy the applicable durable TRX evidence contract.
