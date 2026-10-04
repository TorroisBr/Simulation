# Phase 10 State — Local Generation & Pre-start Authoring

**Status:** PHASE 10 IN PROGRESS — P10-A is promoted; Phase 10 remains open.

**Architecture baseline:** `c285466c355103d3637ac165246591b72eb7bda0`.

**Upstream canonical capabilities:** P8 at
`470667d37863384edadb3d93ef64d8004aff46a3`; P9-B authored-geography code at
`d9a62d7c6bea242653c2d68cc0a70911bb5ed1bf`, with current P9 canonical
closure/State tip `82396ae7ffaf407fda278928da456b06dc5394d4`.

## P10-A — Ruin LocalTopology Genesis Composition

The user approved exactly one `ExplorableSiteKind.Ruin` at the actual selected
P9-B `LocationId`, with the minimum finite LocalTopology facts required for a
small proving layout. The approved fixture may use Entrance → Courtyard → Inner
Chamber. P10-A owns the bounded LocationId-neutral topology owner/migration
seam, stable semantic owner resolution and P10 pre-start composition. P8-C
continues to own its promoted Ruin/site anchor contract and existing City/Site
runtime-handle behavior.

The durable checkpoint record and refreshed technical design are on
`codex/phase10/P10ARecordIntegration` at review candidate
`345dcbc8f05e0d64fed1b058d30f08ad0be8937d`. Independent technical review:
**PASS** on that exact candidate against P8 canonical, P9-B code/current State
and architecture `c285466`. The user-approved scope and satisfied promoted
dependencies made P10-A ready for implementation.

The checkpoint adds no procedural terrain, settlement/population generation,
regional routes, Knowledge, activities, loot, encounters, construction, runtime
expansion, Mod API/loader, retrofit, or other gameplay. Intraday/extensibility
and multi-participant requirements remain review constraints; the P10 profile
creates no P18/P20 state or dependency. P19 loader/API work remains deferred.

## Delivery status and next gate

The implementation candidate is
`codex/phase10/RuinLocalTopologyGenesis` at
`88720a690a2bc9853d1e3967b4f99d2abd294fd3`, based on the reviewed readiness
record `d721aa48479f5239d9d52a1e5fc397e382487d24`. Independent code review:
**PASS** on the exact implementation candidate and exact base. The review
confirmed the selected P9-B Location/anchor handoff, one-Ruin constraint,
ordered and validated pre-start publication, stable semantic identity across
runtime allocator changes, and P8-C composition checks. No blocking findings.

The candidate fast-forwarded unchanged onto
`codex/phase10/RuinLocalTopologyIntegration`. Validation was run against the
same exact code SHA before that ref-only integration: P10 focused tests 6/6,
bootstrap composition 14/14, ALL EditMode 1718/1718, complete official Smoke
5/5, and `git diff --check` passed. A replay attempt in the fresh integration
worktree could not produce test results because Unity package resolution
failed with `ENOSPC`; that attempt is not counted as a pass. The integration
branch contains the same tested code tree and commit.

The delivered candidate implements the actual selected P9-B profile handoff
and provenance, semantic DefinitionId/LocationId-to-RuntimeId mapping through
P8-C's composed-runtime validator, atomic pre-boundary publication, stable
finite topology, and City/Site compatibility. The focused suites and full
EditMode/Smoke gates are specified in
`docs/design/PHASE10_A_CHECKPOINT_RECORD.md`.

The user approved promotion after the exact-tip independent review, regression
gates, and promotion preflight passed. `codex/phase10/canonical` was created at
and pushed to `9501bf076d506fb64d6ee3e6d178574fff36e153`; this record is a
documentation-only follow-up on the integration branch. P10-A now delivers the
bounded Ruin/LocalTopology runtime capability described above. Phase 10 remains
open until its approved objective and any separately accepted mandatory
checkpoints are complete with an independent closure review and a formal State
closure marker.
