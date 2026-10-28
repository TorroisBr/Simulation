# CORE-F selected Daily-v1 LocalTopology exact-empty owner review

**Verdict:** `VALIDATED_CANDIDATE`

## Reviewed identity

- Candidate branch: `codex/core/CORE-F-p12d-local-topology-empty-owner`
- Exact reviewed candidate: `cc6aa06de9755f3b4e7abd57ec0aa0d84a435cfd`
- Candidate tree: `b80987ad2f0e5c518739b68f06171ce9e2570f44`
- Implementation/test source tree: `d8828a60ad2488cabd34526071129809045206d5`
- Parent/base: `ced294c0f2bd56b9b585f882e21bb01c23a1f177`
- Core canonical checked during review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Candidate remote ref matched the reviewed commit.

## Independent review result

The reviewer inspected the complete delta against the actual base, current selected-profile contracts, changed production/test source, and validation artifacts. No blocking correctness, accepted-scope, or test-integrity findings remain.

The selected Daily-v1 LocalTopology places and connections rows both identify one immutable Core owner bound to the exact NPC RuntimeId. They remain Required, cardinality zero, revision zero, and aliased to the same owner instance. Census and capture reject a selected Unity projection that does not match that exact empty identity/state. The unselected/legacy owner path is unchanged. Existing P12 payload/schema behavior and P10 non-empty topology remain outside the change.

All changed assertions preserve Required role, exact cardinality/revision, alias, empty projection, and rejection semantics. The tests do not weaken existing content or persistence claims. The reviewer independently confirmed the cold/warm CoreValidation, full EditMode, Smoke, input stability, and sanitized full-suite package evidence listed in `docs/validation/CORE_F_P12D_LocalTopologyEmptyOwner/VALIDATION.md`.

The only nonblocking note is that the new Core owner source has no final newline. It was left unchanged after review so the reviewed source tree remains exact; any future source edit can normalize it and obtain fresh affected validation/review.

## Evidence and limits

- CoreValidation: 818/818 cold and warm; run `c49eed018553438990c49165c51793e4`.
- Full Unity EditMode: 3266/3266; run `d880644c1ba44026ac6699f399efd782`.
- Official Smoke: 5/5; run `2c18c0cecd7145ec9c8598eba43e5a6d`.
- Verified sanitized full EditMode archive SHA-256: `2e4ef418c6ca20dafc1c92813992dcc9d48a59fa40d54c861998f730074d3817`.
- Protected original reports remain retained and unchanged in owner-only evidence storage.
- One earlier CoreValidation invocation was excluded after resolving relative project paths from the wrong working directory and failing before test execution at the Lab restore step. The accepted run above was executed from the E: candidate with E:-local temp, package, output, and evidence paths. The failed attempt was not used as evidence and its outputs were not cleaned.

This review validates only the bounded exact-empty selected-profile owner slice. It does not promote Core canonical, complete P12-D, change P12-A/P12-G readiness, or close CORE-F. The separate full CORE-F shared application/host gates remain outstanding.