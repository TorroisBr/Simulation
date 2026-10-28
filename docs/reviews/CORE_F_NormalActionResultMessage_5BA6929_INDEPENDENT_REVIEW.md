# CORE-F Normal Action Result Message — Exact-Tip Review

**Verdict:** VALIDATED_CANDIDATE (independent review PASS; no actionable findings).

- Candidate branch: codex/core/CORE-F-initial-restore-orchestration
- Candidate commit: 5ba6929153efc20040991575c1a880a020d39c37
- Candidate tree: 43f7fa780212f0afe069f14a9a4e01666e812b80
- Exact base: b20761b923e6f94ecbc2f194113751e361af620a
- Current Core canonical at review: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Independent review: exact-tip review of the complete base-to-candidate diff.

## Scope and result

The candidate moves successful selected Daily-v1 Normal-action result-message formatting into Core while retaining Unity as the host output adapter. Core requires the selected actor definition to be the canonical object in that actor's admitted definition graph, and formats only the successful Normal dispatch result from the admitted definition/action data. Unity forwards the resulting message to its existing logger. Failed-roll formatting remains on the existing Unity path.

The prior review finding on the parent candidate was that a projectionless Core Normal action could skip Unity completion and lose its report. This candidate resolves that finding by supplying the result message through the existing completion/reporting route. The actor label, authored action text, and fallback action wording match the prior Unity formatting for the selected Daily-v1 path. Regression tests assert the message reaches the host reporting callback and exercise fallback wording; no assertions were weakened.

## Validation evidence

Evidence was checked against the exact candidate code/tree:

- CoreValidation: 917/917 cold and 917/917 warm; run a797a2ca93be422b9bd0bae5bbc48c5e.
- Unity EditMode: 3275/3275 PASS; run 2677152ee1394a31bdc68556fce70c3a.
- Official Smoke: 5/5 PASS; run 82e4fda1309144a08e351144e2d73adf.
- Unity inputs remained stable and changed source hashes matched the run manifests.
- git diff --check: PASS.

The independent reviewer inspected the private manifests/report metadata and confirmed the changed Core/test source identity. Original reports and private evidence remain in protected E: storage and are not copied into this public record.

## Integration constraints

This is a bounded candidate checkpoint only. It does not complete CORE-F, implement DailyV1Application.TryStart, move the full runtime or P12 census authority into Core, complete detached restoration, establish the .NET host, or authorize Core canonical promotion. Core canonical remains unchanged.
