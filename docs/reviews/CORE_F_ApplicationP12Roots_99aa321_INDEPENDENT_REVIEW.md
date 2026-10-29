# Independent Exact-Tip Review — CORE-F Application P12 Roots

**Verdict: `VALIDATED_CANDIDATE`**
**Reviewed:** 2026-10-29

## Exact identities

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Candidate commit: `99aa3218980dae9d78be7bc52bdb8a695ad19efb`
- Candidate tree: `ef4ff33985802f1f63df52b36e90ceaf0fe4e1f6`
- Immediate base / parent: `f2cf0db17d753117f48df77061b22bd8249dc078`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Remote candidate ref matched the reviewed commit at review time.
- Reviewed only the incremental five-path delta from the prior exact-tip checkpoint; no canonical promotion is claimed.

## Review result

No actionable code or test-integrity findings.

The save operation captures P12-C roots once and retains the resulting typed snapshot against its exact operation, witness, token, and owner-vector references. Capture checks the active operation/session graph and retained root bindings, takes snapshots of world identity, allocator, record sequence, spatial authority, P9 manifest, and deterministic random root, validates the staged roots, then revalidates the completed source boundary before binding the snapshot. Subsequent composition reuses the bound snapshot rather than recapturing it.

The Core application derives the image header and P12-C payload sections from that snapshot. It checks the Unity v1 or .NET v2 compatibility descriptor's P8/P9 fingerprints against the Core snapshot, enforces the selected profile identity/version, and enforces the .NET host kind for v2. The existing political-world revision scalar is passed through unchanged. The causal-root codec checks world identity, P8 fingerprint, P9 compatibility, allocator aliases, record sequence, and spatial encoding. The final image is sealed with the operation witness's exact owner vector, with revalidation before and after composition. Closed-operation rejection and the previous null-operation legacy route remain intact; the legacy header/root/owner encoding order is unchanged.

The added tests assert the unchanged political revision, Core P9 manifest object identity and seed, image/token census parity, and explicit P8 and P9 descriptor mismatch rejection. Those mismatch calls provide empty owner rows and assert rejection before section composition. The existing closure test was updated only for the new method signature and still asserts closed-operation rejection. No assertions were weakened.

The incremental candidate changes only the restore coordinator, `DailyV1Application`, the continuation-save operation, focused tests, and its scoped validation record. No ProjectSettings, `.meta`, private evidence, or unrelated files are part of this delta.

## Exact-source validation reviewed

The four changed executable/test input hashes in the validation record match the candidate worktree and both runner input inventories:

| Input | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` | `4b7410a8002c2837b57022dae4a811d01a29f53cf81cd1543a134298ad0d1968` |
| `Packages/com.simulation.core/Runtime/Persistence/DailyV1P12GContinuationSourceWitness.cs` | `84173d9b8b270e7f54d89c30d60f1272dec3e2644d92cb1581937848385ebee5` |
| `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.cs` | `9c68a5f12a4963742fc9f2837e63ca481293f81852c421110ea35a0a5917ae7f` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` | `2384f41aa49f1bcda4d7b4268b6fe896dfc2a570f50e24f89ac20f657232ea36` |

- Official CoreValidation run `931df7aaf1bd4cd0914a975e9aa318a3`: cold 936/936 and warm 936/936, no failures or skips; stable input digest `0dc384023a0bf8e64ee2b05e29331478f6dac4174e13f40f95fa1d55e2ac20fd`. Protected manifest SHA-256 `a1f905e72561c9234b4d5aee33634e1b29c3e31cbb0cc73ed0a08e2cfcf56971`; all listed report/log hashes matched their protected originals.
- Focused Unity persisted-image restore/continuation run `e28c65aa61084427a39a3bc8e7590053`: 1/1 passed with stable before/after input digest `450263c712b31b6cbc13a2918c4969c59b9fdee6924cda4fc98a98d1005bde1e`. Manifest SHA-256 `fd755b329cc0ac7e099e7f68416945d44f4e3b66de1b638e2794a31c7cee8007`; original XML SHA-256 `d1d55fb9034ac00484d0de39e29fdbbd8e04b032d7e0bf81469458846daa252c`; original log SHA-256 `6044aa8d707046c60bb76ce8632edbd5dca6ddc55499ab7e85fd1fbf3dc4ae22`.
- Focused Unity save-operation closure regression run `c95ff16483ed482bacd8d1679fa7ffb9`: 3/3 passed with the same stable input digest. Manifest SHA-256 `d0ca0504e9bd73ea26642893963932a26ace9d02be0a7eed6672cca67d6aa020`; original XML SHA-256 `ad14e9754da38bcc0c17ca2993e4503bbb40ff7e00c9e2204cdef1949045325a`; original log SHA-256 `a8d436a735ede22a7888e16c3ed4e22a21906b500fd3a98e192c74742f0e3a28`.
- `git diff --check` passed for the exact base-to-candidate diff.

Raw reports/logs and manifests with machine-local metadata remain in protected evidence storage and are not included in this record. No full Unity EditMode, Smoke, permanent privacy-safe package, or complete CORE-F acceptance is claimed. Broader integration validation and evidence packaging remain required before any future promotion.

## Promotion status

This record validates only the bounded P12-C root/header image-sealing increment. The Unity coordinator still owns owner-row encoding and detached restore orchestration. No canonical promotion or CORE-F completion is approved or claimed.