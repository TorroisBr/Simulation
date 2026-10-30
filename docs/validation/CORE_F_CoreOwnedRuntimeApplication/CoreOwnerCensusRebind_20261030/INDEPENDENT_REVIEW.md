# Independent Review — Core Owner Census Rebind

**Candidate status: NEEDS_CHANGES** — the implementation review passed, but the required Unity validation gate has not run. This is not a source-code rejection and is not canonical promotion.

## Exact identities

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Candidate commit: `7d78d673c9bc88b87f8b28f5ec657efde28367c1`
- Review base: `b3cc6a7f168b2eaeaa19e57e9398d3040b5bc5ff`
- Current Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Independent reviewer: `core_census_capture_review`
- Review record publication: Master Orchestrator transcribed the independent reviewer’s report because the reviewer’s local process could not create its own review commit. The reviewer did not modify the candidate.

## Scope reviewed

The candidate adds Core capture rebinding for:

1. Per-NPC SpatialKnowledge location and route sections from each exact `DailyV1NpcRuntimeOwners.SpatialKnowledge` instance.
2. Per-Person life/residence sections from the exact bound Core `PersonStore` members.
3. Additive regressions for exact selected owners, aliases, revisions, stable identities, mixed-roster preservation, stale-token behavior, and missing/duplicate rows.

The exact diff from the stated base changes only:

- `Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs`
- `Packages/com.simulation.core/Runtime/Person/DailyV1NpcOwnerCensusProviders.cs`
- `tests/Simulation.Core.Tests/DailyV1CityEconomicOwnerCoreTests.cs`

## Independent implementation verdict

**PASS for implementation correctness.** The reviewer found no production correctness defects.

- SpatialKnowledge IDs, schema, required role, cardinality, revision, distinct owner binding, and ordinal provider ordering match the canonical section contract.
- Each locations/routes pair uses the same owner identity and alias group.
- Selected rows are rebound only when role, schema, owner reference, cardinality, and revision match. Mismatches remain intact for the established P12 stale-token path.
- Mixed NPC rosters require consistent Inventory/MoneyAccount cardinality, require each selected Core pair, and preserve the complete host vector. The unchanged P12 contract still validates the complete census, including all 22 NPC families and the 89-family profile inventory.
- Person life/residence sections use the canonical length-prefixed Person identity, schema 1, exact `PersonRuntime` owner, singleton cardinality, nonnegative local revision, and ordinal ordering. Missing or duplicate selected rows fail closed; stale facts and mixed-roster vectors remain preserved.
- The added tests are additive. No existing assertions or the 89-family census contract were weakened. The reviewer identified one test-message overstatement; it was corrected in `7d78d673`, and the corrected wording says only that the four supplied rows retain their order.

## Validation evidence

The official CoreValidation runner passed against the exact source/test input digest that is now committed in the candidate:

- Run: `863f563227124c3cb9c39b762b8cd600`
- Cold: **952/952 passed**; warm: **952/952 passed**; zero failed, not executed, aborted, or inconclusive cases.
- Discovered test identities: 952.
- Input files: 536; stable input digest: `7d358b5f4b94ef035ad245ef77ea9699e0efed1d11eb15381323dc472eca122c`.
- Manifest SHA-256: `1114cb1a50c2aec8d027d457b70f53c1afa56d1b661eb21f34f776fc03668644`.
- Cold TRX SHA-256: `9de09c85602b5094083a6124b6e761ebecab09313609f53032a251804fe01a6b`.
- Warm TRX SHA-256: `21e0f0784022ed2a6e9dfa8ab7b91025412b64946aa76395bff76faedde85db0`.
- Exact input SHA-256: `DailyV1RuntimeAdvanceStage.cs` `5f61b8b3421792a7fda1fc916c125cccfd0561a302287e86e689f66c7adb86e4`; `DailyV1NpcOwnerCensusProviders.cs` `c7ac2cffb06e479e49b494137c7ce45011755faf9df38f1abeca2c2e778efa86`; `DailyV1CityEconomicOwnerCoreTests.cs` `0d4378777523ddea5f9a616e2af30a03d1f412f77c322eb4821b88f5c65dd25a`.
- `git diff --check` from base to candidate: **PASS**.

The manifest and original TRX reports remain in the protected E: evidence directory and were not copied into this review branch. The source digest was independently recomputed by the Master Orchestrator and matched the manifest. The review agent received the exact hashes and result accounting but could not open its local worktree/process or publish the record itself.

## Remaining validation gate and integration constraints

The official Unity evidence helper failed while establishing the required private evidence-directory ACL on E:, before the Unity Editor launched. It returned an unauthorized-operation error from `SetAccessControl`; no Unity EditMode XML was produced. Therefore this record claims **no Unity EditMode or Smoke result**. Do not promote this candidate until the required Unity suites run with the approved evidence workflow and pass.

This checkpoint does not complete CORE-F, P12 capture/restoration, the DailyV1Application host, or the detached .NET continuation contract. Core canonical remains unchanged.