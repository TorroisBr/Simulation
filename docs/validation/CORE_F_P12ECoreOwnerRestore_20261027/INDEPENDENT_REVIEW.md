# Independent review — P12-E Core owner restore staging

## Verdict

**PASS — validated bounded candidate.** This review covers the P12-E owner
staging migration only. It does not establish CORE-F completion or authorize
Core canonical promotion.

## Source identity

- Core canonical base: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Candidate implementation: `bb408eb489cccd5e8d642853d3cf164be33c129d`
- Candidate implementation tree: `2041fbd258591a3553defc72d03a305331ef1e9b`
- Reviewed candidate tip including validation bookkeeping:
  `b930ca8430da9e041d37aadc054461d31e161b1b`
- Candidate branch: `codex/core/CORE-F-initial-restore-orchestration`

The final bookkeeping commit changes only this validation record. The
implementation tree remains the reviewed candidate tree above.

## Review findings

No actionable implementation findings. The review verified:

- Core stages Military, Conflict, War, and Battle in the required dependency
  order before Justice; stage observers run only after each accepted stage.
- Unity consumes these Core-staged owners and preserves the existing test
  override/stage-order seam without introducing a second authoritative stager.
- The exact selected `SpatialAuthorityStore` is used; LocalTopology remains
  uncomposed for the accepted Daily-v1 profile.
- `BattleResolutionProvenance` remains internal; existing friend-assembly
  access is sufficient.
- Existing P12 rejection and publication behavior remains covered; the change
  does not assert P12-B completion or full CORE-F acceptance.

## Validation identity

The first recorded CoreValidation run was not accepted as exact-source evidence
because its file-hash projection did not match the implementation checkout.
The official runner was rerun against candidate commit
`bb408eb489cccd5e8d642853d3cf164be33c129d`, tree
`2041fbd258591a3553defc72d03a305331ef1e9b`, with stable input digest
`e006428dcc98d73a5d69d75c541cd3fc4d6cc99748faca46d9034b356fc449f4`:

- CoreValidation cold: 902/902 passed.
- CoreValidation warm: 902/902 passed.
- Protected manifest SHA-256:
  `70a5bb5e7e2385173b5964bbb87fb00fa394086b7def72ed9222744df9ee94b1`.
- Cold/warm TRX SHA-256:
  `6d714e207c0770656f8a46ebcd8a235725d90f099c43e597defa4cfa6d81b64d` /
  `82213f2b24643d74087607501943e40ecedf4391d701043a5b7c0b46c7fe6881`.

The independent checkout used a different Windows line-ending projection;
comparison of the two changed test files by Git blob identity confirmed the
same candidate content. No content change was needed. The validation runner
emitted `simulation-core-validation/v1`, not the separate source-context
schema required by the DotNetTrx public packager, so no public DotNetTrx
package or private-to-public comparison is claimed.

The Unity evidence package remains unchanged and valid: full EditMode
3274/3274 passed, filtered Smoke 5/5 passed, the package hashes are recorded
in `VALIDATION.md`, and the independently checked Unity source inputs match
the tested files. `git diff --check` passed.

## Scope limits

This checkpoint moves the selected P12-E Military, Conflict, War, and Battle
restore staging into Core. It does not move the full P12-G restoration
coordinator, implement the public `DailyV1Application.TryStart`, complete the
.detached .NET host or T/T+1/T+2 parity, close CORE-F, or change any canonical
branch.