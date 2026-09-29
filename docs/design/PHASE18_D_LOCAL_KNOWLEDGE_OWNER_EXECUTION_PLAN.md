# P18-D — Actor-Local Knowledge Owner Execution Plan

**Checkpoint:** P18-D — Bounded Consumer and Daily Compatibility Integration
**Canonical P18 base:** `9e790c59e14ca7f7ed195c0e6267e10f3cd039d7`
**Consumer integration before this owner:** `5d7eb2687c3866fb2399faf8b366a299484d8dd4`
**Initial implementation candidate:** `codex/phase18/P18DLocalKnowledgeObservation` at `15b201a`
**Exact-tip hardening candidate:** `codex/phase18/P18DLocalKnowledgeObservation` at `30b73575cfd84f3d4aef1e49d9c1fccfa8efb511`, based on `ea05af4181ef7ff7879f356d4eaa78070ff0c6f5`
**Architecture/alignment baseline:** architecture `c285466`; intraday/extensibility `4b6dd1d`; multi-participant activity `c285466`.

## Objective and contract

Provide the actor-local daily observation operation required by
`docs/design/PHASE18_D_TECHNICAL_DESIGN.md` §4. At boundary activation, the
provider freezes the exact runtime-roster membership and order and creates one
stable step for each `NpcRuntime`. `NpcRuntimeId` identifies the runtime owner;
the optional backing `PersonId` is retained in the step and receipt. Ordinals
express execution order only; they are not receipt identity.

Each actor step is owned by a serialized `NpcLocalKnowledgeObservationRuntime`
held by that `NpcRuntime`. It prepares spatial discovery and the optional
merchant market observation together, then installs both knowledge changes
with one actor-local occurrence receipt. This preserves the legacy order of
discovering `CurrentLocation`, discovering the merchant `CurrentCity.Location`,
and then capturing ordered market items and liquidity. Existing freshness,
source priority, item ordering, observed stock/price, liquidity, and absolute
day semantics remain in their domain owners.

The first prepared snapshot is retained by the receipt. A committed replay
returns that result without resampling the market or applying either knowledge
change again. Current location/city object bindings, observed market values,
actor identity and both knowledge-owner revisions are checked before install.
If the source changes after preparation, the actor's spatial and commercial
knowledge remain untouched and the step can be prepared again against current
truth. No actor decision signal is added by this observation operation.

## Scope and limits

This candidate adds the owner capability and its daily-boundary step provider;
it does not wire the provider into `SimulationRuntime`, define an intraday
profile, or complete P18-D. The legacy `RefreshLocalKnowledgeAndShare` path
remains unchanged. Commercial sharing continues after local observations as a
separate owner pass. Demography, merchant trade-state advancement, temporal
SellGoods composition, and the intraday daily compatibility adapter remain
separate P18-D work.

## Validation and review

The exact code tree at `15b201a` passed:

| Suite | Result | Artifact |
|---|---:|---|
| `P18DLocalKnowledgeObservationTests` | 3/3 | `EditMode-20260928-225542-a7d2150347fc461abc55ca671265991b.xml` |
| `CommercialKnowledgeTests` | 19/19 | `EditMode-20260928-225713-47fbc10dc479498c9ae8f4ffb7b9aefd.xml` |
| `CommercialKnowledgeSharingTests` | 10/10 | `EditMode-20260928-225726-ff2aca2ada744ab79794fb9d41387598.xml` |
| `CoreRuntimeTests` | 11/11 | `EditMode-20260928-225739-a0177c3549634409a2668477a92c03b2.xml` |
| `P18DDailyBoundaryStepProviderTests` | 6/6 | `EditMode-20260928-225754-9627bad07b4c4550ba0c4336f616f126.xml` |

`git diff --check` passed. Independent exact-tip implementation review passed
at `15b201a` against its exact parent `5d7eb26`. The reviewer confirmed the
actor-owned combined spatial/commercial prepared install, current-truth
revalidation, receipt replay, optional `PersonId`, stable runtime identity,
frozen roster order, and preserved legacy observation ordering. That review
identified two non-blocking hardening opportunities; both were implemented in
`30b7357`: preparation now requires the exact active frozen manifest descriptor
at its ordinal, and a repeated `TryCommit` on a prepared object with a matching
retained receipt returns success without reinstalling either child knowledge
state.

The exact code tip `30b73575cfd84f3d4aef1e49d9c1fccfa8efb511` passed
independent exact-tip re-review against parent `ea05af4181ef7ff7879f356d4eaa78070ff0c6f5`.
Focused validation on that code tip passed:

| Suite | Result | XML | Log |
|---|---:|---|---|
| `P18DLocalKnowledgeObservationTests` | 4/4 | `Temp/ValidationResults/EditMode-20260928-232706-a0987a76152d40409da04faf3b210358.xml` | `Temp/ValidationResults/EditMode-20260928-232706-a0987a76152d40409da04faf3b210358.log` |
| `P18DDailyBoundaryStepProviderTests` | 6/6 | `Temp/ValidationResults/EditMode-20260928-232013-e30fe171f9f544e2b44d175519595f00.xml` | `Temp/ValidationResults/EditMode-20260928-232013-e30fe171f9f544e2b44d175519595f00.log` |

`git diff --check` passed for the hardening change. These focused results do
not replace the full integration EditMode, official Smoke, and complete P18-D
review gates.
