# Core-owned P12 image admission and decode

## Candidate identity

- Branch: `codex/core/CORE-F-core-owned-runtime-application`
- Base: `3434120b1262c61e33516c4c92b9fb6ca955fa31`
- Tested code changes: the three files committed unchanged at `f41df5791dd31643b00c9edad042b21f523f6046`
- Candidate tree: `35c4dca1f6ed1b21c7e41d4c28dd31bb4a89321c`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

The Unity and CoreValidation runners captured the source edits while the checkout was at the base commit. The three implementation/test file hashes from both stable Unity snapshots match the committed candidate. The local ProjectSettings changes listed below were also present during validation, are included in the runner snapshots, and remain excluded from the candidate commit.

## Change validated

Core `DailyV1Application` now performs the selected host-identity and complete owner-census admission followed by typed P12 image decoding. The production restore coordinator uses this application-owned operation. It keeps the separate admission result so host/census failures retain their existing failure mapping and typed-decode failures retain their distinct diagnostic. The non-application legacy decode path remains unchanged. The Unity regression exercises a valid detached image through the application and verifies that a different application's bootstrap context is rejected before decoding.

This transfers one bounded persisted-image decision into Core. It does not move the complete restore coordinator, publish a public `TryStart`, establish detached .NET hosting, or complete CORE-F.

## Validation results

- Official CoreValidation, SDK `9.0.315`: cold **926/926 PASS**, warm **926/926 PASS**; run `a33e816d578e443db75489a786106420`; private manifest SHA-256 `85391CEA6FEFFA579481B51E92467C944874BF359A38F701CE5BA7EB7B7F4637`. The original cold and warm TRX reports remain private with SHA-256 `b73bf41e4f59642de106226542466f905ef539921d29553dd9a2b3e15cefe112` and `326aab48d0a7209ee47493451edabfe7c3baa543e6ad8e0768affc24b770fba8`.
- Focused Unity restore test `P12GPoliticalBindingRestoreTests.IntegratedMilitaryAndSiteKnowledgeOwnersPersistRestoreAndContinueWithExactParity`: **1/1 PASS**; run `7432b19a5c294fb2861cd1acf8fe5797`; private manifest SHA-256 `4D6464A7961BD60E05FF9C14527CE7F4268510C61C95D25C67BBF85B3B7259BD`.
- Full Unity EditMode: **3279/3279 PASS**, zero failures, skips, or inconclusive; run `996762566cbf4ebabe7d75a0c6ce8ce0`; private manifest SHA-256 `9E0ABC568ED48F1C41621F08E319011770EF341A393EA9F815D586ED05BC0A65`; original XML SHA-256 `70b540933d8f4350f72c4e8c02c794205d817206e4799a30735a83de592b48a1`.
- Official Smoke filter `Smoke`: **5/5 PASS**; run `69b2a427d2e4425faa5fdffeb6b2b855`; private manifest SHA-256 `E0305F969B434574C9B8C38155FCFDBA14E3CBF502FB4782B85E1AC4FA11983B`.
- The complete EditMode result-only package is in `UnityEvidence/`. The official verifier reported `VALID: runs=1 entries=1` against the protected run manifest. Archive SHA-256: `f0123f67653cc7f416fa22f89120b5f1d9077f74453ff3d86629cce70a24196a`; archive size 173,501 bytes; package manifest SHA-256 `778A136AD0646184D81827507ED323539B8F69DF8D7A9DF589CE3D25509DB5B1`.
- `git diff --check`: PASS for the candidate code diff.

The cold/warm TRX files and Unity original XML/raw log remain in owner-only E-local evidence directories and are not included in Git. The public Unity package contains only normalized result XML. A preliminary Unity `-testCategory Smoke` invocation selected zero cases and is not counted; the corrected project-standard `-testFilter Smoke` run above executed the required five cases. Its original empty report is retained privately.

## Preserved local changes and limits

The following pre-existing user changes were not staged or committed: `ProjectSettings/EditorBuildSettings.asset`, `ProjectSettings/ProjectSettings.asset`, and `ProjectSettings/ShaderGraphSettings.asset`. Private evidence, generated work directories, and other untracked files also remain untouched.

This candidate is not Core canonical and does not claim complete P12-G/Core-F integration. Full runtime composition, public `DailyV1Application.TryStart`, whole restore orchestration in Core, detached host, and separate-process T/T+1/T+2 continuation remain open.


## Independent review

- Exact-tip independent implementation review: PASS, no actionable findings.
- Reviewed code commit: f41df5791dd31643b00c9edad042b21f523f6046; tree: 35c4dca1f6ed1b21c7e41d4c28dd31bb4a89321c.
- Review record: INDEPENDENT_REVIEW.md.
- The reviewer confirmed the three changed-file hashes against the stable manifests, preserved the existing host/census and error-classification behavior, and verified the packaged Unity result against the exact full-run manifest.
- Review record preserves the bounded scope: no canonical promotion, full restore-coordinator migration, public TryStart, detached host, or CORE-F completion claim.