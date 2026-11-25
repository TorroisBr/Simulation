# Independent review — CORE-F P12-C root staging

## Verdict

`VALIDATED_CANDIDATE` — no actionable implementation or test findings.

## Reviewed identity

- Repository: `TorroisBr/Simulation` (origin URL confirmed from repository metadata)
- Candidate branch: `codex/core/CORE-F-shared-daily-v1-app-host`
- Candidate implementation commit: `0dec483e3b181b168ecc007669814ab72e7bc8f1`
- Candidate tree: `9f7b70665d989c4d786d142cf3dfa5fee9a0d7d0`
- Exact base / parent: `05cd17b366ba768fff0ed60ecfee9f388fe4847f`
- Current Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Remote candidate was verified at the exact implementation commit before this review record was added.
- Candidate diff is the seven-file base-to-commit change: four production/test sources, `VALIDATION.md`, the normalized full EditMode package manifest, and its ZIP. ProjectSettings are not in the committed diff.

## Review findings

The application boundary admits initial restoration only on its captured bootstrap thread while the app has no published session, active operation, or prepared/attempted initial genesis. That bootstrap context is constructed by Core as `UnityBootstrapDailyV1`.

For replacement restoration, Core requires the exact currently published session, its owner-thread exclusive reservation, the same reservation-bound session identity, replacement-capture authorization, no other active application operation, the same execution-boundary state, and a healthy completed daily replacement boundary. These checks occur before P12-C image decoding or staging.

After authorization, Core decodes the immutable P12-C image and invokes the existing selected-root stager to produce a detached Core root. The application method does not mutate, capture from, replace, or publish the source session, and it does not retain or expose its token/reservation. The Unity adapter now delegates this staging path to the application and constructs only the existing P9 manifest compatibility projection. The prior no-application adapter path remains for legacy callers. The restore coordinator’s initial-restore call supplies the application; replacement restore supplies both the application and exact reservation. Later D/E/F owner hydration and final publication remain outside this slice, as the validation record states.

The added test, `ContinuationRootStagingRequiresExactReservedSessionBeforeInspectingImage`, asserts that an unreserved attempt returns Busy before image inspection/stage observation, while the exact reserved session proceeds far enough to classify a missing image as InvalidOwnerValue. It adds assertions without removing or weakening existing ones. Existing successful restore and P12-C staging regressions remain part of the reported full suite. This candidate does not claim complete restoration or CORE-F acceptance.

The implementation boundary matches the reviewed P12-C private-root composition contract: detached in-memory root staging from decoded snapshots, with no owner publication or broad hydration claim.

## Source and evidence identity

The four SHA-256 values recorded in `VALIDATION.md` were checked against the committed source/test files. GitHub serves the committed text with LF endings; reproducing the Windows checkout’s CRLF endings yields the recorded hashes for the three CRLF files. The Core application file matches directly.

| File | Recorded SHA-256 | Check |
|---|---|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` | `2ea5755bff56000dcbb48a365b0f67ccc24a01d2920086a99d2ba0174e7fb165` | Match |
| `Assets/_Project/Scripts/P12ACausalRootImageAdapter.cs` | `d920bde6f51bea70549b0759c2c095e221f296ef2d2ebf7e589f1a152bb45c45` | Match after CRLF checkout rendering |
| `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.ImageRestore.cs` | `fc1c8df8e32002576e3c4c7a2dab2d02f3d796fec7942a6bddf6f00a0e9c317a` | Match after CRLF checkout rendering |
| `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs` | `b93b5603a16265b144ff46eb4159ffbf76aa3a5de025601c4ae180b036653dc9` | Match after CRLF checkout rendering |

The public Unity package manifest is Git blob `a1819f0a52521dc1257703691e70e9ddfbf70eaf`; its recorded input snapshot SHA-256 is `bbfa8eb16672334ff1db40942a3f18a5771260f3a7361fc32080ccf60a58264c` (1,956 inputs). The run identifies the expected repository, candidate branch, base HEAD/tree, Unity 6000.3.9f1, and a single successful full EditMode attempt: run `14aa2c0880854c03adb093a4dbaa9bc2`, 3,274 total, 3,274 passed, zero failed/skipped/inconclusive, coherent counts. Original XML SHA-256 is `0c549f08d4d43cc6f1654c2a0f2d6ae9e0b9dd25426ab5354502697be4fa5b02`.

The committed archive was fetched as base64 from the repository and independently hashed: 172,962 bytes, SHA-256 `14a9ec718afac9d0fd0fd9bb5f8c132180badb116702d2164bc8aba31fcfa55c`. This matches both the manifest archive declaration and `VALIDATION.md`. The record also reports the official package verifier PASS. The full package contains only normalized full EditMode XML. The filtered P12 and Smoke originals are not claimed as packaged artifacts; their original metadata limitation and subset relationship are explicitly documented.

The public manifest contains repository-relative input paths and no absolute workstation paths, credentials, or secret-like values. The validation record accurately bounds CoreValidation cold/warm 893/893 to private retained originals, lists their manifest/TRX hashes, and explicitly makes no public TRX package claim because the required TRX source-context adapter is unavailable. The private CoreValidation originals were not independently opened during this review.

## Validation records and limitations

Recorded successful runs:

- CoreValidation run `cb58cdd169274f418e37292f6201fd26`: cold and warm 893/893. Private manifest SHA-256 `49e0c281776de97b80d4215114350fdc1143b818f7610f71f7739950996e90b7`; original cold/warm TRX hashes are in `VALIDATION.md`.
- Focused Unity continuation restoration: 1/1; P12 regression selection: 564/564; Smoke: 5/5. The focused and filtered originals remain private; the validation record explains their evidence handling.
- Full Unity EditMode: 3274/3274; official normalized package and manifest hashes verified above.
- `git diff --check`: reported PASS in `VALIDATION.md`.

This is an exact implementation-candidate review, not canonical promotion or CORE-F completion. P12-D/E/F owner hydration, complete detached restoration, TryStart, the detached .NET host, and CORE-F acceptance remain unclaimed. No implementation files were changed by this review.