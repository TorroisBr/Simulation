# CORE-F Owner Entry Qualification — Validation

## Candidate and scope

- Core canonical base: `1998974515b6357c308d861fb608f2db5071a63a`.
- Architecture contract: `512cf5721927a5b3d6766bc4d19cfd62a6729a41`; `CORE_F_OWNER_ENTRY_CONTRACT.md` blob `2cdcffe0760271cd6e31ee63cf8b23d17760f8f3`.
- Unity test/source candidate: `350d0aaa44f657379da9e1009838896a3da9ffef`, tree `ff290a1b6daa6b68a5e4f4b312a32fca467d3aea`, based on prior reviewed test candidate `5b46ee946a1e29813c11c4dd2bc5b34b5b6a25da`.
- Every Unity run below used that same candidate/tree and stable 1,668-file input snapshot `6bd36d113e9b8bf82322b309121b1d7d9479f9450b34831122e1ebd3138414f7`.
- This is the approved test/evidence qualification package only. No production `Assets/_Project/Scripts`, shared Core, package, profile, or public runtime API changed.
- All 18 no-writer disposition rows remain `Required`; the evidence preserves state and supported-operation conservation without inventing writers or universal hard-zero requirements.
- Shared Runtime/Application/Host remains `WAIT_DEPENDENCY`; this qualification does not declare it ready by itself.

## Owner and source/access qualification

The deterministic qualification generator completed against the exact Core canonical source, promoted architecture contract, source/capture/writer matrices, selected live-identity assertions, and pinned compiler-access inventory.

| Evidence | Result |
| --- | ---: |
| Owner-entry chains with current declaration/runtime member/live owner/writer or disposition/capture/hydration links | 89/89 complete |
| Required owner disposition rows retained | 18/18 |
| Split-host source files | 67 |
| Row-linked split-host destination references | 18,270 |
| Split-host destination groups | 3,365 |
| Selected member-access callsites | 42,625 |
| Core declaration pairs / Unity-engine symbol pairs | 2,582 / 288 |
| Unresolved Core declarations | 0 |
| Unassigned or unresolved destination groups | 0 |

The source-join audit reports 89 owner/capture anchors, 24 operation records, 1,660 references across 142 unique paths, zero unresolved references, 220 runtime source files, and 289 restore source files. `MemberAccessCut.cs` compiled with zero errors. The selected live-owner identity and Required-Knowledge checks passed.

The owner ledger now separates current source anchors from historical citations. Each current anchor is checked against its source line and Git blob in Core canonical `1998974515b6357c308d861fb608f2db5071a63a`; the source reference and candidate working tree must have the same blob. The extraction commit recorded by the anchor CSV is retained as provenance, not mislabeled as the canonical source authority. Historical production-base anchors remain in a separate column. The 89-row anchor input has SHA-256 `97c1a6ed140798a46230254ba7f63a9e219dbb7558f7068acae234433d5e4533` and Git blob `292d6ae1d7f6e0ba8da40a509a104745c788b2ed`.

All 3,365 destination groups are assigned: 2,492 same-Core-assembly groups, 191 promoted host-neutral compatibility-helper groups, and 682 Unity-edge groups (515 object/fake-null, 125 authoring/lifecycle, 25 host identity/path, 12 logging, and 5 input adapters). Every group links to the original compiler rows by ordinal and input-row hash.

`qualification-manifest.json` binds the six generated qualification artifacts, source/access inventories, code/test identities, architecture and disposition records, and the anchor input. Its current SHA-256 is `509146cf7931f58ba4c29f3d0199b06e667a9e614213d8d0440ec532f6f1ddfa`. The owner-entry ledger SHA-256 remains `13ec49ec5d4d02718558f21a331c0daee130da606c7197e62d323e4d731ef3ee`. The raw member-access projection SHA-256 is `c7714eaaccb0b53712017d93157fa7f94a05911f37f28cd0b4fb094d21f945a8`; its compressed projection SHA-256 is `8eb054a7b59d511d864b2bee707a8d62ead1ea78002be9766a85fbf754467121`.

## Unity validation

Unity `6000.3.9f1` / Test Framework `1.6.0` ran the following scopes on the exact test/source candidate above. Each run had a stable input snapshot and zero failed, skipped, or inconclusive tests.

| Gate | Run ID | Result | Original report SHA-256 |
| --- | --- | ---: | --- |
| Focused `P12GPoliticalBindingRestoreTests` | `941958ce38f546b2ab98ca48e8aaf17d` | 15/15 | `d232420f9fdbf1159af699032455916fb8b5b1f7eaa22f0709ed1ef692d32466` |
| Full EditMode | `e97a10e389b1477892b55483d80be993` | 3184/3184 | `a3955cc4907e96a949af1198525d748b9e2f84440f7e662a5f8e0bcfe1ca588b` |
| Official filtered Smoke (`EditMode`, `-TestFilter Smoke`) | `e6b349f9596e412bbf25f52efea1cfcf` | 5/5 | `c3381d2c2325727e8846debbcf1c06691b9c0ef00a2cd45a474d56313cb45e91` |

The three run manifests record the same candidate commit/tree and input digest. The full report contains all 3,184 case results, including the complete 15 focused cases and all five exact Smoke identities/results. An independent XML comparison confirmed that every focused and Smoke `(identity, outcome, label)` record is present in that full report and that the Smoke run contains exactly five passing cases.

The full EditMode report is packaged through the approved result-only Unity evidence packager and verified against its private original run manifest. No raw log or original local path is included in the public package. The standalone focused and filtered Smoke original reports remain unchanged in protected local retention; the canonical result package carries the complete per-case results in the full report. Independent XML comparison confirmed that all 15 focused and all five Smoke `(identity, outcome, label)` rows occur in the 3,184-case full report. No separate Smoke archive is claimed.

- Archive: `Evidence/core-f-owner-entry-qualification-editmode-350d0aaa.zip`.
- Archive SHA-256: `7341f80b684cf7a9f08167721e5b01a2735792f9a2d3278b5a68925b1e388d4b` (167,898 bytes).
- Public manifest: `Evidence/core-f-owner-entry-qualification-editmode-350d0aaa.manifest.json`.
- Public manifest SHA-256: `b7113c30fa0bbc8dda96b752a40a00e3d9d3215ff7ec1aab6d72de22be8b6fc6`.
- Package verification: PASS; one full run, one complete normalized result entry, original-to-normalized comparison passed.

The split-ArmedForce-alias rejection case now submits the malformed image to the integrated P12-G restore coordinator, checks that no restored session is returned, verifies the source completed-boundary token and typed signature remain unchanged, then retries the valid image and checks byte-identical output. The P12-D local-topology exclusion fixture is explicitly grouped: it contains two required place endpoints and one connection, and asserts those counts before checking rejection. It does not claim to isolate the connection predicate from the required endpoint-place observations; the separate place-only exclusion case remains in the qualification map.

## .NET regression evidence

The approved CoreValidation cold/warm evidence was reused under the handoff's exact-effective-input rule. Its full 142-path input digest is `9f69d9ba3b24a32deef96099ec3fe608a45f0394ec7656d198e0a12c4bb92061`, equal to the current input snapshot; no CoreValidation input changed. Run `5d026bdb1d64473fb3d8b004ec8f3151` passed 188/188 cold and 188/188 warm against source `ca55c3f9fa4d9c99e6a47a25adbbd1956ea04a1d`. This is reuse of the existing durable .NET package, not a new .NET execution.

## Independent review and integrity

- Exact-tip independent review passed for evidence tip
  `07d1b2c62dccb71f33f1c7beec5db5f65ebc990e` (tree
  `e400dca4137314f0307136ae0a5d67f124a9d7ae`), reviewing source/test candidate
  `350d0aaa44f657379da9e1009838896a3da9ffef` (tree
  `ff290a1b6daa6b68a5e4f4b312a32fca467d3aea`). Verdict: PASS / VALIDATED_CANDIDATE;
  no actionable findings. See [`INDEPENDENT_REVIEW.md`](INDEPENDENT_REVIEW.md).
- `git diff --check` passed at the reviewed evidence tip. The review-record
  successor changes documentation only; production/test source and packaged
  validation bytes remain unchanged.

- Generated `bin`, `obj`, and Python cache directories are not part of the evidence payload.
- Pre-existing `ProjectSettings/EditorBuildSettings.asset`, `ProjectSettings/ProjectSettings.asset`, and `ProjectSettings/ShaderGraphSettings.asset` edits remain unstaged and untouched.
- The qualification preserves all 89 families, the 18 Required dispositions, current owner identity/cardinality, and existing rejection/restore semantics. It creates no gameplay writers, no profile expansion, and no production implementation.
- This qualification does not itself satisfy shared runtime/restore, detached .NET host, v2 persistence, or deterministic .NET continuation requirements.
