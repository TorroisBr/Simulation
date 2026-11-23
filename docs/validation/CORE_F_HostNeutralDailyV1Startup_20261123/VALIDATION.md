# CORE-F host-neutral Daily-v1 startup composition

## Source identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Validated base commit: `d3d0eea2a325852f389732dc97969e10174f54c4`
- Committed base tree: `fd45cb6fc1bdaadbda096458762ba08b022a1952`
- CoreValidation working-input digest: `a939628ed9bb62fbde0ac2067a311da4b534e8ee57887d576aa2d756aad0933b`
- CoreValidation and both Unity captures reported stable inputs. CoreValidation's source inventory hashes for the three changed files matched the working files:
  - `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` — `e815792e878c08934f6a16f96f860f0f2062b788a925970f5825d94c86d019bb`
  - `Packages/com.simulation.core/Runtime/Application/DailyV1CoreSelectedOwnerCensusProviderComposition.cs` — `4908ffad60f7bc49e7e2461e615f731eec46fac6c3b0099c98b45c7350c2749f`
  - `tests/Simulation.Core.Tests/DailyV1ApplicationHostTests.cs` — `e3ccca87de69e68d19da966f54669bb09c48b97b87a0b717c7d3a72afc3e7e06`

## Executed validation

- Focused Core tests: 2/2 passed for the production census-start rejection and the explicitly test-only Genesis commit gate.
- Official CoreValidation: cold 1,194/1,194 and warm 1,194/1,194 passed; builds and CalendarDemo gates passed. The Lab gate was skipped using the runner's documented `-SkipLab` option because this checkout has no `tools/Simulation.Lab/Simulation.Lab.csproj`.
- CoreValidation run: `2f9df7ebb2504a27a8fe1d5a7684bf99`; cold TRX SHA-256 `5780b7daf0db472390792c4d2a4cc08026ba382c0817ec85994f13abe24bf269`; warm TRX SHA-256 `910cab33fa331b664e863c878b4676c6c9585182594dee7ac8257478c218fcfe`. Original reports and private run manifest remain in protected local evidence storage.
- Official Unity EditMode: 3,325/3,325 passed, run `412049d2c6a44d159503aa0a65097ee8`, Unity `6000.3.9f1`.
- Official dedicated Smoke filter: 5/5 passed, run `1d71a82c1fa9477193b92692531ce0e5`. The five Smoke identities also passed within the full EditMode run.
- The privacy-safe Unity package for the full EditMode report was verified with the official verifier. Archive SHA-256: `21013ae7687a78dcc97cbcc6947a2bd2482a2fb7804c8129d324896f73874c43`.
- The dedicated Smoke XML was not changed or normalized. The packager rejected that filtered report because its parent `Simulation` suite's `testcasecount` does not match the report descendants. The private original remains preserved; the full EditMode package includes the same five Smoke test cases.
- `git diff --check`: passed.

## .NET TRX publication boundary

The preserved CoreValidation records use `simulation-core-validation/v1`. The approved DotNetTrx packager requires a `dotnet-trx-source-context-v1` source-context record, and this run did not produce one. No context was fabricated and no original TRX was modified. The raw CoreValidation reports and manifest remain private; this validation record reports their identities and hashes only.

## Capability boundary demonstrated

The application now attempts Core-owned provider composition before it creates detached .NET runtime-boundary ports, both for Genesis and for a restored session. It derives the expected census section IDs from the admitted owner graph and the fixed P12 contract, compares the complete set before registering anything, validates live witnesses and alias identity, and delegates sealing to the existing execution-boundary APIs.

The current Core provider set still does not cover every required section of the 89-family selected Daily-v1 census. Production `.NET TryStart` therefore fails closed with the exact missing section IDs and does not publish an active session or seal a partial census. No successful host-neutral startup, full-profile readiness, autonomous daily advancement, complete P12 restore, or separate-process continuation is claimed by this checkpoint. The renamed fixture test exercises only the internal commit gate with test-only census readiness; it is not evidence of production startup.

## Evidence locator

- `Unity/coref-host-neutral-startup-editmode-smoke-20261123.manifest.json`
- `Unity/coref-host-neutral-startup-editmode-smoke-20261123.zip`

The private Unity manifests, original XML/logs, and private CoreValidation artifacts remain in their protected E: evidence locations and were not copied into this public record.
