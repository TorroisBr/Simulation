# Master Phase Orchestrator handoff — P12 capability DAG

**Canonical architecture baseline:** `ee8cca1010c8f5f37928e81849b6489bffd6a318` (promoted 2026-10-03). **P12 delivery baseline at handoff:** `origin/codex/phase12/canonical` `54fc23b89cb13598dd184a2b5b6bacf9dc23b0a7`. Re-read remote refs, owning States and code at the next run; these SHAs are evidence, not a frozen future ready set. The accepted architecture decision is `PARTIAL_PARALLELIZATION_APPROVED`, detailed in [the audit](P12_CAPABILITY_DAG_AUDIT.md).

## Ready set and actual blocks

`EXECUTABLE_DESIGN` means bounded architecture/technical design may be dispatched. It does not authorize a runtime implementation checkpoint. `READY_FOR_PRODUCT_SCOPE_DECISION` means the semantic entry is open but the first slice needs a human product choice. Neither label changes Phase State.

| Track | Classification now | Allowed next work | Implementation gate |
|---|---|---|---|
| P12-B | `IN_PROGRESS` | Continue its accepted owner, operation and admission work in its active stream. | No capture eligibility or complete profile yet; do not disturb its worktree. |
| P12-C–G / P12-A | `WAIT_DEPENDENCY` | Maintain the accepted B→C→D/E→F→G→A chain and current design evidence. | Promoted predecessor capabilities, complete included-owner export/hydration, profile inventory and parity as specified in P12 Brief/State. |
| P10 next checkpoint | `READY_FOR_PRODUCT_SCOPE_DECISION` | Choose a bounded local-generation/pre-start authoring consumer. | P10-A is promoted; no next checkpoint or implementation scope approved. Later save coverage is profile-specific. |
| P13 retention / causal-input strategy | `EXECUTABLE_DESIGN` | Design recoverable initial-state, input, mutation and checkpoint/retention strategy without promising fork delivery. | Authoritative reconstruction/fork remains `WAIT_DEPENDENCY` on complete validated continuation of the chosen world/profile, recoverable causal history, compatible execution and new WorldId/provenance. For the accepted daily profile, relevant P12 B–G/A parity is required. |
| P14 next checkpoint | `READY_FOR_PRODUCT_SCOPE_DECISION` | Choose source, transfer, transport or other bounded follow-on. | P14-A promoted; a new scope and reviewed design are still required. Route/time/crew consumers add their actual P8/P18/P20 edges. |
| P15 first runtime construction/founding | `READY_FOR_PRODUCT_SCOPE_DECISION` | Select the first factual creation consumer, initiator and cost boundary; then design its owner/atomicity. | No implementation checkpoint yet. Requires relevant promoted P8 anchor and domain mutation capability; P14 only for chosen material cost, P18/P20 only for chosen timed/crew work. No full-P12 or P13 implementation prerequisite. |
| P16 first military movement/logistics | `READY_FOR_PRODUCT_SCOPE_DECISION` | Select first force movement, supply and command scope; then design operational boundary. | No implementation checkpoint yet. P7 force/Battle, relevant P8 passage, selected P14 supply capability and P18 if duration-based. P14-A alone does not prove force-held supply or replenishment. No full-P12 prerequisite. |
| P17 Strategic War | `DEFERRED` | Keep domain questions explicit. | P16 operational facts, territorial/political authority and War goal/pressure/control/occupation/termination decisions, not P12 closure. |
| P18 bounded temporal foundation | `CLOSED` within approved A–D scope | Reuse only relevant promoted timeline/lifecycle capability. | A future intraday save/fork profile requires its exact temporal-state/input integration. |
| P19 public-extension architecture | `EXECUTABLE_DESIGN` for selected real consumers | Study a bounded semantic public surface using promoted P9/P10/P18/P20 consumers. | Loader/runtime implementation is `DEFERRED` pending chosen scope and design. Durable mod-owned state/retrofit needs applicable continuation and historical compatibility, not blanket P12 closure for all P19 work. |
| P20 next checkpoint | `READY_FOR_PRODUCT_SCOPE_DECISION` | Choose a bounded real shared-activity consumer beyond promoted synthetic P20-A. | No broader implementation checkpoint approved. A future supported save/fork profile needs exact participant/temporal state. |

WI-A, FR-B, FR-C and WX-D are already promoted in bounded scopes on the P12 canonical integration path; Simulation-External World Exchange v2 `collectionCoverage` is at `0ce8403ba05f778db6850f566a974a4c56cf4edb`. They enable the factual producer without P12 save closure. None supplies P12 capture or P13 reconstruction.

## First product-scope choices to present

These are candidate scopes, **not** selected contracts. Request only the product choices; owner interfaces, identity allocation and test shape belong to subsequent technical design and review.

**P15 — runtime construction/founding.**

| Candidate | First consumer and owner boundary | Logical mutation and dependencies | Broader behavior excluded |
|---|---|---|---|
| A — one structure at an existing factual Location/City anchor (smallest) | One authorized actor/command establishes a selected structure type. Spatial authority owns placement/anchor; the selected domain owner owns the structure's factual identity/state. | One validated committed creation boundary, or an explicit start/completion pair only if the product requires duration. No material cost unless selected; P8 anchor is required, P14/P18/P20 are conditional. | General building catalog, settlement founding, workforce, automatic construction, runtime regional expansion. |
| B — found one settlement at an existing unoccupied factual Location | City/settlement authority owns the new entity and its domain facts; spatial authority owns its anchor. Initiator/authority and immediate population/property consequences require product choice. | One coherent cross-domain founding boundary, with relevant P8 and any chosen P14 cost; timed/crew form conditionally consumes P18/P20. | General settlement generator, retroactive simulated history, polity/control system, automatic migration/economy. |

The minimal human choice is **structure or settlement**, **who may initiate it**, and **whether the first slice has a material cost or duration**. Technical design must settle the exact owner API and atomic commit. Either candidate retains created identity, boundary, causal command/order, and any transfers for future exact export/hydration and P13 reconstruction. Before domain promotion, prove exclusion from `UnityBootstrap-Daily-v1` composition or a tested fail-closed profile admission hook integrated serially with P12-B.

**P16 — military movement/logistics.**

| Candidate | First consumer and owner boundary | Movement/supply dependency | Broader behavior excluded |
|---|---|---|---|
| A — one commanded force relocation across one validated passage (smallest) | ArmedForce authority owns committed position/order/progress; a selected force-supply owner holds finite carried stock. | A validated one-hop boundary with explicit supply debit and failure on insufficient supply. P7 and relevant P8 are available; item/supply semantics need a reviewed P14-compatible capability beyond assuming P14-A market stock is force stock. P18 is needed only if duration is promised. | Strategic control, Battle result, occupation, pathfinding network, automatic resupply. |
| B — timed route movement with replenishment | Force authority owns plan/progress; logistics authority owns transfers/replenishment. | Relevant P8 traversal, P14 transfer/supply capability and P18 timeline/lifecycle. More integration than A. | War termination, territorial title, ordinary Person travel-party semantics. |

The minimal human choice is **one-hop or timed route**, **how the first force obtains finite supply (authored carried stock or transfer from a factual source)**, and **who may issue its order**. Technical design must define passage validation, consumption timing, stale order rejection, deterministic order and exact retained state. No Battle, War outcome or territorial control follows from movement. Before domain promotion, prove selected P12 profile exclusion or tested fail-closed rejection, with serial P12-B integration if the owner can compose there.

## Parallel scheduling rule

P12-B continues autonomously on its own canonical stream. P13 strategy and bounded P19 public-surface design are independent document tracks. P10/P14/P15/P16/P20 scope preparation can proceed in isolated worktrees, but no implementation worker starts until product scope, checkpoint contract, technical review and promoted prerequisites actually pass. Treat `P12 open` as a fact about Save delivery, **not** a phase-number lock on all later work. A proposed candidate or contract is not a promoted runtime capability.

Use `PARALLEL WITH ISOLATION` for separate design/domain tracks and `MUST WAIT` for unmet capability or product gates. Serialize integration at `SimulationRuntime`, bootstrap/profile admission, spatial authority, force/material stores, diagnostics and persistence composition when their ownership windows overlap. Do not write to the active P12-B worktree, infer P12 capture from factual reads, or silently add new owners to the accepted save profile. Recompute the DAG after each canonical advance and target revalidation to the contract or code actually affected.
