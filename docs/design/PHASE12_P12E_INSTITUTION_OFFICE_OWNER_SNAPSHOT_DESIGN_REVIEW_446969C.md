# P12-E Institution/Office Owner Snapshot Design Review

**Verdict: NEEDS_CHANGES**

## Review identity

- Checkpoint: accepted P12-E Institution/Office owner snapshot slice.
- Canonical base: `codex/phase12/canonical` at `f5d99cb7008023d14a0ed16ea2149a7d7c18def1`.
- Design candidate: `codex/phase12/P12EInstitutionOfficeSnapshotDesign` at `446969cf055e3268413d5e64a3581745326b10e3`.
- Candidate tree: `99f8e0d75a6d1fabf872d748ba3cd93137a80114`.
- Architecture authority reviewed: `codex/architecture/world-identity-projection` at `47eff220c7ce00f6e7c759bdc2b76780bb46f628`.
- Diff: one documentation-only design file; no code or tests changed.

## Scope and contract review

The design stays within the accepted P12-E Institution/Office owner slice. It reuses the four existing required census sections, keeps the InstitutionStore/OfficeStore reference relationship, preserves local revisions and tenure mutation order, stages privately after the P12-D PersonStore, and excludes runtime publication, new capture semantics, and P12-A readiness. The proposed validation correctly keeps Office distinct from Title/Social Status and does not execute recognition or succession during hydration. These boundaries align with the current architecture and P12-E decomposition.

Documentation-only review does not require Unity validation. `git diff --check` against the named base passed.

## Blocking finding

### [P1] Do not reject equal tenure values when the live owner can retain them

Design §5 requires rejecting duplicate tenure values because `TryAddHistoricalTenure` rejects an already identical row. That helper is only one insertion path; it does not establish a store-wide uniqueness invariant. The supported live assignment/vacancy path can retain identical closed rows:

1. Assign the same Person to the same Office with the same nullable start day.
2. Vacate with the same end day/reason, producing a closed tenure row.
3. Repeat the assignment and vacancy with the same values.

`OfficeStore.TryAssignIncumbent` appends a new open row, while `TryVacateOffice` replaces the matching open row in place and does not compare the resulting closed row against earlier history (`Assets/_Project/Scripts/Institution/InstitutionStores.cs`, `TryVacateOffice`, lines 412–438). The public `SimulationRuntime.TryAssignIncumbent` accepts this supported sequence and delegates successful writes to the owner. Thus two equal closed values can exist as separate retained list entries, distinguished by cardinality and mutation-order occurrence. The store has no tenure identity that would make those occurrences interchangeable or removable.

Rejecting them would make an otherwise valid live owner impossible to export and restore, violating the exact continuation requirement. The design should preserve every tenure occurrence, including equal-valued rows, in exact mutation order. Keep duplicate rejection only where an individual domain operation already enforces it; do not elevate the historical-import helper's behavior into a global invariant. Add a focused regression that creates equal rows through supported assignment/vacancy operations and proves exact count/order survive capture and private staging.

## Required disposition

Revise the design's duplicate-tenure rule and corresponding validation/test requirements, then obtain a fresh independent design review. No implementation should begin from this candidate until that correction is reviewed. No other architecture or product decision is needed.
