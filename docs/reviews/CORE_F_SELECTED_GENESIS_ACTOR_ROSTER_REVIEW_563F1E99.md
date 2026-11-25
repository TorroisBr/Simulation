# CORE-F Selected Genesis Actor-Roster Authority Review

## Verdict

**VALIDATED_CANDIDATE — bounded implementation review only.** This review applies to candidate `563f1e99c96188381f590803f36faa57b543cef3` against base `82623d222b12f9134529791d63e804175a95e77a`, reviewed tree `f3c850e0090cb77c407635e6346cdbb9d10a4993`. It does not establish full CORE-F acceptance or authorize Core canonical promotion.

## Independent review

The independent reviewer confirmed that Core genesis derives the selected actor-turn roster from the ordered NPC owner graph. The selected Unity projection is validated position-by-position against RuntimeId and exact PersonId object identity, so it cannot reorder persisted turn ordinals. Reordering or substituting an identity is rejected. The existing legacy null-hole behavior remains covered by `SelectedRosterPreservesNullHoleOrdinalAndCoreOwnsTurnPrecedence`.

The test-helper follow-up at `563f1e99` changes `CreateSelectedGenesis()` to validate the roster already bound by Core genesis. This preserves meaningful order and reference-identity assertions; it does not weaken them. Restore behavior and P12 codecs were not changed.

## Validation

- Focused Core suites: 42/42 PASS in the independent review run.
- Official CoreValidation: cold/warm 668/668 PASS on the exact source tree; the private run manifest is retained locally under the protected evidence root for run `5d09cf5511e048f6a458d7141a3ed49e`.
- Unity Bootstrap Composition: 39/39 PASS; Runtime Admission: 220/220 PASS. The reviewed follow-up changes only the portable Core test helper; Unity source and test inputs are unchanged from these exact-source runs.
- `git diff --check`: PASS.
- Independent exact-tip verdict: PASS; no actionable findings.

## Scope limits

This checkpoint advances initial roster ordering authority to Core for the selected genesis path. It does not complete the shared `SimulationRuntime`, `DailyV1Application.TryStart`, detached P12 restoration, the fixed .NET host, separate-process T/T+1/T+2 continuation, or CORE-F. Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
