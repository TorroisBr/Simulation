# P12-E Property/Estate Owner Snapshot — Independent Exact-Tip Implementation Review

**Verdict:** `NEEDS_CHANGES` — do not promote this candidate. The candidate remains unchanged; this record documents a rejection-path defect and required validation gaps.

## Exact revisions and provenance

- **P12 canonical base:** `77135b3e0ca8df83c6852f2c234ff9098a833468`
- **Current remote P12 canonical at review:** `77135b3e0ca8df83c6852f2c234ff9098a833468`
- **Candidate branch/tip:** `codex/phase12/P12EPropertyEstateOwnerSnapshotIntegration` at `62bfd8884e2c87271485f49a5d4b87d9369eddd5`
- **Candidate parent:** `77135b3e0ca8df83c6852f2c234ff9098a833468` (direct parent; clean fast-forward ancestry)
- **Candidate Git tree:** `e435599e823c80a05c1ec55c42179d84423d4241`
- **Candidate Assets tree:** `43fa2a62701efc7f9a69edc3f727dc2ca454ec20`
- **Architecture authority:** `47eff220c7ce00f6e7c759bdc2b76780bb46f628`
- **Accepted checkpoint contract:** `docs/phases/PHASE12_BRIEF.md` at the current P12 canonical base; accepted scope is exact detached export and private staged hydration for configured core/daily owners, dependent on P12-B/P12-C and P12-D roots where needed.
- **Technical design:** `codex/phase12/P12EPropertyEstateSnapshotDesign` at `224eaff53fa5bdddbe4a67aa6a8559aea5499c21`.
- **Immutable independent design review:** `codex/review/phase12/P12EPropertyEstateOwnerSnapshotDesign224EAFF` at `b53b3021cd106b108f1913178f426473ca4050db`, artifact `docs/design/PHASE12_P12E_PROPERTY_ESTATE_OWNER_SNAPSHOT_DESIGN_REVIEW_224EAFF.md`, verdict `VALIDATED_CANDIDATE`.

The candidate branch ref still resolves to the reviewed candidate tip, and its direct parent is the current P12 canonical. No candidate code or tree was changed during this review.

## Reviewed scope and positive findings

The complete candidate diff adds the detached three-section Property/Estate snapshot, internal exact-revision factories in the two owned stores, focused tests, a bounded implementation handoff, and validation evidence. It does not modify runtime/bootstrap, profile admission, P12-B token/vector semantics, the P12-E coordinator, P12-G publication, or unrelated owner groups.

The implementation preserves separate ownership, transfer-history, and Estate sections; clones typed IDs; retains all history and its deterministic order; binds both staged stores to the supplied P12-D `PersonStore`; enforces unique deceased Person per Estate; and returns neither output until both private factories succeed. The candidate stays within the accepted owner-snapshot scope and makes no readiness or completion claim.

## Required changes

### 1. Null transfer-history rows can throw during rejection

In `Assets/_Project/Scripts/PropertyEstateOwnerSnapshot.cs`, `TryStage` orders transfer rows at lines 358–367 before validating rows at lines 371–379. `CompareHistory` at lines 494–500 uses null-conditional access for the PropertyId comparison but then dereferences `left.TransferAbsoluteDay`. The snapshot constructor preserves null rows. Consequently, two consecutive null transfer-history rows reach the comparator and throw `NullReferenceException` instead of returning a typed failure. Validate every row before ordering (or make the comparator safely handle null) and add a regression proving malformed input returns failure with both staged outputs null and no owner changes.

### 2. Required malformed-snapshot coverage is incomplete

`PropertyEstateOwnerSnapshotTests` contains six tests. The reviewed design’s implementation validation contract in §4 and §6 requires additional staged/capture rejection cases that are not exercised by this suite. In particular, add focused cases for:

- null rows, malformed/empty IDs, duplicate PropertyIds and duplicate EstateIds;
- unsupported schema and malformed counts/revisions;
- missing current-owner Person, transfer-history Person, Estate deceased Person, and death facts;
- invalid/negative transfer and Estate days, Estate opening before death, and opening after the saved day;
- owner-identity and component-stamp mismatches in the required census vector;
- failed staging leaving both outputs null and source/active owner rows and revisions unchanged.

Existing store, succession, and mutation-epoch regression suites cover domain behavior, but they do not replace these snapshot DTO/factory rejection tests. The independent design review explicitly called for these malformed-input and atomic-pair proofs.

## Validation evidence checked

The candidate’s `docs/validation/P12EPropertyEstateOwnerSnapshot/VALIDATION.md` binds validation to the exact Assets tree `43fa2a62701efc7f9a69edc3f727dc2ca454ec20`, the same tree as this candidate. I verified all entries in its `SHA256SUMS.txt` against the committed artifacts and parsed the committed XML results:

- Property/Estate snapshot 6/6; Property census 2/2; Estate census 1/1;
- Estate/property foundation 9/9; Property transfer foundation 5/5; succession 21/21;
- Property/Estate mutation epoch 5/5; Person snapshot regression 5/5;
- ALL EditMode 2641/2641; official Smoke 5/5.

`git diff --check` from the exact current canonical base to the exact candidate tip passes. These green runs do not cover the missing required cases above and do not override the null-row defect. No Unity tests were rerun during this read-only review.

## Disposition

Keep the candidate and its existing evidence. Add the null-row fix and required focused coverage on an additive candidate tip, rerun the required validation on the resulting Assets tree, and request a fresh independent exact-tip review. This review record does not authorize canonical promotion and does not change P12-E, P12-A, P12-B, or P13 status.