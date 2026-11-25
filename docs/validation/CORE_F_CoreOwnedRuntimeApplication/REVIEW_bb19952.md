# Independent exact-tip review — Core-owned event/history authority

## Reviewed identity

- Candidate commit: bb19952f930c3b2985ed3b6a0f43d0190af5657d
- Candidate tree: 82e9c0d2eee74f464ae06ad61a79d33709ae5e0b
- Review base: 5cc0533d668b69ae922f3506a2abf89e77b3b02d
- Core canonical at review: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Verdict: **VALIDATED_CANDIDATE**

## Review result

The independent reviewer found no actionable defects. Core genesis creates one event/history authority bound to the exact runtime ID allocator, simulation clock, record sequence, store and history instances. The prepared active session retains that same authority.

Both detached P12 restore routes create fresh event/history roots and pass the exact same objects through staged writers, the Core session and Unity projection. The Unity projection enforces identity. The synthetic restore fixture was corrected to use the same roots; its existing parity assertions remain, and image restoration now verifies that event/history read models are fresh and empty. Event/history rows remain outside the persisted P12 profile.

## Validation evidence

- Official CoreValidation: 915/915 cold and 915/915 warm, PASS.
- Focused P12 restore test: 1/1, PASS.
- Full Unity EditMode: 3276/3276, PASS.
- Official Smoke: 5/5, PASS.
- git diff --check: PASS.
- Unity input inventory was stable across focused, full and Smoke runs; all three used the same source-input digest.
- The reviewer verified all changed files against the candidate and official validation input inventories.
- The CoreValidation checkout contained a Unity Library; no Library-independence claim is made.
- Original validation reports and private manifests remain in protected local evidence. No public .NET TRX package is claimed.

Protected ProjectSettings edits, unrelated local files and private evidence are not part of the candidate commit.

## Scope boundary

This review accepts only the exact event/history-authority implementation and evidence above. CORE-F remains incomplete and unpromoted. This checkpoint does not establish complete DailyV1Application.TryStart, a detached .NET host or separate-process T/T+1/T+2 continuation.