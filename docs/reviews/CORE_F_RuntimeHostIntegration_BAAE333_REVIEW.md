# CORE-F Runtime/Host Integration checkpoint — exact-tip review

**Verdict: `VALIDATED_CANDIDATE`** for the bounded checkpoint at the exact
candidate tip below. This verdict does not complete CORE-F and is not canonical
promotion.

| Identity | Value |
|---|---|
| Candidate branch | `codex/core/CORE-F-runtime-host-integration` |
| Candidate commit | `baae333abcdc7c9d9cb043d39d845828fb11e39e` |
| Candidate tree | `a58e474eac913a482a026d4153bfbf1f9aaab255` |
| Parent / checkpoint base | `4a81edbd6f20fa0105ce82d08287013e2cc2c2e3` |
| Core canonical at review | `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea` |
| Remote candidate at review | `baae333abcdc7c9d9cb043d39d845828fb11e39e` |

## Review result

The independent reviewer inspected the full candidate diff against its actual
parent, current canonical and the approved CORE-F contracts. No actionable
implementation or test-integrity finding remains. The restore fixtures use
equivalent provider configurations and verify owner-graph, directive, revision,
epoch and continuation parity. Updated test expectations match the selected
profile identity rules and daily countdown behavior. Independent
`git diff --check` passed.

The validated code scope is limited to Core-owned selected directive
preparation; selected travel and flee owner commits with Unity projection
adapters; selected status/identity projection consistency; and restore binding
validation. The source tree is the exact tree identified above.

## Validation evidence

- CoreValidation run `e711770c36184d2f911519b09e0e0f14`: **601/601 cold and
  601/601 warm passed**. Its private manifest records the exact candidate
  commit and tree, stable inputs, and no tracked source/test modifications at
  execution.
- P12 regression run `5fb25e3f8f1f45868864d86835226aa4`: **556/556 passed**.
- Full Unity EditMode run `16bb1ff861a1470cb0200691ecf154fd`:
  **3251/3251 passed**.
- Official Smoke run `dc5dee99855949ab9541454c8ece0cd3`: **5/5 passed**.
- The Unity validation inputs match the reviewed Unity-relevant source/test
  digest. Unity validation was not repeated because those inputs are unchanged.

The CoreValidation originals remain in protected local evidence storage; this
review records the approved runner manifest identity and did not publish raw
reports or workstation paths. Existing Unity validation evidence is preserved
as recorded by its validation runs.

## Remaining integration constraints and test gaps

`SimulationRuntime` still owns the daily lifecycle, admission, advance lease,
capture-token publication, and Unity-hosted selected runtime/action paths.
There is no shared `DailyV1Application` implementation, detached .NET host, or
separate-process T/T+1/T+2 proof in this checkpoint. The current green test
totals therefore do not establish an operational Core-owned application or
full CORE-F acceptance. The remaining integrated application/host work must
move the selected semantic runtime and session/restore ownership into
`Simulation.Core`, preserve one authoritative owner graph, and leave Unity as a
forwarding/authoring adapter under the approved design.

The worktree's unrelated ProjectSettings edits, untracked `.meta` files, and
generated `bin`/`obj` outputs were outside the candidate and remained
untouched. No canonical promotion or phase-closure claim is made.
