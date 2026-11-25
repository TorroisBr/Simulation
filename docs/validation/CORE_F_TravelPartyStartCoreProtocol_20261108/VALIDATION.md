# CORE-F P12 TravelParty Start Core Protocol Validation

Date: 2026-11-08
Status: PASS for this bounded candidate checkpoint

## Candidate identity

- Base commit: `a644be627ff866b3f81564abcb62e7c625afa2ec`
- Implementation commit: `fa0cedebdf46271cd3663f1694f51408aadd2e14`
- Implementation tree: `45f168ba888d0abf39e0e03aa9f7d1fd8e7673ad`
- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`

The candidate changes only the Core execution-boundary state, the selected Unity TravelParty start adapter, the SimulationRuntime bridge, and focused Core tests. The tests exercise the Core-owned serialized P12 protocol scope, exact mutation-section receipts, allocation receipts, and rejection of unreceipted travel-state drift. The selected path is used only for a fully selected roster with a selected origin City; mixed or unselected rosters retain the existing host-owned fallback.

## Validation results

| Gate | Run ID | Result |
|---|---|---:|
| CoreValidation cold | `b76d224b669c41819a1049c7ec692a69` | 1,059/1,059 PASS |
| CoreValidation warm | `b76d224b669c41819a1049c7ec692a69` | 1,059/1,059 PASS |
| P12 restore regression filter | `ecdabf9586744c32898bb98a135e913c` | 123/123 PASS |
| Unity EditMode | `5653ca37d4754f7cb670c6fb4ba2c01c` | 3,300/3,300 PASS |
| Official Smoke | `32ee2d79d4ae41fe89da9f130428ce69` | 5/5 PASS |
| `git diff --check` | final candidate source | PASS |

The CoreValidation manifest identifies base commit `a644be627ff866b3f81564abcb62e7c625afa2ec`, committed tree `325531250fcc343d40cf47ac3d9fabf1827c686b`, and working-input digest `6b3058bc93281a64b19fe9e3044d33b55f92aabc3119c53c2ad7a6d7b94f982c`. The Unity runner reported a stable before/after project-input digest of `ffd40edc0aa07738a68bdd2ed2f1147cb02997e28c034bf1c41cad38339c1635` for the P12 filter, full EditMode run, and Smoke run. The changed source and test file SHA-256 values below match the runner input manifests.

A user-owned `ProjectSettings/ProjectSettings.asset` modification was present during Unity validation and remains outside this candidate. It was not staged or altered. The private runner manifests preserve the complete input identities. CoreValidation recorded `libraryAbsent=False`; this checkpoint does not claim Library-independent validation.

## Changed source identity

| File | SHA-256 |
|---|---|
| `Assets/_Project/Scripts/P12TravelPartyStartOperation.cs` | `3ceb954a7a5b49466022d9f732c8d5f79a60613fbef01a635dc0fea64f4e0c92` |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `ce01c5734c215040581e67241dcff18cd3fd2f9b0274f9ae0fed8b071129df7f` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1ExecutionBoundaryState.cs` | `f8e38125f772dfc381bc4b309811f7f390cdc534583683dcca9e15cc300df861` |
| `tests/Simulation.Core.Tests/DailyV1SelectedSoloTravelStartOperationTests.cs` | `bd6bb9a537bbdd3cd5c7e22a9d0d5d7a9fe58e6f667280db7e2413ff912824a1` |

## Private evidence integrity

Original XML/TRX reports, runner manifests, and logs remain in the private validation store. This tracked record contains no workstation paths or private runner metadata; the hashes below identify the preserved artifacts.

| Evidence | Artifact SHA-256 |
|---|---|
| Core cold TRX | `ee8c995e63ab7e82339a955b7a88917e39d8e8fd1cdce757c395c4a0575e4baa` |
| Core warm TRX | `085e3ac0cd00bf206a22671ad668480d3fa32289702d44952ea4c009b47d7384` |
| Core manifest | `a7bda2a146fb03bba6a8954ef5526416d55f1b9c58844bf9fd22642c40a6bc97` |
| Core cold build log | `e868c3a674749ca50fe514b762d0501d2cb1cfdbb7ce26ba6f11432db303d0b6` |
| Core cold test log | `eeabc24169952150618d9c3e1f434a68cca21a967ac6305bbfb16d376ef81a44` |
| Core warm test log | `a9172a604b12f7b5a1da0b4ffda8efd578deb41a9199fd4535356d705553f9c8` |
| P12 restore XML | `048fdbf8e379f5aff81189459ad5f32d167589ef144ae3f219f22fa7b31953b1` |
| P12 restore manifest | `03a7372444af8e5aec7bccc5834487bd23c56972a9c771bf42d815db887d2bd6` |
| P12 restore log | `d591cc002df673d0035774c0224033fc2417d9a825eae44ab71074e034bac79b` |
| Full EditMode XML | `bc5c259a3b3c7b2983852c2c4661760bb9132dd879ef16f933fb3ea279a60936` |
| Full EditMode manifest | `b0da6d193e2a8a3308e1020cf69a59a21e0682ff9b2f561db3fe2b0c0b5ed12a` |
| Full EditMode log | `34455ae11ee22dd0d8e91afe2b6f9d8c1cc2a34196f6e68868513737a338f07b` |
| Smoke XML | `406ac71d240f1966e51cde908ed1d91dc419830280e80a14e2a681e4ac469832` |
| Smoke manifest | `87b2ed372239fed5e3c928d72aabce92d3cd745a42f703cec40e150031e2a787` |
| Smoke log | `91bed20782e3781030d386a480ec3e13f10aa864d8e13585b18d1f916f188a45` |

The earlier diagnostic Unity run with six restore failures preceded the final protocol fixes. The focused restore filter and full EditMode/Smoke runs listed above were executed afterward and passed; the earlier original evidence is retained privately.

## Scope limits

This is a validated bounded TravelParty-start integration checkpoint only. It does not complete CORE-F, does not move all 89 census families, does not establish the complete shared Daily-v1 application or detached host, and does not authorize Core canonical promotion.
