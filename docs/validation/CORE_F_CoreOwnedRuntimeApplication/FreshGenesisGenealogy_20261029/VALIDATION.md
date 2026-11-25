# Fresh Selected-Genesis Genealogy Owner Validation

## Scope and source identity

This bounded follow-on makes selected P9 Daily-v1 genesis create and bind one empty `GenealogyStore` in the Core owner graph. The Unity bootstrap passes that exact Core owner to `SimulationRuntime`; the runtime adopts it only when the admitted Core graph already contains it. The legacy construction/clone path remains the fallback when no Core Genealogy owner is bound. No P12 rows or profile semantics changed.

- Base candidate commit: `ee8c7bd41f8a44bdd97ffc68614c4ea2b6f115f1`
- Base tree: `66a30a593a0703a291b6bb0f4b584c7ebebbe271`
- Changed-file set digest (path, byte length, SHA-256 records): `234c15aba279049402bd742e8b5d55718b21778f0b9c654ed4d45f552a5b6a7e`
- CoreValidation input digest: `4fcb7ad57df3729d57d37622e15cfa3ef81c11f139f5e93b128dcf1d14cb9acc`
- Unity full-project input digest: `543a609c979e0137474833ce5f78673b93d8b99720e4d78b7087f56855044fa3`
- All official runners reported stable before/after input snapshots.

| Changed implementation/test file | Bytes | SHA-256 |
| --- | ---: | --- |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | 715973 | `c5f9ebb3d296227e1c002b32a3f6055bb45816aeba469ab1685ea6526a36a152` |
| `Assets/_Project/Scripts/TesteSimulacao.cs` | 206230 | `7b5f7a7d081149fc56f24af4f8712fc6203485ac2173d18fa820c8d9b162fe42` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs` | 335005 | `ca0eeeda55a6808f483e79096bad898de3e9c343361deb229e23b86da57263b7` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1GenesisComposer.cs` | 23892 | `fc96cf01044347c7adaab6f90527371a2f4adeddd898ab9cd39a0c1987d2c1cc` |
| `tests/Simulation.Core.Tests/DailyV1GenesisComposerTests.cs` | 29443 | `aeb0b57c85b8d53adbfffdee212affa17200dc83266ee814cf2e383d812f93d5` |

## CoreValidation

Official CoreValidation cold and warm suites both passed 933/933, with zero failures, errors, timeouts, inconclusive, or unexecuted cases.

- Runner: `5c222d9598a14c58b1601c448710220b`
- Cold test run: `2e49e2c2-a1f3-467e-95a3-7e4e9e2fa4d8`
- Warm test run: `feca41f6-907f-4eb4-af61-b868cc8af8b9`
- Protected runner manifest SHA-256: `B5B6F52BE4375ED2D5CD78D88AAEC507326B1DA80B95FDF5D61FE7EFD4F139AA`
- Cold TRX SHA-256: `f8abc0007668509e4f08095a3477d069ce5141ac3960fbb6e82fde7209bc38ff`
- Warm TRX SHA-256: `9204d8d1a2c751f99fa1ca18f03fc2c027fde76e2ed01ac7e88c3350c90e4c06`
- Cold test log SHA-256: `2a1673b8506498440869f28a4771f52fce4caada926a4e50dc2c25c32b609eea`
- Warm test log SHA-256: `cd9431e063e13fe73aa619e472f07b99dce110d9c6e4c1a18b0f1d79a677c3f2`

Raw TRX and logs remain in protected private storage. The Core runner used the existing Unity Library; this does not establish Library-independent .NET validation.

## Unity validation

The focused selected-genesis identity regression passed 1/1. Full EditMode passed 3279/3279; the separate official Smoke filter passed 5/5. All counts were coherent and all before/after source snapshots were stable.

- Focused run: `3a6d5bef16bf40f1abe83ab40f0d174b`
- Focused protected manifest SHA-256: `311AC122B69C5F93A17C4345ECEC45B54F26F348E5B529AAFCB5A3D0794B1309`
- Focused XML SHA-256: `968553ca6f21fba52f65af809be67baa8904d4ac65408c82bac8d2699217184c`
- Focused raw log SHA-256: `52fc67b31991ec693cae8f73071b8662f86a9bcc07e5a486452044b79703c068`
- Full EditMode run: `c20d8f38b65a4746aae5b31cb6ac07ad`
- Full protected manifest SHA-256: `BFC15449EB40D815637ED7CF9009B888C70E8C2B5FDD51F8EDFA663A8EE15147`
- Full original XML SHA-256: `ec0ff2244e54dfafd6cca38c083d8f56eb6f844430b24b41bf210bd8acc92918`
- Full raw log SHA-256: `bc38be04d4f402c89487407cf1cd1b4f9c0c42d7eacc2055c528cca407d4e2c8`
- Smoke run: `2526f58300f94c14b8975f7ffb7b7ec6`
- Smoke protected manifest SHA-256: `BB510DE7D55335CB9A5F8A4612C032CFDE4105F471E91E87F03E64E6EC3FA019`
- Smoke original XML SHA-256: `5d36f476cf50af5a93562c6d294008ae009c10ea11ad4b2523e1a8b8e65a9358`
- Smoke raw log SHA-256: `54ed010fd3c8147adca6b4e13189f841044f1b3b7b8d34b36b928f321b0e2c03`

The privacy-safe full EditMode package is `coref-genesis-genealogy-editmode-full.zip` with manifest `coref-genesis-genealogy-editmode-full.manifest.json`. Package verification passed (`runs=1`, `entries=1`):

- ZIP SHA-256: `73d0fff25c8afceae635ff055e052b3c28707fc62aa3a1d73c9d42c061ae808e`
- Public manifest SHA-256: `1f09bf5ff2ec02e66e42fb428b0994f2c2cad910642b7fe69b9deade10fd0802`

Protected originals were preserved and are not included in the public bundle. The public manifest and ZIP contain no local workstation paths.

## Integrity and limitations

- `git diff --check` passed.
- No new production public API, P12 row, or profile capability was introduced.
- The three pre-existing ProjectSettings changes and unrelated untracked/private files remain unstaged.
- This is a recoverable candidate checkpoint, not CORE-F completion or canonical promotion.
