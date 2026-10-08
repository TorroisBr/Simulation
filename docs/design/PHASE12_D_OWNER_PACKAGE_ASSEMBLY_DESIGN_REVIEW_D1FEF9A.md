# P12-D owner-package assembly design review — exact candidate D1FEF9A

**Review ID:** P12D-OWNER-PACKAGE-ASSEMBLY-DESIGN-EXACT-TIP-D1FEF9A-R1
**Outcome:** `VALIDATED_CANDIDATE` — PASS for the bounded P12-D Daily-v1 private owner-package and D-graph assembly design. This is a technical-design review only; it does not validate implementation, promote a capability, close P12-D, make P12-A ready, or authorize whole-profile publication.

## Exact reviewed revision

- Candidate branch/ref: `codex/phase12/P12DOwnerPackageAssemblyCurrentBaseRevalidationA4CE`.
- Candidate commit: `d1fef9acf95f40c918784b471e7b0c7191f89905`; commit tree: `9c7fc0d4734f371be1ecfebbad114820f95439df`.
- P12 canonical base/current canonical: `a4ce0abcf261226f4b52fbacc8df9ef8f67a0de2`.
- Direct remote-ref check returned the candidate branch at the exact reviewed commit. The candidate is one clean commit ahead of the base, with no behind commits.
- Candidate diff contains only `docs/design/PHASE12_D_OWNER_PACKAGE_ASSEMBLY_CURRENT_BASE_REVALIDATION_A4CE.md`, blob `9a1f3fc79c45b913c6b97cd7f15baac58b0b6cea` (99 added lines). No code, tests, ProjectSettings, or `.meta` files changed. Exact cumulative `git diff --check` passes; the fetched file has no trailing-whitespace lines.
- Current architecture remote: `codex/phase8/canonical` at `470667d37863384edadb3d93ef64d8004aff46a3`; current Phase 8 State blob `67f8128dba962b6f21703fd07d9e619d8ce397ea`. That State confirms `c285466c355103d3637ac165246591b72eb7bda0` is still the canonical architecture baseline. The architecture document blob is `4a3c73c4428ba7bc43c28f617e243e4cd54078fa`, matching the addendum citation. The later Phase 8 canonical commits do not supersede the cited architecture contract.
- Other current authority blobs: P12 Brief `31d9e1b4df41fc994f0a747274e35c4ef40b6e3a`; P12 State `700a1fad188049f23819c0beae23ea4e34e1ad1f`; D technical design `a6f72aabc26057b46ec8896738c1006c016d880e`; G technical design `f19b3a316627c4a0842a2b740c776e6f71f6d68e`.
- Relevant current code blobs: `P12DNpcRootOwnerSnapshot.cs` `58c44f1b1a0ff1e3ec2a6b7eef3e7ba108067a32`; `P12DCityRootOwnerSnapshot.cs` `ce852ade6bd40ab93611065e1433871f76190fa0`; `PersonStore.cs` `387a766182ca1c5eb3647be616ce2834d9fd81eb`.

## Findings

1. **The D/F boundary does not introduce a P12-D ↔ P12-F cycle.** The P12 Brief assigns D prerequisites B/C, F prerequisites C/D/E, and G prerequisites B–F plus the live-profile inventory. Current P12 State records the partial D/F NPC capture/merge/private reconstruction as already promoted within P12-D, while P12-F still waits for C/D/E. The addendum consumes that already-promoted single-concrete-NPC seam for D relation checks; it expressly preserves disjoint D/F field ownership and says D neither absorbs nor duplicates F-owned fields. It does not depend on completion of the P12-F checkpoint or include F's remaining stores/sections. G remains the later owner of complete B–F composition, whole-profile validation, and the one publication. Thus no reverse dependency is added and the documented DAG remains intact.

2. **Person-before-NPC staging matches the promoted APIs and closes the reciprocal/orphan gap.** `PersonStore.TryCreateFromOwnerSnapshot` privately reconstructs exact Person rows and its derived materialization index without requiring live NPCs; its code explicitly defers cross-owner checks to the later D graph validator. `P12DNpcRootOwnerSnapshot.TryStage` requires that staged PersonStore and City linkers, then checks NPC-to-Person identity and indexed binding. The addendum correctly stages PersonStore first and calls for both-direction validation after the full NPC roster exists, including orphan materialized Person rows. It does not add a PersonStore dependency on NPC construction or change Person identity semantics.

3. **City ownership is reconciled without changing scope.** The current D design assigns selected Daily-v1 City, Market, custody/account, stock, population, receipts/revisions, and ordered NPC-membership facts to D; E contributes provider-owned state that writes those D roots. The addendum applies that contract and identifies the G “E City” wording as stale design text, not a new owner contract. No second City projection or combined-profile behavior is implied.

4. **The bounded package interface preserves boundary identity and owner truth.** It reuses the P12-B completed-boundary token and its exact `OwnerSections` vector identity, the transient D/F capture stamp, existing immutable owner snapshots/factories, and source-local revisions/receipts. It specifies stale-boundary rechecks and does not create a new lock, epoch, aggregate revision, or serialization API. The package copies owner values; census evidence is not used as a replacement for those values.

5. **Private staging and failure behavior are appropriately bounded.** The proposed order consumes B/C roots, stages legacy spatial facts and PersonStore, constructs each D City once, stages the already-merged NPCs once, resolves ordered City membership only after complete relation checks, and validates the complete D graph before returning an opaque unpublished package. A later failure discards the private candidate; source owners, active runtime, and mutation guard remain unchanged. It does not call gameplay writers, replay receipts, publish runtime state, or take over G’s whole-profile guard binding/publication.

6. **Owner cardinality, exclusions, and G handoff remain honest.** The plan retains the exact-zero `p12d.explorable-sites` owner witness and typed Daily-v1 `NOT_COMPOSED` LocalTopology boundary. It rejects dangling, duplicate, cross-owner, nonreciprocal, and orphan relations while avoiding unsupported global cardinality rules. It excludes P10 rows/LocalTopology, P18 receipt payloads, P12-A readiness, P13, whole-profile B–F validation, runtime/bootstrap publication, and continuation-parity claims.

7. **Validation is a plan, not a claimed result.** The proposed focused cases cover token/vector/stamp identity, stale boundary, dormant/materialized/NPC-only actors, reciprocal City/NPC/Person/Genealogy/spatial links, ordered membership/revisions, exact-empty site admission, and no partial publication/source mutation. Relevant promoted D regressions, ALL EditMode, official Smoke, and `git diff --check` remain implementation-time obligations. This documentation-only review ran no Unity tests and makes no implementation-validation claim.

No unresolved product or canonical-architecture decision remains. The two corrections are grounded in the accepted P12-D contract, the current APIs, and the current Phase DAG.

## Readiness and limits

**Bounded P12-D Daily-v1 private owner-package and D-graph assembly: READY_FOR_IMPLEMENTATION after this independent design review.** Implementation must preserve the reviewed boundary and use a serialized integration window for the shared City/NPC/Person assembly hotspot. The partial D/F NPC adapter is an existing promoted dependency inside D; the P12-F checkpoint remains downstream and is not a prerequisite for this slice.

P12-D remains open until its other accepted owners and integration evidence are complete. P12-F and P12-G keep their explicit dependencies. P12-A remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`. This record does not authorize P12-A implementation, whole-profile runtime publication, phase closure, or canonical promotion.

## Review method

The review compared the full one-file candidate diff against the exact base, reread current AGENTS/Execution Model/Brief/State/D/G contracts, checked the published architecture and State refs, inspected the current City/NPC/Person staging APIs, verified the remote candidate ref and blob, and ran `git diff --check` on the exact candidate range. No candidate files were changed and no heavy tests were run.
