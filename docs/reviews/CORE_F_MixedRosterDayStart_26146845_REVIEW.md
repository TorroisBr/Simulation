# CORE-F mixed-roster day-start fallback — exact-tip review

**Status:** VALIDATED_CANDIDATE for this bounded integration slice only. This is not
CORE-F completion, detached-host acceptance, or Core canonical promotion.

## Exact identity

- Candidate branch: codex/core/CORE-F-core-owned-runtime-application
- Candidate commit: 261468453af101973403e626207f111141cafac4
- Candidate tree: 361015a84a4bd606c2347d584b135c82b2ae64c6
- Parent: e50dd720f769fef08f0c8f2fd72d9b0d63a442a8
- Core canonical at review: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Candidate remote ref was synchronized at the candidate commit.

Changed files:

- Assets/_Project/Scripts/SimulationRuntime.cs
- Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs
- Packages/com.simulation.core/Runtime/Application/DailyV1SelectedDailyExecutionStage.cs
- tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs
- tests/Simulation.Core.Tests/DailyV1SelectedDailyExecutionStageTests.cs

## Review result

Independent exact-tip review: PASS; no actionable defects. The reviewer checked the
five-file diff against the stated parent. Core selects the existing whole-stage
compatibility fallback from the host's factual HasUnselectedLiveRuntimeMember
projection. That mixed-roster path skips selected Core day-start owner operations to
avoid double execution. The selected path retains the admitted Core roster, exact owner
graph checks, and host projection checks. Tests add coverage for Core selecting the
mixed-roster fallback; existing Unity coverage still asserts the unselected member
mutation and revision behavior. No test assertions were weakened.

The implementation delegates daily work only for the existing mixed-roster fallback;
it does not claim that the selected actor runtime, application lifecycle, or persistence
is fully Unity-independent.

## Exact-tip validation

- Official CoreValidation run d499cff26aa746b8a0236219a6586cbc: 700/700 cold and
  700/700 warm, status Passed. Lab build, cold, and warm gates also passed. The four
  Core source/test input hashes in the runner manifest match the candidate checkout.
  Manifest SHA-256:
  B63ED15094B398936867373950889F37276D7EF82FD2576EF3068CE7D1AB0F7A.
- P12A continuation suite run 8ae75a62ac71495fbfadbd15ad3903b1:
  37/37 PASS, input stable. Manifest SHA-256:
  B68805F35C61CD7519A9F23B2616C8C10C8A3430CD5DC30E72EC125615FAA020.
  XML SHA-256: f523222b767c0d1f96c092b51c670b8920bbf455556a95c8c019cf79dd4b765f.
- Full Unity EditMode run d98e3b4bf1d94aae9b76173a9cbba9e6:
  3258/3258 PASS, input stable. Manifest SHA-256:
  A3D7D012291EC2B81683BFE79BA654D0329C57705E9F15C750EA8614FA5B30AB.
  XML SHA-256: 2a24e3d6caae89e56c3f9e2ceed309052883c00315c8c422e2c2bdefab84e2b1.
- Official Smoke run 53a5e21b3c074b5eac85cb898ebb5e33: 5/5 PASS, input stable.
  Manifest SHA-256:
  FF79CADC4390EBD4D36E910426000DE1BBD74D307D7D75DBB43205038401E829.
  XML SHA-256: 35518c0672745405f0f8d3a045dbd552cadb3d30b880f7893b373c89131bb8cd.
- Before Unity validation, all five modified candidate blobs were compared with the
  dedicated E: validation worktree and matched. Unity input snapshot digest:
  0a7a0f333092f358e7ee1be582743418047bd78d4716f49936ebd80ca09dd664.
- git diff-tree --check from parent to candidate: PASS.

All official Unity run directories above have protected ACLs, with one explicit owner
ACE and inherited rules disabled. Original reports remain private; this record stores
no workstation paths or raw logs.

An earlier P12A filtered run (90299a4d40ad479b83e4ea26f2a16f5e) returned 33/37 with
four storage failures. No production or test source changed afterward. The exact store
test then passed in isolation, a diagnostic using the same v1/v2 image validators and
atomic slot operations passed, and the clean exact-source P12A suite above passed 37/37.
The initial failure remains preserved as a non-reproduced diagnostic result; it is not
reported as a passing run.

## Remaining CORE-F work

This slice does not deliver the public DailyV1Application.TryStart lifecycle, complete
selected City/NPC runtime construction in Core, detached P12 restoration through the
shared application, or the fixed .NET host and T/T+1/T+2 continuation. Those remain in
the approved integrated CORE-F package. No Core canonical ref or numbered Phase status
was changed by this candidate.
