# P12-G failed-restore gameplay callback validation

## Candidate and scope

- Canonical base: `53319ba027374db40441c9c173706fae4fc1c70f`.
- Test implementation commit: `b59d78e37d3f8727173a9792668dac5823352c94`.
- Git tree: `3d260381232fc81f2119084e84aea6a50208e1db`.
- Assets tree: `0e80fa741fa52a700daa5fac4f1d50d506710c91`.
- Changed implementation path: `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` only.

The existing parameterized in-flight ActorChoice F-hydrator rejection case now wraps the restore attempt in the existing `DailyGameplayExecutionProbeForTest`. It asserts that `AdvanceDayAfterClockAdvanceInvocationCount` remains zero when restore rejects either a Pending input or a `ConsumedAwaitingTerminalAttempt` input with its valid `DispatchStarted` disposition prefix. The existing staged-rejection assertions still prove rejection before F ActorChoice staging/publication and preserve source session/token, health, facts, roots, retry, and subsequent-boundary parity. No production behavior changed.

This is a direct gameplay-callback non-invocation witness for these two named failed-restore cases. It does not establish runtime-wide quiescence, all failed restore paths, P12-G completion, P12-A readiness, capture eligibility, export/hydration, P13 readiness, or Phase closure. The consumed detached-snapshot case remains a robustness witness; valid P12-F source capture rejects that state.

## Validation

Unity Editor: `6000.3.9f1` (`7a9955a4f2fa`). The test source and Assets tree match the committed candidate. The worktree also contained pre-existing, uncommitted `ProjectSettings/EditorBuildSettings.asset` and `ProjectSettings/ShaderGraphSettings.asset` edits; they were not modified or staged by this change. All reports have zero failures, skips, and inconclusive tests.

| Gate | Result | XML | XML SHA-256 | Compressed log | Compressed SHA-256 | Raw log SHA-256 |
|---|---:|---|---|---|---|---|
| In-flight failed-restore cases | 2/2 PASS | [`XML`](results/focused/EditMode-20261011-015518-05cb5946bc9742c2b881403cccf13240.xml) | `79743081C9547DD24C2B679F79B6E4B8610240B96EF99ED6D162F4346BA7A9E9` | [`log.gz`](results/focused/EditMode-20261011-015518-05cb5946bc9742c2b881403cccf13240.log.gz) | `2F8440145B872C9F9E5683A1884F52E908718A86505BD184CC67674CAAA9D505` | `DEC48F45047A7A17EE8E5C9F6A89BAFB8264C76C638582EF77BDA0D49DF72252` |
| `SimulationRuntimeAdmissionTests` | 188/188 PASS | [`XML`](results/admission/EditMode-20261011-015535-75e63a8901e9441db5ab926effc774b3.xml) | `DC8D53350EB66CA432E81D41A2C75141C76CD2C658E642740710946A5F963E9D` | [`log.gz`](results/admission/EditMode-20261011-015535-75e63a8901e9441db5ab926effc774b3.log.gz) | `09A3A4A97F707F13862237EB40F2161702873AADA654E7341872AB17335ADE0B` | `4EA0C9C28D368C8F001937DF68E4A17ED5511C5251C2EDF157B73976ADFE47A3` |
| ALL EditMode | 2862/2862 PASS | [`XML`](results/all-editmode/EditMode-20261011-015609-015cb40da01e42dab7b9a8dedbef62f2.xml) | `5CCC97CF835FF6440746C6C748B41114BBF552F7E746A81B0DA10D726ACABD91` | [`log.gz`](results/all-editmode/EditMode-20261011-015609-015cb40da01e42dab7b9a8dedbef62f2.log.gz) | `BEBD0FF86CD3C30B2D19E4F9E8DAEB013236607F1CDC2FF0ADB0A42E87DDE6E6` | `13AEC77BADCD3E5BD2CB937318CF6FB1823F25DB3213C0764B41F6CC53D86866` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 PASS | [`XML`](results/smoke/EditMode-20261011-015706-34fe79dbd02f462c8d7c1772267d9a21.xml) | `005B2E52761C2CA5FA7E6DE44B15B90CA7890BE0E530C49B07B8667305453D7B` | [`log.gz`](results/smoke/EditMode-20261011-015706-34fe79dbd02f462c8d7c1772267d9a21.log.gz) | `2E917A432CF009FD794CDED7F38884457785959F59C6E59544CB40253D39767A` | `3CA5A521774DDB937F121A658949906BF6CFFBD79DE1A355E219BC994624D5F5` |

`git diff --check` passed for the candidate. Raw logs were hashed before compression; the committed evidence retains each XML report and compressed log, with raw hashes recorded above. The raw `.log` files remain local and were not staged.
