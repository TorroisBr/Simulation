# Independent exact-tip applicability review: CORE-B evidence bundle filenames

Date: 2026-10-14

## Exact target and authorities

- Candidate branch: `codex/core/CORE-B-secure-evidence-directory`
- Candidate commit: `4276bc931c2119e22a0bebcec26b90c085aa9483` (tree `8ce090a8b363c81bde31cf00889b6d5fa9d3d2e5`)
- Direct base: documentation/package candidate `9837c76f2807e039328ce508028b6a71588ce4b3` (tree `4e1d5558243eec1f8e610d31795e42d4b3fa5f50`)
- Reviewed implementation and package source: `15833b01830d0a8789803af0e28e0e5529ddf916` (tree `163c296a530c477a968f2326166cf62a7a26366f`)
- Current canonical verified at review: `origin/codex/core/canonical` = `c95752846a0ce0e1ebf985c4d79e0e17b02bb7af`.
- Prior exact implementation review: `79460dbffef35eeaa85f814e11e2a7b6a6bc894f`; prior package applicability review: `2f79df26d99ae64329bfc40c7b7e59105f3d5eb4`.
- Reviewer: independent Codex reviewer; candidate and canonical refs were not modified.

## Verdict

**VALIDATED_CANDIDATE — PASS for this exact filename/docs successor.** The rename correctly binds the committed paths to the manifest’s declared archive name. Package bytes, code, tests and source identity are unchanged. This review does not promote CORE-B or close a Phase.

## Delta and package binding

The exact delta from `9837c76` consists of one validation-record path correction plus 100%-identical renames of the manifest and ZIP. The new paths are `docs/validation/COREB_DeterministicTemporal/coreb-coldwarm-74920c53a6894ab1a1b7d942748a02b2.manifest.json` and the matching `.zip`. The manifest bundle ID is `coreb-coldwarm-74920c53a6894ab1a1b7d942748a02b2`, and its declared archive filename exactly matches the renamed ZIP. No old filenames remain in the relevant state or validation records. All code, test and tooling paths are identical to the parent candidate; `git diff --check` passes.

The renamed manifest and ZIP have the same Git blob identities as the previously reviewed candidate and match the corresponding retained private-run files byte-for-byte. Their SHA-256 values remain `16f6331cc36c0c01f953e2cda09d71139824bf4ef92ab710287e8e4d9f3e3044` and `b3c745f1c45acaf88397d325c6cc09ae45bfd37d11a2222b757b0b35ec7779a3`; the ZIP is 39,702 bytes. The ZIP’s two normalized report entries match the manifest’s lengths and hashes.

The manifest remains bound to source commit/tree `15833b01830d0a8789803af0e28e0e5529ddf916` / `163c296a530c477a968f2326166cf62a7a26366f` and stable input digest `66f27ccbc8d032ba766446ffc143aec4541b2de5d14db23e74c04938016ef786`. Cold then warm each completed with 97/97 passed; failed, not-executed, inconclusive, timeout, error and aborted counts are all zero. The three unrelated user `.meta` files remain outside the candidate.

## Verification and privacy boundary

Archive-only Verify on this exact checkout returned `VALID` for one run and two entries, with `originalComparisonNotPerformed`. That status is expected: this verification checks the renamed archive and manifest without repeating the private source-context comparison. The original cold/warm reports remain private; their recorded sizes and hashes match the retained originals. Their package-time comparison to the normalized reports was recorded as `originalComparisonVerified` in the exact implementation review `79460db`. This review makes no new private source-comparison claim.

The archive contains only the two normalized cold/warm reports. Scans of the committed public manifest and both archive reports found no drive or UNC paths and no credential-pattern matches. The validation path corrections add no absolute local paths, raw original TRX files, or secrets.

## State and limits

The validation record preserves the reviewed code tip, package/run identities, protected retention boundary, prior package-time comparison, and the candidate’s unpromoted status. `CORE_STATE.md` continues to state that canonical promotion is pending and makes no P12 readiness claim. No Unity or .NET suites were rerun because this successor changes only documentation and package filenames; the exact archive verifier was run.
