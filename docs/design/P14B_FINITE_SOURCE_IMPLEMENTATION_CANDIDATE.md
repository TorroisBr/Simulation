# P14-B finite-source implementation candidate

**Status:** bounded core implementation candidate; focused Unity validation and serialized admission integration remain pending.

## Ancestry and base-drift classification

- Implementation worktree base: P12 canonical `a6572ab3d4330d81edb334ae8b4c84ca5e6b173e`.
- P14-A canonical: `4caecbbfb0464c965811402b3c11d8717605114a`; verified as an ancestor of the implementation base.
- Reviewed P14-B design: `95f2327667010edef79a3dcd21b31e8b7e5b16ab`.
- P14-B review and master handoff: `fed04f061485af5305ff32ab71b510928b757b12`.
- The move from the P14-A tip to current P12 canonical is **`BASE_DRIFT_ONLY`** for the P14-A contract: P12 canonical already contains P14-A and adds the Market revision, prepared install, read-only row view, mutation admission, and post-commit notification hooks. No P14-A patch needed replay. This implementation adds the reviewed finite-reserve behavior as a semantic P14-B change and reuses those P12 Market hooks without adding duplicate revision/admission APIs.

## Implemented scope

- Explicit `ExogenousDaily` and `FiniteReserveDaily` profile selection; the default remains P14-A exogenous behavior and does not create a finite source owner.
- One finite source owner retains authored source identity, title settlement, item/store links, content revision, configured daily limit, initial/current reserve, source revision, and last applied boundary.
- A finite production operation prevalidates identities, revisions, row cardinality, full stock capacity, mutation admission, reserve bounds, and day progression. It prepares a cloned Market replacement, installs equal reserve debit and stock credit synchronously, then notifies the P12 Market owner hook after both roots are installed.
- City daily-flow dispatch uses the finite operation before the existing free-consumption sink. P14-A continues through its existing `AddStock` path.
- Focused EditMode coverage was added for capped output/exhaustion, stock and revision overflow, duplicate rows, stale revisions, identity mismatch, P12 mutation rejection, notification ordering, same-day duplicate rejection, P14-A owner exclusion, and City flow balance.

## Deferred integration and validation

- `TesteSimulacao`/P12 pre-construction finite-profile rejection remains excluded until the serialized admission window is released. The P12 selected daily profile must continue to reject this owner before construction until its owner inventory/save profile is expanded.
- `SimulationRuntime` and P16 integration files were not edited. No P12 Save/export or P13 history support is claimed.
- `git diff --check` passes. Unity EditMode execution was deferred to avoid running Unity concurrently with the active P16 worktree. A `dotnet build Assembly-CSharp.csproj --no-restore` attempt could not run because the generated Unity project file is not present in this fresh worktree.
