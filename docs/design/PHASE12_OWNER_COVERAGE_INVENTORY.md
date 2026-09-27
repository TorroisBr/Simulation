# P12-A owner coverage inventory — evidence only

**Profile:** `UnityBootstrap-Daily-v1` (the bounded SampleScene
`Simulation-GeneralTest.asset` → `TesteSimulacao.InitializeSimulation` daily
profile). **Reviewed source baseline:** docs-only P12 refresh parent
`35b03ffc447c348f367a23872879ad5075ceacd0`, with P8 `470667d`, P9 code
`d9a62d7` / State-status `14a2e8e`, P11 canonical `308e24d0744112e8f2b741521b8b3e4acb51ebbf`,
P18 canonical/State `99cac77f7d66e8eb61fe68efb6959a4a5b7029ca`, P10 code
`9501bf076d506fb64d6ee3e6d178574fff36e153`, P14-A promotion
`c44904bb4b0a066eced1d7e8a773b7dc1eea76c0`, P20-A proposal refresh
`2a03edadcb411c7ca7aa2d0c67ee8d36f972779e`, and architecture `c285466`.

This remains a documentation-only cross-branch inventory. The proposal branch
does not inherit P11 executable code; P11 canonical was inspected separately
for current composition facts. P18 source is present through the merged
`99cac77` parent, but the selected legacy daily bootstrap does not compose its
timeline, activity lifecycle, or availability-decision services. Any later
implementation/integration candidate must include the promoted P11 runtime (or
prove equivalent current composition) before claiming ActorChoice support and
must not treat the P18 source files as composed runtime behavior.

This is a read-only gap inventory from the current runtime composition and
owner APIs. **No profile-included group currently has a demonstrated complete,
exact immutable P12 export plus staged hydration path.** Existing construction,
clone, transaction rollback, Unity serialization and diagnostic snapshot APIs
do not establish those capabilities. There is no P12 save envelope, complete
owner hydration factory graph, or staged runtime publication/single-swap path.
The profile therefore remains `WAIT_DEPENDENCY`.

| Included authority group | Exact immutable export | Staged hydration | Concrete gap blocking P12-A |
|---|---|---|---|
| Profile/build/content/provider identity; effective configuration, calendar, completed-day boundary and capture eligibility | No | No | No versioned P12 envelope/admission manifest or successful-advance token binds the runtime, configuration, content/providers and exact quiescent boundary. `SimulationBootstrapComposition` and runtime wiring describe construction, not a sealed continuation capture. |
| Semantic/runtime IDs, allocator/high-water marks, record sequence and identity indexes | No | No | IDs and registries are used by live owners and partly observed by diagnostics, but there is no complete immutable owner export or restore API that reinstates exact identities/counters and validates global uniqueness before publication. |
| Bootstrap roots: Cities/markets/accounts, NPC roster and mutable NPC/action/inventory/plan state | No | No | Unity assets and `[SerializeField]` fields describe authored inputs and selected object fields; they do not capture all evolved owner state, revisions, causal ordering and references. Owner-specific value DTOs, exact admission checks and validated hydration constructors are missing. |
| Population aggregates, Persons, residence/lifecycle/materialization and genealogy | No | No | In-memory stores, registrations, clones and transaction snapshots support runtime operations/rollback, not a complete immutable representation and staged restore of aggregate/Person/NPC links, dates, relations, allocator state and owner revisions. |
| Knowledge, directives, P11 actor-choice dispositions, travel parties, expeditions and other active commitments | No | No | Diagnostic projections cover selected fields; there is no owner export/hydration contract covering all observations/provenance/freshness, terminal input idempotency/sequence, commitment progress/costs and reciprocal references. P11's current canonical SimulationRuntime composes ActorChoiceStore by default even though the selected Unity bootstrap has no external WorldCommand service/queue; preserve complete records/dispositions, duplicate command IDs and next sequence, and reject in-flight inputs. The queue remains excluded. |
| P9-A/P9-B genesis identity, P9-B authored inputs/outputs and selected P8-A Hex/Location/scale provenance | No | No | P9 can produce and validate the authored geography/manifest, but that pipeline is not a P12 export of evolved P8 owner truth or a staged hydration factory. P8-A exact facts and provenance must be captured and restored through the owning authority. |
| Official daily-domain owner state selected by effective configuration (economy, demography, mortality, trade/Knowledge sharing, crime/social appraisal, and related provider context) | No | No | The profile has no inventory-backed exact export and hydrate implementation for every actually composed provider/owner, including causal random/draw context, mutations, revisions and reconstructible projections. A provider list or runtime clone does not close this gap. |
| Core political, institutional, property, faction/support/recognition, force/manpower/position, conflict/war/battle authorities present in the selected runtime composition | No | No | Owner stores and some transaction snapshots/diagnostic records exist, but no complete profile export/hydration graph restores populated truth, relations, terminal outcomes, revisions and allocator state in owner dependency order. Empty-at-start does not establish handling of later populated state. |
| Mutation health and derived runtime indexes/projections | No | No | No capture lifecycle protocol proves a quiescent, non-faulted boundary across all supported writes. No staged restore validates every owner before binding a fresh guard, rebuilding only owner-derived indexes and publishing exactly once. |

`WorldStateSnapshot` and canonical diagnostic/digest output are comparison and
diagnostic projections, not reconstructible truth or a save DTO: their field
coverage is selective and they do not define owner-authorized restoration.
Likewise, Unity `[SerializeField]` and `[Serializable]` support in scene/assets
is incomplete for runtime continuation: it neither guarantees inclusion of
all causal state nor supplies compatibility validation, owner hydration,
cross-reference/cardinality checks, or atomic publication.

## Explicit profile boundary

P8-B through P8-E are explicit empty sections for this profile; populated state
must reject admission. P10 generated state, P18 intraday state, P19 module
state, P20 shared activities, P13 historical reconstruction/fork guarantees,
and P14-A material-flow state are outside P12-A. Their exclusion does not
remove the P9-B/P8-A authored-geography provenance and truth required above.
P10-A is promoted but its Ruin/LocalTopology profile is not selected here.
P18-A/B/C are promoted on their canonical branch, but the selected bootstrap
does not compose that separate temporal implementation; no P18 state is claimed.

This inventory records implementation evidence and gaps only. It is not P12-A
scope acceptance (recorded separately), implementation authorization, proof of
save/load, or a claim of profile closure. Keep P12-A at `WAIT_DEPENDENCY` until
every included owner has exact immutable export and staged hydration coverage,
and the live canonical composition/profile inventory is demonstrated. Separate
implementation authorization remains an independent gate.

**Source basis:** `SimulationRuntime.cs`, `SimulationModuleSet.cs`,
`TesteSimulacao.cs`, `RuntimeIdentity.cs`, owner/store implementations under
`Assets/_Project/Scripts`, and `Diagnostics/WorldStateSnapshot.cs`,
`WorldStateCanonicalWriter.cs`, and related snapshot projections. Existing
clone and transactional restore helpers were classified by their actual
runtime/rollback contract; none was treated as a P12 serializer or staged
hydrator.
