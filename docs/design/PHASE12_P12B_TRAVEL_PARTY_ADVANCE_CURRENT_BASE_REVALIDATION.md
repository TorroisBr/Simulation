# P12-B TravelParty Advance design — current-base revalidation

**Result:** `DESIGN_REVALIDATED_FOR_IMPLEMENTATION`; no design revision required.

| Identity | Value |
|---|---|
| Current exact Phase 12 canonical base | `6b30d86c3214a98603bea809154e2dc06047d6a3` |
| Prior design base | `0f36331d84ad3139171d36f980dfe6fa635ae30c` |
| Reviewed design tip | `27e4f80ca95adb3301cbb711af6415b34b246b64` |
| Prior independent design review | `27e4f80ca95adb3301cbb711af6415b34b246b64` (review record for exact design tip `c75c673be48b506c71730d12503b42bebc6f7f4c`) |
| FR-C canonical promotion | `9a6f78c43e7ad0a8f73366055151a7710e24a759` |
| Current Phase State promotion record | `6b30d86c3214a98603bea809154e2dc06047d6a3` |

## Revalidation findings

The FR-C promotion is the only source delta between the old design base and current canonical. Its only production runtime hunk registers `FactionFactualReader` in the factual reader set in `SimulationRuntime.cs`; it does not change the daily `runtime.advance-day` operation, the `AdvanceParties` call, operation admission, owner notification, or TravelParty sequencing. `TravelParty.cs`, `NpcRuntime.cs`, `CityRuntime.cs`, `ContinuationCensusProtocol.cs`, `TravelPartyCensusProvider.cs`, and `CityNpcPresenceCensusProvider.cs` are unchanged from the reviewed design base.

On the current base, the daily runtime still calls `TravelPartySystem.AdvanceParties()` inside the existing daily advance flow before legacy `TravelSystem` advancement. `AdvanceParties` retains its reviewed partial-commit order: it may clear start-day flags or decrement member travel days before a group arrives; on final arrival it updates location Knowledge, attempts event recording/sequence allocation, clears member party IDs, and completes the party. The existing provider set still supplies party, City-presence, per-NPC SpatialKnowledge, and sequence witnesses; it still lacks a per-NPC travel-progress revision/provider. The design's ordered owner mapping and partial-progress/error constraints remain applicable.

## Difference classification

| Classification | Finding |
|---|---|
| `UNCHANGED` | TravelParty and owner mutation semantics relied on by the reviewed design remain unchanged. |
| `BASE_DRIFT_ONLY` | FR-C adds the reviewed reader registration and promotion documentation. The runtime change is outside the TravelParty operation path; no TravelParty source was replaced. |
| `REAL_CONFLICT` | None. |
| `SEMANTIC_REVALIDATION_REQUIRED` | Current-base source paths and current P12 callback/reconciliation seams were re-opened before implementation. No assumption changed; implementation must still validate exact owners, mutation boundaries, roster reconciliation, revision saturation, and partial-progress reporting as specified by the design. |

The original independent design review remains valid as a review of the unchanged design. This record is a current-base source revalidation, not a new independent review and not an implementation validation. Implementation will be based directly on `6b30d86` and will retain the accepted design scope and tests.
