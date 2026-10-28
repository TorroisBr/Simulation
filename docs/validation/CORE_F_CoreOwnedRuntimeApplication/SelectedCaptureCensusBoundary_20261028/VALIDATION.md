# Selected Capture Census Boundary — Validation Record

## Scope

This bounded change makes Core enforce selected Daily-v1 capture admission on both sides of the existing host census callback. Core checks the admitted profile, owner thread, exact capture allowance/advance-lease relationship, bound runtime and configuration, selected execution, mutable guard, callback result, nonnegative mutation epoch, and complete selected owner-section vector. It clears census outputs on rejection.

The existing Unity `ContinuationCensusProtocol` remains the live census reader and retains its factual world-publication check. This change does not replace or broaden the 89-family reader, add owner writers, change P12 payload semantics, publish capture tokens, or claim P12 readiness. No owner coverage or hydration capability is inferred.

## Candidate source identities

Validated working inputs were based on candidate commit `db28d143c51f14ae32d1f079a3402fac08d055f8`, with the following SHA-256 identities:

| File | SHA-256 |
| --- | --- |
| `Packages/com.simulation.core/Runtime/Application/DailyV1ExecutionBoundaryState.cs` | `1f943a8cdfff81b7a73e3d7fccfdb8ce1eee090598530f6186a595e73e621bb4` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs` | `3c0ab8192da4982e0fb8f872cf28887a13429b91f58ad61494d3afe1db11e2fa` |
| `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs` | `5db62f7e08d724ff8c67321555e9623f04de038db80c3a983407eac45ab8448b` |

The only code/test changes in this slice are those three files. This record was added after validation and does not alter executable or test inputs.

## Validation

| Gate | Run ID | Result |
| --- | --- | --- |
| Official CoreValidation, cold and warm | `69bbad26632a431aa7171f97f7fef0de` | **918/918 PASS** |
| Full Unity EditMode | `0898a26bf1994969bfef5dfe46f3c720` | **3275/3275 PASS**; zero failed, skipped, or inconclusive |
| Official Smoke filter | `399aae1a86b44c0aaaded4d9b70964e9` | **5/5 PASS**; zero failed, skipped, or inconclusive |
| `git diff --check` | — | **PASS** |

The official CoreValidation and Unity runners reported stable inputs. The EditMode and Smoke run manifests identify the same two production-file hashes listed above. CoreValidation covered the new census-boundary regression in the session-gate test file. Original XML and runner artifacts remain in private owner-only retention; no raw reports or workstation paths are included in this repository record.

The new regression verifies rejection before invoking the host callback for a mismatched DailyAdvance lease/scope, rejection of an idle census while an advance lease is held, acceptance of a complete selected owner vector, and rejection/clearing of incomplete census output.

## Review status

Independent exact-tip implementation review is pending for the candidate commit containing these validated changes. This record is not canonical promotion or CORE-F completion.
