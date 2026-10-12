# P12-G P12-D ExplorableSite post-guard target recapture validation

## Candidate identity

- Canonical base: 4ab594d35e0d8a02fe29335e696d69aeb1f2daef
- Test/code commit: 3707f5593fd9480b00957402f37e039ea3ffc71c
- Code tree: f577bce3393d719e9e1f9234d9e3bc8e2e3c0b06
- Assets tree: fbb9ad14489530ab126db075bedc01d04a6d7907
- Test source blob: 8ab47743932d04787f7f15505cc60ec497077c1a
- Unity Editor: 6000.3.9f1
- Validation date: 2026-10-11 America/Sao_Paulo (Unity XML timestamps are UTC)

## Validation results

| Suite | Result | Original XML SHA-256 | Sanitized XML SHA-256 | Original log SHA-256 | Sanitized log SHA-256 | Published gzip SHA-256 |
|---|---:|---|---|---|---|---|
| Focused P12-D ExplorableSite late-recapture case | 1/1 PASS | 72a2775fdfc585ed7259dd1814745a05074d0f01f54d82d4394ec06736c95a48 | 85df8d10bdec7184e0b8eb15b63d8a86572ce3b2c7c2b5bb0e219df12710e395 | 600570f9ed86e05ec134bdc03c4f40b6356f956406e7bf86d22e01b058d10755 | 05a230314f078cfc775fe504b661f5cf9634d383bea70a0cf50d3ee303d2166f | a101b8b6bc15aafe7a40145da14317d963ccac57ad14070297c2c3308f4e1e1c |
| SimulationRuntimeAdmissionTests | 198/198 PASS | ab6a7ca3b9715442718c7b219b88c9ebb2b2e691c9c32e727369c177117eb1db | 153374d823cf5f4f0fb7c1140af6c0865ae384a886bfb19bb811c08bffc330f6 | ff804375fb8b5d726a4f0c2bc3a571cdc8facec24e8ecc880616d19c4b8a2750 | 9a1469809c57807ccd697a37a64f940d3e75b5bc76a0731d666af8a85ca637fc | f57505967ad45625936cbae1f833ea82f3a21b2b9d2282c322bd96df1ab858d7 |
| ALL EditMode | 2877/2877 PASS | fa7167e1938fe96b22be2a51a8b26344ea7a6ae33a8dfa76823900abc52fb3a1 | 96b099a24a1bb894ad07f8394394f042679975f2d7a124f271b6bb7c3acaf793 | a1670f3136754ecd0946c9f48847bcc1c381be7b94dba0bb2e675b8660aa4c26 | 544e3cc9d3c1a9386e08f7e7b377a2474350ab347e2965e8ed8ad6b7c1ab2a22 | c6a1055e9f437683c3b3a748ee61d3697ca0c17c4c5ec66bbab28e76cd6665fc |
| Official EditMode Smoke | 5/5 PASS | 536e3fe990d64a86058cdd6867ad93c24948fcad3d1bb6e70ad1e7cba318795c | 3d9c24945a31c3d92d31f4580569c9f2582e9868e5f49fdc758eda43eaa63517 | 748451766cb75933811b8dc30e3e4c6ab4062c948e47806e6f5532d3b9773b13 | 7ca3379e40df53ab31ad99d1ab477ab02d3d1c8044d58a9d2db165bf0fd2b2fc | ba64fbbc512554092c6b5a50d889fd24c0bdccfecd7e100af48c94ded3163679 |

All four reports had zero failures, skips, and inconclusive tests. The focused case appears in the focused, admission, and ALL EditMode reports. git diff --check passed. The sanitizer verified that inverse token substitution reconstructs the raw XML/log bytes in memory. Git stores XML as text and normalized its CRLF line endings to LF; the published sanitized XML SHA-256 values below refer to those exact Git blob bytes. This line-ending normalization does not change XML content or test results. Compressed logs are binary and retain their exact sanitized bytes.

## Scope boundary

At CandidateGuardBound, after restored-graph checks and before final target-owner-vector recapture, the test uses the supported ExplorableSiteStore.Add writer to add one synthetic Generic site referencing an already-staged legacy location. It proves the exact p12d.explorable-sites target row changes from cardinality/revision 0/0 to 1/1 and is rejected as TargetOwnerVectorFailed / OwnerCoverageIncomplete before target-vector recapture or boundary admission. It also verifies that the source session, completed-boundary token, source site owner, and complete source projection remain unchanged; a valid retry reconstructs the same graph and continues in parity with an uninterrupted control.

This is only a bounded rejection witness for the explicitly-empty Daily-v1 site owner. The fixture does not add sites to the selected profile and does not imply site export or hydration, P12-G completion, complete owner/shared-epoch coverage, runtime-wide quiescence, universal atomicity, P12-A/P13 readiness, or Phase closure. No production behavior changed.
