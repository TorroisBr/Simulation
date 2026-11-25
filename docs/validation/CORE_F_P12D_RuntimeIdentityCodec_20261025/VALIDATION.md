# CORE-F Runtime Identity Codec Extraction Validation

Date: 2026-10-25

## Candidate identity

- Repository: https://github.com/TorroisBr/Simulation
- Candidate branch: codex/core/CORE-F-p12d-core-hydration
- Core canonical: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Code candidate: 038d7c199d5c2eda248c8c684e7ada0d98351ee1
- Candidate tree: 3f9a35055859fea6d1961d9ad4c6a9ede1000ddf
- Candidate base: a3088251cbb6f225d75b8739c8fe34cf8a1974e8

## Delivered boundary

The existing P12 runtime-identity owner codec and immutable snapshot DTO were moved into Simulation.Core with their Unity metadata GUIDs unchanged. Runtime-identity section schema/version and all eight section IDs now come from one Core contract used by the shared identity index and Unity census provider. Unity-specific registry capture remains in a narrow adapter and retains the revision-before/after consistency check.

This preserves the existing owner-family allowlist, row ordering, alias-group rules, revision semantics, schema, and wire section identifiers. It does not alter profile contents or P12 admission, publication, or restore semantics.

## Validation

The official CoreValidation run passed 771/771 cold and 771/771 warm:

- Run: 0ce7c539edfe4798a337301ea88aed62
- Input digest: cc78b2f48dd233b829261a6000cfedb6087279127d5d846cd3749100d84dc843
- Manifest reports a stable 391-file input snapshot and stable generated dependencies.
- The manifest's 391 input file hashes were compared with the committed candidate: 391 matched, with no missing, mismatched, or out-of-worktree inputs.
- Cold TRX SHA-256: 1bfadf54dc5841701f88fd7b6ea00260c05a5086f00f6e9324990ab36b289a1e
- Warm TRX SHA-256: 15e9a77d91981f51d4642eabba7523ae2981e4b9210d2587dbfece6789c80fec

Unity 6000.3.9f1 / Test Framework 1.6.0 exact-source validation passed:

- Focused P12 continuation codec tests: 37/37; run 2ea26b9d3bbf4a418a707209d5dc1bea; XML SHA-256 26710ca6248795aaa208c32eaec2364d9165d91d972dd02df17d8dba34ef16e4; log SHA-256 ff19b00c1f6b49e00a7989b8d8563b9cf84acca1e3d36f377916eea9d4f1d181.
- Full EditMode: 3263/3263; run 6c6e7afcd3174b198b08d2d033fc74cb; XML SHA-256 8801559a6e139b97539449a25e9af6d2f5310f8c22cc26441349867745376; log SHA-256 e396202cb66eb5cb6eb4f78f713dda5cb6946f8b5fbc79d733dde84fc1590d75.
- Official Smoke filter: 5/5; run 739bb94a7ae44ba090afb3379e1379c3; XML SHA-256 67fc5d84ac91072747b2e50e2ac35922b2b03bc8449d2d2e8b9f02c85a6d9ed9; log SHA-256 573b6eb9f7694335e19a811d757e1f01def1ae176632f0cb2aa44fbf05a0bf52.
- The official Unity runner reported stable source snapshots for these runs.
- git diff --check: PASS.

Raw Core TRX, Unity reports, logs, and private runner manifests remain in protected local retention. They are not included in this public record or represented as a privacy-safe normalized package. The CoreValidation manifest records the pre-commit base SHA with the candidate as working-tree inputs; the recorded input hashes match all corresponding committed candidate files.

## Independent review

The exact-tip code review is recorded in INDEPENDENT_REVIEW.md. The reviewer passed the code diff and explicitly could not access the protected validation manifests. That evidence-access limitation is retained here; the review is not described as an independent validation-artifact audit.

## Limitations

This is a validated candidate checkpoint, not a Core canonical promotion or a P12/CORE-F closure record. It does not establish a complete shared runtime, DailyV1Application, detached .NET hosting, complete owner migration, P12-A readiness, P13 readiness, or CORE-F completion.
