# P12-E Daily-v1 owner-set composition contract

**Status:** Current-base technical integration contract for the accepted P12-E checkpoint. It is not a new checkpoint ID, P12-A authorization, P12-G implementation, Phase 12 closure, or a canonical promotion.

**Canonical base:** `codex/phase12/canonical` at `12339dd423bcab787ef5d10fad4c6e2b1597603d`.

**Authority:** The accepted scope is `docs/phases/PHASE12_BRIEF.md` and `docs/design/PHASE12_CAPABILITY_CHECKPOINT_DECOMPOSITION.md` (§ P12-E). Owner boundaries and hydration rules are in `docs/design/PHASE12_E_TECHNICAL_DESIGN.md`. The promoted current-profile source crosswalk is `docs/design/PHASE12_E_EFFECTIVE_PROVIDER_OWNER_CROSSWALK_A2E8B69.md`. This contract composes those existing contracts and the already promoted owner adapters; it does not redefine them.

## Objective

Add one internal, unpublished `P12EDailyV1OwnerPackage` assembler that captures and privately stages the complete P12-E owner set present in the accepted `UnityBootstrap-Daily-v1` composition. It is the integration seam between the existing E owner adapters and a future P12-G whole-profile composer.

The assembler must use the exact P12-B `DailyCaptureEligibilityToken` and its same `OwnerSections` object for every E capture. It consumes, without modifying, the already staged P12-C continuation roots and P12-D owner package. It returns all E stores as one private package only after E-local and E-to-C/D validation succeeds.

## Included E owners

The source set is fixed by the current promoted crosswalk and existing adapters:

| Owner snapshot | Staged owner output | Required staged roots |
| --- | --- | --- |
| `P12EInstitutionOfficeOwnerSnapshot` | `InstitutionStore`, `OfficeStore` | P12-D `PersonStore` |
| `PropertyEstateOwnerSnapshot` | `PropertyOwnershipStore`, `EstateStore` | P12-D `PersonStore`; captured absolute day |
| `P12EFactionOwnerSnapshot` | `FactionStore` | P12-D `PersonStore`; captured absolute day |
| `P12EPoliticalClaimOwnerSnapshot` | `PoliticalClaimStore` | P12-D `PersonStore`, Property, Institution, Office; captured absolute day |
| `P12EPoliticalSupportOwnerSnapshot` | `PoliticalSupportStore` | P12-D `PersonStore`; staged Faction and PoliticalClaim |
| `P12EPoliticalDecisionOwnerSnapshot` | `PoliticalDecisionStore` | P12-D `PersonStore`; staged Institution, Office, PoliticalClaim |
| `P12EMilitaryOwnerSnapshot` | `ArmedForceStore`, `ContingentManpowerStateStore`, `ArmedForceSpatialStateStore` | P12-D `PersonStore`; P12-C `SpatialAuthorityStore`; `LocalTopologyStore == null` |
| `PersistentConflictOwnerSnapshot` | `PersistentConflictStore` | staged ArmedForce |
| `PersistentWarOwnerSnapshot` | `PersistentWarStore` | staged ArmedForce and Conflict |
| `PersistentBattleOwnerSnapshot` | `PersistentBattleStore` | staged ArmedForce, Conflict, War; P12-C `SpatialAuthorityStore`; `LocalTopologyStore == null` |
| `P12EJusticeRecordsOwnerSnapshot` | `JusticeSystem` | P12-D staged Cities, NPCs, and Persons; exact admitted Justice status definitions and transient runtime services required by the existing adapter |
| `P12ECrimeSocialAppraisalOwnerSnapshot` | `CrimeSocialAppraisalWorldState` | P12-D `PersonStore`; staged Institution; supplied staged `SimulationTime` |

The Crime/Social package contains the existing E-owned TheftOutcome, CrimeKnowledge, and SocialReaction stores. It does not include the P18-only transient receipt caches. The current provider crosswalk remains authoritative for disabled/absent providers and for Economy/Merchant facts owned by D or F. Do not create a second owner for those facts, or invent a mutable owner for Guard.

The selected P12-B vector remains authoritative for owner identity, section identity, schema, cardinality, and local revision. Required-empty owners stay represented as required exact-zero witnesses; an absent owner is not treated as empty. Current Daily-v1's exact-zero PoliticalDecision admission remains in force, so P12-E must not imply that non-empty live PoliticalDecision capture is admitted. Existing adapter support for populated detached values does not widen P12-B admission.

## Internal boundary

The implementation may use one internal immutable staging-context type and one package assembler. The effective operation is:

```text
TryCaptureAndStage(
    sourceRuntime,
    exactCompletedToken,
    exactTokenOwnerSections,
    stagedP12CRoot,
    stagedP12DPackage,
    stagedSimulationTime,
    admitted E staging definitions/services,
    out privateP12EPackage,
    out failure)
```

This is a private in-assembly boundary, not a public API. Source-owner objects must be obtained from the exact census entries in `exactTokenOwnerSections` (or the existing owner-specific accessor already used by the adapter), then passed to their current adapters. Do not introduce a new registry or duplicate owner discovery. The context requires:

- `ReferenceEquals(exactCompletedToken.OwnerSections, exactTokenOwnerSections)`;
- the token validates against `sourceRuntime` before capture and after capture/staging;
- token profile is exactly `UnityBootstrapDailyV1` and the token denotes a successful completed boundary;
- source, C, and D `WorldId` values agree;
- the staged `SimulationTime.AbsoluteDay` equals the token's `AbsoluteDay`;
- the supplied D package and C roots are private staged values from the same enclosing reconstruction attempt and exact token;
- P12-E receives the exact admitted definitions and transient `JusticeSystem` construction services already required by its owner adapter. It neither resolves replacements nor creates defaults.

P12-E does not own `SimulationTime`, the calendar, profile admission, token issuance, or a second capture lock. The enclosing P12-G composition supplies a private staged time whose absolute day matches the token. This package does not capture or stage `SimulationTime` itself.

## Capture and staging sequence

1. Validate the boundary/context and matching C/D `WorldId` values. Do not access or mutate active runtime state beyond the existing read-only owner snapshot adapters.
2. Capture every included E owner snapshot against the one token and exact owner-section vector. Preserve each adapter's exact owner identity, schema, cardinality, revision, detached values, and supported mutation-boundary checks. Do not capture D-owned City/account/market facts again.
3. Revalidate the token after capture. A stale token or changed section vector returns failure and no package.
4. Stage the private E graph in adapter dependency order:
   - Institution/Office, Property/Estate, and Faction from the exact staged Person root and captured day;
   - PoliticalClaim from Person, Property, Institution, and Office;
   - PoliticalSupport from Person, Faction, and PoliticalClaim;
   - PoliticalDecision from Person, Institution, Office, and PoliticalClaim;
   - Military from Person and P12-C SpatialAuthority, with a verified null LocalTopology;
   - Conflict, then War, then Battle, using the exact parent stores; Battle also receives P12-C SpatialAuthority and a verified null LocalTopology;
   - Justice from the exact staged D City/NPC/Person roots and admitted status/service inputs;
   - Crime/Social Appraisal from the exact staged Person/Institution roots and supplied `SimulationTime`.
5. Validate every E owner against its exact snapshot and every relation against the exact C/D staged roots. Do not repair, normalize, infer, or replace missing relations. Keep P12-D-owned roots and the C identity/spatial roots unchanged.
6. Preserve the P12-E → P12-F PoliticalDecision knowledge references as typed unresolved-binding evidence for P12-G. Evidence carries the `PoliticalDecisionId`, the ordered `KnowledgeReferences`, `ExpectedKnowledgeRevision`, and `ExpectedWorldRevision` from that exact decision record. Do not read, stage, or resolve `PoliticalKnowledgeStore`; do not treat E-local CrimeKnowledge as that F-owned authority. No other F binding may be silently omitted: if the current owner crosswalk or adapter reveals one, block this contract for review before implementation.
7. Revalidate the completed-boundary token after all staging. Return a `P12EDailyV1OwnerPackage` with immutable collection wrappers and the unresolved-F evidence only if every prior step succeeds. On any failure, return no package; all partial E candidates remain unreachable, and active runtime plus staged C/D roots remain unchanged.

## Validation and integration constraints

The implementation stays in the P12-E owner composition layer. It may add the package/context and focused composition tests and narrowly needed owner-source accessors. Any accessor must expose the exact installed owner only to this internal boundary and must not change mutation, registration, census, or runtime behavior. Keep `SimulationRuntime` changes to the minimum source/access seam; do not add runtime/bootstrap publication or broaden the active ownership window.

Required focused tests cover:

- every E section is captured once from the exact token vector and maps to exactly one output owner;
- required exact-zero owners remain explicit; the current Daily-v1 PoliticalDecision exact-zero gate is enforced;
- successful staging with supported empty and populated E values, including cross-owner relations, where the current P12-B profile admits those values;
- each E-to-C/D dangling, duplicate, mismatched-owner, wrong-day, and incompatible-definition case fails without returning a partial package;
- PoliticalDecision F knowledge references remain intact in typed unresolved evidence and are not resolved by E;
- P10 `LocalTopologyStore` remains `NOT_COMPOSED` (passed as null to Military/Battle); an injected/composed topology fails;
- token invalidation before, during, or after capture/staging returns no package;
- failure leaves the source runtime and the supplied staged C/D roots unchanged;
- package output is deterministic for the same accepted input and preserves each owner's order/multiplicity semantics.

Then run the complete P12-E focused set, ALL EditMode, official Smoke, and `git diff --check`, retaining XML/log hashes against the exact code tree. Independent implementation review must check the full diff, owner/section cardinality and revision assumptions, atomic failure behavior, ordering, Daily-v1 exclusion boundaries, and exact validation tree.

This package does not implement P12-G parsing/envelopes, global ID uniqueness or shared-sequence validation, E/F binding resolution, P12-F, whole-profile continuation parity, mutation-guard binding, runtime publication, P12-A integration, P13, or Phase 12 closure. Those remain with their accepted checkpoints and gates. P12-E remains open until the owner crosswalk and all accepted E owner exports/stagers are integrated and independently validated.
