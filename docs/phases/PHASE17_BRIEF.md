# Phase 17 — Strategic War v1

**Authority:** planning entry subordinate to `../SIMULATION_ARCHITECTURE.md`. **Readiness:** P17-A is `READY_FOR_IMPLEMENTATION` under the reviewed bounded technical contract and Master handoff; Phase 17 remains open.

**Reviewed technical contract (2026-10-05):** P17-A — Explicit Withdrawal
Demand and Explicit War Termination — passed independent technical review at
content tip `4088d3485f360144cc60ee301cc4ecb2550ef5bc` on
`codex/architecture/p17a-technical-design`. The Master handoff is recorded at
`235de342c45a3f3ef3cc3e095e6b5b69be28f3bd`. Implementation is based on P16
canonical `75a27d7ac97e66c2762835ccea7950a945c2f20d` and P12 canonical
`a6572ab3d4330d81edb334ae8b4c84ca5e6b173e`; refresh those refs and serialize
the selected-profile admission integration before promotion.

**Product direction (2026-10-05):** territorial, attritional/capability and
non-territorial/coercive War goals are compatible parts of one strategic War
model. The [P17 direction and P17-A entry record](../architecture/P17_STRATEGIC_WAR_DIRECTION.md)
selects one coercive withdrawal goal as the first bounded proof. Two Factions,
two sides, two bound forces and one P16 crossing are profile limits, not a
definition of War. P7's `PersistentWarStore` remains the single War authority.

**Historical dependency clarification (2026-10-03):** deferral then reflected
P16 operational facts and unresolved strategic/territorial/political product
semantics, not full P12 closure. P16-A is now promoted and this direction
selects the first non-territorial scope without deciding later control or
political domains. The existing War owner must meet architecture §92A when
extended; supported save/fork still needs exact War state and causal inputs.

## Objective and closure

War progresses through explicit strategic decisions, pressure and consequences without collapsing Battle result, hostility, control, occupation and War termination into one event. Its broader Phase closure still requires later bounded entry decisions and delivered capabilities beyond the first proof.

**First checkpoint:** P17-A — Explicit Withdrawal Demand and Explicit War
Termination. The reviewed design and handoff establish its bounded
implementation contract. Implementation and promotion still require the
specified current-base, admission, validation, and independent code-review
gates.

## Dependencies and gates

- **P17-A hard contracts:** P7 persistent War, Battle and force-binding
  distinctions; P8 registered Hex/passages; P16-A one selected crossing and
  receipt; stable Faction identity. Goal ownership belongs to a strategic
  participant, not the War side or force binding by coincidence.
- **P17-A hard capabilities:** promoted P7 War/force foundations, relevant P8
  spatial authority and P16-A movement. P14 supply meaning is consumed through
  P16-A; P17-A adds no productive source, market or material transfer.
- **Integration:** evolve the existing War owner with strategic participation,
  one actual goal and explicit terminal concession metadata. War reads P16's
  committed receipt at a completed boundary; it never mutates P16 position,
  supply, passage, Battle, control, Faction or political truth. Goal satisfaction
  does not end War. Battle victory does not achieve this withdrawal goal.
- **P12 gate:** P17-A's separate proving composition does not enlarge
  `UnityBootstrap-Daily-v1`. New War state must pass §92A and a tested
  exclusion/fail-closed admission path before domain promotion. Full P12
  closure is not an entry prerequisite; supported save/fork claims wait for
  exact state and causal inputs.
- **Design gate — passed:** participant/goal identity and mutation,
  Faction/side/force validation, receipt evidence, terminal input
  authority/reason/order, deterministic clone/read/invariant/hydration seams,
  negative admission and integration sequence are specified in the reviewed
  technical design. P17-A is implementation-ready subject to the current-base
  and hotspot preflight; the P12 selected-profile exclusion proof is required
  before domain promotion, but full P12 closure is not a prerequisite.
- **Demonstrability:** `FOLLOW-UP_DEMONSTRATION`. Show participants/goal,
  one real P16 crossing, goal satisfied with War still active, then an explicit
  concession and terminal reason. Use a minimal text/table Lab path when the
  non-Unity application/read boundary is ready; prefer the demo before formal
  Phase closure. No polished UI gate on P17-A implementation.
- **Exclusions:** military control/occupation, annexation, ownership or
  jurisdiction change, strategic AI, treaty/peace/ceasefire diplomacy,
  government/Polity, Campaign, generic War goals, multiple movement legs,
  P20 Activity execution, universal pressure score and `War.Winner`.
- **Replay/fork sensitivity:** War/participant/goal IDs and initial declaration,
  P16 position/supply/receipt, goal evidence, accepted external concession
  payload/authority/boundary/order and terminal reason must remain recoverable.
- **Hotspots:** War Store/clone, Faction validation, P16 spatial state, P8
  passage, command capture, runtime composition and P12 admission. Serialize
  shared integration with active Master/P12 work.
- **Downstream:** explicit control/occupation for territorial goals;
  multi-fact pressure and Knowledge-based decisions for attrition; further
  coercive goals, participants, diplomacy and termination modes as separate
  contracts. None is implemented by P17-A.

P20's multi-participant activity capability does not redefine War or add a hard
dependency for strategic War. Large-scale execution may use aggregate units/
armies with its own participation, temporal and outcome semantics; compatible
shared mechanisms are reusable without identical small-group scheduling.
