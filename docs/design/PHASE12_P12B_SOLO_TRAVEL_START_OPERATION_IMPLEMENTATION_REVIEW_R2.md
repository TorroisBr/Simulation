# Independent implementation review - P12-B solo travel-start operation, revision 2

**Result: PASS**

## Exact review target

- Canonical base: `codex/phase12/canonical` at `e76e50bfefc68b827d54ceb74e0b25293e0ad7b8`.
- Reviewed executable commit: `fe0e0be03403e92001173deae1fafe58dfe432d2`.
- Reviewed executable tree: `d72e84d6a442440ab82bacf6a0eb32165a9d7055`.
- Candidate evidence branch tip reviewed after its documentation-only correction: `5394a5e17d5bbe54895a7b374a1a4f6e7df20f58`; this documentation change does not alter the reviewed executable tree.
- Candidate record: `docs/design/PHASE12_P12B_SOLO_TRAVEL_START_OPERATION_CANDIDATE.md`.
- Technical design: `91728e4aee7717c6b002691a9a9e96c4ad22ac71`; source-backed clarification: `4b79a5d176dd73e2814dbfdf76b73c6117ec8d23`.
- Review performed independently by `/root/solo_travel_design_review` against the canonical base and the exact executable code/tree above.

## Findings

The source City is included in operation ownership only when `CurrentCity` reciprocally contains the NPC, matching the City mutation performed by `NpcRuntime.StartTravel`. The stale non-reciprocal City test confirms that the operation does not preflight or report that unrelated City section.

The rejection test executes the actual selected Travel path: TravelSystem commits the charge, a saturated travel-state revision makes `StartTravel` reject, and the existing compensation restores the balance within the same solo operation. The account local revision advances twice; the census mutation epoch advances once; travel state and event sequence remain uncommitted.

The scheduled-request test matches the existing API contract: a Travel action is rejected by the EscapePrison-only `ScheduledDirective` constructor. The implementation does not extend scheduled actions or add gameplay scope.

Both findings from the initial exact-tip implementation NEEDS_CHANGES review are resolved. No additional design or implementation mismatch was found.

## Validation evidence

The exact-tree test artifacts and SHA-256 values are enumerated in the candidate record's Corrected-tree validation table. Independent review confirmed the table values match the retained artifacts:

- `P12SoloTravelStartOperationTests`: 11/11.
- `EconomyTransactionTests`: 45/45.
- `GeneralizedSpatialTravelTests`: 18/18.
- `TravelScoutingTests`: 11/11.
- `SpatialKnowledgeCensusTests`: 16/16.
- `RuntimeIdAllocatorCensusTests`: 3/3.
- `SimulationRecordSequenceP12InvalidationTests`: 11/11.
- `SimulationBootstrapCompositionTests`: 21/21.
- `SimulationRuntimeAdmissionTests`: 31/31.
- `SimulationRuntimeOrchestrationTests`: 12/12.
- `P12TravelPartyAdvanceTests`: 10/10.
- `SimulationRuntimeLongRunTests`: 7/7.
- ALL EditMode: 2216/2216.
- Official Smoke: 5/5.
- `git diff --check`: PASS.

The evidence record also discloses the superseded early 10/11 attempt. Its sole failure was the stale-City test's success assertion because that attempt's fixture had removed the actor's `CurrentLocation`; after the fixture explicitly restores `currentLocation`, the corrected focused run passes 11/11. The failed XML SHA-256 is `60B1D1DEB70D365BCDE3099CD9C6BCF471F2168C481C652C41261E7B61CADC0E`; the corrected focused XML SHA-256 is `C224C81CC64116EDC3D053EF7D7D8EA4F0690A9FF68226CF786F0DB29DE47698`.

The two unrelated local ProjectSettings files were not included; their verified SHA-256 values remain `58BCBFB23DA7AAB5ACD696E7D83E9D75A86442ED39A582159D2493049361BA28` and `5C432C9C73FDF73FEE91C8823EA02AB98E3B7A8EDF341B5AE6901D699CCE52A7`. The unrelated untracked ArmedForce `.meta` files remain unstaged.

## Scope and status limits

This PASS is for the bounded selected-profile solo travel-start operation candidate only. It does not establish complete live owner or operation coverage, complete shared-epoch coverage, global quiescence, capture eligibility, export, hydration, P12-A readiness, P12-B completion, P13 readiness, or Phase 12 closure.

Canonical State was not changed. Canonical promotion remains a separate human gate.