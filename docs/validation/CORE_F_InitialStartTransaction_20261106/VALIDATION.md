# CORE-F Initial Start Transaction Validation

Date: 2026-11-06

## Candidate

- Base: `1d44f47fdc0eae5f960eb688764d568e889c9e2e`.
- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`.
- Scope: Core performs selected initial admission/preparation, invokes the host projection factory, and commits the exact projected session; Unity provides the host projection and P12 restore adapter.
- This is an unpromoted CORE-F integration checkpoint. It does not close CORE-F or claim the detached .NET host is complete.

## Validation

- CoreValidation, cold: 1,025/1,025 passed; zero failed/error/aborted/inconclusive/not-executed.
- CoreValidation, warm: 1,025/1,025 passed; zero failed/error/aborted/inconclusive/not-executed.
- Focused NoSave Core-application start: 1/1 passed.
- Focused P12 detached restore and continuation: 1/1 passed.
- Unity EditMode: 3,300/3,300 passed; zero failed, skipped, or inconclusive.
- Official Smoke filter: 5/5 passed; zero failed, skipped, or inconclusive.
- `git diff --check`: PASS.
- Unity: 6000.3.9f1; Test Framework 1.6.0.
- The .NET CoreValidation runner used `-SkipLab`; its manifest and cold/warm TRX were retained in private local evidence.
- Unity ran through the repository's official result-authority runner. The required evidence wrapper/packager is pinned to PowerShell Core 7.6.5; this environment has 7.6.9. No wrapper manifest or normalized public Unity evidence package is claimed. Original Unity XML and logs are preserved in the verified owner-only local evidence directory.
- The Unity runs used this worktree while unrelated user-owned ProjectSettings changes and unrelated untracked files were present. They were left untouched and excluded from this candidate. Since the pinned wrapper could not produce its before/after input inventory, this record does not claim a complete promotion evidence package.

## Executable source identities

SHA-256 of the exact source/test bytes used for the runs:

| File | SHA-256 |
|---|---|
| `Assets/_Project/Scripts/TesteSimulacao.cs` | `CEBD0A98AD7DB8A03E0EB311E01EB2AF6C873FD4E41A4D889B173641DBA914CA` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` | `1747E7D95FDD20EAA8C6E8009FDB0001E748F845BA12B54915235B71DFCB9919` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` | `36C7A99AB694D6FD63E9A3EB22F47C7BDF6EA6B5DF233CEE8A45BFF9AB6ABF9D` |
| `tests/Simulation.Core.Tests/DailyV1ApplicationHostTests.cs` | `EA754D1E1BBFAD186FD1431D1A6187480EDDD91F07367C3177E1E75E49BF0BBE` |

## Preserved raw artifact identities

Original files remain in protected local retention and were not rewritten. Names, lengths, and hashes identify them without recording their local filesystem paths.

| Result | File | Bytes | SHA-256 |
|---|---|---:|---|
| Core cold | `CoreTests-cold.trx` | 1,463,917 | `C03473E29A3BF0A90EB82A494BFC9BF7BA530DB3116E340AEDB1780E958F2496` |
| Core warm | `CoreTests-warm.trx` | 1,463,917 | `FBDB297AA274731F7773F82B3672DE96332CBFB6D9E00132546384578BDFBAB6` |
| Core runner manifest | `manifest.json` | 1,584,965 | `7C022F7B3DAC029137BF54217FF5CC7FFE61DAA87E1F05653628096F49BA62E8` |
| Focused NoSave XML | `EditMode-20261106-140248-24a7639294504fa392d5b01c331a200a.xml` | 2,710 | `A5D74045CAC1F171D8E700CBD19A606EE9A7E33E7CD0C31D96D9BAA3EB67B67C` |
| Focused P12 restore XML | `EditMode-20261106-140323-d0dd656474a14dd0a30130d5a944a132.xml` | 6,286 | `C41227A322483D354CFFF5A578B468D35BFA948BD9132439E853C2D320E3329E` |
| Focused NoSave raw log (private) | `EditMode-20261106-140248-24a7639294504fa392d5b01c331a200a.log` | 52,334 | `9A9FA7B78F5C6EE2FDBBBEF99B2506D7E82AB6E85B7105CDD53287CB40017470` |
| Focused P12 restore raw log (private) | `EditMode-20261106-140323-d0dd656474a14dd0a30130d5a944a132.log` | 1,195,977 | `4689A0B678C6518BA4B0D5EBE5D82961B7E3C75A1E0FCA5DA4D4505B8DD31E07` |
| Full EditMode XML | `EditMode-20261106-140402-c9156ae5f16844dba8ad9687ce7e0462.xml` | 3,290,803 | `F1D3C6823014C551EA45DA344B8953742483A53E7A0E3AFD64FA5A11BC9732A3` |
| Smoke XML | `EditMode-20261106-140635-083e3a453cc9472692053b2f33d5ea88.xml` | 6,358 | `00276EDA87DF0A0617FEA4E4F5E17F66B7D79CB385DDC7974263A79D97388738` |
| Smoke raw log (private) | `EditMode-20261106-140635-083e3a453cc9472692053b2f33d5ea88.log` | 51,760 | `A02BEB2ED90A2D7CF80DDA9C262F0D1C7778380A4DFA3E4092DD21563782E281` |
| Full EditMode raw log (private) | `EditMode-20261106-140402-c9156ae5f16844dba8ad9687ce7e0462.log` | 249,340,822 | `B6D3C909FE4E04EDF1E2FE668514E65B874156D87C545B611DDE5C1E3083337B` |

All original Unity XML and log files listed above remain in protected local retention and were not rewritten. No private log content is published here.

## Result boundary

The runs establish successful behavior for this exact source/test state. The result is a recoverable, independently reviewable implementation checkpoint, not a portable validation package and not authorization to promote CORE-F canonical.