# Independent exact-tip review — session projection publication

Verdict: PASS for this bounded candidate change. No actionable implementation or test-integrity findings. This is not a CORE-F completion or canonical-promotion recommendation.

## Identity

- Candidate branch: codex/core/CORE-F-core-owned-runtime-application
- Candidate commit: 0c5e23772b39d55479f803b80c69e95ff6234836
- Candidate tree: 9126340c24996b8ba52d3dbcecdcfd8e0930e7e8
- Parent: a1b4cf23ee8ab9aeb117db7faa5b0ba0576c96bf
- Core canonical: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea

The exact commit changes only the intended four files: TesteSimulacao.cs, DailyV1Application.cs, DailyV1ApplicationHost.cs, and DailyV1ApplicationSessionGateTests.cs. The change centralizes opaque host-projection publication and replacement within the Core application’s existing serialized session gate.

## Findings

- Projection lookup, initial publication, replacement, and retirement use the same application gate. A rejected initial publication removes its staged mapping; a rejected replacement retains the old Core session and projection.
- Replacement checks the exact source session and registered projection. The candidate mapping is staged before exchange, then the old mapping is retired only after successful commit.
- The projection marker is empty and opaque. Core uses it only to return the adapter corresponding to the exact published Core session; it does not read simulation authority from the projection.
- The Unity projection constructor binds to the exact Core session execution state and definition graph.
- Tests cover genesis publication, rejected initial restore and retry, stale replacement rollback, and successful replacement retirement. No assertion was weakened.
- The commit diff contains no unrelated source, ProjectSettings, evidence, or user data.

## Validation evidence checked

- CoreValidation exact commit: 993/993 cold and 993/993 warm, zero failed or skipped. The cold/warm TRX reports match their native manifest identity and hashes; cold/warm test identity and outcome sets match, and all three changed focused tests pass in each run. The isolated runner recorded LibraryAbsent=True.
- Unity exact commit/tree: official EditMode result 3296/3296, zero non-passed cases, stable input manifest. The official normalized package verifies and binds the exact commit/tree; the changed Unity production source hashes match its captured input snapshot.
- The separate Smoke-filter run passed 5/5. Its standalone package was not claimed because the approved packager rejected the filtered XML’s inconsistent parent-suite testcasecount. All five case identities are present and Passed in the complete EditMode report.
- The native CoreValidation runner does not emit dotnet-trx-source-context-v1. No public DotNetTrx package or private-to-public projection is claimed.

The Unity public package is committed beside this review record. Protected original TRX, run manifests, raw logs, and the stable local ProjectSettings overlays remain outside Git.
