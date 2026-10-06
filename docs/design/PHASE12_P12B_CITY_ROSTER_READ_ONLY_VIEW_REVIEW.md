# P12-B City roster read-only view design review

**Result:** PASS — independent technical design review.

- Design branch/tip: `codex/phase12/P12BCityRosterReadOnlyViewDesign` at
  `24522619b4418b9f506f9a59aec21eb188848560`.
- Canonical base: `codex/phase12/canonical` at
  `69a5a41ca879fb66ce62efbe0d62e31f7298675b`.
- Reviewed artifact: `docs/design/PHASE12_P12B_CITY_ROSTER_READ_ONLY_VIEW.md`.
- Reviewer: independent P12 owner-audit reviewer; no candidate edits or tests.

The source finding is confirmed: `SimulationRuntime` copies and sorts its
constructor City input into one private list, does not mutate that list after
construction, and returns the same mutable `List<CityRuntime>` as
`IReadOnlyList<CityRuntime>`. Returning one retained `AsReadOnly()` view is
sufficient and source-compatible for current consumers, which use only
read-only list operations. It preserves order and exact City object identity
while making `IList<CityRuntime>` report read-only and reject mutation.

The proposed selected Daily-v1 test is appropriate: verify two City owners,
stable order/references and identity census, reject mutation through the
`IList<CityRuntime>` view, and confirm the roster and census remain unchanged.
P10-A remains separate; no City lifecycle semantics or P12 readiness claim is
introduced.

**Disposition:** READY_FOR_IMPLEMENTATION within the already accepted P12-B
scope. This review does not authorize a new City operation or change Phase
readiness. Exact-tip implementation review, validation, and canonical
promotion remain separate gates.
