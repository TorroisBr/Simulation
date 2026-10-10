# P12-G live source-owner and same-attempt package evidence

**Canonical code baseline:** `a39bbd49ef8755f5eaa6613594e143de4f9d6c3e`
**Candidate Assets tree:** `85f1ff86e02e3fe25d947c42584f5bf8b6364705`
**Profile:** accepted `UnityBootstrap-Daily-v1`
**Scope:** selected Daily-v1 live owner identity and existing P12-D to P12-F detached NPC-row handoff.
**Validation:** focused 27/27 and 53/53, ALL EditMode 2740/2740, Smoke 5/5;
full artifact hashes are in
[`../validation/P12GSameAttemptOwnerVectorEvidence/VALIDATION.md`](../validation/P12GSameAttemptOwnerVectorEvidence/VALIDATION.md).

## Live vector source-owner oracle

The independent 299-row manifest previously compared section IDs, roles, and
provider witness stability, but it did not compare each *registered* row's
`OwnerInstanceIdentity` to the actual source object. The bootstrap test now
builds a separate section-ID-to-owner map from the live runtime roots,
bootstrap roots, exact NPC/City/Person rosters, and the three opaque census
identity tokens. It compares the map's IDs to the independent manifest and
then compares every registered provider's identity to the expected source by
reference. The fixture's 61 fixed rows, 22-per-NPC rows, conditional residence
rows, Person rows, and four-per-City rows must all match exactly.

The dynamic family helper repeats that registered-provider identity check after
the covered NPC roster, Person materialization, and existing-NPC binding
transitions. This ties those transition manifests to the exact registered
source owners, not only to separate family-provider readings.

Cardinality and revision semantics remain cross-referenced to the reviewed
current row-family map in
[`PHASE12_G_DAILY_V1_OWNER_OPERATION_PUBLICATION_RECONCILIATION_564553F.md`](PHASE12_G_DAILY_V1_OWNER_OPERATION_PUBLICATION_RECONCILIATION_564553F.md)
and its later current-source ledger refresh. Family-specific tests assert the
source cardinalities, revisions, and successful writes. The current-base
transition audit found no uncovered supported Daily-v1 membership/binding or
owner-state transition and no specific missing writer/epoch implementation;
the 24-operation matrix and specialized owner tests remain the operation
evidence. No speculative operation or mutation path is added here.

## Existing contract

`P12FDailyV1OwnerPackage.TryCaptureAndStage` validates that the staged P12-D
NPC list and `NpcFRows` list have equal counts and matching `RuntimeId` values
at every index. The P12-F package then carries the P12-D detached rows in a
read-only list while staging TravelParty and Expedition references against
those same rows. No production behavior or row ownership changes in this
checkpoint.

The existing same-attempt integration test
`P12FOwnerPackageCapturesBeforeRootStagingAndStagesAggregateAgainstTheSameAttempt`
captures P12-F first, then stages P12-C, P12-D, P12-E and P12-F against one
`DailyCaptureStagingAttempt`. Before this change it asserted only that the
resulting P12-F detached-row list was non-null.

## Added evidence

The same-attempt test now proves that:

- the P12-F detached-row count equals the staged P12-D row count;
- every staged D `RuntimeId` is nonempty and unique;
- P12-F carries the exact same row object at each corresponding index; and
- the resulting D and F runtime-ID sets therefore have exact one-to-one
  coverage, preserving the deterministic staged order.

The D-to-F assertions are a bounded package-integration witness. The separate
299-row source-owner oracle covers registered identity for the selected live
fixture, while existing specialized suites and the reviewed source ledger
remain the evidence for owner cardinality/revision and supported writer/epoch
behavior. These tests do not prove runtime-wide quiescence, a complete
same-attempt target census, or P12-G readiness.

## Remaining P12-G gates

The new source-owner oracle closes the registered identity mapping for the
current authored Daily-v1 fixture and the explicitly tested NPC/Person
transitions. The existing source ledger and family tests still require an
independent review as a combined owner/cardinality/revision/writer/operation-
epoch/consumer inventory. The remaining evidence/design/implementation
obligations are:

1. Independently review the exact 299-row registered-owner oracle together
   with the current row-family source ledger, 24-operation matrix, and
   specialized writer/epoch/transition tests. Do not infer unsupported
   direct mutation paths from this selected-profile fixture.
2. Compose a complete fresh target-owner census in the restore attempt. This
   includes P8-C/D target stores, global receipt caches, the target Crime
   sentinel and full typed row coverage; consume existing P12-D per-NPC receipt,
   P12-E Justice sentinel, and P12-F ActorChoice temporal checks.
3. After the live inventory prerequisite closes, implement the accepted bounded
   G coordinator using the promoted restored-boundary and active-session seams.
4. Prove pre-allocation rejection, typed whole-graph validation, failed-attempt
   atomicity, no replay, and exact deterministic continuation parity.

No new mutation operation, epoch path, profile row, or gameplay scope is added.
P12-G remains `WAIT_DEPENDENCY` pending review of the combined inventory and
the remaining integrated target/coordinator obligations; P12-A remains
`WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains open.
