# CORE-F Runtime Owner Section Contracts — review-fix validation

**Status:** implementation fixes validated; exact-tip independent review pending. This candidate is not canonical and is not promotion-ready.
**Core canonical:** `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
**Base:** `3faaa8ab66e7c257fa2cccf7777ca087be9adfa0`
**Code/test candidate:** `59a03927d61afbf30c138db0201c59afc3c86ab7` (tree `3e1549c50be2b7fa919efa56770d5111c5f1633e`)

This report supersedes the earlier `2121f562` validation description for the current candidate. It preserves that earlier record as historical evidence.

## Review findings addressed

1. Core owner-section catalogue replacement now requires the active `runtime.npc-membership` protocol operation. A valid unrelated `runtime.economy.market-sale` operation is rejected without changing the catalogue revision; the membership operation remains accepted. Generic operation teardown is unchanged.
2. A Unity integration regression now drives an actual Person-to-NPC materialization binding and roster addition through a protocol sharing `DailyV1ExecutionBoundaryState`. It checks the Core-sorted section identities against the provider-facing projection, catalogue cardinality and one-step inventory revision, owner snapshot cardinality/revisions, and one mutation-epoch advance.

## Exact changed code/test inputs

| File | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1ExecutionBoundaryState.cs` | `93BCDFB7107E5F0EE74FA2423056729BBEEAAA4EB1883F0FBEED08F6D434E5EB` |
| `tests/Simulation.Core.Tests/DailyV1RuntimeProtocolOperationContractsTests.cs` | `B8262C7AB37F34985C5ACE1FCACB520CA7EF6C4804BA295E3D49A5712DF6E036` |
| `Assets/_Project/Tests/EditMode/Editor/ContinuationCensusProtocolTests.cs` | `CC048AAD52D4173D01BF93D00E495CE7CDCCAB7EFCA01B81709D377AFA5FBC13` |

The candidate code commit contains only these three files. ProjectSettings changes, local validation folders, and unrelated untracked evidence remain outside the commit.

## Validation

| Gate | Result | Evidence identity |
|---|---:|---|
| Official CoreValidation cold/warm | 888/888 each, PASS | Run `ceb1370d271a4459b481afdc45770171`; working-tree input digest `86a31df68b77506891c50d27843bd2468e37cf38709e30d2aeeba78912536a8e`; cold/warm report SHA-256 values `a87c5b33b1a32c12b5d59bd016614e8686fc50a1ef4ff2acf9c2839dc7c8358b` and `b8dae0409c24196f2ad2a28e8a2a772cba772b998fd8f1899e60e273d5514e2b`. |
| Focused `ContinuationCensusProtocolTests` | 27/27 passed | Initial import run `c388f4574634430680cd0193e0d55a88` was not used as final stable-input evidence: Unity normalized two ProjectSettings files in the disposable validation worktree. The tested code/test file hashes remained unchanged. |
| Unity ALL EditMode | 3,272/3,272 passed; zero failures, skips, or inconclusive | Stable run `c492fcfe1af44b58a2e8938af8eb61be`; 1,956-file input inventory SHA-256 `49722b6d9b8aff83cb0a13142d22159dc41017862d22f4c9d21738cde4c27627`. |
| Official filtered Smoke | 5/5 passed; zero failures, skips, or inconclusive | Stable run `b1b699fb2cf7436cbf2bb046ac2e5acd`; filter `Smoke`; same 1,956-file input inventory and digest as the full EditMode run. |
| `git diff --check` | PASS | Checked before the code commit. |

The full EditMode and Smoke executions used the same three code/test file bytes as the candidate commit. The first Unity import normalized `ProjectSettings/EditorBuildSettings.asset` and `ProjectSettings/ShaderGraphSettings.asset` only in the isolated validation worktree; the final full EditMode and Smoke inputs were stable and identical to each other. The candidate worktree's user-owned ProjectSettings changes were not copied, staged, or modified by Unity validation.

## Durable Unity result package

The repository-approved packager created the full EditMode result package and verified it against the protected original run manifest:

- Manifest: `Evidence/coref-runtime-owner-section-contracts-review-fix-editmode-20261027.manifest.json`
- Archive: `Evidence/coref-runtime-owner-section-contracts-review-fix-editmode-20261027.zip`
- Manifest SHA-256: `1A8C164B1CCDAF4C9D97F4FAA74C7939E56DF2D89728E74A6CFF33D6344BFC5B`
- Archive SHA-256: `42AC8D657AE0FC5F0CE92B0F6AC9F8F67573FEA15448EF39DF622E614EF6CBDB`
- Official package verification: PASS; one run, one archive entry.
- Package manifest scan for workstation paths: none found.

## Evidence boundary

CoreValidation's original TRX files and private manifest remain in protected local retention. The approved TRX adapter does not accept this CoreValidation manifest schema, so no durable .NET package is claimed. The separate filtered Smoke XML also remains protected locally because the approved packager rejects its parent-suite discovery counts; the full EditMode package contains the same five Smoke cases but is not presented as a replacement for that separate invocation.

This evidence limitation blocks canonical promotion. It does not invalidate the successful implementation validation or exact-tip review workflow. No canonical ref or CORE-F completion status changed.