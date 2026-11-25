# Independent exact-tip implementation review — allocator census and membership binding

**Verdict:** `VALIDATED_CANDIDATE`

**Repository:** `https://github.com/TorroisBr/Simulation`  
**Candidate branch:** `codex/core/CORE-F-try-start-transaction`  
**Base:** `1a33d6e8bc0731b1b1f512d13e8b04f79657ebfd` (tree `cac102e13fa4262b03afdf909f02d15cd4206963`)  
**Reviewed candidate:** `9e96f49aaa1eebc11209b85da1e7477004daa338` (tree `c88e156d35f7f010b060ae7207f2d500e1fa4638`)  
**Core canonical at review:** `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

## Reviewed scope

This review covers only the five-file incremental commit above. It binds the selected `RuntimeIdAllocator` instance to the Core owner graph, preserves that exact root across graph and NPC membership replacement, and makes the event, decision, and travel-party allocator counters Core-derived selected census rows. It does not assert completion of CORE-F, P12, or the 89-family owner obligations.

## Review result

No actionable defects were found.

- Initial execution-root binding stores the same allocator reference on the selected graph. Repeat binding and exact-root predicates require reference identity; a substitute allocator cannot satisfy the selected graph contract.
- Both selected graph replacement and the atomic NPC membership transition bind the existing execution-state allocator into the proposed graph before preparing replacement actor-turn roots. The execution state publishes the replacement graph/roots/roster only after validation succeeds; failure leaves the currently published graph, roster, and roots intact.
- The three Core allocator witnesses use the existing provider and owner bridge. Each has the fixed section ID and schema, the same non-null allocator-owner identity, required role, cardinality one, and a nonnegative counter revision that matches the live allocator. Census reads do not allocate IDs or mutate allocator state.
- Host census composition accepts either the complete exact triplet or complete omission, in which case Core supplies all three rows. Partial omission, duplicate sections, substituted owner identity, stale revision, and differing row facts fail closed. The graph-aware removal path validates exact allocator rows before removing them from the host remainder; production call sites use that graph-aware path.
- The full selected vector is structurally validated after allocator rows are recomposed, retaining the accepted 89-family completeness contract. The change does not reinterpret omitted required rows as empty or alter P12 root/high-water semantics.
- Tests cover exact owner identity/cardinality/revision, full and omitted triplets, partial omission, per-row substitution and stale revisions, live counter revision changes, graph binding during NPC membership replacement, and exact actor-turn roots after publication. Existing allocator methods and ordering are unchanged, so deterministic ID sequencing remains owned by the existing allocator implementation.

## Validation evidence inspected

The private official artifacts were inspected in place. Raw XML and logs are not copied into this review record because they contain machine-local paths.

| Gate | Result | Artifact identity |
|---|---:|---|
| CoreValidation cold | 1008/1008 PASS | Run `1251178b4ab04da48b1e6ec889b1d587`; cold TRX SHA-256 `f9b0b824a572e6f02eafe8ce52f6afea1fd288a708dce398f1f1ddfd8e5cc8bf` |
| CoreValidation warm | 1008/1008 PASS | Same run; warm TRX SHA-256 `b4fb4a22b91ee34c02eaea821bf8ec1fbe9501cf9a01a80f62452b93465578b4` |
| CoreValidation lab build/cold/warm | PASS, exit 0 for all three | Recorded in the same run manifest |
| Focused Unity dynamic-roster restore | 1/1 PASS | Run `338db22f49004843adaf6c48c334067e`; XML SHA-256 `33e9308960ef6e246a9ca6748b2ef959458204ce27c1b18b299b84989b6f2b2a` |
| Full Unity EditMode | 3299/3299 PASS, zero failures/skips | Run `5c12f0868d61489faa886af2ff5649b0`; XML SHA-256 `5e6fa1912f4b3aff83ad03c335f97a561ddc50b74f3f8b1fb9b2f2460c826fc0` |
| `git diff --check` | PASS | Exact candidate diff against base |

The CoreValidation manifest identifies the base commit plus a working-tree source digest and records the modified source names. Its per-file input hashes match the candidate bytes for the three changed Core source files and the changed .NET test file. The Unity manifest records the base commit, stable inputs, and matching candidate hashes for all four changed production files; the separate .NET test source is outside that Unity input set. This evidence is sufficient for the respective source/test scopes and is not represented as a single cross-run package.

Earlier failed validation attempts remain superseded diagnostics and were not used as passing evidence. The review did not alter implementation or test files. This record does not promote the candidate to Core canonical or claim CORE-F/P12 completion.
