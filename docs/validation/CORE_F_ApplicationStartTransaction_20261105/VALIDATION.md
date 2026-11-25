# CORE-F joined initial-start transaction — 2026-11-05

## Scope

This candidate connects the selected Unity `Start` path to the existing
`DailyV1Application` instance's one-time initial-slot admission and exact
NoSave preparation. It does not expose the final public `DailyV1Application.TryStart`
API and does not claim the detached .NET host or CORE-F package complete.

On the normal selected-profile `Start` path, Core now receives the already
admitted effective-definition graph and host identity through
`TryPrepareStartOnExistingApplication`. A NoSave result prepares the Core
genesis composition and active-runtime candidate on that same application
before Unity builds its compatibility projection. The candidate remains
private until Unity has projected the exact Core composition and the existing
`TryPublishPreparedInitialWithHostProjection` gate binds and publishes it.
A `RestoreRequired` result retains the existing P12-G detached restore
coordinator and initial restored-candidate commit gate; it does not run
genesis. The legacy test seam remains admission-only so the ordered stage-hook
contract continues to be tested independently.

The change preserves the exact selected profile, definitions and owner
instances; it adds no second runtime, changes no domain operation or P12 wire
semantics, and leaves the failed-slot fail-closed behavior intact. The
explicitly separate `TryStart` API, descriptor-v2 end-to-end integration, and
detached .NET execution remain outstanding CORE-F work.

## Changed source identity

The source files validated and committed in this checkpoint have these SHA-256
identities:

| File | SHA-256 |
|---|---|
| `Assets/_Project/Scripts/TesteSimulacao.cs` | `a72252a0a70a3d66912bff3d2031ce085d44bf957e60ace93838ff783c809816` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` | `84bb5f0de8ddf8308013c6a9cc3cabb2b821ff3d8f0e34d7ef1693b9aff1f1ab` |

The added NoSave regression proves the prepared Core session remains
unpublished until Unity projects it, and that the exact same application,
admission, definitions and Core session are published afterward. The existing
persisted-image continuation regression now exercises the normal joined
start path and verifies restoration without genesis identity allocation,
followed by its existing uninterrupted-versus-restored parity checks. Existing
corrupt and unavailable-slot regressions also pass with their exact diagnostic
messages unchanged.

## Validation

All accepted Unity runs used Unity `6000.3.9f1`, Test Framework `1.6.0`, the
dedicated E: validation worktree, and the official evidence runner. The final
focused restore run, full EditMode run, and Smoke run have identical stable
input digest `f3ba2972d57ce0e30b4f06abfa61a080f5bb277310ed06599ba1763e31724663`.

| Gate | Run | Result |
|---|---|---|
| Joined-start persisted restore focus | `d24f9abd091a471a80f9d29d800b3927` | 1/1 PASS |
| ALL EditMode | `c33e1c1862364b29b8c745870f73d533` | 3300/3300 PASS; zero failed, skipped, or inconclusive |
| Official Smoke filter | `64c174e2d57b405fa6889b9b7f144b58` | 5/5 PASS; same five identities are present in ALL |
| `git diff --check` | final candidate source/documentation | PASS |

The normalized full EditMode report is packaged at
[`Unity/coref-application-start-20261105-all.zip`](Unity/coref-application-start-20261105-all.zip).
The archive SHA-256 is
`7959e04cb76441af8b97439708a724bb1d7c0538d290c43a11c091349f1faa0c`; the
package manifest SHA-256 is
`1efcc361e41096ad25c113f5e1f6ff3aadca83f65995f839f0bef565d8a4f79d`.
The approved packager passed both comparison with the protected original run
and archive-only verification. The ZIP contains one normalized `test-run`
report with all 3300 passing results. Local workstation-path and
credential-pattern scans found no matches in either the archive or public
manifest. Original XML/logs and the separate filtered Smoke report remain in
owner-only E: evidence storage and were not modified or added to Git.

Unity's first import modified two `ProjectSettings` files in the dedicated
validation worktree. Those local changes were preserved and excluded from the
candidate. That first run had an unstable input digest and is not counted; the
table above records only later stable runs. The candidate worktree's
user-owned `ProjectSettings/ProjectSettings.asset`, unrelated untracked files,
and prior protected evidence were left untouched.

CoreValidation was not rerun for this Unity-adapter-only checkpoint; no Core
runtime or Core test source changed from the prior reviewed candidate. This
record does not claim fresh .NET validation for the new Unity call path.

## Status

Validated implementation candidate on
`codex/core/CORE-F-restore-stage-transaction`, based on
`d2205c4ae53b7817dcd3ea9816135f0a1f9ba2df`. Independent exact-tip review is
required before this checkpoint can be treated as accepted. Core canonical
remains unchanged; no canonical promotion or CORE-F closure is claimed.
