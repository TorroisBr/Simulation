# CORE-F P12-D Core-bound Demographic Capture — Independent Exact-tip Review

**Verdict: VALIDATED_CANDIDATE**

## Reviewed identities

- Candidate branch: codex/core/CORE-F-core-owned-runtime-application
- Exact candidate commit: 24b629c6fcdd6918d1dd1ca402a665b17e363559
- Candidate tree: e0f1222014ede8c6e7594ef614c1170c976e7c2e
- Immediate base commit: 399470abd67462251baf5091f5b227ec02229be3
- Base tree: 6aa0e8af441f37e37a1a22f0cd99b2b5d5f181fc
- Core canonical at review: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Remote direct-ref verification: candidate and canonical matched these SHAs at review. A normal fetch could not update FETCH_HEAD in this environment; git ls-remote confirmed both remote refs without altering repository state.
- Review worktree was pinned to the immutable candidate SHA. The candidate and canonical branches were not modified.

## Scope and payload

The complete base-to-candidate diff contains exactly six implementation/test files and three scoped validation files:

- Assets/_Project/Scripts/P12DDailyV1OwnerPackage.cs
- Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs
- Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs
- Packages/com.simulation.core/Runtime/Persistence/DailyV1P12GContinuationSourceWitness.cs
- Packages/com.simulation.core/Runtime/Persistence/DailyV1P12GCoreBoundOwnerSources.cs
- tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs
- docs/validation/CORE_F_CoreOwnedRuntimeApplication/P12CoreBoundDemographicCapture_20261029/VALIDATION.md
- docs/validation/CORE_F_CoreOwnedRuntimeApplication/P12CoreBoundDemographicCapture_20261029/p12-corebound-demographic-capture-editmode.manifest.json
- docs/validation/CORE_F_CoreOwnedRuntimeApplication/P12CoreBoundDemographicCapture_20261029/p12-corebound-demographic-capture-editmode.zip

No ProjectSettings, metadata, private raw evidence, unrelated or untracked files are in the candidate payload. git diff --check passed.

## Implementation review

No actionable defects were found.

- The application enters the existing ContinuationSave operation lease before capturing the boundary witness and demographic sources. It captures and binds the exact Person/Genealogy owners before publishing the save operation. Capture or bind failure disposes the operation, ends the lease, and returns failure; exceptional cleanup also ends the lease before rethrowing.
- Capture resolves PersonStore and GenealogyStore from the selected Core owner graph and requires that graph to be the exact graph held by the selected runtime execution boundary. The witness is checked around snapshot capture.
- The staged source is bound once to the exact save operation, witness/token/vector and Core owner instances. P12-D section validation requires exactly one matching section and checks schema, role, owner instance identity, cardinality and revision for membership, materialization-binding and genealogy parentage.
- The app-backed Unity path requires the operation and Core-bound City/demographic sources together, verifies exact PersonStore and GenealogyStore references, and stages the captured immutable snapshots. It does not recapture those owner snapshots. The legacy path without an application/save operation retains its prior capture behavior. Supplying an operation without the Core-bound sources fails closed.
- The change preserves the existing P12 staging and hydration order, owner codecs, persisted state, identities, revisions, epochs and wire contract. It does not broaden profile coverage.
- The new tests cover successful capture of the exact nonempty demographic owner graph, section identities/cardinality/revisions, stale demographic-owner identity rejection before operation publication, preserved active session and zero leaked active operations. Existing source-witness invalidation tests continue to cover mutation rejection while the save lease remains active.
- The shared owner source types remain in Simulation.Core; no Unity dependency or parallel authority is introduced.

## Exact-source validation evidence

The committed validation record is docs/validation/CORE_F_CoreOwnedRuntimeApplication/P12CoreBoundDemographicCapture_20261029/VALIDATION.md.

### CoreValidation

- Protected run ID: 828eab5b5c8d499792064d1398a7a236
- Cold: 934/934 passed; warm: 934/934 passed.
- Input digest: b2795012c2d0cfbc18de2c158876952f8fa468608d60a627be14a1b96926f3a1; generated dependencies stable.
- Private runner-manifest SHA-256: 40b13fac7df53b144f12bc1e9aac031a6593e72bafdc8db6cb86a8d92ed4ba9e.
- The six implementation/test checkout-byte hashes match the validation record and its input digest. Their committed Git blobs match under the declared core.autocrlf=true checkout normalization; no semantic source delta is present.

### Unity

- Unity Editor: 6000.3.9f1.
- Focused persisted-image restoration regression: 1/1 passed; stable input manifest.
- Full EditMode: 3,279/3,279 passed, zero failures or skips; stable input manifest.
- Official filtered Smoke: 5/5 passed, zero failures or skips. Its five case identities also occur and pass in the full EditMode report, which is the authoritative full-suite package.
- The filtered PlayMode invocation returned zero cases and is explicitly excluded from gate claims.
- Full EditMode archive SHA-256: 38c74c16a9ec96a0b47f0c1f617f722a2cefe40fbbcd8e91661b0b08f5a1325b.
- Public package manifest SHA-256: e7f0c247973f4bc162f2bd18f8e21fa6a47f6876d5e6e6cb658b9fceabb70959.
- Independent archive/manifest verification and comparison to the protected original run passed. The private runner manifest and raw reports remain outside the repository and were not modified.
- Public validation artifacts were checked for workstation-local paths and credential-like content; none was found.

No tests were rerun during this read-only review. The committed code and test inputs match the recorded tested inputs.

## Integration constraints

This verdict applies only to the exact candidate and bounded Core-bound Person/Genealogy capture described above. It does not promote Core canonical, close P12 or CORE-F, or claim detached host readiness. Existing save admission rejection for the unsupported City receipt vector remains distinct from stale demographic-owner rejection after token issuance. Canonical integration still requires its separate refreshed preflight and promotion workflow.

## Review outcome

The candidate is suitable for the next integration step as a VALIDATED_CANDIDATE. No blocking evidence or implementation finding remains.
