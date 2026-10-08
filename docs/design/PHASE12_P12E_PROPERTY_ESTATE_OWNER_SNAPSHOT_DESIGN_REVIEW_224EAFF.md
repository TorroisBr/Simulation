# P12-E Property/Estate Owner Snapshot Design — Independent Exact-Content Review

**Verdict:** `VALIDATED_CANDIDATE` — the bounded technical design passes independent review. Implementation remains conditional on the documented named owner-hotspot handoff.

**Review date:** 2026-10-08  
**P12 canonical base:** `f5d99cb7008023d14a0ed16ea2149a7d7c18def1`  
**Current remote P12 canonical at review:** `f5d99cb7008023d14a0ed16ea2149a7d7c18def1`  
**Architecture authority:** `codex/architecture/world-identity-projection` at `47eff220c7ce00f6e7c759bdc2b76780bb46f628`  
**Candidate branch/tip:** `codex/phase12/P12EPropertyEstateSnapshotDesign` at `224eaff53fa5bdddbe4a67aa6a8559aea5499c21`  
**Candidate Git tree:** `5a5df5321969517864e2e149721befdb8bd0691a`  
**Reviewed artifact:** `docs/design/PHASE12_P12E_PROPERTY_ESTATE_OWNER_SNAPSHOT_DESIGN.md`

## Review scope and repository evidence

The candidate is a clean fast-forward descendant of the named P12 canonical base. Its complete diff from that base adds only the bounded P12-E technical-design document. The correction from the prior design tip adds explicit Estate deceased-Person uniqueness, cross-row staging rejection, and focused negative-test obligations; it does not alter implementation or product scope.

The design was checked against the current architecture, P12 Brief/State, accepted P12-E owner-export scope, and current canonical source at `f5d99cb`:

- `EstateStore.TryRegister` rejects duplicate `EstateId` and maintains `recordsByDeceasedPerson`, rejecting a second Estate for the same `PersonId` with `EstateAlreadyExistsForPerson`. The source therefore establishes the one-Estate-per-deceased-Person invariant.
- `EstateOpeningSystem` requires the exact world `PersonStore`, a registered Person with a factual death day, and an opening day no earlier than death. `SimulationRuntime.CloneEstateStore` also rejects an opening day later than the current world day. The design preserves these facts and rebuilds the derived deceased-Person index instead of serializing it as independent truth.
- `PropertyOwnershipStore` exposes deterministically sorted ownership and transfer-history views. Current owners must resolve to the bound Person root. Runtime cloning verifies each history Property exists, both historical owners resolve to the same Person root, transfer time is not after the world day, and the reconstructed revision equals the source revision.
- `PropertyOwnershipTransferHistoryRecord.Compare` orders by PropertyId, transfer day, previous PersonId, then new PersonId. The design preserves that ordering and all retained rows; it explicitly does not collapse, deduplicate, or infer history/current-owner continuity.
- The selected Daily-v1 P12-B vector already has separate Required schema-v1 ownership, transfer-history, and Estate witnesses. Existing P12-B operation/invalidation wiring covers registration, transfer/succession, and explicit Estate opening. The design consumes those existing identities, revisions, and completed-boundary token without adding a second epoch or changing admission semantics.

## Findings

1. **Estate cardinality is preserved.** The design requires unique deceased-Person IDs across `p12e.estate.records`, in addition to unique Estate IDs. It explicitly identifies this as the invariant enforced by `EstateStore.TryRegister`.
2. **The required negative stage case is explicit.** It requires a malformed staged value with two distinct Estate IDs referencing the same deceased Person to fail before either owner candidate is returned, leaving source and active-runtime owner state/revisions unchanged.
3. **Other owner invariants are adequately bounded.** The three sections retain their separate identity, schema, row count, and local revision. Both Property sections share one exact owner/revision. Property owners, history Property/Person references, Estate deceased Persons, death facts, and logical-day bounds are validated against the same staged P12-D Person root and saved day. Malformed or inconsistent input yields neither staged owner.
4. **No unsupported relation is invented.** The design does not create Estate-to-Property membership. A deceased Person may independently appear in Estate and current-ownership facts; the runtime succession precondition remains a runtime rule and is not replayed or inferred during hydration.
5. **The reconstruction boundary matches current architecture.** Detached values, stable semantic IDs, owner-local revision preservation, rebuilt derived indexes, private staging, and a later P12-G whole-graph/publication boundary are consistent with architecture §§91–92A. The design does not claim that snapshots are primary truth or that save/load, fork, or profile-wide continuation is delivered.
6. **Scope is controlled.** The proposal adds no gameplay behavior, persistence envelope, ID allocation, mutation-epoch wiring, capture protocol, runtime/bootstrap registration, publication, or P12-A/P13 readiness. It stays within the accepted P12-E exact owner export/private staged reconstruction scope and does not widen Daily-v1.
7. **Concurrency constraints are correctly recorded.** The two stores and their focused tests are the implementation hotspot. The design requires an owner handoff before those files are edited and defers shared P12-E coordinator composition to its serialized integration boundary.

No unresolved product or canonical-architecture decision was found. No implementation test gap blocks this documentation-only review: implementation validation is specified but was not run here. The candidate diff passes `git diff --check`; Unity tests were not run because the candidate changes documentation only.

## Disposition

The design may proceed to bounded implementation only after the documented Property/Estate store hotspot is explicitly handed off. Implementation must include the same-Person/different-Estate negative staging test and the listed exact-owner, rejection, regression, and validation coverage. This review does not approve code promotion, P12-A implementation/readiness, P12-E completion, P13 readiness, or Phase 12 closure.