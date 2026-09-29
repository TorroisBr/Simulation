# P12-B P8-A passive populated-geography census candidate

**Status:** Promoted to `codex/phase12/canonical` as a P12-B partial
foundation. P12-B remains incomplete and P12-A remains `WAIT_DEPENDENCY`.

**Branch:** `codex/phase12/P12BP8APopulatedWitnesses`.

**Canonical base:** `06145c7cbc258c56cc1be1a24adaa1751d32bc01`.

**Reviewed design:** `codex/phase12/P12BP8APopulatedWitnessDesign` at
`0baba5cd1981950f9779f6e9772e41c88dc0cfbf`; independent exact-tip design
review passed. The design record is `docs/design/PHASE12_P8A_CENSUS_DESIGN.md`.

**Code-bearing candidate tip:** `9efba61` (provider/test commit `3bf0619`,
followed by the shared-revision temporal test fix `9efba61`).

**Promotion:** User-approved fast-forward from canonical
`06145c7cbc258c56cc1be1a24adaa1751d32bc01` to the reviewed candidate tip
`644bdae8ded1d8a938ec380370966ca6c235b881`. Independent exact-tip review
passed; durable review evidence is branch
`codex/phase12/P12BP8APopulatedReviewRecord` at `a401860`.

## Delivered boundary

Adds three schema-v1 passive providers over the installed runtime's
`SpatialAuthorityStore`:

| Section | Cardinality | Revision |
|---|---|---|
| `p8a.hexes` | `HexCount` | `SpatialAuthorityStore.Revision` |
| `p8a.locations` | `LocationCount` | `SpatialAuthorityStore.Revision` |
| `p8a.scale-context` | `HasGeography ? 1 : 0` | `SpatialAuthorityStore.Revision` |

The separate cardinalities preserve distinct owner-section identities. The
store revision is shared and conservative: it advances for other spatial
authority changes too, so consumers must revalidate every section sharing
that revision. Providers retain the exact installed owner reference and make
no synchronization or capture-eligibility claim.

`SimulationBootstrapCompositionTests` verifies that the selected authored
bootstrap exposes the runtime-installed owner and produces exact populated
day-zero witnesses (1 Hex, 1 Location, 1 scale context, revision 1). It also
checks stable owner identity across repeated reads. `SpatialGeographyTests`
checks exact empty values, independent section cardinalities, shared revision,
and that a rejected duplicate-coordinate composition leaves all witnesses at
zero and revision zero before a valid composition advances each to one at
revision one. A separate temporal case registers a barrier through the
existing P8-B spatial authority: Hex/Location/scale counts remain 7/1/1 while
all P8-A witnesses observe the shared parent revision advance from 1 to 2.

Changed files:

- `Assets/_Project/Scripts/P12P8ACensusProviders.cs` and Unity `.meta`
- `Assets/_Project/Tests/EditMode/Editor/SpatialGeographyTests.cs`
- `Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs`

## Validation on code-bearing candidate tip `9efba61`

- `SpatialGeographyTests`: 15/15 passed, XML
  `Library/ValidationResults/P12BP8A/EditMode-20260929-214450-da4a0929b7304664b12fc7ab23f1e6da.xml`.
- `SimulationBootstrapCompositionTests`: 14/14 passed, XML
  `Library/ValidationResults/P12BP8A/EditMode-20260929-214656-30967baf7f304c668db14f650f7dd451.xml`.
- ALL EditMode: 1957/1957 passed, XML
  `Library/ValidationResults/P12BP8A/EditMode-20260929-214515-de1df8d8d50544baa385ab67fe6cb25c.xml`.
- Official complete Smoke filter: 5/5 passed, XML
  `Library/ValidationResults/P12BP8A/EditMode-20260929-214552-ab76f5a6ed3642a3bd86680f060a7a8d.xml`.
- `git diff --check`: passed.

The Unity XML and logs are retained in the candidate worktree and are not
committed as source artifacts.

## Limits

These reads are unsynchronized, passive witnesses. They are not registered in
a complete profile census, do not connect spatial writes to the shared
mutation epoch, do not prove owner-thread identity or quiescence, and do not
grant capture eligibility. This is not P12-B completion, P12-A readiness,
export/hydration support, or a save/load implementation. It adds no runtime
registration, mutation hooks, synchronization, or new geography semantics.
