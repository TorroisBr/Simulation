# CORE-F selected NPC-only resident death — exact commit validation

## Exact source identity

This supplemental record resolves the source-identity question raised during independent review. Validation was rerun after materializing the committed `SimulationRuntime.cs` Git blob byte-for-byte, rather than relying on the earlier CRLF checkout representation.

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`.
- Implementation commit: `7271fe39d0a06171e3e9876f39bb19cadb9d5202`.
- Implementation tree: `bd58ccf5eef299460e9e5cf0d52b2d8051dfb318`.
- Evidence commit at execution: `bb259bc6a0e33332ceab01c0c459df23c5166cc8`.
- Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

| File | SHA-256 of committed source bytes |
| --- | --- |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `6e21ba36f3a5526400eefbeda879d3909ddfeb010aa26910c667f31f36ed666e` |
| `Assets/_Project/Tests/EditMode/Editor/P12PopulationLifecycleInvalidationTests.cs` | `e2fa88a0d5a4fc732094eae2430849c4663729cc48fb98b5838ccb22658fdd4d` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedDemographyStage.cs` | `9eba6c3fef27b0dde4f77a918913208cc065e3d7fe5e6fecb7200b84ab3f8a57` |
| `tests/Simulation.Core.Tests/DailyV1SelectedDemographyStageTests.cs` | `ad3c9a42094f633fe940242a429850ed1a78df418653b65d0f22ab24723a0c73` |

The official Unity full-run manifest records `SimulationRuntime.cs` at exactly the committed SHA-256 above (790,192 bytes), with a stable input snapshot. The CoreValidation manifest records both changed Core source/test files at the exact hashes above and reports stable inputs.

## Validation

- Official CoreValidation run `39b69948c51240049ace29582150a787`: cold 1,095/1,095 and warm 1,095/1,095 passed. Stable input digest: `fefbde2b9834ff5af3b9b2ab8add0ba32b7b98a8119b3ab9db2d75266d31b775`.
- Focused official Unity storage regression `DurableStore_RereadsAndValidatesBeforeAtomicReplacement`: 1/1 passed, run `550f073c152445cbae6ca9f7accbd630`.
- Official full Unity EditMode: 3,314/3,314 passed, run `13037e454150446dac1a72001aea6282`; the runner reports stable inputs. The exact alias-rejection regression passed, and `P12PopulationLifecycleInvalidationTests` contributes 31/31 passing cases in the full XML.
- Official Smoke filter: 5/5 passed, run `b1894518629f4478a7638247cb10267c`; its runtime source hash matches the committed blob. All five also occur in the complete EditMode result.
- `git diff --check`: PASS.

The exact-source EditMode package is in `Unity/coref-selected-npc-resident-death-exact-commit-editmode.zip`, SHA-256 `1a54fba0fa0d209cf6b6c97bc95cb3179787000a8dfd9028e515325589a4dfa1` (175,485 bytes). Its package manifest is `Unity/coref-selected-npc-resident-death-exact-commit-editmode.manifest.json`, SHA-256 `f5267049264273b6729dc5dd57e6b70d5c9a830fd39caf8e7f0a1e546bc29103`. The approved verifier passed for the run, manifest, and archive. The package contains normalized XML from the complete suite; private raw XML/logs remain protected and were not copied here.

## Environment note

An initial exact-blob EditMode attempt used a deeply nested process-scoped E: temporary root. The four failures were P12 continuation-store tests, and the maximum generated temporary-file path was 267 characters. A focused storage test then passed using a short E: temporary root, followed by the full 3,314/3,314 pass above. No production or test source was changed for this environment correction. The temporary setting was process-scoped; persistent Windows environment settings were not modified.

## Review and status

## Independent review

- Verdict: PASS. Fresh independent exact-tip review covered candidate tip `3356f4977220ef36ae15be06086ac93f941da251`, tree `976dd2c8f4475e95f9a788cd56648e51e52077a7`.
- The reviewed implementation commit is `7271fe39d0a06171e3e9876f39bb19cadb9d5202`, tree `bd58ccf5eef299460e9e5cf0d52b2d8051dfb318`.
- The review confirmed the persisted-PersonId alias rejection at prepare and commit, post-commit Unity projection timing, transition clearing on failure, and regression coverage without weakened prior assertions.
- The review verified the exact-source Unity package, 3,314/3,314 XML totals, Smoke cases, package hashes, and privacy scan. CoreValidation source/input identities were consistent with the protected run record.
- No actionable defects remain for this bounded candidate.

No Core-canonical promotion is performed. CORE-F remains incomplete.
