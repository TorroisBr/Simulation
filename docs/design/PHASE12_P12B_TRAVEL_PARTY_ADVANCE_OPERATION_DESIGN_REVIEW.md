# P12-B TravelParty advance operation design review

**Verdict: PASS — independent exact-tip technical design review**

- Design branch: `codex/phase12/P12BTravelPartyAdvanceDesign`
- Exact reviewed design tip: `c75c673be48b506c71730d12503b42bebc6f7f4c`
- Canonical base: `codex/phase12/canonical` at
  `0f36331d84ad3139171d36f980dfe6fa635ae30c`
- Review source: an independent review task, separate from the design author.
- Review record: this documentation-only follow-up; the design text and its
  technical boundary are unchanged from the exact reviewed tip.

The review verified the current operation and owner mapping: the nested daily
advance can commit TravelParty membership, each member's travel-progress
fields, destination City presence, per-NPC SpatialKnowledge, arrival events,
and SimulationRecordSequence. The proposed exact NPC travel-progress witness,
specialized roster reconciliation, and registration of only the existing
TravelParty, City, SpatialKnowledge, and sequence owners fit the accepted
P12-B scope.

Revision capacity must be reserved before each local or multi-owner write;
revision saturation must not permit an unrevisioned mutation. Owner notifications
must follow completed commits and be accumulated into one changed-section set
inside `runtime.travel-party.advance`, including both SpatialKnowledge siblings
and sequence allocation. Outside that operation, direct owner writes that
update registered witnesses still need truthful notifications. Later failure
must preserve existing partial progress, report completed commits as the scope
closes, and rely on the existing daily fault-closed behavior. No rollback,
retry, ordering change, or new event failure policy is introduced.

The current sequence and SpatialKnowledge callbacks notify directly, so the
implementation must route those callbacks through the shared operation context
for this bounded nested scope. TravelSystem-only advancement remains outside
the new operation but its registered-owner mutations must continue to refresh
their own baselines.

No unresolved product or canonical architecture decision was found. This
review authorizes implementation only within the already accepted P12-B
capability scope and does not claim P12-B completion, complete owner or
shared-epoch coverage, global quiescence, capture eligibility, export,
hydration, P12-A readiness, or P13 readiness. P12-B remains incomplete,
P12-A remains `WAIT_DEPENDENCY`, P13 remains blocked, and Phase 12 remains
open.
