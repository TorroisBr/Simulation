# Independent review — CORE-F TravelParty start Core protocol

Date: 2026-11-08
Verdict: PASS; no actionable findings

## Exact source identity reviewed

- Base: `a644be627ff866b3f81564abcb62e7c625afa2ec`
- Implementation commit: `fa0cedebdf46271cd3663f1694f51408aadd2e14`
- Implementation tree: `45f168ba888d0abf39e0e03aa9f7d1fd8e7673ad`
- Candidate tip, including validation record: `acd4b94f795ecec0d5824097ed4ec4a512f46310`
- Candidate tree: `a439956ad43c9a11ad40366e2da2642ab0c09351`

## Review findings

The independent review confirmed that the Core path requires the fully selected roster and selected origin to match the exact Core owner graph and checks exact P12 section-set parity. Mixed or unselected rosters continue through the existing host fallback; foreign Core bindings fail closed.

Core captures owner baselines, validates owner identity/cardinality/revision, checks operation/context conflicts and active epoch reservations, enforces mutation receipts, and detects unreceipted drift both before commit and at scope exit. Standalone operation entry without an enclosing DailyAdvance is covered. The added drift test verifies rejection and fault closure. Existing assertions were not weakened.

The validation record matches all four changed source/test SHA-256 values, is bounded to this checkpoint, and contains no workstation paths or secrets. The implementation diff passes `git diff --check`.

## Independent artifact verification

A read-only follow-up verified the preserved Core TRX, Unity XML, manifests, and logs against the validation record. Report counters were:

- CoreValidation cold: 1,059/1,059; warm: 1,059/1,059.
- P12 restore filter: 123/123.
- Full Unity EditMode: 3,300/3,300.
- Official Smoke: 5/5.

All failed and skipped counts were zero. Manifest run IDs, the recorded Core base/tree and input digest, and the stable Unity project-input digest matched. Each changed source hash was present in an applicable manifest. Tests were not rerun. The reviewer did not emit private paths or artifact contents.

## Limitations

This review accepts only the bounded selected TravelParty-start P12 protocol checkpoint. It does not claim full CORE-F acceptance or authorize Core canonical promotion.
