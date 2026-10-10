# P12-G daily gameplay non-invocation witness — validation

**Scope:** add a thread-local, test-only observer at entry to `SimulationRuntime.AdvanceDayAfterClockAdvance` and assert that a successful P12-G restore does not enter the normal daily gameplay callback pipeline. The observer is inactive outside the scoped test. The existing P9 genesis probe remains separately asserted as zero. This proves no entry to the day-level gameplay pipeline during this successful restore fixture; it does not prove absence of every direct domain callback, every failure path, or all possible execution threads.

**Base P12 canonical:** `76dbe90838c2ea020e7c9cd5286752fb701df032`  
**Code candidate:** `f2c54e66c22a4528cef2edd411c1f6e8bf92cbdd`  
**Code tree:** `d167791a91905e6206098373e6481d2b045320f9`  
**Assets tree:** `a406235024c991e986e98749c1b0736306d250c1`  
**Changed code:** `SimulationRuntime.cs` and `SimulationRuntimeAdmissionTests.cs`  
**Unity project version:** `6000.3.9f1`; runner: `Tools/UnityValidation/Invoke-UnityValidation.ps1`.

| Gate | Result | XML artifact | XML SHA-256 | Compressed log | Compressed SHA-256 | Uncompressed log SHA-256 |
|---|---:|---|---|---|---|---|
| Focused `SimulationRuntimeAdmissionTests` | 175/175 PASS | `Focused/EditMode-20261010-205019-2873b2ba8dd74d7bb9bbb3aa4ad7ca79.xml` | `8C20197A440FD4FC543C4EB60C8452C7AEF0720EE88CA91AF9C5AA8136E7410B` | `Focused/EditMode-20261010-205019-2873b2ba8dd74d7bb9bbb3aa4ad7ca79.log.gz` | `7B37B6D9D4C955EC57EBB0CD56ABC79AB576C2BD1B789B4C9730BC2C185373A7` | `9823139BED9AA5A28C2125881E83856491C704FAC70B0B017C699C22BCF9EEB6` |
| ALL EditMode | 2849/2849 PASS | `AllEditMode/EditMode-20261010-205204-2730364d513645da9ee5df61f01f8920.xml` | `E7700C68AE44679F87EA7A529D54C68580242C22C173F55385F0926495D3FB58` | `AllEditMode/EditMode-20261010-205204-2730364d513645da9ee5df61f01f8920.log.gz` | `1C2165F7F908345AEEDED852DCB701BBE7895396B5DFE814203713BF7CB0BDE8` | `C1438A403D6A856FA0589F3EA028A63B78A8AD77DFFDC717BDBCAC31F9332155` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 PASS | `OfficialSmoke/EditMode-20261010-205253-dfc62fc71deb478f9a4557263fff0168.xml` | `E3A7D904FAD3F9CE5DAF227E37B9805537D1E1655038D9D7F683BEE6EC24F638` | `OfficialSmoke/EditMode-20261010-205253-dfc62fc71deb478f9a4557263fff0168.log.gz` | `835D6256419BBC53850BBF1422298AE8D942789A1188A2FC57957C74D927585C` | `D1A16AB95D183DCB4CCA16CAFD79102808128F675E2E14661F1E995FBC6038EA` |
| `git diff --check` | PASS | — | — | — | — | — |

All XML summaries report zero failures, skipped, or inconclusive tests. The focused XML contains the successful integrated restore witness. Each compressed log was decompressed and its SHA-256 matched the raw runner log before the temporary raw log was removed. Validation is bound to the code and Assets trees above. Unity-generated ProjectSettings edits and `.meta` files in the isolated worktree were left untouched and excluded from the candidate.
