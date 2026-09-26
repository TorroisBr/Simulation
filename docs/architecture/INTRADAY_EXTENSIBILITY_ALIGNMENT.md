# Intraday and extensibility architecture alignment — 2026-09-26

## Requirement and verified baseline

The user explicitly requests architectural/documental integration of intraday
simulation, local player-owned code moddability and extensible generation,
preserving settled systems. No implementation is authorized by this record.

`codex/phase8/canonical` local and remote were verified at
`ed7a40a86a6a16e9f4fda75703470c38135fda0e`, with a clean canonical worktree.
Changes are prepared in an isolated architecture worktree/branch. Historical
Phase 5–7 records and existing implementation/design candidate branches are
preserved. This record is subordinate to the amended canonical architecture.

## Architectural integration and phase placement

- Replace the permanent daily actor-tick claim in architecture §11 with one
  logical intraday timeline and availability-driven decisions. Daily cadences
  remain valid per-domain and in explicitly bounded legacy profiles.
- Introduce **Phase 18 — Intraday Temporal Execution v1**, with P18-A timeline/
  scheduler → P18-B activity lifecycle → P18-C actor availability → P18-D bounded
  consumer/daily integration. Its placement is before claims of complete timed
  gameplay/intraday save, not after War merely because its ID is 18.
- Introduce **Phase 19 — Code Mods & Public Extension Surface v1** as deferred
  dedicated platform work. Relevant concrete consumers and stable exposed
  contracts gate entry; component edges to P9/P10, P18 and P12/P13 are conditional.
- P9/P10 acquire a dependency-aware pipeline/contribution contract now, without
  implementing a loader or an unbounded generation framework. Existing-world
  optional retrofit remains separate from new-world participation.
- P11 retains its bounded trusted SellGoods proposal; the daily turn adapter
  is transitional. P12/P13 inventory intraday and extension causality when in
  supported scope. P14/P15/P16 depend on P18 only for actual timed consumers.

Existing phase/checkpoint IDs, core ontology, spatial authority, Knowledge/
execution separation and delivered daily semantics are preserved. No Sleep,
Dreams, theft, needs, Wind & Sail, worldgen system or mod platform is implemented.

## Constraints now versus deferred capabilities

| Constraint now | Dedicated later work |
|---|---|
| Domain/application logic independent of Unity presentation where practical; Unity official UI/renderer | Core extraction/alternate presentation transport, with Unity-hosted IPC an acceptable fallback |
| Real code mods/new mechanics permitted conceptually; world owned by the local player | P19 API/loader, module lifecycle, packaging and supported runtime integration |
| No adversarial player/mod authorization or anti-cheat | No such platform is required; domain coherence/validation remains |
| Semantic hooks instead of polling; deterministic composition where independent contributions are actually needed | Public registries/policies/modifiers/pipelines designed from real consumers, not speculative hooks everywhere |
| Mod-to-core promotion without conceptual rewrite; optional official expansions ideally share public surface | Concrete API/versioning and supported expansion/module contracts |
| Ordered generation dependency graph and recoverable causal provenance | Exact stage APIs, schemas, generation algorithms, loader adapters |
| Generated outputs are historical state; no implicit stage rerun on install | Optional explicit module retrofit/migration and compatible state restoration |
| Retain exact temporal/input causality when introduced | P18 implementation and P12/P13 storage/hydration/reconstruction |

## Evidence and candidate reclassification

Canonical `SimulationRuntime.TryAdvanceDay` advances the day before executing
`AdvanceDayAfterClockAdvance`; that method visits each eligible NPC for one
`EvaluateAction`/execution and then advances legacy travel. This is delivered
daily behavior, not an existing intraday scheduler. P8-E's tracked design uses
explicit day/input operations and expressly excludes automatic `AdvanceDay`
integration. The separate P11 technical design uses a day/roster/transition
application key and the ordinary actor turn. These are the concrete seams to
adapt; geography and merchant transaction authority do not need redesign.

Inspected candidate refs/worktrees were clean. Their existence is not proof of
active execution or promotion; the orchestration task was not active at this
inspection. Do not rewrite candidate branches from this documentation task.

| Track at inspection | Classification for this change | Required next action |
|---|---|---|
| Closed P5–7; promoted P8-A/B/C | `UPSTREAM_IRRELEVANT` to delivered bounded behavior | Preserve evidence and semantics; later temporal adapters get their own tests. |
| P8-D integration, `a670289` (`codex/phase8/P8DKnowledgeRoutePlanIntegration`) | `REVALIDATE` | Confirm day-based Knowledge/estimate policies remain explicit profile semantics, no permanent daily travel limit, and promotion/composition time gates remain coherent. No route-store redesign required. |
| P8-E design, `4b7127f` (`codex/phase8/P8ETechnicalDesign`) | `REVALIDATE` | Preserve explicit-operation slice; record P18-D dependency for automatic intraday execution. Do not invent duration laws or migrate its fixture retroactively. |
| P11 input store, `0a32e86` (`codex/phase11/ActorChoiceStore`), and technical design `7da973c` | `REVALIDATE` | Preserve stable capture/disposition lifecycle; review day/roster adapter as legacy and migration to availability/time boundaries. Intraday integration must use exact logical instants/order. Store is not discarded merely for using current profile. |
| P9 entry proposal, `6c16756` (`codex/phase9/GenesisEntryArchitecture`) | `REVALIDATE` before further approval | Incorporate decided pipeline/stage/contribution and installation boundaries; former freedom to choose an opaque monolith is superseded. No implementation capability invalidated. |
| P12 entry proposal, `a110cf4` (`codex/phase12/ContinuationEntryArchitecture`) | `REVALIDATE` before further approval | Extend inventory/support matrix to temporal and module state; retain explicitly bounded daily saves without claiming intraday coverage. |
| P14 entry update, `956b7a0` (`codex/phase14/MaterialFlowEntryDecision`) | `REVALIDATE` for temporal assumptions only | Static/local source truth remains independent; timed shifts/flow consume relevant P18 capability. |
| P10/P13/P15/P16 planning entries | Dependency refresh | Incorporate the conditional temporal/generation/extension edges in their Briefs; no existing implementation cancelled. |
| P17 strategic War planning | No contract change | Remains deferred and consumes P16; no blanket P18/P19 gate added. |

No complete phase or promoted capability is `INVALIDATED`. The permanent
one-action-per-day architectural assumption is superseded for future intraday
work. Pre-refresh candidate approval is not sufficient until its targeted
impact review is recorded. A later actual diff may require `REINTEGRATE` or
`INVALIDATED` if it freezes a conflicting contract; this record does not
prejudge such uninspected future code. No task was dispatched or resumed.

## Reconstruction and validation boundary

Recover logical instants/calendar, availability, active commitments/lifecycle,
pending work or reconstruction inputs, stable IDs and causal ordering/sequences,
versions/configuration/content and random context. Generation contributors and
module outputs/state/retrofit inputs participate when authoritative. Compatible
historical daily execution is not relabeled as intraday history. No storage or
mod schema is selected here, and no later phase can recreate discarded causality.

This change requires document consistency/dependency review and `git diff --check`.
It changes no executable content, so Unity runs are not required. Later P18
runtime integration requires targeted, regression and long-run validation;
technical design and independent candidate review remain mandatory.

Independent documentation review and targeted re-review passed. The review
checked requirement coverage, phase/DAG boundaries, reconstruction sensitivity
and scope preservation against this baseline. Its two minor clarity findings
(duplicate P12 inventory bullet and an unqualified daily-travel sentence) were
corrected; no blocker, major or minor remains. No Unity tests were run for this
documentation-only change. `git diff --check` passed.
