# Daily-v1 census baseline count evidence

This evidence establishes the registered owner-section count on the exact P12 canonical base before the two exact-zero receipt sections are added.

- Canonical commit: `ea4decdaffa26e80e76ee72135ead0a7d673f358`
- Canonical tree: `9241cfbb3d9aaae9b72e05a66cd12c0969db2d45`
- Profile: `UnityBootstrap-Daily-v1`, loaded from `Assets/_Project/Data/Simulations/Simulation-DailyV1.asset`; test asserts the authored P10 Ruin is null.
- Test: `SimulationBootstrapCompositionTests.SelectedDailyV1ProfileBootstrapsItsAuthoredP8GeographyBeforeDayOne`
- Temporary probe: after the normal `simulation.Start()`, reflect the sealed runtime `ContinuationCensusProtocol.expectedSections` and assert `Count == 233`.
- Probe source snippet: `DailyV1CensusBaselineCountProbe.cs.txt` (SHA-256 `E8FF6C1BBCB5D413062ABA8366880918ACC0A87EFB8EE87DB7A015F1BA61172A`). The snippet was inserted after `simulation.Start()` in the named test inside a detached temporary worktree; no product code or canonical history changed.
- Unity: `6000.3.9f1`, batchmode EditMode, exact single-test filter above.
- Result: 1 test, 1 passed, 0 failed. The assertion observed 233 registered sections.
- XML: `Canonical-ea4dec-DailyV1-CensusCount.xml` (SHA-256 `F393F4E9448D5E2300E632F1CA0442D5CB947F5628ED8158D7190EA16F6E9ADE`).
- Log: `Canonical-ea4dec-DailyV1-CensusCount.log` (SHA-256 `1DAB538E0562F7E57DA9575E162586A32660114EC40CDA262F2E9B0BDBBE3BFE`).

The later selected-profile implementation assertion is 235 (= 233 baseline sections + the two fixed receipt sections). That assertion and its output belong to implementation validation, separately from this exact-base probe.
