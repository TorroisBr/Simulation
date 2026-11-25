# CORE-F Genealogy Census and Restore — Independent Exact-Tip Review

**Verdict:** `VALIDATED_CANDIDATE` / PASS, no actionable findings.

- **Candidate commit:** `2a60855e97f0ba10b905d0f2bbdf9b6de154d76c`
- **Candidate tree:** `ce852ccb46053f6391ecc6f4c6855bf0fc0fb5f0`
- **Reviewed base:** `5cb9d0d9c51da9e7eb8843ae31368feac1e03e53`
- **Core canonical:** `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea` (unchanged)
- **Review mode:** independent, read-only, exact-tip source and evidence review.

## Scope reviewed

The review covered the five implementation/test files in the candidate and the public validation package. It verified that the compatibility restore path supplies the exact staged `GenealogyStore` to the Core composer, rebuilt membership graphs retain the same Genealogy owner instance, and recapture requires exactly one current `Required` Genealogy row with matching schema, owner identity, cardinality, and revision. Missing, duplicate, foreign-owner, and stale rows are rejected; the exact empty owner remains a valid required witness. Added coverage exercises those outcomes and graph rebuilds without weakening prior assertions.

The candidate contains no unrelated `ProjectSettings` changes. The three local `ProjectSettings` edits remain unstaged and outside this commit.

## Evidence reviewed

- Official CoreValidation: cold and warm `965/965`, zero failures or skips. Original run `2b7952a7f8f840cc81406aa79edd485d`; manifest SHA-256 `647158fa60fef0e9456b08c37bce1324cd3e94fe3816f5f05767615167538e20`.
- Focused P12 restore regressions: `1/1` each for restored-session exchange and dynamic NPC roster restoration.
- Full Unity EditMode: `3,283/3,283` PASS; official EditMode Smoke filter: `5/5` PASS. Both exact-source runs have stable input digest `0062a8fcfe71cd20ed157cd24b4638ab1992effba0d8a6ea4625b137f21644f5`.
- `git diff --check`: PASS.
- Packaged full EditMode report: `coref-genealogy-20261030-editmode.zip`, SHA-256 `87c4097294339cc9bb750127de07bf784f54c0d79692e637cfa1a17e8029b2b2`; package manifest SHA-256 `52120f17142868292b9981e32273a4c10b0b5bba9f946a7e1e3cb0f0b1ff1214`.
- Changed source hashes match the validation inventories; the public record, manifest, and archive scan found no workstation paths or credential-like strings.

The independent review did not rerun Unity or .NET; it verified the preserved exact-run manifests, packaged report identity, and candidate source hashes. No implementation or test source changed after validation.

## Disposition

This is a validated bounded candidate checkpoint only. It does not promote Core canonical or complete CORE-F, and it does not infer detached-host readiness or any broader P12 completion claim.
