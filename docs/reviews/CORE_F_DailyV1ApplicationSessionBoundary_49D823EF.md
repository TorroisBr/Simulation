# CORE-F DailyV1Application session-boundary review

- Candidate base: `49d823ef81178107795939bdf317a8393ef50c49`.
- Reviewed scope: six implementation/test files listed in the corresponding validation record.
- Working diff SHA-256: `76ba4c88d41adda1b9472c0833a4d3cfb074e7ee326481fe32402efc1a898c07`.
- Review: independent read-only exact-source review, **PASS**.
- Initial finding: the selected simulation loop had replaced `SimulationRuntime`'s typed-failure diagnostic mapping with a generic exception. The loop now calls the bound `DailyV1Application` session through `AdvanceCoreSessionThroughApplication` and maps the returned failure through the existing runtime formatter.
- Follow-up: the reviewer confirmed the focused test prepares, binds and publishes a Core session, forces an application-bound advance rejection, invokes the exact helper used by the selected loop, and checks the preserved diagnostic and reservation cleanup. No actionable authority, identity, replacement, race, or test-path findings remain.
- The review did not run builds or tests. Validation evidence and its exact input identity are recorded separately.
- This is an unpromoted candidate checkpoint. It does not claim full CORE-F integration, detached hosting, persistence completion, or canonical promotion.