# P12-B Runtime Admission Adapter — Implementation Candidate

**Status:** Implemented candidate; independent exact-tip implementation review passed. Canonical-promotion approval remains pending. P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`.

## Exact base and reviewed contract

- P12 canonical base: `f538a096bf4b2558566518483bc60f0129718a3b`.
- Accepted and independently reviewed design: `e5a32b8ec226204e751e1da41dfcf2546a760718`, preserved at [`PHASE12_P12B_RUNTIME_QUIESCENCE_DESIGN.md`](PHASE12_P12B_RUNTIME_QUIESCENCE_DESIGN.md). Its historical first review rejection is retained; the subsequent independent PASS is appended at [`PHASE12_RUNTIME_QUIESCENCE_DESIGN_REVIEW.md`](PHASE12_RUNTIME_QUIESCENCE_DESIGN_REVIEW.md).
- Implementation commit: `9d4b035bc484286cfb58d66cca07809844c30254`.
- Implementation tree: `911d7cc9ff4e9c0205ae3305df4757099a461b0a`.
- Implementation branch: `codex/phase12/P12BRuntimeQuiescenceAdapterIntegration`.

The accepted design authorizes this bounded P12 prerequisite adapter after its independent design PASS. This candidate does not change the Phase 12 State or claim checkpoint completion.

## Delivered boundary

The selected `UnityBootstrap-Daily-v1` context now carries the `Thread` reference and managed ID captured at `TesteSimulacao.Start`. The runtime verifies that identity at construction, binds its partial census protocol to it, and rejects a P18 timeline profile combined with the P12 adapter.

The protocol registers the fixed bootstrap-publication and daily-advance operation IDs before sealing its operation inventory. After the existing partial census baseline is established, the selected profile holds the bootstrap scope through validation, publication, stage callbacks, and normal return from the complete genesis pipeline. A selected-profile failure revokes any assigned composition, faults protocol admission, and latches Start against retry. Unselected bootstraps retain their existing retry/publication behavior.

For the selected profile, `SimulationRuntime.TryAdvanceDay` and nonzero `TryAdvanceDays` enter the registered outer operation scope under the existing per-runtime advance lease. A runtime-owned `SimulationTime` dispatches its public direct advance calls through the same full runtime day operation. The internal clock commit avoids recursive dispatch. Zero-day and rejected calls open no scope. Wrong-thread calls do not advance; exceptions from an admitted P12 day/batch fault the partial protocol before the scope closes, so incomplete execution cannot appear census-idle.

The SampleScene explicitly selects the accepted daily profile for its existing `Simulation-GeneralTest.asset` bootstrap. No new generated-world content or game behavior is added.

## Identity, temporal, and activity boundaries

This P12 profile remains daily and excludes P18 intraday state. The adapter does not add timeline facts, alter temporal identity/cardinality, add activity lifecycle records, or change P18 clock ownership. P18 timeline composition is rejected for this P12 context; P18-owned direct clock projection and advance behavior remain on their existing paths.

The adapter accounts only for the named synchronous bootstrap tail and outer daily operations. It does not claim that the earlier authored genesis stages are scoped, that all runtime operations or stores are covered, or that operation counts provide synchronization. The existing advance lease remains per-runtime single-writer/reentrancy protection; this change does not add a general thread lock.

## Validation

All result XML and log files are retained under `Library/ValidationResults/P12BRuntimeQuiescenceAdapter` in this E: worktree. The SHA-256 values below identify the exact artifacts.

| Gate | Result | XML (SHA-256) | Log (SHA-256) |
|---|---:|---|---|
| `SimulationRuntimeAdmissionTests` | 8/8 | `EditMode-20261001-132058-356a7c198c6345e69211f8fba3fa2845.xml` — `6043240be20560ef2954d532cb7d133e684bf4fa20b1e372090c34c65418d000` | `EditMode-20261001-132058-356a7c198c6345e69211f8fba3fa2845.log` — `ce157e6a056c419e165ddd938277907d7d7d3d351d4950405da7078531590578` |
| P18D compatibility suites | 26/26 | `EditMode-20261001-132111-da11c3fdaa5d4590a0f79486d848a82e.xml` — `d3bdcdb0c8c8f28da8c810185feb7c2931d2cecf0a05945a0cdbf513d90860e4` | `EditMode-20261001-132111-da11c3fdaa5d4590a0f79486d848a82e.log` — `bcd150a4c9d67b348e85882a8f2450ec396cca401d54a8619345006bf3cdd173` |
| ALL EditMode | 2107/2107 | `EditMode-20261001-132015-0361628e354745d6bb13cc9fd1a32f18.xml` — `aae4b52ce1f8f58af357530373cdfb059cfb5cd8c64403e82398a11585d097aa` | `EditMode-20261001-132015-0361628e354745d6bb13cc9fd1a32f18.log` — `f6e700419ee0e82f638469cec7e93354584b3925e9c351877d5d4a99a37e7456` |
| Official Smoke filter | 5/5 | `EditMode-20261001-132131-67ef32546fca429aab4b10c3ea0c8b57.xml` — `4d4faa31b44d406b62379b7fe01b2340ed9e3640d4c5272d923d3850d11e9f73` | `EditMode-20261001-132131-67ef32546fca429aab4b10c3ea0c8b57.log` — `b567ae1a7a753aac30a752d0bde1b45533552d0ad9f721864969fc518c39d750` |

`git diff --check` passed on the implementation commit. The EditMode run includes the existing daily long-run tests. Focused coverage verifies Start-thread capture, bootstrap scope through full publication, publication revocation/latching after failure, direct clock dispatch and reentrancy, off-thread rejection, interrupted single/batch fault closure, zero/invalid day behavior, and the P18-composition negative path.

## Independent exact-tip implementation review

**Result:** PASS

**Reviewed candidate tip:** `de65ae22f79cddf83769dcedd838f46c109bb210`

**Reviewed implementation commit/tree:** `9d4b035bc484286cfb58d66cca07809844c30254` / `911d7cc9ff4e9c0205ae3305df4757099a461b0a`

**Review base:** P12 canonical `f538a096bf4b2558566518483bc60f0129718a3b`

**Reviewer:** independent Luna implementation review

The exact-tip reviewer confirmed that Start captures both thread identity components only for the explicitly selected profile; the runtime revalidates the identity and rejects P18 composition; the fixed operation registrations precede sealing; and the bootstrap scope begins after the existing partial census baseline and ends after full pipeline return. The reviewer also confirmed the direct-clock dispatcher is selected-profile-only, P18 clock behavior is unchanged, exception paths fault admission, and the documentation preserves P12-B/P12-A limits. No blocking findings were reported. `git diff --check` passed against the exact base and candidate tip. The reviewer independently verified each listed result XML/log SHA-256 and test count.

## Remaining P12 blockers and explicit limits

This is a partial owner-thread/admission capability only. It does not complete the live profile owner/cardinality census, connect supported commits to the shared mutation epoch, prove every owner or operation is covered, provide capture eligibility, or deliver exports/staged hydration. It does not complete P12-B, make P12-A ready, or authorize P12-A implementation. The remaining blocker map is updated in [`PHASE12_B_BLOCKER_RESOLUTION.md`](PHASE12_B_BLOCKER_RESOLUTION.md); its earlier runtime-hotspot hold is superseded only for this adapter slice.
