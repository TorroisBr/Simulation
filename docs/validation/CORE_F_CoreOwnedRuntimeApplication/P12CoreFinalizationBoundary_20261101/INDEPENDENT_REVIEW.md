# Independent exact-tip review — P12 Core restored-boundary finalization

- Candidate commit: `7ce398a75d6b363c2d99b837e863aef34068b34a`
- Candidate tree: `e3cc0e8b91d4fa671171e2b2939ac723c89200e3`
- Review verdict: **PASS / VALIDATED_CANDIDATE**
- Scope: review of the bounded Core application finalization used by capture-bound P12 restore.

## Findings

No actionable code findings.

The Core application requires either an unused current-thread bootstrap boundary or the exact current replacement session with its live same-thread exclusive reservation and healthy completed source boundary. The candidate execution state must be the same object used by the host ports; the candidate is bound to that exact host-port runtime. Core binds the mutation guards, performs two complete matching owner-vector recaptures, and admits the restored completed-boundary token only for the exact world, day, preserved positive sequence, and unadmitted full census. The restore caller supplies the candidate runtime as host ports, and later projection/session publication rechecks reference identity before commit.

The `application == null` compatibility/test branch retains the existing guard binding, recapture, and admission sequence. Existing source-token, target-health, and publication checks remain fail-closed.

## Evidence applicability

The changed source SHA-256 `96671882a6d289d08cfedd5bce7ccc77592f733b374015252392d4658a6b6fdc` matches the candidate blob and validation input snapshot. Validation record:
`VALIDATION.md` at candidate tree above.

The published full EditMode package manifest SHA-256 is `56830d3d1a3d6a2bed62d673755bc95a57edf40959ca2a72228eeaccc9fba092`; archive SHA-256 is `4a8a15b539aa5a95e102a66c092a6bc1492d7fbf0d9a4e5cc2b95ea4ac254bb9`. The official verifier returned `VALID` for one normalized result; the manifest records `3294/3294 PASS` and stable source input. Focused admission is `241/241 PASS`; Smoke is `5/5 PASS`. `git diff --check` passed. No tests were rerun for this independent review.

This record does not claim full CORE-F completion or Core canonical promotion.
