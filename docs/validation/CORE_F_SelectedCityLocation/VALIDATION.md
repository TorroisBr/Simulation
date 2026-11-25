# CORE-F Selected City Location Validation

**Base:** `0d487af641ba7854b87a116107dcf1afbc3d430c`
**Implementation commit:** `06a5ef0f86a124802f71485579981703fa007112`
**Candidate tip:** `6ff1fd4c262c4e05bb236aca29cf4373efb2d451`
**Core canonical:** `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

## Results

| Gate | Result | Evidence |
|---|---:|---|
| CoreValidation cold | 669/669 passed | Packaged cold VSTest report |
| CoreValidation warm | 669/669 passed | Packaged warm VSTest report |
| Unity EditMode | 3,254/3,254 passed | Exact-source Unity archive |
| Official Smoke | 5/5 passed | Exact-source Unity archive |
| TRX evidence adapter tests | 153 assertions passed | Run at candidate tip `6ff1fd4` |
| Evidence package tests | 65 assertions passed | Run at candidate tip `6ff1fd4` |
| `git diff --check` | PASS | Candidate commits |

The CoreValidation run is bound to source commit
`dc1e81793a3805a36eb1dce0fa64984fec86adb2`, tree
`a0f820e988e1d20308a28019350b443f783ce1c7`, with 340 inputs and input digest
`2e25c61be420c275b67cd547151cde5569c8293289219332e41ad938b8ad8e67`. That
source includes the selected-location implementation. The subsequent `6ff1fd4`
change is limited to the evidence adapter and its documentation/tests; the
production C# and test source remain unchanged.

Unity EditMode and Smoke run IDs are `f29a74fd6b614b4db58254d136aacc60` and
`3fe199c1999347859bd2ef7f3e36af0c`. Unity input snapshot digest:
`a2a639e6e0c8ef4d1da839f8d3b36d4c59f18af244e4674b6041cd630192eeb4`.

## Public result packages

- `Evidence/coref-selected-city-location-unity.manifest.json` and
  `Evidence/coref-selected-city-location-unity.zip`; archive SHA-256
  `4515ba715fb02707b02c07c7f1b2721f2e1416e4da2551e28f936f937622c840`.
- `Evidence/coref-selected-city-location-dotnet-cold-warm.manifest.json` and
  `Evidence/coref-selected-city-location-dotnet-cold-warm.zip`; archive
  SHA-256 `8621a8c8226a2a7d1c7d20e78e03d18284ccc96fbdf18095faae6e554d5d976c`.
  The official verifier reported `originalComparisonVerified`, two runs, and
  two result entries.

Raw XML, runner manifests, host settings, logs, and private source contexts
remain in protected local retention and are not published. The normalized
reports preserve result accounting and identities; the packager's verified
comparison was performed against the protected originals.

## Review and limits

Independent exact-tip review passed as recorded in
[`CORE_F_SelectedCityLocation_EvidenceAdapter_REVIEW_6FF1FD4C.md`](../../reviews/CORE_F_SelectedCityLocation_EvidenceAdapter_REVIEW_6FF1FD4C.md).
This is a bounded candidate only. It does not complete CORE-F or authorize
Core canonical promotion. Shared `DailyV1Application`, detached P12
restoration, and fixed .NET host/continuation remain outstanding.
