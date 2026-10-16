# CORE-F Institutional and Political-Claim Owners — Master Handoff

**Objective:** implement the bounded institutional/claim owner source extraction and preserve selected Daily-v1 capture/restore behavior.

**Design baseline:** architecture a20978d2b61e07a4b131afbb3971aae346511acb; promoted Core b2c2c0e94c7a8dd9175a8db3fab4fb9d78dcfb2a; demographic code 9c1ed5e2065f2d509b61e63b39fb6c84508fd5fe (tree f9934338811f7272c8f7013f5bbda14e06c4a92e).

**Dispatch:** READY_FOR_IMPLEMENTATION after independent technical review records PASS and this reviewed handoff is published on the architecture canonical (`codex/architecture/world-identity-projection`). Before then it is a complete technical design candidate. Master verifies that the referenced Core baseline and source identities remain applicable before implementation. This handoff grants no full CORE-F acceptance or promotion.

## Objective and completion criteria

Move the seven exact source bodies in CORE_F_INSTITUTION_CLAIM_OWNER_TECHNICAL_DESIGN.md to the named Core paths. Add the two narrow status/owner bridges defined there. Update family seams in CORE_F_INSTITUTION_CLAIM_OWNER_FAMILY_LEDGER.tsv and every member/caller entry in CORE_F_INSTITUTION_CLAIM_OWNER_MEMBER_CALLER_MAP.tsv. Preserve all six Required P12 families, one exact owner instance, direct binding to the existing guard object, revision/epoch behavior, failure outcomes and P12 bytes/schema. Add restored-only exact-revision clones for PoliticalClaimStore and OfficeStore; keep ordinary and null-source constructor replay unchanged. Verify source-image capture through P12-G restore recapture for closed/same-day tenure and resolved/re-recognized claims. Office continuation cloning preserves the source revision and exact TenureHistoryInMutationOrder after current Runtime person/time/reference validation.

Complete when:

- Each moved type has one compiled implementation. No copy, stub, duplicate Unity owner, Unity reference, IVT workaround or general public Store writer is added to Core.
- Core compiles without Unity. Existing Unity types use the same Core owner objects. The original Runtime guard object is passed by reference through IAuthoritativeMutationStatus.
- Every source/member caller and P12 seam in the access ledger is updated. No successful commit callback, epoch effect, owner alias, failure path or restore stage is lost.
- An authentic source-image capture/P12-G restore/recapture preserves PoliticalClaimStore revision after repeated recognition updates and resolved/re-recognized cases, and OfficeStore revision 3 plus exact TenureHistoryInMutationOrder after register/assign/vacate, including same-day tenure. Ordinary and null-source construction stay compatible. Institution/Office row set and identity remain equal across capture/stage/runtime clone. Office restore clone must preserve the supported RegisterOffice (revision 1), TryAssignIncumbent (2), TryVacateOffice (3) result and the in-place closed-tenure order.
- Existing profile/codec vectors remain byte-compatible; malformed inputs still reject atomically. Inputs/results are fully retained, exact-tip review and normal promotion preflight pass.

## Files and serialized hotspots

**Core source:** seven files/destinations in the design. Bridges: Packages/com.simulation.core/Runtime/Institution/InstitutionOfficeOwnerBridge.cs and Packages/com.simulation.core/Runtime/Political/PoliticalClaimOwnerBridge.cs. Preserve existing script meta GUIDs and exact existing public domain types.

**Unity callsites:** Assets/_Project/Scripts/InstitutionOfficeCensusProviders.cs; PoliticalClaimStoreCensusProviders.cs; P12EInstitutionOfficeOwnerSnapshot.cs; P12EPoliticalClaimOwnerSnapshot.cs; SimulationRuntime.cs; CrimeSocialAppraisal.cs; PoliticalKnowledgeStore.cs; Core SDK project and Simulation.Core.asmdef. Keep P12 codecs, DTOs, package composition, P12-G restore coordinator, Runtime lifecycle and publication in Unity.

SimulationRuntime.cs and P12 E/G sources are a serialized hotspot. Core-only source prep can be isolated after reviewed bridge boundaries; one owner integrates SDK links, asmdef and Unity callsites. Check live ownership before integration.

## Integration order

1. Refresh exact canonical/worktree state and source blobs against the stated baseline; classify any advancement. Recompute current compiler edges, SDK/asmdef membership, callsites, and family identity joins.
2. Move seven source bodies once; remove old Unity compilation ownership only when replacement Core links are active. Add bridges and noEngineReferences compile ownership.
3. Replace guard coupling in Core with direct IAuthoritativeMutationStatus. Bind the same existing guard object. Route internal Runtime/P12 access through the named bridges, retaining exact failure behavior. The legacy CloneOfficeStore loop is preserved; its internal TryAddHistoricalTenure call routes only through TryAddHistoricalTenureForRuntimeOfficeClone, which forwards the existing failure unchanged and is not a general writer.
4. Adapt snapshot/census callers without changing section IDs, schemas, payload layout, ordering or token protocol. Keep private restore validation, vector recapture and one publication.
5. Preserve all cross-owner/time validation in SimulationRuntime. Route only IsRestoredContinuation clones through exact-revision PoliticalClaim and Office clone bridges after all existing cross-owner/time checks; route ordinary construction through current replay-compatible paths, with only the claim-recognition and office historical-tenure clone mutations routed through their narrow forwarding adapters. Runtime keeps the current CloneOfficeStore replay as its complete validation pass and preserves its failure translation. Its historical-tenure insertion calls the exact forwarding bridge; no PersonStore logic moves into Core. For restored contexts it discards that replay candidate, then uses the exact owner bridge to copy immutable source rows into fresh collections in mutation order and set source revision exactly. Ordinary and null-source paths remain unchanged.
6. Run the focused Core .NET and Unity suites, full Core cold/warm, full Unity EditMode and official Smoke gates in the design; run LongRun when required by repo gate or selected behavior. Capture complete inputs/results/toolchains under current evidence policy. Run diff check. Obtain independent exact-tip review.
7. Master verifies Core baseline/source applicability and reviews the complete candidate diff against its actual architecture base. Publish these docs only after exact-tip independent review; any implementation or later promotion still requires its separate review, evidence applicability, State update and all autonomous bounded-promotion conditions. This package does not close CORE-F.

## Dependencies, exclusions, stop conditions

Dependencies: exact promoted Core b2c2c0e APIs, IAuthoritativeMutationStatus pattern, selected Unity Daily-v1 composition and P12 E/G contracts, post-owner graph and current operation overlay.

Excluded: remaining P12 E/F/G owners; property/estate/faction/support/knowledge/decision/military/crime owner authorities; migration or replacement of Runtime/bootstrap/clock/lease/token/census/epoch/restore/publication authorities (the named Runtime adapter and restored-clone corrections remain included); definition conversion; detached .NET host; descriptor-v2/cross-host compatibility; content/profile/architecture/Phase scope changes; blanket movement based on graph counts.

Stop only the affected slice if source adds an unresolved Core dependency, owner alias changes, a P12 vector/payload changes, a writer callback is unmapped, exact guard identity cannot be retained, current failures change, or repeated-recognition evidence falsifies the revision counterexample. Preserve candidate/evidence. Full CORE-F remains WAIT_DEPENDENCY and CORE-X remains deferred.


## Migration and rollback

This is a seven-source, 2,846-line baseline cohort (including comments/blank lines): contracts519, stores635, vacancy190, claim contracts622, claim store383, claim transitions320, property contracts177. Adapter/tests/bridge changes are additional; line count is inspection scope, not a duration estimate. Complexity is moderate because algorithms stay the same while assembly access, shared guard binding and two restored-clone defects must be integrated.

Use an isolated branch from current Core; move each source/meta once, retain global type names and GUIDs, add explicit SDK Compile links and preserve the noEngineReferences asmdef. Keep Unity authoring, public vacancy API and all existing consumers compiling against the same Core definitions. No new definition input or Unity asset conversion is required by these seven files. Core PersonId and existing mutation-status binding are reused. Preserve canonical definition identity in excluded owners.

Keep matching pre-migration build/content/save evidence. Assembly movement changes real assembly/build identities; continue truthful same-build P12 admission rather than pretending old/new descriptors match. This package does not promise old-save migration or cross-build compatibility. Payload/schema value equivalence is distinct from descriptor equality across builds.

Integrate source, bridges and all Unity adapters as one reviewed coherent candidate. If validation rejects the move, retain the candidate and diagnostics. Roll back only through an additive reviewed revert with the matching previous build/content/save, never reset shared history, overwrite a slot, fabricate an MVID or compile duplicate domain implementations. Runtime/P12/SDK/asmdef integration is serialized; independent evidence analysis may continue.