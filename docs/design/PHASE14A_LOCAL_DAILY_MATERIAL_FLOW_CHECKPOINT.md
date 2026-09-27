# P14-A — Local Daily Material Flow v1

**Status:** candidate checkpoint contract, pending independent review against
the current canonical base. It is not an implementation authorization, delivery
record, Phase State, or canonical capability.

**Planning base:** Phase 8 canonical `470667d37863384edadb3d93ef64d8004aff46a3`;
architecture baseline `c285466c355103d3637ac165246591b72eb7bda0`. The Phase 8
advance from `77f3e1a47a1e007492a794ea777d681a21a36d09` to `470667d` records
State-only closure and changes no P8 capability. The reviewed
technical proposal carried in
[`PHASE14_TECHNICAL_DESIGN.md`](PHASE14_TECHNICAL_DESIGN.md) originated at
`f31b806` and records its earlier independent design and identity/cardinality
reviews. The current checkpoint review must confirm that this bounded contract
and ordering remain valid against the stated base, including both current
alignment records.

## Objective and closure

Establish one bounded, factual source-to-sink material flow for a manually
authored City: one configured exogenous daily source for one item, the same
City's free population consumption of that item, and the closing aggregate
market balance after the daily economy pass. The economy-enabled policy gates
the pass. The source has no input, reserve, depletion, or transformation.

P14-A closes when the selected profile publishes and applies its source through
domain-owned stock mutation; applies the existing same-City free-consumption
semantics against actual available stock; exposes deterministic source/sink
outcomes and the closing stock; preserves title/custody distinction; and passes
the focused identity, anchor, determinism, overflow, stock-limited consumption,
disabled-economy, and reconstruction-inventory checks listed in the technical
design. The authoritative balance is:

```text
closing stock = opening stock + applied source quantity - actual free consumption
```

This is a daily-profile closure only. It makes no claim about intraday timing,
transport, route flow, market transactions, save/load implementation, replay, or
historical fork implementation.

## Selected profile and authority

- Exactly one selected authored City instance, one configured source, and one
  item are in the proving profile. Additional sources/items/cities need a later
  checkpoint that defines their cardinality and composition behavior.
- The source is exogenous and adds its configured positive daily quantity when
  effective `Economy.Enabled` is true. There are no production inputs, finite
  reserves, extraction, depletion, transformation, or random selection.
- The existing same-City population sink requests free consumption. Actual
  consumption is `min(requested, available stock)`; stock removal and reported
  actual quantity are one domain transition. No account or money mutation is
  part of this profile.
- Settlement owns the source and resulting material. The market store is the
  stock custodian. Market-counterparty identity and purchasing power do not
  represent ownership.
- Source and market/domain stock owners retain mutation authority. The
  coordinator invokes the existing daily order; it does not create a second
  stock mirror or a general production framework.
- A complete positive source addition must fit in the aggregate integer
  balance. Overflow rejects that contribution without partial mutation,
  saturation, or wraparound. Expected rejection leaves World Truth unchanged
  for that operation.

## Stable identity and spatial cardinality

- Every selected City instance has an authored, stable
  `SettlementSemanticId`, unique among City instances in this supported world
  profile. It is distinct from `CityData.DefinitionId`,
  `CityRuntime.RuntimeId`, Unity object identity, display title, and the P8
  `SpatialAnchorOwnerId` lookup key.
- The profile associates that settlement-instance identity with the stable P8
  `LocationId`. Before publication or mutation, the location must resolve in
  the promoted spatial authority and equal the anchor returned for the selected
  City instance by the existing P8 anchor binding. Missing, duplicate,
  unresolved, or mismatched identity/binding input rejects profile composition
  before material mutation.
- Runtime IDs and object references may locate the selected City only during
  execution. They are not persisted or fingerprinted as semantic settlement
  identity. The P8 owner lookup key remains an in-process handle; P14-A does not
  change P8 behavior.
- The configured source uses an authored stable `ProductionSourceId` (or
  equivalent semantic key), namespaced by settlement identity, plus compatible
  definition/content revision. Do not derive it from a list index or runtime
  identity. A narrow legacy resolver is permitted only if settlement plus item
  uniquely identifies the one source; otherwise missing/duplicate IDs are
  unsupported and composition rejects them.
- Market-store/custodian and item definitions use stable semantic IDs. Effective
  content/configuration identity is versioned separately from runtime objects
  and display labels.

## Deterministic daily boundary

The logical boundary is the effective calendar day and deterministic economy
pass order. P14-A preserves the existing source-before-consumption-before-price
update order and effective `Economy.Enabled` gate. A day key is sufficient only
for this explicit daily profile; it cannot stand in for a future intraday
instant or causal sequence.

Apply the complete source quantity atomically, then calculate the existing
population consumption request from authoritative population and resolved
parameters, cap actual free consumption to available stock, then update derived
prices. Results/projections report accepted quantity or a deterministic
rejection reason, actual consumption, and closing stock. No event or diagnostic
projection becomes an alternate material authority or consumes randomness.

## Reconstruction inventory

This checkpoint does not implement save, replay, or fork. Its authoritative
inventory contract preserves enough information for those later consumers to
reproduce the bounded causality:

- settlement-instance, P8 location, source, market-store/custodian, and item
  semantic identities;
- compatible source/content revision, effective economy and consumption
  configuration, effective enabled policy, and calendar identity/version;
- opening aggregate item stock at the boundary;
- the daily boundary and stable source application order, accepted source ID
  and quantity, and actual free-consumption quantity (or equivalent compatible
  state and inputs that deterministically reproduce them);
- settlement title and market custody as distinct facts; and
- closing stock and a rejected-source reason when rejection explains the result.

Deterministic diagnostics/projections sort by semantic IDs and expose causal
inputs/results where available. This contract is not an event-sourcing mandate
or history-retention policy. Future P12/P13 work owns actual persistence and
reconstruction implementation. A later intraday consumer must include P18's
exact logical instant, same-instant order, pending work, and lifecycle inputs;
a date alone cannot order those effects.

## Dependencies and execution ordering

**Promoted capabilities consumed by P14-A:**

- P8-A factual geography for stable `LocationId` identity.
- The promoted P8-B/C integration's City anchor binding/query capability (the
  selected contract refers to P8-C's City-anchor semantics). The authored
  settlement-instance identity remains distinct from both the location and
  runtime lookup handle.

**Not consumed:** P8-D passage topology, P8-E travel, P18 intraday scheduling,
P19 loader/public extension API, and P20 shared-activity coordination. These
are conditional dependencies only for later route-dependent, timed,
mod-defined, or multi-participant consumers respectively. The passive daily
source/sink adds no sleep, theft, work-shift, crew, or other gameplay behavior.

P9 is not a semantic prerequisite: the profile is authored manually. P9-A was
promoted at code tip `43f08b3` and Phase 9 closure was recorded at canonical
State/Brief tip `96f2c1aaf742f313bbb9643e5f5b3d844c402c78`. Its authored
bootstrap integration is complete; the serialized P9 ownership edge is
satisfied. That closure-only advance changes no P9-A APIs or content boundary.
P14-A still requires independent review of this current-base contract and its
own explicit scope/product gates before implementation readiness can be
derived. This is not a claim that P14 consumes P9's generation capability.
P10 is optional content. P14-A does not need to rerun genesis or infer identity
from generated ordering.

The selected implementation also depends on no P14-to-P15 material-cost
consumer. P15 may consume this capability after it is promoted. P16's logistics
scope may later consume appropriate material-flow capability, but P14-A adds
no transport. P12/P13 own actual save/reconstruction features and do not block
this daily domain slice.

## Ownership, validation, and exclusions

Likely code ownership from the reviewed technical design includes authored
source/profile data, `CityRuntime` coordination, the existing `MarketRuntime`
stock authority, runtime economy-pass composition, and deterministic
diagnostics/projections. These are shared semantic hotspots; a P14 worker must
use an isolated branch, inspect the exact current base, and coordinate serially
with any P9-owned files. This candidate contract does not itself assign a code
worker or authorize implementation.

Required focused validation is specified in
[`PHASE14_TECHNICAL_DESIGN.md`](PHASE14_TECHNICAL_DESIGN.md), including stable
identity and P8 binding/cardinality, deterministic same-input results, atomic
overflow rejection, stock-limited consumption, title/custody separation, the
disabled-economy gate, and reconstructable closing balance. Broader tests and
promotion gates are derived from the accepted implementation checkpoint and
repository execution policy.

Excluded: source extraction/reserves, inputs/transformation/spoilage,
multi-source or multi-city production, paid consumption, merchant transactions,
money mutation, transport/routes/network flow, production schedules beyond the
existing daily pass, multi-worker operations, collective activities, general
economy/trade/optimization, universal source frameworks, P19 loader/API,
P12/P13 persistence/replay, and concrete gameplay such as Sleep, Dreams,
robbery/gangs, rituals, War gameplay, or MegaEventos.

## Review and readiness record

- Planning base: Phase 8 canonical `470667d37863384edadb3d93ef64d8004aff46a3`;
  its advance from `77f3e1a` is State-only closure.
- Architecture baseline: `c285466c355103d3637ac165246591b72eb7bda0`.
- Technical design provenance: `f31b806`; its recorded historical independent
  design review and identity/cardinality re-review remain useful evidence.
- Current-base independent checkpoint review: **pending**.
- Current implementation status: **`WAIT_DEPENDENCY`**, pending independent
  current-base checkpoint review and the explicit profile/product gates above.
  The former serial owner edge to overlapping P9-A startup/genesis work is
  satisfied by P9-A's promotion; P9 remains no semantic prerequisite.
- Implementation readiness: **not yet derived**. After current-base review and
  acceptance of the bounded profile, the Master must recompute dependencies,
  file ownership, and integration order. No checkpoint IDs or implementation
  authorization are established by this refresh.
- No code/tests changed; no capability delivered; no canonical State claim.
