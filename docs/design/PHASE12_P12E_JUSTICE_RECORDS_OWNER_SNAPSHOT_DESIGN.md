# P12-E Justice Records Owner Snapshot Design

**Status:** Current-base technical design candidate; pending independent exact-content review.

**Checkpoint:** P12-E — Profile-selected core and official daily-domain owners.
**P12 canonical base:** `codex/phase12/canonical` at `565f6b5817e0126c2f89e0a1452b09a8fb03cc19`.
**Architecture baseline:** `codex/architecture/world-identity-projection` at `47eff220c7ce00f6e7c759bdc2b76780bb46f628`, including the intraday/extensibility and multi-participant alignment records.
**Scope authority:** Accepted P12 Brief and `PHASE12_E_TECHNICAL_DESIGN.md`; P12-E prerequisite capability work is authorized. P12-A remains separately gated.

## 1. Contract boundary

This slice supplies detached exact owner values and private staged reconstruction for the current `JusticeSystem` in `UnityBootstrap-Daily-v1`. It covers only the ordered wanted-record and prison-sentence rows owned by that instance, with the exact local Justice revision. The proposed snapshot section is `p12.crime-justice.records`, schema version 1. It consumes, without changing, the existing P12-B census section `p12b.justice-records` and its completed-boundary owner vector.

The accepted P12-E owner map assigns justice/custody/sentence and warrant facts to E. The single P12-D/F NPC projection remains the authority for NPC status, condition, life, and action values; P12-D owns City and NPC roots. This adapter resolves justice references to those exact staged roots and never duplicates or applies their values.

This is a bounded owner subtask under P12-E, not a new numbered checkpoint. It does not implement runtime/bootstrap composition, mutate the P12-B census protocol, or establish P12-E completion.

## 2. Exact stored values

`JusticeSystem` does not assign stable IDs to `WantedRecordRuntime` or `PrisonSentenceRuntime`. Preserve each list's exact order and multiplicity. A zero-based row ordinal is only a transport key for linking a sentence to its warrant; it is not persisted domain identity and must not be used as a new allocator or gameplay key.

For each wanted-record row, preserve:

- target `NpcRuntime.RuntimeId` and City `RuntimeId`;
- exact `Bounty`, `SentenceDays`, and resolved/active disposition;
- its original list ordinal.

For each prison-sentence row, preserve:

- target and City RuntimeIds;
- the referenced wanted-record ordinal;
- exact `RemainingDays`, `FailedEscapeAttempts`, and `WasArrestedToday`;
- its original list ordinal.

Preserve the exact nonnegative `P12CrimeJusticeRevision` independently of row count. The census cardinality remains `wantedRecords.Count + prisonSentences.Count`; a revision is not derived from that sum. Do not collapse equal-valued rows: separate rows and their order are source state.

The P12-E value section does not copy the P12-B witness, NPC statuses, City facts, operation events, diagnostic logs, or P18 temporal receipts. The selected Daily-v1 profile excludes the Justice P18 boundary-step receipt state. Capture must consume its existing exact `p12b.justice-p18-receipts` witness with the exact registered owner, cardinality one, and revision zero; reject a nonzero or mismatched witness. Do not add another census provider or serialize transient mutation-guard, P12 callback, or Daily-profile receipt-boundary bindings.

## 3. Capture and staged-root order

Capture accepts the admitted runtime, its exact installed `JusticeSystem`, the completed-day eligibility token, and that token's exact P12-B owner-section vector. Require the exact `p12b.justice-records` witness to reference that Justice instance and match its current combined cardinality and local revision before copying. Copy wanted and sentence rows in owner order, then recheck the token, both owner witnesses, and the exact-zero Justice P18 receipt witness. Any owner, count, revision, or boundary change rejects capture.

Staging runs after the exact P12-D staged City and D/F NPC roots exist. Resolve every RuntimeId against those exact staged roots; reject missing or ambiguous identities. Reconstruct wanted records first, preserving their order and all historical/resolved rows. Then reconstruct sentences using the wanted-row transport ordinal and require the sentence's target and City to be the exact same references as its referenced warrant. Retain the current domain invariants and reject malformed scalar values, null rows, invalid ordinals, duplicate object references, and declared count/revision mismatch.

Private owner factories must install recorded values without calling public live mutation methods, notifying P12, emitting events, synchronizing statuses, or replaying daily work. The staged group remains unreachable from the active runtime. It uses the admitted profile's existing status/configuration assets; it does not serialize those assets or diagnostics. Later publication binds fresh P12 callbacks and the mutation guard through the normal P12-G integration. No transient P18 receipts or runtime bindings are restored.

The adapter validates only relations already represented by Justice: exact staged target/City identity, sentence-to-warrant identity, and the source owner's existing row rules. It does not strengthen the domain contract by inferring NPC status from a warrant, changing a status to make the records agree, or inventing a new relationship between historical records and current NPC fields.

## 4. Validation plan

Add a focused `P12EJusticeRecordsOwnerSnapshotTests` suite covering:

1. Exact empty capture/stage with section identity, owner identity, cardinality zero, revision zero, and a current completed-day token.
2. Populated round-trip of multiple ordered wanted rows (including resolved history) and sentences, preserving every scalar, row order, multiplicity, exact local revision, and sentence-to-warrant relation.
3. Exact staged NPC and City object references, including rejection of missing or ambiguous RuntimeIds and mismatched target/City/warrant links.
4. Rejection of missing, duplicated, reordered, wrong-owner, stale, or changed P12-B witnesses; stale-token rejection when capture crosses a successful owner mutation.
5. Rejection of negative revisions/counts, overflowed cardinality, malformed bounty/sentence values, null rows, duplicate object references, invalid sentence ordinals, and row/cardinality mismatch; all failure paths return no staged group and leave source and previously staged roots unchanged.
6. Exact-zero Justice P18 receipt admission and rejection of a populated or stale receipt witness. No receipt is restored by this Daily-v1 slice.
7. A post-stage mutation test proving reconstruction does not emit P12 mutation callbacks, events, or NPC status changes; subsequent publication binding remains an integration responsibility.

Integration validation must include the focused suite; affected Crime/Justice, NPC-root, P12 census/epoch, and runtime-admission regressions; ALL EditMode; official Smoke; and `git diff --check`. Retain exact XML/log/hash evidence tied to the reviewed code tree. Any code-tree change after review requires affected validation and a fresh exact-tip implementation review.

## 5. Ownership and exclusions

Expected implementation ownership:

- `Assets/_Project/Scripts/JusticeSystem.cs` for private detached-row capture and no-notification staged factories;
- one new `Assets/_Project/Scripts/P12EJusticeRecordsOwnerSnapshot.cs` and Unity `.meta`;
- one focused EditMode test file and `.meta`, plus validation/review evidence.

Do not edit `SimulationRuntime.cs`, `TesteSimulacao.cs`, `P12CrimeJusticeCensusProviders.cs`, `P12CrimeJusticeInvalidation.cs`, `CrimeSystem.cs`, `GuardSystem.cs`, City/NPC root snapshot ownership, or P12-G publication in this slice.

This design does not cover CrimeSystem hidden-status operation receipts, CrimeJournal/events, `CrimeSocialAppraisalWorldState` (already promoted separately), configured Guard/Crime provider state, economy/demography/mortality, Merchant/CommercialKnowledge sharing, P12-F Knowledge or commitments, global P12-E integration, P12-G publication, P12-A or P13 readiness, or Phase 12 closure. These remain independently inventoried P12-E/F owners and gates.

## Readiness

The owner family and export/staged-hydration requirement are inside the accepted P12-E scope and its prerequisite implementation authorization. A fresh independent current-base design review is required before implementation handoff. If review confirms the owner boundary, row semantics, token/vector use, and exclusions above against canonical code, this bounded owner snapshot is ready for implementation; a changed canonical base requires drift classification first.
