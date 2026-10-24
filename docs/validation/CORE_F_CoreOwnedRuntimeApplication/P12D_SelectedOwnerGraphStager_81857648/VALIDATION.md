# P12-D selected owner graph stager validation

Implementation candidate: `81857648a1b9051bd7d0e7fc107104307361c520`
Candidate source tree: `162e830ffeef9179a035a1b6c7efc88ef3a74101`
Base: `e1af751393ad7ba5816f48003071a9dc2f3990c2`
Branch: `codex/core/CORE-F-p12d-selected-owner-stager`

This validation covers the bounded P12-D Core owner-graph stager and exact selected City/NPC owner references. It does not claim P12-D or CORE-F completion.

## .NET CoreValidation

Official `Tools/CoreValidation/Invoke-CoreValidation.ps1` run `0c3c514aedb042a6bf25b81d8f47db60`: **PASS**. Cold CoreValidation: **717/717**; warm: **717/717**. The default gate also completed locked restores, CalendarDemo cold/warm, and the Simulation.Lab build and fixed cold/warm scenario. SDK: `9.0.315`.

- Committed source tree: `162e830ffeef9179a035a1b6c7efc88ef3a74101`.
- Captured input digest: `e4e5b795bf3035d87d2ce5e13ff1e65d063c31ff51fd31bd7d688ff2a2fee3f5`; no worktree changes were present during this run.
- Protected runner manifest SHA-256: `16674778ee46c24088ed26a13afebcc857e9860c6f5ef6d11b2be3749042ea53`.
- Original cold TRX SHA-256: `a745e6eeef06cb712b602df525bf4a9b28b0eb3dad64f14f9b204fa618bb0954`.
- Original warm TRX SHA-256: `e3030bf8b2a937f5f2782b266e44e2258210f231b4d3e59f9133832d66f95117`.

No public DotNetTrx package is claimed. This CoreValidation runner emitted `simulation-core-validation/v1`; the approved TRX adapter requires a separately captured `dotnet-trx-source-context-v1`, which this runner did not emit. The original reports and manifest remain in protected local evidence storage; they were not rewritten.

## Unity

Official wrapper, Unity `6000.3.9f1`, full EditMode run `8a5ce6416ff94e77bfdfd7158227fbac`: **PASS, 3261/3261**. The same full report contains all five Smoke cases, each Passed. Its protected original XML SHA-256 is `6dbf0c4e556cf7925ca7d2e5ac1ff121753e74ee38a4db066f607832540f4ae9` (3,263,308 bytes).

The privacy-safe package is retained here:

- `editmode.manifest.json`, SHA-256 `a97592b64ece7959747645feea3f358b03b7fbfee454fd9230c58a7c8a32f498` (443,069 bytes).
- `editmode.zip`, SHA-256 `e63f92eeb401835cc0b448f03568d5e148b6b33cf8f8c6331b64a01b487b598e` (172,557 bytes).
- Bundle: `coref-p12d-editmode-full-81857648`.
- Package verification against the protected source run manifest: **PASS**.
- Stable Unity input snapshot: `c215e064e168c8c995ca96b93d30cf6280358c49b6f9f639e4e4ec9c9e64dd67` before and after the packaged run.

The first Unity initialization rewrote line endings in `ProjectSettings/EditorBuildSettings.asset` and `ProjectSettings/ShaderGraphSettings.asset`; `git diff --ignore-space-at-eol` confirmed no semantic content delta. Those validation-worktree-only changes remain unstaged and are excluded from the candidate commit. A subsequent full EditMode run used a stable before/after snapshot and produced the package above.

The separate filtered Smoke run returned **5/5 Passed** (private run `d451eb7abe0e41de95c91f0f212aa292`). The packager correctly refused a separate filtered package because NUnit retained the full-suite root testcase count while emitting only five descendants. The full EditMode package includes and verifies those same five passing Smoke cases, so no separate filtered package is claimed.

## Additional checks

- Focused stager tests: **2/2 PASS**.
- `git diff --check`: **PASS** before implementation commit; rerun on this documentation/package delta before commit.
- Independent exact-tip implementation review: pending.
- Canonical promotion: not performed.