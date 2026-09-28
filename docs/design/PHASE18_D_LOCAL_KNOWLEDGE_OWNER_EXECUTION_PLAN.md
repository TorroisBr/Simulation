# P18-D — Actor-Local Knowledge Owner Execution Plan

**Checkpoint:** P18-D — Bounded Consumer and Daily Compatibility Integration
**Canonical P18 base:** `9e790c59e14ca7f7ed195c0e6267e10f3cd039d7`
**Consumer integration before this owner:** `5d7eb2687c3866fb2399faf8b366a299484d8dd4`
**Implementation candidate:** `codex/phase18/P18DLocalKnowledgeObservation` at `15b201a`
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

`git diff --check` passed. Independent exact-tip implementation review is
pending. These focused results do not replace the full integration EditMode,
official Smoke, and complete P18-D review gates.
