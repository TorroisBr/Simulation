# CORE-F selected NPC Inventory and MoneyAccount census — independent review

**Disposition: VALIDATED_CANDIDATE**

## Exact identities

- Review branch: `codex/core/CORE-F-NPC-census-review-record`
- Review base / candidate exact tip: `11c85ac7deea3a98158c5d240a946b158cdd1f45`
- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Candidate code commit: `b3448e171ffbcc90f5e29421e0e20f787540dcb2`
- Candidate code tree: `cac40d9ca450e1f21699a6b2253a0591a54a37a1`
- Source base: `9c5e85cdbc5d094a4b934d63c689ebbb8a972a91`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Live local and `origin` candidate refs both resolved to `11c85ac7deea3a98158c5d240a946b158cdd1f45`. Local and `origin/codex/core/canonical` both resolved to the stated canonical SHA. The candidate is not an ancestor of canonical.
- The exact tip adds only the validation record. The base-to-code-commit diff is exactly four code/test files, as listed in `VALIDATION.md`.

## Review scope and findings

Reviewed the complete `9c5e85c` → `b3448e1` source diff, the exact-tip validation record, repository `AGENTS.md`, `.agents/skills/candidate-review/SKILL.md`, the P12-B NPC Inventory census design/candidate and its prior exact-tip implementation review (`15e5a543f3896c02f9dbd9c36c9ba75bc90dac2b`, review record commit `f98c5d4f3ddc3721b4b31de1a40f063e2b333831`), the promoted P12-B per-NPC MoneyAccount review, the dynamic NPC census review, the CORE-F Daily-v1 host and shared-source contracts, and the owner-family census declaration.

The implementation derives two providers per selected graph NPC and checks the checked expected count, unique nonblank runtime IDs, exact dynamic section keys, unique provider witnesses, required schema, nonnull owner identity, nonnegative cardinality/revision, and complete coverage. It ordinal-sorts the composed witness set. Exact omission is allowed only when both NPC families are entirely absent from the structurally complete host snapshot and the exact graph has selected NPCs; those rows are then recomposed from Core owner witnesses. Otherwise selected rows must match required role, schema, owner object identity, cardinality, and revision before rebinding. A mismatch remains in the vector and is rejected by the Core composition checks rather than omitted.

When host inventory and MoneyAccount row counts exceed the selected graph count, the established mixed-roster host path returns the complete vector unchanged. The Unity admission test verifies that every selected and compatibility row remains present, including the unselected compatibility NPC. Exact-roster tests verify both families' selected owner identities and facts and verify a stale revision yields no omission. The capture test also checks the host mutation epoch remains equal to the completed-boundary token epoch. The 89-family P12 owner inventory and structural completeness gate are unchanged; the existing five P12-F and three allocator rows remain validated and disjoint from the newly Core-recomposed dynamic rows.

The diff changes existing assertions to account for dynamic cardinality and expands coverage. It removes no test case or assertion; the .NET test was renamed to describe its expanded coverage. No source/test changes outside the four declared files were found. `git diff --check` passed.

No actionable defect was found in the reviewed exact-roster omission, mixed-roster fallback, stale/substituted row retention and rejection, cardinality/revision, full-vector, or capture-boundary behavior. This validates only the bounded candidate; it does not complete CORE-F or promote Core canonical.

## Validation evidence reviewed

The candidate worktree's private artifacts were inspected in place and left unchanged. The raw Unity XML reports match the candidate validation record:

- Focused `StartCapturesAndPassesTheSelectedProfileOwnerThread`: run `cc138b1cd7ab4f32acd7112c4f65a5da`, 1/1 pass.
- Focused `SelectedCrimeJusticeDailyStagePublishesP12WritesAndUsesWholeRosterCompatibilityFallback`: run `86a1a220f2f542efaa805c5ff6ec67cc`, 1/1 pass.
- `SimulationRuntimeAdmissionTests`: run `b1c45e730ddb469fa7788cd4368e1a04`, 243/243 pass.
- Full EditMode: run `1760572392af492397bafcc5759e42f9`, 3299/3299 pass.
- Official EditMode Smoke: run `1b70958502034574a8e8d706f45e1cc2`, 5/5 pass.
- `SimulationRuntimeLongRunTests`: run `c9f39ac89b5f49c985359b6012da3296`, 7/7 pass.
- Official CoreValidation: run `a4701bc89ae642ee8a435a6c45434e6e`, 1016/1016 cold and warm; cold/warm Core, CalendarDemo, and Lab build/run gates exited successfully. The passing corrected run is retained separately from the excluded wrong-working-directory invocation.

The initial PlayMode zero-test filter is excluded. The official Smoke gate is the EditMode Smoke run above. The validation record contains hashes for the source inputs, manifests, XML/TRX reports, and documents the excluded diagnostic runs. Private reports and logs remain preserved in the candidate worktree.

## Integration boundary

The candidate is based on `9c5e85c`, while Core canonical is `a07402f`; refresh dependencies against current canonical before any integration decision. This review does not grant promotion or canonical authority. Preserve the candidate's isolated source diff and its private validation artifacts.

