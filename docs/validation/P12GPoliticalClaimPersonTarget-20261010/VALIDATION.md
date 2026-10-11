# P12-G PoliticalClaim Person-target Missing-reference Rejection Validation

## Candidate

- Canonical base: `f31e4aefc89d032aec66a274378a829a90ce1fff`.
- Implementation commit: `c90311baf547d8e59c68fa74dd4c055c24c9c71f`.
- Git tree: `bf7c9a5b331a085e968a27b6f8e67fe4f4736b35`.
- Assets tree: `3011133ef4f7941d9babf632eaa6e7756167ad0b`.
- Changed implementation path: `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` only.

This test-only case starts with a valid `StatusRecognition` claim whose Person target and claimant are both registered. At the detached PoliticalClaim snapshot boundary, it changes only the well-formed `TargetId` to a distinct missing Person identity. The existing P12-E validator rejects after earlier E owners/Faction stage and before PoliticalClaim owner staging or publication. The case verifies source session/token/health/facts/roots/owner graph preservation, continuation parity, a successful untouched retry with the original target identity, and parity after another completed boundary. No production seam or domain behavior changed.

## Validation

Official runner: `Tools/UnityValidation/Invoke-UnityValidation.ps1`, EditMode, on the project at the candidate worktree. All reported result XMLs parse with zero failed, skipped, and inconclusive tests; compressed logs decompress to the raw-log SHA-256 below.

| Gate | Result | XML | XML SHA-256 | Compressed log | Compressed SHA-256 | Raw log SHA-256 |
|---|---:|---|---|---|---|---|
| Focused case | 1/1 PASS | `EditMode-20261011-003354-32b661a07f15466e9fec91ba45268aac.xml` | `AB05BD1A6D9F028AA1A4B93442CD6EB4DA38F436DF451DC848AD561C4F5BE528` | `EditMode-20261011-003354-32b661a07f15466e9fec91ba45268aac.log.gz` | `33489FC324C624DC7A232D736B2EF928EDF2C7DDC48285EEF55C339B0F4BD19D` | `3F7382D205E69E640119EBCBAB97EDFEA0924268050E6EA44A7C2F512D541764` |
| `SimulationRuntimeAdmissionTests` | 185/185 PASS | `EditMode-20261011-003449-f634ba52de124985b6d216eb9bff5ea8.xml` | `4F8594A7A20E15885D3316F4454CB88003336C2587D133CFDEA52BCA062002B7` | `EditMode-20261011-003449-f634ba52de124985b6d216eb9bff5ea8.log.gz` | `AE7183FB44620CFA658FE72F8623ADAC164E7C18528E4D99C4F2464FE92A8659` | `B32DA5DA542D1E0BD8BE0091F13B586FC767E1D2128408B033D6A76EAC401648` |
| ALL EditMode | 2859/2859 PASS | `EditMode-20261011-003527-287d44e557654182a8790ae75030e70e.xml` | `469362AA5F58AF12ABE1DB34E8569601200DA68FE8F301B06C966F4BA220780D` | `EditMode-20261011-003527-287d44e557654182a8790ae75030e.log.gz` | `22268A4C821C0CF6503C4AB96772AAC47694990409A0DB3F7BD36C50429C565F` | `B9808D78E469629A5D1794D114A0305D3EE9150802BE0DED392A3CAC9CF962E0` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 PASS | `EditMode-20261011-003625-11e393358fc245bda9d51e7c7974137b.xml` | `64CD02A52131F960931DE7709C4380C273438A521FBC6D2C4A3C0331D81DB242` | `EditMode-20261011-003625-11e393358fc245bda9d51e7c7974137b.log.gz` | `B3F61305D614DFAA61EBD6CB8657FCE49143E7D34CC36282C900445523FDCC8D` | `E65C8F035E567B5BEAE4C56D88E4F17FF898BE53A32C98EF1732B1AF27C87B9A` |

`git diff --check f31e4aefc89d032aec66a274378a829a90ce1fff..c90311baf547d8e59c68fa74dd4c055c24c9c71f`: PASS.

These results validate this bounded test-only candidate. They do not establish P12-G completion, exhaustive owner/epoch coverage, runtime-wide quiescence, full causal no-replay or multi-boundary parity, capture eligibility, export/hydration, P12-A or P13 readiness, or Phase closure.