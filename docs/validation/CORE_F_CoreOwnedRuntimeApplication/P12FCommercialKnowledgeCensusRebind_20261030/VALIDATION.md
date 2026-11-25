# P12-F CommercialKnowledge census rebind validation

## Scope

This candidate rebinds the three selected NPC CommercialKnowledge census
sections—markets, liquidity, and share receipts—to the exact
`DailyV1CommercialKnowledgeOwner` in the admitted Core owner graph. The Unity
projection remains the P12 mutation-boundary adapter and is checked against
that same Core owner, revision, and section cardinalities. P12-D capture checks
the Core owner identity while reading values through the forwarding projection.

The complete host census remains intact for mixed rosters. Missing, duplicate,
or uneven section families fail closed; stale section facts are preserved for
the existing P12 classifier. This is a partial census rebind only and does not
claim complete P12-G owner coverage or CORE-F completion.

## .NET CoreValidation

- Official run `a5c3ee9476574882a1bbd9286a0e9c95`: cold **957/957 PASS** and warm
  **957/957 PASS**.
- The runner recorded stable inputs with digest
  `e61ddd6706a3360fad9b356917da6e1e0ff1625085ed68d70d61d9b5d0b478bf` across
  536 source and test inputs. Those same 536 files were re-hashed in the current
  candidate worktree: 536 matched, zero were missing, and zero differed.
- Native CoreValidation manifest SHA-256:
  `44ff242b1a2c570e90b91138abbdcc8450d94c7ae3a2934ecc405231f2419234`.
- Original cold TRX SHA-256:
  `7b08eebf2689acb0e8cf8a91ff631cdfb7ea29272e55012cadee94ce657e3e07`.
- Original warm TRX SHA-256:
  `f80fb0ac6b5b841ef6dd7ce96660e3273bb7217dc4da3debcf1c8aa958fef352`.
- The 536 current input files matched the run exactly; no Core source or test
  input changed after this run.
- The official runner emitted `simulation-core-validation/v1`, not the separate
  `dotnet-trx-source-context-v1` required by the approved public TRX packager.
  Direct submission of the native manifest was rejected by the packager. No
  context was fabricated and no public .NET TRX package or normalized
  projection is claimed. The original reports and manifest remain unchanged
  in protected E: local evidence retention.

## Unity validation

- Official full EditMode run `d5777e4f4bb648958cbaf460fdeb8248`:
  **3283/3283 PASS**, zero failed, skipped, or inconclusive. The runner marked
  the source stable before and after execution; source digest
  `3895ae40e449bb09d6c0682c5b576fe8ee0e735601d488fc93a94076130ee9c9`.
- Full EditMode original XML SHA-256:
  `60604d7ee170d546f00f19287680033ceb1f59e9ed4054e56d5636fa88547390`.
- Full EditMode original log SHA-256:
  `a948227516bcea06fb4585475b08fa42b4543b93ec67cecd2822bc735266bff6`.
- Full EditMode private run-manifest SHA-256:
  `6a00a8cbf8f095739290df4c570e5a498a7dfcf94b459fec083d1b7bbfdcf003`.
- Official standalone Smoke run `029c29c2340a4286992b3f3135e5ac9d`:
  **5/5 PASS**, zero failed or skipped, with the same source digest.
- Standalone Smoke original XML SHA-256:
  `0a34e2eeeb6335edf51b2472b7709011cf6957d2b5b8c18a7544e0d1cc929507`.
- Standalone Smoke original log SHA-256:
  `eec81dbce90e1dd8c803363c65267ea443e34f6e7ee29770cdb12f47f2f41e44`.
- Standalone Smoke private run-manifest SHA-256:
  `1a51a977eb6cded90eacad47fd84443f99cc66ae09f358938d5d917b478c3098`.

The complete EditMode result, including the five Smoke tests, is in the
privacy-projected public package below. The separate filtered Smoke run is
retained privately. Its filtered NUnit XML keeps the full-suite `testcasecount`
on parent suites; the package validator correctly rejects that hierarchy as
inconsistent with the five filtered leaves, so no filtered public package is
claimed.

## Public Unity evidence package

- Package manifest:
  `UnityEvidence/coref-p12f-commercial-knowledge-census-editmode-20261030.manifest.json`
- Manifest SHA-256:
  `1c93d12b759a3bdcb5701124a06ca7b6c63f5dd2508fd33bc150302ff20e1115`.
- Archive:
  `UnityEvidence/coref-p12f-commercial-knowledge-census-editmode-20261030.zip`
- Archive SHA-256:
  `7a8010ca35789f0dde0772124dd63ebc4179a901a32154fb12ea13196d8cdd73`.
- The official verifier passed against the exact private full EditMode run
  manifest: `VALID: runs=1 entries=1`.
- The public manifest contains no private workstation path. Original XML,
  logs, and private run manifests remain in protected E: retention.

## Reviewed source identities

| File | SHA-256 |
|---|---|
| `Assets/_Project/Scripts/NpcKnowledgeCensusProviders.cs` | `2ee62be2727172bf4ec04c7a335c7d2ca9582a57f0c9ed6318477517c2ebc92f` |
| `Assets/_Project/Scripts/P12DNpcRootOwnerSnapshot.cs` | `9fd963723acba6e43ee5874cd01307ab4da92e798d53e1f35c8e3b6255b21a4c` |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `62e040bf8f82eaac98d9a47e20ca7df36cb389a928ff193e06e829261f59aed4` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs` | `fb1171cc472ec38c69c727a97ee8d258b4dcc435a26f305d6b864f6560c1f389` |
| `Packages/com.simulation.core/Runtime/Person/DailyV1NpcOwnerCensusProviders.cs` | `ffb1328a71f33286efffc2bd510843cdf4baa03af7028c2832cddd8840cc0cd7` |
| `Assets/_Project/Tests/EditMode/Editor/NpcKnowledgeCensusTests.cs` | `9cbe37103bbb1d779852c7c12a1ff7a5c9b1c1243e1992a00841ff9cc1df502e` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs` | `028612b7bd41932a4a1a9abf1077067aba78cedd7b35118a283a81639efb6d0a` |
| `tests/Simulation.Core.Tests/DailyV1CityEconomicOwnerCoreTests.cs` | `5c0fa14a5a0e8cd588f9873030ebc6fc173f40a7714163f9d0fd8faa227088f1` |

`git diff --check` is required at the candidate checkpoint. This record does
not claim canonical promotion, full P12-G readiness, or CORE-F acceptance.
