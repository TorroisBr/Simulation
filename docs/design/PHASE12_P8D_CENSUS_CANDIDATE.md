# P12-B P8-D passive route-owner census candidate

**Status:** Promoted to `codex/phase12/canonical` as a P12-B partial
foundation; P12-B remains incomplete and P12-A remains `WAIT_DEPENDENCY`.

**Branch:** `codex/phase12/P12BP8DZeroWitness`.

**Canonical base:** `codex/phase12/canonical` at
`c7ff9fd9f4245c3f8968b9196a4c20de19dd0fb2`.

**Code-bearing candidate tip:** `f0575ef43a77898aae8fb8565d4b709b850a46d8`.

**Reviewed candidate tip:** `d92fdfb6b5ceb517c210be7cea5faab52ebb5641`;
independent exact-tip implementation review passed. Durable review evidence
is branch `codex/phase12/P12BP8DReviewRecord` at
`934741142cd74e06d2c284af45a62a068fa3d9de`.

**Promotion:** User-approved fast-forward from canonical
`c7ff9fd9f4245c3f8968b9196a4c20de19dd0fb2` to candidate tip
`d92fdfb6b5ceb517c210be7cea5faab52ebb5641`.

**Technical design:** `codex/phase12/P12BP8DZeroWitnessDesign` at
`9dd1e54dc0f600c641831a57791f2a02904a18c1`; independent exact-tip design
review passed. This implementation follows that bounded design without
changing P8-D or P8-E behavior.

## Delivered boundary

Added passive schema-v1 census adapters for the two P8-D owners composed by
the selected profile:

| Section | Installed owner | Cardinality | Local revision |
|---|---|---|---|
| `p8d.spatial-route-observations` | Runtime `SpatialRouteKnowledgeStore` | `ObservationCount` | `Revision` |
| `p8d.person-route-plan-history` | Runtime `PersonRoutePlanStore` | `PlanCount` (retained rows) | `Revision` |

The bootstrap test verifies exact empty day-zero values, installed-owner
identity and stable identity across repeated reads. Route-planning tests verify
new observation batches, duplicate replay, conflicts and invalid input; plan
acceptance, stale rejection, latest-per-actor replacement, and retained
history cardinality; and P8-E status-only travel transitions that advance the
plan revision without changing retained-row count.

Changed files:

- `Assets/_Project/Scripts/P12P8DCensusProviders.cs` and its Unity `.meta`
- `Assets/_Project/Tests/EditMode/Editor/SpatialRoutePlanningTests.cs`
- `Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs`

## Validation on the code-bearing candidate tree

- `SpatialRoutePlanningTests`: 21/21, XML
  `Library/ValidationResults/P12BP8D/EditMode-20260929-210438-0f774368f53445d9bd790f795afa5803.xml`.
- `SimulationBootstrapCompositionTests`: 14/14, XML
  `Library/ValidationResults/P12BP8D/EditMode-20260929-210633-22f7685d17814e16be07403f8784b8b7.xml`.
- ALL EditMode: 1955/1955, XML
  `Library/ValidationResults/P12BP8D/EditMode-20260929-210654-1e491621c60e40e5a4535ed67a9586cd.xml`.
- Official complete Smoke filter: 5/5, XML
  `Library/ValidationResults/P12BP8D/EditMode-20260929-210739-e7fd58276d3d40f98a820a5e526c2bc6.xml`.
- `git diff --check`: passed.

The XML and logs are retained locally in the candidate worktree. The tested
source tree is the code-bearing candidate tip; the result artifacts are not
part of the Unity source commit.

## Limits and integration constraints

These are unsynchronized passive owner reads. They do not register either
section in the incomplete profile census, connect owner writes to the shared
mutation epoch, prove owner-thread or quiescence, or grant capture eligibility.
They add no owner APIs, runtime registration, serialization, hydration, or
restore behavior and do not complete P12-B or make P12-A ready. Remaining
P12-B owner and invalidation/quiescence evidence stays open as recorded in
`docs/PHASE12_STATE.md` and
`docs/design/PHASE12_B_BLOCKER_RESOLUTION.md`.
