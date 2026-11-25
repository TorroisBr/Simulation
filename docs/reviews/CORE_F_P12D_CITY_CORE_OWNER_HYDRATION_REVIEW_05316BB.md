# Independent Review — CORE-F P12-D City Core-owner Hydration

**Verdict: PASS — exact-tip implementation review; no actionable findings.**

## Reviewed identities

- Repository: `https://github.com/TorroisBr/Simulation`
- Candidate branch: `codex/core/CORE-F-p12d-core-hydration`
- Evidence tip: `25eb868d6756733ec06d2f3b2dedadac52fcc26a`
- Reviewed code commit: `05316bbefa1eb87ad40bda85aa90537616c2fffd`
- Reviewed code tree: `6efccc9a1cb68346b9114b530529d15fb59e3299`
- Candidate base: `509caeeb74eda2f13a6401d64e961ecf15870373`
- Current Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Reviewer: independent read-only review agent `/root/p12d_core_hydrator_review_f2`

The remote candidate ref matched the evidence tip. The code commit is directly based on the stated base; the evidence tip is its docs-only successor and retains the same tree. Core canonical is an ancestor of the code candidate. The worktree’s unrelated ProjectSettings edits and untracked private evidence remain outside the reviewed diff.

## Review result

I inspected the complete base-to-code diff, the evidence-only successor, the approved P12-D City-root staging design and its reviews, the CORE-F runtime/restore boundary, source and tests, and the validation record/package.

The selected restore path now passes detached row values into the Core hydrator. Core resolves the City and each Market item by exact identity from the admitted `DailyV1EffectiveDefinitions` graph, verifies that the legacy and resolved Location IDs agree, and delegates construction to the existing Core owner factory. Invalid inputs return no owner bundle.

The staged City owner bundle preserves:

- The exact admitted City and Item definition objects and definition graph.
- City, Location, counterparty, population, and population-economy identities.
- The Open/account-free counterparty and Free/account-free population mode required by this profile.
- Market row order and multiplicity, amounts, desired amounts, stored prices, and Market revision.
- Population aggregate value/revision, receipt identities/fingerprints/transitions, and the separate receipt-ledger revision.
- Ordered NPC membership IDs and its existing revision, with duplicate membership rejected.

The Core factory binds the exact Market and counterparty instances together and validates population receipts before returning the bundle. Unity’s selected City projection is then created from those returned owner instances; its existing exact-reference checks prevent parallel Market, Population, or counterparty authority. The nonselected legacy staging branch remains on its previous Unity-owned path. The existing P12-D relation assembler remains responsible for linking staged NPC object references in captured order after NPC staging.

P14 material-flow and unsupported account-backed state remain rejected. The implementation does not replay production, consumption, trade, or price refresh during hydration. Rejection occurs before a City projection or owner bundle is returned, preserving the existing private staging boundary.

No actionable identity, value, revision, ordering, authority, rejection-atomicity, profile, or test-integrity finding remains.

## Validation and evidence reviewed

### CoreValidation

The private run record `dec268ac4fd546f79e1d82693d1eab9b` reports cold and warm CoreValidation at **743/743 PASS**, with zero failures. I checked both preserved report counters and independently rehashed all **373** recorded inputs against the current candidate worktree; all matched the recorded digest `58ab881613b19d2d2f38b97606e61a21f675352c01bda6b8800be9cf04fc64ff`.

The recorded run notes that a Unity Library was present. These results therefore do not establish Library-independent execution.

### Unity validation and public package

The validation record ties the runs to code commit/tree above:

- Focused City-root suite: **18/18 PASS**, run `6d30ba905c1c4eb09c657bd066c6b750`.
- P12-D regression filter: **68/68 PASS**, run `e8e7a5c4107e4f6792881990d00078e1`.
- Full EditMode: **3263/3263 PASS**, run `2061ae9d32454a7ca939d2c924bbe5b3`.
- Official Smoke: **5/5 PASS**, run `af89b6ef46d24858b3f3fb58874d55ab`.
- All Unity runs record stable input inventory of 1,878 files with digest `14916f88563b0fec63308b8e716fdb40a302a0a9e56f4465dab42b914dc62db5`.

I verified the public archive SHA-256 `56b813b5cb81ea54d350553e076418a53dedd23025a5003d489873d6910e4d0e` and manifest SHA-256 `a038c0ed2c01492df3a7ecb585fb8234f815c98e216101fecfcac99d32c46fcf` against their committed files. Parsing the packaged normalized full-suite XML confirmed 3,263 passed cases, zero failed/skipped/inconclusive cases, zero output elements, and no local workstation paths. The report includes all 18 City-root cases, all 68 P12-D cases, and the five Smoke case identities. The validation record reports that the official package verifier matched this result-only package to the protected original full-suite run.

The exact candidate diff passes `git diff --check`.

## Scope and limitations

This PASS covers only the bounded P12-D City Core-owner hydration integration. It does not complete P12-D or CORE-F, publish a restored graph, establish full-profile capture/export/hydration, detached .NET host readiness, or T/T+1/T+2 continuation parity. Core canonical is unchanged. No promotion or Phase-closure verdict is implied.
