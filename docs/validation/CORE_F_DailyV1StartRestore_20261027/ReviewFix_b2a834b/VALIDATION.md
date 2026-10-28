# CORE-F initial-load fail-closed review fix

## Candidate identity and scope

- Candidate branch: `codex/core/CORE-F-app-start-restore`
- Implementation commit: `b2a834b23a9594a7a5d2fc04a6e762660b13da1b`
- Commit tree: `62bcb66721f23f4323bbc92818da556e96e28cad`
- Parent: `f054622b75ffbcd4b932f512f675a6df485193df`
- Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

The change is limited to `DailyV1Application.TryLoadInitialContinuationImageForStart` and its Core test. An invalid first host input now consumes the one-shot initial-load attempt and blocks genesis on that otherwise-unused application. The result remains `InvalidOwnerValue`. A completed `NoSave` result remains the sole load outcome that permits genesis. No restore owner, Unity caller, or persistence format changed.

## Validation

| Gate | Result | Identity |
|---|---:|---|
| Official CoreValidation cold | 899/899 passed | run `8cfb11b0e3c04b1aaebb6d527d90facc`; cold TRX SHA-256 `45058f2f1dbe8682c9ece876d8bcdf07f62a02391b8781249adea268c3e38a86` |
| Official CoreValidation warm | 899/899 passed | same run; warm TRX SHA-256 `3adec0e2f37328a0977fde52e3d3c84aa52aed90e148e4335ff978a5ebf05362` |
| CoreValidation provenance | PASS | manifest SHA-256 `cf3ead490aab35372965ea2d4b1ed7b6bf669021f11cd4992b8fd2a74a2a0f06`; source commit/tree match this candidate; zero worktree changes; Library absent; Lab cold/warm outputs passed |
| Full Unity EditMode | 3274/3274 passed; 0 failed, skipped, or inconclusive | Unity 6000.3.9f1; run `5cf971aba42e49ccb6e6bcf4363acda2`; stable input digest `975c2519386c2ef4cbaea408dfc72ab36e6b7f2efa8e8c40371ebcd1a2149597`; original XML SHA-256 `deb5b2b6b33a75801526e3f40a1d7c19e04cf927bf61e8b6c18a69a379852ca1` |
| Unity result package | Verified | package manifest SHA-256 `ef45f94036987ae89438f1ba9fc4a0c3d1c42539d4352d8076bfce74c6081bc7`; archive SHA-256 `266d54826364e318cd89e3864e9c4b8636b5bf839380100ac27f85633d41ad1d`; one normalized XML entry; official verifier PASS; package scan found no workstation paths or credential-like text |
| Official Smoke | 5/5 passed; 0 failed, skipped, or inconclusive | run `11155b2a5dd04ceda0c6500378134f2b`; stable input digest `975c2519386c2ef4cbaea408dfc72ab36e6b7f2efa8e8c40371ebcd1a2149597`; original XML SHA-256 `aae6de836d576d87e40262da62e39b32c127c2a163057bcef7e2bd1af80c4101` |
| `git diff --check` | PASS | checked for the implementation patch |

The first Unity run was not counted because initial project import changed only generated `ProjectSettings/EditorBuildSettings.asset` and `ProjectSettings/ShaderGraphSettings.asset` in the isolated validation worktree. Both files and its private original evidence were preserved; the subsequent stable run above is authoritative. The CoreValidation raw TRX/logs and Unity original XML/logs remain in protected local retention. Only the official normalized Unity result package is included here. No absolute workstation paths or raw reports are published.

## Independent review

- Verdict: **PASS**, no actionable findings.
- Exact candidate/tree: `b2a834b23a9594a7a5d2fc04a6e762660b13da1b` / `62bcb66721f23f4323bbc92818da556e96e28cad`.
- Durable independent review commit: `68de89ee418a17d02dfa381300b53c7bb4302b30` on `codex/review/core/CORE-F-app-start-restore-b2a834b`.
- Record: `docs/validation/CORE_F_DailyV1StartRestore_20261027/FOLLOWUP_REVIEW_b2a834b.md`.
- The reviewer independently assessed the exact two-file code diff and regression. Protected validation manifests and reports were not accessible to that reviewer; test results above are recorded from the official runners, not claimed as independently rehashed by the reviewer.

This is a bounded review fix, not CORE-F acceptance, a complete `TryStart`, canonical promotion, or a claim that the detached host is complete.
