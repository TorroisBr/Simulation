# CORE-F Crime/Justice selected-stage multi-day revalidation

**Status:** Candidate validation evidence; not canonical promotion or CORE-F acceptance.

## Source identity

- Repository: `https://github.com/TorroisBr/Simulation`
- Candidate branch: `codex/core/CORE-F-runtime-boundary-3ec0aa`
- Candidate commit: `6e0af1212f346605367254dd76be2abc19e4fbf0`
- Candidate tree: `2c18d367a1ffc012b416c1aae002478536d1235b`
- Candidate base: `2c11ef7b772e34207f9d7f1592f91e1b08746e66`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Crime/Justice stage source SHA-256: `f498c8ba07f788cfed2c5aa9334482db0cea563efaac852750aa0835221d8b03`
- Runtime admission test source SHA-256: `ab02fd2fc20f1a06254fe15707b84ba868e9206ebd03d67ffe3002a3adea6cc1`

## Review finding addressed

The preceding exact-tip review returned `NEEDS_CHANGES` because the new selected Crime/Justice stage had only one-day direct-stage integration evidence. The existing long-run suite did not exercise this selected-profile path. This follow-up adds `SelectedCrimeJusticeDailyStageContinuesAcrossPublicDailyAdvances` and validates it through the normal public `SimulationRuntime.TryAdvanceDay` path for two consecutive admitted Daily-v1 days.

The fixture uses the exact `UnityBootstrap-Daily-v1` profile, verifies that every roster member is bound to the same selected definition graph, and creates a warrant/arrest through the existing P12 Crime/Justice action scope. It verifies the arrest marker clears, the sentence advances exactly once on each day, the hidden timer advances once on each day, the selected Justice and NPC owner revisions advance, the roster mutation epoch advances on both completed days, and the owner census remains valid. The existing one-day stage integration case continues to verify the exact separate writer scopes and three changing-scope epoch increments.

## Validation

All recorded runs used Unity `6000.3.9f1` and Test Framework `1.6.0`. Each run reports stable validation inputs, coherent totals, zero failures, zero skipped tests, and zero inconclusive tests.

| Gate | Result | Private run ID |
| --- | ---: | --- |
| Selected multi-day public-advance case | 1/1 PASS | `9b605d189fa5425eb95e88bd2ff1a4b5` |
| ALL EditMode | 3247/3247 PASS | `0e73a7dac8424c0ab3fe909fbd4f3d13` |
| Official Smoke (`EditMode`, `TestFilter=Smoke`) | 5/5 PASS | `686595efb7054680a998a40e667bfdf6` |
| CoreValidation cold/warm | 576/576 PASS each | `130b424318894ef79ae37239029835a4` |
| `git diff --check` against the candidate base | PASS | — |

The CoreValidation source inputs are unchanged from its recorded cold/warm run. The added file is a Unity EditMode integration test and is not part of that .NET CoreValidation input set. Its exact source hash is recorded above.

## Public evidence package

- Manifest: [`coref-crime-justice-selected-multiday-editmode-exacttip-20261021.manifest.json`](revalidation-20261021-selected-multiday-exacttip/coref-crime-justice-selected-multiday-editmode-exacttip-20261021.manifest.json)
- Manifest SHA-256: `1846280120043007b7fa5132ea00ea83abcca1d77a47841878f2199b2d0726d3`
- Archive: [`coref-crime-justice-selected-multiday-editmode-exacttip-20261021.zip`](revalidation-20261021-selected-multiday-exacttip/coref-crime-justice-selected-multiday-editmode-exacttip-20261021.zip)
- Archive SHA-256: `92017796d06f17bfa2098ac1924fc6dc10769d69ab993072418d4f66a00fb5a4`
- Private run-manifest comparison and archive verification: `VALID`, one run and one entry.
- The packaged full EditMode report contains the multi-day case and all five Smoke identities. The filtered Smoke report and its logs remain in protected local retention.

Original reports remain preserved in protected local validation retention. The public package contains normalized result evidence only and has no local workstation path dependency.

## Scope boundary

This evidence closes only the selected-profile multi-day validation finding for the bounded Crime/Justice daily-stage integration. The candidate remains unpromoted. It does not establish a Core-owned full daily runtime, `DailyV1Application`, detached .NET restoration, .NET host continuation, general mixed-roster Core execution, or CORE-F completion.
