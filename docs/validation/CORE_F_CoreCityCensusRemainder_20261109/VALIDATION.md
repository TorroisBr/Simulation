# CORE-F Core-owned City Census Remainder — Validation

Date: 2026-11-09

## Scope

This bounded integration moves the four selected City census families into
Core's host-remainder projection: Important-NPC presence, Market stock rows,
Population aggregate, and Population operation receipts.

Core omits City rows only when the complete selected set has the exact section
identity, required role, schema, owner-instance identity, cardinality, and
revision witnessed by the admitted runtime owner graph. It keeps all City rows
on the existing P12 path if any selected row is missing or stale, the vector is
partial, or an additional City roster is present. Core restores its selected
City witnesses before P12-F applies the structurally complete owner-vector
gate. The mutation-epoch check remains before projection.

This does not change the selected P12 section contract or imply that all 89
families are Core-owned. It does not claim P12 readiness or CORE-F completion.
Unity still supplies the complete quiescent source vector and epoch.

## Exact implementation identity

- Candidate branch: codex/core/CORE-F-restore-stage-transaction
- Base: 6ea144c9544c7bc0aaf964f86e1edc09dd38f6f1
- Production implementation commit: fb837410fa742d4b8d5290ec2c4b0739c26df512
- Review-driven test-only follow-up: ace43a3a41d8b8aebdb318ba383e17d4f84c9c0e
- Candidate tree after test follow-up: be2db1166b3df84bd34b8ec90348f1aef7240df9
- DailyV1RuntimeAdvanceStage.cs SHA-256: ee40b068026c82157afca2491e0f05344035517ae21a1200416a718db1e3780a
- DailyV1ApplicationSessionGateTests.cs SHA-256: 1daaeb6a15514d23af608bb214376ac42056acc854f6f785fcf4375f90f16ab3

The source and test hashes were recomputed after their commits. The production
source did not change for the test-only follow-up.

## Validation

- Official CoreValidation run 89e1be289d8f413195571e6dbfe90c9e: PASS,
  1,076/1,076 cold and 1,076/1,076 warm after the partial-City regression.
  Discovery count 1,076; stable input digest
  13efe1eab5236f0d563ca91392ec7844e0536914ea057c8ffdd3a3bfbd579f71.
  Cold TRX SHA-256 f9d2d158fd10a2db380cfaa088afc514de7dff79209453db9ae15e7c56ee4d43;
  warm TRX SHA-256 5851bc9f36bcb647440dbafcb3267bdfff45e4dce880b011bb54b9d8c3f29c28.
  The runner also completed its demo and Lab build/test gates. This worktree
  contained a Unity Library, so this run makes no Library-independence claim.
- Official full Unity EditMode run e4d3b22fa4e848f58ebae68bab4b3034: PASS,
  3,303/3,303, zero failed or skipped. Unity input snapshots were stable at
  d39ac9326d6ce24bbbe2dbf07fa1249bca80d7a869c76c56943251c9bf08b8ca.
  XML SHA-256 7934d0d64a17c726abee804938ffd3193b7251b43ff54e824529ee340304b0cc;
  log SHA-256 b897f53342bc61378eaed66d76129708c0788ea357c29af1d6be180fbbae20ca.
- Official filtered Smoke run 4e0af08a0b6a4ad7a8c32f61fc7ca6e3: PASS,
  5/5, zero failed or skipped, EditMode filter Smoke. It used the same stable
  Unity input snapshot above. XML SHA-256
  7fa3d2debe47e588c759cf7c52635160499c50f381bc5f12dec638306c686932;
  log SHA-256 82c6d48dc09c58ee5fdb67f2f05d1b98fa9439eefb4e6a49f765be2a96f1dc09.
- git diff --check: PASS for the implementation commit and working changes.

Raw XML and logs remain in the official private evidence store. Only run
identities, result counts, input digests, and artifact hashes are recorded here.
The earlier PlayMode invocation selected zero tests and is not counted as Smoke
evidence; the successful five-case EditMode Smoke filter above is the accepted
result. Superseded diagnostic runs remain preserved and are not claimed as
final validation.

## Review and integration boundary

The first independent exact-tip review found no production defect and requested
a direct partial-City vector regression. Follow-up commit ace43a3 adds that
assertion: the remainder keeps each captured City row by reference and the
next City-composition gate rejects the partial set. CoreValidation was rerun
after the test-only change. The full Unity input manifest contains no
Simulation.Core.Tests source; its Unity source snapshot and production code
hash remain unchanged, so the exact EditMode and Smoke evidence above still
applies. Independent review of the updated exact tip is pending.

No canonical promotion has occurred. The candidate is a recoverable partial
CORE-F integration only.
