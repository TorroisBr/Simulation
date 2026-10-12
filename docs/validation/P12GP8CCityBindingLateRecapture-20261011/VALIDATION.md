# P12-G P8-C City binding post-guard recapture validation

## Candidate identity

- Canonical base: `309b2e592cdc39224e256a992bb92cec819ebfa7`
- Test/code commit: `73a9763b2f17075eb6fe2de1aa7a3dd95d35a2c8`
- Code tree: `b218c60bddab88d45709f3e12b3f04e07b200cef`
- Assets tree: `71d4f6a5716f3e7eabb3cc36a01822b412f5d009`
- Test source: `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs`
- Test source blob: `ea16a734b9f7b85bdeeea1832bb7c3b5e86997b6`
- Unity Editor: 6000.3.9f1
- Validation date: 2026-10-11 America/Sao_Paulo (Unity XML timestamps are UTC)

## Validation results

| Suite | Result | Original XML SHA-256 | Sanitized XML SHA-256 | Original log SHA-256 | Sanitized log SHA-256 | Published gzip SHA-256 |
|---|---:|---|---|---|---|---|
| Focused P8-C City binding recapture | 1/1 | f082973a16a5b5f55f2c3d5867bcb4a8d187e5406ba5c8bef06d4e78f6a4c93b | 3f37f2007c8c9329bc6f1108879926ae17df2cf4251d2c496c70800396450333 | cd294c9739fd65c46093abe42fdd35337e315bc08772c213ffd4a5c527d53231 | 447ca0c40640053d487f981d6adbb2d2a5ab1b40cfcf8b26ca95d26eea88b1be | e130bff255aa65de049f04fc253e852896efd9828500f37dfd5708b906201821 |
| Official Smoke | 5/5 | 8fffab8570a4d06688dd73292d20535572d51ec9bd53ed1bd20fe1e369bf8b9d | 3b24c7cc15994a702cfbbeed11913768d798870771a2b922d377a64196efa3fb | 76f7d3a095eb349b1b12f2180d43facd77d846977f0f8625fb30ed8de8967cf2 | af1d298e4c9bedb74385d68f7ea01cab726472874ab0fe335b7bb8ded8c0eb7b | 1813f9494cbd176abbc04f839091f46c9c930d50acdd9151f21d2db16d510437 |
| SimulationRuntimeAdmissionTests | 197/197 | 479576134bc1e5a4c608014b3f0aa1368ccc8ead2cee1090223cdb1b92d31553 | 5c23c881054a6cdb1932cd025959f993ac40c4a47b058272ec7098b5a9329a7d | 0f4450184d8ba291ab77eae746ca7efae47feb1d75dbda8f96d9b4a40be5df0c | 43c91b59d189097ac8c6c174486a5c8ce8d77bed82f6d46bbe7d9299b4adb9f3 | 1aada0f763b07cdac8341702d68c699dc352937a4c1a9e2a230c96d6524b3d4b |
| ALL EditMode | 2876/2876 | 1ab4b9f01372b4b96c70e6b4a1f8dc5f5c8b762bf82bc62938f57d0510b3a351 | 8234949721aa061b7724709be77a8eddf5d02c4647d191604c36e7eca26b1f7e | 9e289fa7c07721a6fc81de82a1862c33496250bcbae23264021fb81af0795d91 | 49d8951dd240e14ae05cfc84c4f48805201fd9960d5a4befd357988a1a7acf89 | d6dbf404e3e1bda99fc1a30461d0cbd735d3ee12b1bfd202563f6118f0292efc |

All reports had zero failures, skips, and inconclusive tests. The focused test appears in the focused, admission, and ALL EditMode reports. `git diff --check` passed. Results were produced by the repository UnityValidation harness; Unity was not rerun after artifact-only sanitation because code/test source and validation semantics are unchanged.

## Scope boundary

At `CandidateGuardBound`, after target checks and before final target-owner recapture, the test creates a valid P8-C City-to-Location binding in the private restored candidate. It verifies the exact target owner row changes from cardinality/revision 0/0 to 1/1 while the observed shared census epoch is unchanged, and that recapture rejects before target-vector admission or publication. The test preserves the source session/token/health/projection, compares uninterrupted continuation, and verifies a later valid retry.

This is one bounded P12-G late-recapture witness. It does not claim complete target-owner or owner/epoch coverage, runtime-wide quiescence, universal rejection/atomicity, causal no-replay, all-owner parity, capture eligibility, export/hydration, P12-A/P13 readiness, or Phase closure.
