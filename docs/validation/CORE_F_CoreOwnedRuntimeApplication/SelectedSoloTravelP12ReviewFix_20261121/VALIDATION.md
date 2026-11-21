# Selected solo travel P12 authorization review fix — validation

## Candidate identity

- Branch: `codex/core/CORE-F-restore-stage-transaction`
- Production/test commit: `3e5d142bd1266571c3b53d53ba09b2b2eea7bd5d`
- Commit tree: `9bcf133f83145937edc58f83672f70ae99e28683`
- Previous committed integration: `1ac3b336eb99e759c39f32975171f51a1cf77400`
- Scope: correct the selected solo-travel P12 spatial-knowledge owner predicate and prove one newly discovered route is admitted through the complete census.

The validation invocations ran on the exact final production and test bytes before the production/test commit was created. The official CoreValidation and Unity input snapshots captured those inputs; their SHA-256 values were compared with the resulting committed file bytes. Only validation documentation and the normalized Unity result package were added afterward.

| Executable input | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1ExecutionBoundaryState.cs` | `8f5a410397b77d22121f6c3b0c2e2dacd038ba57095fd4aafe29e8ce6477bd83` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedActionExecutionStage.cs` | `1995c5f74ef23d335e7fa39a9aad798ac737fc19386011195e8add28dc580119` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedTravelDayStage.cs` | `0b120264c976cb7c4c1f9b5b26b71329f1d8948d20cc7f5fb961d26be4998b08` |
| `tests/Simulation.Core.Tests/DailyV1SelectedDailyExecutionStageTests.cs` | `c2e9aba0cdc7429488f7c087e2412a63af04db6abebfdad35acedf6d8915f3fb` |

## Validation results

- Focused `CoreOnlyActorTurnExecutesAdmittedActionWithCompleteP12Census`: **14/14 PASS**.
- Official CoreValidation: **1,185/1,185 cold and 1,185/1,185 warm PASS**. Run `18c08a36c3314a059e03eba64ea6924a`; protected native manifest SHA-256 `a3b5d2e77bf23c36c8c125cef7b1e230abbe3470dd1c4466e0f00dc337a37bc9`; original cold TRX SHA-256 `5aeabf8e07d8a227a812338bb84df2a152b82a22a66c101385b85bd8c1067d33`; warm TRX SHA-256 `e1bde39ca81a5bf2597d33695d028165e2989128f191751bcf2eb4629a7040bb`.
- Full Unity EditMode: **3,325/3,325 PASS**, no failures, skips or inconclusive cases. Unity `6000.3.9f1`; run `27b278e914404b1fa3eee0f5998aad7e`; stable input snapshot; protected manifest SHA-256 `8a31a27c7e0ca77455bf0ada43391064392fb906618f51e6362208857124e246`; original XML SHA-256 `dd41418eadedb1f4c62ac934bd7f3fd3191baaef1fa117848df7d30eec4cf9df`.
- Official filtered Smoke: **5/5 PASS**, using the documented EditMode `-TestFilter Smoke`; run `b670703932944c09be0ea478f1a68f82`; protected manifest SHA-256 `567c86410b2a3b9e770044db70ea70934cfc1a9ca510111ce33efc446f6a3de7`; original XML SHA-256 `c4558bf9da2b7f81ff0784020d21d2f0bdb7976a91efc6b14fdf007a8b19e52f`. All five Smoke identities also occur in the full EditMode result.
- A separate `-TestCategory Smoke` diagnostic selected **0** tests and is excluded. It is not represented as a passing gate.
- `git diff --check`: **PASS**.

## Normalized Unity result package

- [Full EditMode normalized result manifest](UnityEvidence/coref-selected-solo-travel-p12-reviewfix-20261121-editmode.manifest.json) — SHA-256 `6f094f8678499ced2a1ccad719324347422ff8ca33ba9b470f5317ed3660d4c6`.
- [Full EditMode result-only archive](UnityEvidence/coref-selected-solo-travel-p12-reviewfix-20261121-editmode.zip) — SHA-256 `41797c63722b466346fd39020bcbbe79ecd963389a907ed74d465381fa9bf736` (176,120 bytes).
- The official verifier returned `VALID: runs=1 entries=1` against the protected original run manifest. Original XML and logs remain protected in the local evidence store; no original report was modified.
- Native CoreValidation originals remain protected; no incompatible TRX package was manufactured.

This checkpoint is not a Core canonical promotion or CORE-F completion claim.