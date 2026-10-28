# Independent Review — Core-Owned Daily Advance Protocol Contract

- Verdict: PASS — no actionable findings.
- Reviewed: 2026-10-26.
- Candidate commit: `939a3434004d58aeaec2a643c90785bb50478aa4`.
- Candidate tree: `3b3d8e0fecdbb423b12f19587fde34bbaa8ce38e`.
- Candidate parent: `d860fae3e14a778dc541bddd6c33186afafbda5f`.
- Candidate branch: `codex/core/CORE-F-shared-daily-v1-app-host`.
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

## Review findings

The Core-selected runtime boundary owns the `runtime.advance-day` protocol contract identifier and chooses the exact P12 capture-scope allowance. It checks the allowed scope before and after the host census read. A host failure clears any partially returned owner vector and epoch, and a reentrant scope change is rejected without exposing census evidence.

The Unity host adapter forwards the Core-selected operation identifier into the existing admission protocol. This preserves current Unity protocol behavior while moving selection of the operation contract and capture-scope policy into Core. The existing 89-family census implementation, its identities, and mixed-roster fallback are unchanged and are not claimed as migrated or complete.

Coverage includes the operation identifier, the four supported capture allowances, rejected scopes, partial-output clearing, and reentrant scope mutation. No assertions or validation requirements were weakened.

## Exact-tip validation evidence

The private owner-only evidence packages are retained by run ID; manifests include the exact input snapshot and artifact hashes.

- CoreValidation run `f08f150c29f34519a958537cbd03ca9a`: cold **871/871**, warm **871/871**, zero failures. Manifest SHA-256: `774cabc547a7f6ffb7bf0855b21ae83fcf40e18677fd1aedbf14eb8131459a3c`.
- Unity EditMode run `47dcff4903964d4c8cfd9974d5b61154`: **3271/3271**, zero failures, stable input. XML SHA-256: `f79b95e28671649d543891986bbbd5cd98e09d52955a6a3198d2589109919988`; log SHA-256: `4870337d036687d6933249f2d12079c6d8b1f05f7c8214e8302245bb91a4cb03`; manifest SHA-256: `10adf4b646c92b33c638662b1e849a18160b0dd871463b0fcab8f43a7cb49ede`.
- Official Smoke run `a1a2583d328c438b829a1b8760952acd`: **5/5**, zero failures, stable input. XML SHA-256: `2ef0a9fdd8b9b2b6cb3b82b537773cc2dbac70b10cefafceeac641601d3cfd39`; log SHA-256: `be6b055967b53412bc1fbbdc272984b04794d804e4889f79283c19ae14ac0ae4`; manifest SHA-256: `dca58fccc0ae75529b450329b70411020ad9c1421938e64c03c75a6146933592`.
- `git diff --check`: PASS before the candidate commit.

The independent reviewer verified exact source hashes for the changed Core and Unity production/test files against the validation manifests and found no source drift.

## Scope and limitations

This is a bounded P12/runtime-authority integration checkpoint only. It does not move the 89-family owner census into Core, complete selected runtime authority migration, deliver the full `DailyV1Application.TryStart`, complete detached restoration, or close CORE-F. Core canonical remains unchanged. The candidate has not been published; the earlier standard push of its predecessor was rejected by the remote `commit_refs_rules` policy.
