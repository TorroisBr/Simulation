# Core-owned selected travel execution context — validation

Date: 2026-10-30

Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`

Implementation commit: `579a627ea399f93a79aaec07f61e1399d73bdaa6`

Implementation tree: `c3a2e5d3cc7643af084c47a0df161d62c12b2d7a`

Candidate base: `cc7c1d721edcef543d96afbc7319f1a1f9d46efa`

Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

## Scope and limits

For admitted selected Daily-v1 execution, Core now constructs the travel-day
execution context and binds it to the exact admitted `DailyV1ExecutionBoundaryState`
and `DailyV1SelectedRuntimeOwnerGraph`. The host supplies only projection,
P12 travel-party protocol, and result notification ports. Core rejects a
context bound to another graph and rechecks the admitted session/graph binding
before owner-operation, publication, and observation calls. Existing
non-admitted compatibility construction remains unchanged.

This is a bounded context-ownership step. The Unity host still implements the
projection/P12 travel ports; this does not complete Core ownership of all actor
actions, the complete 89-family census/capture authority, `DailyV1Application.TryStart`,
detached restoration, or the executable .NET host. Core canonical is unchanged.

## Exact changed source identities

| File | SHA-256 |
|---|---|
| `Assets/_Project/Scripts/SimulationRuntime.DailyV1TravelStage.cs` | `c09ab40289dc2d52a85e28e7ff25683fbcf4978f9825c049dc8b76fa6a72fca6` |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `ffd5d6c9b2bcee1a4e7da524184f817065f57b11aae31b0b4f64ff86d0aae5f2` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedDailyExecutionStage.cs` | `b9b995610408e1a0d80eafa2a84845c62777ee16fdbad8300139f56efbfdc01e` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedTravelDayStage.cs` | `f6c10b4b510dd9d9a0731fa4fe8ad11012ccbf633ca5268003c49ed13d0b604c` |
| `tests/Simulation.Core.Tests/DailyV1SelectedDailyExecutionStageTests.cs` | `0a7d3ceda5ca2d125cd18fe383caacc5ca7fa37a5df1467570a8effeaac757be` |
| `tests/Simulation.Core.Tests/DailyV1SelectedRuntimeOwnerGraphTests.cs` | `bcf687fe262a5a0e71393fc2404770ba287e62d2fc2031dd4b4d9c771b6598f4` |

The committed file contents match the source bytes recorded by the validation
runners. No executable source changed after the runs. The three pre-existing,
user-owned `ProjectSettings` modifications and unrelated untracked/protected
files were not staged or modified; they remain outside the candidate.

## Official CoreValidation

- Runner status: **Passed**; cold **958/958**, warm **958/958**.
- Runner run ID: `c8deb67b5a8543a39808865df9d690a6`.
- Cold test run: `0cb67634-1a8a-4247-ab04-47386c7ddac5`.
- Warm test run: `7bab21dd-f41a-4bae-9be1-444e4991adce`.
- Stable input digest: `f25700930cab3dfaa1a56c3efc422541752905912f1fe4c3af7dac504c41afc9`; 536 source inputs and generated dependencies remained stable. All 536 source-input hashes match the current candidate worktree.
- Cold TRX SHA-256: `bcfe723cd84942a76e034c63ea166533a8b0df98403e5154df490336f9cb356f`.
- Warm TRX SHA-256: `8d6aef17c17ed7630bc7f8b9f2dfc6a93ec47f9ba0339547d986958abdff5f0d`.
- Protected native runner-manifest SHA-256: `bad9fba45a55eed1d25e88d36116bed58335174f27a7bcfb96aab7bbfa8be2bd`.
- Original TRX and manifest remain in owner-only local evidence. The native runner manifest does not satisfy the separate `dotnet-trx-source-context-v1` schema required for public normalized TRX packaging, so no public .NET report package is claimed. The validation worktree had a Unity `Library`; this run makes no Library-independent proof claim.

## Official Unity validation

- Unity EditMode: **3283/3283 passed**, zero failures, skips, or inconclusive cases.
- Run ID: `1d9bc42fe8cd43628acbad71a81f2170`; Unity `6000.3.9f1`.
- All 1,974 captured project inputs were stable before/after execution and matched the current worktree after checkpointing; no input hash mismatched.
- Protected original XML SHA-256: `28873f34faea8829c9cf62994cd40d2f9fef35c4c8d739f1253a75a46281c4d4`.
- Protected runner-manifest SHA-256: `05c6a518f47a876bd83765a1a3ef29f7d397252d16daafaad285e9840edc8843`.
- Privacy-safe normalized package verifier: **VALID**, one run, one archive entry; verification used the protected original runner manifest.
- Public normalized manifest SHA-256: `0be5f5982dab74fe9cc1bbae06ff3e9e895dac4b54c83629a49ad550972fefd7`.
- Public result-only ZIP SHA-256: `cf1c4537d420f19f44ed3d06ad2feafb291ba44b07910ec4c4ab8085660a3c9a`.

The standalone official Smoke filter (`TestFilter=Smoke`) also passed **5/5**
with zero failures or skips, run `bc46a1ec7c514e359dbbfb1c1d20635e`. Its five
case identities match the five cases in the full EditMode package, and the
full-run and Smoke-run file inventories have identical relative paths and
hashes. Their overall manifest digests differ because the full run recorded the
pre-checkpoint base commit plus dirty implementation files while the Smoke run
recorded the committed candidate. Original Smoke XML and its manifest remain
protected locally; no standalone normalized package is claimed.

An earlier selector attempt used `TestCategory=Smoke` and selected zero cases;
that invocation is explicitly excluded. The counted Smoke result above used the
repository-established `TestFilter=Smoke` selector.

## Final check

`git diff --check` passed. The implementation commit contains exactly the six
listed production/test files. This candidate remains an incomplete CORE-F
checkpoint and is not promoted to Core canonical.
