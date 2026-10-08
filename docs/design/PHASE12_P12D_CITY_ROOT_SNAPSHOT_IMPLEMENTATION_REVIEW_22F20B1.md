# P12-D City Root Owner Snapshot — Exact-Tip Implementation Review

**Result: PASS**

**Reviewed candidate:** `22f20b1ab0cf9ba4fc36155c645554c98bff9c6e`

**Implementation code commit:** `58c142329d034fbed18ece25ee017d0d69f4d62f`

**Base P12 canonical:** `aa1a40f2e6da53a1a7388601bd13052f1a535545`

**Reviewed Assets tree:** `e83151063141cb1395e1d53d30e850c6f81ed778`

**Design:** `docs/design/PHASE12_P12D_CITY_ROOT_SNAPSHOT_DESIGN.md` (blob `3ae6725227a166d8030a017c210d9a5459cab830`)

**Design review:** `docs/design/PHASE12_P12D_CITY_ROOT_SNAPSHOT_DESIGN_REVIEW_F30E6BA.md` (blob `ace865a3dd6a08cb97d9ada8f38664d7fa1a644a`)
**Architecture baseline:** `47eff220c7ce00f6e7c759bdc2b76780bb46f628`

## Review scope and findings

Reviewed the City, Market, and SettlementPopulation owner changes; the detached snapshot and staged factories; membership relation linker; focused tests; design and design-review records; and validation manifest. The implementation matches the accepted bounded City sub-slice. **No blocking findings.**

- Capture requires the Daily-v1 completed-boundary token, shared stamp, exact token owner-section vector, and unique required sections with exact owner identity, schema, cardinality, and local revision. City membership order and `ImportantNpcRevision` are preserved without adding a synthetic City revision.
- Market rows preserve order, multiplicity, amount, desired amount, stored price, and `MarketRuntime.Revision`. Staging resolves admitted item definitions uniquely and restores exact rows/prices/revision without replaying trade or price updates.
- Population aggregate identity/value/revision and the separate receipt-ledger revision are retained. Receipt rows are sorted by identity and copied, including immutable transition values. Staging validates receipt identities/transitions and reconstructs the exact ledger. Existing rollback pruning semantics remain represented by the retained receipt state and local revision.
- Staged City/Market share the exact open, account-free counterparty; population economics remain free and account-free. Account-backed City state, nonempty/advanced P18 City receipts, and P14 material-flow state are rejected.
- City staging reserves an empty private membership list; the one-shot linker fills it in captured order after NPC objects exist, validates reciprocal City/Location references, and does not replay gameplay membership operations or advance the revision.
- The candidate does not add runtime/bootstrap composition, NPC serialization, shared graph construction, P12-B invalidation, P12-G publication, whole-graph validation, or broader readiness claims.

The only delta from implementation commit `58c1423` to reviewed tip `22f20b1` is a documentation correction to three source hashes in `docs/validation/P12DCityRootOwnerSnapshot/VALIDATION.md`. The candidate Assets tree remains exactly `e83151063141cb1395e1d53d30e850c6f81ed778`. The corrected hashes were independently recomputed from the committed Git blobs; all five listed source hashes now match.

## Retained validation evidence

Validation was not rerun for this documentation-only correction. I checked the retained XML and compressed-log hashes against the manifest and parsed the XML results:

| Gate | Result | XML SHA-256 |
|---|---:|---|
| Focused `P12DCityRootOwnerSnapshotTests` | 7/7 passed, 0 failed, 0 skipped | `F4F98E7B15FD67E5CBB673239FBDCEBF27BE1BD47F93E67B9C26F1DE523746D5` |
| ALL EditMode | 2563/2563 passed, 0 failed, 0 skipped | `334ABB4B826BC1292A0DD3ABDBEF7F8EDA615CC499A1112B12B490FB994A026F` |
| Official Smoke | 5/5 passed, 0 failed, 0 skipped | `14B68F99FA1D984F4ADC2130A0F83A713D61A77FCDAE58F66381B8EEEE692D08` |

The compressed-log hashes also match the manifest. `git diff --check` from canonical base through reviewed tip passes. Candidate tip `22f20b1` is a descendant of canonical base `aa1a40f`; no implementation code changed in the docs-only correction.

## Limits

This PASS covers only the reviewed P12-D City root owner snapshot/staging sub-slice. It does not claim P12-D completion, P12-A readiness, P13 readiness, whole-graph validation, runtime/bootstrap integration, profile-wide export/hydration, P12-G publication, or Phase closure. P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked.
