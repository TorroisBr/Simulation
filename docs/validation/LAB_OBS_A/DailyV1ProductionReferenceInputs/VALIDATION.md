# Daily-v1 Production Reference Inputs — Validation

## Scope

This candidate adds the generated, internal Core-owned `DailyV1LabSelectedReferenceInputs` provider for the accepted fixed Daily-v1 proving profile and verifies its identity against the existing frozen Unity capture. It does not add an observation reader, `AdvanceAndObserve`, Lab CLI behavior, persistence, Atlas, P13, CORE-X, or new architecture authority. LAB-OBS-A remains `WAIT_DEPENDENCY` until its official Lab adapter and coherent observation boundary are delivered under their separate contract.

The production provider is compiled only by `Simulation.Core.csproj`. The frozen fixture source remains at its canonical Unity EditMode test path and is linked into the Core test assembly solely as the parity oracle. Production Core code does not reference or compile that fixture. The generator's default target remains the existing fixture; the `core-reference` target emits the internal Core provider.

## Deterministic generation and parity

- Captured source baseline: `CORE_F_WP1_VALUES_BASELINE` at `422805cbe...` (as pinned by the generator).
- Generator inventory: 164 schema rows, 718 captured occurrences, 38 selected assets.
- Generator checks passed for both the unchanged frozen fixture and the generated Core reference provider.
- Frozen fixture canonical UTF-8/LF content remains unchanged: SHA-256 `c7280804cb43a597992fd59667c646b8991e6d0d9ea22042c03f837e83becadc` (the current checkout uses CRLF; generator verification normalizes line endings before comparison).
- Frozen fixture input identity recorded by the official run: SHA-256 `91420f481f6fac3fd0fef0a0f6b28a81f8ad1f64afc179691ece32cb8ef6e09d`.
- Generated Core provider source: 168,119 bytes, SHA-256 `d1eff6ca188d505cc49522c17425f8119e28bd80e31d7869b4b97bd8fd533930`.

`ProductionProviderMatchesFrozenOracleBeforeAndAfterCoreAdmission` compares the complete typed definitions graph, ordering, references, authored projection, exact numeric representations, effective identity/fingerprint, P9 manifest/stage identity and runtime owner fingerprint before and after real Core admission. The owner fingerprint checks exact City/NPC/Market/Item definition identities, full 299-row census vector (the accepted 89-family shape), and each baseline census owner's reference identity, cardinality, and revision.

No P8 binding is added: the captured City continues to have a null `materialFlowLocationId`. The profile retains its captured authored/effective seed semantics without constructing or mutating Unity authoring objects.

## Real runtime stock probe

`CampoVerdeTrigoStockChangesThroughTwoRealCoreDaysAndRepeatsDeterministically` starts through the ordinary no-save Core application lifecycle and advances the same Core session for two days. The observed stock for `CAMPO_VERDE` / `TRIGO` was day 0 = 420, day 1 = 450, day 2 = 480, a signed two-day delta of +60. A second independently allocated WorldId produced identical three stock values and delta. Both cold and warm official test executions reproduced the same observations.

These values are observations from the current captured proving profile, not a forced market mutation or a universal stock promise. The test requires a nonzero delta from ordinary execution and does not add a trade or synthesize stock.

## Validation

- Focused tests: 2/2 passed.
- Official CoreValidation: PASS, cold 1,214/1,214 and warm 1,214/1,214; all failures/skips/errors/timeouts/aborts were zero.
- The official runner used `-SkipLab`, as this checkpoint supplies Core production inputs and does not implement the Lab adapter/read boundary.
- The official run's compile/source input snapshot was stable before and after execution. Worktree input digest: `4f43a2cbbcae4e644675f65a1d38f7a877595def8b170a9f633279cc0b726402`.
- .NET-only checkout had no Unity `Library` directory.
- No Unity validation was required for this Core-only source/test/tool change: no Unity production, assembly-definition, test, or package input changed.
- `git diff --check`: PASS.

Private original reports and logs are retained unchanged under protected evidence run ID `6003a873f18646cf8f21bf082194bd91`; the raw reports and private manifest are not copied into this repository. Evidence-manifest SHA-256: `f7ed0d9366682f8977d5888916dbaeef381c0f8119f01ef30c75606965f90dfb`.

| Artifact | Bytes | SHA-256 |
|---|---:|---|
| `CoreTests-cold.trx` | 1,753,398 | `71f893c29a29f632c9575fc8e964bb07127f089df8e63eeeb9ebfb2005e9c32e` |
| `CoreTests-warm.trx` | 1,753,394 | `6e1ef7d5807dde91c11e4e8a5b17dd4a2a8fd9de733fb359fcf10aaa065f9a8f` |

The runner's exact input hashes for the changed source/tool inputs are preserved in the private manifest and were reconciled to the current files before this record was written:

| Input | SHA-256 |
|---|---|
| `Tools/generate_daily_v1_captured_content_fixture.py` | `0d2b37d8cfda526dfe05ceaee4939ac28d91c8559d32ffb1fd453bf1ed64330b` |
| `src/Simulation.Core/Simulation.Core.csproj` | `2d641912989de418d3d5c367f2b0d9e94a16674f91bce7259c4e75338354c306` |
| `src/Simulation.Core/ReferenceContent/DailyV1LabSelectedReferenceInputs.cs` | `d1eff6ca188d505cc49522c17425f8119e28bd80e31d7869b4b97bd8fd533930` |
| `tests/Simulation.Core.Tests/Simulation.Core.Tests.csproj` | `eba4ee1eeefbcfde4fa350e8fa6eadf65b9aaed4211e4a03d6f2fa3d88d7520d` |
| `tests/Simulation.Core.Tests/DailyV1ProductionReferenceInputsTests.cs` | `26c6b63b79dcde97b72520ac4fc7a964472551b3c01839b68affd675e40368f6` |

## Boundary and remaining work

This checkpoint provides the production-owned fixed reference input provider and proves its captured-definition identity plus real two-day Core stock behavior. It does not establish an official Lab adapter, observation API, writer isolation, concurrent read boundary, or user-facing Lab command. Those remain LAB-OBS-A prerequisites; no Core canonical promotion or architecture canonical change is included here.

## Evidence packaging recapture and closure (2026-11-26)

The protected original run `6003a873f18646cf8f21bf082194bd91` and its original reports remain unchanged. The approved packager declined that run because its captured untracked-directory status path ended with a slash (`?? src/Simulation.Core/ReferenceContent/`), which the path normalizer treats as an empty component. No source or report was rewritten. To satisfy the source-context contract, the official CoreValidation runner was run once more on the clean, committed candidate source; the recapture is the authoritative packaged execution for this checkpoint.

The recapture is run `9300a513be1a4ebea53b5917881e2a2b`, with the exact validated Core source commit `b9b049d3f52c22ba40ff87896a12474a5d2c85c6` and tree `569cb5f0fea9455ee341bb87e5e49a0945f450b4`. Its stable input inventory contains 566 source inputs and 3 generated dependencies, digest `0075209ab9b433ec2073258a098c23c939bd342f57b67ad894774e31f0699fa5`; the runner recorded no worktree changes, and `Library` was absent. The pinned official CoreValidation run passed cold and warm at **1,214/1,214** each, with all failures, errors, skips, timeouts, aborts, and not-executed results at zero. The two native reports are retained privately and remain byte-identical to their captured identities:

| Original report | Bytes | SHA-256 |
|---|---:|---|
| `CoreTests-cold.trx` | 1,753,398 | `288c44e35d2a7a4261f60fd00a0d9e7a2fa7ba88daab8dc0c8d1a639e589408d` |
| `CoreTests-warm.trx` | 1,753,398 | `a4051f4beb1cb82f3ad7e72ae177547fae08d73552280f385a242435704e541e` |

The clean recapture then exposed a separate official adapter defect: standard Core package manifests do not declare optional `format.runProfile`, but source-context verification accessed it unconditionally. The minimal correction in tool commit `a71b32abe2fad8b2895c95c18ad37e58810b0030` guards that optional field; a regression exercises standard-profile dispatch when it is absent. This tooling-only correction does not change Simulation Core production code or its C# test inputs. The focused evidence-tool suites passed: TRX adapter **154 assertions** and package-tool **69 assertions**.

The official packager produced bundle `lab-obs-a-daily-v1-core-9300a513be1a4ebea53b5917881e2a2b`. Its normalized cold and warm reports each contain 1,214 individually passing test cases and preserve their native run identities, test identities, names, results, and counters. The normalized reports are 1,515,191 bytes each with semantic hashes `3d5783f456423352339c09c1c294bf7c19fa8ce18df2dbb2204ad44dfac7cbf1` (cold) and `948e46a74db67c5066c0c2fdcb6b2a3eaa6c424e4eb4f10af4a444292e46c967` (warm). Both source-context/original-comparison verification and archive-only verification passed. The independently rebuilt deterministic archive matched its recorded hash.

| Published artifact | Bytes | SHA-256 |
|---|---:|---|
| `evidence/lab-obs-a-daily-v1-core-9300a513be1a4ebea53b5917881e2a2b.zip` | 580,993 | `fe0f9e1a72145f117bdad186c95808f24e9703f2348393d9933e9608f77ebc8e` |
| `evidence/lab-obs-a-daily-v1-core-9300a513be1a4ebea53b5917881e2a2b.manifest.json` | 260,901 | `e147be2bdb46a8f95fe31db954762d686c6346fde1fcf0158be0d8f46c051768` |

The independent package review passed. It matched all 2,428 cold/warm case identities, definitions, names, outcomes, and counters to the original reports, confirmed deterministic archive reproduction and public privacy checks, and verified that the package binds to tool commit `a71b32a` and source commit/tree `b9b049d` / `569cb5f`. The package contains normalized reports and public manifest only; original TRX, logs, runner manifest, source context, traces, and recovered outputs remain in owner-protected E: storage. No Unity validation was needed because no Unity production, assembly-definition, test, or package input changed.
