# CORE-F P12-E Genesis Owner Graph — Independent Review

- Verdict: `VALIDATED_CANDIDATE`
- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Candidate commit: `74be5aaa64ea21c532e6ad4287ca78b17be79d61`
- Candidate tree: `a89c486e92d5ad70c6ce3d4b3d224b68dd2d2724`
- Exact base: `8e8537032cb89213d8fbcc1f4207781862f23d5c`
- Core canonical reviewed: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Review scope: Genesis construction, P12-E owner identity, census fixture composition, Unity runtime binding, and the exact candidate diff.

## Findings

PASS — no actionable findings.

Genesis retains the exact Core Person, Justice, and PoliticalDecision owners. The new P12-E owners use the empty owner graph required by the accepted P9 proving profile, with Positions and Battles bound to the exact Genesis spatial authority. Unity receives those same staged owner instances, satisfying the runtime exact-instance checks without parallel stores.

The census fixtures replace synthetic P12-E rows with facts from the retained Core owners. Assertions remain intact and verify owner identity, dependency links, cardinality, and the required census rows.

## Exact-tip validation evidence

- CoreValidation: 1092/1092 cold and 1092/1092 warm; run `5c208a2c283b4439a346673e574feb38`.
- Unity EditMode: 3306/3306; run `f0eb3270669048438ac75f3551ea99c5`.
- Editor-assembly smoke identities: 2/2 (`2951194e7f974a56827e561c604e4309`) and 1/1 (`681028a767a046679b1f2955dac45b37`). The documented PlayMode filter returned zero cases and is excluded from the smoke totals.
- `git diff --check`: PASS.

## Limits

This review does not establish public `DailyV1Application.TryStart`, a detached .NET host, full CORE-F acceptance, or canonical promotion readiness. The candidate remains unpromoted. Existing protected and unrelated worktree files were excluded from the candidate.
