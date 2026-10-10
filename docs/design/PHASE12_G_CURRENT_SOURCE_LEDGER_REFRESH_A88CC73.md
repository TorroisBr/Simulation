# P12-G current-source ledger refresh

**P12 canonical baseline:** `a88cc7386fa89aa1354dea2a160a4705092657fe`
**Architecture canonical:** `47eff220c7ce00f6e7c759bdc2b76780bb46f628`
**Scope:** Reconcile current reviewed evidence with the older P12-G owner/operation/publication ledger. This is source and evidence bookkeeping only; it adds no runtime behavior or checkpoint scope.

## Reconciled current evidence

The reviewed 299-row/24-operation reconciliation at `PHASE12_G_DAILY_V1_OWNER_OPERATION_PUBLICATION_RECONCILIATION_564553F.md` remains the source for the Daily-v1 row families, owner/revision sources, B–F consumers and supported-ingress qualifications. Its specific Crime/Social and publication status text predates later promoted evidence and is superseded only as described below.

### Crime/Social selected-profile ingress and epoch

The normal selected Daily-v1 Steal path is now source-mapped through `SimulationRuntime.TryAdvanceDay` under the existing `runtime.advance-day` operation. `TheftAcceptance` and `KnowledgeAndAppraisal` are internal coordinator contexts, not registered operation IDs. The exact source mapping is recorded in `PHASE12_G_CRIME_SOCIAL_INGRESS_EPOCH_CROSSWALK_97BCC5C.md`; the current-base runtime-ingress test and independent review are recorded in `PHASE12_G_CRIME_SOCIAL_RUNTIME_INGRESS_REVIEW_D859D0F.md`. The focused test observes the three Crime/Social owner rows and revisions after the accepted path and confirms the outer operation is active.

The later exact-epoch test and review, `PHASE12_G_CRIME_SOCIAL_EXACT_EPOCH_REVIEW_33CE302.md`, isolate the composite's own contribution as exactly `+1` shared mutation epoch while the outer day operation is active. This closes the selected normal Daily-v1 Steal ingress and its composite epoch witness. It does not certify every direct Crime/Social call, other owner writers, exhaustive supported ingress, or runtime-wide quiescence.

The existing P12-B direct synchronous owner-write contract remains as reviewed: direct store or standalone composite calls use their existing owner-thread, revision/headroom and epoch-capacity preflights and notify committed changes through the established owner callbacks. These paths do not acquire invented operation IDs. Their existence and contract do not prove that every reachable in-process call is part of the supported Daily-v1 flow.

### Expedition and P12-E Military dispositions

The independent supported-ingress reconciliation in `PHASE12_G_SUPPORTED_INGRESS_RECONCILIATION_F3E27A5.md` and review `PHASE12_G_SUPPORTED_INGRESS_RECONCILIATION_REVIEW_A9ECD9D.md` remain current. The accepted Daily-v1 profile has no supported successful Expedition producer because it composes neither a P10 site nor the Expedition autonomy producer. Keep the Expedition owner Required and preserve its P12-F payload; do not add an unsupported operation or infer a permanent zero rule.

The eight base Military/Conflict/War/Battle owner sections also remain Required. The selected profile has no normal configured mutator caller for those owners. Public mutators and direct in-process references remain a source-surface/quiescence caveat; their presence neither establishes another normal Daily-v1 ingress nor proves they are impossible.

### Operation count and package/publication status

The current source-linked operation reference is the 24-ID matrix, including `runtime.person.parentage`, as recorded in the reviewed `PHASE12_G_DAILY_V1_OWNER_OPERATION_PUBLICATION_RECONCILIATION_REVIEW_10C1302.md`. The older 23-ID count and its 278-row formula are historical and superseded by the current 299-row formula and 24-ID reference.

P12-G restored-boundary admission is now a promoted prerequisite, recorded in `PHASE12_G_RESTORED_BOUNDARY_ADMISSION_IMPLEMENTATION_REVIEW_6F0DDE5.md`. The active-session holder and serialized reference exchange are also promoted; exact-tip review is durably recorded on `codex/phase12/P12GRestoredBoundaryAdmissionReview` at `4441c25aef3da57583c2cbd870bd4c659fe81855`, and validation is recorded in `validation/P12GActiveSessionPublication/VALIDATION.md`. These seams supersede the older ledger statements that the admission API and active-session holder/swap were still unimplemented. They do not supply a restore coordinator, complete target graph, or whole-graph atomicity.

## Remaining P12-G blockers

P12-G is still `WAIT_DEPENDENCY`. The remaining evidence and implementation gates are:

1. Complete the live owner/cardinality inventory across the full 299-row vector and all supported dynamic membership, binding and owner-state transitions. Join every row to its exact installed owner, cardinality/revision source, successful writer or explicit no-supported-writer disposition, and package/target consumer. Existing fixture and selected transition tests are useful evidence but do not yet establish this exhaustive closure.
2. Integrate exact target-owner checks for all owners that have no payload receiver or require sentinel/temporal checks, including P8-C/D, global and per-NPC receipt owners, Crime/Justice sentinels, and ActorChoice temporal input. Individual provider or staging tests do not prove the future G coordinator invokes them in the required order.
3. After the live inventory prerequisite is complete, implement the bounded G coordinator using the promoted B admission and active-session publication seams, then prove whole-graph validation/rejection, failure atomicity, no replay/repeated effects, and continuation parity under the accepted technical design.

No operation ID is added, no owner is reclassified as empty, and no gameplay is added. P12-B through P12-F remain promoted within their recorded scopes; P12-G and P12-A remain `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains `OPEN`. This reconciliation makes no capture-eligibility, export/hydration, P12-A/P13-readiness, or Phase-closure claim.

## Current-canonical writer and inventory blocker refresh — `38334ad`

This read-only source audit is current through P12 canonical
`38334ad55ce1ad467d2119ac968f52cc3aad055a` and architecture canonical
`47eff220c7ce00f6e7c759bdc2b76780bb46f628`. Since the source-ledger refresh
above, the only `Assets` change is the reviewed test assertion recorded in
`PHASE12_G_EXACT_CRIME_RECEIPT_REGISTRATION_REVIEW_EB41667.md`; production
owner and writer code did not change.

### Supported writer and epoch dispositions

- **Crime/Social:** the old open selected-ingress item in the
  `564553f` reconciliation is superseded by
  `PHASE12_G_CRIME_SOCIAL_INGRESS_EPOCH_CROSSWALK_97BCC5C.md` and
  `PHASE12_G_CRIME_SOCIAL_EXACT_EPOCH_REVIEW_33CE302.md`. The supported
  authored Daily-v1 Steal path is inside the registered
  `runtime.advance-day`; its nested Crime/Social stages are not separate
  operation IDs. The exact runtime test observes the three owner rows, an
  active outer operation, and the composite's exact `+1` shared epoch.
  Covered direct standalone writes have their existing owner-thread,
  revision/headroom, epoch-capacity, and callback behavior. This does not
  certify arbitrary direct calls or exhaustive runtime quiescence.
- **Crime and Justice daily/action writes:** supported Daily mutation uses
  the reserved owner scopes nested under `runtime.advance-day`; normal action
  commits use the exact-owner action scopes. P12-bound owner mutators reject
  when the required scope is absent. This is no confirmed selected-profile
  writer/epoch defect. Justice target-sentinel validation remains a G target
  composition obligation.
- **Expedition:** the accepted Daily-v1 profile has no Ruin site or composed
  Expedition producer. Keep the required source owner and F payload. A public
  facade is not evidence of a supported selected-profile writer; do not add
  an operation or infer a permanent zero rule.
- **Military, Conflict, War, and Battle:** the eight base owners remain
  required and captured by E. P16/P17 producers are outside this profile. No
  normal Daily-v1 mutator caller is evidenced. Public mutable references
  remain a source-surface/quiescence caveat, not a confirmed supported
  unwrapped writer or missing epoch edge.
- **Other public helpers:** the registered runtime facades define supported
  ingress. A public helper or raw owner reference alone does not establish a
  second supported Daily-v1 operation. Preserve this as an evidence boundary;
  do not add speculative wrappers or security checks.

No new production writer or mutation-epoch implementation is justified by
the current source audit. The owner-to-writer join remains incomplete as a
whole because the 299-row reconciliation is organized by row family and does
not yet prove every live object's identity, cardinality/revision source,
supported transition, and package/target consumer together at admission.

### Current remaining P12-G obligations

1. Complete the live 299-row owner/provider join, including exact identity,
   cardinality and revision source, supported successful writer and
   enclosing operation or explicit no-supported-writer disposition, and
   B-F package or target consumer. Validate every supported dynamic
   membership, binding, and owner-state transition; the promoted N/P/M
   transition witness is retained evidence for those tested transitions only.
2. Integrate one complete fresh target-owner census with the same staged
   restore attempt. The promoted P12-D per-NPC receipt, P12-E Justice
   sentinel, and P12-F ActorChoice temporal package witnesses remain valid;
   the promoted `eb41667` assertion covers the source Crime owner only.
   Still bind/validate P8-C and P8-D fresh target stores, global receipt
   caches, and the target Crime sentinel, and include the existing package
   witnesses in the coordinator's target census.
3. After the live inventory prerequisite is closed, implement the accepted
   G parse/admit/stage/validate/publish coordinator. Reuse the promoted
   completed-boundary admission and active-session exchange seams; do not
   recreate them.
4. Validate pre-allocation rejection and complete typed cross-owner graph
   checks, then prove failure atomicity, no replay or repeated effects, and
   deterministic continuation parity for the exact selected Daily-v1
   profile.

The current audit therefore closes no additional readiness gate: P12-G
remains `WAIT_DEPENDENCY`, P12-A remains `WAIT_DEPENDENCY`, P13 remains
`BLOCKED`, and Phase 12 remains `OPEN`. It identifies no product or
architecture decision required for the supported writer paths above. The
public Military source-surface caveat is preserved for the G quiescence
review and is not treated as authorization to add a new caller or owner.
