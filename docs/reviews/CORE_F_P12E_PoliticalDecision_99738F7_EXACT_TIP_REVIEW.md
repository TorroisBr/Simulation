# CORE-F P12-E PoliticalDecision Owner Snapshot — Exact-Tip Review

**Verdict: `VALIDATED_CANDIDATE` — PASS.** Independent code and evidence reviews found no actionable issues.

## Identity and scope

- Candidate branch: `codex/core/CORE-F-P12E-political-decision`
- Candidate: `99738f7daced48757f4c50e881ee3654fd6d7b19`
- Candidate tree: `96a09e96c0a08e6d0f3a347bc4d8f9a20bf45c69`
- Base / parent: `0b0315085b7df89f0dad6f7739aef4eb4934f411`
- Shared application candidate at review: `codex/core/CORE-F-shared-daily-v1-app-host` at `0b0315085b7df89f0dad6f7739aef4eb4934f411`
- Current Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Current Phase 12 canonical: `4cc31073f0ec6ba5c718084e4cebd6e914931d5d`
- The remote candidate ref was verified at the exact candidate SHA after publication.

This bounded extraction moves the PoliticalDecision runtime owner, its snapshot/codec, and its census provider into Simulation.Core. The Core capture-source contract and Unity adapter preserve the completed-capture boundary. The Core stager now stages PoliticalDecision after its required D/E owners and keeps its exact owner identity and revision. Unity metadata GUIDs are preserved. Capture, live staging, and detached staging use the same Core-owned authority. No P12 schema or profile semantics were changed.

## Independent review

The exact-tip code review compared the complete candidate diff with its parent and the approved P12-E owner-snapshot contract. It found no actionable defects. It verified owner identity, schema and ordering, cardinality, local revision, completed-token and owner-vector checks, D/E-before-PoliticalDecision restore ordering, and preserved metadata GUIDs. Existing assertions were not weakened.

The separate evidence review verified exact source identity, input stability, coherent results, artifact hashes, and protected evidence ACLs. No validation artifacts were modified or included in the candidate code commit.

## Validation evidence

| Gate | Result | Run identity | Artifact identity |
| --- | --- | --- | --- |
| CoreValidation cold | 862/862 PASS | `011896c80de343a1b8973007c06c08fd` | Manifest SHA-256 `6ba456ef10484e45663c35b7f42c0e6eb3ea792e3bdd55333cce85479efbfcbd`; cold TRX SHA-256 `dd4f97b10a674f276d9c186b1736415fd41d2094c0be783b7cec5b7fe9a1c80b` |
| CoreValidation warm | 862/862 PASS | `011896c80de343a1b8973007c06c08fd` | Warm TRX SHA-256 `54c3ac8987b86c8d1635336bdd858af87baa6807ccba3fc78287d65d571ded60` |
| Full Unity EditMode | 3271/3271 PASS | `55be8933951e4ef1b883f584281ec5c7` | Stable input; manifest SHA-256 `bbf3c6f51e7cdc44b6d1ec789c99eaf4f6a1854e661f6c289996d1b408acd573`; XML SHA-256 `378036a6991b6c8be04ff27106757903faef6fefa646336dd7c92d19c1a2f80d`; log SHA-256 `83710365e828cca38ee61b63f1124665a049c08cf90efadb157ebe4a29834577` |
| Official Smoke | 5/5 PASS | `0b0f0aaac9d54678af93bd6234b5f1b4` | EditMode `Smoke` selection; stable input; manifest SHA-256 `118582dfbb7512242b4fe605b0834dd299dbfc0270b7ea135fcbbe1b55f3a77f`; XML SHA-256 `423cb5705395153a1f985080aa0d2a5eab18291a521c35a6bd5abf5e9f4258f3`; log SHA-256 `0182620d1350a357d64b1c2f0593132243656feaafa9842ba5d27c634c90f8ce` |
| `git diff --check` | PASS | Candidate diff `0b031508..99738f7` | No whitespace errors |

The CoreValidation manifest records stable inputs and the exact candidate commit/tree. The full EditMode and Smoke manifests also bind to the exact commit/tree and report `inputStable=true`. The evidence reviewer verified the cold/warm TRX counts and all recorded Core report/log hashes. Raw XML/log artifacts remain in protected local evidence storage; only run IDs and integrity hashes are recorded here.

## Excluded diagnostic attempts

These runs are preserved but are not counted as passing gates:

- An initial full EditMode attempt used the default C:-rooted temporary directory, had five P12-A storage failures, and changed ProjectSettings during Unity import. The official rerun used the project-required E:-local TEMP/TMP and passed the complete suite.
- Filtered P12-A XML reported 37/37, but its Unity process did not exit before the official timeout. It is not a pass.
- A PlayMode Smoke filter selected zero cases and changed ProjectSettings; it is not a Smoke result. The accepted Smoke gate is the stable five-case EditMode `Smoke` selection above.
- The focused P12-E-only run reported 5/5 but had `inputStable=false`; those tests are included in the stable full EditMode run.

## Integration constraints

This review validates only the bounded P12-E PoliticalDecision owner extraction against the current shared application candidate. It does not complete CORE-F, establish the detached host or full application lifecycle, or authorize Core canonical promotion. Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`; Phase 12 remains closed in its recorded scope. Continue by integrating this candidate into the shared CORE-F application candidate and refreshing its dependency graph. Do not infer CORE-F completion from this checkpoint.