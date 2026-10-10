# P12-G P8-D target-owner validation — 2026-10-10

This bounded restore-coordinator slice validates both selected Daily-v1 P8-D
target owners: `p8d.spatial-route-observations` and
`p8d.person-route-plan-history`. For each row, the coordinator obtains a
fresh witness from the exact staged runtime owner and requires the contract's
exact owner identity, zero cardinality, and zero local revision.

The rejection matrix replaces each target `SpatialRouteKnowledgeStore` and
`PersonRoutePlanStore` in turn with a distinct, valid empty store while the
registered target vector remains bound to the original staged instance. Each
replacement is asserted empty at revision zero, and restore must reject the
identity mismatch before publication. Shared assertions preserve the source
session/token/graph, compare subsequent continuation against a control
runtime, and prove a subsequent valid restore.

The slice covers only these two P8-D target-owner checks. It does not change
P8-D source-owner semantics or add route data, and it does not complete the
remaining same-attempt target census, whole-graph failure injection,
no-replay, full continuation parity, or P12-G/P12-A/P13 readiness.

## Exact source baseline and validated code

- P12 canonical base: `1f34fc1458e29569888c6e0f764012eddb7ecd88`
- Code commit: `b95740629b6b7b6c5ef4a2f5076e6352ae09f091`
- Code commit tree: `7971311c5adc409c772c99eea8ed71099e8f30ac`
- Validated `Assets` tree: `fd06e830670acfff5477420da4fe5cfa98c3d2aa`
- Changed production file: `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.cs`
- Changed test file: `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs`

## Results

All runs completed with zero failures, skips, or inconclusive tests.

| Gate | Result | XML SHA-256 | Raw log SHA-256 | Compressed log SHA-256 |
|---|---:|---|---|---|
| `SimulationRuntimeAdmissionTests` | 121/121 PASS | `8601940F94822F7ADD6A0C874DF0BD09D4EDAEC558F17BF854B84D0C0A29D42B` | `C3B22CB362AB26978C53063AFF85F2A7141DFCF451F227BDFAC5229AF822C567` | `649F78F6F1A81D8652DC6EEFEA1936619CA749BC9356C1E0A9221860D7299076` |
| ALL EditMode | 2795/2795 PASS | `286251F6197498BDC92206712E8DA7A19E017F0C87B3A6A1F2C99226902C6229` | `A48159362FD88874AD56586AA6B08DA2937C36C0B78B421E4123160EAF46B230` | `8CBE0DC647CF3498D5868F8B6A104415D4ADC7ABEC5559D859215B013B3CD5A7` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 PASS | `44D47449C8E658CF5426B669828CEA1DF100A6148E8BB3815492D98FBC0D76CA` | `3EF91D1659E33786AD5CAD6A23D3A77E1338118C18CF737AC7911A785E92EF25` | `4AC3345483FA31D32C8AFCC06DDE66628C7E00C1DF4D28A1AE62B0EE228AC7C2` |
| `git diff --check` | PASS | — | — | — |

The matching XML and compressed logs are retained in `Focused/`, `All/`, and
`Smoke/`.
