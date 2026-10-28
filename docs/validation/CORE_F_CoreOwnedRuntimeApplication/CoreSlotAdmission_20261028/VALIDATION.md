# Core-owned initial continuation-slot admission — validation

Date: 2026-10-28
Candidate base: `45875a1d721418606769f64ad85db3593984fc0f`
Scope: when the Unity host cannot provide a truthful P12 compatibility identity, the Core application inspects the initial slot; only `NoSave` permits the caller to proceed to ordinary Genesis validation. A present slot or storage failure remains fail-closed. The regular identified-host load path is unchanged.

The four changed production/test files below were byte-identical to the files in the official CoreValidation and Unity validation inputs. No executable source changed after these runs. Protected ProjectSettings edits and unrelated local files were not part of the candidate change.

| File | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` | `3add1d42a35efbb65c9f30ea45fb698e9f6e7c1eefc878744b85dd58d6274cfd` |
| `tests/Simulation.Core.Tests/DailyV1ApplicationHostTests.cs` | `e19c3f2d8abe7ad5ac36f20ffd4b7887d7f7c57be3897ff438a6e699d6d69525` |
| `Assets/_Project/Scripts/TesteSimulacao.cs` | `97d456c2fa5cbe5f64eb13498a986a601a764e6153f962e930d06eeb4bd24dec` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` | `dd2ff0039388fbc3f0bd01d6aba52cb93a3370fa63dbf41370ed48c7015140db` |

## Official CoreValidation

- Runner: `simulation-core-validation/v1`, run `3ce0ae58d89d4aa99a48115406c3351b`, status `Passed`.
- Cold: 904/904 passed; warm: 904/904 passed.
- Captured inputs stable; generated dependencies stable.
- Original cold/warm TRX SHA-256: `4d6da2a021241605162bddbd34148c89d04c5dc4bb65166fe241c2d401625bb9` and `5a381abd0d4ba1e4e5a0fc9e9d51be934a32bebf63a1241618eb6f339687e351`.
- Runner manifest SHA-256: `ba622b0112cb659eb046ed871aa6d784a680cb8fa9f2c55b886a0e0535f71ad7`.
- The official runner emitted its private CoreValidation schema, not the separate `dotnet-trx-source-context-v1` input required by the approved public TRX packager. No public .NET TRX package or normalized projection is claimed. The original reports and manifest remain unchanged in the owner-only evidence store.

## Official Unity validation

- Affected regression filter (P10-B generated Ruin Genesis, P12-G Political Binding Restore, and runtime admission): 253/253 passed, stable inputs.
- Full EditMode: 3275/3275 passed, stable inputs, no failed, skipped, or inconclusive cases.
- Standalone official Smoke filter: 5/5 passed, stable inputs. The full EditMode report also contains these cases.
- `git diff --check`: passed.
- The privacy-safe full EditMode package is included here and passed the official package verifier against its original private run manifest. The standalone Smoke XML was retained privately; it was not combined with the full report because the filtered XML has parent-suite counters that do not match the full-report structure.

Package identities:

- `unity-editmode.manifest.json` SHA-256: `303602d5fde8776f9772ec4cda091e8e122d5b6110bea405b620a87ba3b32f8a`.
- `unity-editmode.zip` SHA-256: `e89b903ac65ffc394b0102a4a10e44989a9e6a2edc5774eebb7fdd6360f3fc67`.

The exact source report run identities are retained with the private original evidence; this record does not replace or reinterpret those originals.
