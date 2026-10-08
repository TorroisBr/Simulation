# P12-E Institution/Office Owner Snapshot Design Review

**Outcome:** `PASS` — the owner slice preserves the accepted semantics and specifies adequate exact-order and duplicate-occurrence coverage.

## Exact content reviewed

- Candidate: `codex/phase12/P12EInstitutionOfficeSnapshotDesign` at `52a1e5942e6c5fd57040566f71fe0e4fda508373`.
- Exact base and current P12 canonical: `f5d99cb7008023d14a0ed16ea2149a7d7c18def1`; candidate parent matches this SHA.
- Candidate tree: `6a9cefb8f9d49dbf2e92b839535baf0608d66ac6`.
- Candidate `Assets` tree: `86df24ff56a945160da517b6f62325ba1f8af8dc`, equal to the base. The complete diff adds only `docs/design/PHASE12_P12E_INSTITUTION_OFFICE_OWNER_SNAPSHOT_DESIGN.md`; `git diff --check` passed. No Unity validation is required for this docs-only candidate.
- Current architecture revalidation: `codex/architecture/world-identity-projection` at `e16796014d348e3b59da7ed848101c4c03926ba5` (architecture blob `25843842688239cdc3b80988b2e28dbaa16b4987`). The proposal cites earlier architecture tip `47eff220`; review checked the current §§25, 91–92, 91A–91B, and 92A and found no invalidated assumption. Also reviewed the current Phase 12 Brief/State, Roadmap, Execution Model, accepted P12-E technical design/reviews, Institution/Office census contract, current owner code, and runtime clone path.

## Review findings

No blocking findings.

The owner value schema keeps active incumbencies distinct from retained tenure history, preserves nullable dates and exact reason/closed values, and does not infer current occupancy from history. The capture reads `TenureHistoryInMutationOrder`; the private factory restores the complete list directly and avoids runtime clone/public mutation paths. This is necessary because vacancy closes an open row in place and later supported cycles can append a value-equal closed row, while `TryAddHistoricalTenure` has a duplicate guard used by clone construction. The design explicitly says not to use that guard as a snapshot validity rule and to preserve every occurrence and order (`PHASE12_P12E_INSTITUTION_OFFICE_OWNER_SNAPSHOT_DESIGN.md:60,89,92`; `InstitutionStores.cs:163,328,436,446-475`; `SimulationRuntime.cs:10026-10040`).

The implementation test obligations are sufficient for the identified risk: they require a repeated assignment/vacancy cycle that creates two equal closed tenure values at separate list positions and asserts both survive detached export and staged round-trip with unchanged count and order; they also require exact list-order preservation for populated histories and deterministic repeated capture (`PHASE12_P12E_INSTITUTION_OFFICE_OWNER_SNAPSHOT_DESIGN.md:131-132`). Open-tenure/incumbency consistency, duplicate IDs, dangling references, enum/date validation, fail-closed staging, source immutability, and no domain replay are also specified.

## Readiness boundary

This validates the bounded Institution/Office design only. It does not claim P12-B completion, P12-A readiness, profile-wide owner coverage, implementation delivery, P12-G publication/parity, canonical promotion, or Phase 12 closure. Implementation remains subject to the named owner handoff and its required focused/regression/full validation.