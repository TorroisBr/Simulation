# P12-G political binding decision and finite completion handoff

Date: 2026-10-12.
Architecture base: `58157f9ac7bf573e9a4ed1b2d485a2db190e7c4f`.
P12 canonical inspected: `eef376d51f36641918a13b2e937e704051814820`.
Owner: General Architect; implementation owner: General Orchestrator.
Scope: existing P12-G in-memory same-build/current-host Daily-v1 contract.
Status: technical decision candidate; independent review required before adoption.

## 1. Decision

Accept **A**, bounded test-only synthetic restored-continuation source, with
the exact construction and context seam below. Accept neither the existing
unadmitted seeded baseline nor a snapshot override as the positive round trip.

This closes an evidence/test-seam blocker for Required E/F owner data already
in the contract. It does not make a new political writer supported by Daily-v1.
Option B is unnecessary for this evidence obligation. A future supported
political producer would need its own reviewed writer/owner-thread/operation/
epoch contract; that future work is not a prerequisite created by this decision.

The fixture may count toward §6.2 populated E/F round trip, its named §6.3
binding rejection, §6.4 failure preservation/retry, and §6.5–6 no-effect and
multi-boundary parity evidence. It cannot count as normal bootstrap-to-political-
writer end-to-end evidence, exhaustive arbitrary-call quiescence, or proof of
a previously simulated source history. Every claim must carry that distinction.

## 2. Current-source findings and authorities

The following paths are repository-relative references to the inspected P12
commit, not necessarily the executable contents of the older architecture tree.

| Boundary | Current source and finding |
|---|---|
| Selected reachability | `docs/design/PHASE12_B_DAILY_V1_REACHABILITY_AUDIT.md`: installed PoliticalKnowledge and PoliticalDecision owners are Required, with no configured successful Daily political writer. Three public facades are not supported ingress merely by being public. |
| Census | `Assets/_Project/Scripts/P12DeferredOwnerCensusProviders.cs`: exact installed store Count/Revision witnesses, schema 1; IDs `p12f.political-knowledge.holders` and `p12f.political-decisions.records`. The latter remains E-owned despite its historical prefix. |
| Writes | `SimulationRuntime.cs`, `PoliticalDecision.cs`, `PoliticalKnowledgeStore.cs`: runtime facades delegate without political P12 operation/census refresh. Owner methods increment local revisions. A post-construction write can stale census/token; this decision repairs none of those writer semantics. |
| Initial versus restored census | `SimulationRuntime.TryRegisterP12DeferredOwnerSections`: ordinary bootstrap requires zero; restored context registers the reconstructed populated baseline. The existing `RestoredDailyOwnerBaselineAcceptsPopulatedPoliticalKnowledgeAndDecisionOwners` test proves only that baseline. |
| B token | `SimulationRuntime.TryAdvanceDay`, `TryPublishCompletedDailyCaptureToken`, `TryGetCompletedDailyCaptureToken`: positive successful outer advance, closed scopes, healthy owner-thread, exact census and epoch; provenance `CompletedAdvance`. No idle/unadmitted state substitutes. |
| E capture | `P12EPoliticalDecisionOwnerSnapshot.cs`, `P12EDailyV1OwnerPackage.cs`: token/vector identity, exact owner, count/revision, immutable record fields and typed D/E links. E emits unresolved Knowledge bindings when KnowledgeReferences is nonempty. |
| F capture | `P12FDailyV1OwnerCapture.TryCapture`: authentic token-bound F capture before C allocation; retained capture stages against the same C/D/E attempt. PoliticalKnowledge stays F. |
| G graph | `P12GDailyV1RestoreCoordinator.TryValidatePoliticalKnowledgeBindings`: binds by decision ID; exact ordered reference lists and expected revisions; holder resolves in staged F and expected revisions cannot exceed reconstructed values. It does not resolve opaque reference strings as individual observation keys. |
| Source context limitation | `SimulationRuntimeAdmissionContext.CreateRestoredContinuationContext` rejects an already-restored context; G calls it in private candidate composition. A seeded restored baseline is therefore not an ordinary second-restore source even after it gets a valid token. |
| Publication | `TesteSimulacao.TryPublishRestoredSession`: idle serialized holder checks followed by one `Interlocked.Exchange`. No fallible post-swap disposal/callback is present. |

Current owning State records B–F promoted within scope, the 299-row selected
live inventory prerequisite closed in its supported-ingress bounds, G partially
delivered / WAIT_DEPENDENCY, A WAIT_DEPENDENCY, P13 BLOCKED, Phase 12 OPEN.
Current C–F/package and coordinator code implement the reviewed sequence;
their presence is not completed §6 evidence.

The old `02009f9` and `a88cc73` crosswalks are dated source evidence.
The later `PHASE12_G_CURRENT_BF_PACKAGE_INTERFACE_REVALIDATION_3840AA70.md`,
its independent PASS review, the `7b2846a` live-inventory review, and current
State supersede their unimplemented-seam and missing-inventory statements.
The populated political relationship item remains pending at the inspected tip.

## 3. Source fixture contract

Construct two independently owned, equivalent synthetic sessions: source and
uninterrupted control. Share only compatible immutable definitions/configuration
where current owner contracts allow; mutable stores, root state, guards, census,
contexts, time and owner identities must be private and distinct.

1. Use current accepted Daily-v1 definitions, effective config/calendar, P9-B
   manifest/geography and all required composition roots. Prefer authentic
   C/D/E/F private staging from a normal completed baseline as construction
   material, retaining its normal bootstrap admission context as an owner-thread
   anchor. This does not claim that construction material already contains
   the political state under test.
2. Populate detached, unpublished political owners before constructing the
   synthetic runtime/census. Use existing owner constructors/mutators and
   exact staged Person/Institution/Office/Claim/Faction/Property roots.
   All facts satisfy current snapshot/constructor invariants and captured-day
   bounds. Do not mutate an already-registered source owner or refresh its
   census manually. Construction is test setup, not a simulated writer history.
3. Smallest positive graph: at least one valid Person holder with one retained
   Knowledge observation, and one immutable PoliticalDecision with a nonempty
   canonical KnowledgeReferences list. Choose `Other`/`None` if sufficient,
   avoiding new gameplay. Preserve a meaningful holder reference and expected
   Knowledge/world revisions accepted by the existing graph contract.
   The older baseline with empty KnowledgeReferences cannot cover this edge.
4. Preserve exact identities, ordered candidate/evidence/reference lists,
   provenance, dates, revisions and counts. Decision records bind to the
   synthetic graph's PersonStore, not the construction-material source.
   Use IDs consistent with existing root/high-water validation; never reset
   allocators or reassign IDs to make staging pass.
5. Construct the complete synthetic runtime with the retained normal context's
   `CreateRestoredContinuationContext()`. This permits populated Required
   baseline owners, not a populated ordinary genesis profile. Keep the
   SimulationActiveSession context identical to the runtime's installed one.
   Validate the entire independently derived owner/vector/alias map and
   profile invariants, including exact-empty/excluded owners and optional
   omitted noncausal models. Dynamic counts follow the accepted
   `61 + 22*N + U + P + 4*C` formula; 299 is the authored fixture count,
   not a universal cardinality or synthetic fixture requirement.
6. Bind core mutation guards through `TryBindRestoredCandidateMutationGuard`;
   establish the synthetic session's factual-read publication boundary using
   `TryMarkWorldPublishedForFactualRead` only after its complete private setup
   is healthy, owner-thread bound and quiescent. This marks no completed P12
   boundary and is not evidence of ordinary genesis/restore publication.
7. Call the real positive `TryAdvanceDay` on that graph. Require complete
   success, healthy final census, closed operations/lease and a valid
   runtime-issued `CompletedAdvance` token with positive core sequence.
   No direct token constructor, reflection to set private token/sequence,
   source-token transfer, or `TryAdmitRestoredDailyBoundary` with invented
   preserved day/sequence is permitted.
8. Read the token through `TryGetCompletedDailyCaptureToken` and revalidate it.
   Its WorldId/context/config/calendar, day, core sequence, exact current
   vector and epoch belong to this synthetic instance. Compare source/control
   at this real boundary. The sequence counts this instance's successful
   advances; it does not attest the construction baseline's past.
9. Freeze source political data after that boundary. Ordinary later supported
   daily advances are allowed. Post-token direct political writes must fail
   capture eligibility under current rules; include that negative control.
   Never force capture of a stale source to manufacture a green round trip.

Authentic capture is mandatory: the positive G call must use ordinary
`P12FDailyV1OwnerCapture.TryCapture` and `P12EDailyV1OwnerPackage.TryCaptureAndStage`
with the exact source token/vector/attempt. No E/F snapshot substitution,
prebuilt political payload insertion or success-return stub is allowed.
Negative tests may corrupt only detached/private target data, after authentic
source capture, with the corruption point and claimed proof explicitly named.

## 4. Necessary bounded test context seam

Current G cannot compose a target from the synthetic restored-context source
because its normal context derivation deliberately rejects nested use.
Do not alter that production behavior or relabel the source/session context.

Authorize one optional **internal test-only target-admission-context factory**
at the coordinator composition boundary, e.g.
`Func<SimulationRuntimeAdmissionContext, SimulationRuntimeAdmissionContext>`
passed as the final optional argument of `TryCreateRestoredSession` and
forwarded only to its private candidate constructor. Production callers pass
null and retain the existing source-context derivation exactly.

For this fixture only, the callback returns a fresh restored-continuation
context from the retained original normal bootstrap anchor. The coordinator
must reject null/invalid results: exact Daily-v1 profile, restored flag,
same captured Thread object/managed ID as the source, current owner thread,
and a distinct context from the source. It may not substitute a runtime,
payload, owner vector, token, day, sequence, manifest or graph validator.
Tests also verify invalid factory/thread/profile outputs reject privately.

A supplied factory is allowed only on this internal test route with a
restored-context source. It remains uncomposed by normal Unity/profile facades.
No production caller starts supplying it; no general loader/context authority
is introduced. A new argument/seam, if needed, is mechanical test plumbing,
not a supported repeated-restore capability.

Everything after context selection is the actual production path: C/D/E/F
private owners, complete G typed graph validation, fresh target census,
guard binding and final vector/epoch recapture, target-bound restored admission.
All source rechecks remain. No validator or completed-boundary condition is
skipped. The target token preserves the source's actual captured day and
positive sequence with fresh target identity and RestoredContinuation provenance.

Direct coordinator evidence counts as coordinator/graph evidence. It does
not by itself prove the Unity holder's final exchange; retain existing actual
publication evidence and classify any remaining holder obligation separately.

## 5. Required bounded evidence

Implement one cumulative political fixture family, not one independent
promotion per assertion. Required outcomes:

- **Real source boundary:** no token before successful advance; real positive
  advance gives a valid token; authentic E/F snapshots have positive counts
  and exact installed owners, revisions, days and nonempty unresolved binding.
  A stale post-token source cannot restore/capture.
- **Round trip:** G reaches authentic F-before-C capture, E/F staging, political
  binding validation, final target-vector recapture and target admission.
  Source/control/target exact facts agree; mutable target identities and
  PersonStore bindings are fresh. Source token cannot be the target token.
- **Binding rejection:** corrupt the still-private candidate to exercise
  holder-resolution failure and future expected-revision failure with a
  nonempty reference list. Require `BindingValidationFailed` at the actual
  political validator, before target admission/publication. Avoid an earlier
  unrelated census failure masking the intended branch.
  Retained E binding metadata versus reconstructed record must also match.
  Do not invent a reference-string parser or observation-key schema.
- **Failure preservation:** rejected attempt returns no candidate, preserves
  source session/context/token/health and every included owner fact/revision;
  source/control continuation agrees, and valid retry succeeds. Required
  corruption is private; source snapshots/owners remain unchanged.
- **No effects and parity:** no decision outcome executes, no genesis runs,
  no holder/observation duplication, no restore-time gameplay/charge/movement/
  consumption/dispatch. Compare all included C–F snapshot facts, root
  identity/provenance, time/calendar, sequences/allocators/random context,
  owner vector facts and alias/bindings immediately and at two or more
  subsequent successful boundaries with identical inputs.
  Source political owners stay stable under those supported daily operations.
- **Default isolation:** normal bootstrap restore regression still runs with
  null factory; default context behavior and ordinary political-write census
  rejection are unchanged. No new production supported ingress is asserted.

Use mapped authentic owner snapshots plus explicit root/identity/alias checks;
the structural projection helper is supplementary and must have an enumerated
coverage map. A generic diagnostic digest alone cannot prove included truth.
Opaque KnowledgeReferences remain preserved ordered strings under current
semantics, not asserted factual observation lookups.

Exclusions: authentic ordinary genesis-to-political-write history, supported
Daily political commands, arbitrary same-process/public-API safety, general
restored-session recapture/repeated-restore support, serialized save/load,
cross-host/build migration, P13 historical fork, P18/P19/P20 state and UI/history
feed parity. Synthetic data does not change any profile's genesis contents or
turn excluded authorities into admitted payload.

## 6. Wider P12-G closure assessment

| Class | Current assessment | Required action |
|---|---|---|
| Covered prerequisites | Selected live inventory and supported-ingress dispositions; B–F package interfaces; in-memory coordinator, target-vector checks, restored admission and holder exchange implemented. | Preserve current reviewed evidence; revalidate only changed assumptions. Do not reopen B–F or rediscover their old missing-seam lists. |
| Missing test seam/evidence | Populated Required political binding source unavailable via normal writer; nested context blocks this synthetic test route. | Execute sections 3–5. No upstream political writer capability solely for the fixture. |
| Missing aggregate evidence | Complete mapping of current admission/rejection, target recapture, owner-internal failures, no-replay and all-included-owner multi-boundary parity to §6. | One closure matrix and batched residual validation below. Existing witnesses are reused, not duplicated. |
| Potential true capability defect | Any concrete admitted state/reference rejected incorrectly, unsupported owner silently captured, or real supported operation whose census/thread/epoch is not preserved. | Keep a precise blocker; bounded fix/design at its owner. None is established by the political test-source limitation itself. |
| Outside selected contract | Serialized parse/digest/duplicate-envelope sections, storage/migrations (P12-A); historical reconstruction (P13); P10/P14/P16/P17 extensions, P18 timeline, P19 modules, P20 activities; arbitrary public callers. | Record explicit disposition, no new G implementation. Required base E military owners are not excluded merely because their current configured producer is absent. |

Two current limitations need explicit reconciliation, not speculative code:

1. **Repeated restore:** restored-source context derivation is a real current
   source-surface limitation. This handoff claims one supported restore
   attempt plus post-restore normal continuation, not general repeated
   save/restore cycles. Preserve the limitation for P12-A's public lifecycle
   design. If an accepted G requirement actually needs that production route,
   record it as a true capability gap and design it separately; the test
   factory must never be counted as delivering that capability.
2. **Lifecycle failure:** current publication has a checked pre-swap gate and
   non-callback reference exchange; current candidate graph defines no
   post-swap disposal hook. Source/code reachability can disposition a
   nonexistent disposal failure as not applicable at this boundary. Prove
   actual pre-swap rejection and swap identity/consumer coherence. Do not
   inject a new disposal framework to satisfy historical generic prose.
   Any existing resource/fallible hook discovered by the audit remains
   required, not waived by this statement.

## 7. Finite completion sequence

This is a bounded decomposition of existing §6, not new checkpoint IDs and
not authorization to claim P12-G complete from the new political test alone.

### Window 1 — Freeze the closure matrix and political fixture

Create one current-tip matrix in the owning P12 evidence/State workflow:
every §6 requirement, supported source class, C–F owner/graph edge, target
census family, observable rejection/failure path and publication boundary.
Each entry has one of: covered (exact test/ref/result), pending evidence,
specific capability defect, or not applicable with current-source reason.
No row is silently dropped. Keep source-reachable round-trip cases separate
from detached-invalid-input robustness tests.

Build matrix from current source, latest State and reviewed records, not from
every historical review's old residual paragraph. Retain terminal ActorChoice,
directives, TravelParty, dynamic NPC/Person/Genealogy, P9 identity rejection,
Crime/Social, target-owner substitutions and late recaptures already proved.
In particular, Genealogy duplicate-edge false return after partial staging
and thrown failure after partial staging are covered; entry-stage injection
alone is not internal-failure evidence.

Execute the political fixture in the same window when hotspot ownership permits.
This decision reclassifies only its existing evidence-source blocker.
Other closure claims stay pending.

### Window 2 — One batched residual rejection/failure matrix

List actual guards/call boundaries in current C–F/G and associate them with
existing package and integrated tests. Local owner validation plus an integrated
same-attempt failure-propagation witness may close a shared branch only after
independent review verifies the exact equivalent call/rollback protocol.
Distinct branches/owner invariants require their own applicable evidence.
No Cartesian enumeration of arbitrary records, unsupported producers or
arbitrary public APIs is required.

Use parameterized residual tests grouped by owner/guard/failure semantics;
positive populated data uses authentic capture. Detached negative overrides
may exercise validation robustness, never masquerade as source histories.
Required internal failures include false/throw after partial private staging
where reachable; an observer thrown before/after a call cannot substitute.

Target checks include each remaining exact owner/sentinel/empty/alias family,
source temporal rejection before C allocation, fresh target vector and
post-guard recapture. Reuse the shared comparer tests plus family-specific
source/target probes with a reviewed provider-to-guard mapping. Do not demand
one new test/promotion per section ID when several rows share the same proven
mechanism; do not assume one row proves a different provider.
Compatibility covers only current in-memory profile/provider/config/calendar/
P8/P9/source identity checks. Serialized envelopes stay P12-A.

Stop adding residual cases when this finite reviewed matrix is discharged.
A newly found branch changes the matrix with its exact justification, not an
unbounded request for “more exhaustive testing”.

### Window 3 — Integrated parity/no-replay and P12-G completion audit

Consolidate a small set of supported evolved fixtures plus the explicitly
synthetic political case. Cover every admitted owner value/revision and
cross-owner relation in the comparator, exact-empty/sentinel state, current
dynamic membership/bindings, and causal input/commitment classes. Combine
compatible populated state where safe; preserve separate fixtures only for
different reachable transitions or incompatible state. No unsupported
Expedition/site/military producer is added to make state nonempty.

Demonstrate immediate graph equivalence, no restore-time genesis/gameplay,
first subsequent causal effect/idempotency equivalence, and multiple completed
boundaries. Document populated omitted noncausal rows and why they do not
participate in authoritative parity. Keep current source preservation/retry
and publication-consumer coherence evidence.

Independent completion audit checks the matrix, actual final source/code,
all retained result evidence and original §6 without inferring closure from
a count of micro-tests. If any contract row remains unresolved, G stays
incomplete with its named row/capability. Only a separately reviewed P12-G
delivery/State update can change its readiness. P12-A, P13 and Phase 12 have
their own unchanged gates, regardless of this audit.

## 8. Master technical handoff and validation

After independent PASS/adoption: **READY_FOR_IMPLEMENTATION for the bounded
political evidence fixture and necessary internal test context seam**.
Other windows are closure-oriented execution/review work under the existing
G contract; concrete newly found production defects need bounded review.

Base at execution: refresh P12 canonical from the inspected `eef376d5`;
classify any changes to contexts, political owners, packages, coordinator,
test helpers and publication. No merge of the older architecture executable
tree into P12. Consume this document by content. Existing evidence packager
is promoted at the inspected P12 tip; use its wrapper/package/verify workflow.

Owned implementation surface:

- test fixture/helper in `Assets/_Project/Tests/EditMode/Editor`, preferably
  a focused political restore suite; reuse current helpers without broad
  refactoring of the actively shared admission test file;
- `P12GDailyV1RestoreCoordinator.cs` only for the bounded optional context
  factory plumbing/validation; no production algorithm, owner schema,
  mutation/census protocol or public profile API changes;
- normal P12 evidence/review/State matrix records.

Serialize coordinator/admission-test hotspots with the Master. Use isolated
worktrees for concurrent writers; no edits to active implementation checkouts.
Required focused suites: new political fixture, PoliticalDecision/Knowledge
foundation and E/F snapshot suites, C–F package integration, G owner vector,
SimulationRuntimeAdmission and bootstrap/publication regressions. Run required
Phase 5–8 regressions, final ALL EditMode, complete official Smoke and diff-check.
LongRun remains required if implementation changes daily-loop/long-horizon
behavior; this seam/fixture contract permits no such change. Retain one
cumulative exact-input package/review where safe; original XML is result
authority and the current packager's protected evidence rules apply.

Independent exact-tip implementation review must verify test-source provenance,
legitimate token, untouched production null path, authentic E/F capture,
private corruption reaching its claimed validator, exact included-owner
comparators, exclusions, no premature readiness and final required gates.
A failing legitimate fixture exposes a concrete gap to resolve, not permission
to bypass admission or silently switch to B.

## 9. Reconstruction and decision boundary

No authoritative schema/state/input is added or discarded. Existing WorldId,
typed IDs, owner revisions, Knowledge provenance, opaque decision references,
time, effective content/configuration, random roots and causal commitments
retain their current meanings and exact continuation payloads.
The synthetic baseline has no attested pre-fixture simulated causal history;
its real advances and restored-target boundary are explicitly scoped evidence.

No P12-A, P13, Phase 12 readiness, Phase closure/reopening, future profile,
new political gameplay or generic synchronization authority follows.
No product choice remains for this bounded test decision. A genuine incompatible
contract/capability discovered during implementation is surfaced precisely;
routine design/validation/promotion mechanics remain autonomous.
