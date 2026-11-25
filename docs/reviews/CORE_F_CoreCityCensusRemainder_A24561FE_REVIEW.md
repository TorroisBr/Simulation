# CORE-F Core-owned City Census Remainder — Exact-Tip Review

Date: 2026-11-09

## Reviewed identity

- Candidate branch: codex/core/CORE-F-restore-stage-transaction
- Exact candidate: a24561fe0c0f64bd653a7d3c8f147c9a1599a5a8
- Candidate tree: 2dd0bdfade371bba0af2ce8a807b5522b2a6e206
- Exact base: 6ea144c9544c7bc0aaf964f86e1edc09dd38f6f1
- Current Core canonical: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea

## Verdict

**VALIDATED_CANDIDATE.** Independent exact-tip review passed with no
outstanding findings. The previous review's test-coverage finding is resolved
by the test-only follow-up at ace43a3a41d8b8aebdb318ba383e17d4f84c9c0e.

The full candidate delta contains only:

- Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs
- tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs
- docs/validation/CORE_F_CoreCityCensusRemainder_20261109/VALIDATION.md

The implementation preserves Core's epoch-before-projection ordering. It omits
the selected City Important-NPC presence, Market stock, Population aggregate,
and Population operation-receipt rows only when the complete witness set
matches the exact Core owners by section identity, required role, schema,
owner object identity, cardinality, and revision. Stale or partial selected
rows and extra compatibility City rows remain available to the existing P12
classification path. The later composition gate rejects a partial selected
City set. Population and receipt rows preserve their shared owner identity.

The independent reviewer verified the partial-City regression reaches the
host-remainder projection, checks that each remaining captured City row is
preserved by reference, and verifies the next City composition gate rejects
the incomplete set. Existing exact, stale, and mixed-roster assertions remain.

## Validation reviewed

- CoreValidation run 89e1be289d8f413195571e6dbfe90c9e: 1,076/1,076 cold and
  1,076/1,076 warm; zero failures. Stable input digest:
  13efe1eab5236f0d563ca91392ec7844e0536914ea057c8ffdd3a3bfbd579f71.
  Cold and warm TRX SHA-256 values match the validation record.
- Full Unity EditMode run e4d3b22fa4e848f58ebae68bab4b3034: 3,303/3,303,
  zero failures or skips. Stable input digest:
  d39ac9326d6ce24bbbe2dbf07fa1249bca80d7a869c76c56943251c9bf08b8ca.
- Filtered Unity Smoke run 4e0af08a0b6a4ad7a8c32f61fc7ca6e3: 5/5, zero
  failures or skips, same stable Unity input digest.
- The reviewer checked the final source hashes against the committed files and
  validation record; the Unity input manifests include the unchanged
  production source and exclude the .NET-only test file. The test-only update
  therefore does not invalidate the exact Unity evidence.
- XML and log artifact hashes match the private run manifests. Raw results
  remain in protected storage; no private machine paths or raw logs are
  published.
- git diff --check: PASS.

The reviewer verified that the candidate fast-forwards from its exact base and
that protected ProjectSettings edits, unrelated validation files, untracked
files, and private evidence are not part of the candidate diff.

## Limits

This is a bounded candidate only. It does not claim that all 89 owner families
are Core-owned, that P12 is READY, or that CORE-F is complete. Unity remains
the full quiescent census-vector and mutation-epoch source. No canonical
promotion occurred.