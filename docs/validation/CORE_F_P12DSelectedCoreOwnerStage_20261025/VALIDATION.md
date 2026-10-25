# CORE-F P12-D selected Core owner staging

Date: 2026-10-25

## Candidate

- Candidate branch: `codex/core/CORE-F-p12d-core-hydration`
- Parent candidate: `9aa98d9b0ac9f0c565c82298c5d6b843bd75d0aa`
- Implementation commit: `3a468f2e6b0e0518bfc8f60c24621501cab1863e`
- Implementation tree: `1853b397e0f6420c59cfed8561d3204c72dfffa9`
- Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`; no canonical promotion is claimed.

## Scope delivered

For selected-profile image restore, Core now stages the complete P12-D City/NPC owner vectors from the existing detached row values and the admitted definition graph before the CityRuntime/NpcRuntime compatibility projections are constructed. It reuses the existing Core hydrators and reciprocal membership check. The Unity adapter must consume the exact returned owner references; missing City/NPC handoff entries fail closed instead of invoking another hydration path.

This is a bounded restore-stage integration only. It does not complete P12-D, P12-G, the shared application lifecycle, v1/v2 persistence integration, detached .NET hosting, or CORE-F acceptance.

## Validation

- Official CoreValidation, run `462bca22962d468b8f8f7d997dfe49c5`: cold 773/773 PASS; warm 773/773 PASS. Protected runner manifest SHA-256: `5e637c9a1133e222ce63103626a2c0c6731f9435102bd00ced5fccff07858f70`. The independent exact-tip review verified that the captured Core/test source entries match the committed implementation tree.
- Official Unity EditMode, run `2eccc5e53a884fea8467e77263151f80`: 3263/3263 PASS, zero failed or skipped. The run was captured after implementation commit `3a468f2e` on the exact candidate source.
- Public Unity evidence package: `Unity/full-editmode/p12d-selected-core-stage-full-editmode.manifest.json` and `.zip`. The manifest SHA-256 is `02bfef9b4ac51586e65847527e466ed40cc79936d53d50ec11bca686f944dbca`; archive SHA-256 is `3f3e6e7baec38dab0a17d957639ec1b1284805baa88f8b919c557f000f342e92`. The official packager's `Verify` action passed with one run and one archive entry.
- `git diff --check`: PASS.

The earlier focused City (18/18), NPC (32/32), and P12-G restore (15/15) runner reports remain preserved in protected local evidence. The P12-G restore filter was rerun after the fail-closed review correction. The focused filtered XML was rejected by the public package validator because its enclosing `Simulation` suite retained the full assembly count; the complete EditMode run produced a structurally consistent, verified public package. No original XML was edited or reinterpreted.

CoreValidation's protected run manifest remains private. The generic DotNetTrx packaging entrypoint does not accept the CoreValidation manifest schema, so no public .NET TRX archive is claimed. No alternate report conversion was used.

## Review

Independent exact-tip implementation review: PASS for commit `3a468f2e6b0e0518bfc8f60c24621501cab1863e`, tree `1853b397e0f6420c59cfed8561d3204c72dfffa9`. The review confirmed the fail-closed handoff, same-definition identity, no duplicate hydration on the selected image path, unpublished staging, and matching candidate origin. No actionable findings remain for this bounded slice.

## Next integration boundary

Continue moving P12 image decode/restore composition and the selected application/persistence lifecycle into the shared Core boundary under the approved CORE-F design. The current coordinator still retains Unity-side decode and later owner-package orchestration; public `DailyV1Application.TryStart`, the end-to-end v1/v2 persistence union, fixed .NET host, and detached continuation remain outstanding.
