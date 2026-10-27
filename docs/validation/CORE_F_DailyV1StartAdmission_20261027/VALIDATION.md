# CORE-F Core-owned initial slot admission

## Candidate and scope

- Candidate branch: `codex/core/CORE-F-runtime-start-admission`
- Implementation commit: `2c7de0c66b09cc415ee5419332822661c3df908d`
- Implementation tree: `5bb8562903e9ecabbf23df60df3c5551efe6da56`
- Base commit: `f054622b75ffbcd4b932f512f675a6df485193df`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Prior exact-tip review of the base: `NEEDS_CHANGES`, durable record `4e0a518dc49621aa9c9406a41045921c44eeebe6`

This bounded successor closes that review finding and moves the initial P12 slot-presence inspection into `DailyV1Application`. Genesis is permitted only after a successful `NoSave` result. A present slot, invalid/unprobed call, storage failure, or mismatch of the probed storage root/file-operations instance keeps genesis blocked. Loading uses the exact root and file-operations object previously inspected. The Unity bootstrap delegates slot inspection to Core and only derives its compatibility descriptor after Core reports a present slot.

The Core regression coverage now includes missing-slot genesis, corrupt-slot rejection, invalid and unprobed load attempts, inspection failure, and binding the no-save proof to the inspected storage owner. Existing assertions and P12 semantics were retained.

This does not complete `DailyV1Application.TryStart`, move the entire restore coordinator into Core, establish a detached .NET host, or close CORE-F. Unity still builds the current profile/compatibility inputs and the existing restoration path remains a separate integration dependency.

## Implementation identity

| File | Git blob | SHA-256 of tested worktree bytes |
|---|---|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` | `0e54ebe51f8995d2b863c13e6ae532266f0ce90f` | `e77ce49ba5957ce386eef90c0010fee2215b0e0ed2abed9cda98b38c29293745` |
| `Assets/_Project/Scripts/TesteSimulacao.cs` | `5f64ae2eb69fc8639b79744672adf9f7a16dd933` | `d565379bc31d9b016e75e5a8d61ec5f66fbd723c6f945e0b4014ef6542219593` |
| `tests/Simulation.Core.Tests/DailyV1ApplicationHostTests.cs` | `8c5d7d4b8f9826663c1e345b3929fdc97193cabd` | `bed84a860c57ab2c8126202b058ca0ac6698ac206f50224fe13afbb7c55f99fb` |

These three files are the complete implementation commit. Local `ProjectSettings/EditorBuildSettings.asset` and `ProjectSettings/ShaderGraphSettings.asset` modifications remain unstaged and outside the commit.

## Validation

| Gate | Result | Evidence identity |
|---|---:|---|
| Official CoreValidation cold | 901/901 passed; zero failed, aborted, skipped, or inconclusive | Run `58a6faeca8f3436eaec62dfa3524f56f`; input digest `bf582c2ed3bf8148430f3ace6dfc91b81115dbbf2499cbacd893109c840f9827`; TRX SHA-256 `5f1f58d702b893816c83a01edd5c20f8ce9a89dfa6490c859469ea99d4582249` |
| Official CoreValidation warm | 901/901 passed; zero failed, aborted, skipped, or inconclusive | Same run; TRX SHA-256 `e2eaaf82f93f3cbcfe3af7061d06699449969b784fe095c5d0dad5380958b61a` |
| Full Unity EditMode | 3274/3274 passed; zero failed, skipped, or inconclusive | Run `0e03eaf76f9c4fa4a28eb96e0216a5f2`; stable input digest `c642f420dd96b45938a79c43e76d49d658fd4b7ab9c7dc81fb035bd943900ed8`; original XML SHA-256 `0380baa99448b73e0d5e1118567dd56fcc3978bf5b57f1fc14f7c9c5034553d7` |
| Filtered Unity Smoke (`EditMode -TestFilter Smoke`) | 5/5 passed; zero failed, skipped, or inconclusive | Run `04fd1a92059541ba8164fe26300fed2e`; stable input digest matches the full EditMode run; original XML SHA-256 `cad74f8fb104642d6b485ca8cde698dff9aa522524b41748f0c019f90f99a634` |
| Unity result package | Valid against protected original | Archive SHA-256 `9bfe562d2075a1231d9bf1caa57e488657f883d56bcdf98eb029aee9e6615ea6` (172879 bytes); package manifest SHA-256 `3e39d0ebed8b9d3bc25ae757abb794a450c2bada78ecc8ab056a72889104c906` |
| Focused Unity regressions | Both pass in the stable full report | `PresentInvalidDailyV1SaveFailsClosedBeforeGenesis`; `UninspectableDailyV1SaveFailsClosedBeforeGenesis` |
| `git diff --check` | Passed | Checked before and after the validation record; only the three implementation paths were staged |

Unity was `6000.3.9f1` with Test Framework `1.6.0`. The full and Smoke runs have the same stable validation-input digest. Their protected originals remain private; only normalized NUnit XML is in the verified archive. CoreValidation's original manifest/TRX/logs remain in protected local retention because its runner does not normalize or publish TRX. The cold/warm totals and hashes above are from that protected runner manifest; no raw TRX or local filesystem path is included here.

The first full EditMode attempt, run `e6da3335afd8498083aaacee3afee0e5`, had one test failure because the changed Unity error text no longer matched an existing `LogAssert.Expect`. The bootstrap log wording was restored without changing the fail-closed behavior; the exact test then passed, and the final stable full suite passed 3274/3274. The earlier failure is retained only as diagnostic history and is not counted as a passing gate.

## Remaining integration work

The application now owns the initial storage-slot decision and fail-closed rule. Continue with the coordinated `TryStart` lifecycle over the Core-owned runtime/session, then integrate the shared restoration coordinator and detached .NET host. No canonical promotion or full CORE-F acceptance is claimed by this checkpoint.
