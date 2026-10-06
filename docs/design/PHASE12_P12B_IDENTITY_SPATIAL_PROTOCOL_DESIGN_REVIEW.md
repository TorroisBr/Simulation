# P12-B/C/D RuntimeIdentity and Spatial Protocol Design Review

**Result:** PASS — independent exact-tip technical design review.

**Reviewed proposal:** `codex/phase12/P12BIdentitySpatialProtocolDesign` at `d73c44202affc4adc936187313145f11a166a027`.

**Canonical base:** `codex/phase12/canonical` at `80d0ec825a9ad8da819cc43f8ac214fb49e27291`.

**Architecture baseline:** `codex/architecture/world-identity-projection` at `e16796014d348e3b59da7ed848101c4c03926ba5`.

The exact design tip is available on the named remote branch, the canonical base is the current remote P12 tip, and the architecture reference resolves to the requested remote commit. The review compared the proposal with the current P12 State, accepted Phase 12 Brief/profile boundary, blocker matrix, and canonical implementation call sites.

The proposed eleven additions are internally consistent with the 242-section baseline: eight typed `RuntimeIdentityRegistry` sections (`p12c.runtime-identities.{npcs,cities,locations,routes,explorable-sites,local-places,local-connections,notable-items}`), two legacy `SpatialNetworkRuntime` sections (`p12d.legacy-spatial-network.{locations,routes}`), and the existing `p12d.explorable-sites` owner witness. The resulting 253 count is specific to this selected composition. The identity registry’s day-zero counts (NPC 10, City 2, Location 2, Route 2, and four empty typed indexes) and shared local revision 16 are consistent with the authored bootstrap registration sequence; the spatial network’s two Locations, two Routes, and revision 4 are likewise consistent. Distinct P8 SpatialAuthority and legacy SpatialNetwork ownership is preserved.

The NPC membership contract uses the existing `runtime.npc-membership` boundary and preserves append-only registry semantics. Genesis NPCs must be the exact instances already registered in the identity index. A new post-genesis NPC is added to that index as part of a successful membership commit; an existing identical object retains its identity/revision; an ID bound to another object or type is rejected. Unregister changes active membership only. Since every typed index witness reads the same registry revision, a new NPC identity requires all eight registry section baselines to be notified together in the same operation/epoch. This addresses the concrete `SimulationRuntime.TryRegisterNpc` cross-owner gap without changing gameplay behavior or introducing a new operation ID.

The stated preflight requirement is appropriate: validate owner thread, operation/shared-epoch capacity, current baselines, and all ordinary membership/Person/presence/guard failure conditions before inserting the append-only identity row. Returned failures must preserve registry, roster, and census baselines; exceptional allocation failure after a committed owner write follows the existing faulted-runtime contract. The proposal explicitly requires these semantics and corresponding negative/re-registration/unregister tests.

The spatial/site boundary is supported by the selected-profile source map: production `RegisterLocation`/`RegisterRoute` and site `Add` calls occur during authored genesis; P10 Ruin/site/topology creation is a separate profile/genesis path. No normal post-publication Daily-v1 gameplay writer was identified. The design adds exact witnesses and an explicit unchanged-section assessment for detecting unsupported direct mutation, without claiming every operation scans those owners, adding a new spatial operation, or treating public visibility alone as supported gameplay. Daily-v1 remains the P9-B-only profile; P10-A and GeneralTest remain separate.

The §92A treatment remains bounded: required owners are bound to exact installed instances; excluded typed identity indexes and the site store are explicitly empty with fail-closed admission/negative-test obligations; the proposal does not claim semantic export/hydration, capture eligibility, or complete continuation. It is consistent with accepted P12-B/C/D capability work and does not authorize final P12-A integration.

**Limits:** This design review is not implementation review, validation, canonical promotion, or a readiness determination. P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked. P12-F Expedition stays deferred to its accepted dependency-gated checkpoint. No complete owner/epoch coverage, global quiescence, capture eligibility, export/hydration, Phase completion, or closure is claimed. Implementation still requires its own exact-tip review and the specified focused/full/Smoke validation.
