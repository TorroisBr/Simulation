# Incremental Core/P12 epoch-owner validation — 2026-10-22

This record covers one additive implementation change on top of candidate
`e65b8803d62f7cf66b42c5c94562841affd42093` (tree
`884c808bef704613f31a706d0ff5217a6f93f002`). It is an in-progress
integration checkpoint, not a completed CORE-F package or canonical
promotion.

## Change

`DailyV1ExecutionBoundaryState` now owns the selected runtime's
`DailyV1MutationEpochState`. A `ContinuationCensusProtocol` attached to that
runtime uses the same object; standalone census protocols retain their
independent epoch state. The Unity integration test asserts exact reference
identity while retaining the existing operation-registry behavior.

## Exact-source validation

The official Unity EditMode runner executed
`ContinuationCensusProtocolTests`: **26/26 passed**, with zero failed,
skipped, or inconclusive tests. The protected run manifest reports one
attempt, stable before/after inputs, and input digest
`77d25559b4631e0de49dd8756003a593589594471beb1176cbb342371ccbb44e`
over 1,846 files. Its private run ID is
`46b0b6804a7f468a95d75edd092deabf`.

The matching source hashes in that input snapshot are:

| File | SHA-256 |
| --- | --- |
| `Packages/com.simulation.core/Runtime/Application/DailyV1ExecutionBoundaryState.cs` | `3259eaaae85d7cb11b2653c948d339c677321b1eaf4494ddfb59bdd4188caf9f` |
| `Assets/_Project/Scripts/ContinuationCensusProtocol.cs` | `bc1c0d14d76a131a24c1b7412f3db1d910cc77975a84c2b0e5bc4d8048d676f4` |
| `Assets/_Project/Tests/EditMode/Editor/ContinuationCensusProtocolTests.cs` | `15f6f2deeffdd93f630a357de302e5c2049ddaf189cc837c28e1f96615871118` |

The protected original XML SHA-256 is
`ac86379ce872b534ccce21b1120ad7889565e24da2aa47375eb3cda3e9331e76`;
the protected original log SHA-256 is
`9cf0d28b8963c89155cbd3ca035320cbf20e80f9a9088b8a14e8976ebd7aabb3`.
The original XML and log remain in private validation storage and are not
published. A preceding invocation with a .NET-style filter selected zero
tests and is excluded from this result; the class-name-filtered run above is
the only Unity result claimed here.

`git diff --check` passed for the three implementation/test files.

## Supplementary CoreValidation run

The official CoreValidation runner reported cold/warm **640/640** for run
`e4f9d92ded7a4b05983fdd11d4423776`. Its protected manifest is retained. Its
explicit input list does not include the changed linked Core source file, so
this result is supplementary and is not used as exact-source validation for
this change. The Unity result above binds the changed Core source, Unity
adapter, and integration test hashes. Full package CoreValidation and source
inventory evidence remain required at the integration gate.

## Scope boundary

This change does not implement the shared application/session, move P12
capture or detached restoration, add an executable .NET host, or establish
T/T+1/T+2 parity. CORE-F remains open and this checkpoint is not promoted.
