# P12-D technical design review — exact candidate 0735103

**Review ID:** P12D-DESIGN-EXACT-TIP-0735103-R1
**Outcome:** `VALIDATED_CANDIDATE` — PASS for the P12-D technical design and the D/E/F owner-boundary context stated here. This is a design review only; it does not validate code, promote a capability, close P12-D, or make P12-A ready.

## Exact reviewed revision

- Candidate branch/ref: `codex/phase12/P12DECurrentRevalidation` / `origin/codex/phase12/P12DECurrentRevalidation`.
- Candidate commit: `0735103009b0161b3175349e9c78db241913de74`; tree: `7b8b5a2888d577d944e16ffdd0258f12183e84f5`.
- P12 canonical base: `0e786db8e6ed5ed937ff62e3f63258d8b73fd93c`.
- Architecture canonical: `47eff220c7ce00f6e7c759bdc2b76780bb46f628`.
- Candidate diff from P12 base contains only these documentation files: `docs/design/PHASE12_D_TECHNICAL_DESIGN.md` (blob `18eece609c8d387d5407d188e5a9b4635526dccb`), `docs/design/PHASE12_E_TECHNICAL_DESIGN.md` (blob `2e172f17e61e264f1129a00714ae3afeff5bd365`), and `docs/design/PHASE12_OWNER_COVERAGE_INVENTORY.md` (blob `bdeb78dbc4a7bede2c7a4c6252a14a55b497f61c`).
- Review scope: the exact D technical-design content and the inventory/E text only where needed to assess ownership overlap. This is not a full P12-E review.

## Findings

1. **Accepted scope and current prerequisites align.** The P12 Brief assigns D bootstrap factual roots and Person/population relations after B/C, while E may proceed only with isolated ownership; F owns Knowledge and active commitments; G owns whole-graph staging and publication (`docs/phases/PHASE12_BRIEF.md:9-18`; `docs/design/PHASE12_CAPABILITY_CHECKPOINT_DECOMPOSITION.md:155-190,259-290`). Current P12 State marks B and C COMPLETE/PROMOTED within bounded scopes, states the D/E dependency is satisfied, and keeps P12-A at `WAIT_DEPENDENCY` (`docs/PHASE12_STATE.md:1-49`).
2. **World identity and architecture constraints are preserved.** D consumes/carries the existing P12-C `WorldId`; it neither mints nor replaces it and adds no P13 fork behavior (D design `:8`). This matches the architecture rule that same-continuation save/load preserves `WorldId`, while a new independent fork needs explicit identity/provenance (`docs/SIMULATION_ARCHITECTURE.md:5670-5682`). The design cites the current architecture documents and both alignment records (`a231a2a014bf58be5ce382c48a55f3654df89a61`, `4ed6fcc60b348461e3201d4f3d480c21a3154ec3`) without adding P18/P20 or mod-extension scope.
3. **D/E/F owner partition is coherent.** Current Daily-v1 City and nested economic/population facts, receipts, revisions, and ordered `ImportantNpcs` membership are D; E has no City value section. D owns NPC roots and the current action-definition reference; F owns mutable action/travel/merchant payloads, active travel progress, and in-profile Knowledge. The same P12-B token/component vector binds the disjoint NPC slices before one hydrator (D design `:46-58,63`; E design `:69-98,359-360`; inventory `:296-300`). The inventory correction now describes E commercial-sharing behavior/configuration separately from F Knowledge observations, and names `PoliticalKnowledgeStore` and commercial-sharing observations in F.
4. **Genealogy owner contract is concrete and matches the live owner.** The D design requires the schema-v1 section even when empty; detached ordered edge values and exact local revision; direct revision restore without replay; and nonnegative revision no smaller than edge count, allowing removal gaps and the saturated birth-rollback case. It assigns null/malformed, duplicate, self-edge, and cycle checks to the isolated factory and defers PersonStore endpoint membership until the merged D validator after Persons are staged (`docs/design/PHASE12_D_TECHNICAL_DESIGN.md:50,80,90,105`). The design explicitly forbids a PersonStore dependency in GenealogyStore.
   At P12 base, `GenealogyStore.cs:5-8` states the store knows only `PersonId` and does not validate world membership; `:87-100` increments the local revision on successful add and rejects saturated adds; `:166-229` rejects ordinary saturated removals while the internal birth rollback can remove without incrementing. The existing B census is required schema v1, section `p12d.genealogy.parentage`, and reports the exact owner, count, and revision (`GenealogyCensusProvider.cs:8-25`). The D test contract covers empty-versus-missing, revision gaps, saturated restore/later-write rejection, immutable export, and separate merged-stage dangling-endpoint rejection (`D design :105`).
5. **One status-source mismatch is historical, not a design blocker.** The Roadmap at architecture commit `47eff` still says B was incomplete at older tip `54fc23b` (`docs/ROADMAP.md:192`). Current P12 State at `0e786` supersedes that dated readiness statement; the refreshed owner inventory now records current bounded B/C responsibilities (`docs/design/PHASE12_OWNER_COVERAGE_INVENTORY.md:285-300`). This is a stale roadmap status reference, not an unresolved D semantic or dependency.

A minor non-blocking shorthand remains at D design `:65`: the export-contract bullet mentions Genealogy edges without repeating the revision requirement. The owner table, hydration, validation, and test clauses specify exact revision preservation, so this does not block the isolated slice.

## Readiness and limits

**GenealogyStore isolated owner slice: READY_FOR_IMPLEMENTATION.** The bounded slice is exact detached edge/revision export plus a private staged factory that rebuilds owner indexes, restores the revision directly, and validates only Genealogy-local invariants, with the specified owner tests. It has no PersonStore dependency. It does not require changes to `SimulationRuntime`, `TesteSimulacao`, bootstrap, CityRuntime, or NpcRuntime; later merged D validation checks PersonStore endpoints and later serialized integration wires the owner into full capture/hydration.

**Whole P12-D remains OPEN.** City/NPC shared-owner adapters, other factual-root owners, cross-owner validation, and D integration evidence remain outstanding. P12-A remains `WAIT_DEPENDENCY`; this record grants no implementation authorization beyond the reviewed isolated owner slice and makes no save/load or phase-closure claim.

## Validation

The reviewed candidate is documentation-only. No Unity or code tests were run, and no implementation validation is claimed. The review was performed against the exact commits and blobs listed above. The review worktree is based on the candidate commit; the candidate branch and unrelated dirty P20 checkout were not modified.