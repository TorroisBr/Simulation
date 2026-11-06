# CORE-F Restored Core Target Invariant Review Fix — Independent Review

## Verdict

PASS — VALIDATED_CANDIDATE for the bounded restored-target invariant test correction.

## Exact identities

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`.
- Candidate commit: `d2205c4ae53b7817dcd3ea9816135f0a1f9ba2df`.
- Candidate tree: `b98ddf4870fa4648b19e996eb9d0ea9acf8bf0b6`.
- Candidate parent and test-only review base: `70290dee1a62b36afc724a2fb266f87f431b0028`.
- Underlying implementation base: `1468b8cfa6ee3802c8e8bb92821310aeabbe8d45`.
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Origin candidate ref was verified at the exact candidate commit; Core canonical ref matched the SHA above.
- Review was independent of the implementation and validation authors. This review covered the cumulative invariant implementation and the follow-up test/evidence delta. The production source was unchanged from `70290dee`; the prior test finding was the uncovered conditional P8 path and absence of a populated Required P11 positive case.

## Review findings

The added P8 test binds a position store to the exact session PersonStore, binds the anchor store to the same P8 spatial authority, and demonstrates success for exact-zero `ExplicitlyEmpty` rows. It rejects omitted position and anchor rows, nonzero cardinalities for each, a wrong position owner identity, and a wrong anchor role. These cases exercise the conditional Core-owned P8 branches and shared exact-witness checks. The accepted P12 owner matrix defines both rows as required exact-zero witnesses.

The populated P11 test obtains the exact selected `ActorChoiceStore`, captures one input, observes cardinality one, and validates a matching `Required` target row. It demonstrates that the Core target gate compares the actual owner identity/cardinality/revision without imposing zero cardinality. The P12-F staging contract separately rejects pending histories; this direct invariant-gate test is not an end-to-end persisted restore case and is not reported as one.

Production behavior is unchanged from the reviewed `70290dee` candidate. The invariant gate preserves P8 conditional ownership, P11 Required semantics, and the excluded P18 temporal exact-zero check. No unresolved findings remain for this bounded slice.

The candidate delta from `70290dee` contains only the test source and its validation record plus DotNet package/manifest. `ProjectSettings/ProjectSettings.asset`, private evidence, untracked files, and `.meta` files remain excluded and untouched.

## Validation and evidence

The changed test source SHA-256 is `771f6a6a250dee8bae3826c3b0e6f529bb836c194a1089b37f95aa5e7e59f2ce`. It matches the validation record and the CoreValidation manifest. The unchanged production source SHA-256 remains `4a5d0203382c39182796363adbd34b785bd9427bfb7f2f4a65fcb8279b7536b4`.

Official CoreValidation run `a64dabe2f91148079908d6ede49e5b0a` passed cold and warm, 1,022/1,022 each, with zero failures, skips, or inconclusive/not-executed results. Its stable input digest is `2e3c28e9fc43aa7d6246575842f6ee91f3ed32aeeaa25bba6eead7b48aae21d6`; the package manifest records source-projection verification for both original reports. Original reports and the protected runner manifest remain in protected evidence storage.

The committed DotNet package archive SHA-256 is `57d8430e80ecf320314dd689454f33ce87775b3940c8afa62af94fc3b11420db`; manifest SHA-256 is `93f075c1cfcf6f885316f880bd9c45843db172c51ea2be755f2132938b37bad4`. Both cold/warm archive entries match manifest byte lengths and SHA-256 values. A content scan of the public record, manifest, and packaged reports found no workstation paths or credential-like patterns.

No Unity production or Unity test source changed in this test-only follow-up. The prior Unity package is explicitly historical because its ProjectSettings snapshot differs from the preserved user file; it is not claimed as validation of this follow-up. No Unity tests were rerun for this review.

## Scope limit

This PASS applies only to the restored Core target invariant gate and its test correction. It does not approve Core canonical promotion, claim detached-host acceptance, or complete CORE-F.
