# CORE-F P12-E PoliticalSupport Core Migration Review

| Field | Identity |
|---|---|
| Verdict | `VALIDATED_CANDIDATE` |
| Candidate branch | `codex/core/CORE-F-P12E-political-support` |
| Candidate commit | `0b0315085b7df89f0dad6f7739aef4eb4934f411` |
| Candidate tree | `ed9a9c2de29b7db35a51fec04470eec95b786d7b` |
| Candidate base | `d2d5b4d9ef268e5aa64b7902188784abf4ba88bc` |
| Parent | `a4e1d09b04179f4fcd556af55be810428450afb2` |
| Core canonical at review | `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea` |
| P12 canonical at review | `4cc31073f0ec6ba5c718084e4cebd6e914931d5d` |

## Scope reviewed

The candidate moves the existing PoliticalSupport contracts, transitions,
store, census providers, owner codec, and owner snapshot into
`Simulation.Core`, preserving their Unity `.meta` GUIDs. It introduces a
Core capture-source boundary and keeps runtime/token access in the Unity
adapter. The Core P12-E owner stager now stages PoliticalSupport after the
exact Person, Faction, and PoliticalClaim roots; the Unity P12-E package
consumes the staged result for both live capture and detached restoration.

The implementation preserves the existing Required schema-v1 owner section,
exact store identity, cardinality, local revision, detached relation history,
typed endpoints, captured-day bounds, and deterministic relation ordering. The
private hydrator binds only the provided staged roots, preserves the captured
revision, reconstructs the derived active-pair index, and returns no partial
stage on failure. No duplicate PoliticalSupport type implementation remains
under `Assets`.

The exact `DailyCaptureStagingAttempt` contract is retained at the Unity
boundary: package capture passes its existing attempt into the adapter, and
the adapter delegates freshness validation to
`DailyCaptureStagingAttempt.IsCaptureCurrent(runtime, token, vector, attempt)`.
That preserves exact runtime, completed token, owner-vector, and replacement
operation identity. Core has no dependency on the Unity-only attempt type.

## Review result

The complete candidate diff was reviewed against its exact base, the accepted
P12-E PoliticalSupport snapshot design and its independent design/implementation
reviews, the P12-E owner package review, and the applicable Phase 12 contracts.
The implementation and test changes remain within that bounded extraction
scope. The updated tests route capture calls through the adapter without
weakening assertions. Core stager tests verify PoliticalSupport output,
ordering, and rejection of an invalid support snapshot without a partial
stage.

No actionable implementation, contract, scope, determinism, identity,
revision, staging, or test-integrity findings remain. The earlier integration
failure caused by omitting the staging-attempt identity was corrected before
this exact-tip review and is covered by the restored package call path and
successful regression suites.

## Validation evidence

All following Unity runs used candidate source commit
`0b0315085b7df89f0dad6f7739aef4eb4934f411`; their private run manifests record
stable before/after validation inputs:

| Gate | Result | Private evidence |
|---|---:|---|
| `P12GPoliticalBindingRestoreTests` | 15/15 PASS | `E:\ev\93e714bb745a4463a9dfa0a7f984900b\8c0572268101428699f73190e211766e\run-manifest.json` |
| `SimulationRuntimeAdmissionTests` | 227/227 PASS | `E:\ev\ba0bd1da67264b438d580b8db4fdacc4\21e35c6146e140658105bb4976f8b6e2\run-manifest.json` |
| ALL EditMode | 3271/3271 PASS | `E:\ev\8f2ee0aaa71446bba16cf9fa096c9e5b\d257aa19aa8a4111a88c8386498d5034\run-manifest.json` |
| Official Smoke | 5/5 PASS | `E:\ev\73524750548847e5a7833b408c49a256\e1d1b5f6c04d4cc8a06568bd5084073c\run-manifest.json` |
| `git diff --check` | PASS | Candidate diff against the base |

The complete EditMode report was packaged and privately verified against its
run manifest. Package manifest:
`E:\ev\p12e-political-support-0b031508-packages\p12e-political-support-editmode-0b031508.manifest.json`.
Archive:
`E:\ev\p12e-political-support-0b031508-packages\p12e-political-support-editmode-0b031508.zip`.
Archive SHA-256:
`698d3cc33105de82bedcc19bae05b9bdbeb3dad2cb910199ab4e2800a6ad0b7b`.
The archive contains all 3271 cases, including the five Smoke-named cases.
The separate filtered Smoke raw report is preserved; its filtered NUnit XML
was not separately packageable because the parent `Simulation` suite count
does not match its filtered descendants.

CoreValidation passed cold and warm at `2454518b216a36b2b516b3ea6e9e9c9799bb006f`
(861/861 each). Its retained manifest is
`E:\ev\20c2857f2518470a9dd6b3507482df63\manifest.json`. The Core production
sources are unchanged from that validated commit through this candidate; later
changes are confined to Unity package/adapter files and test call sites.

## Limits and integration constraints

This is a candidate review only. It does not promote the candidate to Core
canonical or P12 canonical and does not complete CORE-F, the entire P12-E
owner inventory, global quiescence, profile-wide capture eligibility, P12-A,
P13, or Phase 12. Those boundaries and their existing blockers remain as
recorded by their owning contracts.
