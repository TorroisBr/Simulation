# P12-B/C/D RuntimeIdentity and Spatial Protocol Implementation Review

**Result:** PASS — independent exact-tip implementation review.

**Reviewed candidate branch/tip:** `codex/phase12/P12BIdentitySpatialProtocolIntegration` at `06f7678d167e82b3f476683420db79f87c0bb82b`.

**Exact code commit/tree:** `6dbe17744c9cd4670de89624b8a0099e215701bc` / `30f422f76bfa20bc16661f937dca823cda951819`.

**Canonical base:** `codex/phase12/canonical` at `80d0ec825a9ad8da819cc43f8ac214fb49e27291`.

**Design and design review:** proposal `d73c44202affc4adc936187313145f11a166a027`; independent design-review PASS `106a4c6` on `codex/phase12/P12BIdentitySpatialProtocolDesignReview`.

**Architecture baseline:** `codex/architecture/world-identity-projection` at `e16796014d348e3b59da7ed848101c4c03926ba5`.

## Review findings

The candidate is a clean additive descendant of the stated P12 canonical base. The reviewed code diff is exactly base `80d0ec8` through code commit `6dbe177`; the final evidence tip `06f7678` changes only validation documentation/artifacts after that code commit. `git diff --check` passes for the complete code diff.

The implementation follows the reviewed 11-section contract: eight schema-v1 `RuntimeIdentityRegistry` sections, two legacy `SpatialNetworkRuntime` sections, and the `ExplorableSiteStore` section. It binds providers to exact owner objects, verifies schema/section identity and roles, checks nonnegative witnesses, and rejects nonzero `ExplicitlyEmpty` state before sealing the selected Daily-v1 inventory. The corresponding composition test checks 253 total sections, exact owner identities, cardinalities, shared/local revisions, and section roles. Day-zero counts are NPC 10, City 2, Location 2, Route 2, four zero identity indexes; the eight registry sections share revision 16. The legacy network has two Locations and two Routes at revision 4. P10-A remains outside the P9-B-only `UnityBootstrap-Daily-v1` profile.

The corrected bootstrap rule is implemented: when Daily-v1 constructor roster composition encounters an NPC missing from the exact installed identity registry, `TryRegisterNpcWithinMembershipCensus` returns `RuntimeFaulted` before calling `RegisterNpc`, and the constructor fails before runtime publication. The regression `SelectedDailyProfileCompositionRejectsNpcMissingFromIdentityRegistry` verifies the registry count and revision remain zero. Existing bootstrap NPCs must already map to the exact object; same-ID different-object or cross-type aliases reject.

For post-genesis membership, registry type/revision capacity is preflighted before insertion. A new NPC identity is inserted only as part of the existing `runtime.npc-membership` commit and all eight sections sharing the registry revision are marked changed together in the same operation/epoch. Unregister preserves the append-only identity mapping; re-registering the same inactive object does not increment the identity revision. Rejection and exact-object/alias cases are covered. The owner-thread, operation-scope, epoch, ordinary Person/guard, and City-presence checks remain ahead of the insertion. Unexpected failure after committed owner mutation follows the existing faulted-runtime behavior.

The implementation registers the legacy spatial and site owners as bounded witnesses and validates their unchanged baselines at NPC membership admission. It adds no geography/site mutation operation or gameplay behavior. The profile test exercises explicit detection of unsupported post-publication spatial drift. No claim is made that every operation scans these owners or that capture is eligible.

## Exact validation evidence

The corrected candidate evidence at `06f7678` records Unity `6000.3.9f1` validation against code `6dbe177` / tree `30f422f`:

- `SimulationBootstrapCompositionTests` 24/24 PASS.
- `PropertyEstateMutationEpochTests` 5/5 PASS.
- ALL EditMode 2417/2417 PASS.
- Official Smoke 5/5 PASS.
- `git diff --check` PASS.

I independently parsed the committed XML artifacts and confirmed all four report `Passed`, with totals 24, 5, 2417, and 5 respectively. The committed Git-blob XML SHA-256 values, Unity-emitted XML SHA-256 values, and source blob hashes match the amended manifest. The raw-log archive SHA-256 is `5FC15793969F71FA273FE418B23A6C2AE644514EFB188C41D72B339DEBCAF3A5`; all four successful log-member hashes also match. The archive additionally retains the first corrected-test compile diagnostic for audit. Initial pre-correction validation is clearly separated from the final corrected evidence.

## Scope limits

This promotes no gameplay feature and is only the bounded P12-B/C/D owner inventory and NPC membership invalidation slice. P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked. P12-F Expedition remains deferred to its accepted dependency-gated checkpoint. This review does not establish complete owner or shared-epoch coverage, global quiescence, capture eligibility, export, hydration, P12-A readiness, P13 readiness, Phase completion, or closure. Canonical promotion remains separate.
