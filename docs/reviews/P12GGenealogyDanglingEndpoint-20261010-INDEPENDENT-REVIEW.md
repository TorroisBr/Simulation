# Independent exact-tip review — P12-G dangling Genealogy endpoint

- Verdict: **VALIDATED_CANDIDATE**
- Candidate branch: `codex/phase12/P12GGenealogyDanglingEndpoint`
- Candidate tip: `f45ff2c95b1f822596b74788bd9f4b840ba1fe49`
- Base / canonical at review: `8ff0020c73218c28eaca3aca29deb3b52d6508a9`
- Code commit: `c38c8565b914608864628dc2811ac6b17235877d`
- Candidate code tree: `97bb2d55afb39b912df33d911a0c52f2ce61e573`
- Validated Assets tree: `121c30d1316cd8b5abcea82315e437fe1d10acdc`

## Review findings

The remote candidate branch resolves exactly to the supplied tip and is a clean two-commit descendant of the supplied base. Its only code change is 47 added test lines in `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs`; the other changed paths are the validation manifest and its focused, ALL EditMode, and official Smoke XML/compressed-log artifacts. No production code, ProjectSettings, or .meta files changed.

The new `genealogy-dangling-person-endpoint` parameterized case registers the same existing Person in source and uninterrupted control before the completed boundary, then corrupts only the private restored candidate. It confirms the endpoint exists in the candidate PersonStore, the absent endpoint does not, and `TryAddParentage(existing, absent)` creates exactly one edge. The integrated restore rejects with `BindingValidationFailed` and the exact unresolved-Genealogy-endpoint diagnostic. The shared harness confirms the active source session, completed token, owner-thread health and authoritative graph remain unchanged, compares subsequent source continuation with the uninterrupted control, then proves a valid restore retry succeeds. No P12-G semantics or production behavior is changed.

## Validation evidence

The exact candidate manifest is `docs/validation/P12GGraphRejectionCoverage/GenealogyDangling-20261010/P12GGenealogyDanglingEndpoint-VALIDATION.md`. It binds code commit `c38c8565b914608864628dc2811ac6b17235877d` and Assets tree `121c30d1316cd8b5abcea82315e437fe1d10acdc`.

| Gate | Manifest result | XML SHA-256 | Raw log SHA-256 | Compressed log SHA-256 |
|---|---:|---|---|---|
| Focused rejection suite | 19/19, 0 failed/skipped/inconclusive | `F58C7C693A93CEDB9B0857D85179970219E1CB72C44E8ABEE170EF280BE1AD25` | `20125203D41B0387E0B356003DB99F690083AC5E7B53F7D9D35A8AB43E800BC2` | `0C0A439C1BAEBBB5954ABA33F32731676B503FCF1BB00012F9359F91F451FE96` |
| ALL EditMode | 2804/2804, 0 failed/skipped/inconclusive | `E1DE27A6284EBA198F5EDA0093D09EC6F53661FA554FC2C64D61F0D67AF28D03` | `FB73FA448E5D245B31528BAEB19FF7FC96D0B28D914EE818190BB91D0438EA83` | `E034F0EC485027CAF5E5992837B587A09F285612E99E039BBE779495D2E105C4` |
| Official Smoke | 5/5, 0 failed/skipped/inconclusive | `E1450EF8BE1F26813EE07A52BD7E369A1FFB4BAB0C4186581A104E2FCEC373CB` | `0C2F1741ED9546BDB8BE6CB0FA3F479A110711C15991F44109C25C7823B3F8BC` | `3ED04E66BE79ADEDC9ECC4A2A675F8533E5F28E06011A2ED7EBD09B08E0FFFBE` |
| `git diff --check` | PASS (`8ff0020..c38c856`) | — | — | — |

The full focused XML was inspected: it reports 19/19, zero failed/skipped/inconclusive, and the new parameterized case result is Passed. The Smoke XML header reports 5/5, zero failures/skips. The connector returned no content for the large ALL EditMode XML range, so that run's outcome and artifact hashes remain manifest-reported evidence rather than independently recomputed here. The manifest records unchanged pre/post hashes for user-edited `ProjectSettings/EditorBuildSettings.asset` and `ProjectSettings/ShaderGraphSettings.asset`, and says unrelated untracked .meta files/historical validation outputs were left untouched.

## Limits

This closes only the dangling Genealogy Person-endpoint membership case. Broader graph/relation corruption, failure injection, no-replay, and continuation parity obligations remain open. No P12-G, P12-A, or P13 readiness or phase closure is implied.
