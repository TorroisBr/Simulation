# P12-G target-owner exact-zero interface audit — current canonical

**Audit baseline:** P12 canonical `02009f9063dd252bd4b177fd6aef1e74dcd947f5`; architecture canonical `47eff220c7ce00f6e7c759bdc2b76780bb46f628`; P12-G candidate tip before this audit `029658d6fbfea5b6d60f26fbfc687386979649ab`.

**Status:** read-only source/interface audit. It narrows the existing P12-G target-owner obligations to specific current constructors and checks. It does not implement the G coordinator, validate the full live inventory, change the P12 DAG, or establish P12-A/P13 readiness.

## Result

The P12-B runtime census already contains exact-owner checks that a future G candidate can reuse for most excluded or exact-zero target owners. The reviewed C/D/E/F packages also expose the construction boundaries needed to create fresh target owners. No current P12-G orchestrator assembles those outputs, checks the complete target vector, or invokes restored-boundary admission, so these interfaces reduce implementation uncertainty without closing the G dependency.

The rules below distinguish a new target owner from a source census witness. A source token/vector is bound to the original runtime and its owners. G must create the target graph privately, then read and validate the target's own census owners before fresh restored-boundary admission and the single active-session publication.

## Target-owner disposition

| Owner family | Existing construction/check | G requirement still open |
|---|---|---|
| P8-C City/Site bindings and Person positions | `SimulationRuntime` creates fresh `LegacySpatialAnchorBindingStore` and `PersonSpatialPositionStore` instances when no staged instance is supplied. The P12-C/D packages do not export these selected-profile explicit-empty sections. `P12RuntimeIdentitySpatialCensus` binds each provider to the exact installed object and requires cardinality zero at registration. | Build them against the staged P8-A/Person/Site roots; query the target providers after composition and reject missing, substituted, or populated owners before admission/publication. Do not manufacture bindings or positions. The P8-C registration checks zero cardinality; it does not require a zero revision. |
| P8-D route observations and Person route-plan history | The runtime creates new stores when no staged instance is supplied. Registration verifies empty route-plan history, exact provider/owner identity, zero cardinality, and zero revision for both P8-D rows. The package set has no P8-D payload stage for this Daily-v1 profile. | Rebuild the providers over target stores and include them in the fresh target census. Do not reuse source P8-D stores or treat absent providers as empty. |
| Global NPC-decision and economy keyed-sale receipt caches | `SimulationRuntime.TryRegisterP12ExactZeroReceiptOwners` reads each provider twice, requires stable exact owner identity and zero cardinality, then registers the ExplicitlyEmpty section. The selected profile has no payload exporter for either cache. | Use fresh cache owners for the target composition or reject unsupported source receipt state before staging. Include the target witnesses in pre-publication validation. Current registration permits a nonzero local revision when cardinality is zero; the target composition should preserve a fresh owner rather than reuse a cleared source cache. |
| Per-NPC P18 local-observation and merchant-trade receipt owners | `P12DNpcReceiptOwnerCensusProvider.CreateProviders` requires two distinct installed owners per exact live NPC, sorted by RuntimeId; each provider fails unless cardinality and revision are both zero. `IsExactCoverage` compares the provider family to the exact current NPC roster and owner references. | Rebuild this provider family only after staged D NPCs exist. The source provider list is tied to source NPC object identities and cannot be copied to the target. Verify exact target roster coverage before admission. |
| Crime/Justice P18 receipt sentinels | The current Daily census registers one sentinel per installed `CrimeSystem`/`JusticeSystem` and checks exact owner identity, cardinality one, and revision zero. The P12-E Justice capture repeats the exact Justice sentinel check. Crime's sentinel has no E snapshot consumer or receipt payload in this profile. | Install the staged/current target Crime and Justice authorities first, then validate each target sentinel against its target owner. A source sentinel or source system reference is not a target witness. Keep the sentinel data-free and at revision zero. |
| P18 temporal ActorChoice inputs | F capture reads the same `ActorChoiceStore` identity/revision as the required P11 row and rejects nonzero temporal input count, temporal captures/dispositions, or pending states. The reviewed package order captures F before C roots are staged, and the retained regression tests that order. F staging creates a new store from the P11 snapshot only; no temporal payload is staged. | Preserve the pre-root source rejection. After target F staging, query the target temporal provider and require zero before restored admission/publication. Keep this outside the 299 serializable-section vector. |

## Evidence and integration boundary

- `P12RuntimeIdentitySpatialCensus` declares the P8-A/B/C roles and initial cardinalities, and registers the two P8-D rows with zero revision.
- `SimulationRuntime.TryRegisterP12ExactZeroReceiptOwners` requires stable zero-cardinality witnesses for the two global caches.
- `P12DNpcReceiptOwnerCensusProvider` proves exact two-per-live-NPC provider coverage and rejects replaced or populated receipt owners.
- `P12CrimeJusticeInvalidation` checks the Crime and Justice sentinel identity/cardinality/revision before registering those required rows; `P12EJusticeRecordsOwnerSnapshot.TryCapture` independently requires the Justice sentinel at cardinality one and revision zero.
- `P12FActorChoiceSnapshot.TryCapture` checks temporal input state before constructing its detached snapshot. `P12FDailyV1OwnerCapture.TryCapture` invokes that capture. The retained regression `P12FOwnerPackageCapturesBeforeRootStagingAndStagesAggregateAgainstTheSameAttempt` establishes the source capture-before-C order; its exact-tip review is `PHASE12_G_CAPTURE_ORDER_TEST_REVIEW_22DE553.md`.

These are existing APIs and component guards, not proof that a future G call sequence uses them correctly. The P12-G package audit explicitly records that there is no current G orchestrator. Restored admission must read a fresh candidate-owned quiescent census, preserve the source day and successful-core sequence as scalar boundary facts, mint a target-bound token, and run immediately before one active-session exchange. It must not reuse a source token or mark the candidate publicly published merely to obtain a normal post-advance token.

## Remaining P12-G gates

1. Complete and validate the effective live-profile owner/provider inventory against all 299 expected sections, including dynamic and conditional owners, supported successful writers, and composed objects without a vector row.
2. Reconcile the exact supported source ingress for the remaining Expedition and Military mutation paths; do not add excluded gameplay or security checks.
3. Integrate the fresh target-owner checks above with current B-F package outputs and the reviewed restored-boundary API.
4. Complete the `TesteSimulacao` single active-session root and consumer audit, then prove failed staging leaves the old graph active and successful restore performs one exchange.
5. Independently review and accept the bounded implementation checkpoint as required by `EXECUTION_MODEL.md` before starting G implementation. P12-G remains `WAIT_DEPENDENCY`; P12-A remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains open.
