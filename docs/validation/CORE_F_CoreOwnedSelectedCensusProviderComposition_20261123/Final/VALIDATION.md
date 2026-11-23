# CORE-F selected owner census provider composition

## Source identity and scope

The implementation under review is commit 92aa9c03dfe61f0af5e3019e7a30906de5a360b8 on branch codex/core/CORE-F-restore-stage-transaction, with tree 5bbf12ec04f8e63ddf806e87911ea084ebc27873.

This slice composes the existing Core PersonStore, City, identity/spatial, and selected NPC census provider factories against one live selected owner graph. Unity's census sealing path consumes that composition and retains its existing host projection checks. Runtime NPC membership transitions continue to build a fresh provider for the changed membership boundary.

This is not the complete selected-profile census. It does not establish 89-family coverage, complete P12 census readiness, detached restoration readiness, a public DailyV1Application.TryStart, or CORE-F completion. Mixed or unselected roster members retain the existing Unity compatibility path.

## Validation

- Official CoreValidation: cold 1194/1194 passed; warm 1194/1194 passed. Run caa00cecae024d9ab698f8b2430f2f0f. The official run records the source commit and tree above. Original reports and logs remain in the private E: validation area and are not included in this public record.
- Focused Core-only actor action regression: 14/14 passed; included in the full CoreValidation run.
- Official Unity EditMode: 3325/3325 passed, with zero failed, skipped, or inconclusive tests. Run a5d9714b10874b7c812fd2561d2268f8.
- Official Smoke: 5/5 passed using the project-prescribed EditMode Smoke filter. Run 6e6ce83464574ca8a13f09443cd15c0f. The full EditMode report also contains each of the five Smoke cases once as passed.
- Focused ContinuationCensusProtocolTests: 32/32 passed in run f8950675c1fe49cd9fdfadf057dce524; the final full EditMode run covers those cases on the committed source.
- git diff --check: passed.

The Unity EditMode report is packaged at Unity/coref-selected-owner-census-composition-editmode-final-20261123.zip. Its SHA-256 is 175fba0ccf63ee07de9710b04c33fa750e9b8ad9be75b90c5849ed67c96e2f34. The manifest binds the report to the source commit/tree and records the original and normalized report hashes. The official package verifier passed for one run and one report.

A separate exploratory PlayMode filter matched zero tests. It is not counted as validation and is retained only in private diagnostics. A filtered Smoke XML was not published because its inherited suite counts are inconsistent with the filtered cases; no original XML was edited. The full unfiltered EditMode package supplies the reviewable report containing the five Smoke cases. CoreValidation reports/logs remain private because the run did not produce the additional source-context required by the TRX evidence packager; no package is claimed for them here.

## Review and promotion boundary

Independent exact-tip implementation review is required against this source commit and the evidence above. This candidate is not promoted to Core canonical. No full CORE-F acceptance or Phase closure is claimed.
