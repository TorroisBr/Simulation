# Selected P8 Census Owner Identity Binding — Validation Record

## Scope

This bounded candidate makes the Core runtime advance boundary compare census witnesses against the exact P8 owner instances already bound in its selected runtime owner graph: `PersonSpatialPositionStore` and `LegacySpatialAnchorBindingStore`. A duplicate relevant section or a witness carrying a different owner instance is rejected as incomplete owner coverage, and the boundary clears the census and mutation-epoch outputs. If either P8 owner is not yet bound into the graph, that owner remains the host census protocol's responsibility during this staged migration.

The change does not claim that all P8 owners have migrated into Core, does not replace the host's live census reader, and does not add P8 writers, shared-epoch coverage, capture eligibility, export, hydration, or P12 readiness. The existing complete selected-profile owner-vector checks remain in force.

## Validated source identities

The three changed implementation/test files were validated against candidate base `b2bbbea357aa01ae474203857f44951333a02499` with these SHA-256 identities:

| File | SHA-256 |
| --- | --- |
| `Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs` | `b67282b049f2e6f6d257d062e2f06e730b9a8440ba5dfbfd32757af239b4e2ba` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedRuntimeOwnerGraph.cs` | `b19d780110e11d25b840575de6c3f2724c076bb993e6277bfe5aacdcf9dff4b1` |
| `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs` | `6c0c26924db645c6602b6cd4302d548402902ff8d90fe3a1313d8d3c026bb9be` |

The new regression binds the P8 position and legacy-anchor owners into a selected runtime graph, accepts a complete census carrying those exact owner identities, rejects a substituted anchor identity with cleared outputs, and continues to reject an incomplete owner vector. It also preserves fixture behavior for graphs where those P8 stores remain unbound.

## Validation

| Gate | Run ID | Result |
| --- | --- | --- |
| Official CoreValidation, cold and warm | `d3f67c55b8f54b1ab50a6feceb39f46a` | **918/918 PASS** |
| Full Unity EditMode | `3072ea3f69d0449f9c74d1516d0d2877` | **3275/3275 PASS**; zero failed, skipped, or inconclusive |
| Official Smoke filter | `7121faef16444e01b0d28cff468febd3` | **5/5 PASS**; zero failed, skipped, or inconclusive |
| `git diff --check` | — | **PASS** |

CoreValidation's saved official manifest reports stable inputs and 918 discovered/passing tests. Its three source hashes match the table above. The Unity and Smoke runs completed successfully on the same worktree contents; their runner records report stable source inputs, and the changed production-source hashes match the validated candidate. A wrapper-summary parsing error occurred after the Unity runner had completed; it did not change the runner result or its saved evidence.

Original reports and private validation evidence remain in owner-only storage. This record does not include raw report data, local filesystem paths, or credentials. No executable or test source changed after these runs.

## Review status

Independent exact-tip implementation review is pending for the candidate commit containing these validated changes. This record is not canonical promotion or CORE-F completion.
