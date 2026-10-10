# P12-G dangling Genealogy endpoint rejection validation

- Candidate code commit: `c38c8565b914608864628dc2811ac6b17235877d`
- Candidate code tree: `97bb2d55afb39b912df33d911a0c52f2ce61e573`
- Validated `Assets` tree: `121c30d1316cd8b5abcea82315e437fe1d10acdc`
- Base canonical: `8ff0020c73218c28eaca3aca29deb3b52d6508a9`
- Unity Editor: `6000.3.9f1`

## Scope

This test-only slice adds one explicit P12-G §6.3 graph-rejection case. A valid private restored candidate is given one Genealogy edge from an existing staged `PersonId` to an absent `PersonId`. The integrated validator rejects the graph as `BindingValidationFailed` with the expected unresolved-endpoint diagnostic before target-owner capture or publication. The existing corruption harness verifies the source session and completed-boundary token remain valid, its authoritative owner projection is unchanged, deterministic continuation still matches the uninterrupted control, and a later valid restore succeeds.

The candidate changes only `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs`; it changes no production behavior, profile, ProjectSettings, or `.meta` files. This closes only the dangling Genealogy endpoint membership case; broader relation, graph, failure-injection, no-replay, and continuation-parity obligations remain open.

## Validation results

| Gate | Result | XML SHA-256 | Raw log SHA-256 | Compressed log SHA-256 |
|---|---:|---|---|---|
| Focused `DailyV1RestoreRejectsCorruptedRootOrOwnerVectorAtomically` | 19/19; 0 failed, skipped, or inconclusive | `F58C7C693A93CEDB9B0857D85179970219E1CB72C44E8ABEE170EF280BE1AD25` | `20125203D41B0387E0B356003DB99F690083AC5E7B53F7D9D35A8AB43E800BC2` | `0C0A439C1BAEBBB5954ABA33F32731676B503FCF1BB00012F9359F91F451FE96` |
| ALL EditMode | 2804/2804; 0 failed, skipped, or inconclusive | `E1DE27A6284EBA198F5EDA0093D09EC6F53661FA554FC2C64D61F0D67AF28D03` | `FB73FA448E5D245B31528BAEB19FF7FC96D0B28D914EE818190BB91D0438EA83` | `E034F0EC485027CAF5E5992837B587A09F285612E99E039BBE779495D2E105C4` |
| Official Smoke (`-testFilter Smoke`) | 5/5; 0 failed, skipped, or inconclusive | `E1450EF8BE1F26813EE07A52BD7E369A1FFB4BAB0C4186581A104E2FCEC373CB` | `0C2F1741ED9546BDB8BE6CB0FA3F479A110711C15991F44109C25C7823B3F8BC` | `3ED04E66BE79ADEDC9ECC4A2A675F8533E5F28E06011A2ED7EBD09B08E0FFFBE` |
| `git diff --check` | PASS (`8ff0020..c38c856`) | — | — | — |

All XML files report `Passed` with coherent totals. Each compressed log was decompressed and its SHA-256 verified against the original raw log before the raw log was removed from the candidate folder. The focused XML includes the `genealogy-dangling-person-endpoint` parameterized case with result `Passed`.

## Protected local files

The pre-run and post-run SHA-256 values matched for both user-edited settings:

- `ProjectSettings/EditorBuildSettings.asset`: `58BCBFB23DA7AAB5ACD696E7D83E9D75A86442ED39A582159D2493049361BA28`
- `ProjectSettings/ShaderGraphSettings.asset`: `5C432C9C73FDF73FEE91C8823EA02AB98E3B7A8EDF341B5AE6901D699CCE52A7`

The unrelated untracked `.meta` files and historical validation outputs were left untouched.
