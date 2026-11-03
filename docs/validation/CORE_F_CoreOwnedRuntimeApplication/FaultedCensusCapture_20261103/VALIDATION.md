# Faulted census capture boundary validation

## Scope

This candidate adds a capture-scope rejection when the selected UnityBootstrapDailyV1 census protocol has faulted. It preserves pre-fault capture assessment behavior and does not require census readiness as a prerequisite. The regression checks None, SessionReplacementValidation, ContinuationSave, and DailyAdvance allowances and verifies that no Core operation is active during rejection.

This is a bounded P12 capture-safety correction. It does not deliver DailyV1Application.TryStart, the complete shared runtime/host integration, or CORE-F completion.

## Source identity

- Core canonical at validation: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Candidate code commit: fdc57ef74f4dde012eae9ee8caf2d0e7b10da5ed
- Candidate code tree: 5e96b4b1ec1106afd7596af0241f2ebccc24f0d4
- Candidate base: ebf64d09d3ed8292f8e8f4231f174271b764b6dd
- Packages/com.simulation.core/Runtime/Application/DailyV1ExecutionBoundaryState.cs: SHA-256 bf35fc9468f1205d234c359a8cef382abc65fa52bd8d93e7285111372c31a7b2
- tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs: SHA-256 e2c35f1e82fc043f7a7dc9246a46574ab4b9661550c9dba12fca4080c573d039

Both source hashes match the committed candidate.

## CoreValidation

The approved CoreValidation runner completed cold and warm runs on the candidate source:

- Run ID: bc24ac660e27432584f1b7703c03136a
- Result: cold 1003/1003 PASS; warm 1003/1003 PASS
- Input digest: 1de03187b938b2c873e14be96d3d64bedb104e18e98d280bba6825a08507a0e6
- Source and generated-dependency stability: PASS
- Private run-manifest SHA-256: 204bd0780ec84dd5c31a5d0b5ee475b5bb3d476ce5c24c62f5ba7f0557a9ee89
- Cold TRX SHA-256: c6985647ffc8a46429e77a83fd5288b4043c1b316d32d105fb19455636e9a2d7
- Warm TRX SHA-256: 615173b2cfb419b788877777e8634ddd965f9d29cbad68469a2604657c37d235

The original TRX files and private runner manifest remain in protected evidence storage. The native CoreValidation manifest does not satisfy the separate dotnet-trx-source-context-v1 packager input contract, so no public DotNetTrx package is claimed. This run does not claim independence from a Unity Library.

## Unity EditMode

The official Unity evidence runner completed the full EditMode selection on the exact candidate source:

- Run ID: 27a4ab280d1947b4b646980fe06c06a5
- Unity: 6000.3.9f1
- Result: 3298/3298 PASS; 0 failed, skipped, or inconclusive
- Input digest before and after: 04fb7176ce67f2baf9d1c8a29047f1d94090568ab5d05e06a3f369a1c7af0356
- Source snapshot stability: PASS
- Private run-manifest SHA-256: a764a33086699f24fb16f3ae850e451f4ea5b181dc956913056d36e9538c205f
- Public sanitized manifest SHA-256: 574ed04bcfc6787d18376f78512c940bae6c8e1f88b68a4c24ff937877290831
- Public evidence archive SHA-256: 06adadb3cfdeba0afd9c770401ec5f5c693eab80c6dbdc44ee7ae690fc5908a6
- Official package verification with the private run manifest: VALID, 1 run, 1 entry

The package contains the sanitized XML report and no local workstation paths. The original Unity log and private manifest remain in protected evidence storage. No separate Smoke run is claimed.

## Attempt disposition

- A CoreValidation invocation launched from the C: project root was excluded: its source-trace verification could not bind the C: test assembly to this E: candidate. It is not counted as candidate validation.
- An intermediate exact-worktree CoreValidation attempt exposed a test type-name typo; it was corrected before the passing cold/warm run.
- An earlier capture check rejected both not-ready and faulted protocols and caused 13 test failures. The condition was narrowed to the selected protocol fault latch only; the final cold/warm run passed.
- An initial Unity run passed tests but its input snapshot changed during Unity import, limited to ProjectSettings. It is excluded. The final run used a separate stable validation worktree with the same two ProjectSettings values already present before the run; the input digest remained unchanged.
- git diff --check: PASS for the implementation change.

## Review and promotion status

The code candidate awaits independent exact-tip review. Core canonical has not advanced. This evidence record does not imply canonical promotion or CORE-F completion.