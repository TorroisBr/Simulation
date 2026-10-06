# Phase 12 — Save & Deterministic Continuation

**Authority:** planning entry subordinate to `../SIMULATION_ARCHITECTURE.md`. **Readiness:** P12-A scope is accepted after independent review at `5264f0c`; the refreshed acceptance record is documentation-only. The full design review passed at `9fde12a`, scoped reference refresh at `d1a8414`, and P18/P11 current-canonical refresh at `5264f0c`. Implementation remains `WAIT_DEPENDENCY` until complete included-owner exact export and staged hydration, a live profile inventory, and separate implementation authorization are established.

## Objective and closure

For supported compatible versions/profiles, continuing from boundary T and saving at T, loading, then continuing with the same inputs produce the same future authoritative results. Save is not replay or selective History.

**Accepted checkpoints:** P12-A — `UnityBootstrap-Daily-v1` remains the bounded profile integration scope. P12-B through P12-G are its accepted prerequisite capability checkpoints, recorded in `../design/PHASE12_CAPABILITY_CHECKPOINT_DECOMPOSITION.md`; their scopes and prerequisite implementation authorization were accepted on 2026-09-27 after exact-tip independent review. This authorizes only prerequisite capability work. P12-A remains `WAIT_DEPENDENCY` until all included owners have exact export/staged hydration, the live profile inventory is validated, and its separate implementation authorization is recorded. Acceptance is not delivery or canonical promotion.

| ID | Closure boundary | Dependencies |
|---|---|---|
| P12-B — Profile admission and completed-boundary lifecycle | Exact profile/provider admission and capture eligibility only at a successful completed daily boundary. | Accepted P12-A profile semantics; bounded technical design and independent review. |
| P12-C — Identity, genesis provenance, and deterministic roots | Exact typed allocators/sequences, selected P9-B provenance, P8-A facts, and continuation-relevant deterministic-random roots. | P12-B. |
| P12-D — Bootstrap factual roots and Person/population relations | Exact export and staged hydration for bootstrap factual authorities, including existing legacy spatial/site/exploration links and Person/population relations. | P12-B and P12-C. |
| P12-E — Profile-selected core and official daily-domain owners | Exact export and staged hydration for the core and configured official daily authorities in the accepted profile. | P12-B and P12-C; referenced roots from P12-D where needed. May be developed alongside P12-D only with isolated ownership and planned integration. |
| P12-F — Knowledge, directives, P11 choices, and active commitments | Exact causal Knowledge, directive, terminal actor-choice, and active commitment state for the accepted profile. | P12-C, P12-D, and P12-E. |
| P12-G — Staged restore, whole-graph validation, atomic publication, and parity | Private staged hydration, complete graph validation, one publication boundary, and continuation parity/rejection evidence. | P12-B through P12-F and a validated live profile inventory. |

## Dependencies and gates

- **Hard semantic contracts:** deterministic simulation, semantic IDs, effective configuration/calendar/content compatibility and current-world mutation authority are already defined.
- **Hard capabilities:** complete closure needs hydration/continuation for every authoritative domain in the declared supported save scope; domain-specific integrations wait for stable corresponding contracts/capabilities.
- **Integration dependency:** restoration occurs at consistent boundaries and composes all relevant stores, plans, RNG state and command context without a second world authority.
- **Soft ordering:** generation and content may evolve in parallel; this does not excuse a false claim of complete save coverage.
- **Resolved bounded profile:** `UnityBootstrap-Daily-v1` uses the SampleScene-selected `Simulation-DailyV1.asset`, a dedicated P9-B-only continuation config, through the validated Unity bootstrap, exact compatible build/runtime and current-host numeric profile, and completed-day capture boundaries. `Simulation-GeneralTest.asset` remains the separate P10-A Ruin/LocalTopology proving profile and is rejected before identity allocation when submitted to Daily-v1. The selected Daily-v1 asset enables the P9-B authored-geography profile and publishes exactly one P8-A Hex and one anchored Location before day one. P9 canonical closure is `82396ae7ffaf407fda278928da456b06dc5394d`; P9-B code integration is `d9a62d7c6bea242653c2d68cc0a70911bb5ed1bf` (implementation tip `00395ef80cfa2364d34ed2170e0735d3a4b1513d`), with promotion State/status record `14a2e8ee01e49f1ffdcecf8fd64d274c728ead44`. This P12 profile is incompatible with P9-A-only configs. Any retained P9-A-only profile requires a distinct profile identity and admission that explicitly rejects authored geography. P12 excludes intraday state, P9/P10 generated worlds, P19 module state, P20 shared activities, and P13 historical fork guarantees.
- **Architecture/checkpoint gate:** P12-A's candidate contract was independently reviewed against current canonical architecture, Roadmap, Execution Model, P8 State, P9-A/P9-B profile-manifest contracts, P11 input authorities, current P18 State, reviewed P20 technical design and proposed P20-A checkpoint, and both alignment records. The selected P9-B geography capability is present on current P9 canonical at code integration `d9a62d7` and closure `82396ae`; refreshed P12 review and scope acceptance are recorded in the contract. P10-A is promoted at code tip `9501bf0` with promotion State record `9e79b58`; the current P10 canonical State/Brief tip is `252ad6b`. Its bounded Ruin/LocalTopology profile remains excluded from P12-A.
- **Exclusions:** historical fork guarantee as a Phase 12 result, universal event sourcing, implicit cross-host numeric portability.
- **P12-A profile assumption:** use the clearly recommended, bounded `UnityBootstrap-Daily-v1` profile: same-build/current-host daily continuation through the normal Unity bootstrap and built-in providers, captured only at a successfully completed daily boundary. This is an orchestrator planning assumption, not an architecture amendment or broader product promise. No P13 history/fork guarantee, cross-host guarantee, loader/module state, generated-world state, or shared-activity state is implied.
- **P12-A causal-input boundary:** the selected bootstrap has no external `WorldCommand` service/queue composition; adding it is unsupported and must reject profile admission. P11's current canonical `SimulationRuntime` still composes an `ActorChoiceStore` by default: preserve full records/dispositions, duplicate-command idempotency history and sequence; reject `Pending` (including deferred) and `ConsumedAwaitingTerminalAttempt` records. A thrown actor attempt rethrows from its `AdvanceDay`, so it is capturable only at a later successful daily boundary while the runtime remains healthy. The current SampleScene bootstrap creates legacy NPCs without `PersonId` and starts with an empty `PersonStore`; P11 choice execution applies only to Person-backed NPCs. The P9-B/P11 composition proves the choice store and continuation-state owner, not that SampleScene NPCs can execute SellGoods choices. Any future Person-backed actor capability is separate upstream scope; P9-B geography remains unchanged. This does not add a new security boundary; normal domain/action semantics remain authoritative.
- **Replay/fork sensitivity:** all authoritative truth, plans, Knowledge, IDs/allocators, logical time/calendar, effective config/content and causal randomness needed to continue.
- **Hotspots/parallelism:** `SimulationRuntime`, domain stores, command capture, diagnostics versus actual save state, composition/versioning; state inventory can start alongside P8/P9 work, integration is domain-gated.
- **P12-A implementation gates:** implementation remains `WAIT_DEPENDENCY` until every included owner has exact export and staged hydration support validated for this profile, the live canonical composition and profile inventory are revalidated, and separate implementation authorization is complete. Scope acceptance is recorded above; no P12-A implementation checkpoint has been delivered. P9-B code integration is on current P9 canonical at `d9a62d7` (closure `82396ae`) with promotion State/status tip `14a2e8e`. The code-bearing additive P9-B/P11 composition `ec75e6a0912704446fe47f9d727b4656709d05ab` was validated and reviewed; its docs-only refresh `36e3064e8f60e9c7e8914a23c62d380b708da587` was independently revalidated and promoted to P12 canonical. It changes only `docs/PHASE18_STATE.md`, so the ec75e6a code review and Unity validation remain applicable. Preserve the selected P9-B geography profile identity/schema and fingerprint/provenance, seed/config/calendar, inherited P9-A stage identities, P9-B stage/dependencies, authored inputs/outputs and first boundary without rerunning genesis. P8-A's selected Hex, terrain/revision provenance, anchored Location and scale context are populated required state; P8-B through P8-E passage, City/Site presence anchors, Person positions, route Knowledge/plans, and travel remain empty for this profile. Unsupported or partially populated state must reject admission. P8-E is not a blanket daily-profile dependency. P14-A is promoted but not configured by the selected Daily-v1 City entries; a future profile that composes its authored material flow must include its owner state or reject admission. Complete included-owner exact export and staged hydration coverage plus a live profile inventory are not yet demonstrated.
- **Read-only owner evidence:** [`../design/PHASE12_OWNER_COVERAGE_INVENTORY.md`](../design/PHASE12_OWNER_COVERAGE_INVENTORY.md) records per-authority export/hydration gaps. It is evidence only, not checkpoint acceptance or implementation authorization; P12-A remains `WAIT_DEPENDENCY`.
- **Downstream unlocks:** continuation foundation for P13 historical reconstruction/fork.
- **Deferred:** final storage format, migration matrix and replay algorithm until entry design.

## Temporal and extension-state inventory — 2026-09-27

Inventory/design can proceed alongside P18 and generation work. This daily
profile does not compose the P18 timeline and makes no intraday continuation
claim. If a later profile includes P18 state, reconstruct its boundary identity
`(worldId, profileId, absoluteDay)`, tick quantum/version, effective
`MaxDispatchesPerInstant` (or equivalent dispatch limit), work identity
`(ownerId, workId, revision, occurrence)`, exact logical instant, causal wave and
same-instant order, pending boundary/work, persisted occurrence/sequence and
owner idempotency/effect state. Preserve sealed external inputs with target
`LogicalTick`, accepted sequence/order, and boundary state. Include promoted P18-B activity instance,
lifecycle/revision/receipt, participant commitments and availability facts, and
P18-C PersonId-keyed decision/attempt state only when the selected consumer
composes them. P18-A/B/C and the bounded P18-D consumer slice are promoted,
and P18 is formally closed within its recorded scope at `a49de9d`. Broader
consumers remain outside that scope. The `UnityBootstrap-Daily-v1` profile does
not compose P18 temporal state, so its legacy daily execution remains
authoritative. The P18-D `SimulationRuntime` hotspot has been handed off to
P12-B, but this ownership handoff does not satisfy P12-B's composition,
live-owner census, mutation invalidation, or readiness gates. A day value
alone cannot stand in for these facts, and the bounded daily profile does not
depend on P18-D.

Include causally relevant generation stage/contributor identities, effective
versions and outputs. Mod-owned state joins the inventory when supported;
P19 module lifecycle and explicit retrofit compatibility must be available before
claiming those integrations. Do not invent mod schemas or require P19 to save
ordinary official worlds. Missing compatible code/content cannot silently
discard extension state. Revalidate pre-change continuation proposals against
these additions; historical retained state is not regenerated on installation.

## Shared-activity continuation inventory

When P20 is in the supported save scope, hydrate one stable `ActivityInstanceId`
and compatible definition/version separately from participant identities. Keep
the architecture's one-or-more participant cardinality; the reviewed two-Person
P20 fixture does not define a universal count or role policy. Include partial
formation and independent PersonId decisions, roles where applicable, agreements,
reservation intents and committed intervals, scheduled start, shared lifecycle/
revision/context, already-applied participant effects, and pending due work with
its causal input, order, sequence and idempotency state. Never infer one
Activity-to-one-actor cardinality or duplicate a shared instance into actor-owned
truth. P20 remains conditional and outside this profile; its reviewed design is
not an implementation capability or blanket P12 dependency.

## Current dependency refresh — 2026-09-29

P18 is formally closed by closure marker `a49de9d`; the current P18 canonical
State tip is `8ac2d78`. The P18-to-P12-B `SimulationRuntime` hotspot handoff is
effective. This resolves the P18 ownership prerequisite only; P12-B remains
blocked on the complete live owner/cardinality census, committed-write
invalidation proof, and its accepted admission/eligibility gates.

The P9-B/P11 composition is canonical on P12 at `36e3064e8f60e9c7e8914a23c62d380b708da587`.
It preserves the validated executable tree from `ec75e6a` and changes only
`docs/PHASE18_STATE.md`. Exact-tip independent review passed; the ec75e6a code
review and Unity validation remain applicable because no executable file
changed. P12-A remains `WAIT_DEPENDENCY`; this record does not grant its
separate implementation authorization or claim owner export/hydration
coverage.

## Post-promotion refresh — 2026-09-29

P12 canonical advanced docs-only from `4d2a9ad5c7f98a7805dede72f9722aec063231e8`
to `0b5b4abb0d0a6064500adafe6a3454e41868c102`. The promotion records refreshed
P12-B/G owner evidence, the preserved P12-C candidate's current-base
compatibility classification, and exact-tip review. It does not change the
validated `ec75e6a` executable tree or the checkpoint dependency edges.
P12-B remains blocked on complete live owner/cardinality and committed-write
invalidation evidence plus owner-thread/quiescence proof; P12-A remains
`WAIT_DEPENDENCY`. P13 remains dependency-gated. No capability became
implementation-ready through this promotion.
