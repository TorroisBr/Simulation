# Selected P8 Census Witness Integrity — Validation Record

## Scope

This follow-up makes the Core advance boundary compare a bound P8-C owner’s complete live census witness with the corresponding supplied owner-section row: section identity, schema version, exact owner instance, cardinality, and revision. Duplicate rows for a bound section are rejected. Existing complete owner-vector, admission, post-callback state, and output-clearing behavior remain in force. A P8 owner not yet bound into the Core graph remains under the existing host census protocol during this staged migration.

The regression covers acceptance of the exact graph-bound vector, rejection of substituted identity, rejection of a false cardinality and stale revision for the legacy-anchor owner with census/epoch outputs cleared, and rejection of a partial vector. It does not claim migration of all P8 owners or the 89-family reader, shared epoch coverage, P12 capture readiness, export, or hydration.

## Validated source identities

Working sources were validated against candidate base `b1cf8ee121b3be049a7565bcf0a1e8212ef84fd5` with these SHA-256 identities:

| File | SHA-256 |
| --- | --- |
| `Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs` | `63a1f465cab42469eb2b3a118986d7f4dcaf99652e84ba8ea909b62da417983c` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedRuntimeOwnerGraph.cs` | `9f669ba2bd49f8ea6f8d446c312e35174d15f6ab0eb9341a1aaf6139eb971196` |
| `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs` | `9b98a376dfc74c25f222b3ba64de8ecd0a564312d0cf8ef6ccd8cd4440c7ae60` |

## Validation

| Gate | Run ID | Result |
| --- | --- | --- |
| Official CoreValidation, cold and warm | `30f7c2ba5ac643f092439163a0276da8` | **918/918 PASS** |
| Full Unity EditMode | `ffebe25f7ca748fb8ddd7f16512fd67f` | **3275/3275 PASS**; zero failed, skipped, or inconclusive |
| Official Smoke filter | `b22ad1e794fd4e3aa74be968d3476d4c` | **5/5 PASS**; zero failed, skipped, or inconclusive |
| `git diff --check` | — | **PASS** |

All three official run manifests report stable inputs; the Core manifest reports 918 discovered tests. The Core manifest’s three source hashes match the table above. Unity manifests include both changed production files and their hashes match; the standalone .NET test source is not part of Unity’s compile inventory and is covered by CoreValidation.

Original XML/logs and private evidence remain in owner-only E: retention. No raw report, workstation path, or credential is included here. No executable or test source changed after these runs.

## Review status

Independent exact-tip implementation review is pending for the candidate commit containing these changes. This record is not Core canonical promotion, P12 readiness, or CORE-F completion.
