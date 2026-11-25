# CORE-F Crime/Justice Legacy Compatibility Regression — Validation

Date: 2026-11-10
Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
Base: `10c59bdbc144154685225b45246eaa2b8386baeb`
Status: candidate-only; no Core canonical promotion or CORE-F completion claim.

## Change

Added one Core regression for the existing `AdmissionEnabled == false` compatibility path in `DailyV1CoreSelectedActorTurnExecutionBoundary`. It uses the actual admitted actor-turn context factory, confirms the supplied mutation runs once without delegating to the legacy host mutation wrapper, and verifies Core neither pins this non-admitted binding nor faults the census session. No production source or Unity test source changed in this follow-up.

Test input SHA-256: `d8fa658ee1bc5a0f1a777e191686d947113ef9c97ffc174b5be1cb75d2d29ecb`.

## CoreValidation

Official run `64c084cfc6d34d7dbfb570deb4455d21`: **1,089/1,089 cold and 1,089/1,089 warm PASS**. Captured source inputs were stable (`e3074f0391b4fe8cf035adccae19982fe47c840a4b4356de36e3e28a24e99374`), and the new test hash matched the private input manifest.

- Cold TRX SHA-256: `cc104baa66815d2a93c73318666c8335d08bb2b3cf7984cf7e2183fa76554ffb`
- Warm TRX SHA-256: `432f870bfb74f08d81d4e49efda21731c66e13690b063f8c1ba402d836526669`
- Private manifest SHA-256: `2ce821b6e205e50fc2f5e43acba104dd0e5d43b53e6459f4601f48e9d79bfc4a`

Original reports and manifest remain in protected local evidence. The native CoreValidation report is not the separate `dotnet-trx-source-context-v1` schema, so no public DotNetTrx package is claimed. The runner reported a Unity Library in the worktree; this does not establish Library-free validation.

## Unity evidence applicability

The parent candidate's Unity source/test inputs remain unchanged: this follow-up modifies only `tests/Simulation.Core.Tests` and this record, outside the Unity runner's captured `Assets`, `Packages`, `ProjectSettings`, and validation-tool roots. Therefore the parent's exact-source full EditMode result (3,305/3,305) and Smoke-filter result (5/5) remain applicable. The full EditMode package is published and verified in `CORE_F_SelectedCrimeJusticeSessionBinding_20261110`; the filtered Smoke result remains protected only because the packager rejected its inconsistent parent suite count.

## Limits

This test closes a compatibility coverage note only. It does not broaden the reviewed session-binding implementation, establish a public `DailyV1Application.TryStart`, deliver independent .NET continuation, complete the 89-family P12 census, or satisfy full CORE-F acceptance.
