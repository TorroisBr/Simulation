# CORE-F P12-E Institution/Office Core Boundary

## Scope and source identity

This checkpoint extracts the existing Institution/Office census, owner snapshot, and codec implementation into `Simulation.Core`. The snapshot now depends on a small capture-source port. The Unity adapter supplies the exact live InstitutionStore, OfficeStore, PersonStore, P12-B token/vector, and existing capture-staging freshness predicate. Owner identity, section cardinality/revision checks, institution-office binding, person relation checks, tenure constraints, and snapshot staging remain enforced by the moved Core implementation.

The existing Unity `.meta` GUIDs for the three moved source files were preserved. The Unity-specific capture adapter is the only new Unity production boundary. No profile, owner, persistence, admission, or runtime semantics were expanded.

- Base: `25bdf58f1d24bd1f589194a31dd92ff65a3674ac`
- Candidate implementation commit: `69b0c9b333dcfde285098b455cadf01f223a301f`
- Candidate tree: `d1af2ac4a99b71cf8cc71b51d8bd9eebe42abec3`
- Core canonical at validation: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

## Validation

| Gate | Result | Evidence identity |
|---|---:|---|
| Official CoreValidation cold | 845/845 PASS | Run `c019d2ce7f2947d29a8bc59c41980b54`; source-input digest `4858df4a29309097ce702eff2f30178142165ab10d19f2d446fa32d49fe1afc2` |
| Official CoreValidation warm | 845/845 PASS | Same run; separate warm TRX |
| Unity EditMode ALL | 3271/3271 PASS | Run `7accabcde6a443d99cff57226a006364`; Unity 6000.3.9f1; stable input digest `8c2ef810905f4ff6efbb2a311bc6ac4a2413797348eba4e2b04cc4ec214283a6` |
| Official filtered Smoke | 5/5 PASS | Run `0a2236b8f8a9468c8a01e8d3963027bb`; same stable input digest |
| `git diff --check` | PASS | Candidate implementation diff |

The CoreValidation run was produced before Unity created a `Library` in its isolated worktree; the runner reported `libraryAbsent=True`. The complete EditMode package at `Evidence_69b0c9b_clean/coref-p12e-institution-office-candidate-69b0c9b.manifest.json` and `.zip` passed official verification with private-original comparison. The normalized archive SHA-256 is `30bf1bce3e4f01ea9ebbd4aac4c943faf12280f04285b9fcd35e482dc20c52d5` (173,109 bytes); the package manifest SHA-256 is `52853aecb3f4aa2c87fd21600235ba50b051b49f408bbb8bab0652761dc83af4`.

All five Smoke test identities are also included in the complete ALL report. The separate filtered Smoke original and runner manifest remain in protected local retention; no filtered public package is claimed.

The CoreValidation original manifest and cold/warm TRX reports remain in protected local retention. Manifest SHA-256: `a5f7754e71da80543c7c75f0aa13ebac3dc880a369d5993280953c2c6a1b0a7b`; cold TRX SHA-256: `7b97a329617abcbf6b5c4abd0dfae4413779b3bba7d96724c6a956afe2450346`; warm TRX SHA-256: `8485d651d9acc1ea4256529ed51f0283b33343ff821ad080dd9de2676020eba1`. No public DotNetTrx package is claimed because this run emitted `simulation-core-validation/v1`, not the separate approved `dotnet-trx-source-context-v1` adapter context. The retained originals were not modified or repackaged.

The isolated Unity worktree's initial import changed two local ProjectSettings files. That first run was not used for the packaged result. The subsequent ALL and Smoke runs were stable; those worktree-local settings changes were excluded from the candidate commit. The original candidate worktree's ProjectSettings changes and unrelated untracked evidence were left untouched and excluded from this checkpoint.

## Status

This is an implementation checkpoint, not CORE-F completion or canonical promotion. DailyV1Application, shared runtime composition, detached restoration, and .NET host acceptance remain open under their existing contracts.