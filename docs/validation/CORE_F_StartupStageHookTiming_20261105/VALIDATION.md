# CORE-F Daily-v1 Startup Stage-Hook Timing

## Scope

This checkpoint resolves the independent review finding against the Daily-v1 start-preparation implementation. It corrects no-save startup so Core genesis is prepared from inside the ordered Unity bootstrap pipeline, where the existing stage-completion callback can be forwarded at the correct point. The exact NoSave admission, effective definitions, logger, application, and unpublished Core session remain bound together. A restored slot remains fail-closed and does not fall through to genesis.

The existing `p9.genesis.validate-profile/v1` callback stays at the Unity validation tail, after the runtime and bootstrap census scope exist. Core-owned content stage callbacks run during Core preparation in their existing order. Publication remains after the publication stage. The regression verifies that callback order equals the admitted definition stage order and that Core preparation is still private during Core-owned stage callbacks.

This is an internal startup integration checkpoint. It does not publish the public `DailyV1Application.TryStart`, establish the detached .NET host, or close CORE-F.

## Source identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Implementation commit: `1b0ac0a2189122895eef4e31149efe12886e0ff0`
- Implementation tree: `065d65019cc60b3b65a38036e7445226b262021a`
- Previous candidate: `be3645f69d14609e690671c6ec512fab19cba11a`
- Core canonical is unchanged: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

Validation ran before the implementation commit, against the exact source bytes later committed at `1b0ac0a`. The official runner manifests bind those source hashes and stable input digests. The candidate commit contains only the three reviewed source/test files. The user's `ProjectSettings/ProjectSettings.asset` modification and existing untracked protected evidence were left untouched and unstaged.

## Validation

| Gate | Run | Result | Stable input digest |
|---|---|---:|---|
| CoreValidation cold and warm | `8e0caf35c8e74e3998b9587d54c6a78b` | 1,014/1,014 each | `2eca8e3b2c94386a362506ed27f39c4284f78bce20497ea17579d1f73d908872` |
| Focused Unity EditMode | `f8dc0fda456743b3b07cd2c509048ec5` | 1/1 | `0bcda3f0ff63d7fc01b7a8207bd0948bacb6c21f29effaf5ffd968c808b7cb11` |
| Full Unity EditMode | `67ceaf5546e948ba95925b26d91a53c1` | 3,299/3,299 | `0bcda3f0ff63d7fc01b7a8207bd0948bacb6c21f29effaf5ffd968c808b7cb11` |
| Official Unity Smoke (`EditMode`, filter `Smoke`) | `811a0ca9efcf4194a0c5de20f81a9805` | 5/5 | `0bcda3f0ff63d7fc01b7a8207bd0948bacb6c21f29effaf5ffd968c808b7cb11` |
| `git diff --check` | after implementation | PASS | n/a |

CoreValidation also completed its locked build, CalendarDemo, and Simulation.Lab cold/warm gates. The checkout contained a Unity `Library`, so this run makes no Library-independent .NET host claim.

The three source/test SHA-256 values recorded by the official Unity manifests, and matching the committed content, are:

- `Assets/_Project/Scripts/TesteSimulacao.cs`: `11b4ba9619c191d26ea539ae4e3cb9f0937b0870b77dda44a13ca40e9a6379d3`
- `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs`: `dbcdd3a997eec65c2ccce2abe0e7c3e5b2a10ce21cc6e7b7e754a8a620f0136c`
- `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs`: `6a389dbebff4c64c62c492cadd578ac1a8c567208a7a78c64afa7e159967d06c`

## Evidence integrity

The complete EditMode XML was packaged and verified by the approved Unity evidence tool. Package manifest SHA-256: `97ee5e3b2c227e61bef7b5e9943074752ac77fc02037fbbb0ce20ce6858f91e2`; archive SHA-256: `f4370825ba93e017c9b19723f578a70f5622bb16366e1eeab9f6cb9a8b7435bd`. Verification returned `VALID` for one run and one result entry. The public manifest contains no absolute workstation or drive paths.

The CoreValidation private manifest and original cold/warm TRX reports remain protected and unchanged; they are not copied into this repository. Their SHA-256 values are respectively `ee8f12095d0a2ca253c78ea39bfb6ef9cb054538689986ae8065057340ef0b18`, `2adbab4167a9e397c604f1572087dd08b1a1da674e901a67c26e1b3e872692ba`, and `90344246053b274a1b0dfc6b2c7964e08262de3c856aa8b721b368e0039d2406`.

Focused and filtered Smoke originals remain in protected local retention. The full EditMode package contains those passing cases; no filtered XML was modified or published. An unrelated PlayMode filter invocation selected zero tests and is excluded from all gate results.
