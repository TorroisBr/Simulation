# CORE-F host-neutral Daily-v1 startup — PersonStore census correction

## Source identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Validated base commit: `388a85887245f9b2616ca1adbc954bce639cafd1`
- Committed base tree: `895a5981821649bb9ac5e4ab4a120f863296e508`
- CoreValidation working-input digest: `cb8401389dcf999b9424afd01f2f59262a01d2ffe5017e275f2c5c4bc603eb00`
- CoreValidation and both Unity captures reported stable inputs. CoreValidation source-inventory hashes matched the current files:
  - `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` — `e815792e878c08934f6a16f96f860f0f2062b788a925970f5825d94c86d019bb`
  - `Packages/com.simulation.core/Runtime/Application/DailyV1CoreSelectedOwnerCensusProviderComposition.cs` — `165f5287ca221512ed1bf51509475eb7c4c2331fd5c7e84cf482421153bfcfd1`
  - `tests/Simulation.Core.Tests/DailyV1ApplicationHostTests.cs` — `2cd6e30b0561770bab0031d5fab4def841f5f0f6caea0f45fe35455f448fa4e5`

## Executed validation

- Focused standalone-Person census regression: 1/1 passed.
- Official CoreValidation: cold 1,195/1,195 and warm 1,195/1,195 passed; build and CalendarDemo gates passed. The documented `-SkipLab` option was used because this checkout has no `tools/Simulation.Lab/Simulation.Lab.csproj`.
- CoreValidation run: `4f684d852e6e4ed6a74f844c52787195`; cold TRX SHA-256 `d7666bfc10241d33c6912aa5322b690242b661600b230fc4a0246d0c086a4555`; warm TRX SHA-256 `8497aef9ff0c804023ac3dc7b61e845155b6aa4b4a3dc0a44945b7b5cb409cf6`. Original reports and private manifest remain in protected local evidence storage.
- Official Unity EditMode: 3,325/3,325 passed; run `a8254c6d77e74aa6aadb6a47fbd4ef55`; Unity `6000.3.9f1`.
- Official dedicated Smoke filter: 5/5 passed; run `a1a44ba9825241aea99da81b2afbcf29`. The same five Smoke identities also passed in the full EditMode run.
- The privacy-safe package for the full EditMode report passed the official verifier. Archive SHA-256: `23d43589e11e23f3498410218fb26d8b0c57348fd64c4e0e440c8674320b9568`.
- The dedicated filtered Smoke original remains private and unchanged. It is not included as a separate public package; the prior identical filtered-suite package attempt was rejected by the official verifier because the parent `Simulation` suite testcase count disagrees with its descendants. The complete EditMode XML and public package include the same five Smoke test cases.
- `git diff --check`: passed.

## .NET TRX publication boundary

The preserved CoreValidation records use `simulation-core-validation/v1`. The approved DotNetTrx packager requires a `dotnet-trx-source-context-v1` source-context record, which this run did not produce. No context was fabricated and no original TRX was modified. This record publishes result identities and hashes only; the raw CoreValidation reports and manifest remain private.

## Capability boundary demonstrated

The startup inventory now derives one `PersonLifeResidence` section for every exact `PersonStore.Persons` member, including Persons without an NPC materialization. NPC-to-Person bindings must resolve back to the same PersonStore instance. Unmaterialized NPCs continue to require their own `NpcResidence` section. The regression verifies this expected Person row while confirming that Core composition does not fabricate a provider for it.

The production startup still fails closed when Core providers do not cover the complete selected 89-family census; it registers no partial census and publishes no active session. In particular, Core still lacks a provider for an unmaterialized Person's life/residence row. No successful host-neutral startup, full-profile readiness, autonomous daily advancement, complete P12 restore, or separate-process continuation is claimed.

## Evidence locator

- `Unity/coref-host-neutral-startup-reviewfix-editmode-20261123.manifest.json`
- `Unity/coref-host-neutral-startup-reviewfix-editmode-20261123.zip`

## Exact-owner composition and Unity projection follow-up — 2026-11-23

This follow-up supersedes the earlier missing-owner list above for the current
working tree. It preserves that earlier record as historical evidence.

### Production delta

- Core Genesis now creates and binds one empty `PoliticalKnowledgeStore` to the
  exact Core owner graph. The Unity startup adapter passes that same instance
  into `SimulationRuntime` in both Core-prepared construction routes. The runtime
  retains its existing reference-identity rejection; no parallel store is made.
- Core application-start provider composition now includes the exact existing
  P12-C roots, P12-E owners, Genealogy owner, JusticeRecords owner, six P12-F
  owners, the exact-empty ExplorableSite and decision-occurrence-receipt owners,
  and the supported Core City/NPC/identity/spatial owner witnesses.
- `DetachedCoreStartFailsClosedUntilTheProductionOwnerCensusIsSealed` verifies
  those owner identities, live cardinality/revision, exact-empty baselines, and
  the exact sections still missing. A partial census is not registered or
  sealed and the unpublished session remains unpublished.

### Exact-source validation

- Official CoreValidation run `1f090182ce504e628f8dabb9d1a21dcb`: cold and warm
  1,195/1,195 PASS; build and CalendarDemo gates PASS. Input digest:
  `4c92cd011a20e19d03babf2d7c5e63dc8dca0807c3fbe8c1a4e8702686a27d72`.
  Original cold/warm TRX SHA-256 values are `8d4d950a7818f6e1bf72e44a2be37122488f864870f715da8c00b8843b248a96`
  and `82bcb3b0ef58b29f2d3e8016ce031b25130ce810613e7a4a91661d1c2ce62a16`;
  the protected native manifest SHA-256 is
  `af8df58974cae4c8361a5317b14d11f89fdd30c96bcea7c72f67d488348865ca`.
- The retained CoreValidation report remains in its original private
  `simulation-core-validation/v1` form. No normalized public .NET package is
  claimed. The approved adapter needs a separate protected
  `dotnet-trx-source-context-v1` mapping and verified report/assembly/runtime
  provenance; none has been created for this run. Original reports and manifest
  were not changed. Packaging may be recoverable under the approved §7 procedure
  without rerunning, but remains an open evidence task until that complete
  context is independently verified.
- Focused Unity restore regression: `c839342c62bc472cac4512dc4d469a8a`,
  1/1 PASS. The prior downstream cross-thread writer test:
  `68295389255c43b38a15b5cdf5833234`, 1/1 PASS.
- Official full EditMode run `a74599b1fe6a4dc5bf9ecc2b1551f879`: 3,325/3,325
  PASS, stable input digest
  `c763387131f6525fc39866e8b1f343647d842f69e7ec314dedfc9422af06d129`.
  Official EditMode Smoke filter `521a276aec6947d4aa175b7cc03c53d7`: 5/5
  PASS, same input digest. The five Smoke tests are also part of the complete
  EditMode report; the separate filtered report remains private because the
  established packager may reject its parent-suite aggregate counts.
- The full EditMode privacy-safe package passed Package and Verify. Archive
  SHA-256: `23cfd4b7158921c82422985b39502bdc98e6482574e7682cd793743df65406f6`.
  Files are under `UnityAdapterProjectionFix/`. The original XML and raw logs
  remain in protected local evidence.
- A pre-fix full EditMode run `bbdd98fc265b451e9d870627a0a1a4f0` had 304
  failures. The retained stack identified the missing exact Core
  `PoliticalKnowledgeStore` projection; it is not represented as a successful
  run. The corrected focused and full runs above pass. A separate PlayMode
  invocation selected zero tests and is excluded from all Smoke claims.

### Current host-neutral readiness boundary

Core startup still fails closed against the complete 89-family profile. The
current exact diagnostic has 23 missing provider rows:

- `p12b.crime-p18-receipts`
- `p12b.justice-p18-receipts`
- `p12e.economy-keyed-sale-receipts`
- for each of `npc-000001` through `npc-000010`:
  - `p12d.npc-local-observation-receipts/<npc-runtime-id>`
  - `p12d.npc-merchant-trade-state-receipts/<npc-runtime-id>`

These rows remain backed by current Unity P18/NPC/economy host receipt owners;
this follow-up does not synthesize substitutes or claim they are Core-owned.
The exact 89-family readiness rule is unchanged. This evidence does not claim a
published Core session, full-profile readiness, autonomous Core day advancement,
full P12 restoration, or cross-process continuation.

Private Unity manifests and original XML/logs remain in their existing E: evidence locations. The CoreValidation originals remain at their existing E: location, whose ACL status is recorded below. No private artifacts were copied into this public record.

### CoreValidation TRX recovery check — 2026-11-23

- The preserved CoreValidation runner reports a complete cold/warm pass and its
  original manifest, reports, logs, and runtime-selection traces still match the
  recorded byte counts and hashes. The pinned VSTest logger was independently
  verified as version 17.14.1 with the contract's expected 247,632-byte SHA-256
  identity.
- The approved source-inventory check also confirms that the current worktree's
  Core inputs and three generated dependency files match this run's recorded
  source commit/tree and input digest.
- The approved `Test-ProtectedEvidenceDirectory` check rejects the evidence root
  because its ACL still inherits permissions. The owner-only-looking access
  listing was not sufficient. No ACL was changed, and no `retention-receipt.json`
  or `dotnet-trx-source-context-v1` record exists for this run.
- Section 7 recovery can derive several provenance fields from the runner,
  reports, source inventory, settings, runtime traces and retained output. It
  cannot make the existing originals compliant while their containing evidence
  directories fail the required protected-ACL check. Relocating or rewriting the
  path-bound runner manifest would invalidate its original identity. No context
  or normalized .NET package was created, and the original reports and manifest
  remain unchanged.
- This run therefore remains private, unnormalized CoreValidation evidence; it
  is not presented as a compliant public evidence package. The evidence policy
  forbids rerunning unchanged CoreValidation solely to normalize reports. If a
  later relevant Core/test/tool input change requires validation, capture that
  run under a newly verified owner-only E: evidence root and retain its complete
  source context at execution time.
