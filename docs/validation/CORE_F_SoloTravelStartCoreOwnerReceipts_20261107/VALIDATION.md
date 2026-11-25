# CORE-F Selected Solo TravelStart P12 Owner Receipts

Candidate implementation: `6f62f6523d44bdc9dc9ad93f9e7f740ea7f3091f`

Candidate tree: `9ec1edbfb8aad143b9abadb42c55aea4acdbc2ed`

Base: `fc6a3af6349694cab3ccb0c93b18c427a479ee04`

Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

This bounded candidate moves the selected solo TravelStart operation's exact P12 census baselines and committed-write receipts for its already-declared Core owners into `DailyV1ExecutionBoundaryState`. It covers the selected actor's MoneyAccount, TravelState, TravelPlan, paired SpatialKnowledge rows, the source City's NPC-presence row when the actor is present, the RuntimeIdAllocator event counter, and SimulationRecordSequence. It preserves the one host-vector preflight, host mutation notifications, existing action behavior, and the accepted operation's section set. Each recorded owner write must advance exactly one revision; City presence removal must remove exactly the selected actor and advance its owner revision once. Unreceipted drift and multi-revision receipts fault closed.

This does not complete P12, CORE-F, the shared Daily-v1 application, detached restoration, or the .NET host. It does not claim that all owner rows for a whole runtime are Core-owned.

## Validation

| Gate | Result | Evidence |
|---|---:|---|
| Focused `DailyV1SelectedSoloTravelStartOperationTests` | 20/20 passed | Includes real `DomainEventRecorder` event/sequence allocation; exact identity/cardinality; MoneyAccount, TravelPlan, and City-presence receipts; unreceipted drift; and multi-revision rejection. |
| Official CoreValidation cold | 1,055/1,055 passed | Run `e676afe1184a4efa86921f5d893dec56`; cold TRX SHA-256 `b5f6ba2c7f8c689e1ba89ef4cc00470d42adaebd9d0d45da4c3178a4612148b2`. |
| Official CoreValidation warm | 1,055/1,055 passed | Same run; warm TRX SHA-256 `657526a937acc4d041b2a12c78ff27bd2290ea564cc1c6ca80996c4bf3f3a07d`. |
| Official Unity EditMode, all | 3,300/3,300 passed | Run `93c55b0a95204c9bacd674a0e231c38e`; Unity `6000.3.9f1`; input snapshot SHA-256 `f027aab1b0f1a39ac1debe7a9a0b75cd6d45e56eb30e668f3a7a3944b4a3a689`; original XML SHA-256 `bd800a7e9653e7f8bf815877216185ba6947f43f4dfe6b56f7343b1ad6e1e579`. |
| Official Unity Smoke (`EditMode -TestFilter Smoke`) | 5/5 passed | Run `0ef4af1aa5d1472a84cd50e7bacf435b`; same input snapshot; original XML SHA-256 `b0de7cc749b7c3922a398ce2e27277b9d250acbcbb0dd9df034651d2eec69f02`. |
| Unity evidence package | Verified | Archive `coref-solo-travel-receipts-editmode.zip`, SHA-256 `418db6cb84254bbd5d485d864192a3d94bb8bdb77ff0655b90a4f696ff3e8a45`; companion manifest SHA-256 `75a4ffdf6932a79a1dd819dd8e8ca93b830e46b7b2974842f120adfc6419dc07`. The approved verifier reported `VALID: runs=1 entries=1`; original XML and raw logs remain in protected local evidence. |
| `git diff --check` | PASS | Candidate source diff. |

The CoreValidation run preceded the candidate commit. Its stable input manifest binds both changed files to the exact committed bytes: `DailyV1ExecutionBoundaryState.cs` SHA-256 `b60eb166b71a48e571d942da77559ed29070ba9e44f065da6bbd096c68431155` and `DailyV1SelectedSoloTravelStartOperationTests.cs` SHA-256 `0de0af00c2af52f6aa75f880d1f16070fb8358ed4d48bc07275505ae7170fe49`. The Unity before/after input snapshots were stable and record the same production source hash. The CoreValidation manifest reports that this worktree had a Unity Library; these runs make no Library-independence claim.

The protected CoreValidation manifest SHA-256 is `4aeba850900677b85297d1c5ab6ce2b7b49bb9cd562f7174fb79b099de2b1079`. Its source baseline was the candidate's parent commit; the changed Core source and test hashes above were verified against the committed candidate after validation. Original Core TRX reports and Unity XML/log evidence remain in protected local storage and were not altered.

## Evidence limits

The Unity archive contains only the normalized complete EditMode report. The separately filtered Smoke original remains protected and its five identities are also included as passed in the complete EditMode report. No private machine paths, raw logs, ProjectSettings changes, or unrelated files are included in the public archive or candidate source commit.
