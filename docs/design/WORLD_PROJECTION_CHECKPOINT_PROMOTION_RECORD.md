# World identity / factual projection checkpoint promotion and dependency refresh

**Date:** 2026-10-01. **Target:** `codex/architecture/world-identity-projection`. **Prior architecture tip:** `f27954af6d880df123736027fd55e86874d6de68`. **Approved candidate:** `bd0d7979d556da12fd69046087d75d0a93b1803d`, fast-forwarded without conflict. The user's explicit approval in this task authorizes this architecture-canonical promotion; it does not authorize runtime implementation or Phase closure.

The candidate adds only `docs/ROADMAP.md` and five bounded design/review documents. Its independent technical review found WI-A and FR-B `READY_FOR_IMPLEMENTATION` and FR-C and WX-D `WAIT_DEPENDENCY`; the reviewer verified the exact final candidate tip. `git diff f27954a..bd0d797 --check` passed and no executable content changed, so Unity tests were not run for this promotion. This record, architecture §§91A–91B and Roadmap describe the canonical planning result; no Phase State is marked as delivered.

## Execution DAG and readiness

```text
WI-A World identity ─────────────────────────────┐
FR-B factual read foundation → FR-C Faction ────┼→ WX-D producer
                                                  ↑
                         External collection-coverage contract
```

- **WI-A:** `READY_FOR_IMPLEMENTATION` as an independent implementation candidate. It must preserve private genesis draft, successful callback/finalization and healthy-scope close before public identity, no identity after failed genesis, and typed compatible P18 handoff. Its `TesteSimulacao`/`SimulationRuntime` integration is serial with P12-B hotspot work.
- **FR-B:** `READY_FOR_IMPLEMENTATION` as an independent implementation candidate. Its first coherent profile is explicit `UnityBootstrapDailyV1`; relevant Faction/Person Store mutators must share read admission. It supplies immutable capability-oriented records, never live Stores or P12 snapshot DTOs. Its runtime integration is serial with P12-B and WI-A where files overlap. WI-A is not a hard prerequisite for FR-B.
- **FR-C:** `WAIT_DEPENDENCY` on a promoted FR-B **capability**, not merely its design. Do not start its implementation before FR-B promotion.
- **WX-D:** `WAIT_DEPENDENCY` on promoted WI-A and FR-C capabilities and an approved Simulation-External collection-coverage contract. Required v1 arrays cannot encode unsupported versus known-empty truth. No Simulation-side reinterpretation, TypeScript dependency in the domain, or first portable artifact is authorized while this remains open.

P9 and P18 are architectural-alignment inputs, not reopened phases. P12-B is optional infrastructure reuse for FR-B, never a blanket dependency or proof of a global coherent cut. P12-C and future P12-A must preserve WorldId on same-branch continuation; P13 must issue a new WorldId with source and actually simulated fork-boundary provenance. P12-A/full P12, P13 mechanics and P19 are not prerequisites for the initial live factual reader/producer path. World Exchange types remain outside Simulation domain contracts. The first producer is a later dedicated integration project, not a second World Truth owner.

## Current-canonical and hotspot audit

At refresh, remote `codex/phase12/canonical` is `f913dd088f71b74cfab7c1bd8a1a79b7ce9a29ea`, ahead of the P12 base `43dba1b5d16cba1558c4c39239f3cd0d4c669958` inherited by this thematic architecture branch. Its added MoneyAccount census/provider and narrow `SimulationRuntime` census-registration/property additions do not change WorldId publication, Faction/Person mutator paths, or the reviewed factual-read semantics. Classify the WI-A/FR-B **design assumptions** as `UPSTREAM_IRRELEVANT` for that bounded promotion; implementation must still rebase/integrate against the then-current P12 canonical and rerun affected tests (`REVALIDATE` at integration). This architecture promotion does not merge or modify the active P12-B branch, its dirty worktrees, or its State. The selected profile's P12-B bootstrap/day admission is partial; FR-B must provide its own exact Faction/Person writer admission rather than calling that census a complete global epoch.

Worktree scheduling: WI-A and FR-B may have separate implementation worktrees, but no simultaneous writer may own `SimulationRuntime` or the bootstrap publication seam alongside active P12-B work. Isolated contracts/analysis are `PARALLEL WITH ISOLATION`; shared hotspot edits and final integration are serial. FR-C and WX-D are `MUST WAIT` for their named hard dependencies. The external coverage issue is an External-owned contract gate, not a request to alter Simulation-External in this promotion.
