# CORE-F P12-F host census boundary validation

## Candidate identity

- Candidate branch: codex/core/CORE-F-try-start-transaction
- Exact code candidate: 72e791a07509fb572422ac5026a49227493e78a6
- Candidate tree: 3d6364325a38cd1041b5aea2034760787049fe64
- Base: dd1d10d86fcf08e47221b865051b9343cffdee9c
- Core canonical at validation: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Remote candidate was verified at the exact code candidate.
- Changed implementation paths:
  - Assets/_Project/Scripts/SimulationRuntime.cs
  - Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs
  - Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs
  - tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs

This bounded slice moves five P12-F census rows—ActorChoice, ScheduledDirective, TravelParty, Expedition, and PoliticalKnowledge—behind the Core session port. The Unity host port omits them only when exact Core graph/source identities are bound. Core recomposes the five rows from the live candidate graph before the existing complete 89-family vector checks, including advance capture and restoration target capture. Identity, schema, role, cardinality, revision, duplicate/substitution rejection, and restore revalidation remain fail-closed.

## Validation

- CoreValidation cold: 1004/1004 passed.
- CoreValidation warm: 1004/1004 passed.
- Core source commit/tree: 72e791a07509fb572422ac5026a49227493e78a6 / 3d6364325a38cd1041b5aea2034760787049fe64.
- Core input digest: 3e1aa747c1124ca3edf00b479af64d306c9c5bfe40913fccb0ca06a626d75888.
- Protected original CoreValidation manifest SHA-256: 544f391751cc9b0a4bd0d4d37e20d08398ef83850918619d367fd80757373d15.
- Cold TRX SHA-256: 3a7df9db268dc1b54199cc1158bb191199ec358a87821c0a4a618015b63c3ded.
- Warm TRX SHA-256: 3fe5b64e7f8051eb483668914cf862694cceb0daef3d5fd50b7e54fe0b12ef97.
- Full Unity EditMode: 3299/3299 passed, zero failed or skipped, stable exact-source inputs.
- Unity run-manifest SHA-256: 84c64bc264b2d2a2187f329b6b1642ddb88397af7d78476f148a7baae404b2eb.
- Unity input digest: 95bfbad1b02117c2ebcf5f73081703ea751c4d30d1bd2cfb5ed87ace95250965.
- Full EditMode XML SHA-256: 99c91dea738024805c759ca6682df45c3c8d09c4dc370186276ba540505def65.
- Separately executed Smoke: 5/5 passed with the same input digest. Those cases are also included in the full EditMode run.
- Smoke run-manifest SHA-256: d3f0047f7e5a55b135c62ff313a701ca24ff6103db0b04dd7d64bb82f67ea35f.
- The filtered Smoke XML is not packaged: its parent-suite aggregate counts are stale, and the canonical verifier rejects it. No XML was edited or accepted as normalized evidence.
- Full EditMode normalized package: one report entry; archive SHA-256 381eefa0146039383a1d7bc64a082c9d6a81737e989dd13bfe3e4efb0cb10e69; package-manifest SHA-256 cbf0a090b415d92e8b712f60c1f3e94248a811d3cd5f9517246890a79ff98f63.
- Canonical packager verification passed against the exact full-run manifest. The packaged archive and manifest scan found no local workstation paths.
- git diff --check and git show --check passed for the code candidate.

### .NET evidence packaging status

The original CoreValidation run passed and its manifest binds the exact candidate, stable input digest, and cold/warm reports. Its private originals remain preserved. A normalized DotNetTrx package is not included in this checkpoint.

The canonical adapter requires a matching private dotnet-trx-source-context-v1 record; the CoreValidation runner manifest is not that context and was rejected as such. No context matching this runner manifest was present. The exact validation worktree now contains two protected ProjectSettings edits produced by Unity after the CoreValidation run, while the captured runner snapshot recorded a clean worktree. The adapter requires the present worktree status to match that captured snapshot. Those edits and the private original reports were left untouched; no alternate packager or fabricated context was used.

Accordingly, the .NET results are recorded as successful exact-source validation, but compliant package publication and remote independent reproducibility remain outstanding. This evidence record does not claim the .NET packaging gate is complete.

## Independent exact-tip review

The independent reviewer returned VALIDATED_CANDIDATE for exact code candidate 72e791a07509fb572422ac5026a49227493e78a6 and tree 3d6364325a38cd1041b5aea2034760787049fe64. No actionable implementation or test-integrity findings remain. The review checked the five-row Core authority boundary, complete 89-family vector, exact identity/revision semantics, capture/restoration recapture, fail-closed tests, and current exact-source validation manifests. The unstable first-import Unity run was excluded; the stable full EditMode run above is the applicable evidence.

This is a candidate checkpoint only. It does not promote Core canonical or complete CORE-F. The .NET evidence-packaging limitation above remains open.