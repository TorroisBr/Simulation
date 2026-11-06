# CORE-F joined initial-start transaction — independent exact-tip review

## Verdict

`VALIDATED_CANDIDATE` — no actionable findings remain for this bounded
checkpoint. This review does not promote Core canonical or claim CORE-F
completion.

## Reviewed identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Candidate commit: `6a50adcb188e28af020590b98238e61bb45d9f04`
- Candidate tree: `9ae264536b96673ae9bf813d325b717abf1bd642`
- Base: `d2205c4ae53b7817dcd3ea9816135f0a1f9ba2df`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Remote candidate and canonical refs were queried directly and matched the SHAs above.
- Independent reviewer: `/root/review_p8_p11_restore_gate`

The committed candidate delta contains exactly two implementation/test files
and three validation-record/package files. It contains no ProjectSettings,
private evidence, metadata, or unrelated user files. The candidate worktree's
uncommitted ProjectSettings edit and untracked private/user data remained
untouched and excluded.

## Code and contract review

On the normal selected-profile `Start` path, the adapter first captures its
one-time bootstrap context, then invokes `TryPrepareStartOnExistingApplication`
on the existing `dailyV1Application`. Before this call, the compatibility
adapter verifies the selected profile, P10 exclusions, P14 source admission,
effective definitions, and binding closure. The start preparation retains
reference identity for the existing application, exact admission token,
effective definitions, genesis composition, and Core session.

For `NoSave`, Core prepares the candidate while leaving `ActiveSession` empty.
The Unity adapter projects that exact Core composition and publishes through
`TryPublishPreparedInitialWithHostProjection`; the gate verifies the admitted
NoSave result, exact application/preparation/session identities, bootstrap
context, and unused session gate before binding the projection and publishing.
Any failed admission, preparation, projection, or publication fails closed;
there is no fallback genesis after a present or rejected slot.

For `RestoreRequired`, the adapter retains the admitted image and preparation,
uses the existing P12-G detached restore coordinator, and commits through the
initial restored-candidate gate with the exact admission/image pair. That path
does not run genesis and does not allocate new genesis identities. The
implementation preserves the established projection-before-publication
ordering and leaves a failed private candidate unpublished.

The new NoSave regression checks that the same application admitted the slot
and prepared the exact definitions/session, that the candidate remains
unpublished before Unity projection, and that the exact Core session is
published afterward. The persisted-image regression now exercises the joined
start path and retains its identity-allocation and continuation-parity
assertions. Existing corrupt/unavailable-slot fail-closed tests remain in the
full suite. No assertion weakening or unrelated production behavior change was
found.

The legacy admission-only test seam remains separate; the candidate does not
claim to implement the final public `DailyV1Application.TryStart` surface.
Scope remains limited to this Unity initial-start transaction. Descriptor-v2
end-to-end integration, a detached .NET host, full CORE-F acceptance, and phase
closure remain unclaimed and outstanding.

## Validation and package evidence reviewed

The committed validation record is
`docs/validation/CORE_F_ApplicationStartTransaction_20261105/VALIDATION.md`
(SHA-256 `a0b8a636605e45721d25e6728d3d5daee2f13381710cc967453aa3f766844741`).
It records focused restore 1/1, full EditMode 3300/3300, Smoke 5/5,
`git diff --check` PASS, and no fresh CoreValidation claim.

Exact changed source identities match both the committed candidate and the
validation manifest input snapshot:

- `Assets/_Project/Scripts/TesteSimulacao.cs` —
  `a72252a0a70a3d66912bff3d2031ce085d44bf957e60ace93838ff783c809816`
- `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` —
  `84bb5f0de8ddf8308013c6a9cc3cabb2b821ff3d8f0e34d7ef1693b9aff1f1ab`

The validation manifest SHA-256 is
`1efcc361e41096ad25c113f5e1f6ff3aadca83f65995f839f0bef565d8a4f79d`.
The committed ZIP SHA-256 is
`7959e04cb76441af8b97439708a724bb1d7c0538d290c43a11c091349f1faa0c`,
and the normalized XML entry SHA-256 is
`471ff97f9e64e429bdfadc01abdc3bf4158ed0e392c918019a47856075879b50`.
The archive-only inspection confirmed one XML report with 3300 test cases,
3300 passed, and zero failed, skipped, or inconclusive; it includes both the
new NoSave test and `PersistedDailyV1ImageRestoresWithoutUsingSourceSessionAndContinuesIdentically`.
The manifest run ID is `c33e1c1862364b29b8c745870f73d533`.
No local absolute path or credential-pattern matches were found in the
committed record, manifest, or normalized report.

Provenance note: the packager's repository anchor records HEAD
`70290dee1a62b36afc724a2fb266f87f431b0028` and tree
`46cbb0469d6c663f90b563ef6efc29b3e49e18aa`; the implementation/test changes
were present as worktree content during validation and were committed later.
The manifest's per-file hashes for both changed files match the candidate's
committed blobs exactly. Thus this review accepts the Unity evidence for the
exact changed source/test files, not as a claim that the validation worktree's
Git HEAD was the final candidate commit/tree. Original raw XML/logs and the
separate filtered Smoke report remain in owner-only storage and were not
independently inspected or copied; review verification used the committed
normalized archive, manifest, validation record, and candidate source.

## Disposition

No actionable findings remain for this bounded checkpoint. The candidate is a
validated review milestone only. Canonical remains
`a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`; no canonical promotion or
CORE-F/Phase closure is authorized or performed by this record.
