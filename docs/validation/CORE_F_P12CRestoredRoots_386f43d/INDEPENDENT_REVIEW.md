# CORE-F P12-C Retained Roots — Independent Review

**Verdict: PASS.** This review accepts only the bounded implementation checkpoint recorded in VALIDATION.md. It does not promote Core canonical or complete CORE-F.

## Exact identities

- Reviewed implementation commit: 386f43d38b69e87f407dcd9505580d15d15237e3
- Reviewed implementation tree: 166499fc54ebc5712d39198bc642f7631869269a
- Base: 01ea01fb29b755bb2e99e3f5d7c6eb11daf5f358
- Candidate evidence/package commit inspected: 83b9afaefa65d119d2aee9febc79938b93a6e98e
- Current Core canonical: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Candidate branch: codex/core/CORE-F-shared-daily-v1-app-host
- Published candidate ref: codex/core/CORE-F-runtime-app-session-boundary-20261025

The candidate evidence commit is a docs/package-only child of the reviewed implementation; implementation source and test tree are unchanged.

## Review result

The complete ten-file code diff was inspected against the exact base. No actionable findings, root/lineage broadening, or unsupported P12-C semantics were found.

The initial Core session retains the exact Core P9 manifest snapshot and P8 SpatialAuthorityStore. The admitted selected SimulationRuntime retains that same P8 store; compatibility runtimes keep their prior clone behavior. Restore supplies the staged P9/P8 roots to a separate restored-session factory and leaves GenesisComposition null. A later Core-bound save captures from those retained roots under the active operation, checks the Unity projection's store identity, and revalidates the source census before returning snapshots. The existing legacy capture branch remains intact.

The review checked object identity, root/profile binding, P9 manifest linkage, restored-session distinction from genesis, repeat save after restore, stale-census rejection, operation invalidation, compatibility behavior, and the changed assertions. No separate authoring source of truth was introduced.

## Independently checked evidence

- CoreValidation run c132b6140e7448e68825907f736570b4: cold and warm each 822/822 PASS, zero failures. Stable input digest: fb4dd1ca70fc18df394e8b0c214ba18abf93a810625f0e600a917156833f122e. Changed Core source/test inputs match retained run inputs.
- Focused restored-save run 0b4e35460690439d98411e15043d0b3c: PASS.
- Focused restore-stage operation invalidation run 9c22f9df105249ce8007e7aa649d3da6: PASS.
- Unity ALL EditMode run e2f7fa3e121d4d9fb19c6be3efcc164f: 3269/3269 PASS, zero failed, skipped, or inconclusive. Smoke run e033b0b9379c447ba8bff40dc80423e4: 5/5 PASS. Stable Unity input digest: aa74e4355cfb482ebe5a91df1431e2514588edf7b004303caac1f3bf10c09224; changed Unity source/test bytes match the validated inputs.
- The public complete ALL EditMode archive and manifest verify against the protected original. Archive SHA-256: 5b59173bd81bbe5658691f7216b3b5d11e502358272a2e7b07b9ec2ee55ebdb9. Manifest SHA-256: cff9d0e0c5cfd6c89155f5b571779b397d2e34038786dea7e735ddd34f97f15b.
- Privacy checks found no workstation paths or credential values in the public validation record, manifest, or normalized XML.
- git diff --check passed.

Original CoreValidation TRX and filtered Unity reports remain protected; this checkpoint makes no public .NET TRX package claim. The public Unity package contains the complete ALL EditMode report. These evidence limits do not satisfy or replace the final CORE-F acceptance package.

The two pre-existing local ProjectSettings edits are not part of the candidate or review.

## Limits and integration notes

This change preserves P12-C roots for later saves from a restored Core session. It does not claim a complete detached .NET host, separate-process T/T+1/T+2 parity, complete source closure, P12-A readiness, P13 readiness, canonical promotion, or CORE-F completion. Keep the reviewed code tree unchanged when reusing this review; a code/test change requires new affected validation and exact-tip review.
