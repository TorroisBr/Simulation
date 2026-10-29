# Independent exact-tip review — P12-E Battle Core owner stage

**Verdict: `VALIDATED_CANDIDATE`**

## Reviewed identity

- Repository: `https://github.com/TorroisBr/Simulation`
- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Exact candidate code commit: `84b1a723f48ed1bf2207f501aedf2bce3ab59916`
- Candidate tree: `129b8a3eaf828e2643b40a26d25de691634360f1`
- Immediate base: `965bafd5980e1e963b4bf3d830d20f57a34eeab6` (tree `55aa5eabcb300d9ce1f32a2b4af2e460dfc52edf`)
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Candidate branch and Core canonical refs were checked against `origin`; they were unchanged during review.

## Scope and findings

The full candidate diff was reviewed against the exact parent. It moves the persistent Battle value contracts, authoritative store, owner snapshot, census provider, and owner codec into `Simulation.Core`, then inserts Battle after the existing Conflict and War private restore stages. The definitions for `PersistentBattleStore`, `PersistentBattleRecord`, the Battle owner snapshot, census provider, and codec each have one authoritative declaration in the Core package; there is no remaining Unity-owned duplicate.

The restore builder requires the preceding exact staged ArmedForce, Conflict, and War owners and passes those same instances plus the exact P8 spatial authority to Battle staging. It assigns the new stage only after snapshot validation succeeds, builds only after the complete ordered chain reaches Battle, and returns no stage on failure. The Unity owner package now consumes the Core-staged `Battles` object in both capture/continuation staging and decoded-image restoration rather than staging a second Battle store. The prior `EBattleOwnerStaged` observer identity is retained through the Core-to-P12-G mapping.

The accepted `UnityBootstrap-Daily-v1` excludes `LocalTopologyStore`. The package therefore passes `localTopologyComposed: false`, and the Battle stage rejects composed LocalTopology. This is consistent with P12-E §2, §4 and the accepted profile boundary. The Core store no longer depends on the Unity `LocalTopologyStore` type: the existing narrow sub-location resolver interface is in Core and the Unity resolver remains an adapter. Existing sub-location test coverage supplies that resolver and retains the supported spatial-reference behavior. This review does not extend Daily-v1, P10, P17, or Battle gameplay scope.

Mutation authority, store revisions, terminal-write preparation/commit, exact owner/parent identities, census section identity/cardinality/revision, detached snapshot values, deterministic row order, and validation-before-private-stage construction remain with the existing domain owners. No active owner is mutated during detached staging. Existing snapshot tests cover exact-empty and populated round trips, detachment, unsupported/composed topology, stale/duplicate/malformed witnesses, bad references, atomic rejection, and saturated revision; the added Core restore-stager test asserts Battle's dependency order and exact shared parent references. No actionable source, authority, atomicity, assembly-boundary, `.meta`, scope, or test-integrity finding remains.

All five moved/new `.meta` GUIDs are valid and unique in the candidate tree. The 100% moved-source metadata retains its prior GUID. `git show --check` and `git diff --check` pass for the reviewed candidate.

## Exact-source validation verified

### CoreValidation

- Private CoreValidation run ID: `bf00c4a8104043cd8c848a743077e7ec`.
- Private manifest SHA-256: `7db7344f0a41248f03c591a42393bc814da7a7da6535da03b1b9bc275fe5720d`.
- Manifest status and input stability: `Passed`, `stable=true`.
- Cold: 926/926 passed; warm: 926/926 passed; zero failed, not-executed, or inconclusive.
- Original cold TRX SHA-256: `0275041d2a8cd5cad7426058b97f8cdaa6408ec981e3d906752dcac2f7adbda3`.
- Original warm TRX SHA-256: `0587500a1a6423ffb21a7f00bdc2fad5be0201bdfd44f9cc39a3aba6d2b7e3e2`.
- The private run manifest records base commit `965bafd...` plus a stable modified-worktree input digest (42 worktree change entries). To confirm applicability after candidate commit, I compared every one of the 529 CoreValidation input file hashes in that manifest byte-for-byte with the exact candidate worktree at `84b1a723`; all 529 matched, with no missing input. The original TRX files also match the manifest hashes. This substantiates the 926 cold/warm results for the candidate's actual .NET build/test input set rather than treating the manifest's base-commit field alone as candidate identity.

### Unity EditMode and Smoke

- Full EditMode private run ID: `93d7768280c74d329b063c5f455b362f`; its stable before/after repository identity is exact candidate `84b1a723...`, tree `129b8a3...`; all 3,279 cases passed, with zero failures, skips, or inconclusive cases.
- The full EditMode package manifest is `coref-p12e-battle-editmode-84b1a72.manifest.json`; the verified archive is `coref-p12e-battle-editmode-84b1a72.zip` (173,741 bytes, SHA-256 `335fbd5806597e11db253722be8c476a48ecca8f7d80b6b0f6b6ba5bc74823c9`). Its single normalized NUnit XML entry has 3,279 test-case nodes and the same candidate/tree identity. The package archive SHA and declared length match.
- Separate official filtered Smoke run ID `9265e7b83b2744dabc234433ca3ef64e` is stable on the exact candidate/tree and passed 5/5, with zero failures, skips, or inconclusive cases. Its protected original XML SHA-256 is `b45bd82e71e1d7f9ab0edf1d47d224ee9e6c87817b93d52c34100001293ac6eb`, matching its manifest and untouched file. The filtered-report packager rejects the nested `Simulation` suite's full-discovery `testcasecount` of 3,279 against the five selected case nodes; the filtered XML remains private and unchanged. The independently verified, packaged full EditMode XML contains those same five test identities with identical `Passed` outcomes. This is the established evidence disposition recorded in current `docs/CORE_STATE.md` for prior CORE-F validations: keep the filtered original private and use the verified full report containing all five Smoke identities. Accordingly, this packaging limitation is recorded but does not block this candidate.

No validation report or candidate source/test file was modified during review. The candidate worktree's unrelated ProjectSettings changes, untracked files, metadata, and protected evidence remain untouched.

## Decision and limitations

The Battle owner extraction/staging slice is an independently reviewed, exact-tip validated candidate. `VALIDATED_CANDIDATE` is not canonical promotion. Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`; do not treat this slice as P12-E completion, P12-A readiness, CORE-F completion, or a Phase closure. Existing broader CORE-F application/host and P12 integration obligations remain governed by their separate contracts.
