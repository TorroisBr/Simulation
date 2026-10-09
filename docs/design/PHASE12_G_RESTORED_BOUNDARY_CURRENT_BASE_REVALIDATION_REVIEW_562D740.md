# P12-G restored-boundary current-base revalidation — independent documentation review

**Verdict: PASS — source/design revalidation only**

**P12 canonical base/current tip:** `678b01dc9c9dddf05cbd0a64033afc7b1ed1615b`

**Architecture canonical:** `47eff220c7ce00f6e7c759bdc2b76780bb46f628`

**Reviewed candidate:** `562d7404f346867d793e1ec6ecaa3aca640f8607`

**Candidate Git tree:** `f59ef54de5709a2346473e2bf66fbb5863b7d66a`

**Reviewed path:** `docs/design/PHASE12_G_RESTORED_BOUNDARY_CURRENT_BASE_REVALIDATION_678B01D.md`

**Reviewed blob:** `1c9fde2a5e2c891161fce84693dc8287543ea185`

The review compared the candidate document with the exact production-source diff from `02009f9063dd252bd4b177fd6aef1e74dcd947f5` to current P12 canonical, the current Phase 12 State and Brief, Architecture General canonical, and the previously reviewed restored-boundary/publication seam at `65a16e0cae85aa0b8fbc29bd596b05e0f06c07df` (reviewed tree `518abf72b26476ad01cf15f84fd2dce4f1e4a98b`).

The only production Scripts change in that interval is the already-promoted City-bound Person materialization notification in `SimulationRuntime`: after successful materialization into a nonnull starting City, it calls `censusScope.MarkCityPresenceChanged(startingCity)`. This remains within `runtime.npc-membership`; the exact City, ImportantNpcs cardinality/revision, and shared epoch are included in the promoted witness. It adds no restore-specific authority or publication behavior, so the reviewed admission contract remains compatible.

The corrected document consistently describes the restore API and its checks as a proposed design requirement. It does not present the seam as implemented, does not link to an absent validation artifact, and explicitly classifies itself as source/design revalidation rather than implementation validation. Its limitations are accurate: no envelope parser, whole-graph assembler, publication owner/swap, export, hydration, or gameplay semantics are delivered; P12-G and P12-A remain `WAIT_DEPENDENCY`, P13 remains `BLOCKED`, and Phase 12 remains `OPEN`. It claims no new readiness or expansion of P12-B scope.

`git diff --check` for the candidate against the canonical base is clean. No tests were run for this documentation-only review; this review record does not imply implementation or canonical promotion.
