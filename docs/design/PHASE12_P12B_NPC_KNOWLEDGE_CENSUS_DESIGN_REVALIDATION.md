# P12-B Per-NPC Knowledge Census Design Revalidation

**Classification:** `UPSTREAM_IRRELEVANT` to the reviewed Knowledge census
contract; refresh any implementation candidate from current canonical before
work/integration.

**Design:** `codex/phase12/P12BNpcKnowledgeCensusDesign` at
`3856432da1a14b8aec4fd92c1562aaebda107524`; prior exact-tip PASS record is
`codex/phase12/P12BNpcKnowledgeCensusDesignReviewRecord` at
`723f4b026bd7d881e204fd476354afc1dc6dd2c3`.

**Current canonical base:** `codex/phase12/canonical` at
`19d0373d6a71b63536248ecc9091e66c9b3a708b` (remote tip verified).

The TravelParty promotion adds a fixed schema-v1 `p12f.travel-parties`
provider bound to the installed `TravelPartyStore`, reporting active party
instances and that store's revision. The bootstrap composition exposes this
provider and now checks that `GroupTravel` shares the installed store. The
party provider counts party instances, not member NPCs. The related
`TravelPartyStore` revision/mutation window and `ExpeditionSystem` return
window do not change `NpcRuntime` ownership, roster membership, any of the
four Knowledge owner stores, their observation write paths, or
`ContinuationCensusProtocol`'s SpatialKnowledge/Inventory roster-family
registration. The fixed `p12f.travel-parties` section is disjoint from the
ten proposed per-NPC Knowledge section prefixes.

Accordingly, the Knowledge design remains valid at its reviewed contract:
four owners and ten new sections per NPC, plus the existing two
SpatialKnowledge sections; Inventory and TravelParty remain separate. The
selected-profile live NPC roster and the Knowledge cardinality baselines are
not changed. P12 State records that this promotion adds no P18 timeline or
handoff behavior and no permanent Activity-to-Actor/NPC-owned authority rule;
the selected P12 profile's temporal and P20 exclusions remain intact.

The implementation candidate must nevertheless refresh from `19d0373`.
`SimulationBootstrapComposition.cs` is a shared integration hotspot: preserve
the promoted TravelParty provider and same-store invariant, add/reconcile the
Knowledge family against the current fixed required-section inventory, and
recheck section collisions and composition tests on the current base. The
promotion does not itself register TravelParty in the runtime
`ContinuationCensusProtocol`, grant P12-B readiness, or resolve the outstanding
global owner-census/invalidation/quiescence blockers.

No Unity tests were run for this documentation-only revalidation. The reviewed
Knowledge candidate remains unchanged; this record classifies its scope impact
only and does not substitute for exact-current-base implementation review and
validation.
