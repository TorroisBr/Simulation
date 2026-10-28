# CORE-F P12-G save-scope exception fix — validation record

Status: **VALIDATED_CANDIDATE** for this bounded Core save-witness/scope-balancing slice only.

## Source identity

- Candidate branch: `codex/core/CORE-F-p12d-core-hydration`
- Commit/tree: `2f3892973c59e41fb4c08ef4f00a2c40ea82c316` / `4a6de7c150dce6e983d9d4b579f928c0c4e6c673`
- Parent: `33e0667209f4cb8528c6e92c18f9020bd82ab2ce`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

This fixes exception cleanup after Core registers a continuation-save scope and adds a regression proving the original capture exception is preserved and the registered operation count returns to zero. The commit changes only `DailyV1Application.cs` and its focused Core test. The tested source-file hashes match the committed blobs.

## Validation

### CoreValidation

- Run `699c5f76714f4257af2b7c799654462f`: cold and warm **808/808 PASS**, zero failures, skips, or inconclusive results.
- The run captured commit `33e0667209f4cb8528c6e92c18f9020bd82ab2ce` plus the two-file exception fix as a stable working input; input digest: `c219560a5c063325f788cdd33064edec9cbb70569b5167e6452c13ebbec4dba7`.
- Private manifest SHA-256: `0db9d98ecbb30c57ee31295e1fdf59dbc1c1cdf4fa0d55ffcac92ed4125a6307`.
- Original cold/warm TRX reports and logs remain in protected local evidence storage. No public .NET package is claimed.

### Unity EditMode

- Unity `6000.3.9f1`, run `d4f1c77a6f2746f9ace950b2ab91a516`: **3264/3264 PASS**.
- Stable effective-input digest: `5903ee1ec1bc703decc26db6fa0f2e60f5c7a1323d987e7b077a626c2d3d1617`; the changed production file hash matches commit `2f389297`.
- Public normalized-result archive: `coref-p12g-save-scope-2f389297-editmode.zip`, SHA-256 `d8969f019259b8b345d2106c441738aa8a0d6ec9d190cab4567dc483f82ecf93`.
- Public manifest SHA-256: `50e111717681cca1ba24174cfab9ef4239fb7c774e288f84802ee89a0b87613b`.
- Package verification passed: one run, one result entry; archive hash matched the manifest.

### Official Smoke filter

- Run `290b9f9f5804479c9c7f04cb46aca5ab`: **5/5 PASS**, stable effective-input digest identical to the full EditMode run.
- The five passing identities are contained in the normalized full EditMode result above; no separate Smoke archive is claimed.
- `git diff --check` and the pre-commit staged-diff check passed.

## Scope limits and preservation

This checkpoint supplies an internal Core continuation-save operation and exact completed-boundary witness seam. It does **not** integrate P12-G payload capture, detached C/D/E/F owner staging, target admission, image composition, atomic storage through the public application API, or complete P12-G behavior. It does not complete CORE-F or claim Core canonical promotion. Protected ProjectSettings edits and private original evidence remain outside the candidate commit.