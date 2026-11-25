# CORE-F Core-Composed Resident Death Application Advance — Independent Review

## Verdict

`VALIDATED_CANDIDATE` — no actionable implementation or test findings.

## Reviewed identity

- Reviewed candidate: `1b4e1f92a3b61363cefe411d015bc292ae9b47a7`.
- Candidate tree: `9a156e68f0f216fb8a610bbc219f443e5d518473`.
- Base: `bb70de35913e527c5c75c36752923ea85ca0cfec`.
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Scope reviewed: the complete candidate diff, application-level resident-death regression, test-seam changes, validation record, and sanitized EditMode package.

## Findings

The regression exercises `DailyV1Application.TryAdvanceDay` through the published Core session. Deterministic mortality reaches the Core-composed resident-death stage without a Unity demography projection. Assertions cover exact Person/NPC/City bindings, owner revisions, one mutation epoch, the completed-day capture token, released operation/lease/reservation state, a healthy census protocol, and complete post-advance census capture. Test-helper changes are additive and retain their previous defaults. Existing assertions were not weakened.

Validation hashes and counts in `VALIDATION.md` match the retained evidence. The official filtered Smoke run passed 5/5 with stable inputs, but the official packager rejected its XML because ancestor-suite testcase counts disagree with the filtered cases. That report was not rewritten or published. The five exact passing Smoke cases also appear in the verified full EditMode bundle. This is retained as an explicit evidence limitation; no standalone sanitized Smoke package is claimed.

The candidate was reviewed read-only. No files were changed by the reviewer.