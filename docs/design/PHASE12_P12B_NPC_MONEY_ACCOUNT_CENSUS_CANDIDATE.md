# P12-B per-NPC MoneyAccount census implementation candidate

**Candidate branch:** `codex/phase12/P12BNpcMoneyAccountCensusImplementation`.
**Canonical base:** `43dba1b5d16cba1558c4c39239f3cd0d4c669958`.
**Code-bearing commit:** `2bdd0990acc2bdc2d6073b0863fc1ae94a209c4d` (tree `58f0e76525a0ee6dc9b7f73f34dcbf71db944a09`).
**Design authority:** reviewed P12-B per-NPC MoneyAccount census boundary and approved implementation scope.
**Status:** independent exact-tip implementation review PASS; durable record: `PHASE12_P12B_NPC_MONEY_ACCOUNT_CENSUS_IMPLEMENTATION_REVIEW.md` (this docs-only commit). Awaiting separate canonical promotion approval.

## Delivered boundary

Adds a passive schema-v1 owner-section provider for each currently rostered NPC. Section IDs use the NPC RuntimeId; provider order is ordinal by RuntimeId. A witness binds the exact already-installed `MoneyAccountRuntime`, reports cardinality exactly one, and reads that owner's existing local revision. Missing owners, duplicate RuntimeIds, aliased account owners, and an in-place owner replacement fail closed. The census does not materialize an absent account.

The family is reconciled with the existing NPC roster membership census. Add/remove and separate same-ID re-registration publish the current provider snapshot; the exact owner and account identities remain stable for retained NPCs. A same-roster account replacement does not silently rebind a published section.

Coverage includes NPC roster changes, duplicate/aliased identities, absent/replaced accounts, positive debit and credit revision changes, repeated read stability, and unchanged assessment for zero/failed writes. Selected-profile composition asserts one exact account owner per NPC.

This is a passive local-revision census only. Debit/credit writes are not wired to the protocol mutation epoch. No shared-epoch completeness, runtime operation, capture eligibility, export/hydration, P12-B readiness, P12-A readiness, or P13 readiness is claimed. P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked. Unrelated ProjectSettings edits and untracked ArmedForce `.meta` files are outside this candidate and remain untouched.

## Validation on the code-bearing tree

Validation used `Tools/UnityValidation/Invoke-UnityValidation.ps1`; result artifacts are retained in `Library/ValidationResults/P12MoneyAccount/` in the candidate worktree.

| Gate | Result | XML and SHA-256 | Log and SHA-256 |
|---|---:|---|---|
| `EditMode -TestFilter NpcMoneyAccountCensusTests` | 10/10 passed | `EditMode-20261001-174933-5871433c5bf84ace9d5aef5f5b8111ba.xml` — `3ABF17399E635D9F8B4991931DC27564B7F7BA01D50FAEBCFD4AE85D59D8F73F` | `EditMode-20261001-174933-5871433c5bf84ace9d5aef5f5b8111ba.log` — `17D9BA37EE6FAB6C211920AEE82306B981F965DAC040EF86452BF6484FF173FF` |
| `EditMode -TestFilter SimulationBootstrapCompositionTests` | 14/14 passed | `EditMode-20261001-174950-85798b561c3549cf99f7d5ac3352d2e4.xml` — `EFE3F64218DECC61F7A7A407698C11E8CC09D6F49D9744F9E4A0B2DA79371BA3` | `EditMode-20261001-174950-85798b561c3549cf99f7d5ac3352d2e4.log` — `8CB5669C7E93ADD53FAA857E7885B4D37507B1417FC04305614A9F60BDFD3297` |
| `EditMode -All` | 2118/2118 passed | `EditMode-20261001-175008-fbad0d37a06348e7987e9a247357d384.xml` — `6C42E21ABDC85A17334E9B8F3C4A57B667F3B95A65AC4CC602A88A7FDEB5735C` | `EditMode-20261001-175008-fbad0d37a06348e7987e9a247357d384.log` — `14115E8085089E9C605DD633A0128611354E3614767CD5187EBC5A052511905F` |
| Official `EditMode -TestFilter Smoke` | 5/5 passed | `EditMode-20261001-175049-eeaf47dcaa35450a941351284cd1f589.xml` — `1B795F1FDC4DC4AAB56947BA71808F3336D55B78AF4AADEC9833FD4F4C2879C6` | `EditMode-20261001-175049-eeaf47dcaa35450a941351284cd1f589.log` — `1D8C74CD999C3E52E66FA18C5E8B12EBA035B09A663E4D859B359E981B91235A` |
| `git diff --check` | passed | — | — |

The four XML result summaries were parsed from the retained artifacts; all report `Passed`, with zero failed and zero skipped tests. The candidate commit contains only the census source, runtime/protocol integration, focused tests, and their two Unity metadata files.
