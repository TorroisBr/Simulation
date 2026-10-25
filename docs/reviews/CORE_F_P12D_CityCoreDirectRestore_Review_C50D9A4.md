# Independent Review — CORE-F P12-D City Core Direct Restore

**Verdict: PASS — VALIDATED_CANDIDATE.** No actionable findings.

## Reviewed identities

- Repository: `TorroisBr/Simulation`
- Candidate branch: `codex/core/CORE-F-p12d-core-hydration`
- Exact evidence tip reviewed: `c50d9a41c81e898b7bf4f0f190a9db8ab6516c64`
- Implementation commit: `86c6677a1e05cd57271661cb2aa7d6596859e303`
- Implementation tree: `7fd42b376c98e6298422c3ef958c7ae14e16be4e`
- Candidate base: `39b29f81b40b01b62ebeb0c872b347c637e4512d`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Independent reviewer: `/root/p12d_stage_review`
- Review method: read-only exact-tip review; no tests rerun by reviewer.

The reviewer inspected the full code diff and docs-only evidence successor against the approved P12-D and CORE-F runtime/restore contracts, current source, tests, and validation record. Candidate and canonical refs were refreshed; canonical is an ancestor of the candidate. The evidence-tip successor adds only the validation record, public manifest, and XML-only archive; it does not alter the reviewed code tree.

## Findings

No actionable source, identity, definition-binding, ordering, revision, schema, atomicity, authority, or scope finding remains.

The selected restore path passes the decoded Core City hydration DTOs into the existing Core selected-owner stager. It creates the Unity City projection only from the returned staged owner set. A failed Core stage exits before projection; it cannot fall through to Unity rehydration. The image decoder publishes no partial DTO collection, and the Core stager leaves its output unset until its validation and relation assembly succeed.

The path preserves City and Item definitions from the admitted effective-definition graph, validates the staged spatial Location binding, retains City schema version 1, rejects unsupported schemas in both decode and hydration, and preserves row order, multiplicity, identities, values, revisions, and the existing nonselected legacy path. No wire or section identity changed.

## Validation evidence inspected

The committed validation record and manifest bind evidence to implementation commit `86c6677` and tree `7fd42b3`:

- CoreValidation: 787/787 cold and warm PASS.
- Full Unity EditMode: 3264/3264 PASS.
- Official Smoke: 5/5 PASS.
- `git diff --check`: PASS.
- XML-only public package: 3,264 passed cases, zero failed/skipped/inconclusive; no local workstation paths.

The reviewer inspected the validation record and manifest but did not rerun tests. The reviewer could not independently download the archive through the read-only GitHub connector. The Master independently hashed the local archive and manifest: archive SHA-256 `eaf3c91ea4ef938bd777b34a7bda716db81028cbe20129c0d3092726a1869a0a`; manifest SHA-256 `4e55c784b8cd2b93da896737fd7c054e1b090670504048a45d3120a0d78bf0db`. Both match the committed validation record.

### Windows line-ending identity note

The validation input snapshot recorded `P12ACityOwnerCodec.cs` at 3,166 bytes with SHA-256 `656bd923bd1a2cb9b76ffaf688b2409d8e761123a23fd8d56b1ce694ff33b960`. The raw Git blob is 3,164 bytes because Windows checkout line endings add two CR bytes. The file content otherwise matches; Git's configured clean conversion maps the current checkout to the exact candidate blob. This is normal checkout normalization, not a content or package-integrity discrepancy. The current worktree hash matches the captured validation representation.

## Scope and limits

This review validates only the bounded P12-D integration that sends decoded City values directly through the Core selected-owner stager before Unity projection. It does not complete P12-D, P12-A, P12-G, detached host integration, or CORE-F. No Core canonical promotion or Phase closure is implied.
