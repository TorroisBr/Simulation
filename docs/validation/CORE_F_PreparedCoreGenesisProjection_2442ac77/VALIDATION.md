# CORE-F Prepared Core Genesis Host Projection

This candidate changes the Unity selected-start adapter to build its private host projection directly from the exact `DailyV1GenesisComposition` already prepared by Core. It retains the selected Unity runtime adapters and existing P12 bootstrap publication/census checks. It does not claim that the daily runtime has moved out of Unity or that CORE-F is complete.

## Exact source

- Candidate commit: `2442ac778f221ca06299111f041bb5cb982a4b3c`
- Candidate tree: `bcd8b4a69ccbe7a09c10df3fed9985f660f042a1`
- Unity input digest: `64cc2f3b1ed4caf17d9681c639110494b6583530d5822b464b83d3f6abe7bf1e`
- The source and test SHA-256 values in the official input inventory match the committed files:
  - `Assets/_Project/Scripts/TesteSimulacao.cs`: `6c42395d8138310b84fcb656dd0ea14ea27652dc37125c72b498634c8d27d354`
  - `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs`: `c32a2f118defeaa2fc2ac0930a7c78e199f1fb482dba72783fe28ea909e39d67`

## Unity validation

All three official runs used Unity `6000.3.9f1`, reported `inputStable=true`, and recorded the same input digest above.

| Gate | Run | Result |
|---|---|---:|
| Focused selected NoSave startup regression | `aaa983b7fe4545b5b26a74d2f985cc9c` | 1/1 passed |
| Full EditMode | `b83286caf2344751a7fa7d0f76b75876` | 3,321/3,321 passed; zero failed, skipped, or inconclusive |
| Official Smoke filter (`EditMode`, `Smoke`) | `66333e5ce786415ab9fd295c10eeec7f` | 5/5 passed |

The full EditMode report contains all five passing Smoke cases. The filtered Smoke report remains in protected original evidence; no separate archive is claimed for it. An attempted PlayMode filter selected zero tests and is excluded from all gates. Initial Unity runs made one-time local `ProjectSettings` writes in the isolated validation checkout; they were excluded. The final focused, full, and Smoke runs were captured after those writes settled, with stable matching input inventories. The normalized Git blobs for those settings remain identical to the candidate commit; no ProjectSettings file is part of this candidate.

## Public full-suite package

- [Manifest](Unity/coref-prepared-genesis-2442-final-editmode.manifest.json), SHA-256 `a64df2f8fdbf4cf15e7025353777eab05ec59d3a78e4e6a8effb4d9c957a35ac`
- [Archive](Unity/coref-prepared-genesis-2442-final-editmode.zip), SHA-256 `7debbbbed74d829728228a003f71037070439f25cd66b4f3b9faeb1b635eff27`
- The approved packager verifier passed against the protected original full-run manifest (`runs=1`, `entries=1`). Raw Unity logs and original reports remain in protected local evidence and are not included in the archive.

The earlier stable full-suite package created before the final same-digest rerun is not the package referenced above. The final package and all three gates identify the same Unity input snapshot.
