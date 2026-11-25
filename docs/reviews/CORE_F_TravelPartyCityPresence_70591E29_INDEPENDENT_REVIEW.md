# CORE-F TravelParty City Presence — independent review

Status: **PASS**

## Exact review target

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Base: `174aa7bb5e7a7b3a2eda5ac8b5ab26eb4b5905f4`
- Reviewed candidate: `70591e299004e6d58780bba3184f8416655415b0`
- Reviewed code tree: `c8eef3741180f78ed2e92c19d64804299a954910`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Reviewed files: `DailyV1ExecutionBoundaryState.cs` and `DomainEventAuthorityCoreTests.cs`.

## Review result

Independent exact-tip review found no actionable defects. Core binds each City ImportantNpc baseline to the exact selected City presence owner. A receipt is accepted only when cardinality changes by one membership and the owner revision advances by one. Sequential receipts advance the local baseline. Unrecorded drift and a multi-revision change represented by one receipt fault closed. The complete host-vector preflight remains intact; compatibility-roster and other non-Core census rows continue through the existing host protocol. The added tests cover sequential add/remove receipts, unrecorded drift, and multi-revision rejection. Existing assertions were not weakened.

## Validation evidence

The validation and package record is [VALIDATION.md](../validation/CORE_F_TravelPartyCityPresence_70591E29/VALIDATION.md). The independent reviewer verified that CoreValidation's source hashes for both changed files match the committed files; cold and warm runs each passed 1,040/1,040. The official Unity EditMode package points to this exact commit and tree and reports 3,300/3,300. The official filtered Smoke run reports 5/5 against the same commit, tree, and stable input snapshot; its five identities occur as passed in the full EditMode report. `git diff --check` passes.

## Limits

This review covers only the bounded selected City presence receipt behavior. It does not approve canonical promotion or claim P12/CORE-F completion, full owner-vector authority in Core, detached hosting, or broader persistence readiness.
