# CORE-F P12-D Core spatial owner hydration

## Source identity

- Candidate branch: `codex/core/CORE-F-p12d-core-hydration`.
- Core implementation commit: `86c7d5e2fb2d0cd937dbd081d60026da93c18c4a`.
- Core implementation tree: `202c35f1732ac3dc4339e584a8004c111911dfc2`.
- Parent candidate: `38c1608176cdaee1af4c0197a6560444c60c2fe9`.
- Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`; no canonical promotion is claimed.

The Core hydrator now validates and stages the detached spatial owner, preserving location identity/order, route identity/order, route endpoints and travel duration, and the exact owner-local revision. The Unity adapter preflights the unpublished host identity registry, registers those exact staged Core Location and Route instances, then wraps that same owner. Existing P12 section IDs, schema, codec and capture-boundary responsibilities are unchanged.

## Validation

| Gate | Result | Evidence |
|---|---:|---|
| Focused Core spatial hydrator tests | 3/3 PASS | Initial incremental compile/test; superseded by full CoreValidation below |
| Official CoreValidation cold | 779/779 PASS | Run `dc2d9c94415e4fb49618c3fd2eef94a2` |
| Official CoreValidation warm | 779/779 PASS | Same run; separate Library-free E: worktree at the exact source commit |
| Library independence | PASS | Runner recorded `libraryAbsent=True` for the exact source commit and tree |
| Official full Unity EditMode | 3263/3263 PASS; zero failed, skipped or inconclusive | Run `c37089be2b414485be78b52d07c23bba` |
| Official Smoke filter | 5/5 PASS; zero failed, skipped or inconclusive | Run `188fa63cadb043bfa5bbefc6821eadfc`; all five test identities and pass outcomes also occur in the verified full EditMode archive |
| `git diff --check` | PASS | Implementation diff |

The protected CoreValidation manifest SHA-256 is `877a227eaf7050daff81c0ad4f0e1a23313066b09bdf01c1d2f14d69a137def6`. It binds the CoreValidation run to implementation commit `86c7d5e2fb2d0cd937dbd081d60026da93c18c4a` and tree `202c35f1732ac3dc4339e584a8004c111911dfc2`.

The normalized full EditMode package is `coref-p12d-spatial-owner-hydrator-editmode.zip`; its SHA-256 is `a8f9594b4da5015269f3daebeb8ee71ca872dc3664fdb37be496b622b6456fe7`. The adjacent package manifest SHA-256 is `2e9d63d67eb0820c17a41c471265249e53d936f6069cf0b67d7560a0cd80741d`. The official packager Verify action passed with one run and one archive entry. A privacy scan found no local workstation paths in the public manifest.

The protected full EditMode run-manifest SHA-256 is `8121844fdb88c908b77a23b871374c51e28dd9cdf4c91f018ca563f276c9a69f`.

The standalone Smoke report and raw log remain unchanged in protected local retention. Their SHA-256 values are respectively `2495700bab65dcee8bf7ffbf70e4eecb107af58cb6da6643256a2a2c3daab4b0` and `6f21cf6aaff48a0161f5f2b74a8eeed2c61c8ade760603ae48e83bd48450dbf8`; the protected run-manifest SHA-256 is `7effd17638a49daa735840d0b7a7c32d88c8b6a9c24c4e2b4281538b8241246e`. The standalone filtered XML is not published because the evidence packager rejects the filtered parent-suite aggregate count mismatch. No XML was edited. Each of the five filtered case identities and outcomes was matched to the normalized full EditMode report, and the complete report package passed verification.

Validation diagnostics: an initial PlayMode attempt using the `GMConsolePlayModeSmokeTests` filter returned zero cases and is not counted as a gate. The supported official Smoke selection is the EditMode `Smoke` filter described above. The zero-case run and all original files remain in protected local retention.

## Scope boundary

This is a bounded Core-owned spatial snapshot hydration slice. It does not claim complete selected-owner restoration, shared application composition, `DailyV1Application.TryStart`, P12-A readiness, detached host continuation, or CORE-F acceptance. The caller must continue to stage the host identity registry as unpublished and discard it if later composition fails. No P12 canonical promotion is claimed.
