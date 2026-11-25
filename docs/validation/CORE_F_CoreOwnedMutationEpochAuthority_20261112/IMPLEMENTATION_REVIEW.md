# Independent implementation review — Core-owned selected mutation epoch

Verdict: **VALIDATED_CANDIDATE** for the bounded implementation described here. This is not canonical promotion or CORE-F completion.

## Reviewed identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`.
- Exact review/documentation tip: `e78ef9c3b1eb1fc712d1ad63bf8f73c0486e8097`.
- Implementation commit: `535e5a3158b08a63b68c4bf00d65212e9f082330`.
- Reviewed implementation tree: `43632273125d4329047b1c64d7c329726899baf5`.
- Diff base: `b566a7f78f6e3495b3c4f8d582263aaa773dbc7b`.
- Core canonical observed: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Review covered the complete base-to-candidate diff, exact test-input identity, validation record, and committed normalized Unity report. The source tree did not change at the documentation-only tip.

## Review result

The selected Core execution boundary checks epoch capacity and reservations, asks the census protocol to validate and refresh the complete changed-owner witness set, and then advances the shared epoch once. Selected receipt sets remain exact and deterministically ordered. Existing `NotifyCommittedMutations` epoch publication remains available for legacy and mixed-roster writers. No issue was found in the reviewed scope's owner-identity checks, census witness validation, or operation atomicity behavior.

The reviewer independently verified:

- CoreValidation cold/warm: 1,119/1,119 each; five changed Core source/test files match the retained input hashes and both TRX hashes match the validation record.
- Full Unity EditMode: 3,319/3,319; filtered EditMode Smoke: 5/5; all eight changed Unity/Packages files match both retained source manifests.
- The committed Unity archive and public manifest hashes match the validation record; its XML contains all five passed Smoke cases.
- `git diff --check` passes.
- Core canonical is an ancestor of this candidate. Protected dirty and untracked files remain untouched.

## Integration limitation

This review is scoped to `b566a7f` through this selected mutation-epoch checkpoint. The candidate branch also contains earlier unpromoted commits beyond Core canonical, so this verdict does not authorize promoting the branch tip. Before any future canonical integration, the full cumulative ancestry and code must be revalidated under the canonical-promotion workflow. Any code/tree change, rebase, or relevant canonical advance requires affected revalidation and fresh exact-tip review.

The checkpoint preserves but does not migrate the complete 89-family census registry. It does not complete shared P12 capture/restoration, public `DailyV1Application.TryStart`, or the standalone .NET host.