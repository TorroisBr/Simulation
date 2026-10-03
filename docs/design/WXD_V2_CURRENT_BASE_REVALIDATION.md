# WX-D v2 current-base revalidation and bounded design update

**Status:** READY_FOR_IMPLEMENTATION after this bounded design clarification receives its independent exact-tip review.
**Promoted architecture design:** `codex/architecture/world-identity-projection` at `451340c56e9b676bf6ea43412bcb856b9ccde3de`, document `docs/design/WORLD_EXCHANGE_FIRST_PRODUCER_DESIGN.md`.
**Simulation implementation base:** `codex/phase12/canonical` at `aa8f0305bea9f10c15045e07400d8785c2bd9e23`.
**External contract base:** `Simulation-External` `origin/main` at `0ce8403ba05f778db6850f566a974a4c56cf4edb`; exact-tip code/review evidence includes `a3f2edaa42aeb9b1cff8b37635f988cf9b8bf64f`, which is an ancestor of this main tip.

## Current-base audit

The refreshed Simulation canonical is the requested `aa8f030`. Its history contains the promoted WI-A identity, FR-B factual-read core, FR-B live integration, and FR-C Faction factual reader.

Since the FR-C promotion tip `9a6f78c43e7ad0a8f73366055151a7710e24a759`, Phase 12 promoted the bounded `TravelPartySystem.AdvanceParties` operation at code `ac0bcffe4d345c81d77bfa56b19e3591a9ebb46c`, tree `14e2f4e485a83791af43b781546bd6f90b3913f5`, with promotion record `aa8f030`. The changes add TravelParty census/invalidation and operation handling. The FactualRead files, WorldIdentity, Faction/Person stores and contracts, and SimulationBootstrapComposition are unchanged across that interval. The current FR-C reader registration remains present in SimulationRuntime.

The active Phase Master has separate in-progress P12 edits to RuntimeIdentity, SimulationRuntime, SimulationBootstrapComposition, and P12 tests. The inspected edits concern RuntimeIdAllocator event-counter census/invalidation and do not alter FactualRead admission, the FR-C reader, or WorldId semantics. WX-D adds no edits to those shared files; a later Phase canonical advance still requires current-base revalidation before handoff.

No compatibility issue was found in the promoted WI-A, FR-B, or FR-C surfaces.

## External v2 contract and collection coverage

External main now owns schema v2 and the portable contract. V2 retains the required nine arrays and requires exactly one `collectionCoverage` status for each collection. Statuses describe the whole World named by `world.id`, not a query or selected cohort:

- `INCLUDED`: complete collection and at least one entity.
- `KNOWN_EMPTY`: complete collection and no entities.
- `UNSUPPORTED`: producer cannot provide full-collection coverage; array empty.
- `NOT_INCLUDED`: producer is capable of full coverage but deliberately omits it; array empty.

A failed or inconsistent read blocks export. It cannot become `UNSUPPORTED` or `NOT_INCLUDED`. WX-D emits only schema v2; the nine v1 arrays are not a fallback.

## Factual completeness at the selected read boundary

The promoted FR-C capability is `simulation.faction-truth/v1`. Its reader enumerates the complete `FactionStore.Factions` owner for the composed world, validates every source record, checks affiliation Faction endpoints and chronology, validates active Person endpoints, rejects invalid/duplicate affiliations, and sorts the copied Faction and active-affiliation facts deterministically. An invalid endpoint produces the reviewed `Unavailable` diagnostic path.

The adapter accepts only a published `SimulationBootstrapComposition`. Its public `FactualReads` surface performs one `TryCaptureCoherent` call for FR-C. The capture must be coherent and include the logical boundary, both store revisions, source capability/version evidence, and a `Present` `FactionTruthFacts` result. The coordinator brackets the read with the selected FR-B admission and before/after boundary and revision checks. A missing profile admission, unrecognized result shape, absent revision/boundary, or failed read blocks the export.

The composition exposes the same typed `WorldId` instance held by its runtime. WI-A's `WorldId.Value` is already validated as `world:<32 lowercase hex>`. This is the only World identity source used.

Because FR-C enumerates the full authoritative Faction owner at that same coherent boundary, a successful `Present` result supports whole-World Faction coverage: use `INCLUDED` when the projected Faction list is non-empty and `KNOWN_EMPTY` when it is empty. A failed, `Unavailable`, `Unsupported`, malformed, or inconsistent result returns no artifact.

FR-C's active-affiliation facts are endpoint-validated but do not establish a complete Person collection. People therefore remain `UNSUPPORTED`; the adapter omits Faction `memberIds` and does not emit generic Relationships. It maps no support, Knowledge, ideology, policy, creation-day, or presentation-derived data.

## Mapping and v2 payload

- Map the typed `WorldId.Value` directly to `world.id`. Omit `world.name`.
- Map every Faction fact using the promoted design's type-scoped, reversible mapping: `sim:faction:` plus unpadded base64url of the strict UTF-8 `FactionId.Value`.
- Include a Faction `name` only when FR-C supplies a nonblank source-owned display name.
- Preserve deterministic Faction ordering from FR-C.
- Include all nine entity arrays. For this first producer, only `factions` can be complete. Set `people`, `cities`, `locations`, `organizations`, `institutions`, `items`, `historicalEvents`, and `relationships` to empty arrays with `UNSUPPORTED` coverage.
- `NOT_INCLUDED` is not applicable: this producer has no additional complete collection that it deliberately omits.
- Validate the produced v2 structure and status/array consistency before returning or publishing it. Use deterministic UTF-8 JSON with stable object-key ordering and source-deterministic array ordering.

## Adapter boundary clarification

The portable v2 DTOs, ID mapping, coverage validation, deterministic JSON serialization, and atomic `*.world.json` publication belong to a dedicated embedded integration package at `Packages/com.simulation.world-exchange-producer`. A narrow Unity host in `Assets/_Project/Scripts/WorldExchange` accepts a composed bootstrap, calls its promoted FactualRead surface once, then passes copied World/Faction facts and boundary evidence into the package. The host never reads a Store directly. No Simulation domain or runtime owner depends on World Exchange types, and WX-D does not modify SimulationRuntime or bootstrap composition.

The first artifact is a stale-able, read-only World Exchange v2 projection. It is not a Simulation save, P12 snapshot, transport endpoint, synchronization path, or write-back mechanism.

## Focused validation and review

The implementation must cover WorldId mapping, non-empty Factions as `INCLUDED`, empty Factions as `KNOWN_EMPTY`, unsupported arrays and empty-array semantics, failed/unavailable capture, invalid affiliation endpoint failure, optional/blank names, deterministic ordering and bytes, omission of membership/Knowledge/support data, all nine coverage entries, schema v2 only, and no false known-empty claims. There is no `NOT_INCLUDED` output path in this slice.

Validate the generated artifact against External `world-schema` at the pinned v2 contract tip without changing Simulation-External. Run focused WX-D tests, affected FR-B/FR-C and bootstrap/runtime regressions, ALL EditMode, official Smoke, and `git diff --check`. Preserve validation XML/log evidence and hashes. Obtain an independent exact-tip implementation review.

The eventual Phase Master handoff must record the current canonical base, reviewed code SHA/tree, candidate/docs SHA, independent review SHA, validation counts, External contract SHA `0ce8403ba05f778db6850f566a974a4c56cf4edb`, touched files, coverage mapping, and scope limits. No numbered Phase canonical ref is moved by this work.
