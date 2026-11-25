# CORE-F P12-D Core spatial owner codec

Date: 2026-10-25

## Candidate

- Candidate branch: `codex/core/CORE-F-p12d-core-hydration`
- Parent candidate: `cb5173cf97ad548789436aa8652fdf7e05576ea3`
- Implementation commit: `38c1608176cdaee1af4c0197a6560444c60c2fe9`
- Implementation tree: `283769f38edbf856df1c8cd5995c4e0292813cad`
- Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`; no canonical promotion is claimed.

## Scope delivered

The detached spatial-network owner snapshot values and P12-A location/route codec now compile in `Simulation.Core`. The codec owns the exact existing location and route section IDs and schema version; the Unity census provider aliases those Core constants. Its wire representation, ordinal location ordering, route ordering, revision checks, cardinality limits, endpoint validation, and failure behavior remain unchanged. Unity still owns live spatial census capture and the runtime adapter.

Core tests cover encode/decode round-trip, sorted location identity, unresolved route endpoint rejection, and mismatched section revisions. This is a bounded image-codec extraction. It does not complete spatial runtime composition, P12 restore, `DailyV1Application.TryStart`, detached hosting, or CORE-F acceptance.

## Validation

- Official CoreValidation, run `cda9ee8125644a318e698da496ea3292`: 776/776 passed cold and warm. The run used the exact committed source and a Library-free E: worktree. Private evidence manifest SHA-256: `c66169135d76e82658ad6256df8ea5dca19306531dcd97ef836d5c93e31ee33c`.
- Official Unity EditMode, run `03bff1bb5dc946448cbfb8c41b24db96`: 3263/3263 passed, zero failures or skips, against candidate `38c1608176cdaee1af4c0197a6560444c60c2fe9` and tree `283769f38edbf856df1c8cd5995c4e0292813cad`.
- Public Unity evidence package: `Unity/full-editmode/coref-p12d-spatial-owner-codec-full-editmode.manifest.json` and `.zip`. Manifest SHA-256: `a4444acf69d49845befa6b39d4b804997fecdaef030271adc751825a6524bc7f`; archive SHA-256: `56d80902c2f9047644d5cb40a92c62fe16364e77ec4e5d2fa709ecb0b71f6f38`. The official packager Verify action passed with one run and one archive entry.
- `git diff --check`: PASS.

The CoreValidation manifest and raw TRX/log evidence remain in protected local evidence storage. The generic TRX package entrypoint does not accept the CoreValidation manifest schema, so no public .NET archive is claimed. No alternate conversion was used. The Unity package contains the normalized test XML only; its original report remains in protected local evidence storage.

## Review

Independent exact-tip implementation review: PASS for commit `38c1608176cdaee1af4c0197a6560444c60c2fe9`, tree `283769f38edbf856df1c8cd5995c4e0292813cad`. The review verified the exact section IDs/schema, unchanged codec semantics, Unity provider aliasing, preserved Unity asset GUID for the moved codec, and Core test coverage. No actionable findings remain for this bounded slice.

## Remaining integration boundary

Continue moving detached P12 owner image decoding and restoration composition into the shared Core boundary. The live spatial census provider, full selected owner decoder, application lifecycle, persistence-host union, fixed .NET host, and separate-process continuation remain outstanding.
