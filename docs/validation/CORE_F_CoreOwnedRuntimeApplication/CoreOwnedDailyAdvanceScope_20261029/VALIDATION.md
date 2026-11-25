# Core-owned DailyAdvance protocol scope — validation

Date: 2026-10-29
Candidate base: `8246504413802efe44d6667c41bcd7c71e18e103`
Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

## Scope

The selected Core runtime now opens and closes the registered
`runtime.advance-day` protocol scope against its exact
`DailyV1ExecutionBoundaryState`. Unity no longer receives a callback to begin
that scope. Before Core opens it, the Unity census adapter reports whether its
existing owner-thread protocol is healthy and idle. Core retains responsibility
for registering the scope, closing it, invalidating the capture token, and
faulting the runtime if close fails. P12 owner census, selected action
projections, legacy compatibility, and fault reporting remain host adapters.

Focused tests verify Core scope ownership during selected-stage construction,
normal idempotent close, fail-closed off-thread disposal, and rejection of an
unhealthy P12 protocol before the clock advances. This is a bounded runtime
integration increment. It does not deliver the complete shared runtime,
`DailyV1Application.TryStart`, detached restoration, the fixed .NET host, or
CORE-F acceptance.

## Exact validation inputs

The source and test files were validated in the candidate worktree before their
checkpoint commit. Their SHA-256 values below match the official runner input
inventories; no source or test edits were made after validation.

| File | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs` | `6bd79ef73c2c168945d05ce4b111e2113045aaa5117e4cecfb1e46d2bbcfe2ab` |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `013d17805900e135bd5adac381dad3ffc8dbb6c337775421d5e3727403d994fe` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeOrchestrationTests.cs` | `33ba8d6378a9194d631b8c68ac5b3e901873ff405eacf50a28d521aa0424b6c5` |
| `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs` | `39afa53e6d704ccf40866c450e59fb55940b93033e1926b556f8427e21041050` |

## .NET CoreValidation

- Official runner: `simulation-core-validation/v1`, run `faedd6717a1e4bcf9027c4839390d105`, status `Passed`.
- Cold and warm: **922/922 passed** each; no failed, skipped, inconclusive, or aborted tests.
- Stable input digest: `adc5d34ba32f3af1d64989540a14f8746d3b6682ee502efc495a69a28a9540d7`.
- Original cold/warm TRX SHA-256: `343c617b594c4cfd542d9f68aec60162cdef6ca467a092cfbc2ba75bc6fed47f` and `8a178181a6e0f2dc2a9c703826d172b74a904aa7fa949dddc437ea18ae7ae336`.
- Private runner manifest SHA-256: `ad71e926ac39349fb6d8a31dc4056944c4e35732f06646c0a4328343806f4110`.
- The runner reported that this checkout contains a Unity Library. No Library-independent .NET claim is made. Raw TRX and logs remain in the protected private evidence store; no public .NET TRX projection is claimed.

## Unity validation

- Unity `6000.3.9f1`, Test Framework `1.6.0`, official wrapper and stable input inventory.
- Focused `SimulationRuntimeOrchestrationTests`: **13/13 passed**, run `39ee6d8206c446df94c43f62fcb11b91`.
- Full EditMode: **3279/3279 passed**, zero failed/skipped/inconclusive, run `dfa9196eb68f433b8a0c36d11367914c`; input digest `47b79dfdbcd064bedc513cdbc6933401f51c359d26b2f769a5972764cbefc9f3`.
- Official filtered Smoke: **5/5 passed**, run `2be78efb84de43d995a59df8922514d6`. The full EditMode result also contains the Smoke cases.
- The full EditMode normalized package is included beside this record. Its manifest SHA-256 is `f6a061d0385e702003bdd54a15547b472fc18889e2202c915d64ae08552d7722`; archive SHA-256 is `a82e6009976fa5f5d52ac49d45ca258598ebb540473c11aab08d0ce2592d750f`. The official verifier passed with one run and 3279 result entries.
- The standalone filtered Smoke original remains private; it is not separately normalized because filtered NUnit suite counters do not match their emitted descendants. The complete EditMode package includes and verifies those five cases.

`git diff --check` passed for the implementation and test files. Existing
ProjectSettings edits and pre-existing private/untracked work were left
untouched and excluded from the checkpoint.
