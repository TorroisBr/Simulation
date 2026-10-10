# P12-G restored target-owner identity evidence — 2026-10-10

## Candidate and exact source

- P12 canonical base: `39474ce14ba30e643a453061d7a6eda26d15021e`.
- Code candidate: `7c3290f73a63e9a2fe1f08631fc9de1a98cf7fa6`.
- Candidate Git tree: `f58c6fab4a11b703b54d41e895db43896e3b1834`.
- Candidate `Assets` tree: `7f47a3fdea897ac608d52f1995d498854afbedeb`.
- Changed executable/test paths are limited to:
  - `Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs` (Git blob `ad8aa693003e012f99004430c58f53d19a88959f`).
  - `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` (Git blob `8630facc029bd2094604490a8c7c990f657d3d15`).
- Production `Assets/_Project/Scripts` is unchanged. No owner, profile, operation, gameplay behavior, or persistence contract changed.
- Unity Editor: `6000.3.9f1`; validation used `Tools/UnityValidation/Invoke-UnityValidation.ps1`.

## Evidence added

The existing successful Daily-v1 restore/continuation test now builds the expected
section-to-owner map from the reconstructed runtime and composition, then checks
that every target vector ID occurs exactly once and points by reference to its
exact C/D/E/F-produced target authority. Existing source/target vector comparison
checks matching schema, role, cardinality and local revision, fresh target owner
instances, and the alias pattern. The test deliberately derives its count from
the exact successful source boundary: this fixture registers a Person for P11
history, so its completed-boundary vector has 300 rows while the day-zero live
inventory fixture has 299. This is a temporal/dynamic cardinality difference,
not a changed profile contract.

An integrated corruption case replaces only the private candidate composition's
`ExpeditionStore` with a distinct empty store while the candidate runtime's
registered Expedition census still identifies its installed store. The restore
coordinator rejects the mismatch as `TargetOwnerVectorFailed` before publication.
The shared failure-atomicity harness verifies the old active session, token,
health and graph remain intact, compares the next normal advance to an
uninterrupted control, and proves a later valid restore retry succeeds.

This is a test-only P12-G increment. It does not complete the §6 section and
compatibility rejection matrix, all restore-boundary failure injection, causal
no-replay or every multi-boundary case. It establishes no P12-G completion,
P12-A readiness, P13 readiness, general serialization, or Phase 12 closure.

## Final exact-tree validation

Every final XML reports `Passed` with zero failed, skipped or inconclusive tests.
Each compressed Unity log was decompressed and its SHA-256 matched the recorded
raw-log hash before the raw copy was removed.

| Gate | Result | XML | XML SHA-256 | Raw log SHA-256 | Compressed log SHA-256 |
|---|---:|---|---|---|---|
| Focused `SimulationRuntimeAdmissionTests` | 122/122 | `EditMode-20261010-174529-8ed86dbb511142c1ae4a7093ad2eabea.xml` | `9CB2491446B27B2995090850CE91AD7DD6CD868EE664F36DCCB9D57E145C8F75` | `296B30F7B215AFD7657ADEAA2B936EBA9103DB225F657D280548913D52BE7DB6` | `13BE394A18D061804281A5F5FDC892184143EE0C00BEDB4FD8E55634A9C20D7E` |
| ALL EditMode | 2796/2796 | `EditMode-20261010-174553-1c9b9a3593f1451cb494ac90b89f296a.xml` | `E43CA80DA5F99199D52B22796EA5109C244E0C3AD09DE7F94C278CB189DB5396` | `0D89B9C3E09BEB6DB9FBAF7D71A9CF419C1D058D29C3F5670AAB405F1B2173C2` | `0C6DD145469D70E91B71E5B56B1F3EAD0F8E430A3D53478B7262E6B3CE1530A1` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 | `EditMode-20261010-174635-7d1439d8b0874f37bed3077aec1ad9df.xml` | `8769E32C1763F68A846E041F1142AA833FB53736002896D34DB2D9AC1E5D1F69` | `21F9301843192562AFDE451C9C817294D4B64BB0CE5806C21DB5985865F02700` | `55F8FF93A49AB4E9048D9FA691B70345C5444547AFC8A6A0492A8AF4009E2FDA` |
| `git diff --check` | PASS | `39474ce..7c3290f` | — | — | — |

### Superseded diagnostic

| Attempt | Result | XML SHA-256 | Raw log SHA-256 | Compressed log SHA-256 |
|---|---:|---|---|---|
| Initial target-map assertion with stale day-zero count | 121/122; one assertion failed | `D009697C8EDF74209950BEF90E61D8B4437279F74981473C3DCB70F92C3EEE3D` | `281FCEF7588CF9596FCFA000A4FB6DABE4332B70CA1FC632EA1D42140D262DF5` | `78043AF59684F62FB8E4D7034AB1EF5ED866EFC467AD379C6C200A167EB8FCD9` |

The runner's first launch returned `NoResultXml` before producing a result or log;
that attempt is not counted. A corrected final-test assertion was also required:
the first mapping run used the day-zero constant 299 and exposed 300 target rows
after the test's Person registration. Its failed XML/log are retained alongside
this manifest as a diagnostic; the assertion now compares against the exact
source boundary vector, and the final focused, full-suite, and Smoke runs above
all pass on the final code tree.

## Status boundary

P12-G remains `WAIT_DEPENDENCY`; P12-A remains `WAIT_DEPENDENCY`; P13 remains
`BLOCKED`; Phase 12 remains `OPEN`. No unrelated `ProjectSettings` edits,
untracked `.meta` files, or earlier raw XML evidence were staged for this
candidate.
