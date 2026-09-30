# P12-B City NPC-presence projection census candidate

**Status:** Implementation candidate; canonical promotion requires approval.
This is a partial passive census foundation. It does not complete P12-B or
make P12-A ready.

## Candidate and review

- Feature branch: `codex/phase12/P12BNpcOwnerCensusImplementation`
- Code candidate: `aafa81ea6a2cf6b8963d15ee9ed8d26577b368a2`
- Exact base: `41b6abd97a8a63fca380523c486f5c2df716e09f`
- Independent exact-tip implementation review: PASS
- `git diff --check` against the base: PASS

The design contract is in
`docs/design/PHASE12_P12B_CITY_NPC_PRESENCE_CENSUS_DESIGN.md`.

## Delivered boundary

Each installed City has one schema-v1 passive witness keyed by its stable
RuntimeId and bound to that exact `CityRuntime` object. It reports the live
`ImportantNpcs` count and City-owned monotone membership revision. Before
issuing a witness, it checks the runtime's live NPC roster in both directions:
projected members must be unique registered NPCs pointing to that exact City
and Location, and every rostered NPC pointing to the City must occur once in
its projection. Duplicate, missing, or non-reciprocal membership fails closed.

The City owns all supported projection mutations and exposes a live read-only
view. Membership changes increment the revision once; duplicate, absent,
rejected, and no-op changes do not. Revision capacity is preflighted for
cross-City moves, single travel departure/arrival/cancellation, party start and
rollback, and final-day party arrival before participant state or costs change.

This provider remains passive and is not registered in the active P12-B
continuation protocol. It does not add City writes to the global mutation
epoch, establish owner-thread quiescence, provide City export/hydration, or
grant capture eligibility. The City composition still relies on the existing
fixed `SimulationRuntime.Cities` boundary.

## Validation evidence

All runs used the official `Tools/UnityValidation/Invoke-UnityValidation.ps1`
harness in this candidate worktree:

| Gate | Result | Evidence |
|---|---:|---|
| `GeneralizedSpatialTravelTests` | 18/18 | `Temp/ValidationResults/EditMode-20260930-200847-19df88761726438d94320a2e219693fe.xml` |
| `GroupTravelTests` | 32/32 | `Temp/ValidationResults/EditMode-20260930-200911-e43a48a72afa43bda0e22609c778f050.xml` |
| `SimulationBootstrapCompositionTests` | 14/14 | `Temp/ValidationResults/EditMode-20260930-200928-5aa7dfef8e574703a9ed8a03c085732c.xml` |
| All EditMode | 2042/2042 | `Temp/ValidationResults/EditMode-20260930-201247-3c223832250b4f1fafd2ea860a29d9f1.xml` |
| Complete official `Smoke` filter | 5/5 | `Temp/ValidationResults/EditMode-20260930-201333-452db28f7954465c9cafdd74d14d1028.xml` |

The final all-EditMode and Smoke runs include the exact code candidate. The
focused runs preceded removal of one unused, unrelated Inventory accessor; the
final full regression gates were rerun after that removal.

## Retained P12 limits

- P12-B remains incomplete; this City witness is not global owner coverage.
- P12-A remains `WAIT_DEPENDENCY` until complete owner export/hydration,
  validated live-profile inventory, and its separate authorization are present.
- Inventory, other NPC-owned facts, global mutation-epoch wiring, and
  cross-owner quiescence remain separate blockers.
