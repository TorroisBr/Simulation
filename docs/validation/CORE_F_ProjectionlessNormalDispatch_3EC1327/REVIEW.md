# CORE-F Projectionless Normal Dispatch Review

## Review identity

- Verdict: `NEEDS_CHANGES`
- Base commit: `838122e18dd615d81cf042395575550386caaa43`
- Candidate commit: `3ec1327c3cf4138d6ad380e2179853af9b0a51cc`
- Candidate tree: `8477cce1b24eebee0c6b999f563181b2fe0a53a6`
- Current Core canonical during review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Corrective commit: `48fe593724a01decab5859b0f0ff8ed151da2545`
- Corrective tree: `dc72b846d2ac5a21811ba1df8839347472110c27` (identical to the reviewed base tree)

## Finding

The candidate skipped `IDailyV1SelectedActorActionExecutionBoundary.TryCompleteDispatch` when an action was projectionless and Core-owned `Normal`. That callback is also the host result-reporting path: Unity formats the actor/action message from `NormalActionLogText` and logs it even when the Unity action runtime and action object are null. No equivalent Core host-reporting route existed for the projectionless path. Skipping the callback therefore dropped accepted Normal action result output.

Keep result reporting independent from the Unity action projection. Either retain the completion callback as a host reporting port or route the exact result through an explicit host output port, with a regression asserting the result reaches that route. Unity adapter call sites that explicitly passed `false` preserved their direct behavior but did not resolve this finding.

## Validation evidence

- CoreValidation: cold 916/916 and warm 916/916 passed; the changed Core and test file hashes matched the candidate implementation.
- Unity EditMode: 3275/3275 passed on the final candidate source inputs.
- Official Smoke: 5/5 passed on the final candidate source inputs.
- `git diff --check`: passed.
- An earlier Unity attempt produced no test XML because the three Unity adapter call sites still used the old method signature. The candidate was subsequently corrected and the full EditMode/Smoke runs passed.
- Validation manifests and original reports remain in the protected local evidence store; no raw logs or workstation paths are published in this record.

## Disposition

The candidate was not promoted. Commit `48fe593` preserves the published branch history and restores the source tree to the exact reviewed base tree. The NEEDS_CHANGES finding remains a constraint for future projectionless action-result integration; this record does not change CORE-F readiness or canonical status.
