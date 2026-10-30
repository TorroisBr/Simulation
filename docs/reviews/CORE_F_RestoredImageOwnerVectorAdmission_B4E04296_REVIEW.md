# CORE-F restored-image owner-vector admission — exact-tip review

**Verdict:** `NEEDS_CHANGES`
**Review date:** 2026-10-29 (America/Sao_Paulo)
**Reviewer:** independent read-only review; candidate and protected evidence were not modified.

## Exact identities and ref preflight

- Repository origin: `https://github.com/TorroisBr/Simulation`
- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Candidate tip: `b4e04296cf254e8f5e2124e1f96af1a19e7aaf11`
- Candidate Git tree: `1f5bef28877485caa0a622a788ef3d07d4dbb531`
- Actual comparison base and parent: `dbb813fd625f369f76b5e50fe3d59eeaf5f8ad75`, tree `4f1f48294def701e26d5eb0d7498a2d9621375e5`
- Current Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Refreshed remote candidate and canonical resolve to the exact SHAs above; the candidate is a clean child of the stated base.

The code diff adds `DailyV1Application.TryCaptureAndValidateRestoredImageOwnerVector` and routes the image-restore path through it. The other changed files are the validation record and its evidence package. No test source changed in this candidate.

## Implementation review

The new operation first requires the application's exact initial/replacement staging authority. It checks that the candidate session's `ExecutionBoundaryState` is the same object exposed by the host census port, captures the private target's unadmitted owner vector and epoch through that port, and compares the vector to the persisted image under the same application reservation. On success, the coordinator retains that vector/epoch for `TryFinalizeRestoredCandidateBoundary`; finalization binds the exact runtime host boundary, binds the mutation guard, recaptures and matches the vector/epoch, then admits the restored boundary. The coordinator assigns the candidate to its output only after all validation succeeds. The fallback host path continues to capture and compare its vector directly. These checks appear fail-closed on inspection and preserve the one-publication boundary.

The current P12 restore contract requires the exact target vector to be validated, recaptured after guard binding, admitted only after it still matches, and the old session to remain untouched on any failure. The following findings prevent acceptance of this candidate.

## Findings

### 1. The owner-vector validation stage is emitted twice and too early

In `P12GDailyV1RestoreCoordinator.ImageRestore.cs`, both branches emit `TargetOwnerVectorValidated` before the shared target checks complete:

- The application path maps the helper's `Validated` callback to `TargetOwnerVectorValidated` (around lines 335–337).
- The fallback path emits the same stage directly after the vector comparison (around line 366).
- The shared path emits `TargetOwnerVectorValidated` again after the Expedition exact-zero, allocator high-water, sentinel, and political-binding checks (around line 379).

The prior behavior emitted this stage once, after those checks. The candidate therefore reports duplicate completion and exposes it before all target validation is complete in both the application and fallback paths. Preserve the stage contract by emitting the observable stage once after the shared checks; the helper may perform its Core comparison without independently publishing that later coordinator stage.

### 2. CoreValidation evidence does not identify the candidate's application source bytes

The validation record reports CoreValidation cold/warm **936/936** and cites private manifest SHA-256 `ced8a84921f56d2b6a243b61fb97af83d840da2e551e0698cd38553802c38f4d`. The private manifest is stable and reports the run's source commit/tree as the stated base `dbb813fd` / `4f1f4829`, with working-tree changes listed.

However, its compile-input entry for `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` is **105,962 bytes**, SHA-256 `99df35ab858a1aea865025130584d025e0dd5001242d8daeb02621f40e77a63e`. The candidate's file is **105,815 bytes** in the Git tree and its SHA-256 is `cd23f3eb8c845667167c30666b51967ece224f9599c0905ca57f41da72a94af8`, matching the validation record's changed-source identity and Unity input manifest. The private CoreValidation input therefore does not match the candidate's application source bytes. The 936/936 result cannot be counted as exact-candidate CoreValidation evidence until the official runner records and tests the candidate's exact source inputs.

### 3. The reported mutation-atomicity test is not the new image/application path

The private run manifest `4574d41e0107b36340d2c3b642c87df6dc7406482efb04d846aa2ad073a52a3a` reports one passing case: `SimulationRuntimeAdmissionTests.DailyV1RestoreRejectsPrivateTargetRosterMutationAfterOwnerVectorCaptureAndKeepsSourceRetryable`. That test calls the in-memory `TryCreateRestoredSession` path with no `DailyV1Application`; it exercises the existing fallback, not `TryCreateRestoredSessionFromImage(..., application: ...)` or the new Core capture operation.

The focused successful image restore test and the full EditMode run do exercise the new application path. They prove successful capture/restoration, but there is no exact-path failure injection after the image path captures its target vector. Add or identify a test for that route that mutates the private target after capture and verifies rejection, no publication, preservation of the source session/token, and successful retry.

## Validation and artifact review

- Focused image restore: **1/1 PASS**, run-manifest SHA-256 `7790bba9d786e1942fdf4586e211dd2126dccb727f2721bb4ced143234c96908`.
- The focused mutation-atomicity case above: **1/1 PASS**, with the path limitation in Finding 3.
- Full Unity EditMode: **3279/3279 PASS**, zero failed/skipped/inconclusive; input digest `6051ad3f0051becc1af6328b8a2489a85d96210ca2ae3b378e86483ad79a4c61`, run-manifest SHA-256 `2011c25899dabfe12b0812b8c6a595f171b171a35efda7e6da8b53ace3f89437`.
- Official Smoke filter: **5/5 PASS**, zero failed/skipped/inconclusive; same input digest, run-manifest SHA-256 `b8c0695faae27a87bc5214a1fa769d1c20ac2e565f8eba957debcc41958f9893`. A preliminary category-filter run selected zero tests and is not counted.
- Candidate-range `git diff --check`: **PASS**.
- The published EditMode archive SHA-256 is `7b3eb8c9101a8af55904b1ec3fabce2c4237df49b60286e9cb45da101ed206b8`; manifest SHA-256 is `ba71ce5b8252c267e4208ba0a843903347c7a45b63ad0aac56c4f4a8b932e993`. The approved verifier passed with private original comparison (`runs=1`, `entries=1`). The package contains one normalized XML result entry; all 3,279 cases passed. A path/credential-pattern scan passed. Original reports/logs remain in protected retention and were not changed.

The full Unity input manifest binds the two changed production files to this candidate's source hashes. The CoreValidation discrepancy above is independent of that Unity evidence. No candidate source or evidence was changed during this review.

## Disposition

Resolve the duplicate/early stage notification, provide exact-candidate CoreValidation evidence, and add exact-image-path mutation-atomicity coverage before recording `VALIDATED_CANDIDATE`. This is a bounded restore integration only; it does not promote the candidate, complete P12-E/P12-G or CORE-F, or close a Phase. Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
