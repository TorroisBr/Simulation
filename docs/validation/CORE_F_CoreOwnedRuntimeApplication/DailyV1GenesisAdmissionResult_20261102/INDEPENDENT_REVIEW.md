# Independent candidate review

## Verdict

`VALIDATED_CANDIDATE` for the bounded Core genesis-admission-result correction. This verdict does not mean CORE-F is complete or approve canonical promotion.

## Exact identities

- Review base: `520abace6ad2c3e8cb1d1bccd34a6e522a5d6a70`
- Corrected source commit: `a237eb243b6dddbcd69d0a81ae7cea2fc0716603`
- Exact reviewed candidate/evidence tip: `c501c71504c66e71703b4da9d4b44d798d8702b7`
- Reviewed tree: `994d32665b45862f45020ed918dfafbaab1b6288`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Reviewer: independent candidate-review agent

## Findings and evidence

The prior synthetic-`NoSave` finding is resolved. The application stores the actual initial slot inspection/load result, requires a completed unblocked `NoSave` before genesis, rechecks that the same result remains current after preparation, and returns that same result instance. The regression asserts reference identity. Present, corrupt, incompatible, or inaccessible slots still block genesis. The candidate remains unpublished during preparation.

The exact corrected source delta and its validation record/package were reviewed. The Unity source hashes match the committed files; the normalized full EditMode package matches its manifest and archive digest; its report and package contain no local workstation or private evidence paths. Full EditMode passed `3296/3296`; the correct Smoke filter passed `5/5`. CoreValidation passed `995/995` cold and warm; its native evidence remains private, and no public TRX package is claimed. The filtered Smoke package rejection is disclosed in the validation record. The three ProjectSettings edits and all unrelated/untracked evidence were not included in the candidate commits.

## Scope and constraints

This review covers the bounded admission-result correction over the previously recorded start-preparation checkpoint. It does not re-review every historical file in the cumulative CORE-F branch and does not imply that public `DailyV1Application.TryStart`, Core-owned detached restoration, an independent .NET host, separate-process continuation, or full CORE-F acceptance has been delivered. The prior reviewer finding is closed; those remaining integration requirements stay open.
