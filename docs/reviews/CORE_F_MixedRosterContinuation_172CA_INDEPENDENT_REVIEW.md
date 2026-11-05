# Independent Review — CORE-F Mixed-Roster P12 Continuation Composition

## Verdict

**PASS.** No actionable findings were identified in the reviewed three-file candidate.

## Exact source identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Candidate commit: `172ca1a686835e7c0e25837e6a6763f8d6ee6753`
- Candidate tree: `ff186ee425629567e15aeba0faa71540daa4769d`
- Review base / parent: `74878d67ece6880989b0a50c84f82fe86ed0a254`
- Current Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Repository origin: `https://github.com/TorroisBr/Simulation`
- Candidate and canonical refs were refreshed from origin for this review.

## Reviewed scope

- `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs`
- `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.cs`
- `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs`

The candidate is a child of the stated base. Its production change permits Core P12-D/E/F composition when the active save operation has no Core NPC owner sources, but only when the Core P12-D encoder also reports that it omitted NPC sections. The Unity adapter continues to encode the complete NPC roster for this partial graph. The application merges those adapter sections with exact operation-bound Core sections before the existing immutable image composer.

## Review observations

- The save operation must remain active, belong to the same application, and match the active session. Core D/E/F encoders retain their operation-bound capture and revalidation checks. The subsequent image composition revalidates the operation and validates the merged sections against the source witness census.
- `includesNpcOwnerSections` must agree exactly with `CoreNpcOwnerSources` presence. A mismatch fails closed. The coordinator routes an active Core save operation through Core composition; the non-Core path is unchanged.
- Duplicate Core section identities and duplicate adapter identities are rejected. Overlapping identities are accepted only when schema versions, role, alias group, section revision, row ordering, owner keys, row revisions, and payload bytes match. Equivalent overlap uses the Core section; non-overlapping adapter NPC rows remain present.
- The added test uses the whole-roster adapter encoder and operation-bound Core D/E/F sections, then verifies unique merged identities and retention of both sets. Its unselected fixture has no supported selected Daily-v1 definition, so it does not assert that this invalid profile can be saved; existing staging rejection remains intact.
- The change does not alter owner mutation, revision/epoch behavior, capture-token issuance, image validation, or atomic persistence. It does not claim that unsupported roster content is restorable or that CORE-F is complete.

## Validation evidence applicability

Validation was run against the final source contents before the candidate commit. The private manifests recorded stable inputs; their changed-source SHA-256 identities were checked against those contents, and the committed Git blobs were checked against the candidate. Git line-ending normalization accounts for the different SHA-256 representation between captured working files and Git blobs.

| Gate | Result | Evidence identity |
|---|---:|---|
| CoreValidation cold | 1,017/1,017 PASS | Run `18f2b2a0ece948298a9e95b00eee917b`; input digest `4ab1e0054dbae7ce17c3561a3269fb01442814bd1271e56f24f84a8267a2821a`; inputs and generated dependencies stable |
| CoreValidation warm | 1,017/1,017 PASS | Same run and stable input identity |
| Focused mixed-roster Unity test | 1/1 PASS | Run `3b2a395331c7467580a177dcd28e5e71`; source inputs stable |
| Full Unity EditMode | 3,299/3,299 PASS | Run `c2b0d42decc4477d8697e503d5c149e9`; stable input digest `641126df1ae68ac390f30463a5ffb852cd513dab793ac830f96469c217ed87c6` |
| Official Unity Smoke | 5/5 PASS | Run `1b266e5211694a8794031fb1c0a38771`; same stable input digest |
| `git diff --check` | PASS | Candidate source diff |

The changed source input hashes recorded by the Unity manifests are:

| Source | Captured input SHA-256 | Candidate Git blob |
|---|---|---|
| `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.cs` | `6137d45b755da4079c6618669da076c6da238a3e29f3a40bba8e52cb009ba2ff` | `2101f36657ebccf25461098363490291b7a26d29` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` | `349aeee53a1dd1a6c5ac15451f2993976263d15fab054a1b3dd15cb359b4b7da` | `31a15034fdc98f2b5a0ac8756fa1106903011258` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` | `bd46cec5efee669c6128e4471ac9902d78f9ffa047c016fdf42fdd89955d89fc` | `d71013ac5987064d3bf4ea0aeac6ffe7752c666a` |

An earlier `-TestCategory Smoke` invocation selected zero cases and is excluded. The repository-documented `-TestFilter Smoke` invocation above ran and passed all five Smoke cases.

Private original reports and manifests remain in protected E: evidence storage and are not included in this review commit.

## Limits

This review applies only to candidate `172ca1a686835e7c0e25837e6a6763f8d6ee6753` and the three files listed above. It is not canonical promotion, CORE-F completion, or a broader P12 profile admission.
