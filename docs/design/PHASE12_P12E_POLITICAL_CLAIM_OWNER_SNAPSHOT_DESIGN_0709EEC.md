# P12-E PoliticalClaimStore owner snapshot contract

**Status:** Owner-specific technical-design candidate; implementation has not
started. **Base:** `codex/phase12/canonical`
`0709eec1244f791e81b1d05f669bb4a577a9fdb6`. **Architecture:**
`47eff220c7ce00f6e7c759bdc2b76780bb46f628`.

This contract narrows the accepted P12-E owner-snapshot design to the current
`PoliticalClaimStore` and its institution-scoped recognition records. It does
not change either existing P12-B census identity, the P12-E profile, or the
meaning of claim and recognition facts. The generic P12-E design blob at this
base is `15aaee09d3295cff81a48e166b620c89f5156346`; its §7 requires the exact
owner fields, references, revision, writers, detached export, staged
reconstruction, and rejection evidence recorded here. The prior generic design
review is not review of this addendum: this candidate needs its own exact-tip
design review before implementation.

## 1. Authority and existing evidence

`PoliticalClaimStore` is the single authority for claim records and current
claim/institution recognition records. Its dictionaries are keyed by
`ClaimId.Value` and by `PoliticalClaimRecognitionRecord.BuildRecognitionId`;
the latter encodes the lengths and values of ClaimId and InstitutionId. The
store exposes separately sorted, read-only `Records` and `RecognitionRecords`
and one shared `Revision` (`Assets/_Project/Scripts/PoliticalClaimStore.cs:8-35,
323-347`).

Preserve these already registered required schema-v1 census section IDs as
the owner snapshot section identities:

| Section | Stable ID | Cardinality | Revision |
|---|---|---:|---|
| Claim records | `p12e.political-claim.records` | `PoliticalClaimStore.Count` | `PoliticalClaimStore.Revision` |
| Recognition records | `p12e.political-claim.recognitions` | `PoliticalClaimStore.RecognitionCount` | `PoliticalClaimStore.Revision` |

The constants and provider show that both witnesses point to the same exact
store object and report its same revision
(`Assets/_Project/Scripts/PoliticalClaimStoreCensusProviders.cs:13-44`). Keep
both sections required, including when empty. Zero claim rows and zero
recognition rows are explicit empty state, not missing state. Do not rename,
merge, re-version, or allocate a new section ID for this work.

The current canonical State records the P12-B commit operation
`p12.political-claim.owner-commit`: only successful claim registration,
recognition application, and claim resolution increment the existing
PoliticalWorldRevision and notify both census sections in one mutation epoch;
the local store revision remains the shared revision
(`docs/PHASE12_STATE.md:2845-2867`). That is existing P12-B evidence, not a
claim that every runtime writer or every P12-B owner is covered.

## 2. Exact retained fields and identity

### Claim record section

One row represents one immutable `PoliticalClaimRecord`. Its primary identity
is the exact ordinal `ClaimId.Value`; there is exactly one row per claim ID.
Preserve these fields without deriving a substitute:

- `ClaimId.Value`;
- `ClaimantPersonId.Value`;
- `ClaimType`;
- `Target.Kind` and `Target.TargetId`;
- `Basis` and `BasisDescription`;
- `CreatedAbsoluteDay`;
- `Status` and nullable `ResolutionAbsoluteDay`;
- the complete `EvidenceReferences` sequence.

`PoliticalClaimRecord` canonicalizes evidence references by retaining each
nonblank exact string once and sorting with `StringComparer.Ordinal`; null
`BasisDescription` becomes the empty string. Export that canonical value
sequence exactly, including an empty sequence. The row does not contain
recognition state, recognizing institution, or recognition history: recognition
is exclusively the second owner section
(`Assets/_Project/Scripts/PoliticalClaimContracts.cs:278-385`).

The existing typed target set is closed for this contract:

| Target kind | Typed identity | Compatible claim types |
|---|---|---|
| Office | `OfficeId` | `OfficeEntitlement`, `SuccessionEntitlement` |
| Property | `PropertyId` | `PropertyEntitlement` |
| Institution | `InstitutionId` | `InstitutionalAuthority` |
| Person | `PersonId` | `StatusRecognition`, `LineageEntitlement` |

Preserve both kind and exact target ID; never infer kind from the ID string.
The mapping is defined by `PoliticalClaimTarget` and
`PoliticalClaimRecord.IsTargetCompatible`
(`PoliticalClaimContracts.cs:73-80, 91-130, 441-465`).

### Recognition relation section

One row represents the current recognition record for a `(ClaimId,
InstitutionId)` pair. The pair is the primary identity and has cardinality at
most one; a claim can have no recognition rows or one row per distinct
recognizing institution. `RecognitionId` is the existing deterministic
length-prefixed encoding and must be preserved exactly as derived identity,
not replaced by an allocated ID.

Preserve all row values:

- `ClaimId.Value` and `InstitutionId.Value`;
- current `State`;
- `RecognitionAbsoluteDay`;
- `Reason` (null is canonicalized by the domain object to empty string);
- the full ordered `History`, with each entry's `State`,
  `RecognitionAbsoluteDay`, and `Reason`.

History order and repeated state values are meaningful. Do not sort, deduplicate,
truncate, or reconstruct history from only the current recognition state. A
record constructor appends the current entry when the supplied history is
empty or does not end in that exact entry; staged hydration must validate the
input first and reject a missing/mismatched terminal entry rather than silently
repairing it (`PoliticalClaimContracts.cs:487-583, 612-621`).

## 3. Supported writers and exact revision semantics

The supported live mutation ingress is the `SimulationRuntime` facade:

1. `TryRegisterPoliticalClaim` registers a claim after validating its
   claimant, typed target, and world-day bounds.
2. `TryApplyPoliticalClaimRecognition` applies a previously proposed
   transition; applying to an existing `(ClaimId, InstitutionId)` replaces
   that pair's current record and appends/preserves history, without changing
   recognition cardinality.
3. `TryApplyPoliticalClaimResolution` replaces the claim record with its
   terminal status and resolution day, without changing claim cardinality.

`TryProposePoliticalClaimRecognition` and
`TryProposePoliticalClaimResolution` are read/prepare operations; they do not
mutate the owner. The corresponding store internals are implementation
mechanics, not additional supported live ingress. Bootstrap clone work occurs
before admission and is not a live mutation path. The facades are at
`SimulationRuntime.cs:6526-6592, 6594-6613, 6624-6670, 6672-6685,
6688-6731`; the owner store mutation methods are at
`PoliticalClaimStore.cs:39-78, 137-182, 204-253, 256-303`.

Each successful store mutation advances the one `long Revision` exactly once.
Registration of a claim changes the claims cardinality; initial registration
of a recognition pair changes recognition cardinality; recognition update and
claim resolution preserve their respective cardinalities. Failed validation,
stale transitions, duplicate identities, rejected admission, and revision
overflow leave owner values, both cardinalities, and local revision unchanged.
P12-B then invalidates both owner sections once under its existing single
operation/mutation epoch. Serialization and staging add no live mutation,
P12-B operation, writer, revision, or epoch wiring.

These are the supported successful runtime facades found in the current code
search. A future implementation review must recheck direct store calls and
write paths against the exact implementation base; any newly found live writer
is a blocker until explicitly covered by this owner revision/invalidation
contract. No write path may be added through the snapshot API.

## 4. Capture and deterministic detached export

Capture occurs only at a valid P12-B completed boundary, using its existing
admission token, selected-profile owner inventory, and quiescence/read-cut
protocol. This owner adds no lock, token, registration, or eligibility rule.
Capture the two collections and their shared local revision from the same exact
`PoliticalClaimStore` authority. Both section witnesses must agree on owner
identity, schema version 1, and revision; their cardinalities must equal the
detached exported row counts. A before/after revision check must reject a
changed owner read rather than package mixed versions.

Export only immutable detached rows. Sort claims by ordinal `ClaimId.Value`
(the existing getter's tie-break claimant ID remains a deterministic
defensive tie-break); sort recognition rows by ordinal ClaimId then ordinal
InstitutionId, matching the current getter. Preserve the canonical evidence
reference ordering and recognition history order verbatim. Export local
revision once as owner-section metadata and bind that same value to both
sections. It is not equal to row count and must not be recomputed by counting
the rows or by replaying mutations.

The export package contains exactly the two required sections and no decision,
faction, support, knowledge, or reconstructed fact sections. Its owner
identity binds to the existing selected-profile manifest entry and the exact
runtime's `PoliticalClaimStore` owner instance; do not introduce a second
serialized owner identity.

## 5. Private staged reconstruction and rejection rules

Stage from the two detached schema-v1 sections only after their referenced
roots are available in the same private composition:

1. The P12-D `PersonStore` supplies every claimant and Person target.
2. The P12-E Institution/Office package supplies every Institution and Office
   target and every recognition Institution.
3. The P12-E Property/Estate package supplies every Property target. Its
   current canonical implementation track must finish/revalidate before this
   claim stage can be fully integrated; a target must not be dropped or
   deferred to a later live resolution.
4. Build a private, unbound `PoliticalClaimStore` candidate; validate every
   claim first, then every recognition relation. Validate exact expected row
   counts and owner-section identity/revision before returning the staged
   owner to the enclosing composition.

Use existing typed IDs and owner lookups. Claims must have a registered
claimant, a supported compatible target kind, and an existing target. Claim
creation day must be within the captured world timeline. Active claims have no
resolution day; terminal claims require a valid resolution day not before
creation and not after the captured day. Recognition must point to a staged
claim and registered Institution; its current recognition day must be on or
after claim creation and no later than the captured day. Recognition identity
must equal the existing length-prefixed pair identity. Each history entry must
be non-null, have a defined state, be chronological, and lie within the
claim/world timeline; the terminal entry must exactly agree with the current
state, day, and reason. These checks preserve the current transition and
snapshot invariants; no domain truth is inferred from a claim or recognition.

The staged store's local `Revision` must be restored to the captured exact
value through a private owner hydration path after successful row assembly.
Do not replay public mutations to synthesize it: resolution and recognition
replacement advance revision without adding rows. Preserve even a captured
`long.MaxValue` revision exactly; it makes subsequent writes follow the
existing overflow rejection but is not malformed snapshot state. Reject
negative or disagreeing section revisions, unsupported schema, absent required section, duplicate
claim IDs, duplicate `(ClaimId, InstitutionId)` relations, invalid typed IDs,
missing/dangling/wrong-kind references, invalid states/dates/history, count
mismatch, invalid revision metadata, and any incomplete candidate.
Reject rather than filter or normalize a malformed row. On any failure,
discard all staged state and leave active runtime, current owner, its bindings,
revisions, and random state untouched. This owner does not publish or bind the
candidate; P12-G owns final graph validation and publication.

## 6. Required focused test matrix

The implementation must add a dedicated PoliticalClaim owner snapshot suite
and retain the existing `PoliticalClaimFoundationTests`,
`P12PoliticalClaimCensusTests`, and relevant
`PoliticalSupportFoundationTests` / `PoliticalLegitimacyDecisionFoundationTests`
regressions. At minimum cover:

| Case | Required assertion |
|---|---|
| Empty owner | Both required sections export and stage explicitly empty; shared revision and owner identity remain exact. |
| Populated claims | Round-trip every claim field, each supported target kind, evidence ordering, active/terminal state, dates, and exact identity. |
| Recognition cardinality | Zero, one, and multiple distinct recognizing institutions for a claim; duplicate pair rejects. |
| Recognition replacement | Current row replaces in place, cardinality is stable, ordered history and current reason/day/state survive, shared revision advances exactly once. |
| Claim registration/resolution | Successful registration and terminal resolution preserve all fields and cardinality rules; local revision is exact. |
| Exact local revision | Include a source where revision exceeds row counts because of recognition replacement and resolution; staging restores that exact revision rather than recomputing it. |
| Determinism | Permuted source insertion order yields byte/value-equivalent ordered owner sections and repeated round-trip equality. |
| Root references | Missing claimant, wrong/missing typed Office/Property/Institution/Person target, missing recognition claim or Institution all reject the private candidate. |
| Malformed data | Missing/unknown section/schema, duplicate identities, wrong owner, mismatched revisions/cardinality, invalid enum/date/status, malformed/nonchronological/history terminal mismatch all reject with no active mutation. |
| Failure atomicity | Inject a failure after claims have staged but before recognition completion; source runtime and candidate publication target remain value/revision/binding equivalent. |
| Existing contract | Failed runtime writes and revision overflow preserve values/revisions and existing P12-B census semantics; no new operation or census IDs appear. |

Then run the focused political suites above, relevant Institution/Office,
Property/Estate and Person owner suites, the owner-composition suite, ALL
EditMode, complete official Smoke, and `git diff --check`. Implementation
validation must retain inspectable XML/log artifacts and exact code/tree
hashes. The generic P12-E design §6 supplies the final whole-profile and
P12-G-boundary obligations; this contract does not claim them complete.

## 7. Integration order, readiness, and exclusions

The field and writer boundary is now specified by this candidate, but the
candidate is not an implementation review or authorization to promote. Obtain
fresh current-base exact-tip design review before code work. Keep the owner
code/tests in separate files; serialize edits to `SimulationRuntime.cs`, the
P12-E private staging coordinator, and any persistence/bootstrap integration.
Integrate claims after Person, Institution/Office, and Property/Estate roots
are available, then let downstream F owners consume the staged claim root
without duplicating claim facts. The promoted Property/Estate owner snapshot
supplies the exact Property target root; staging must bind to it and reject a
missing or mismatched owner. This is a known dependency boundary, not a
product ambiguity.

Explicit exclusions: no code implementation in this checkpoint; no changes to
the two P12-B census IDs, schema, operation, or mutation wiring; no
`PoliticalDecisionStore`, `FactionStore`, `PoliticalSupportStore`, or
`PoliticalKnowledgeStore` snapshot; no decision sequence or allocator; no
Crime/Justice/social-appraisal or provider state; no P12-G graph/publication;
no P12-A profile integration/readiness, global quiescence or capture-eligibility
claim; no Phase closure; no P13 history/fork semantics; no new gameplay or
claim/recognition meaning. Existing claim/recognition facts remain evidence
about asserted claims only and do not change target truth.

## Current-canonical base revalidation — P12 canonical `a768f2d` (2026-10-08)

The exact-content design review for the original owner contract passed at
`da4c02d` on `codex/review/phase12/P12EPoliticalClaimOwnerSnapshotDesignC286BC7`,
reviewing design commit `c286bc7b41c1c5b499d98b55f192be784ee43faa` against
canonical `0709eec1244f791e81b1d05f669bb4a577a9fdb6`. After that review, P12
canonical advanced to `a768f2d9eca161f5cff059a782737412f43b2861` with the
separately reviewed Property/Estate snapshot promotion. Current-base replay
commit `465d0b2957b2d2dc621a4c0b3d79cccf971f5b89` places the same owner
contract directly on that canonical base. This addendum records the current-
base revalidation and is part of the candidate for fresh exact-tip review.

**Classification: `BASE_DRIFT_ONLY`.** The `0709eec..a768f2d` delta adds the
Property/Estate owner snapshot implementation, tests and validation, the
independent review record, and the P12 State promotion update. It changes no
`PoliticalClaimStore`, PoliticalClaim census provider, claim/recognition
writer, revision rule, or P12-B claim operation. The two stable section IDs,
exact fields, cardinalities, shared local revision, supported writer list,
and failure/atomicity contract remain compatible. The promoted
Property/Estate snapshot now provides the exact staged Property target root
that this claim stage consumes. P12-E owner ordering remains after Person,
Institution/Office and Property/Estate, with wrong or missing typed roots
rejected before returning a candidate.

This revalidation does not resolve the separate PoliticalDecisionStore or
CrimeKnowledge ownership partition questions, add runtime integration, or
change P12-B readiness. Obtain a fresh independent exact-tip review of this
current-base design candidate before implementation. P12-E remains IN
PROGRESS; P12-A remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12
remains open.
