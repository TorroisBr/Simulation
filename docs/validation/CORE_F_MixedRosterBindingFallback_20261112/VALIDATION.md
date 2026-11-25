# CORE-F mixed-roster Person binding fallback — 2026-11-12

**Purpose:** resolve the exact independent-review finding on candidate 73fbd3e while preserving the accepted selected-profile operation and the established Unity compatibility fallback.

## Candidate identity

- Candidate branch: codex/core/CORE-F-restore-stage-transaction.
- Source commit: ed01f834f314ca29d35406ab31efe29a98fba322.
- Source tree: acebca05b092cc0a81fd09d700c4e15f4fa9ffb9.
- Base: 73fbd3ecead257e69f893364fac19a44a431f5b0.
- Core canonical at validation: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea.
- Changed source file SHA-256: Assets/_Project/Scripts/SimulationRuntime.cs — f42c60828a1e51141d1ac7742ae48f44812e9b311da1ae9297ccc101f58ef10d.
- Changed test file SHA-256: Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs — d85cd6b6985aad4d0d44307bee155ca2b2439858d6c568273a58452de1f4fa8a.

## Change and regression

The selected-roster Person-binding projection is now attempted only when the target NPC uses selected Daily-v1 definitions and the exact selected owner instance is present in the current selected Core graph. A legacy NPC absent from that graph therefore reaches the existing mixed-roster Unity compatibility path.

The new test SimulationBootstrapCompositionTests.MixedDailyV1RosterBindsLegacyNpcThroughCompatibilityPath verifies successful binding, the exact Person instance, one PersonStore revision, one membership epoch, and that the runtime remains healthy. It does not weaken or replace the selected Core binding assertions.

## Validation

All validation below used source commit ed01f834 and stable before/after input snapshots.

### CoreValidation

The official runner passed its cold and warm gates: **1,100/1,100 each**. The runner's Lab build and cold/warm demonstration gates also passed.

- Run ID: e59d51bd80d94d9fa8fa0f85dc5586d4.
- Source input digest: fb6c30e3c5f077c8ba7e2bc1005660de604cc514417c5a2033894e8d4edec58b.
- Protected runner-manifest SHA-256: ba54552ccd05770a1b10eafede120733bb535d7617e8d33436f5edfd0cf11f9c.
- Original cold TRX SHA-256: a80370c2ec40b342b8e94184cf7cf7a9dfdc8746037f7f7438c8bbbf6bec718a.
- Original warm TRX SHA-256: 81b5804689db0111355faf58e6ca9e90aa27368d0b3f41a96ec93d5d3a688ddb.
- The runner reports a Unity Library in this worktree. This run makes no Library-independent .NET-host claim.
- The native runner emitted simulation-core-validation/v1, not the separate private dotnet-trx-source-context-v1 required by the approved public TRX adapter. The original manifest and TRX files remain unchanged in protected local retention. No public DotNetTrx package or original-to-public comparison is claimed; no source context was fabricated.

### Unity EditMode and Smoke

Unity 6000.3.9f1, official pinned validation runner:

- Full EditMode: **3,317/3,317 passed**, 0 failed, 0 skipped. Run ID 01800e8b5d344dc88ca931e8b7847df6; stable input SHA-256 619dcd0fc693b545b1c04bef1d8bf1cec212446a58e1a739d9731ea1029ec099; private run-manifest SHA-256 a1e8f7dafe31dea75e8195bec5666fe800053f82b9afd8d6802b6a5ddbed537e.
- The new fallback regression appears exactly once in the full report and passed.
- Official Smoke filter: **5/5 passed**, 0 failed, 0 skipped. Run ID 7d504ba8253d4065b92f6f94db5f5346; the input digest is the same as the full run; private run-manifest SHA-256 631be5042d1fffa665be4d3ca704aeb1462bd63d3f1bacdbfb84d320d55197b3.
- All five Smoke case identities and pass outcomes also appear in the full EditMode report.
- The official result-only package contains the complete normalized full EditMode report. Package verification against its private run manifest passed with one run and one entry. The original XML and log remain unchanged in protected local retention.

Public package:

- Unity/coref-mixed-roster-binding-editmode.zip — 176,462 bytes, SHA-256 fb526df2b778db3edc313ad9d5b498f7d1661978800948a2e8e164fb280d3d22.
- Unity/coref-mixed-roster-binding-editmode.manifest.json — SHA-256 5d1d99d89acbca421581199eef7bfe90a8963ed1b30146f0c5f759e1c484be7c.

The existing local ProjectSettings edit was preserved, excluded from the candidate commit, and included in the Unity input snapshot. Other unrelated changes and local evidence were left untouched and unstaged.

git diff --check passed.

## Review and status

The prior independent review of 73fbd3e returned NEEDS_CHANGES for the mixed-roster legacy binding path. That finding is addressed by the guarded target-owner check and regression above. A fresh independent exact-tip review of the corrected candidate and this evidence record is pending.

This is candidate work only. It does not close CORE-F and does not promote to Core canonical.