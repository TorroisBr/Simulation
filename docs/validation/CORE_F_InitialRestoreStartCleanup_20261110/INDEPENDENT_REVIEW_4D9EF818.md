# Independent exact-tip review — restore-start cleanup follow-up

## Reviewed identities

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Exact candidate tip: `bd9d4e8e2f7a405d9004d9429b616bfb3ad106b2`
- Reviewed code tip: `4d9ef8186c70a654b2a4e006eb36671c7fee3bac`
- Reviewed code tree: `a126b7ed58d92288c9341403c3495d1935f216e3`
- Base: `49331cd7b131b8bc33a87d3f9a0a5f6077fae0dd`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Review verdict: **PASS**

## Findings and disposition

The preceding review returned `NEEDS_CHANGES` for not exercising a real
`RestoreRequired` initial-start failure. The follow-up regression now captures
and loads a real completed continuation, enters the production
`TryStartOnExistingApplication` projection callback, runs the real P12-G
restore coordinator, observes the exact image-bound commit proof and private
prepared references, and rejects the projection before publication. Assertions
verify that the active/published session remains absent, the prepared image and
session references are cleared, and the one-use proof is revoked. The reviewer
confirmed that the prior finding is closed, no assertions were weakened, and no
additional actionable source defect remains.

## Exact-tip evidence reviewed

- Focused official EditMode test: stable **1/1 PASS** on code commit
  `4d9ef8186c70a654b2a4e006eb36671c7fee3bac` / tree
  `a126b7ed58d92288c9341403c3495d1935f216e3`.
- Full official EditMode: stable **3303/3303 PASS**, zero failed/skipped/
  inconclusive, same exact source identity; all five Smoke test identities
  are present and Passed.
- Official Unity evidence verifier: **VALID**, one run/one entry, archive
  SHA-256
  `e5811981150caef4efb43bf9a32b5cb1c638165a235b11fb7d328b30612619ac`.
- CoreValidation cold/warm **1088/1088 each** is applicable from the preserved
  run because this follow-up changes only a Unity EditMode test and leaves the
  Core implementation/test inputs unchanged.
- Privacy-safe package hashes and exact run identities are recorded in
  [`VALIDATION_FOLLOWUP_4D9EF818.md`](VALIDATION_FOLLOWUP_4D9EF818.md).

## Scope boundary

This review validates the bounded initial restore-start cleanup candidate. It
does not claim P12-G or CORE-F completion, a public complete `TryStart`, an
independent .NET host, T/T+1/T+2 parity, or readiness for Core canonical
promotion. No canonical promotion is proposed.
