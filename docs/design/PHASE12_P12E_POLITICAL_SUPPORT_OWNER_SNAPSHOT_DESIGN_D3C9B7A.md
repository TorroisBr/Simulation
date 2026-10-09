# P12-E PoliticalSupport Owner Snapshot Design

**Status:** Current-base technical-design candidate; implementation has not started.

**Checkpoint:** P12-E — Profile-selected core and official daily-domain owners.
**Canonical base:** `codex/phase12/canonical` at `d3c9b7a4a17274f1347352b3b35613c3987e30ee`.
**Accepted scope:** P12-E export and private staged hydration for configured Daily-v1 authorities, as recorded in `docs/design/PHASE12_CAPABILITY_CHECKPOINT_DECOMPOSITION.md` and `docs/design/PHASE12_E_TECHNICAL_DESIGN.md`.

## Contract boundary

`PoliticalSupportStore` is an E-owned relation authority. Architecture §36 and the current owner implementation define its source/target kinds, Support/Oppose disposition, active/ended lifecycle, and one active relation per typed source-target pair. This design stores those existing facts only. It adds no producer, gameplay, relationship meaning, decision policy, public API, or profile scope.

The P12-B PoliticalSupport census and owner-commit operation are already present in canonical code. This design consumes their existing Required section and completed-boundary token; it does not alter the section ID/schema, owner-thread admission, mutation protocol, invalidation, operation registration, or runtime operation receipts.

The current selected Daily-v1 runtime installs its own `PoliticalSupportStore` clone against its installed `PersonStore`, `FactionStore`, and `PoliticalClaimStore`. The P12-B census provider identifies that exact clone at section `p12e.political-support.relations`, schema 1, with `Count` and the store-local `Revision`. The selected bootstrap begins with zero rows and revision zero, but supported runtime commits can populate it later.

## Exact immutable payload

Export exactly one Required schema-v1 section:

| Field | Contract |
|---|---|
| Section identity | `PoliticalSupportStoreCensusProvider.RelationsSectionId` (`p12e.political-support.relations`) |
| Owner identity | Exact installed `PoliticalSupportStore` from that section's witness |
| Cardinality | `PoliticalSupportStore.Count`, including active and ended rows |
| Revision | Exact nonnegative `PoliticalSupportStore.Revision`; preserve it directly, never derive it from row count |
| Rows | Every immutable record in `PoliticalSupportStore.Records`, copied into detached snapshot values |

Each row preserves exactly:

- `RelationId.Value`;
- source kind (`Person` or `Faction`) and the matching typed ID value;
- target kind (`PoliticalClaim` or `SuccessionCandidate`) and the matching typed ID value;
- disposition (`Support` or `Oppose`);
- `StartedAbsoluteDay`;
- nullable `EndedAbsoluteDay`.

`IsActive` is derived from `EndedAbsoluteDay == null`; it is not an independent field. Do not serialize `activeByPair`, `OwnerToken`, mutation guards, transitions, proposal objects, or derived indexes. Do not allocate/recompute relation IDs or revisions during capture or hydration.

The enclosing snapshot also retains `CapturedAbsoluteDay = token.AbsoluteDay` as completed-boundary metadata. This is not an owner row or a new census section. `TryCapture` copies it from the exact P12-B token; no later clock read may replace it.

Capture emits rows in the exact existing `PoliticalSupportStore.CompareRecords` order: source kind, source ID ordinal, target kind, target ID ordinal, disposition, start day, then relation ID ordinal. Preserve ended rows and history order as returned by the owner. A later re-add is a separate row with a new caller-provided relation ID; an existing ID is never reusable. Multiple ended rows for a pair are valid, while at most one row for that typed pair may be active regardless of disposition.

## Capture boundary

`P12EPoliticalSupportOwnerSnapshot.TryCapture` accepts the admitted `SimulationRuntime`, exact `DailyCaptureEligibilityToken`, and the token's exact owner-section vector. It requires reference equality between the vector and `token.OwnerSections`, then finds exactly one matching Required schema-v1 witness. The witness must identify a `PoliticalSupportStore` and match its current `Count` and local `Revision`.

Copy every row to an immutable detached DTO. Validate non-null/valid identities and enums, typed union shape, and timeline bounds against the token's absolute day. The current runtime clone path already rejects future support starts/ends; snapshot staging must preserve that same boundary: start day is nonnegative and no later than the captured day; an end day, if present, is between start and captured day. Do not add extra policy beyond existing owner/domain validation.

After the copy, recheck the exact owner witness, count, revision, and completed-boundary token. On any mismatch or malformed owner row, return failure with no snapshot. The token remains P12-B-owned; this snapshot does not create capture eligibility or another lock.

## Validation and private staging

Validate the detached section before constructing any candidate:

- exact section ID and schema version;
- nonnegative revision, including `long.MaxValue`, preserved verbatim;
- declared cardinality equals the row count;
- each relation ID is nonempty and unique under ordinal comparison;
- source and target tags are defined and match exactly one nonempty typed ID;
- disposition is a defined enum value;
- start/end days satisfy the existing owner and captured-day bounds;
- a Person source and succession-candidate target resolve against the exact staged `PersonStore`;
- a Faction source resolves against the exact staged `FactionStore`;
- a PoliticalClaim target resolves against the exact staged `PoliticalClaimStore`;
- no duplicate active typed source-target pair exists. Ended history rows remain allowed.

The source/target pair must use the store's existing length-prefixed typed `PairKey` semantics, so values containing separators cannot alias. Preserve exact typed distinctions even when two IDs have the same text. Rebuild `activeByPair` from active rows using the existing owner logic; it remains derived state.

`TryStage` invokes validation using the snapshot's retained `CapturedAbsoluteDay`; it does not accept or consult a current runtime day. The staged validator rejects any row outside that captured boundary even if the runtime later advances.

Add one private unpublished factory in `PoliticalSupportStore`, analogous to the existing P12-E factories in `PoliticalClaimStore` and `FactionStore`. It receives the exact staged Person/Faction/PoliticalClaim roots, validated relation rows, and captured revision, constructs a fresh private candidate, preserves the revision exactly, and rebuilds the derived active-pair index. It must not bind/publish the candidate into a live runtime. Any failure returns `false` and a null staged owner; the three staged roots and current runtime remain unchanged.

Staging is ordered after staged Person, Faction, and PoliticalClaim roots. Faction and PoliticalClaim may be staged independently once their own dependencies are staged; PoliticalSupport validates against both outputs. Whole-graph publication and cross-owner integration remain P12-G responsibilities.

## Validation plan

The focused `P12EPoliticalSupportOwnerSnapshotTests` suite should cover:

1. valid empty Daily-v1 capture at the exact completed boundary and preservation of the required zero-row/zero-revision section;
2. all four typed source/target combinations, both dispositions, active and ended rows, exact value fidelity, detached copies, deterministic ordering, and repeated capture equality;
3. exact identity and revision preservation where revision differs from row count, including an ended-row replacement and terminal-history/re-add sequence;
4. successful staged round trip against the exact staged roots, rebuilding only the active-pair index;
5. dangling Person, Faction, and PoliticalClaim references; wrong tags; malformed IDs/enums/dates; duplicate IDs; duplicate active pairs; cardinality/schema/revision errors; and late-failure all-or-nothing behavior;
6. separator-containing source/target IDs that would expose an ambiguous concatenated key, plus same-text values under distinct typed kinds;
7. failed capture for missing/duplicate/wrong-role/wrong-schema/stale witnesses, a changed owner revision/cardinality, and an invalidated completed-boundary token.
8. the stored capture day is exact, staging uses it, and a later runtime day cannot admit future-to-capture relation dates.

Keep the existing `PoliticalSupportFoundationTests`, `P12PoliticalSupportCensusTests`, and relevant PoliticalClaim/Faction staged-owner tests. On the final code tree run the focused owner and affected regression suites, ALL EditMode, official Smoke, and `git diff --check`; retain XML/log/hash artifacts. Existing P12-B census evidence remains evidence for its existing protocol only.

## Ownership, integration, and exclusions

Implementation owns `PoliticalSupportStore.cs` for the private reconstruction factory, one new `P12EPoliticalSupportOwnerSnapshot.cs` plus `.meta`, and its focused EditMode suite plus `.meta`. It does not own `SimulationRuntime.cs`, bootstrap composition, the existing census provider, P12-B operation/census wiring, Faction/PoliticalClaim snapshot files, or shared persistence orchestration. No other active candidate should edit the PoliticalSupport owner while this slice is implemented and reviewed.

This slice does not capture or reconstruct `SimulationRuntime.PoliticalWorldRevision`; that aggregate remains a separately inventoried runtime-level concern and is not substituted by the PoliticalSupport local revision. It also does not claim complete E-owner coverage, global quiescence, profile-wide capture eligibility, P12-G publication, P12-A readiness, P13 readiness, or Phase 12 closure. P10/P14/P18/P19/P20 scopes remain untouched.

## Readiness

The P12-E scope and prerequisite implementation authorization are accepted. A current-base independent design review must pass before implementation starts. This design is not implementation approval or canonical promotion and does not alter any Phase status.