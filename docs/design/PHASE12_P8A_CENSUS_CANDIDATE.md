# P12-B P8-A passive populated-geography census candidate

**Status:** Implementation candidate; independent exact-tip review pending.
P12-B remains incomplete and P12-A remains `WAIT_DEPENDENCY`.

**Branch:** `codex/phase12/P12BP8APopulatedWitnesses`.

**Canonical base:** `06145c7cbc258c56cc1be1a24adaa1751d32bc01`.

**Reviewed design:** `codex/phase12/P12BP8APopulatedWitnessDesign` at
`0baba5cd1981950f9779f6e9772e41c88dc0cfbf`; independent exact-tip design
review passed. The design record is `docs/design/PHASE12_P8A_CENSUS_DESIGN.md`.

**Code-bearing commit:** `3bf0619` (`Add passive P8-A spatial census witnesses`).

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
revision one.

Changed files:

- `Assets/_Project/Scripts/P12P8ACensusProviders.cs` and Unity `.meta`
- `Assets/_Project/Tests/EditMode/Editor/SpatialGeographyTests.cs`
- `Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs`

## Validation on code-bearing commit `3bf0619`

- `SpatialGeographyTests`: 14/14 passed, XML
  `Library/ValidationResults/P12BP8A/EditMode-20260929-213920-b4fa6edc3dfd44daa56228c8e3999aff.xml`.
- `SimulationBootstrapCompositionTests`: 14/14 passed, XML
  `Library/ValidationResults/P12BP8A/EditMode-20260929-213948-29ac4ebb522f45a88992a25162461fed.xml`.
- ALL EditMode: 1956/1956 passed, XML
  `Library/ValidationResults/P12BP8A/EditMode-20260929-214023-4033a54a4f144ed99ae23f68450fd7c0.xml`.
- Official complete Smoke filter: 5/5 passed, XML
  `Library/ValidationResults/P12BP8A/EditMode-20260929-214104-748bd83286624783849045e9a6e7b2b2.xml`.
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
