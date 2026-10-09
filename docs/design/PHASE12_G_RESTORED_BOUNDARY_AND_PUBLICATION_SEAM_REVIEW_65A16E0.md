# P12-G restored-boundary and publication seam — independent design review

**Result:** PASS — exact-tip technical-design/documentation review only
**Candidate:** `65a16e0cae85aa0b8fbc29bd596b05e0f06c07df`
**Reviewed tree:** `518abf72b26476ad01cf15f84fd2dce4f1e4a98b`
**P12 canonical base:** `02009f9063dd252bd4b177fd6aef1e74dcd947f5`
**Architecture canonical:** `47eff220c7ce00f6e7c759bdc2b76780bb46f628`
**Review date:** 2026-10-09

The review verified that the protocol now calls
`P12FDailyV1OwnerCapture.TryCapture` with the exact source runtime,
composition, completed-boundary token, and owner vector before C/D/E roots or
any staged domain object are allocated. It retains the detached capture for
later `TryStage`; the capture and staging APIs are distinct, and staging
revalidates the same source token/vector/staging attempt. This allows the
ActorChoice source capture to reject nonzero temporal inputs before the
detached ActorChoice snapshot is constructed. It remains a source-side
zero witness only and does not complete live inventory or admission.

The restored-boundary design matches the current implementation boundary:
the existing token is runtime-bound, the normal publisher runs after a
successful advance, the general evidence path requires factual-read
publication and reads `CurrentDay`, the candidate runtime captures its initial
day at construction, and the census protocol exposes a copied quiescent owner
snapshot. The proposed private Daily-v1 seam carries only the preserved day
and successful-core sequence, creates fresh candidate-bound owner/epoch/token
evidence with explicit `RestoredContinuation` provenance, and does not fake an
advance. This seam is not implemented or ready for integration by this design
review alone.

The active-session source audit also matches code: `TesteSimulacao` keeps
`publishedComposition` and `worldPublished` separately and uses cached runtime
and reporting dependencies; `SimulationBootstrapComposition` does not retain
the logger or Justice system. A single active-session snapshot is therefore
required to avoid stale runtime/report aliases at restore publication.

No conflict was found with the intraday/extensibility or multi-participant
alignment records. Daily-v1 continues to exclude P18 temporal state, P19
modules, and P20 activity payload; future activity identity and participant
cardinality constraints remain intact.

P12-G remains `WAIT_DEPENDENCY` on complete live-profile inventory, package
interface verification, restored-boundary implementation/evidence, coherent
publication, and whole-graph continuation/rejection evidence. P12-A remains
`WAIT_DEPENDENCY`, P13 remains `BLOCKED`, and Phase 12 remains open. This
documentation-only review does not imply implementation readiness or delivery.
No Unity tests were applicable or run; `git diff --check` passed.
