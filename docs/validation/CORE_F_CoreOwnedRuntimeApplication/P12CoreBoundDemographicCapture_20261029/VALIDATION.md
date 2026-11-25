# P12 Core-bound demographic capture validation

This is a bounded candidate checkpoint record. It validates app-backed P12 capture of the exact Core-owned PersonStore and GenealogyStore snapshots under one active continuation-save operation. It does not promote Core canonical or claim CORE-F completion.

## Source identity

- Candidate branch: codex/core/CORE-F-core-owned-runtime-application
- Base HEAD: 399470abd67462251baf5091f5b227ec02229be3
- Base committed tree: 6aa0e8af441f37e37a1a22f0cd99b2b5d5f181fc
- CoreValidation working-input digest: b2795012c2d0cfbc18de2c158876952f8fa468608d60a627be14a1b96926f3a1
- Core and Unity source/input snapshots were stable during their final runs.
- git diff --check: PASS.

The six validated implementation/test inputs and SHA-256 identities are:

| File | SHA-256 |
|---|---|
| Assets/_Project/Scripts/P12DDailyV1OwnerPackage.cs | ee85786fef9db081624d19fb0080240dd8a7d386f2a1010fb6d8d48ef6063e17 |
| Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs | 2584d40cd9ace5187a7706422dd500e42bd97085dc0278534d560fcd0b14f034 |
| Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs | 27ea9c3b0cafaf7cc874aedbfbdf5cd7197f6a893fa87b8091df37e496e5f30c |
| Packages/com.simulation.core/Runtime/Persistence/DailyV1P12GContinuationSourceWitness.cs | d981dec4129751e9efcf684f9f9f6ee883ca3a18f794d4f20ee3f5ad2286247e |
| Packages/com.simulation.core/Runtime/Persistence/DailyV1P12GCoreBoundOwnerSources.cs | f61fe9ab6223a771a5dea8c3024512008d873c9fe7b03bcab9f48c35bd2eb6e4 |
| tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs | 1a3cc5677f95fec715ce8e8261bae0bab691b3667a1b88b2a9cc3d80460b94c8 |

## CoreValidation

- Protected run ID: 828eab5b5c8d499792064d1398a7a236
- Cold: 934/934 passed. Warm: 934/934 passed.
- Core input digest: b2795012c2d0cfbc18de2c158876952f8fa468608d60a627be14a1b96926f3a1; generated dependencies stable.
- Private manifest SHA-256: 40b13fac7df53b144f12bc1e9aac031a6593e72bafdc8db6cb86a8d92ed4ba9e.
- The runner reported that this checkout contains Unity Library; no .NET/Unity-Library independence claim is made.

Original reports:

| Report | Bytes | SHA-256 |
|---|---:|---|
| CoreTests-cold.trx | 1,342,346 | 9aa56faef78c759c529d7558996019e62657267850f2811a357ec99bd014301e |
| CoreTests-warm.trx | 1,342,346 | f75a13b13bfe7f6400f523b7b7e751c9c58b492465017194c887998478583478 |

Cold/warm test log SHA-256 values: abfab913a420521fb6bb6e0f5418d9b100e2939b68bb5c0324e1faa54641b9d6 and bfa2eaa6ac53d72a1ce3c1623abe193627ac0164f87ba188f4c5f3b9f7b8f00c.

### Required failure classifications

- ContinuationSaveRejectsMismatchedDemographicOwnerIdentityBeforeOperationPublication: after minting a valid token and changing the demographic owner identity, capture admission returns StaleToken; no save operation is published.
- SaveAdmissionRejectsCoreCityDailyEconomyReceipt: the unsupported City receipt vector returns OwnerCoverageIncomplete. This existing outcome remains distinct from the post-token stale-owner outcome.

Both outcomes passed in the 934/934 cold and warm runs and preserve their separate contract meanings.

## Unity

Unity Editor: 6000.3.9f1.

- Focused restore regression: DailyV1RestoreHydratesNonemptyPersonAndGenealogyGraphAndContinuesDeterministically, 1/1 passed in run 69f4235c4a314f939c3f583041f0458c. The final full EditMode run below includes the same regression after only a trailing-newline cleanup in the new Core source.
- Full EditMode run: 26d2f038cd9d47528ed1a22026d3d9e3, 3,279/3,279 passed; 0 failed/skipped. XML SHA-256 7e8181f3eedb7011282fe6df35a0330f031181a5f26749fcfced7c06ee0e68d1; raw log SHA-256 a86741b1056c52bad291a025114d7304d11aac3769afec30861c3465ead32e6c.
- Official EditMode Smoke filter: run fd6cd7f9982647d78230f1d1d0ee576c, 5/5 passed; 0 failed/skipped. XML SHA-256 202cf713e203bb3be00affd30ece6c1b3c9df822cccabbe0c9eea62543adb4a8; raw log SHA-256 4ab3d1196f94fdc077200fe8f2a85596e4c05705a1d7c4241356db9da256f1d1.
- Both final Unity runs reported stable inputs. The full EditMode run is authoritative for the complete result set and includes the five Smoke identities.
- A filtered PlayMode invocation produced zero test cases and is excluded from all gate claims.

The full EditMode evidence package is included here:

- p12-corebound-demographic-capture-editmode.zip SHA-256: 38c74c16a9ec96a0b47f0c1f617f722a2cefe40fbbcd8e91661b0b08f5a1325b
- p12-corebound-demographic-capture-editmode.manifest.json SHA-256: e7f0c247973f4bc162f2bd18f8e21fa6a47f6876d5e6e6cb658b9fceabb70959
- Archive verification against the package manifest and protected original run manifest: PASS.

Raw reports, runner manifests and logs remain in the protected evidence store; they are not included in this repository folder. The filtered Smoke report is recorded above, while the complete EditMode archive contains its five test identities. No original evidence was changed.

## Scope and acceptance boundary

The application-backed save operation now binds exact Core PersonStore and GenealogyStore references from the selected runtime owner graph and captures immutable snapshots while the continuation-save scope and completed-boundary witness remain active. Unity consumes these snapshots only after verifying the exact projection references. The no-application legacy path remains unchanged. This checkpoint does not claim P12 or CORE-F completion, detached host readiness, or canonical promotion.