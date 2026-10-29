# P12 Core-owned restore C/D validation

This record binds the reviewed C/D candidate checkpoint to its exact validation evidence. It is a candidate checkpoint only; it does not promote Core canonical or claim CORE-F completion.

## Source identity

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Base HEAD: `9e8daed31185fbb311691ec473b0dff6052e7ec6`
- Base committed tree: `02375ae23c3f94e05e72472b58db6dea55983989`
- Tracked implementation diff SHA-256: `911c8722b434e7e0c22a01558eda97c2ff06fd8eb0ed047f029435f539118950`
- `git diff --check`: PASS

## Unity EditMode

- Unity: `6000.3.9f1`
- Selection: all EditMode tests; 3,279 passed, 0 failed, 0 skipped, 0 inconclusive.
- Protected run ID: `d0b5f3cf8a8a4fc2bc96a4e06c67f52d`
- Input snapshot stable: yes; Unity executable stable: yes.
- Package archive: `coref-p12d-editmode-all-20261029.zip`
- Archive SHA-256: `fdcb4d83fb434fae65cb07bd547ee35641a82c0eefd6a0b2ec51fcadcbc3e64e`
- Package manifest: `coref-p12d-editmode-all-20261029.manifest.json`
- Package manifest SHA-256: `b676672de6bdd9454a63701d003cd239aed3da2026af73c6f3a3650f25b69a2b`
- The archive verified against the package manifest and protected original run manifest.

## CoreValidation

- Protected run ID: `73bd517624f9435bbc39704836693f08`
- SDK: `9.0.315`; selected `Microsoft.NETCore.App`: `8.0.28`.
- Cold: 932/932 passed. Warm: 932/932 passed. No failed, skipped, inconclusive, timed out, or unexecuted cases.
- Working-input digest: `abea529ba354b5de47344841533e0085b0cff773a10f80ab0e4836ffc3d29f7f`; input and generated dependency snapshots remained stable.
- Private manifest SHA-256: `ae9cdb85346530df9547f5ec33f79a4fd4d644e6e6498880ff54e57e8454fd1d`.

Original reports:

| File | Bytes | SHA-256 |
|---|---:|---|
| `CoreTests-cold.trx` | 1,339,386 | `2fd39840bddec9b56179850c87ca787e1c629098c0ae9c4c820d326d1d178c19` |
| `CoreTests-warm.trx` | 1,339,386 | `f343da8d62c9a644c62dd95727aa21dee204fe212b8dfdc647b6ce5d1573b163` |

Original command logs:

| File | SHA-256 |
|---|---|
| `sdk-version.log` | `a69d5385a85ad3607ec943b7cb1efad3a9b747affcbaa838b252d5a7488860a8` |
| `dotnet-info.log` | `efb8154f89c4389cbb2f06aa3593ad371c0b439bb8c0539c7a4c619bc8c3dcbf` |
| `dotnet-runtimes.log` | `2a184ccf85dfae8925b716c0128847839e9d7c3984174a125d82db17295da92e` |
| `restore-tests-locked.log` | `2ad9967f2280d0195ad457a92fe6a353a8492625846632386df6dfc446efe4bc` |
| `restore-demo-locked.log` | `2ad9967f2280d0195ad457a92fe6a353a8492625846632386df6dfc446efe4bc` |
| `restore-lab-locked.log` | `2ad9967f2280d0195ad457a92fe6a353a8492625846632386df6dfc446efe4bc` |
| `cold-build.log` | `9c2512010f97ac4cccc56bfca239f42e9e12f37f568860b1122fc7c7d094f823` |
| `list-tests.log` | `30c6641f3eec5ec43361255ce09affd690879461e51985ce7302b558e2c1e067` |
| `cold-test.log` | `18bbdcecbf6652cbdac58c56e7bf524787691fb1f2620c68579f135c761c2573` |
| `warm-test.log` | `8eb49b81dedef302274e6f2ceedd4232e82b2962b9d66f706964b6486ab77a74` |
| `cold-demo.log` | `1ae98819d73f4bb27a060df204eadb3b2ac1de795de7ff1532033f38714a49db` |
| `warm-demo.log` | `1ae98819d73f4bb27a060df204eadb3b2ac1de795de7ff1532033f38714a49db` |
| `lab-build.log` | `d6bb7a68b21d8936f4fbcb3f627d464b1fcb31362f19f89dc03ca797fbe97cb3` |
| `lab-cold.log` | `a47346cc527c3a29a1ecc2aa334dfd94b6c1d5d3336ffda445fc6be4d102f405` |
| `lab-warm.log` | `a47346cc527c3a29a1ecc2aa334dfd94b6c1d5d3336ffda445fc6be4d102f405` |

The private cold and warm host-selection traces were 303,054 bytes each, selected CLR `8.0.28`, and have SHA-256 values `22e940842ab7a9722dc9593fa5d2d4ad515b857dbb4b9251265410c43a8f40d0` and `755dca6fdb03dbba60018d03a6fc89cf847e54a30ef3af9eff943317fb1e0bf5`, respectively. Private original reports, logs and manifest remain in the protected evidence store; only the privacy-safe normalized Unity archive and its public manifest are included here.

The CoreValidation runner noted that this checkout contains a Unity `Library`; these results do not claim .NET validation independent of Unity Library.