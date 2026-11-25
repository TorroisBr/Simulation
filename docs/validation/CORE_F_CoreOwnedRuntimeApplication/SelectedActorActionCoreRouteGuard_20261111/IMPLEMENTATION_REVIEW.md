# CORE-F guarded selected actor-action routing — independent review

Result: **PASS — no actionable findings.**

## Identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`.
- Exact implementation commit reviewed: `279375c80b76a8cee2ef121c206a3bb146d1e332`.
- Exact implementation tree reviewed: `72d56fc181306696f720b44317e41b11fdf0b410`.
- Previous implementation commit: `c8b8420816daa2c091ea2b4259e215de4704d535`.
- Validation/evidence parent at full-run capture: `ec900225cc2e713a16117f5a17ec67ff882d6b5d`.
- Cumulative code base reviewed: `aef01355dc72ecbf409d19639c80d0ddae715f37`.
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- The cumulative executable delta since the code base is limited to `SimulationRuntime.cs` and `SimulationRuntimeAdmissionTests.cs` across the two implementation commits. The remote candidate matches the reviewed branch history.

## Findings

The selected Unity actor-turn adapter opts into `IDailyV1SelectedActorActionExecutionBoundary`. Core's selected stage checks each exact actor and uses the existing Core decision and action-dispatch pipeline. Unity remains responsible for action projection and effect/admission ports.

The selected adapter's `EvaluateAndExecuteAction` now validates the exact actor and throws before calling Unity action selection or execution. Its regression test asserts the failure and unchanged action-slot revision. Core roots provide the selected pipeline's random source, and the stage checks root identity against the admitted execution state.

The cumulative change leaves the mixed-roster whole-stage fallback, original roster ordinals, null-owner compatibility entries, and P12's 89-family census unchanged.

No actionable correctness, identity, RNG, P12, mixed-roster, or test-integrity finding remains.

## Validation applicability

The guard-specific validation and normalized report package are recorded in `VALIDATION.md`. The reviewer checked the run manifest, source hashes against the exact implementation files, stable input digest, and archive hash/size. The earlier c8 evidence package covers the preceding opt-in checkpoint; the separate guard package covers this exact guard and test. The review did not edit the candidate or rerun validation.

This review accepts only the selected actor-action routing/guard checkpoint. It does not complete CORE-F, establish the public `DailyV1Application.TryStart`, prove detached .NET continuation, or authorize Core canonical promotion.
