# CORE-F P12-D Direct City Restore Inputs — Validation

**Status:** validated candidate implementation; not promoted to Core canonical. This bounded integration routes the City values returned by the Core image decoder directly into the existing Core selected-owner stager. Unity adapts the resulting Core owners to its existing City runtime projection.

## Source identity

- Repository: `TorroisBr/Simulation`
- Candidate branch: `codex/core/CORE-F-p12d-core-hydration`
- Candidate code commit: `86c6677a1e05cd57271661cb2aa7d6596859e303`
- Candidate tree: `7fd42b376c98e6298422c3ef958c7ae14e16be4e`
- Candidate base: `39b29f81b40b01b62ebeb0c872b347c637e4512d`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

The code commit contains only the eight City decoder/adapter/stager and focused test files. Existing user-owned `ProjectSettings` changes, `.meta` files, private evidence, and unrelated untracked files were left outside the commit.

## Validation

- Official CoreValidation run `b56897bbf0d946bc98b75351b8b73ff0`: **787/787 cold and 787/787 warm PASS**. Locked build, CalendarDemo, and Lab gates passed. The private owner-only manifest SHA-256 is `b3b10a338eca3a19b22fb8abc75104aca9d378c54b3d2f337f1ede93c9da0e8b`; its 426 recorded Core/test input hashes all match the committed candidate. Input digest: `dd30a249901cae1517be77e9d4f76401a25ff7f99f4b910b16c89f59823ffcc2`.
- Focused Unity EditMode `P12GPoliticalBindingRestoreTests`: **15/15 PASS**, run `b5fd9d0cf1e04a1eb170b1977060ba0f`; original XML SHA-256 `f339bf622fa1f28a18e92aec53aabbe6515ffd04e89a29d92b3d477697bed6f1`.
- Full Unity EditMode run `28ad6df6a5d041cab7d636dc82c88fab`: **3264/3264 PASS**, with zero failed, skipped, or inconclusive cases. The stable 1,912-file input digest is `9bda6c1d4608d15d27c11262017e434eaddbb9f917937852efb969a646d832bf`; original XML SHA-256 `baf9d3e24faa0a18e3f90589d48658a657e0408cf94d92225aa28d15c23c2d09`. Every recorded input hash was rechecked after commit; 0 mismatches.
- Official EditMode Smoke filter (`-TestFilter Smoke`) run `2f8193c72b224e1289a322228a40916d`: **5/5 PASS**, zero failed/skipped/inconclusive; original XML SHA-256 `3b48612338e51f3a3895f4cbf79a7b0a1ab3b00359aa1232fc0a5b652af0805d`. The five Smoke cases also occur as passed cases in the public full EditMode package. A prior `-TestCategory Smoke` diagnostic matched zero tests and is excluded from the gates.
- The approved packager verified the public full EditMode package against the protected original: `coref-p12d-city-core-direct-restore-20261025.zip`, SHA-256 `eaf3c91ea4ef938bd777b34a7bda716db81028cbe20129c0d3092726a1869a0a`, 172,480 bytes. Public manifest SHA-256: `4e55c784b8cd2b93da896737fd7c054e1b090670504048a45d3120a0d78bf0db`. The archive contains 3,264 test cases, including five Smoke cases; all are passed. The public manifest contains no local workstation paths. Original reports and private runner manifests remain in the owner-only E: evidence store; this record does not publish raw logs or a CoreValidation TRX archive.
- The candidate passed `git diff --check` before commit. A Unity `Library` was present; no Library-independent claim is made.

The Unity input inventory includes pre-existing uncommitted ProjectSettings and user files in this worktree. Their recorded hashes were stable across the run; none were staged or committed. All Assets, Packages, tests, and Tools input hashes match the committed code candidate.

## Scope and limits

This proves the selected P12-D City image values flow from the Core decoder into the existing Core stager while preserving City schema identity and Unity projection compatibility. It does not complete full-image orchestration in Core, P12-D, P12-A, detached host integration, P12-G, or CORE-F. No canonical promotion or Phase closure is implied.