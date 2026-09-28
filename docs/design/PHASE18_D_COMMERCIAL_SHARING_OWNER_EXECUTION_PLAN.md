# P18-D — Commercial Knowledge Sharing Owner Execution Plan

**Checkpoint:** P18-D — Bounded Consumer and Daily Compatibility Integration  
**Canonical P18 base:** `9e790c59e14ca7f7ed195c0e6267e10f3cd039d7`  
**Current composed P18-D candidate base:** `6fcbfab2f7ad4382051201e55c78fdf32a8dd820`  
**Architecture and alignments:** architecture `c285466c355103d3637ac165246591b72eb7bda0`; intraday/extensibility `4b6dd1d`; multi-participant activity `c285466`.  
**Reviewed semantic/technical authority:** `docs/design/PHASE18_D_TECHNICAL_DESIGN.md`, especially §4 “Daily-owner target identity and temporal cardinality”; its bounded P18-D design review passed at `9ed6d90`.
**Execution-plan review:** independent review PASS on plan commit `2734924` against exact parent `6fcbfab`; the reviewer confirmed the post-observation all-sender snapshot step, recipient-owned edge receipts, and scope within accepted P18-D. No findings.

## Objective and limit

Add one resumable daily commercial-knowledge sharing pass that preserves the
existing sharing result and gives each directed sender-to-recipient transfer a
recipient-owned atomic receipt. This plan is an implementation record inside
the accepted P18-D checkpoint; it does not add a checkpoint, change sharing
policy, or authorize Phase 18 promotion/closure.

The pass preserves the current eligible-merchant predicate, `CompareMerchants`
ordering, grouping by `CurrentCity` object identity, day rotation, adjacent
pairing, transfer direction/order, observation sort, freshness/provenance
filters, and maximum-successful-update rule. Local observation steps remain
earlier in the frozen boundary order. The legacy daily method remains intact.

## Frozen identity and ordering

At boundary activation, the provider freezes the exact runtime roster
membership/order and eligible merchant subset, including each merchant's
`NpcRuntimeId`, backing `PersonId` when present, current `CityRuntimeId`, and
the resulting ordered directed edges. It computes City object-identity groups
while resolving the unique registered `CityRuntimeId`; it never substitutes a
list ordinal or location alone for the City identity. It records the boundary
day, effective configuration/content identity and operation versions. A retry
uses the same manifest descriptors and does not re-filter merchants, regroup,
or recompute pairings.

The activation manifest cannot contain the final source values: local
observation steps must commit first, and the existing design requires sharing
to observe those results. Therefore the sharing provider emits one
`commercial-sharing-snapshot` step after all local-observation steps and before
any directed transfer. When that step executes, the commercial-sharing owner
captures value copies of every sender's then-shareable market and liquidity
observations, using the existing day/freshness/provenance rules and stable sort.
It atomically retains the whole phase snapshot and a receipt keyed by the
boundary occurrence plus frozen pairing/configuration/content fingerprint.
Snapshot values retain semantic item ID, location ID, price/stock or liquidity
values, observed/received day, source and source runtime ID, and source ordering;
an `ItemDefinition` object reference may accompany its semantic ID for the
current runtime, but is never the only identity. Equal-receipt retry reuses the
first committed snapshot even after recipients have changed. Conflicting
descriptor reuse fails without replacing the snapshot.

This snapshot step is metadata owned by the existing commercial-sharing
authority; it does not mutate recipient world truth. It is not transient
provider memory. Its retained values and receipt remain available until the
boundary pass is complete, and must be included if a future supported P12/P13
profile captures this P18-D continuation state.

## Recipient-owned transfer

For each frozen directed edge, prepare exactly one transfer against that
receiver's `CommercialKnowledgeRuntime`. The owner API works on copied market
and liquidity lists, applies the existing `ShouldReplace` precedence in the
frozen candidate order, counts only successful improvements toward the
existing per-interaction limit, and installs both lists together with the
receipt only after all preparation succeeds. A valid edge commits its receipt
even when no observation improves, so a retry cannot gain a later update.

The edge receipt identity is boundary occurrence + sender and receiver
`NpcRuntimeId`; it also records the backing `PersonId` values when present and
the frozen phase-snapshot fingerprint. The immutable receipt contains the
first result/count. Repeating the same identity/fingerprint returns that result
without reapplying; reusing the identity with a different fingerprint is
rejected without mutation. The provider routes the frozen snapshot values to
the receiver owner and does not update several recipients in one transaction.
Each edge returns only source signals already defined by its domain owner; this
slice adds no new actor-trigger or availability meaning.

## Recovery and validation

Recovery proceeds in the frozen order: resolve the existing phase-snapshot
receipt (capture once if absent), then resolve or apply the first uncommitted
directed edge. A committed recipient receipt skips that edge; a missing receipt
prepares and installs only that receiver's batch. The P18-D coordinator keeps
its existing role as manifest/cursor owner and never owns Knowledge effects.

Focused checks cover ordered candidate application, market and liquidity
replacement behavior, successful-update limit, no-op receipt, exact replay,
conflicting identity reuse, invalid-batch atomicity, source immutability, and
snapshot-plan replay after a recipient has already changed. Validate against
the current integration tip before merging, then run the affected sharing and
knowledge suites. The complete P18-D daily-loop integration still requires
independent exact-tip review, required full EditMode/Smoke and `git diff
--check`; this slice alone does not claim chronological `SimulationRuntime`
composition or profile readiness.

## Exclusions

This plan does not implement actor-local spatial/commercial observation,
demography, the full intraday runtime/profile, P14 material flow, travel,
directives, expeditions, save/replay, or any new gameplay. It does not change
the legacy sharing method or create a cross-recipient transaction. Those owners
remain excluded until their own reviewed receipt/prepared-install seams exist.
