# LAB-OBS-A Facade — Refreshed Exact-Tip Review

**Verdict:** `VALIDATED_CANDIDATE`. No actionable implementation, test-integrity, evidence-integrity, or scope findings. This review does not promote the candidate or declare the wider LAB-OBS-A workstream complete.

## Reviewed identities

- Candidate branch: `codex/core/LAB-OBS-A-coherent-observation-facade-363d3646`
- Candidate commit/tree: `46503a0a82c0ef514061e0a0836d22d3017f0f08` / `f3d159893b7eda40f3e7aabe52d4c1fc0c8847f6`
- Base and remote-tracking Core canonical at review: `363d3646a68ac650a942f649d849739d9ced06e4`
- Candidate parent and merge base: `363d3646a68ac650a942f649d849739d9ced06e4`
- Previously reviewed candidate: `273badbec178f5b1fcfc3f94f197f7fc1f615b81`
- Validated implementation source: `f8d8e1c02fe7ea8493019f482adb37af14555b0f`, tree `e0eaa3ad7ac35de1b96dfce786c68576a35340e1`

The candidate checkout was clean. The complete six-path base diff is limited to the DailyV1Application integration, the observation-run implementation/tests and their explicit project compile lists, and the validation record. `git diff --check` passes. The candidate does not change the Core/Lab profile tooling or `docs/CORE_STATE.md`; all five relevant tooling/State blobs match the base.

The five executable source/project/test blobs match both `273badbe` and `f8d8e1c` exactly:

- DailyV1Application: `2efd32d2d901095cb5da3f189e670cec4c5cc991`
- Observation run: `1cdf6ea14ff42ca4f9aed5fe7339b3dae226fa73`
- Core project: `af9116c686220f7a32945a79aaf4a18febdae258`
- Observation tests: `1c969958f771057f62a9c6fd5c298a8f7bb343c9`
- Test project: `957d3b813ec79233175c52a5fed23668bcb6ac11`

The refreshed-base delta from `08f6700` to `363d3646` contains only the already-reviewed Core/Lab TRX profile files and Core State. Thus no intervening product or test input invalidates the prior implementation review or exact-source evidence. The prior code review found no defects; this review verifies that the code remains byte-identical and that the evidence gap is now closed.

## Validation applicability

- **CoreValidation:** run `f29d80b3befd4c759f1d98ddc4c3c2a7`; source `f8d8e1c` / tree `e0eaa3ad`; input digest `4f589d70acac822214018555d3a7dc5bcbca9ef679ac20096812293d75cad13f`; cold and warm each `1,229/1,229`. The native manifest hash is `90f9e405a034d641f9fa53e35cec2da53205aa4c330f5d5bf51bda329db254e3`; `LibraryAbsent=True`.
- The Core/Lab `core-lab-15-artifacts-v1` package is bound to reviewed profile tool commit `733e228e7d8ca5c76cc4b2e3719446bd37210c17`. Those four tool blobs are unchanged in the current canonical base and candidate. The official verifier record reports valid package integrity, verified original comparison, one run, two entries, and `gatePassed`. ZIP SHA-256: `58dec316d1de34327bbc91dff5df90ddb3d7c9fa89b37ff0da0b9d9440f7db57`; manifest SHA-256: `4c71ae58b86fd4e23e2966cf0cc6f6c4124896f9ad8fd93c89d2844d6cecfe4f`. All 17 manifest-bound original reports/logs were preserved byte-for-byte. The public archive contains only normalized cold/warm reports.
- **Unity EditMode:** run `742e61a93faa449a861beffecc2f6871`, `3,327/3,327`, on stable input digest `14fd8f877b520a75c3017b629c231494519b9f64acab85a2bf55a7fe504234ac`.
- **Smoke:** run `02147942257041868190df5dc8961c6d`, `5/5` on the same Unity input digest. The five Smoke cases also pass in the full EditMode report; the original filtered XML remains preserved and no synthetic or repaired XML is claimed.

No validation rerun was necessary for this refreshed candidate: its executable inputs match the tested source, the intervening canonical delta is evidence tooling/State only, and the Unity input digest is unchanged. The official Core+Lab verifier result is from the preserved run/package record; this review did not rerun tests or mutate evidence.

## Findings and integration constraints

- Code/test findings: none.
- The validation file's latest pre-review note that exact-tip review was pending is superseded by this exact-tip review record.
- The local `codex/core/canonical` ref observed during review still pointed to `08f6700`; `origin/codex/core/canonical` and the candidate base were `363d3646`. Refresh refs immediately before any integration or promotion. No fetch was performed during this read-only review.
- LAB-OBS-A remains limited to this Core facade input. The official producer/encoder, remaining observation obligations, and any wider Atlas work are not implied complete. No canonical promotion was performed.