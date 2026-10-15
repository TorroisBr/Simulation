# CORE-F post-demographic source/access reconciliation

## Identity and purpose

This is a post-promotion source-graph observation for the bounded demographic-owner extraction. It is anchored to Core canonical `b2c2c0e94c7a8dd9175a8db3fab4fb9d78dcfb2a`, whose implementation commit is `9c1ed5e2065f2d509b61e63b39fb6c84508fd5fe` (tree `f9934338811f7272c8f7013f5bbda14e06c4a92e`). The exact-tip implementation review and its evidence are in [`../IMPLEMENTATION_REVIEW.md`](../IMPLEMENTATION_REVIEW.md) and [`../VALIDATION.md`](../VALIDATION.md). This directory records the current compiler/source graph and a bounded overlay for the moved owners; it does not replace the historical owner or operation matrices.

## Generation and integrity

The graph was generated from Unity 6000.3.9f1's current `Assembly-CSharp` compiler response using the archived Roslyn analyzer source at `docs/validation/CORE_F_SourceClosure/audit-source/Program.cs.txt`, with SDK/Roslyn 9.0.315. The compiler response SHA-256 is `bf4cba9081b5f7d2a5383218b63d3614c68d475ce16a6f702dbae7e97b45aa15`.

The archived analyzer contains an old fixed expected-source count. The disposable generation copy changed only that guard to accept the measured input count; the archived source was not edited. The analyzer's compiler inputs, parse options, symbol inventory, reference resolution, and graph logic were otherwise used as archived. The manifest records this limitation. The omitted parser-added reference is one unresolved synthetic `mscorlib.dll`; the distinct Unity `mscorlib` reference is resolved and hashed. No production source was changed to generate this evidence.

All 322 `source-inventory.csv` paths resolve to the exact recorded Git blobs at the implementation tip. All 44 source entries in `core-build-inputs.csv` likewise match their recorded Git blobs. A privacy scan found no absolute local paths in the nine generated data artifacts. CSVs and manifests use repository-relative source paths only. The workstation's `core.autocrlf=true` initially caused seven CSV artifacts to be staged with different line-ending bytes; only line endings were normalized to LF, all CSV records remained unchanged, and the listed hashes were recomputed. The local `.gitattributes` pins generated CSV/TXT files to LF across Windows checkouts so their SHA-256 identities remain verifiable. SHA-256 identities:

| Artifact | SHA-256 |
|---|---|
| `analysis-manifest.txt` | `d482c36cca9c278a30fb8d329e361e0c89a001b6c5e803e14165011d2743877e` |
| `core-build-inputs.csv` | `eef956bf5e8c3b4f08ecf17c515aa7d5f0548847799d84f33f84635b7c759aa4` |
| `core-symbol-edges.csv` | `c68c5a56b235bf6eae43e8ff1850ca83db5765c90805a754747b7c22c87fce66` |
| `metadata-references.csv` | `ed9429e1eb5628676c57feb793cb0cab5380d4276b4613574bfbf74964038ee5` |
| `owner-source-identity-join.csv` | `c9d4b372e7be2b052c1ffb539f4586829be62c7be6b65e6cc52daa2b6f7ffee1` |
| `preprocessor-symbols.txt` | `8cbb9654286f2e8918e64573b6137ad15f73bb9720882984fccb5a3175490d50` |
| `source-edges.csv` | `b57d60e9bf8d5a1cee4742cd27c41558d565664ac68f01e0d728ecae3ed7a1c3` |
| `source-inventory.csv` | `9fc596b98cfd285222943278f78a3866d211cfe9c888a58dcf0094d71052ae13` |
| `unity-symbol-edges.csv` | `9af4ab5c5bcffcd7557f96b5e185ba99225b5f6e5ce54f689bf946e43b432c83` |

The preprocessor-symbol file was normalized from the generator's escaped-newline form to ordinary line breaks, consistent with the prior graph artifact convention. Its hash above is for the committed representation.

`owner-source-identity-join.csv` is a mechanical, additive join of source-file references already present in the promoted `OWNER_WRITER_MATRIX.csv`, `OWNER_CAPTURE_TARGET_CROSSWALK.csv`, and `OPERATION_CASE_MAP.csv` to the current canonical Git tree. It contains 1,660 matrix-field/path associations over 142 distinct source files: 89 Unity application sources and 53 Unity test sources. All references resolve and carry the exact current Git blob. It does not edit or replace the original matrices. In the historical operation-map field, only `runtime.person.parentage` and `runtime.person.residence-bind` used normalized P12 family keys; the other 22 used descriptive phrases. The additive [`OWNER_OPERATION_CURRENT_OVERLAY.md`](OWNER_OPERATION_CURRENT_OVERLAY.md) now maps current source-backed family keys for all 24 documented operation cases where the source constructs those IDs, while keeping the historical map intact. None of the historic 89-family/capture-target source citations names the newly promoted Core owner source paths. These are concrete reconciliation gaps, not evidence that the corresponding runtime behavior is absent.

### Current source-identity rebind audit

The joined blobs were compared with the evidence baseline `8ea3b47e9ab1be9ab3fe6261a73dc33fece8cd8f`. This comparison is an identity refresh, not a claim that every changed source needs semantic revalidation:

- All 89 writer-matrix `source_path/source_blob` fields refer to the shared historical `P12AContinuationImage.cs` source (`00be1456…`), not to the owner implementation body. Its current blob is `b324bdab7d4432b36e923b767a5b81bb129dc46e`. The nine moved owner bodies and their current package identities are listed below; the historical matrix field must not be interpreted as the owner-source identity.
- 87 of 89 provider-source references match their historical blobs. The two exceptions are `p12d.city-population.aggregate/` and `p12d.city-population.operation-receipts/`: both remain in `SettlementPopulationCensusProvider.cs`, whose current blob is `4fc8fc273aa2943aaededde61e95d36213b655c9` (historical `36a6c2d6…`). The changed read goes through `PopulationOwnerBridge.GetOperationReceiptCensus`. Genealogy's provider source remains identical.
- Five of the nine shared owner bodies are exact source moves: `AggregateDemographyContracts.cs`, `AggregateDemographyTransition.cs`, `PopulationChangeSet.cs`, `SettlementPopulationSystem.cs`, and `SettlementPopulationTransition.cs`. Four changed during extraction: `AggregateDemographySystem.cs`, `SettlementPopulationRuntime.cs`, `GenealogyStore.cs`, and `GenealogyContracts.cs`. The directly affected owner families are the two City population families (seven population sources) and `p12d.genealogy.parentage` (two genealogy sources). This is the current owner-source mapping; it does not expand the set of P12 families.
- The capture, encoder and decoder associations contain 254 source references, each matching its historical cited blob. The D capture/stage package references match for all 89 rows. Of 102 codec-case source associations, 89 still match; the 13 changed associations all cite the expanded `P12AContinuationCodecTests.cs` (`a484f7eaf0d2f163d4c5d3fcac743931576b1143`), which added exact-empty coverage. The current ALL EditMode evidence is bound to this promoted implementation; historical source hashes remain historical.
- Restore-negative case associations retain 70 exact source identities. The other 89 references point to two files that changed: 87 to `SimulationRuntimeAdmissionTests.cs` (current `116206e14e93f2864a1f54ee16fcd9403ebfb247`) and two to `P12GPoliticalBindingRestoreTests.cs` (current `59c08a3abce2dfb082efffaa99a5216b1acbe4b9`). This source-identity change alone does not imply a failed retained case.
- The current selected Daily-v1 composition test builds expected owner instances for each registered section, checks the expected section-ID set against the census inventory, and checks each provider's `OwnerInstanceIdentity` against its expected runtime/bootstrap owner. This is direct identity evidence for that selected composition and its retained transitions; it is not an all-world constructor proof or a complete writer/epoch census.
- The historical operation map's 17 admitted-root source pointers all named old `SimulationRuntime.cs` (`a1880b40…`); current is `4344293459f48eff44a433bc6438b34a21196981`. The current 24 registration pointers resolve to 17 current runtime sources, six `P12PopulationLifecycleCensus.cs` sources (`1a714ce53d68e447a85fd74d8a43bc86217b1ed4`), and one unchanged `P12TravelPartyStartOperation.cs` source. Seven historical operation-case pointers (advance-day, bootstrap-publication, four economy cases, and merchant advance) use the changed `SimulationRuntimeAdmissionTests.cs`; the other 17 source identities remain exact. The current operation overlay re-walks these 24 supported cases and is still not an exhaustive ingress inventory.

These rebinds establish which historical source pointers are current, stale, or shared-root references. They do not establish all live aliases/members, every writer and bypass, revision/epoch closure, global owner-thread quiescence, a completed-boundary token, or detached-host continuation parity. Those are still explicit dependencies of the next package.

## Current graph result

- `Assembly-CSharp`: 322 source files; 2,923 unique directed file-pair declaration-reference edges; 50 shared-partial declaration links; 122 strongly connected components (SCCs).
- Reachability within that assembly: 227 runtime-reachable, 296 restore-reachable, and 26 outside both closures.
- The source-inventory classification counts are 229 `CoreMove`, 67 `SplitHostDomain`, and 26 `HostOnly`.
- Unity references: 477 symbol edges across 76 files; 57 of those files are runtime-reachable and 67 are restore-reachable.
- Shared Core references: 2,005 symbol edges across 176 files. The SDK project currently compiles 44 explicit Core source links.
- Roslyn produced 0 errors and 8 warnings (`CS0108`, `CS0618`). These are graph-parser diagnostics, not a new product build claim.

The prior 0094 planning graph recorded 329 `Assembly-CSharp` sources. The current graph captures the post-owner source partition: the nine owner source bodies are compiled from the shared Core package and are no longer `Assembly-CSharp` nodes; the Unity application retains its adapter/contracts and has the corresponding new binding source. The current counts are generated from the actual response file, not extrapolated from the prior graph.

## Moved source identities and current Core boundary

The nine approved owner source bodies now compile from these package paths:

| Owner source | Current package path | Current Git blob |
|---|---|---|
| `AggregateDemographyContracts.cs` | `Packages/com.simulation.core/Runtime/Population/AggregateDemographyContracts.cs` | `60a1003c25bf088cb870961582fc05b82fd525f9` |
| `AggregateDemographySystem.cs` | `Packages/com.simulation.core/Runtime/Population/AggregateDemographySystem.cs` | `7de6cf29590fa2034a8c12b919065f71260d0647` |
| `AggregateDemographyTransition.cs` | `Packages/com.simulation.core/Runtime/Population/AggregateDemographyTransition.cs` | `bb3feb9edbedb081f6822d5d2fd5586c71f743a9` |
| `PopulationChangeSet.cs` | `Packages/com.simulation.core/Runtime/Population/PopulationChangeSet.cs` | `a7e5ce18234d5bdb4f0b425d4fa5572f9e860730` |
| `SettlementPopulationRuntime.cs` | `Packages/com.simulation.core/Runtime/Population/SettlementPopulationRuntime.cs` | `76254a69f4c7f19a66fed45a45fde218e96fc8eb` |
| `SettlementPopulationSystem.cs` | `Packages/com.simulation.core/Runtime/Population/SettlementPopulationSystem.cs` | `af7ccc809a59e864f474201e278e0eb1bde44ae2` |
| `SettlementPopulationTransition.cs` | `Packages/com.simulation.core/Runtime/Population/SettlementPopulationTransition.cs` | `21bda8ceaec40bb8e2d4d04831a9bd2dca0e61cd` |
| `GenealogyStore.cs` | `Packages/com.simulation.core/Runtime/Genealogy/GenealogyStore.cs` | `e8b6a9caefcac3a285c90b93a1627c13f8ed41d9` |
| `GenealogyContracts.cs` | `Packages/com.simulation.core/Runtime/Genealogy/GenealogyContracts.cs` | `feede4d2fd1065fc29d9d3bb2daf00201d1fc84e` |

The Unity `P12AGenealogyOwnerCodec.cs` is a direct `ParentageRecord` consumer (Git blob `52ab45966526cd85710fa6d649d3aace5589cf93`); it remains an adapter and does not duplicate the shared owner implementation.

The adapter boundary also compiles `PopulationOwnerBridge`, `PopulationRollbackPoint`, `GenealogyOwnerBridge`, `IAuthoritativeMutationStatus`, and `MutationStatusBinding` from the shared package. The read-only mutation-status view binds to the original Unity `AuthoritativeMutationGuard`; it does not introduce another guard or epoch authority.

## Access overlay and retained authorities

The promoted P12 owner families remain the authority for capture, staging, writer and epoch semantics. This graph supplies current source identities and declaration edges around the moved owner API; it does not replace the existing owner/operation/capture-target evidence.

| Owner/family | Shared owner | Current Unity-side access and retained P12 edge |
|---|---|---|
| Aggregate population | Seven population source files above; `PopulationOwnerBridge` and `PopulationRollbackPoint` | P12 families `p12d.city-population.aggregate/` and `p12d.city-population.operation-receipts/`. Current bridge callers include `BattleOutcomeApplication`, `CityRuntime`, `P12ACityOwnerCodec`, `P12DCityRootOwnerSnapshot`, `P12PopulationLifecycleCensus`, `PersonDeathLifecycle`, `DailyDemographyBoundaryStepProvider`, `NpcResidenceMigrationSystem`, `SettlementPopulationCensusProvider`, and `SimulationRuntime`. The owner and current writers remain the existing same-instance runtime authorities. |
| Genealogy | `GenealogyStore` and its contracts; `GenealogyOwnerBridge` | P12 family `p12d.genealogy.parentage`. Current bridge callers include `GenealogyMutationGuardBinding`, `P12DDailyV1OwnerPackage`, `PersonGenealogySystem`, and `SimulationRuntime`; direct source users also include `P12AGenealogyOwnerCodec` (which consumes the moved `ParentageRecord`), `WorldStateSnapshot`, `GenealogyCensusProvider`, `P12EDailyV1OwnerPackage`, `P12GDailyV1RestoreCoordinator`, `SimulationBootstrapComposition`, and `SuccessionCandidateSystem`. |
| Guard identity | Shared `IAuthoritativeMutationStatus`; Unity guard remains writer/authority | The interface is consumed by `AuthoritativeMutationGuard`, `CityRuntime`, and `GenealogyMutationGuardBinding`. The graph confirms the declaration path but does not prove run-time binding or every epoch callback; those remain governed by the promoted exact-tip tests and owner matrix. |

The 89-family matrix and 24-operation map remain historical, accepted-scope evidence. This overlay identifies moved-source and bridge edges that need to be joined into the next owner ledger; it does not silently rewrite their old source blobs or claim that all 89 current implementations have been revalidated. The City aggregate/receipt and Genealogy parentage families are the directly affected P12 rows. Population lifecycle, Person life/residence and NPC residence operations are linked access paths that must retain their existing writer and invalidation behavior. The receipt family is registered and captured, but the current accepted Daily-v1 population operations do not admit a receipt-changing writer; see [`OWNER_OPERATION_CURRENT_OVERLAY.md`](OWNER_OPERATION_CURRENT_OVERLAY.md) for the exact-zero boundary and evidence.

## Readiness consequence and limits

The graph proves that the approved owner bodies compile from shared Core and enumerates declaration-level consumers in the current Unity assembly. It is not a method-call graph, does not establish every instance alias or constructor, and does not prove full owner cardinality, all writers, revision/epoch coverage, all source exports, staged hydrators, references, rejection atomicity, or detached-host parity. A source file's membership in runtime/restore reach is not owner-completeness proof.

Accordingly, the demographic-owner checkpoint is promoted within its reviewed scope. The next `CORE-F-Shared-DailyV1-Application-and-Host` package remains `WAIT_DEPENDENCY`: the exact 89-family member/source/access ledger still needs a current-source join across owner instance, constructor, every admitted writer and invalidation path, capture/export, decoder/hydrator, relation and exact rejection. The current graph also retains a substantial Unity boundary (76 Unity-referencing files) and large application SCCs; it does not make the shared bootstrap/runtime or detached restore implementation ready. Continue with a bounded owner-ledger reconciliation and source/member exception list before dispatching the serialized runtime hotspot. The fixed Daily-v1 scope, P12 contracts, and CORE-X deferral are unchanged.
