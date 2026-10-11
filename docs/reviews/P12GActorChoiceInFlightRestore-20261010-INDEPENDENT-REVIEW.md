# P12-G in-flight ActorChoice restore rejection — independent review

## Result

**PASS — exact-tip independent review; no actionable findings.**

- Candidate branch: `codex/phase12/P12GConsumedActorChoiceHydratorAtomicity`.
- Reviewed candidate tip: `0c335c1aec6bd7d209e8f409f7bf0167641b6601`.
- Canonical base: `d3bddfd7a4ffb00fbccfb22c84ee62c49380e71f`.
- Implementation commit: `617932280bfa60297f0a617faf2a1ad344d89293`.
- Implementation Git tree: `8ff334ad6a2e3f35498bea959d5731874c0a6f86`.
- Implementation `Assets` tree: `0fab41ff0d654c6607c4f6d144bb66c6cbaa3463`.

The review examined the only implementation path changed, `SimulationRuntimeAdmissionTests.cs`, and the candidate's validation manifest/artifacts. The parameterized cases retain captured input, command, Person, action, sequence, day, snapshot schema, census revision, and next-sequence values. Pending has no lifecycle dispositions; `ConsumedAwaitingTerminalAttempt` carries the original `DispatchStarted` disposition as its valid lifecycle prefix. Both are rejected by the normal P12-F staged factory before `FActorChoicesStaged`, F staging completion, or publication. Existing assertions continue to cover source session/boundary/health/facts/roots/owner projection preservation, source continuation parity, valid retry, and a subsequent boundary.

The validation manifest's XML, compressed-log, and decompressed-log SHA-256 values match. XML totals and non-pass counts are focused 2/2, `SimulationRuntimeAdmissionTests` 188/188, ALL EditMode 2862/2862, and official Smoke 5/5, with zero failures, skips, or inconclusive cases. `git diff --check` passes. The candidate tip adds only validation documentation/artifacts after the reviewed implementation; its `Assets` tree is unchanged.

This review covers only integrated rejection of the two in-flight ActorChoice states. It does not establish general causal no-replay, P12-G completion/readiness, capture eligibility, P12-A export/hydration, P13 readiness, or Phase closure.
