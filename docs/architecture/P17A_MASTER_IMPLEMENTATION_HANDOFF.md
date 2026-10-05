# Master handoff — P17-A explicit withdrawal and War termination

**Checkpoint:** P17-A — Explicit Withdrawal Demand and Explicit War
Termination. **Promoted architecture base:**
`codex/architecture/world-identity-projection` at
`ffd75652d89d862b83d634868c560f8540869b89` (P17 direction content
promoted at `6a1b3ef4d0ea32c109fcfb434d3ee00d05cf589a`).
**Reviewed technical content:**
`4088d3485f360144cc60ee301cc4ecb2550ef5bc` in
`codex/architecture/p17a-technical-design`; [design](../design/PHASE17A_EXPLICIT_WITHDRAWAL_TECHNICAL_DESIGN.md)
and [independent PASS review](../design/PHASE17A_EXPLICIT_WITHDRAWAL_TECHNICAL_DESIGN_REVIEW.md).
**Implementation code audited:** promoted `codex/phase16/canonical` at
`75a27d7ac97e66c2762835ccea7950a945c2f20d`; P12 canonical was
`a6572ab3d4330d81edb334ae8b4c84ca5e6b173e` at handoff preparation.
Refresh all implementation refs, States, tests and shared-worktree ownership
before dispatch; these SHAs are evidence, not a request to reset active work.

## Ready work and exact boundary

The reviewed design is `READY_FOR_IMPLEMENTATION` as a bounded contract.
Master may create an isolated implementation candidate when a safe integration
window exists. General Architect does not implement P17-A. One existing P7
War owns two strategic Faction participants and A's one actual demand; B's
bound P16-selected force leaves Hex H by one validated P16 crossing and
finite supply debit. Goal achievement is derived from P16's retained receipt
and leaves War Active. A later explicit controlled GM concession by B ends
the bounded War with reason and full accepted-input provenance, whether or
not A's goal was achieved. One goal/two participants/one hop are fixture
limits. No War winner, pressure meter, territorial control or diplomacy.

Implement the exact profile/admission and owner seams in the design:

1. Evolve **existing** `PersistentWarStore` and record; keep P7 legacy Wars
   valid. Add distinct strategic participant/goal IDs, initial-only atomic
   configuration, relation validation, frozen P17 force bindings, raw-end
   rejection and one reasoned terminal commit. Capture/validate the complete
   P7+P17 War record and owner revision; preserve clone/invariants.
2. Extend the **existing** P16-A receipt/state with trusted accepted-request
   origin/authority for this profile, atomically with its position/supply
   commit. Leave standalone P16-A semantics intact. The trusted authority ID
   exists in initial immutable P17 composition/config capture before inputs.
3. Add only the explicit `P17AWithdrawalWar` runtime composition/operations
   and coherent immutable observation. It requires matching P16 state and a
   bound scenario/GM capability; P17 state rejects every other composition,
   including `Standard`. Clone Faction before P17 War to validate against
   resolved owners; preserve downstream political composition order.
4. Prove P12 `UnityBootstrap-Daily-v1` exclusion/fail-closed rejection for
   P17 state with and without its matching P16 owner. Serialize runtime/P12
   admission edits with active P12-B work; do not expand its save profile.

## Validation and integration gate

Use focused EditMode tests for valid crossing/goal/Active/concession/Ended
flow, concession after failed crossing, wrong force/source, stale revisions,
guard and authority rejection, duplicate IDs/requests, raw-end bypass,
unchanged state on every rejected mutation, clone and deterministic exact
capture/staged validation. Assert accepted crossing and concession provenance
survives clone, and Battle result alone changes neither War nor goal/control.
Run affected P7/P8/P16/P12 regressions and the selected-profile negative
admission test. Documentation review did not run Unity tests. A future Lab
demonstration is `FOLLOW-UP_DEMONSTRATION`; domain code promotion does not wait
for its host, while formal Phase closure should prefer a runnable scenario
when the approved non-Unity application/read seam exists.

Shared hotspots are `PersistentConflictWarBattleContracts.cs`,
`PersistentConflictWarBattleStores.cs`, `ArmedForceSpatialPosition.cs`,
`SimulationRuntime.cs`, runtime admission/P12 hooks and affected tests.
Classify active work as `PARALLEL WITH ISOLATION` for separate files and
`MUST WAIT` for the same runtime/admission hotspot until ownership is clear.
P12 remains an independent execution front; P17-A must not slow or mutate it
outside a serial integration window. Revalidate design assumptions if the
implementation base advances, then run independent code review and the
normal human checkpoint-promotion gate. P17-A does not join
`UnityBootstrap-Daily-v1` by implication.

**No remaining product/architecture choice** is required for this bounded
implementation. Territorial goals, occupation, attrition decisions,
multi-goal/coalition Wars, ceasefire, capitulation, peace, P20 Activity and
P13 fork remain later consumers.
