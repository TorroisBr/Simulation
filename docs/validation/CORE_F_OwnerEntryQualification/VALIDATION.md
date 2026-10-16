# CORE-F Owner Entry Qualification — Validation

## Candidate and scope

- Core canonical base: `1998974515b6357c308d861fb608f2db5071a63a`.
- Architecture contract: `512cf5721927a5b3d6766bc4d19cfd62a6729a41`; `CORE_F_OWNER_ENTRY_CONTRACT.md` blob `2cdcffe0760271cd6e31ee63cf8b23d17760f8f3`.
- Unity test/source candidate: `5b46ee946a1e29813c11c4dd2bc5b34b5b6a25da`, tree `b3bc684fdbd125cc369f46fdf88c887b163d7164`.
- Every Unity run below used that same candidate/tree and stable 1,668-file input snapshot `9ea055ad1bf6fe51158bde011f66105b7402a56930cc62217f4c69bedbb5c389`.
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

`qualification-manifest.json` binds the six generated qualification artifacts, source/access inventories, code/test identities, architecture and disposition records, and the anchor input. Its SHA-256 is `ab194847f4fe8577563503add81cda79f4f7bf8698ad89ecf5fdaed2bcdb2c36`. The owner-entry ledger SHA-256 is `13ec49ec5d4d02718558f21a331c0daee130da606c7197e62d323e4d731ef3ee`. The raw member-access projection SHA-256 is `c7714eaaccb0b53712017d93157fa7f94a05911f37f28cd0b4fb094d21f945a8`; its compressed projection SHA-256 is `8eb054a7b59d511d864b2bee707a8d62ead1ea78002be9766a85fbf754467121`.

## Unity validation

Unity `6000.3.9f1` / Test Framework `1.6.0` ran the following scopes on the exact test/source candidate above. Each run had a stable input snapshot and zero failed, skipped, or inconclusive tests.

| Gate | Run ID | Result | Original report SHA-256 |
| --- | --- | ---: | --- |
| Focused `P12GPoliticalBindingRestoreTests` | `70caf0785a53432c9afbee8e2e32cc6f` | 15/15 | `0fb469610cce6950f875479f268557d7006bed0aef704e3636e8041fb13d137f` |
| Full EditMode | `b0775247948f40418bb75fcd1b559568` | 3184/3184 | `7d9b6a1f8e0b7eecaad558b206f96fe4bf3b3a56a9a4f39d80e7387aa90add91` |
| Official filtered Smoke (`EditMode`, `-TestFilter Smoke`) | `54eb77f085c9446185a4f0bf84a9b43e` | 5/5 | `4681176a64e20e355ea8a5a8b0099005bff9824faedc79ed8989338e783f46ed` |

The three run manifests record the same candidate commit/tree and input digest. The full report contains all 3,184 case results, including the complete 15 focused cases and all five exact Smoke identities/results. An independent XML comparison confirmed that every focused and Smoke `(identity, outcome, label)` record is present in that full report and that the Smoke run contains exactly five passing cases.

The full EditMode report is packaged through the approved result-only Unity evidence packager and verified against its private original run manifest. No raw log or original local path is included in the public package. The standalone focused and filtered Smoke original reports remain unchanged in protected local retention under the runner's `%LOCALAPPDATA%\Temp\Simulation\COREFOwnerEntryQualification-20261016\<RunId>\original` directories; the canonical result package carries the complete per-case results in the full report. No separate Smoke archive is claimed.

- Archive: `Evidence/core-f-owner-entry-qualification-editmode-5b46ee94.zip`.
- Archive SHA-256: `79346f257e0964572cb76c93611ad27b4816252628f6a74c30d40027fbb80d5c` (168,092 bytes).
- Public manifest: `Evidence/core-f-owner-entry-qualification-editmode-5b46ee94.manifest.json`.
- Public manifest SHA-256: `97bdbc4cc6c1ee3134085884f7670577f45f9a6f5f2d9c7e6901875f2a0c3a19`.
- Package verification: PASS; one full run, one complete normalized result entry, original-to-normalized comparison passed.

## .NET regression evidence

The approved CoreValidation cold/warm evidence was reused under the handoff's exact-effective-input rule. Its full 142-path input digest is `9f69d9ba3b24a32deef96099ec3fe608a45f0394ec7656d198e0a12c4bb92061`, equal to the current input snapshot; no CoreValidation input changed. Run `5d026bdb1d64473fb3d8b004ec8f3151` passed 188/188 cold and 188/188 warm against source `ca55c3f9fa4d9c99e6a47a25adbbd1956ea04a1d`. This is reuse of the existing durable .NET package, not a new .NET execution.

## Integrity and preservation

- `git diff --check` is required at the final exact candidate tip and will be recorded with the independent review.
- Generated `bin`, `obj`, and Python cache directories are not part of the evidence payload.
- Pre-existing `ProjectSettings/EditorBuildSettings.asset`, `ProjectSettings/ProjectSettings.asset`, and `ProjectSettings/ShaderGraphSettings.asset` edits remain unstaged and untouched.
- The qualification preserves all 89 families, the 18 Required dispositions, current owner identity/cardinality, and existing rejection/restore semantics. It creates no gameplay writers, no profile expansion, and no production implementation.
- This qualification does not itself satisfy shared runtime/restore, detached .NET host, v2 persistence, or deterministic .NET continuation requirements.
