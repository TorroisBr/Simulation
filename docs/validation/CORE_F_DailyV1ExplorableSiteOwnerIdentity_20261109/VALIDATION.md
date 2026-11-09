# CORE-F selected Daily-v1 ExplorableSite owner identity

**Status:** validated incremental candidate evidence; not Core canonical and not CORE-F completion.

## Scope

The selected Daily-v1 Core owner graph now retains the exact empty `ExplorableSiteStore` created during genesis. The Unity projection reuses that same store instead of constructing a second authoritative instance. Restored P12 owner staging binds its staged empty site store into the candidate graph and verifies reference identity before the restored graph can be accepted. The graph's mutation guard includes the bound store.

This preserves the accepted Daily-v1 exact-empty site profile. It does not add P10 site content, widen the P12 profile, claim the complete 89-family census, or complete the shared runtime/application/host contract.

## Candidate and source identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Validation base HEAD: `6287680e3214d55f755e89c603ddeea45cef0357`
- Validation base tree: `751d1e2ed091cbd23f06e27f923a4e1e6f07ad32`
- Validation input manifests record stable source snapshots. SHA-256 values for the five production files match in both applicable official snapshots; the two `Simulation.Core.Tests` files match the CoreValidation snapshot. The Unity and Core manifests remain available with the preserved private originals and the public Unity result package below.
- The pre-existing `ProjectSettings/ProjectSettings.asset` change was present in the Unity snapshot and was not staged or modified by this checkpoint. Other private/untracked evidence and unrelated files were also excluded.

## Validation

### CoreValidation

- Official runner: PASS, cold 1,070/1,070 and warm 1,070/1,070.
- Run ID: `45087b312011460b8eb3603c94fe0d82`.
- Stable input digest: `fcf7b766e1a6114218821059877925b190e50dfd8ca7af8174a37ebfcf26964c`.
- Original TRX and logs remain in the protected E: evidence location. No raw TRX or private logs are committed; this candidate record does not claim a public .NET evidence package.

### Unity

- Unity Editor: 6000.3.9f1; Test Framework: 1.6.0.
- Focused `SimulationBootstrapCompositionTests`: 39/39 PASS.
- Full EditMode: 3,302/3,302 PASS; zero failed, skipped, or inconclusive.
- Official Smoke: 5/5 PASS.
- Full-suite run ID: `794b96bf97844c8da23eb9936865ee31`.
- Stable full-suite input digest: `b9afc96662ed3601949620d69203b37c3369a8c9d87f5a2cbc43f204153b84fe`.
- The result-only ZIP and sanitized manifest are the official Unity package in this directory. Its archive SHA-256 is `10641e96eea0463fd95353c65c9e6deef65232b5ba332a567607de96d8764808`; the official verifier reported `VALID: runs=1 entries=1`.
- Focused and Smoke originals remain in protected E: evidence; the full EditMode package contains the full regression result. No original XML or raw log is committed.

`git diff --check` passed on the implementation changes before packaging. It is rerun at candidate commit preparation. This checkpoint is not a canonical promotion; exact-tip independent review and subsequent CORE-F integration remain required.
