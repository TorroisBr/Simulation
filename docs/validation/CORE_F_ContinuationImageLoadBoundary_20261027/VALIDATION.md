# CORE-F continuation-image load boundary validation

## Candidate and scope

- Code candidate: `23eb358309bbf0628515ae08a721574097338aa0`
- Code tree: `2c190e3f4eae5de563e5121b8734b69ff1a1bd8f`
- Parent/base: `9f91656f7e1999109b1f067f468c7fb6e7b6f75d`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Candidate branch: `codex/core/CORE-F-shared-daily-v1-app-host`

This slice routes continuation-image reads for Unity-v1 and .NET-v2 through
the exact Core application session and replacement reservation. It preserves
the existing detached staging and publication path. It does not complete
restoration, the detached .NET host, or CORE-F, and it was not promoted to
Core canonical.

The three committed file SHA-256 values match the bytes in the successful
validation input snapshots:

| File | SHA-256 |
|---|---|
| `Assets/_Project/Scripts/TesteSimulacao.cs` | `09241d0424d7b9d3221d7d89661e00970ae0a883e92fdaf3982abf35265ee7b4` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` | `b59232492688331487e1994e888e43d67125c573e731b363ac1b3b2ab832ee46` |
| `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs` | `f47eaa881b9a70855e4fcc362f6780afda3ba3567a13b11eb3151f22bec1ca29` |

## Validation

All successful runs used the official repository runners. Original reports and
logs remain unchanged in owner-protected private E: evidence storage; their
machine-local paths are intentionally omitted here. These private runs are
not a substitute for the complete permanent evidence package required before
any future canonical promotion.

| Gate | Run ID | Result | Original XML SHA-256 |
|---|---|---:|---|
| CoreValidation cold and warm | `06dfad5f5c8e4ec689a3913c9bb94e34` | 892/892 cold; 892/892 warm | cold `46a9f96613680295a70bf02f9723d1ce2075cba2be52871ec303d2c9cfde126a`; warm `faad625d6a43b9242b1105e95ab504aaf6c4bdb836cdb50897be590ed64ec923` |
| Focused Unity P12 restore | `a3de3a1365514a50b62c97c224621b93` | 1/1 | `5f82387268b9d50dae97e4b0001af722eb633b904e2ac7e893340c9ba6710424` |
| Unity ALL EditMode | `5b65070bf9474e32bd34c2e760348555` | 3274/3274 | `5e9de79d50919fa8a57c3de9ce51469a1649670b9fa87e50473435182a4313ae` |
| Official EditMode `Smoke` | `c0e12b0eb3844054bb4075f8ec60d9de` | 5/5 | `ef8ed4de5ec6c4773160f10549e8252fe24d02cef3e26a236fa9a176a3f76090` |

The Unity runner recorded stable input snapshots across all 1956 inputs. The
test runs occurred before the code-only commit, from the same source bytes;
the Unity input manifest therefore names parent `9f91656` as `HEAD` and records
the candidate files as working-tree inputs. The pre-existing modified
ProjectSettings files were present during these runs, remained untouched, and
were excluded from the candidate commit. Their hashes and all other input
identities remain in the private original manifests. The common Unity input
snapshot SHA-256 is
`4de84aaa07496157cc303c8a449dc39707824134eaf2823e67f6367fcffd2efb`.

The CoreValidation input digest is
`8697507c24aec8986cd4119f66c7d7d5d8a831b05deee7d888a5b28c38b71fff`; the
runner recorded stable inputs and the pinned .NET SDK `9.0.315` with the
testhost selecting CLR `8.0.28` for both cold and warm runs.

`git diff --check` passed for the candidate change. An unfiltered PlayMode
probe discovered zero tests and is not counted as a validation gate; this
repository's five-case Smoke gate is `EditMode -TestFilter Smoke`.

One initial CoreValidation attempt ran 892 tests and had one failed test
assertion: the new test expected `Busy` when the required replacement
reservation argument was absent. The API correctly rejected that invalid
argument as `InvalidOwnerValue` before storage access. The test expectation was
corrected without changing production behavior; the successful full cold/warm
run above followed. The original failed run remains preserved privately and
was not edited or represented as a passing result.
