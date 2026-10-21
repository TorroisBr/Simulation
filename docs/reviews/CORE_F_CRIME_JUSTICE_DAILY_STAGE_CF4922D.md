# CORE-F Crime/Justice daily-stage exact-tip review

**Verdict:** `VALIDATED_CANDIDATE` / PASS
**Review scope:** Bounded selected Daily-v1 Crime/Justice daily-stage integration and its validation evidence.

## Reviewed identity

- Repository: `https://github.com/TorroisBr/Simulation`
- Candidate branch: `codex/core/CORE-F-runtime-boundary-3ec0aa`
- Exact candidate tip: `cf4922d564641b2a11e4deaf611e3dc2ff1a9934`
- Candidate tree: `36c45972115cbb67b1c5ea5274b652a467ac4b2c`
- Base: `2c11ef7b772e34207f9d7f1592f91e1b08746e66`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Promoted Crime/Justice stage source: `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedCrimeJusticeDailyStage.cs`
- Stage source SHA-256: `f498c8ba07f788cfed2c5aa9334482db0cea563efaac852750aa0835221d8b03`
- Exact runtime admission test SHA-256: `ab02fd2fc20f1a06254fe15707b84ba868e9206ebd03d67ffe3002a3adea6cc1`

The independent reviewer verified that the candidate branch and remote ref resolved to the exact tip above, the Core canonical remained at the stated base, and the candidate was a clean descendant. The reviewed implementation code tree is `2c18d367a1ffc012b416c1aae002478536d1235b`; the later candidate commit records validation evidence only.

## Review result

No actionable findings remain. The earlier review's selected-profile long-run evidence finding is closed by the exact-source two-day test in `SelectedCrimeJusticeDailyStageContinuesAcrossPublicDailyAdvances`, which exercises the ordinary public `SimulationRuntime.TryAdvanceDay` path twice using the accepted `UnityBootstrap-Daily-v1` profile.

The reviewer confirmed:

- A roster containing an unselected NPC takes the whole-stage Unity compatibility fallback. The public-advance regression verifies selected and unselected Crime/Justice projections continue to advance through that fallback; it makes no claim of Core execution for the mixed roster.
- The selected Core path preserves the established phase and writer order: Justice arrest-marker clear, Crime hidden-state advance, Justice sentence/warrant advance, and final Justice wanted-status synchronization.
- The exact P12 integration test verifies separate admitted Justice/Crime owner writes, local revision changes, a no-op target projection, the corresponding mutation epochs, and valid owner census.
- The selected multi-day public path continues a sentence and hidden timer once per completed day, advances revisions and mutation epochs, and retains valid census across both days.
- Core unit coverage includes active countdown, resolved warrant handling, sentence removal when the actor is no longer arrested, release of an arrested actor without an active sentence, subsystem combinations, no-op revisions, and rejection of unresolved Justice links before stage mutation.
- Strict Justice relationship checks agree with the selected owner graph and P12 restore invariants; unresolved or inconsistent warrant, sentence, NPC, and City references are rejected before this Core stage mutates owners.
- Source and test bytes match the identities in the exact-tip validation snapshot. The packaged full EditMode result includes the new long-run test; the package hash matches its manifest.

## Validation evidence reviewed

| Gate | Result | Private run ID |
| --- | ---: | --- |
| Selected multi-day public-advance test | 1/1 PASS | `9b605d189fa5425eb95e88bd2ff1a4b5` |
| ALL EditMode | 3247/3247 PASS | `0e73a7dac8424c0ab3fe909fbd4f3d13` |
| Official Smoke | 5/5 PASS | `686595efb7054680a998a40e667bfdf6` |
| CoreValidation cold/warm | 576/576 PASS each | `130b424318894ef79ae37239029835a4` |
| `git diff --check` | PASS | candidate base `2c11ef7` |

The Unity evidence uses Editor `6000.3.9f1` and Test Framework `1.6.0`; all reported inputs are stable and test totals coherent with zero failed, skipped, or inconclusive cases. CoreValidation's source inputs were unchanged by the later Unity-only test addition. The normalized full EditMode evidence archive is `92017796d06f17bfa2098ac1924fc6dc10769d69ab993072418d4f66a00fb5a4`; the matching manifest SHA-256 is `1846280120043007b7fa5132ea00ea83abcca1d77a47841878f2199b2d0726d3`. Protected original XML and logs remain in local retention.

## Limits

This PASS validates only the bounded Crime/Justice daily-stage candidate. The candidate remains unpromoted. It does not establish Core ownership of the complete daily loop, `DailyV1Application`/`TryStart`, detached P12 restoration, a fixed .NET host, separate-process T/T+1/T+2 continuation, or full CORE-F acceptance. Phase/Core canonical advancement is outside this review.
