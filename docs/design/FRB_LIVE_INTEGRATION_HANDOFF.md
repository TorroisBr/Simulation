# FR-B Live Runtime Integration Handoff

## Handoff state

`INTEGRATION_HANDOFF_READY`

This is a capability candidate for Phase Master review and integration. It does not advance or modify a numbered Phase canonical ref. Phase Master retains canonical advancement authority.

## Identity and source refs

- Candidate branch: `codex/frb/frb-live-integration`
- Reviewed code commit: `5c43733088bfbe860183f849d16295b542c1f465`
- Reviewed code tree: `d7d463960756e313bdc3800a20ecc976b659f2c5`
- Candidate's previous implementation commit: `96d675c623648ef6dd89be18e97fa3311c005e57`
- Exact Phase 12 canonical base at refresh: `1ac675cc558aa919a749167647c10506c11303fc`
- Architecture authority: `codex/architecture/world-identity-projection` at `451340c56e9b676bf6ea43412bcb856b9ccde3de`
- Independent exact-tip review: **PASS**, performed read-only against the reviewed code commit and tree in `.worktrees/frb-live-review-fix1`; no blocking issues were reported and the reviewer ran no tests.

The reviewed code commit descends directly from the refreshed Phase 12 canonical base. The remote-tracking canonical and architecture refs still resolved to the SHAs above after `git fetch origin`. Recheck the canonical ref immediately before integration. Integration is a fast-forward from the listed base if that ref has not moved.

## Implemented boundary

The runtime now exposes a non-null `FactualReadCoordinator` for every composed runtime. `UnityBootstrapDailyV1` binds admission to the exact composed `FactionStore` and `PersonStore`. The surface stays unbound on legacy, unselected, and P18 runtimes; capture attempts there return `Unavailable`.

On the selected daily profile, draft reads fail closed until successful world publication completes and the bootstrap operation scope closes. Admitted reads require the captured owner thread, a healthy and idle runtime, and matching logical day plus `FactionStore` and `PersonStore` revisions around the capture. The P12 mutation epoch is checked only as a health signal; it is not treated as a whole-world coherence boundary. Store mutation entry points remain guarded during the read lease.

The reader set is intentionally empty. A successful capture on the selected profile can report the bounded boundary and store revisions, while requested capabilities remain `Unsupported` until a reader is separately implemented. This candidate does not add FR-C, faction facts, or an exporter.

Changed files relative to the Phase 12 canonical base:

- `Assets/_Project/Scripts/FactualRead/FactualReadAdmission.cs`
- `Assets/_Project/Scripts/SimulationRuntime.cs`
- `Assets/_Project/Scripts/SimulationBootstrapComposition.cs`
- `Assets/_Project/Scripts/TesteSimulacao.cs`
- `Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs`

The final review specifically checked the previously identified unsupported-profile status gap. The fix creates an unbound coordinator for unsupported profiles and adds a regression test requiring an `Unavailable` result.

## Validation and artifacts

All runs below completed on the final reviewed code content before its code commit was recorded. Each test run passed with zero failures and zero skips. `git diff 1ac675cc558aa919a749167647c10506c11303fc 5c43733088bfbe860183f849d16295b542c1f465 --check` also passed.

| Gate | Result | Artifact stem | XML SHA-256 | Log SHA-256 |
|---|---:|---|---|---|
| `UnsupportedRuntimeProfileExposesUnavailableFactualReadSurface` | 1/1 | `EditMode-20261002-211138-82d57e2f45f04461a0cf11aa6e2afb1e` | `195CE0718EC862F2EC095ABA894C17F3AC566D6751FF7044D297853E5AC2C584` | `62464707AFE419AC224AB9161132AA5E3C476F5ED1E09DABBD55D3276823E712` |
| `SelectedDailyProfilePublishesCoherentFactualReadOnlyAfterBootstrapCloses` | 1/1 | `EditMode-20261002-211211-144c32612d094ca3a049b0041c28c1d6` | `B455FC251D07E72CF38E8E4E8DF0B32F92DE6F0F1E158C43785661E82979B073` | `FEC06D8F039EC2274A8AA69A1A4429A87A63A963393E986183113BE147261171` |
| All EditMode tests | 2,176/2,176 | `EditMode-20261002-211229-53cfc0bc18974e28b5c8c903a55d2b75` | `8E9A8C4EC6E21E424FD0BAB941F69D96D64483BA087E9E5BF7BBAD7E52523B26` | `1FA1E0832D6C126EB60861342621C2EC80C53C18AD13693EE7A7B6848D4EF0D6` |
| Smoke | 5/5 | `EditMode-20261002-211314-ef664d9872ca44d392336b926c349795` | `6345CCB4714E6EB29A13F40713CDB54B3B9FDC0EC3387A7562BF7348B6967D82` | `560736BB31AAFFE101C3C45FEAFD678ADF45D7A2EE6BA3330163E6BE4B8B9AE0` |

Artifacts are retained in the candidate worktree at `Library/ValidationResults/FRB-Live-Integration-20261002-Fix1`. The directory is local validation output and is ignored by Git.

## WI/FR dependency state

- **WI-A — current/promoted.** The P12 canonical includes the WI-A runtime integration (`242ae6bf81c2f4da832be1e7948bbaac004a620b`) and its promotion state at the canonical base above.
- **FR-B foundation — promoted.** The core read contracts and coordinator (`0ad19ecd9633e01d358a9ff826ca3ca5f8e3627c`, tree `09c3243ccb5f21a559dbcba3fa1dc6c4050ff516`) are already on P12 canonical, with the promotion records present there. This handoff supplies the live runtime binding for that core.
- **FR-B live integration — ready for Phase Master.** The reviewed candidate above is based on the current Phase 12 canonical tip.
- **FR-C faction reader — waiting.** Start after Phase Master integrates FR-B; it is not included in this candidate.
- **WX-D producer — blocked by dependencies.** It requires WI-A, FR-C, and the external collection-coverage contract. The external repository was not modified.

The dependency-respecting next action is Phase Master integration of this FR-B candidate. After that promotion, FR-C is the next executable FR node. WX-D remains gated on the external collection-coverage contract. The worktree audit found no uncommitted edits to the runtime, composition, bootstrap-publication, or factual-read hotspots at the time of handoff.

## Canonical ownership

Do not move `codex/phase12/canonical` or any numbered Phase canonical ref from this candidate. The capability ref remains the reviewed code commit. The companion `codex/frb/frb-live-integration-review` ref carries this durable handoff and review record.
