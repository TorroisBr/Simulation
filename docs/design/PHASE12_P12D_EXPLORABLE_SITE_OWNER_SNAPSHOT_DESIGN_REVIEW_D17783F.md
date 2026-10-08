# P12-D ExplorableSite Owner Snapshot Design Review — d17783f

**Outcome:** `VALIDATED_CANDIDATE` — PASS; `READY_FOR_IMPLEMENTATION` for the isolated `ExplorableSiteStore` snapshot and private staged-factory slice only.

This review resolves the proposal's four listed design questions from the exact current owner, P12-D contract, P12-B/C boundaries, and current architecture. It does not review or authorize P12-D integration, a populated Daily-v1 profile, P10 LocalTopology, P12-A, or Phase closure.

## Exact reviewed content and baseline

- Proposal branch: `codex/phase12/P12DExplorableSiteOwnerSnapshotDesign`
- Proposal commit: `d17783f02ab5f30107db7c109a57cbeb56bc40e1`
- Proposal tree: `265d0bdba17174b8c14f7fabe7f64c36e461ba54`
- Reviewed proposal blob: `992cc93e79dbd427d31526ace7ffbff82f15f775`
- P12 canonical base and current remote tip at review: `da2a73896bc405ae6f11c536a5fbe8d471b00c21`
- Architecture canonical: `47eff220c7ce00f6e7c759bdc2b76780bb46f628`
- Proposal diff from base: one added documentation file, `docs/design/PHASE12_P12D_EXPLORABLE_SITE_OWNER_SNAPSHOT_DESIGN.md`; no code changes.
- The candidate branch and proposal file were unchanged during review. `git diff --check` passed. No Unity tests were run because this is a documentation-only design review.

## Findings

1. **Daily-v1 exact-empty admission is preserved.** The current P12 State marks `p12d.explorable-sites` required-empty and `p12c.runtime-identities.explorable-sites` explicitly empty for `UnityBootstrap-Daily-v1`. The proposal keeps both independent checks bound to their exact owners and the P12-B completed-boundary evidence. A populated generic site snapshot cannot satisfy or override either admission rule. The current SampleScene/Daily-v1 path remains the dedicated P9-B profile; P10-A and P10-B Ruin/LocalTopology remain separate. No profile configuration or admitted owner set changes.

2. **The owner boundary matches the D contract.** P12-D assigns site runtime identity, site-instance identity, definition identity, exact legacy-location reference, and `ExplorableSiteStore` order to the factual owner. The isolated factory rebuilds only the store's ordered collection and RuntimeId index. It does not synthesize P8 anchors, P10 topology, or Knowledge, and leaves the staged RuntimeIdentityRegistry handoff to the enclosing D stage. This matches the current D design's required single-owner staging and excluded-profile rules.

3. **Site-instance identity decision.** Preserve `SiteInstanceId` exactly and require it to be non-empty and unique among rows in one staged `ExplorableSiteStore`. Architecture `47eff220` states that distinct factual site instances have their own stable identity, distinct from definition/archetype and Location. Two rows with the same `SiteInstanceId` would alias that identity even if their RuntimeIds differ. Multiple sites may still share a definition or legacy Location; no uniqueness is imposed on those values. `ExplorableSiteStore.Add` currently checks only RuntimeId, so the private restore factory must enforce this snapshot/graph invariant without changing gameplay `Add` behavior. Add an explicit duplicate-SiteInstanceId rejection test.

4. **Revision validity is derivable from the current owner.** At a stable capture boundary, schema-v1 `Revision` must be nonnegative and exactly equal the number of site rows. `ExplorableSiteStore` starts at zero; each successful `AddCore` increments once after both indexes are published; its exception compensation reverses that increment; `RollbackGenesisSite` removes the last site and decrements once. There is no committed removal path that creates revision gaps. Thus `Revision == rowCount` is the reachable-state rule; restore that value directly after validation rather than replaying `Add`. Daily-v1's empty owner consequently has revision zero. Cover negative and mismatched revisions in the focused tests.

5. **Merged P12-C registry handoff is a later D-stage operation with an existing seam.** The isolated site factory must not access or mutate `RuntimeIdentityRegistry`. In the enclosing unpublished D stage, construct each `ExplorableSiteRuntime` once, place those exact instances in the staged site store, then register those same references in the already-staged P12-C registry through `RegisterExplorableSite` after the required legacy Locations are staged. Verify the registry lookup returns the same object reference for each RuntimeId and that owner/registry cardinalities agree. A cross-kind RuntimeId collision or any failed registration rejects the entire unpublished D package; the active runtime is never incrementally changed. Keep cross-owner collision and concordance tests at that merged integration boundary. This does not imply that the current P12-C Daily-v1 empty site section may become populated.

6. **Definition resolution uses the already-admitted content set.** The site snapshot stores only `DefinitionId`, not a Unity asset reference or a new definition version. The enclosing composition supplies the exact `ExplorableSiteData` definitions admitted for the selected profile/build. The owner factory must resolve each row by ordinal `DefinitionId` against that supplied set and require exactly one compatible match; missing, null, incompatible, or duplicate-ID matches fail closed. The existing P12-B/B-C profile compatibility contract remains responsible for the compatible build/content identity. This adds no definition-versioning or content-identity scheme. The accepted Daily-v1 set contains no admitted site rows, so its resolver is not used for a populated site; another profile must explicitly admit both site sections and its definitions before using populated snapshots.

## Readiness and limits

The isolated owner slice is implementation-ready: immutable detached schema-v1 rows; exact ordered capture; exact local revision; private reconstruction from already-staged legacy locations and the exact admitted definition set; and owner-local rejection for malformed rows, duplicate RuntimeIds/SiteInstanceIds, invalid revisions, missing references, and ambiguous definitions. Add the duplicate SiteInstanceId, revision equality, and ambiguous-definition tests described above to the proposal's focused suite.

The later D integration still owns P12-B token/vector binding, the P12-C registry handoff and cross-type collision tests, cross-owner validation, and publication. The readiness verdict grants no changes to `SimulationRuntime`, bootstrap/admission, P12-B/C semantics, P8/P10 owners, or profile configuration. In particular it does not implement P10 LocalTopology, make P10 state capturable under Daily-v1, claim P12-D completion, make P12-A ready, or establish P13 readiness.

P12-D remains open; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked. The current P12-B/C promotions and exact-empty Daily-v1 admission remain authoritative.
