# CORE-F Owner Entry Qualification — Validation

## Candidate and scope

- Core canonical base: `1998974515b6357c308d861fb608f2db5071a63a`.
- Architecture contract: `512cf5721927a5b3d6766bc4d19cfd62a6729a41`, `CORE_F_OWNER_ENTRY_CONTRACT.md` blob `2cdcffe0760271cd6e31ee63cf8b23d17760f8f3`.
- Test/evidence implementation candidate: `af411093de1b95cf46ae7cdfad595864bca62ee4`.
- Candidate source tree: `99e74525432efe0d2ff3fed5c60bf12a1922bdaa`.
- No production `Assets/_Project/Scripts`, shared Core, package, or public runtime API was changed. This is the approved test/evidence qualification package only.
- All 18 no-writer disposition rows remain `Required`; their scope is preserved/restricted owner state and supported-operation conservation. No writer or hard-zero rule was invented.
- Shared Runtime/Application/Host remains `WAIT_DEPENDENCY`. This evidence does not declare it ready.

## Owner and source/access qualification

The deterministic qualification generator completed against the exact candidate and promoted architecture sources:

| Evidence | Result |
| --- | ---: |
| Owner-entry chains, with declaration/runtime member/live owner/writer or disposition/capture/hydration links | 89/89 complete |
| Required owner disposition rows retained | 18/18 |
| Split host source files | 67 |
| Row-linked split-host destination references | 18,270 |
| Split-host destination groups | 3,365 |
| Selected member-access callsites | 42,625 |
| Core declaration pairs / Unity-engine symbol pairs | 2,582 / 288 |
| Unresolved Core declarations | 0 |
| Source-integrity and absolute-path checks | PASS |

`MemberAccessCut.cs` compiled and its semantic projection completed with zero compile errors. The post-institution source join passed with 89 owner/capture anchors, 24 operation records, 1,660 references across 142 unique paths, zero unresolved references, 220 runtime source files, and 289 restore source files. The current owner identity and selected Required Knowledge zero checks passed. The two stale political-restore test anchors were refreshed to the current source blobs; no production source identity changed.

The qualification artifacts are bound by `qualification-manifest.json`. Its six declared artifact hashes were verified. The raw member-access projection has SHA-256 `c7714eaaccb0b53712017d93157fa7f94a05911f37f28cd0b4fb094d21f945a8`; the compressed projection has SHA-256 `8eb054a7b59d511d864b2bee707a8d62ead1ea78002be9766a85fbf754467121`. The generated manifest SHA-256 is `42ad8c5543a8380bd002b7517f7b957424e32c8f6ad395247ebc32637296dafc`.

## Unity validation

Unity `6000.3.9f1` and Test Framework `1.6.0` ran against the exact candidate commit/tree. Each run recorded stable inputs: 1,668 inputs, snapshot SHA-256 `38ada9fd9c997e9ce8482771bb04a6cd61b7f893920fce1d0408e30b27e3477d`, and no input changes during execution.

| Gate | Run | Result | Original report SHA-256 |
| --- | --- | ---: | --- |
| Focused `P12GPoliticalBindingRestoreTests` | `511af7652f3241b9b26289954e319225` | 15/15 | `ce7f08ad873b4b361da912e381e776b0a44090d30f717773f6fa6ed052d819c8` |
| Full EditMode | `53d0fea85e1249e69b290ca6f2bf6609` | 3184/3184 | `9f152366d88faf5b7b88560432136b2fe0c931cf021ba10d766994ad0a5ec90a` |
| Official filtered Smoke (`EditMode`, `-TestFilter Smoke`) | `34bdbf644869488698689638bf4b6255` | 5/5 | `7b416b2092a1b711778d5173789b0278c4b1e2cfea68218db325ae1e907cc444` |

All runs had zero failed, skipped, or inconclusive tests and coherent counts. The five official Smoke identities each occur exactly once with a passing result in the full EditMode report. The filtered Smoke XML is retained unchanged in protected local evidence; the strict public packager rejects filtered reports whose ancestor suite counts describe full discovery, so no separate Smoke package is claimed. The same packager was not weakened. Original XML and raw logs remain in protected local retention and are not included in the public package.

The full EditMode report was packaged and verified against its original private runner manifest. The normalized package contains the complete result XML and no raw logs:

- Archive: `Evidence/core-f-owner-entry-qualification-final-editmode.zip`.
- Archive SHA-256: `8f4826dda517dd31a1c479f1dafc350741448e47dbdae006713ba08edcbe63ba` (167,889 bytes).
- Public manifest: `Evidence/core-f-owner-entry-qualification-final-editmode.manifest.json`.
- Public manifest SHA-256: `7f4ee638a411678128fc504eff15b75e44344d1905b96642819f7686aae6a325`.
- Package verification: PASS; one run, one report entry; original-to-normalized comparison verified.

## .NET regression evidence

The approved CoreValidation cold/warm evidence was reused because its full 142-path effective-input snapshot exactly matched the current candidate input snapshot, SHA-256 `9f69d9ba3b24a32deef96099ec3fe608a45f0394ec7656d198e0a12c4bb92061`; no CoreValidation input changed. The retained prior run `5d026bdb1d64473fb3d8b004ec8f3151` passed 188/188 cold and 188/188 warm against source `ca55c3f9fa4d9c99e6a47a25adbbd1956ea04a1d`. The package is evidence reuse under the handoff’s exact-input-equality rule; no new .NET run is claimed.

## Integrity and preservation

- `git diff --check`: PASS.
- The test/evidence changes are confined to Editor test sources and audit/validation materials; no production implementation changed.
- The pre-existing `ProjectSettings/EditorBuildSettings.asset`, `ProjectSettings/ProjectSettings.asset`, and `ProjectSettings/ShaderGraphSettings.asset` edits remain unstaged and untouched.
- Generated `bin`, `obj`, and Python cache directories are not part of the evidence payload.
- This qualification does not change checkpoint identity, create writers, widen the selected Daily-v1 profile, or satisfy shared runtime/restore, detached host, persistence, or continuation implementation gates.
