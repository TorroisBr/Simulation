# P12-E FactionStore owner snapshot contract

**Status:** Owner-specific technical-design candidate; implementation has not
started. **Base:** `codex/phase12/canonical`
`a768f2d9eca161f5cff059a782737412f43b2861`. **Architecture file blob:**
`4a3c73c4428ba7bc43c28f617e243e4cd54078fa`. **Generic P12-E design blob:**
`15aaee09d3295cff81a48e166b620c89f5156346`. **P12 Brief / State blobs:**
`31d9e1b4df41fc994f0a747274e35c4ef40b6e3a` /
`c5f259e033f3c05c9df72d9fde42e4016a257e0d`. **Owner inventory blob:**
`0757cd0c39e7c1e53ae0c0ae99fe191151ef5dc3`.

This contract narrows the accepted P12-E owner-snapshot boundary to the current
`FactionStore`, its faction definitions, and its retained affiliation-tenure
records. It does not change Faction or affiliation semantics, existing P12-B
census identities, or the accepted Daily-v1 profile. It is an owner evidence
sheet, not implementation or a phase-readiness claim. The generic P12-E design
§7 requires the fields, identities, references, revisions, all supported
writers, detached export, staged reconstruction, and rejection evidence
specified below; that generic design's review is not a review of this
owner-specific contract.

## 1. Authority, exact identity, and census coverage

`FactionStore` owns immutable `FactionRecord` definitions and immutable
`FactionAffiliationRecord` current/terminal tenures. Its primary dictionaries
are keyed by ordinal `FactionId.Value` and `FactionAffiliationId.Value`. A
third dictionary, keyed by the Faction/Person pair, is a derived index for the
single currently active tenure. The store also retains the exact `PersonStore`
reference it validates against, an ephemeral owner token used to bind
transitions to that store, the local revision, and runtime mutation/read
bindings (`Assets/_Project/Scripts/FactionStore.cs:5-47, 315-360`).

Preserve the existing required schema-v1 census IDs exactly as the snapshot
section identities:

| Section | Stable ID | Cardinality | Revision |
|---|---|---:|---|
| Faction definitions | `p12e.faction.records` | `FactionStore.Count` | `FactionStore.Revision` |
| Affiliation tenures | `p12e.faction.affiliations` | `FactionStore.AffiliationCount` | `FactionStore.Revision` |

`FactionStoreCensusProvider` attaches both witnesses to the same exact store
instance and returns its same local revision for both; schema version is 1
(`Assets/_Project/Scripts/FactionStoreCensusProviders.cs:13-44`). Both
sections are required even when empty. Empty sections must be encoded as zero
rows rather than omitted. Do not rename, merge, re-version, or replace these
IDs.

Canonical State records the existing P12-B wrapper as operation
`p12.faction.owner-commit`. Its current supported runtime ingress is
`TryRegisterFaction`, `TryApplyFactionAffiliation`, and
`TryApplyFactionAffiliationEnd`; successful writes notify both sections once
under the existing single mutation epoch and preserve the owner-local revision
(`docs/PHASE12_STATE.md:2848-2867`,
`Assets/_Project/Scripts/SimulationRuntime.cs:268, 5345-5387`). The initial
profile admission check observes empty cardinalities; that initial emptiness
does not bound the later saved owner to empty state.

## 2. Exact retained fields and cardinalities

### Faction definition rows

Exactly one row per ordinal `FactionId.Value`, with all fields from
`FactionRecord`:

- `Id.Value`;
- `DisplayName` (the record preserves the exact string; null is canonicalized
  to empty string);
- `CreatedAbsoluteDay`;
- `MembershipPolicy` (`CannotLeave`, `LeaveNoRejoin`, or `LeaveAndRejoin`);
- `ExpulsionAllowed`.

The definition is immutable after registration. Capture the enum and Boolean as
retained facts; do not infer one from current affiliation rows or substitute
defaults (`Assets/_Project/Scripts/FactionContracts.cs:50-95`).

### Affiliation-tenure rows

Each row is a separate immutable tenure identified by the exact
`AffiliationId.Value`. Preserve:

- `AffiliationId.Value`;
- `FactionId.Value` and `PersonId.Value`;
- `JoinedAbsoluteDay`;
- nullable `EndedAbsoluteDay`;
- nullable `EndReason` (`VoluntaryLeave` or `Expulsion`).

`IsActive` is derived as `EndedAbsoluteDay == null`; it is not a second stored
Boolean. Preserve nullable `EndReason` exactly. The domain constructor allows
an ended record whose end reason is absent, while forbidding an end reason on
an active record; do not manufacture a reason or derive it from the faction's
current policy (`FactionContracts.cs:97-193`).

`FactionAffiliationId.BuildStableId` encodes the faction ID, person ID,
joining day, and allocation sequence in the exact ID string. The sequence is
not a separate store field. Preserve the identity string exactly; do not
recompute it with a guessed sequence during restore. `TryProposeAdd` obtains
the next allocation sequence from the number of prior tenure rows for that
Faction/Person pair. Thus all terminal and active affiliation rows are
continuation state: deleting, collapsing, or merging old rows can change
future rejoin decisions, the next generated ID, or both
(`FactionContracts.cs:168-183`, `FactionStore.cs:109-120`,
`FactionTransitions.cs:88-125`).

Cardinality invariants:

- faction definition IDs are unique;
- affiliation IDs are unique across all current and historical rows;
- there is at most one active affiliation per `(FactionId, PersonId)` pair;
- any number of terminated rows per pair is retained, subject to the actual
  Faction policy and successful writer history. `LeaveAndRejoin` deliberately
  supports distinct historical tenures. No universal one-affiliation-per-pair
  restriction is allowed.

`FactionStore` stores history as multiple immutable affiliation rows, not as a
single overwritten relation. Its active-pair index is derived from those rows
and must be rebuilt during staging. Current canonical diagnostic validation
also rejects duplicate IDs, duplicate active pairs, missing roots, and invalid
time ranges (`FactionStore.cs:11-16, 123-149, 269-283, 307-313`,
`WorldStateInvariantValidator.cs:2281-2392`).

The active-pair dictionary currently forms its key as faction ID, U+001F, then
person ID (`FactionStore.cs:297-305`). Rebuild and validate against this same
owner behavior; reject two distinct active typed pairs that alias the internal
key. Changing the key encoding is outside this owner-snapshot contract.

## 3. Supported writers and exact revision semantics

The live selected-runtime writes are:

1. `SimulationRuntime.TryRegisterFaction` registers an immutable faction
   definition. It rejects a missing ID or a creation day after `CurrentDay`,
   then commits through the existing Faction owner operation.
2. `TryApplyFactionAffiliation` adds a tenure created by
   `TryProposeFactionAffiliation`. Proposal is read-only and allocates the
   stable affiliation ID using the pair's complete historical row count.
   The proposal enforces the active-pair and `LeaveNoRejoin` rules.
3. `TryApplyFactionAffiliationEnd` terminates the currently active tenure
   using a proposal from `TryProposeFactionAffiliationEnd` (voluntary leave)
   or `TryProposeFactionAffiliationExpulsion` (expulsion). Proposal enforces
   `CannotLeave` and `ExpulsionAllowed`; the commit records the explicit end
   day and reason.

The high-level API paths are at `SimulationRuntime.cs:6318-6361,
6363-6430, 6432-6480, 6481-6524`; transition rules are at
`FactionTransitions.cs:71-235`. The `FactionStore` source object passed to the
`SimulationRuntime` constructor is a pre-composition input that the runtime
clones. Its public registration methods support constructing that detached
input; they are not a second live mutation route to the private active runtime
store. Runtime reads expose read-only copies
(`SimulationRuntime.cs:775-776`). The private construction clone validates
references/dates and preserves source revision with `Clone(PersonStore)`
(`SimulationRuntime.cs:9621-9679`).

Every successful faction registration, affiliation add, and affiliation end
advances the one local `long Revision` exactly once. A faction registration
changes faction cardinality; an add changes affiliation cardinality; an end
replaces an affiliation value with terminal fields and preserves affiliation
cardinality. Failed validations, stale proposals, duplicate/active-pair
rejections, wrong-store proposals, owner-thread/admission rejection, and local
or shared-epoch exhaustion leave owner values, both cardinalities, and local
revision unchanged. P12-B invalidates both section IDs once for each successful
operation in one mutation epoch. Owner export/hydration must not add mutation
operations or alter these writers/revisions.

## 4. Detached export and deterministic continuation

Capture the two sections from the same exact `FactionStore` under the existing
P12-B completed-boundary token and owner read-cut/quiescence evidence. This
owner adds no capture lock, token, admission registration, mutation epoch, or
capture-eligibility rule. Both census witnesses must match owner instance,
schema 1, and exact same revision; each witness cardinality must equal its
detached row count. A changed revision during export rejects the capture.

Export immutable detached values only:

- sort faction rows by ordinal `FactionId.Value`;
- sort affiliation rows by ordinal `FactionId.Value`, then ordinal
  `PersonId.Value`, then ordinal `AffiliationId.Value`, matching the current
  getter's `CompareAffiliations`;
- preserve every historical tenure, exact stable ID, nullable end reason,
  and exact day without normalization;
- record the shared owner revision as metadata for both sections, not a
  revision per section and not a value recomputed from row count.

The serialized owner identity binds to the existing selected-profile manifest
entry and the exact `FactionStore` authority. Do not serialize the ephemeral
`OwnerToken`, pending transitions, `PersonStore` object reference, active-pair
dictionary, mutation guard, or `FactualReadAdmission`. The latter values are
runtime machinery: staging will rebuild the active index and P12-G will bind
the fresh healthy guard/admission after graph validation.

## 5. Private staged reconstruction and rejection rules

The minimum referenced root is the P12-D `PersonStore`; every affiliation's
`PersonId` must resolve there. Build a private `FactionStore` with that exact
staged PersonStore dependency. Stage faction definitions before affiliations.
For each affiliation, require an existing staged `FactionId` and `PersonId`,
then add the exact row and rebuild the active-pair index. Validate all owner
references and cardinalities before returning the candidate. Do not stage a
`PoliticalSupportStore` here; its Faction target references are a downstream
owner/integration concern and remain in that accepted checkpoint.

Validate these existing domain/owner invariants without reinterpreting policy:

- all typed IDs are nonempty and unique in their own identity domain;
- faction policy enum values are defined and faction creation day is between
  zero and captured `CurrentDay`;
- every affiliation has an existing faction and person, joins on or after the
  faction creation day and no later than captured `CurrentDay`;
- if present, `EndedAbsoluteDay` is not before joining and is no later than
  captured `CurrentDay`; a defined end reason requires a terminal row;
- a pair has at most one active row;
- every row and both cardinalities exactly match the source snapshot;
- affiliation IDs are preserved verbatim. Recompute the next join sequence
  only through the existing historical-row count after restore.

Preserve each membership policy and each stored end reason as facts. Do not
infer an old event's cause from policy, reject an otherwise constructible
ended row merely because its optional `EndReason` is absent, or turn
`LeaveNoRejoin` into a universal limit on all Faction affiliations. The
current owner/diagnostic invariants check the policy enum, IDs, references,
one-active-pair cardinality and dates; the transition API applies prospective
rejoin/leave/expulsion policy. Staging restores exact current owner state; it
does not replay or synthesize event history.

Restore the exact captured local `Revision` by the private hydration path after
the rows and active index validate. Do not replay public registration/end
operations to derive it: terminal transitions advance revision without adding
rows. Preserve `long.MaxValue` as valid owner state; future writes then follow
the existing overflow rejection. Reject absent required sections, unknown
schema, wrong owner identity, negative or disagreeing shared revisions,
duplicate IDs, missing roots, duplicate active pair, invalid policy/end enums,
invalid dates, malformed rows, count mismatch, or incomplete indexes. Reject
instead of filtering, renumbering, or coalescing. On any error discard all
staged data and leave the active runtime, its owners/bindings/revisions, and
random state unchanged. This owner does not publish or bind the staged
candidate; P12-G owns global graph validation and atomic publication.

## 6. Required focused tests and validation

Add a dedicated Faction owner snapshot suite; retain
`FactionFoundationTests` and `P12FactionStoreCensusTests`, and run relevant
Person and political-support reference regressions. The exact suite artifacts
at this base establish census counts/shared revision and P12-B facade
invalidation, not export or hydration. Cover at minimum:

| Case | Required assertion |
|---|---|
| Empty sections | Both required sections export/stage as empty with the exact owner identity and shared revision. |
| Faction values | Round-trip every faction field, including each membership policy, exact DisplayName, and ExpulsionAllowed. |
| Current and terminal tenure | Preserve exact stable affiliation ID, faction/person IDs, join/end days, nullable EndReason, and derived IsActive. |
| Rejoin history | Preserve multiple distinct historical tenures for LeaveAndRejoin; after round-trip the next proposal has the same sequence-derived stable ID. |
| LeaveNoRejoin | Preserve all historical rows; subsequent proposal still rejects after any earlier tenure. Do not infer policy from cardinality. |
| Active pair | Rebuild exactly one active pair index and reject multiple active rows for one pair. |
| Revision | Include successful end operations so revision exceeds current row counts; restore exact revision. Verify a captured max revision is preserved and later mutation fails without state change. |
| Determinism | Permuting source insertion order yields the same section order and value-identical repeated round-trips. |
| References and time | Missing Person/Faction, join before faction creation, future creation/join/end, end before join, and missing ID reject staging. |
| Policy and nullable reason | Preserve an ended row with no EndReason; preserve voluntary leave and expulsion reasons exactly; invalid enum values reject. |
| Metadata and atomicity | Missing/unknown section/schema, wrong owner, disagreeing revisions, duplicate IDs, count/index mismatch, or injected late failure discard the whole private candidate without changing active state. |
| Existing mutation contract | Rejected/stale/cross-store/owner-thread/epoch-capacity writes preserve values, counts and revision; no census ID or P12-B operation changes. |

Implementation integration then runs the focused owner snapshot suite,
`FactionFoundationTests`, `P12FactionStoreCensusTests`, relevant
`PoliticalSupportFoundationTests`/`P12PoliticalSupportCensusTests`, Person
owner snapshot tests, the owner-composition suite, ALL EditMode, complete
official Smoke, and `git diff --check`. Retain inspectable XML/log artifacts
and exact code/tree hashes. The generic P12-E §6 and P12-G still own complete
profile parity, global graph validation, and publication evidence.

## 7. Dependencies, hotspots, and explicit exclusions

P12-B and P12-C are promoted prerequisites, and P12-D supplies the Person root.
Faction and affiliation reconstruction is then a prerequisite root for any
downstream staged owner that references Faction. Keep edits isolated from the
current Property/Estate owner work; serialize integration touching
`SimulationRuntime.cs`, the P12-E staging coordinator, bootstrap/persistence
composition, or any shared owner registration. The domain contract itself has
no product/canonical architecture ambiguity: it implements already-decided
Faction-specific membership policy and preserves the current historical-tenure
capability.

Explicit exclusions: no implementation code; no changes to the two census IDs,
schema, `p12.faction.owner-commit`, or P12-B mutation wiring; no
`PoliticalSupportStore`, `PoliticalClaimStore`, or `PoliticalDecisionStore`
design or snapshot; no Knowledge/Crime/Justice/provider state; no Group,
Organization, Polity, faction-office, support, loyalty, obedience, gang, or
other gameplay work; no P12-G graph/publication; no P12-A readiness, complete
owner coverage, capture eligibility, or Phase closure; no P13 fork/history
semantics.
