# FR-C current-base exact-tip review

**Result:** `PASS`

| Identity | Value |
|---|---|
| Reviewed FR-C code commit | `ec042b30b1c0a390f611c47cb22b75631cfb9556` |
| Reviewed FR-C tree | `fabb182e79bf7c36035b791736edcb42785056ac` |
| Exact Phase 12 base | `0f36331d84ad3139171d36f980dfe6fa635ae30c` |
| Promoted FR-B integration | `28d33a2c8f10ddb724819893a0b5f2804a6f5b0b` |
| Architecture authority | `451340c56e9b676bf6ea43412bcb856b9ccde3de` |
| Diagnostic revision acceptance | `775f3c9c7f11dc3312dd4dbf7ccac424d67f2029` |

## Review result

The reviewer found no blocking code issue at the exact candidate tip against the exact current canonical base. Faction capture remains behind FR-B admission and preserves the existing owner-thread, bound-profile, logical-boundary, publication-health, and exact Faction/Person revision checks. The reader is registered after the existing P12 sequence initialization and does not bypass or replace those paths.

The registered `simulation.faction-truth/v1` capability returns `Unavailable` when admission cannot begin in an unbound or unsupported profile. Unknown capabilities remain `Unsupported` during an admitted capture. Tests cover both outcomes.

The reader returns copied immutable Faction and active-affiliation facts in deterministic order, validates active Person endpoints and Faction endpoint/join chronology for active and ended rows, filters ended affiliations from current facts, and retains no Store reference in returned facts. No Knowledge, support, presentation, or World Exchange data is read or projected.

## Current-base difference classification

| Classification | Finding |
|---|---|
| `UNCHANGED` | FR-C fact semantics and diagnostic behavior match the accepted design. `FactualReadContracts.cs`, `FactualReadCoordinator.cs`, and `FactualReadFoundationTests.cs` had no intervening canonical edits relative to the prior FR-C base. |
| `BASE_DRIFT_ONLY` | Current canonical P12 sequence, census, runtime, and test changes were retained. No FR-C patch discarded or replaced those changes. |
| `REAL_CONFLICT` | None. |
| `SEMANTIC_REVALIDATION_REQUIRED` | The narrow `SimulationRuntime` reader registration and actual-capability bootstrap expectations changed composition behavior; current-base focused, bootstrap, full EditMode, and Smoke gates were run separately and passed. |

## Review scope

This was a read-only exact-tip code review. The reviewer ran no tests; validation evidence is recorded in `FRC_FACTUAL_READER_CANDIDATE.md`. `FactionStore.cs`, `PersonStore.cs`, `FactualReadAdmission.cs`, and `SimulationBootstrapComposition.cs` were not modified. The runtime integration is limited to registering `FactionFactualReader` in `SimulationRuntime.cs`; bootstrap tests now exercise the registered capability.
