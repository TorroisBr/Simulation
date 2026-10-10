# P12-G P8-C target-owner validation — 2026-10-10

This bounded restore-coordinator slice validates both selected Daily-v1 P8-C
target owners: `p8c.city-site-location-bindings` and `p8c.person-positions`.
For each row, the coordinator reads a fresh witness from the exact owner
installed on the staged target runtime and requires cardinality zero. It
compares the witness revision to the revision recorded in the target vector;
P8-C does not impose an independent zero-revision requirement.

The rejection matrix replaces each installed target owner with a distinct,
valid empty owner while the registered vector remains bound to the original
staged object. Both owners report zero cardinality, so identity comparison is
the discriminating check. The shared assertions verify rejection before
publication, preserve the source session/token/graph, compare later
continuation against a control runtime, and prove a subsequent valid restore.

This slice covers only the two P8-C target-owner witnesses. It does not cover
P8-D target owners, other remaining target owners, whole-graph failure
injection, no-replay, full continuation parity, or P12-G/P12-A/P13 readiness.

## Exact source baseline and validated code

- P12 canonical base: `c57fd22bb058e43672c01d8f5755d5d29eaa40b0`
- Code commit: `3d4f8b403dd93c8872699455c3494105f73096a9`
- Validated `Assets` tree: `c4d1b78a92e95f10d83726bcc5f98f791c68122d`
- Changed production file: `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.cs`
- Changed test file: `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs`

## Results

All runs completed with zero failures, skips, or inconclusive tests.

| Gate | Result | XML SHA-256 | Raw log SHA-256 | Compressed log SHA-256 |
|---|---:|---|---|---|
| `SimulationRuntimeAdmissionTests` | 119/119 PASS | `A956A1FF9B294E611B9541DAD2328316F813FD68037A1871B670EB06CC15AFBA` | `9A0410426EFEA4635AE7CCAFBC184C8A4973532BE51028B735BD45FF2E3DDB7C` | `28E0AC68F6B08C6CA2C9009CA9B122352C8FB1C071FD9C94CC51C134E64FD3DA` |
| ALL EditMode | 2793/2793 PASS | `64D25D60088B7284048710E58D9CC6E3FBC998D6ED637A7DBF7A82B429E1248E` | `54EC08626CB7B0CDE2775EDAFBD0C3E245F2FC7EFE4C6F02284A1AFB152DA2D9` | `7FCEDA2D40B2821F700044A2893454B40213F94B57607AE28AE9EF48FBFEA9BB` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 PASS | `DE88AD57D8C2A16779F1AFE0495E994DCF436B025316B08CD396F951C037FF90` | `52F4C09CEFBACE231C528F6B1ACF172B903CE0D42B8F8D3C913CA5D365E8160C` | `2DE059349CE19344C6AE979FC9FDA2DE1BCA1E11B6464131FFC374BA700DFEF3` |
| `git diff --check` | PASS | — | — | — |

The matching XML and compressed logs are retained in `Focused/`, `All/`, and
`Smoke/`.
