# CORE-B Deterministic and Temporal validation

## Candidate

Branch `codex/core/CORE-B-deterministic-temporal-c957`, based on current Core canonical `c95752846a0ce0e1ebf985c4d79e0e17b02bb7af` and including current-baseline review commit `3eb18a6a09ee430e15b2d479c02031b221443fd8`. Design `d20c2352887184ccc964d7f9586407d00a727d32`; independent design PASS `96a7df8c2ebad6cf5ce3bfe2bde140f98c6e46dc`.

The bounded change moves DeterministicRandom and LogicalTimeline into the engine-free Core package with their original `.meta` GUIDs; adds one invariant stable-key encoder and Unity adapter; assigns explicit SDK source ownership; links the shared Unity fixtures into the SDK test project; and exposes the specified narrow P12 snapshot bridge. The RNG source changes only for the bridge and explicit unchecked rollover expression. LogicalTimeline delegates stable identity encoding to the shared encoder. No ProjectSettings file changed.

## Validation

- Source-closure compile: `dotnet build Simulation.Core.csproj --no-restore -c Release` passed with zero warnings/errors.
- Locked restore and shared .NET tests passed. CoreValidation cold and warm each passed 97/97 cases. Run `83e8e0475f154ff6a531b1f5cf06550d`; stable 41-file input digest `d955f8a7084878a408312b91d27071d60e5fd9fbb88ef92055c9b7facfcace24`. Cold/warm TRX SHA-256: `08b6a10e2fc0a08e0eefdfbab52fd938183a2b96bf744170bbb432ae5cd1c88a`, `fb025dd74e851b24a1ddf8065fc7b45511029dd7869f33a81a94bb75e40e2380`. Original .NET reports/logs remain private.
- Unity focused RNG/snapshot: 17/17. Focused LogicalTimeline: 38/38. ALL EditMode: 3,085/3,085 with zero failures, skips or inconclusive results. Shared Unity test files are linked directly by the SDK test project; original LogicalTimeline test source remains the linked Unity source, and no duplicate test implementation is maintained.
- Captured input recheck: 41/41 CoreValidation inputs and 1,559/1,559 Unity inputs matched their captured SHA-256 values.

## Unity evidence

Complete ALL EditMode wrapper run `fac20be02e0848efa032f0180883c1e4` is packaged and verified (one run, one report). `core-b-final.zip`: 161,427 bytes, SHA-256 `8a419e649617e1f4e2b535a7ce27683658358e473a7c26a3c33d9dfdc1e06538`. Package manifest SHA-256 `399b6774fb44358346304d766eb364b1c2149b1fedbc0e3df8574c4abc639321`. Normalized XML: 1,605,537 bytes, SHA-256 `b6669716a8deb423af584662befaa112322c47e9947918332cc1b8e9679d5112`. The original XML is privately retained (3,110,389 bytes; SHA-256 `3fdd85ee388c7f4c6ca64f13af8065bea4392bf2747ac24a565dc08cd80c0bad`). Captured Unity inputs had zero mismatches against the candidate. Capture HEAD/tree `97245cc0870ec4a841032dc2ecdba3debfcc5c9e` / `bccd63ee1238786a158dafb3bd880f0a0cc4b1e2` identify the pre-commit snapshot, not candidate metadata.

Official Smoke independently passed 5/5 in run `4b701ec151ff42c5b1902833d1f3d140`. Its report remains private and is omitted from the package because the supported packager rejects filtered reports with incoherent aggregate suite counts. The raw report was not modified and the packager was not weakened; all five Smoke cases are also passed in the complete ALL report.

The public package and manifest were checked for local absolute paths and common credential formats. Local report paths are redacted by the approved packager. Private original reports remain in protected LocalApplicationData.

## Limits

CoreValidation records `worktreeLibraryAbsent=false`; this checkout has an existing Unity Library, so no Library-free SDK claim is made. This checkpoint does not claim CORE-B promotion, P12 readiness, full SimulationRuntime hosting, cross-build save compatibility, or CORE-C readiness.
