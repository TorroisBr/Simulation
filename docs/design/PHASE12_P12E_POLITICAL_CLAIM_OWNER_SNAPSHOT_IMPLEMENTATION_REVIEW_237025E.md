# P12-E PoliticalClaim Owner Snapshot Implementation Review

**Outcome:** `NEEDS_CHANGES` — exact-tip independent review. The implementation appears consistent with the reviewed owner design on static inspection, but required capture and malformed-input coverage is absent from the candidate's tests. This is a validation-coverage finding, not a demonstrated data-copy defect.

## Exact candidate and evidence

- Candidate branch: `codex/phase12/P12EPoliticalClaimOwnerSnapshotImplementation`
- Candidate tip: `237025e58cd3d1100d4c2d4a525e364ffa343407`
- Candidate Git tree: `7f5338bd03f3057a4b4326f578f62d16c56e3c8e`
- Candidate `Assets` tree: `24f284dfe154f1d3d2d1cc1313135d03e1585a65`
- Current P12 canonical/base: `a768f2d9eca161f5cff059a782737412f43b2861`, tree `7ba0866058bc5618473237b4af6322d964352961`
- Candidate is a clean descendant of the current canonical. The candidate's implementation parent is `27fe695cf1b52d4e51634bcf6dc37e4e57dfd7ad`.
- Reviewed current-base owner design: `94ce311d6c035bd89d7bf7754cab3cac3e373e84`; its independent exact-content design review is `c21670f5e920dac79bb23d7cae116c7c1db1e8c3`.

The full base-to-candidate diff adds the owner snapshot code, one private factory in `PoliticalClaimStore`, the focused suite, design/review copies, and retained validation artifacts. It does not alter the two census IDs or P12-B writer wiring. `git diff --check` passes.

The validation manifest's 24 artifact SHA-256 values all match the retained files. The XML results match the manifest: PoliticalClaim owner snapshot 5/5, PoliticalClaim foundation 13/13, P12 PoliticalClaim census 6/6, PoliticalSupport foundation 6/6, PoliticalLegitimacyDecision foundation 8/8, Institution/Office owner snapshot 11/11, Property/Estate owner snapshot 11/11, Person owner snapshot 5/5, Bootstrap composition 26/26, P12-C private root composition 51/51, ALL EditMode 2667/2667, and official Smoke 5/5. Unity was not rerun, per review instructions. The exact `Assets` tree matches the validation tree.

## Findings

### [P1] The actual capture/export path has no test coverage

`P12EPoliticalClaimOwnerSnapshot.TryCapture` is implemented at `Assets/_Project/Scripts/P12EPoliticalClaimOwnerSnapshot.cs:114`, but the candidate's dedicated test file contains no invocation of this method; the only five test cases exercise manually constructed snapshots through `TryStage`. A search across the candidate's EditMode tests finds no other reference to this snapshot type. Therefore the tests do not exercise selected-profile/capture-token admission, duplicate/missing owner-section witnesses, exact same-owner/schema/role/cardinality/revision matching, stale completed-boundary rejection, revision-bracket failure, or detached deterministic export. These are central P12-E capture obligations in the reviewed design, not incidental branches. Add focused runtime-capture tests covering the valid path and representative stale/malformed witness failures, plus deterministic detached output, before this candidate can be considered fully validated.

### [P1] The required staged-rejection matrix is materially incomplete

The reviewed design's §6 requires root-reference, malformed-row, identity/cardinality, and history rejection evidence. The five tests cover empty staging, one populated example, duplicate recognition and terminal-history mismatch, max revision, and missing section/schema/revision disagreement. They do not test missing claimant or typed target roots (Person, Institution, Office, Property), a missing recognition claim/Institution, duplicate claim IDs, invalid recognition identity, invalid claim/recognition enum or date/status values, malformed evidence, nonchronological/null history rows, or section row-count mismatch. The implementation has checks for many of these cases, but the acceptance contract requires focused evidence for fail-closed reconstruction. Add the required targeted cases and assert failed staging returns no candidate while source/runtime roots and revisions remain unchanged.

### [P2] The populated round-trip test does not assert every retained claim field

`ClaimsAndRecognitionStageEveryValueAndDoNotRecomputeRevisionFromCounts` stages all four target kinds, but its assertions verify only a subset of the retained claim values. It does not assert each claim's exact ID, claimant, target ID, ClaimType, Basis, description, creation day, status, and full optional resolution value; nor does it prove each target-kind/type mapping is preserved. The design requires exact field fidelity. Extend the assertions (or split into focused tests) so the name and evidence establish the stated contract.

The design also requires insertion-order determinism and repeated round-trip equality; neither is demonstrated by the five focused tests. Cover this alongside the capture/export tests above.

## Code review observations

Static inspection found the main data flow aligned with the reviewed contract: capture checks the selected Daily-v1 token and same exact owner witnesses, copies both sorted owner views, brackets the read with owner/revision/token revalidation, and emits detached immutable row objects. Staging validates typed references against Person, Institution/Office, and Property roots before calling the private unpublished `PoliticalClaimStore` factory; that factory preserves exact revision and builds a fresh owner. The implementation keeps claim/recognition census identity and semantics unchanged and leaves publication to P12-G.

These observations do not replace the missing test evidence above. No candidate code was edited, no Unity tests were rerun, and no canonical promotion or P12-A/P12-G readiness is implied.
