# Independent review — CORE-F P12-D Core NPC owner hydration

Verdict: **PASS — VALIDATED_CANDIDATE**

- Base: `b7fbb9e4c3b4343a84a4bbd39e6af8e5753f3261`
- Reviewed candidate tip: `7990d5b886c18fa02ec854e16de2c1b962ecee6f`
- Implementation commit: `f555260716157a8ccf62453a39c298aac946014b`
- Implementation tree: `d811828896ab2db6fcd48defff595aa8dac558b2`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Candidate branch: `codex/core/CORE-F-p12d-core-hydration`

An independent reviewer inspected the complete base-to-candidate diff, the approved P12-D/CORE-F contracts, tests, exact-tip validation record, package manifest, and archive. The remote candidate matched the reviewed tip. The reviewer found no actionable implementation defects, duplicate authority, weakened assertions, or scope expansion.

The review confirmed that selected Core NPC owners are hydrated from the paired detached rows before Unity `NpcRuntime` projections are constructed. The resulting projection retains the exact hydrated owner instances; identity, values, revisions, references, and cardinality are checked before use. Ordered City membership assembly remains deferred to the later graph composition stage. Merchant plan projection binding remains staged and upgrades only when the full selected definition identities match.

## Validation evidence inspected

The exact implementation commit/tree has the following recorded validation:

- Official CoreValidation: 718/718 cold and 718/718 warm; run `ffd185e4cb864340acc1d123032a10a9`.
- Focused `P12DNpcRootOwnerSnapshotTests`: 32/32; run `76ac732709b04a52b2c39de857d05526`.
- Full Unity EditMode: 3263/3263; run `4877b73f8eb4435bb6835a0a3290f1d1`.
- Historical `Smoke` name filter: 5/5; run `ef0a66968403416a849edb1712e1ce50`.
- `git diff --check`: PASS.

The full EditMode archive SHA-256 is `75e3fd0563b09ab4dddcf576970e69a89441b24076ab228a229326ddea6614b0`; its manifest SHA-256 is `4da3945bbc00e9a5bd1c90145192688b5f9b959bdfe134bcc449714ded855110`. The reviewer verified the archive contents, normalized XML hash, complete result accounting, manifest relationship, and public-artifact privacy scan. The public package contains the complete 3263-case suite, including the focused and Smoke cases.

## Evidence limitations

- The official packager rejects separately filtered focused and Smoke reports because Unity retains the full assembly `testcasecount` on the outer suite while the filtered descendants contain only the selected cases. Those filtered reports remain in protected local retention and were not altered or published as standalone packages.
- CoreValidation's protected local evidence reports `simulation-core-validation/v1`, not `dotnet-trx-source-context-v1`. No public TRX package or original-to-public TRX comparison is claimed.
- CoreValidation ran in a checkout containing Unity `Library`; Library independence is not established.

These limitations are reported explicitly and do not invalidate the evidence claims above. The independent review was read-only; this record is authored separately from the review result.

## Scope and status

This validates only the bounded P12-D Core NPC owner-hydration candidate. It does not complete P12-D, CORE-F, or Core canonical promotion; and it does not establish complete restored-graph validation, runtime-wide quiescence, capture eligibility, detached-host readiness, or deterministic continuation parity. The two local ProjectSettings edits are unrelated, remain unstaged, and are not included in the reviewed candidate.
