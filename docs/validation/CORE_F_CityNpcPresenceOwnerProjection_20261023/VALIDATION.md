# CORE-F City/NPC Presence Owner Projection

This is an implementation milestone on the existing CORE-F runtime/application candidate. It does not complete CORE-F, promote Core canonical, or claim a detached host.

Independent exact-tip review: see [REVIEW.md](REVIEW.md). Verdict: `VALIDATED_CANDIDATE`; reviewed code commit `d0f4df4eef9cf17e6fa8a4fe7edece6335cfced3` on base `cebd8cd201149e5192880e32411a5b5ad7237b7d`; no actionable findings.

## Change

For selected Daily-v1 City owners, `CityRuntime.ImportantNpcs` now resolves the exact Unity NPC projections from the ordered `DailyV1CityNpcPresenceOwner` RuntimeId vector through the owning `SimulationRuntime` registry. The selected Core owner is the membership authority; the old Unity list remains the unselected/legacy path and is cleared when the selected runtime binds. Snapshot membership, membership preflight/commit projections, and Core-to-P12 bridge reads use that same owner-derived view. Binding preflights every selected City projection before changing any City binding.

The composition regression constructs a `SimulationRuntime` over the admitted City/NPC owners and verifies exact registered object identity, ordered snapshot IDs, and owner revision after removal and re-addition. The travel regression now verifies that a deliberately stale legacy cache entry cannot override Core-owned membership; the non-arrival travel owner advances once, and the P12 mutation epoch advances once. It preserves the assertion that no arrival is reported.

## Validation

- Unity 6000.3.9f1, Test Framework 1.6.0.
- Full EditMode: **3255/3255 passed**, zero failed, skipped, or inconclusive. All five Smoke cases are present and passed in this run.
- Official Smoke filter (`EditMode`, `Smoke`): **5/5 passed**.
- Focused `SimulationBootstrapCompositionTests`: **39/39 passed**.
- Focused `P12DCityRootOwnerSnapshotTests`: **18/18 passed**.
- Focused `SimulationRuntimeAdmissionTests`: **220/220 passed**.
- The focused and full-suite reports were captured with the approved evidence wrapper. A private-source verification of the full EditMode package passed.
- Final full EditMode run ID: `248d2eba39584bcb878f7147df80a004`.
- Final original XML SHA-256: `19344a882131e3c66fa6ff7d28688687773aa731cd2293b28ab8e04538c60575`.
- Input snapshot SHA-256: `cf723fb492519db7d724ccbe7298ecf1a66d4dd540f9a77233ce1efe164e70b3`.
- Normalized package: `coref-city-npc-presence-editmode-final-20261023.zip`; archive SHA-256 `a95443adc8483c72812ff590a118ace651ee55ab7dca3f449e7a40b6b814eb98`.
- Package manifest SHA-256: `f3651c4f8c8adfdcddb4db069f78973a67cf4d8351b2396926822d5cb88341ba`.
- Separate final Smoke run ID: `0bbd592133a3425294316439ee2cbe2f`, 5/5 passed on the same input snapshot.
- The manifest records the matching SHA-256 of each changed production/test input. The archive contains normalized result XML only; original XML and raw logs remain in protected local retention.
- The earlier full-suite package in this directory is retained as historical evidence from before the final fail-closed tightening; only the `-final-` package is used for the final source snapshot claim.
- `git diff --check` passes for the implementation and test changes.

Filtered NUnit XML was retained privately but not included in a public package because Unity reports the full `Simulation` ancestor suite count while emitting only filtered cases. The complete EditMode report is the packaged result authority. Intermediate failures were corrected before the final pass: two CS0136 local-name collisions, and an over-restrictive selected-definition check that rejected supported dynamic person materialization. The exact registered runtime identity is the applicable projection requirement. A separate PlayMode filter selected zero tests and is not counted as validation.

## Scope limits

This changes the Unity projection boundary around an existing Core City-presence owner. It does not move `SimulationRuntime`, make the shared application operational, or complete P12 restoration, detached .NET hosting, or T/T+1/T+2 parity. CoreValidation was not rerun because no `Simulation.Core` source changed.
