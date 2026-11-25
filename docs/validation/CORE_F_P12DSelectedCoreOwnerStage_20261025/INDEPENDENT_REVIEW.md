# Independent review — P12-D selected Core owner staging

Review date: 2026-10-25

- Candidate commit: `3a468f2e6b0e0518bfc8f60c24621501cab1863e`
- Candidate tree: `1853b397e0f6420c59cfed8561d3204c72dfffa9`
- Verdict: **PASS**

The exact-tip review verified that the selected image-restore package requires a successful Core City-owner lookup and passes that exact owner to the Unity projection. The NPC path receives the Core-staged owner dictionary, requires matching count and per-row keys, and verifies exact definition object and runtime identity before projection. The Core stage returns only after City/NPC hydration and reciprocal membership validation; it remains unpublished until package composition succeeds.

The initial review finding concerning a missing City handoff falling back to a second hydration was fixed before this reviewed commit. No other actionable defects were found. The reviewer verified candidate and origin identity, source/test evidence correspondence for CoreValidation cold/warm 773/773, the post-fix P12-G Unity restore result 15/15, and `git diff --check`. The full EditMode run and its public package were produced after the implementation commit and are recorded in `VALIDATION.md`.

This review applies only to the bounded selected-owner staging slice. It does not approve canonical promotion or claim CORE-F acceptance.
