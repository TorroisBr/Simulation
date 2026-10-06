# P12-B/C/D RuntimeIdentity and spatial protocol reconciliation design

**Status:** Proposed for independent technical review. This is a bounded integration within the accepted P12-B, P12-C, and P12-D capability scopes. It does not authorize final P12-A profile integration.

**Canonical base:** codex/phase12/canonical at 80d0ec825a9ad8da819cc43f8ac214fb49e27291.

**Architecture baseline:** codex/architecture/world-identity-projection at e16796014d348e3b59da7ed848101c4c03926ba5. Revalidation includes §92A. The accepted P12 profile remains the P9-B-only UnityBootstrap-Daily-v1; Simulation-GeneralTest.asset and P10-A remain separate.

## Source findings

The current selected Daily-v1 protocol seals 242 sections. SimulationBootstrapComposition already creates eight schema-v1 RuntimeIdentityRegistry providers, two schema-v1 SpatialNetworkRuntime providers, and an ExplorableSiteStore provider. The full-bootstrap profile test reads these exact owners, but the providers are not registered into the sealed 242-section inventory.

The selected profile's day-zero evidence is exact: RuntimeIdentityRegistry counts are NPC 10, City 2, Location 2, Route 2, ExplorableSite 0, LocalPlace 0, LocalConnection 0, and NotableItem 0, all on the same registry owner at revision 16. SpatialNetworkRuntime has two Locations and two Routes on one owner at revision 4. ExplorableSiteStore is empty at revision 0.

RuntimeIdentityRegistry is an append-only typed identity index with one shared census revision. Each successful Register* call increments that revision; every one of its eight census providers reports the same revision. The registry currently has no removal API.

The selected runtime also supports post-genesis NPC membership through SimulationRuntime.TryRegisterNpc and TryUnregisterNpc. SimulationRuntime owns a separate active-roster dictionary and list, but it receives no RuntimeIdentityRegistry reference. A successful runtime NPC addition therefore changes the roster-following P12 owner sections without adding a newly materialized identity to the exact registry used by TesteSimulacao and identity lookup consumers. This is a concrete cross-owner membership gap.

The current production callsites for SpatialNetworkRuntime.RegisterLocation/RegisterRoute and ExplorableSiteStore.Add in the selected bootstrap execute during authored-world genesis. P10 Ruin/site/topology registration belongs to its separate genesis profile. No supported post-publication Daily-v1 gameplay callsite for adding geography or sites was found. The public APIs remain usable by direct callers; invoking them outside a runtime-owned supported operation is outside this P12 profile contract. A later world-expansion capability must add its own operation boundary before it is admitted to a continuation profile.

## Bounded contract

Register the existing owner witnesses in the selected Daily-v1 protocol:

| Owner | Existing section IDs | Role in Daily-v1 | Baseline evidence |
|---|---|---|---|
| RuntimeIdentityRegistry | p12c.runtime-identities.npcs, cities, locations, routes | Required | Exact installed registry; counts 10, 2, 2, 2; shared revision 16 |
| RuntimeIdentityRegistry | p12c.runtime-identities.explorable-sites, local-places, local-connections, notable-items | ExplicitlyEmpty | Same registry; each count 0; shared revision 16 |
| SpatialNetworkRuntime | p12d.legacy-spatial-network.locations, routes | Required | Exact installed network; counts 2, 2; shared revision 4 |
| ExplorableSiteStore | Existing ExplorableSite census section | ExplicitlyEmpty | Exact installed store; count 0; revision 0 |

These are 11 existing provider sections, increasing the selected protocol inventory from 242 to 253. The section count is evidence for this exact composition, not a universal profile constant. Preserve separate ownership between P8 SpatialAuthority and the transitional legacy network.

The SimulationRuntime and SimulationBootstrapComposition must bind the exact registry, network, and site-store instances that produce these witnesses. Runtime publication fails closed if a required provider is missing, duplicated, malformed, has a different owner identity, reports an unsupported schema, or violates an ExplicitlyEmpty role.

## Runtime NPC membership and invalidation

Use the existing runtime.npc-membership operation. For the bootstrap roster, each NPC must already be present in RuntimeIdentityRegistry as the exact same object; bootstrap composition must not register it twice.

For a post-genesis TryRegisterNpc call:

1. Validate the current identity and roster baselines, owner thread, the existing membership operation, and shared-epoch capacity before mutation.
2. If RuntimeIdentityRegistry has no row for the RuntimeId, register this NPC identity as part of the membership commit. If it contains the same object, preserve the existing identity and revision. If the ID belongs to a different object or type, reject before roster mutation using the existing duplicate-identity result path.
3. Complete the existing active-roster and per-NPC owner updates.
4. After a successful new identity insertion, notify all eight RuntimeIdentityRegistry sections together because they share one owner revision. Reconcile the existing roster, lifecycle, presence, and per-NPC sections in the same membership operation and one shared epoch.
5. TryUnregisterNpc removes only active membership. It leaves the append-only identity row in place; re-registering that same object does not create a second identity or increment the registry revision.

All ordinary rejected registration paths leave the identity registry, active roster, and census baselines unchanged. Preflight every known failure condition before the identity insertion. Exceptional allocation failure after a committed owner write faults the runtime under the existing partial-operation contract; no rollback semantics are added to the domain.

No runtime operation is added for SpatialNetwork registration or ExplorableSite addition. Their selected-profile witnesses are established from completed genesis. An unnotified post-publication change advances the owner's local revision/count; an explicit TryValidateUnchangedSections assessment over the affected IDs rejects that drift. This design does not add a universal pre-operation scan or claim capture readiness. Adding runtime geography, routes, Ruins, sites, local topology, or notable items remains outside the accepted Daily-v1 contract.

## Validation and review obligations

Focused coverage should prove:

- The full selected Daily-v1 composition registers all 11 sections with exact owner identities, schema versions, roles, cardinalities, and revisions; the total inventory is 253.
- Each of the four nonempty identity indexes and both network sections retain the existing positive profile values.
- The four excluded typed identity indexes and ExplorableSiteStore reject nonzero pre-admission state through ExplicitlyEmpty admission.
- A post-genesis newly registered NPC becomes resolvable from the exact RuntimeIdentityRegistry, updates the NPC identity count/revision, and advances all eight registry section baselines in the existing membership operation.
- Re-registering the same inactive object preserves identity cardinality and registry revision while restoring active roster membership.
- A different object with a previously used RuntimeId is rejected without changing any owner or epoch.
- Unregistering an NPC changes active roster census only and retains its historical identity mapping.
- A direct unsupported SpatialNetwork or site-store mutation changes its owner revision; an explicit TryValidateUnchangedSections assessment over the affected IDs rejects the unannounced drift. This design does not claim every Daily-v1 operation performs that assessment.
- P10-A GeneralTest remains a separate proving profile and remains rejected by Daily-v1 before identity/publication.

After implementation, rerun the affected focused suites, ALL EditMode, official Smoke, and git diff --check. Preserve source/XML/archive hashes in the candidate evidence. Do not infer complete owner/epoch coverage, global quiescence, capture eligibility, export, hydration, P12-A readiness, P12-B completion, P13 readiness, or Phase closure.