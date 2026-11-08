# Independent review — CORE-F TravelParty start compensation

Date: 2026-11-08
Verdict: PASS; no actionable findings

## Exact identity reviewed

- Base: `d7d697c628838decb10390beb5679f1b8071807a`
- Implementation commit: `aab04809c465fe110d9424c3f3500d70f86afc82`
- Implementation tree: `a566a7f3a423aa0165374db908e13cbe39853929`
- Candidate tip, including records: `4bd8d800c9d063dea7379582f031743d889f9e88`
- Candidate tree: `044f29d053b364a77b11a9e98fde28e49ec4ec58`

## Review findings

The selected Core receipt accepts one TravelParty-store addition with exactly one revision increment, then permits at most one compensating removal after that receipt. The removal must increment revision exactly once and restore the operation''s initial store cardinality. Removal without a receipted addition, repeated additions/removals, and other cardinality changes remain rejected.

The new selected-path regression performs the add and removal receipts, verifies restored cardinality and revision `+2`, checks one final P12 notification, and verifies that the operation scope disposes without fault. Other owner receipt paths are unchanged. Existing assertions were not weakened. The implementation diff passes `git diff --check`.

## Evidence verification

The reviewer confirmed that source hashes and the listed manifest, XML, and log hashes match. Report counters were:

- CoreValidation cold and warm: 1,060/1,060 each.
- Focused Unity TravelParty start/P12 suite: 16/16.
- Full Unity EditMode: 3,300/3,300.
- Official Smoke: 5/5.

Input snapshots were stable and failed/skipped counts were zero. The reviewer did not rerun tests or promote the candidate. No private absolute paths appear in the tracked validation record.

## Scope limits

This review accepts only the bounded TravelParty-start compensation correction. It does not complete CORE-F or authorize Core canonical promotion.
