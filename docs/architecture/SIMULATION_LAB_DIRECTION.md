# Simulation Lab — human demonstrability and Unity independence

**Status:** canonical architecture/planning direction; unnumbered Lab foundation
`READY_FOR_TECHNICAL_DESIGN`, not `IMPLEMENTATION_READY`.
**Reviewed architecture base:**
`f6924e63d8e5731da1d33021d0361e7defe6dad7` on
`codex/architecture/world-identity-projection`. **Scope:** documentation only;
no Lab, UI, runtime API, checkpoint implementation or Phase closure is approved
by this record.

## Product meaning

Automated validation and human demonstration answer different questions.
Tests/review establish invariants and regressions; a runnable scenario lets a
person observe and experiment with the capability. A future substantial
behavior checkpoint should assess a minimal scenario, with the three gate
classifications in `../EXECUTION_MODEL.md`. A text view is enough for numeric
state and transitions; graphs help topology, movement and branches. There is no
per-checkpoint GUI mandate and no retroactive gate on closed work.

The Lab would be a small, cumulative scenario runner and inspector that uses
the same Simulation execution and read paths as other clients. It may start as
a console/.NET host; exact packaging is technical design, not settled here.
One scenario creates an approved world, submits supported intent through the
appropriate application authority, advances it through Simulation time and
shows approved observations and rejections. It does not own an alternative
world, duplicate rules, reach into mutable Stores, bypass command/admission
guards or treat debug snapshots as domain API. It is not a production game UI,
mod loader, persistence format, whole-World exporter or generic scripting
engine.

```text
scenario input / Lab host     Unity host       future external client
          \                      |                    /
           approved Simulation application boundary
           genesis/composition · commands · temporal execution
                 authoritative Simulation domain
           capability readers / perspective-safe observations
          /                      |                    \
   Lab inspection            Unity view       approved projection adapter
```

This is the target relationship, not a claim that a portable assembly already
exists. Scenario setup does not turn an authored fixture into runtime authority;
post-start inputs and state changes use the ordinary authorities. External
causal inputs need the ordering, logical boundary, authority and payload that
future reconstruction requires. A Lab command rejected by domain admission
cannot mutate World Truth. A truthful display must distinguish authoritative
facts, actor Knowledge, unavailable reads and unsupported concepts.

## Canonical evidence and available boundary

Architecture §85 already places clients behind `WorldCommand`/validation and
§91B requires immutable capability readers and coherent authoritative cuts.
The current implementation canon (`codex/phase12/canonical` at
`a6572ab3d4330d81edb334ae8b4c84ca5e6b173e`, inspected read-only) has
`WorldCommandService`, `WorldCommandFoundation`, FR-B/FR-C factual reads and
the bounded WX-D Faction export. These are useful semantic seams. FR-B currently
serves the `UnityBootstrapDailyV1` admission profile, and WX-D exports Faction
facts only; neither is a general execution host or general reader for all
example scenarios.

No honest Unity-independent application host is delivered yet. In current
code, `SimulationRuntime` references `UnityEngine`; its factual-read admission
profile captures the Unity Start thread. `TesteSimulacao` is a `MonoBehaviour`
using authored `SimulationConfigData` and runtime composition; the selected
genesis fingerprint consumes that Unity-authored configuration. Core command
registration also resolves Unity object definitions. Other systems on likely
demo paths still reference Unity. A CLI that merely parses exported JSON would
not prove domain execution portability. The cleanest *existing semantic*
boundary is the command/temporal-authority plus capability-reader boundary;
the executable non-Unity seam must be extracted and validated on one chosen
path. This record does not assert that moving one file is sufficient or require
the whole Unity project to be extracted at once.

`Simulation-External` main at
`0ce8403ba05f778db6850f566a974a4c56cf4edb` was inspected read-only.
World Explorer and `world-io` consume portable, read-only World Exchange v1/v2
artifacts; v2 has explicit collection coverage. Its docs explicitly exclude
live mutation, save and bidirectional sync. It is a good display consumer for
approved exported facts, but an interactive Lab should use a distinct
Simulation-side execution host. A later integration may share a public
projection/schema or show a Lab-produced artifact in Explorer; the factual
exchange remains a read model, not an input command or world authority.

P19's extension surface can learn from the same real application seams, but
the Lab can be an official client without a public mod loader. Neither P19 nor
P12 full closure is a blanket prerequisite. A future remote client, IPC or
general extension API needs separate technical and product review; this
proposal chooses no transport, security regime or loader lifecycle.

## Smallest supporting capability and first consumer

Create a separately designed, unnumbered **Simulation Lab foundation** only
when a concrete demonstration is selected. Its minimum reusable pieces are a
repeatable scenario start path, one supported operation/advance path, and an
approved observation path with human-readable before/after/failure output.
The design must name the exact chosen profile, authoritative implementation,
non-Unity build boundary, command/advance ordering, reader coverage and
deterministic fixture. Extract Unity dependencies only along that path; keep
Unity as another consumer. Future scenarios add capabilities to this foundation
instead of cloning a parallel simulation or creating one disposable runner per
checkpoint. There is no generic Lab menu or domain-wide inspection API in the
foundation.

**Recommended first useful demonstration:** finite-source depletion using the
already promoted P14-B domain capability. One small selected world shows the
source's starting reserve and stock, advances through production, shows reserve
decreasing and stock increasing, then shows no further output at exhaustion.
A readable reserve/stock capability and non-Unity composition remain
prerequisites for the Lab scenario; the domain promotion did not deliver that
scenario or create a retroactive demonstration gate. A simpler delivered
operation may serve as the initial host
smoke test, but it must be labeled with the behavior it actually proves.

The roadmap lists incremental P10/P14/P15/P16/P20/P12/P13 consumers and their
individual capability gates. The Lab is a **validation/consumption opportunity**
for each, not an upstream hard dependency. A checkpoint can classify a scenario
`REQUIRED_FOR_CHECKPOINT` when the reviewed scope and paths support it, or
`FOLLOW-UP_DEMONSTRATION` when the domain work should land first. For a later
observable Phase closure, prefer an actual demonstration if practical; justify
an exception without rewriting old completion history.

## Dependency and impact assessment

```text
Architecture §§85, 85A, 91B and selected real domain capability
        → bounded Lab technical design and non-Unity seam proof
        → Lab foundation implementation/review (separate future authorization)
        → one scenario consumer at a time

P14-B promoted (satisfied) + coherent reserve/stock read + non-Unity execution
        → recommended finite-source demo
P10-B/P15-A/P16-A promoted (satisfied) + appropriate read/execution path
        → their optional/required scenario per reviewed checkpoint gate
P20-A promoted synthetic behavior (satisfied) + applicable read/execution path
        → possible shared-activity demo
P20-B label/scope mismatch across architecture Roadmap and Phase 20 State
        → reconcile before attaching a P20-B Lab scenario
P12/P13 selected capability + appropriate read
        → corresponding optional/required scenario per reviewed checkpoint gate
P19 loader                               no dependency for Lab foundation
full P12/P13                             no blanket dependency for Lab
```

The policy addition is forward-looking. Closed phases and delivered P8/P9/P11/
P14-A/P18/P20-A, bounded WI-A/FR-B/FR-C/WX-D, and now promoted
P10-B/P14-B/P15-A/P16-A and the bounded P20-B implementation do not need
reopening or revalidation merely because they lacked a human demo. The
architecture Roadmap at this base names P20-B as joint civil travel, while the
current Phase 20 State names its promoted P20-B as Daily-profile empty-owner
admission/census for that travel owner. This is an unresolved checkpoint
identity/scope discrepancy, not a delivered joint-travel scenario or a license
to relabel either contract here. P20-A is the promoted synthetic shared-activity
behavior and can be evaluated independently for a Lab scenario. Planning a
P20-B Lab scenario waits for the owning architecture/State reconciliation.
Active P12-B and
other current Master candidates are not interrupted or invalidated; classify
each such candidate against its actual owning branch when architecture is
promoted. Subsequent checkpoint designs or formal Phase closure proposals
should assess demonstrability at their own gate. There is no reintegration
request for the active Master. The proposed
Lab host may later collide with `SimulationRuntime`, bootstrap/genesis, P12
admission, command registration or readers; its actual implementation must
serialize those hotspots with whatever implementation front is then active.

No new product choice is needed for this direction. The first Lab
technical design must still select one supported scenario/profile and prove a
viable non-Unity boundary; any scope that changes gameplay, public API or
checkpoint obligations beyond this record requires its own review. Normal
architecture-canonical promotion is recorded in `../ARCHITECTURE_STATE.md`;
future Lab technical design and implementation retain their own gates.
