# Independent implementation review — Core-owned selected City census rebind

## Verdict

**VALIDATED_CANDIDATE — PASS.** Exact-tip implementation and evidence review found no actionable findings within this bounded City census rebind. This review does not approve Core canonical promotion or complete CORE-F.

## Exact identities

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Reviewed candidate commit: `49710bf3a1ab715944fded359f39ac1a129a59e7`
- Reviewed candidate tree: `534b7a0379d5763e987f5e740b7cb4ff7ba144b8`
- Exact parent: `7d78d673c9bc88b87f8b28f5ec657efde28367c1`
- Reviewed integration base: `b3cc6a7f168b2eaeaa19e57e9398d3040b5bc5ff`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Separate review branch: `codex/review/core/CORE-F-city-census-rebind-49710bf`

The candidate commit adds the City census rebind, its additive Core regression tests, and the validation record/package. The cumulative base-to-candidate tree also contains the previously implemented SpatialKnowledge and Person census rebinds; this exact-tip review inspected the final composed code paths and focused on the City change.

## Reviewed behavior

The City rebind derives the four selected census rows from the exact `DailyV1CityRuntimeOwners` graph: important-NPC presence, Market stock rows, aggregate population, and population operation receipts. It validates each City’s unique runtime identity and binding to the admitted definitions; emits the existing canonical section IDs and schema; requires `Required` role, exact owner object identity, cardinality, and local revision; and uses the existing encoded City key for the three P12-D City image families that require it.

Population aggregate and operation receipts both identify the same `Population` owner instance, matching the existing City population alias family. The aggregate census is a required singleton (`cardinality = 1`) even when the current population value is zero. Receipt cardinality/revision are taken from the Population owner’s receipt census.

The host’s four City-family row counts must agree. Missing or duplicate section IDs and malformed selected owner graphs fail closed. In the selected-only case, every expected City section must exist and match the Core witness; mismatched captured rows are preserved unchanged rather than overwritten. In mixed-roster composition, the complete host census vector is returned in its original order after confirming every selected Core row is present. Core does not splice its selected City subset into compatibility rows.

This leaves the existing full P12 validator authoritative for the complete 89-family vector. It still checks the exact family key sets, roles, identity/alias rules, and `61 + 22*N + U + P + 4*C` cardinality before token publication; the complete-vector gate was not weakened. The P12-G City source capture also rechecks each selected City row against its exact owner, schema, cardinality, and revision before encoding. Thus malformed compatibility rows cannot become a valid captured image merely because the mixed-roster rebind preserves them.

The new tests are additive. They cover all four exact Core bindings, aggregate/receipt aliasing, the zero-valued singleton, host row-order preservation, complete mixed-roster preservation, uneven family counts, missing/duplicate rows, and preservation of stale role/schema/identity/cardinality/revision snapshots for downstream P12 classification. No existing assertion was removed or weakened.

## Validation and evidence

### CoreValidation

The protected official CoreValidation manifest is run `a96a85ceeb6e47d2bbc0dd3011913069`. Cold and warm TRX files each report 955/955 passed, with zero failed, not-executed, aborted, or inconclusive cases. Their actual SHA-256 hashes match the protected manifest and the public validation record:

- Cold TRX: `a8fd280d59ad73e9cc5a24aa1278147fb0b30515638c3048a49f2548ff6ecaef`
- Warm TRX: `567cdda9187518d42238ead233c74486a243d8eb5d6a4eca0acb6e129983791f`
- Protected CoreValidation manifest: `93f098004d64f73f5be5d7e934baf6709aa6cbad3b53a50400b973c62dab6448`
- Stable input digest: `4082ae30e0ce4fdf4fdf64bdf2bfed771f3aae63ee5d5484ecb92115fcf5a041`

All 536 manifest input-file hashes match the candidate validation worktree. The two files changed for this slice match their recorded input hashes: `DailyV1RuntimeAdvanceStage.cs` (`ecf64a29cd5116c88db212d4c0308ea1dce1b5106daee02aa636b2f8380c2251`) and `DailyV1CityEconomicOwnerCoreTests.cs` (`63a9e95ddcdfe9aa979d3bbf94d7d90c336bba5002dcc2e6ae904749b167da97`). The 26 raw-file hashes that differ when the same commit is freshly checked out in this review worktree are line-ending-only differences: all normalize to the same LF content, and `git hash-object --path` resolves to the exact candidate tree blob in both worktrees. All 26 paths are normally tracked (`H` in `git ls-files -v`; no assume-unchanged or skip-worktree flags). The paths are listed in the appendix.

### Unity EditMode and Smoke

The official Unity 6000.3.9f1 full EditMode run `7e695e7bc8854d7b97698382674f6ffc` reports 3282/3282 passed, with zero failures, skips, or inconclusive cases. The official filtered Smoke run `ea99a5752092456c836e145fac42b924` reports 5/5 passed, also with zero failures, skips, or inconclusive cases. Both manifests report stable 1,974-file inputs and the same digest, `8ca6da0b293e035e9ddc91a9c8b742a2e81c16a32e2e33006b2bb8a4b4410648`. Their original XML hashes match the protected manifests and the validation record:

- Full EditMode XML: `526db6f3db7e400c21317c950e1275a8298010414a106218aa494d2ab335a972`
- Smoke XML: `207e4a1cc94ad3acc81a3e85bc49f953baba8ab4e58cd3855d6f869de7e6a52e`

The committed result-only EditMode package was verified by the official verifier against the protected original full EditMode run manifest. It contains one report with 3282 test cases, all passed; the invalid zero-case category-selector probe is not included or counted.

- Normalized package manifest SHA-256: `9796de80eccba0bd167871e667de3fbc0c463534d3c27a2d0bf79201da2c69c7`
- Result-only archive SHA-256: `7aa679afb4ca191440b26898ddd2f9ff8e4561c3081b9e40eb8310a5cad40db2` (173,783 bytes)

`git diff --check` passes for the candidate diff. Validation was independently inspected from retained manifests, source inventories, original result files, and the committed package; unchanged tests were not rerun.

## Findings and limitations

No actionable implementation, test-integrity, or evidence-applicability findings remain for this bounded candidate. The result establishes only this selected City census rebind. It does not move the complete 89-family census, runtime-wide mutation-epoch capture, host-thread protocol, or completed-boundary publication into Core; nor does it complete `DailyV1Application.TryStart`, detached restoration, the independent .NET host, or CORE-F. No canonical promotion is approved.

## Appendix — line-ending-only raw hash differences

Every path below has `git ls-files -v` flag `H` in both the candidate and fresh review worktrees; clean-filter blob IDs match the exact candidate tree.

- `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs`
- `Packages/com.simulation.core/Runtime/Application/DailyV1ApplicationSessionGate.cs`
- `Packages/com.simulation.core/Runtime/Application/DailyV1DecisionOccurrenceReceiptAuthority.cs`
- `Packages/com.simulation.core/Runtime/Application/DailyV1DomainEventAuthority.cs`
- `Packages/com.simulation.core/Runtime/Application/DailyV1ExecutionBoundaryState.cs`
- `Packages/com.simulation.core/Runtime/Application/DailyV1GenesisComposer.cs`
- `Packages/com.simulation.core/Runtime/Application/DomainEventAuthority.cs`
- `Packages/com.simulation.core/Runtime/Configuration.meta`
- `Packages/com.simulation.core/Runtime/Conflict/PersistentBattleContracts.cs`
- `Packages/com.simulation.core/Runtime/Conflict/PersistentBattleStore.cs`
- `Packages/com.simulation.core/Runtime/Conflict/PersistentConflictWarContracts.cs`
- `Packages/com.simulation.core/Runtime/Conflict/PersistentConflictWarStores.cs`
- `Packages/com.simulation.core/Runtime/DomainEvents.cs`
- `Packages/com.simulation.core/Runtime/Identity/DailyV1RuntimeIdentityIndex.cs`
- `Packages/com.simulation.core/Runtime/Persistence/DailyV1P12DSelectedCoreOwnerStager.cs`
- `Packages/com.simulation.core/Runtime/Persistence/DailyV1P12ECoreOwnerRestoreStager.cs`
- `Packages/com.simulation.core/Runtime/Persistence/DailyV1P12ECoreOwnerSourceCapture.cs`
- `Packages/com.simulation.core/Runtime/Persistence/P12CSelectedCoreRootStager.cs`
- `Packages/com.simulation.core/Runtime/Persistence/PersistentConflictCensusProvider.cs`
- `Packages/com.simulation.core/Runtime/Persistence/PersistentConflictOwnerSnapshot.cs`
- `Packages/com.simulation.core/Runtime/Persistence/PersistentWarCensusProvider.cs`
- `Packages/com.simulation.core/Runtime/Persistence/PersistentWarOwnerSnapshot.cs`
- `Packages/com.simulation.core/Runtime/Person/DailyV1NpcOwnerCensusProviders.cs`
- `tests/Simulation.Core.Tests/DailyV1P12DSelectedCoreOwnerStagerTests.cs`
- `tests/Simulation.Core.Tests/DomainEventAuthorityCoreTests.cs`
- `tests/Simulation.Core.Tests/P12ECoreOwnerRestoreStagerTests.cs`