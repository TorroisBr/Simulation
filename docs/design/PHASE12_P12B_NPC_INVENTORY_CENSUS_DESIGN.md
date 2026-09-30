# P12-B dynamic NPC inventory census design

**Status:** Bounded technical contract for an accepted P12-B owner-census work
package. It is not an implementation, new checkpoint ID, P12-B completion, or
P12-A readiness claim.

**Evidence base:** P12 canonical `f7a66963ff6f44ee6116c0b37fd7374bf34ace5a`.
The roster-following NPC SpatialKnowledge family was promoted at `0021b0a`.
This extension must join that same outer membership operation and preserve
its fixed PersonStore witnesses.

## Owner and witness

For each NPC currently installed in `SimulationRuntime`, publish one schema-v1
passive inventory section keyed and ordered by ordinal `RuntimeId`. Bind it to
both the exact `NpcRuntime` and that NPC's exact `InventoryRuntime`. Report:

- cardinality: `InventoryRuntime.Items.Count`;
- revision: the existing `InventoryRuntime.Revision`.

Keep both values. Quantity or average-cost changes can alter inventory truth
without changing row count, while the existing owner revision advances for
successful `AddItem`, `RemoveItem`, and prepared installation. Do not derive
inventory contents from market, account, or diagnostic state.

## Dynamic membership contract

The inventory family follows the same installed-NPC membership boundary as
the SpatialKnowledge family. Add/remove operations must stage and validate both
per-NPC families together with any affected fixed PersonStore sections, then
publish one atomic family delta and at most one epoch increment. A rostered
dead or emigrated NPC retains its inventory section; explicit roster removal
removes it. The current runtime supports unregister followed by a later
registration with the same RuntimeId as two separate successful operations;
each produces its own family delta. This contract does not add an atomic
same-ID replacement operation.

The provider set is derived from the runtime roster, not an arbitrary external
list. Unannounced owner replacement, duplicate/blank RuntimeIds, missing
inventory owners, malformed sections, or owner-revision drift fail census
closed without publishing partial family state. Observe an existing inventory
owner without invoking `NpcRuntime.Inventory`'s lazy-creation getter; use an
internal non-materializing owner accessor or an equivalent owner-presence
check before admitting a provider.

## Writer evidence and limits

Inventory writes can occur through the public `NpcRuntime.Inventory` owner,
including direct `AddItem`/`RemoveItem`, NPC trades, market purchases and
sales, prepared keyed-sale installation, and expedition resource retrieval.
The selected bootstrap also adds authored starting inventory before runtime
publication.

This owner-census slice uses existing inventory revisions; it does not claim
those writers are connected to the global P12-B mutation epoch. Until each
supported committed writer is mapped to a complete owner notification
boundary, any changed inventory revision must invalidate/fail-close this
partial census. Do not add partial notifications that can miss direct owner
calls or multi-owner transaction commits. The published day-zero witness
records the live owner identities, row counts, and revisions after normal
bootstrap; authored aggregate item totals are not a substitute for per-owner
facts.

## Required evidence

- The selected authored bootstrap observes every installed NPC's exact
  `NpcRuntime` and `InventoryRuntime` identities, row count, revision, and
  stable ordinal ordering.
- Dynamic roster add/remove update both this family and SpatialKnowledge
  atomically. After an explicit unregister, a later re-registration using the
  same RuntimeId seeds the new exact owner pair in its own operation. Retained
  roster members keep their exact inventory owner.
- Same-row quantity/cost changes, row insertion/removal, successful trade,
  purchase/sale, prepared keyed sale, and expedition retrieval change the
  owner revision as specified by the domain operation; failed/no-op paths do
  not publish a partial census baseline.
- A changed revision with no registered committed-write notification makes
  census assessment fail closed; it is not silently rebased.

## Dependencies and exclusions

Implement against the promoted dynamic SpatialKnowledge family and preserve it
and the existing PersonStore sections in the same atomic roster reconciliation.
It does not add owner-thread or global quiescence proof, city-presence census,
full inventory transaction epoch wiring, export/hydration, P12-A capture
eligibility, or broader P12-B readiness. The City ImportantNpcs projection is
a separate owner and contract. City composition remains assumed fixed; the
existing `SimulationRuntime.Cities` read-only view still wraps a mutable
backing list and composition sealing/drift detection remains a separate
blocker.
