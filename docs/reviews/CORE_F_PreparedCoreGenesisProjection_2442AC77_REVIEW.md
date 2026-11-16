# CORE-F prepared Core genesis projection — exact-tip review

## Exact identity

- Review branch: `codex/core/CORE-F-prepared-genesis-projection-review-2442ac77`
- Implementation candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Reviewed implementation commit: `2442ac778f221ca06299111f041bb5cb982a4b3c`
- Reviewed implementation tree: `bcd8b4a69ccbe7a09c10df3fed9985f660f042a1`
- Implementation base: `ab6e25e3c3a424f6ad53e1963aea5c9471fa0a8e`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Additive review branch base: `a9d6a73e6530381f0ac7f5aa0e4a67ede8f208af`
- Verdict: **PASS — VALIDATED_CANDIDATE**

The additive base `a9d6a73` is a later docs-only implementation-branch commit adding this candidate's validation record and full-suite package. The implementation source and test blobs remain those reviewed at `2442ac778`; this verdict is bound to that exact implementation commit/tree. The implementation candidate remains unpromoted.

## Reviewed change

The complete implementation diff from `ab6e25e` to `2442ac778` changes only:

- `Assets/_Project/Scripts/TesteSimulacao.cs`
- `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs`

The selected NoSave start path now creates the Unity host projection directly from the exact `GenesisPrepared` Core composition instead of re-entering `InitializeSimulation`'s stage loop. The regression adds reference-identity assertions for the Core owner graph, execution boundary, runtime ID allocator, record sequence, selected City/NPC graph, and simulation clock.

## Review result

No actionable findings.

The projection helper verifies the selected Daily-v1 admission context, exact preparation/application inputs and object identities, existing bootstrap state, and the P10 exclusions before building the host adapter. It projects the already-composed Core roots and passes the same owner graph, clock, allocator, record sequence, and execution boundary into Unity's runtime projection. It does not construct another genesis graph or allocate a second runtime identity authority.

The selected initialization retains the existing logger and module configuration, scenario diagnostics, system reconstruction, Justice reference binding, candidate-profile validation, selected chronicle formatter, P9 fingerprint/provenance, and bootstrap thread check. This Start transaction supplies no stage-progress callback, so bypassing its no-op stage loop drops no presentation callback. The ordinary `InitializeSimulation` path remains available for the other supported/compatibility bootstrap route.

P12 remains fail-closed: the selected runtime requires the receipt and runtime-identity/spatial census owners, opens the existing bootstrap publication scope after taking the census baseline, validates the candidate profile, closes the scope, reassesses registered-owner census health, and marks factual reads available only after successful closure. The helper returns a private Unity projection to the existing Core start transaction; it does not publish a second active session. On failure it returns no projection, marks bootstrap failed, faults runtime admission, and faults/disposes any open publication scope. Existing 89-family owner census/hydration contracts are not changed or weakened by this projection-only diff.

The restore path in `TryBuildInitialStartHostProjection` and the non-selected/legacy initialization path are unchanged. Mixed-roster runtime fallback and action execution are outside this diff. P10 Ruin/LocalTopology and generated Ruin remain excluded from this Daily-v1 proving profile.

The added test asserts that the published projection is bound to the exact Core-prepared session and definitions, owner graph, execution boundary, allocator, record sequence, runtime owner graph, and clock. No existing assertion was weakened.

## Exact-source validation evidence

- Unity input digest: `64cc2f3b1ed4caf17d9681c639110494b6583530d5822b464b83d3f6abe7bf1e`; the official run records report `inputStable=true`.
- Focused selected NoSave startup regression: run `aaa983b7fe4545b5b26a74d2f985cc9c`, 1/1 PASS.
- Full Unity EditMode: run `b83286caf2344751a7fa7d0f76b75876`, 3,321/3,321 PASS, zero failed/skipped/inconclusive. Its input snapshot names implementation commit `2442ac778` and tree `bcd8b4a6`.
- Official Smoke filter (`EditMode`, `Smoke`): run `66333e5ce786415ab9fd295c10eeec7f`, 5/5 PASS. The full EditMode report also contains the five Smoke cases.
- Full EditMode manifest SHA-256: `a64df2f8fdbf4cf15e7025353777eab05ec59d3a78e4e6a8effb4d9c957a35ac`.
- Normalized full-suite archive SHA-256: `7debbbbed74d829728228a003f71037070439f25cd66b4f3b9faeb1b635eff27`.
- Changed source SHA-256: `TesteSimulacao.cs` `6c42395d8138310b84fcb656dd0ea14ea27652dc37125c72b498634c8d27d354`.
- Changed test SHA-256: `SimulationRuntimeAdmissionTests.cs` `c32a2f118defeaa2fc2ac0930a7c78e199f1fb482dba72783fe28ea909e39d67`.
- `git diff --check` from implementation base to candidate: PASS.

The review relied on the recorded exact-source runs and package identities; the reviewer did not rerun Unity. An attempted PlayMode invocation that selected zero tests is excluded. Original reports and raw logs remain in the existing protected evidence location; this record contains no private workstation path.

## Limits

This validates only the prepared-Core-genesis-to-Unity-projection integration at the stated implementation tip. It does not promote Core canonical, close CORE-F, establish a fully Unity-independent runtime, complete the public `DailyV1Application.TryStart` contract, or make broader P12 readiness claims.
