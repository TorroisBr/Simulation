# Independent Exact-Tip Review — CORE-F Initial Publication NoSave Gate

**Verdict: VALIDATED_CANDIDATE**

## Reviewed identities

- Repository: https://github.com/TorroisBr/Simulation
- Candidate branch: codex/core/CORE-F-initial-publication-admission
- Candidate tip: 82c3b8feb42a0d96bdf6734c2c9dfbb0a90dcd18
- Candidate tree: 29c4df55844e35a0465f23f86892ba753067aae1
- Candidate parent / implementation commit: b86a3c9a0b4942d6edf49c21692baf8b9f41143f
- Implementation tree: c47b5e54a5cf4a655b1dd9423541a22845270ce6
- Exact base: 2a97c2fe3271b4c7249283594c19ba8fd20bb3c3
- Current Core canonical: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea

The refreshed remote candidate and Core canonical refs matched these identities. The implementation commit is a direct child of the stated base. The candidate tip adds only its validation note, Unity package manifest, and package archive after the implementation commit. The base-to-candidate executable diff changes only DailyV1Application.cs and DailyV1ApplicationSessionGateTests.cs. No ProjectSettings, unrelated metadata, or untracked user files are in the candidate diff.

## Code and test review

DailyV1Application.TryPublishInitial preserves the prepared-genesis path's exact prepared-session, execution-boundary, definitions, and admission-lineage checks. Its direct fallback now rejects unless the application has a successful slot inspection, that inspection found no slot, no load was attempted, no genesis-blocking result was recorded, and the candidate descends from the application's bootstrap admission context. These checks are made under the same session-gate lock as publication. The existing TryHasSlot contract maps a successful missing slot to NoSave; a present slot reports Loaded, and inspection errors fail.

DirectInitialPublicationRequiresSuccessfulNoSaveInspection constructs a bound candidate without inspecting the slot, then asserts publication fails and ActiveSession remains null. Existing direct fallback tests now establish a successful missing-slot inspection. No existing assertions were removed or weakened. This review found no actionable code or test defect in the bounded direct-publication gate.

git diff --check passed for the exact base-to-candidate range.

## Validation and evidence review

- The validation note records CoreValidation cold/warm as 902/902, run de4dc72b2c59446586c71f44e9639e33, with original protected manifest and TRX hashes. Those raw protected files were not independently accessible during this review. The note accurately makes no public DotNetTrx package or original-to-normalized comparison claim because the run lacks the packager's required dotnet-trx-source-context-v1.
- The Unity package manifest identifies EditMode run 8058b21234024baf9d46e3aba3df0275, exact implementation commit/tree, and 3274/3274 passed with zero failed, skipped, or inconclusive tests. Its input snapshot source hash for DailyV1Application.cs matches the committed source after Windows CRLF normalization.
- The committed archive is 173,309 bytes and SHA-256 4bc4235495497261d1364ad0f7951e545bcef35be0fe370bb7be3494339a4e4d, matching both the manifest and validation note. It contains one normalized XML result, 1,714,332 bytes, SHA-256 4ac25004b3eef0bc255ed727e9a09f84b938cb6035570c17ab08c6293492e8b0. The XML reports 3274 passed, zero failed/skipped/inconclusive, and 3274 test cases. The public manifest and normalized XML contain no detected absolute workstation paths.
- The validation note records official filtered Smoke as 5/5; its separate raw evidence remains protected and was not independently inspected. No separate public Smoke package is claimed.

## Limits

This validates only the bounded NoSave prerequisite for direct initial publication. It is not canonical promotion, formal phase closure, completion of DailyV1Application.TryStart, full P12 restoration, detached .NET hosting, or full CORE-F acceptance. Core canonical remains a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea.
