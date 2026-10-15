# CORE-F WP1-Values initial independent integration review

Verdict: **NEEDS_CHANGES** for full WP1 closure at
`dad86c6cdcaac43321fd0b27d6763bc2b103bb0b` against promoted Core
`8fc975631db0d4e53fa58c490c752fb79d209b0e` under architecture
`4da844dee0c69fdf66562590fa96cc728fc86bf7`.
Independent reviewer: `wp2_final_review`; transcribed by the Master.

Required correction: compare the actual Unity `DailyV1DefinitionAdapter.TryCreate`
typed output field by field against the independent fixed constructor fixture.
Current P9 record/hash and raw P12 member/byte comparisons do not detect a wrong
typed conversion for a field outside P9 curated records. The164-row test checks
property and converter-name presence only. The missing comparison must exercise
null/list shape and ordering, stable reference IDs, float bits, and enum numeric
value plus spelling across every mapped field, with copied immutability proof.
P12 need not derive from normalized P9 values; preserve the distinct raw boundary.

Other source audit findings were sound: explicit selected member converters,
readonly stable type/ID definition tables and copied lists, the independent
718-member/164-row constructor generator, all184 exact P9 records, retained P10
legacy path, and the single allowlisted P12 encoder with captured Unity CLR tokens.
The generator does not consume expected P12 bytes as its input. Full diff-check
passed. This result is not a promotion or full CORE-F readiness verdict.

The separately reviewed content component corrected its stricter list-count cap
with exact64MiB wire preflight and bounded output, then added Unity capture-time
preflight before DTO expansion. Source review accepted that boundary at9bb9c353.
Root Unity compilation subsequently caught private nested-member/cross-assembly
constant accessibility; the narrow selected-factory constant and private-class
method accessibility correction at3e5455ce was independently confirmed. Final
Unity compilation and its large-list budget tests passed atdad86c6c. Earlier
failed diagnostic runs remain protected and are not final evidence.

Final validation and fresh independent exact-tip review are required after the
field parity correction. No scope/product/P12 compatibility change is requested.
