# CORE-F restored-image owner-vector integration — exact-tip review

**Verdict: `VALIDATED_CANDIDATE` for this bounded restoration change.**

Review date: 2026-10-30
Reviewer: independent read-only review

## Exact identities

- Repository origin: `https://github.com/TorroisBr/Simulation`
- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Candidate commit: `65efea72e55136d026866b1870e742efa2f8cd4a`
- Candidate tree: `1551584efae93f8e21e16099805b9ca213b58a5d`
- Exact parent/base: `b4e04296cf254e8f5e2124e1f96af1a19e7aaf11`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Refreshed origin candidate and canonical refs matched those identities.
- The candidate is a clean fast-forward child of the stated base. The review did not modify candidate code or protected evidence.

The complete six-file diff was reviewed against the exact parent. It contains two production files, one additive Unity test change, and the validation record plus its public manifest/archive. No existing assertion was weakened or removed.

## Prior findings resolved

The prior exact-tip review `d565e2ffe4971e4ade0fe3e9de33e7a6fb50bca3` identified three issues. This candidate resolves them:

1. **Owner-vector stage notification:** `TryCaptureAndValidateRestoredImageOwnerVector` now reports only the capture callback. The image-application and legacy fallback paths both report `TargetOwnerVectorValidated` at the common point after the shared exact-zero, allocator high-water, sentinel, and political-binding checks. The successful persisted-image test asserts the stage occurs once, after candidate composition and before spatial validation. A failed comparison or failed common check does not report validation complete.
2. **CoreValidation source identity:** the official cold/warm run's private manifest is SHA-256 `efbf6bfa71a765766eaaf562f7a4bba5685ed40fd7413821413d55e4ceb4c8c5`, with stable working-input digest `9809e4fac912d0aceed14f7806a085f419da7e07e1d912117c046fbef7e4ee0f`. Both runs passed 936/936. Its `DailyV1Application.cs` compile input is 105,717 bytes with SHA-256 `eee3a3c5f6ced17c22fc0b8ae48bbbd02ac28863750f0682a90e5e33e81af55d`, matching the candidate's normalized source and the Unity input snapshot. The retained cold and warm TRX files match their recorded hashes and each reports 936 passed, zero failed.
3. **Persisted-image/Application atomicity:** the new `CoreImageRestoreRejectsPrivateTargetRosterMutationAfterOwnerVectorCaptureAtomically` test decodes a completed persisted image and exercises `TryCreateRestoredSessionFromImage` with a real `DailyV1Application`. It mutates the private target roster after the first owner-vector capture. The initial vector comparison succeeds, then finalization binds the candidate's private mutation guard and detects the changed vector on recapture. The test correctly permits this guard binding on the discarded private candidate, while asserting restore failure, null returned candidate, no recapture-success or boundary-admission stage, no active-session publication, and byte-identical source image on re-encoding. This tests the actual image/Application route and the failure boundary without requiring rollback of private candidate state.

The test-source identity in the full Unity run is also attributable to this candidate. The protected runner snapshot records the source file as 45,450 bytes, SHA-256 `6bd710d3097a2527e198da94d8d9acd11117f0993852cc8ab306f20a9d276910`. That exact pre-commit file is retained and matches the run manifest; after line-ending normalization its UTF-8 text equals the candidate's clean committed source. The candidate Git blob uses normalized LF while the Windows checkout expands line endings, so raw working-file bytes differ without a source-text change.

## Implementation and contract assessment

The application still requires its exact restored-continuation staging authority and verifies that the candidate and host census port share the same `ExecutionBoundaryState` instance. It obtains the private target census through the host port, compares it to the persisted image under that authority, and passes the accepted vector/epoch to finalization. Finalization remains responsible for binding the candidate guard, recapturing and matching the vector/epoch, and only then admitting the restored boundary. The coordinator returns the candidate only after the remaining binding, spatial, roster, owner, and boundary checks pass. The non-Application fallback retains its prior direct capture/validation/finalization sequence.

The injected roster change is confined to the private candidate. It cannot replace the source or active session and is rejected before the candidate can be returned or published. The added `privateCandidateObserverForTest` is an internal test seam on the internal image-restore path; no public application API or persistence semantics changed. The edit preserves the existing image bytes, owner identities, revisions, capture token, and restored-boundary admission sequence.

## Validation and evidence

- Official CoreValidation: cold **936/936 PASS**, warm **936/936 PASS**; stable input digest above. The exact changed Core source input is bound by the private manifest. The original TRX SHA-256 values are `77a0730a53109687cc91996198fa20cbb667cc6f0c5fe5c2da79e7dc38a51270` and `e7a2af0de0a0d1c8b4e752cd7bbf70429f911f39e2c9a07feeef449eae81fbed`.
- Persisted-image roster-mutation atomicity: **1/1 PASS**; private run-manifest SHA-256 `e7f7c8e7548c3909fde95f8180bad1e035c05f7843d1b11cca6fa9f6475036a0`; XML SHA-256 `bd634b15adbf2cdbf8ef174f4be4f87d006ef607c8e77520fbedff71a2a65d77`.
- P12-G restore fixture: **16/16 PASS**; private run-manifest SHA-256 `b23430de60ba4cc2fb7bbad03d2d896a151374a29263b6344970a08c3aa57043`.
- Full Unity EditMode: **3280/3280 PASS**, stable input snapshot. The exact full-run XML is retained in the package below.
- Official Smoke filter: **5/5 PASS**; private run-manifest SHA-256 `ee0e2a1d76cb8d439f4637988e45593573bfdf6e8ebe53ffb57cf4812063925d`. The five passing Smoke identities also occur in the full EditMode report.
- `git diff --check`: PASS.
- Official evidence verifier: `VALID`, one run and one report entry. Archive SHA-256 `4a7fc953640da173a486042b9f95e711a4668cc1b1eca8538d0459c85e8c9d8d`; public manifest SHA-256 `7dbeaf7cd43e7d988373d1997a1d10e2ee4e5b40bf59a6b452620dd3e1c4c58c`.
- The packaged XML contains 3,280/3,280 passing test cases and no workstation absolute paths. The public manifest contains no workstation paths or credential-pattern matches. Original XML, logs, and private manifests remain in protected storage and were not modified or published.

## Disposition

No actionable finding remains for this exact bounded restored-image owner-vector change. This verdict does not promote the candidate, complete P12-E/P12-G or CORE-F, or establish broader host/persistence readiness. Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
