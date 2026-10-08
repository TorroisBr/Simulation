# P12-E Property/Estate Owner Snapshot Design Review

**Outcome:** `NEEDS_CHANGES` — one owner-invariant validation gap remains.

## Exact content reviewed

- Candidate branch: `codex/phase12/P12EPropertyEstateSnapshotDesign` at `bbf7272eaeb033d25d774d0f65140846538e92b7`.
- Exact base and current P12 canonical: `f5d99cb7008023d14a0ed16ea2149a7d7c18def1`; candidate parent matches this SHA.
- Candidate commit tree: `cd83d738f73f5e2a08a4122ee1eefb80edd97c24`.
- Candidate `Assets` tree: `86df24ff56a945160da517b6f62325ba1f8af8dc`, equal to the base `Assets` tree. The complete base-to-candidate diff adds only `docs/design/PHASE12_P12E_PROPERTY_ESTATE_OWNER_SNAPSHOT_DESIGN.md`; no Unity tests are required for this docs-only change. `git diff --check` passed.
- Architecture authority consulted: `codex/architecture/world-identity-projection` at `e16796014d348e3b59da7ed848101c4c03926ba5`. Reviewed current architecture, Execution Model, Roadmap, Phase 12 Brief/State, accepted P12-E technical design and review, Property/Estate census designs and reviews, P12-B Property/Estate mutation-epoch design, and current owner implementations.

## Finding

**E1 — The staged Estate validation does not explicitly preserve the one-estate-per-deceased-Person invariant.** The design requires unique `EstateId` values and valid deceased `PersonId` references (`PHASE12_P12E_PROPERTY_ESTATE_OWNER_SNAPSHOT_DESIGN.md:44,50`), but its required rejection cases mention only generic “duplicate IDs” and describe deceased-Person index behavior without requiring duplicate-deceased-Person rejection (`:72-73`). The current `EstateStore.TryRegister` rejects a second Estate for the same deceased Person through `recordsByDeceasedPerson.ContainsKey` (`Assets/_Project/Scripts/Property/EstateStore.cs:84`). Two rows with distinct EstateIds and the same deceased Person therefore need an explicit owner invariant and negative staging case; otherwise reconstruction may fail while rebuilding the index or produce a staged owner that violates existing behavior.

Required correction: state that `DeceasedPersonId` is unique across Estate rows, reject duplicate deceased-Person references before returning either staged owner, and require a fixture with two distinct EstateIds for one deceased Person that proves staging returns no pair and leaves source/active state unchanged.

## Reviewed areas without additional findings

The design preserves the selected Daily-v1 owner sections, Property shared identity/revision, separate Estate revision, full retained transfer history, typed C/D references, and existing succession semantics. It does not invent an Estate-to-property membership relation, replay domain transitions, mutate or duplicate the staged Person root, bind the final guard, add a capture lock/epoch, or claim P12-G publication. Its private staging, failure-atomicity, determinism, and integration boundaries are otherwise consistent with the accepted P12-E contract and current owner source.

No implementation or Unity tests were run because this candidate changes documentation only. This review does not authorize implementation, P12-A integration, canonical promotion, or Phase closure.