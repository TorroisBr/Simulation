# P12-G current-source ledger refresh — independent documentation review

**Result:** `PASS` — the two-document refresh is source-supported, accurately bounded, and docs-only.

**P12 canonical base/current tip:** `a88cc7386fa89aa1354dea2a160a4705092657fe`

**Architecture canonical:** `47eff220c7ce00f6e7c759bdc2b76780bb46f628`

**Candidate:** `a7810cd149abbc1bcdadca9073db9001162431ad`

**Candidate Git tree:** `f223eaedcb93262ca663bab1947d13d4c8b845ea`

**Reviewer:** `actor_choice_impl_review`, independent source/documentation review.

## Findings

The exact-base diff changes only `docs/PHASE12_STATE.md` and adds `docs/design/PHASE12_G_CURRENT_SOURCE_LEDGER_REFRESH_A88CC73.md`. `git diff --check a88cc73..a7810cd` passes. No Unity validation is applicable to this documentation-only change.

The Crime/Social reconciliation matches the retained source crosswalk and independent reviews. The selected authored Steal path enters through `SimulationRuntime.TryAdvanceDay` and remains under `runtime.advance-day`; `TheftAcceptance` and `KnowledgeAndAppraisal` are internal composite contexts, not IDs in the 24-operation matrix. The Daily-v1 runtime-ingress test demonstrates the selected action path and changed the three Crime/Social owner rows. The later exact-epoch test probes immediately around `TryAcceptTheftOutcome` while the outer day operation is active and asserts a composite delta of exactly `+1`. The cited reviews bound these findings to that one selected normal ingress; they explicitly do not claim all direct calls, all owner writers, total day epoch delta, or runtime-wide quiescence. Current source retains the same Crime/Social integration and test path.

The Expedition disposition is supported by the selected-profile reconciliation: `Simulation-DailyV1.asset` has no authored P10 Ruin site; the runtime constructor has an optional `AdventureExpeditionAutonomySystem` that the selected bootstrap does not supply; and daily autonomy calls are null-guarded. `TesteSimulacao.TryStartExpedition` remains a public facade and requires an installed target site. The refresh preserves Expedition as a Required P12-F owner and calls out the direct-call/source-surface caveat. It neither infers a permanent zero rule nor claims arbitrary direct calls impossible.

The eight base ArmedForce, manpower, Conflict, War, and Battle owner sections remain Required; P16/P17 proving operations are profile-gated. The refresh retains the documented distinction between no normal configured Daily-v1 mutator caller and possible public/direct in-process reachability.

The operation matrix lists 24 IDs, with `runtime.person.parentage` as the final addition. The current 299-row census equation is `61 + 22N + (N-M) + P + 4C`, yielding 299 for the documented authored fixture. The reconciliation correctly supersedes historical 23-operation/278-row wording while retaining the caveat that the matrix's old closing count sentence is stale; it does not silently mix the historical formula into the current 24-ID reference.

The promoted restored-boundary admission record is present in P12 canonical. The active-session promotion is recorded in P12 State at the base, with implementation `c5f80a1`, validation `1c4ff4d`, and independent review commit `4441c25aef3da57583c2cbd870bd4c659fe81855` on `codex/phase12/P12GRestoredBoundaryAdmissionReview`. The candidate accurately limits both promotions to prerequisites and does not claim a restore coordinator or whole-graph atomicity.

The candidate preserves readiness accurately: P12-B through P12-F are promoted only within recorded scopes; P12-G and P12-A remain `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains `OPEN`. It retains the live owner/cardinality and transition census, integrated target-owner checks, and whole-graph validation/rejection, failure atomicity, no-replay, and continuation-parity obligations. No unsupported closure, implementation-readiness, P12-A export/hydration, gameplay, or downstream readiness claim was found.

## Limitations

This is an independent docs/source reconciliation, not a new code audit of every owner writer or a certification of exhaustive live-graph coverage. It does not change the existing P12-G gates or promote any capability. No Unity tests were run.
