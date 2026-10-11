# P12-G successful ScheduledDirective + active TravelParty parity — validation

Date: 2026-10-11

## Candidate and scope

- Canonical base: `ab7c4b4e8347546c6bd8f4353d98353176b99807`
- Code candidate: `31241dab2e81d036b63f1b7a7d839f9d94f903e4`
- Candidate Git tree: `080355aa35d8ebe4ccc6975441b47f59a7989c5d`
- Candidate `Assets` tree: `7b8414ea93f73eef9a81a4f2a470035d79e0467d`
- Code diff: one test file, `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs`

The added test is `SimulationRuntimeAdmissionTests.DailyV1RestorePreservesSuccessfulScheduledDirectiveEffectWithActiveTravelPartyAcrossBoundaries`. It uses the normal supported flow: Jobson commits Steal, a Guard performs Arrest, two other eligible NPCs start a TravelParty, and restoration occurs at the healthy completed day-2 boundary while the sentence, pending day-3 directive, and party are present. On day 3 the due EscapePrison directive succeeds while the party remains active. The test compares the full included-owner projection and continuation roots across the source, uninterrupted, and restored runs; day 4 confirms the terminal directive does not replay its bounty effect. No gameplay state is manually injected and no production code or profile contract changes.

This is one bounded continuation-parity case. It does not close P12-G, establish universal no-replay or all-state parity, or imply complete owner/epoch coverage, global quiescence, capture eligibility, P12-A readiness, export/hydration, P13 readiness, or Phase closure.

## Final validation

Each run below is from the code candidate above. XML reports show zero failures, skips, and inconclusive tests.

| Gate | Result | XML SHA-256 | Compressed log SHA-256 | Decompressed log SHA-256 |
|---|---:|---|---|---|
| Focused combined-parity test | 1/1 | `FA1BF1F8A3D9DF839338BE9FCFFF9ED066B564938AAAB56A5218F36047BA41A5` | `39E46A6CEEEA994615156226F80F594887366857B44996EA5E258834B5DC3E3E` | `202B9B0E2B0D5778F2D9CE75A2448D45C928A43AEA1F8F0FA437F564019370B5` |
| `SimulationRuntimeAdmissionTests` | 193/193 | `69E68E37AF7399C7513258FEDFBAE1FB95CA6DA186E78915B3A02E242C14AA75` | `B771A1EDF13629CE9F8516F3C1D923D9FE9557EACEE5B5618423B56BA715AC9A` | `24F28D656A9950CD662078D4AC1E0E659C0546E643D026B902E892EE89335A14` |
| ALL EditMode | 2867/2867 | `DABE0D9B1ACF767C9026B11EFEDBF8E63CE6BDAC0EFC3A70BC2DA55EDE74EAFA` | `9FA4F7A578D542E000CC3137F4550217FB1D888271DBB45EC72903600CE3C1A4` | `1D221F415E17A9CB2079397408905EF5044419FB1525537D3720E65949DE4F1A` |
| Official Smoke | 5/5 | `62E407098B806B318B5315D45E199214AD85A2EE217A30343828C08AC89E2E89` | `6D8A88677A8C80557E58E2ED4A457B394578305AA7AF8FAFD96E4909042789BB` | `D4B441D3D140752497383F4B898BB82CFEB65BDEAB5900B91B6145313A120DAB` |

Artifacts are retained in the sibling `Focused/`, `Admission/`, `AllEditMode/`, and `OfficialSmoke/` directories. Only these four successful XML reports and compressed logs are included here; exploratory/failed runs remain outside the final evidence set. `git diff --check ab7c4b4e8347546c6bd8f4353d98353176b99807..31241dab2e81d036b63f1b7a7d839f9d94f903e4` passed.

## Phase boundary

This test-only promotion advances one supported P12-G continuation-parity fixture. P12-G and P12-A remain `WAIT_DEPENDENCY`; P12-B through P12-F remain promoted within their recorded scopes; P13 remains `BLOCKED`; Phase 12 remains `OPEN`.

## Publication artifact path sanitation

Incidental absolute Windows local filesystem roots in the retained XML/log artifacts were replaced with `LOCAL_PATH_REDACTED`, `PROJECT_ROOT`, or `UNITY_EDITOR_ROOT` tokens. Test-result content and all reported counts remain unchanged. See [SANITIZATION.md](../../P12GSanitizedPublication-20261011/SANITIZATION.md) and [PATH-NORMALIZATION-2.md](../../P12GSanitizedPublication-20261011/PATH-NORMALIZATION-2.md) for both artifact hash mappings. Unity was not rerun because the reviewed executable and test-source trees are unchanged.
