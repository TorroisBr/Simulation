# P12-B Dynamic NPC SpatialKnowledge census implementation candidate

**Status:** Exact-tip independent implementation review passed at code tip
`2c782a7a08abfc1c8b2ba9201efad12f4ac279ae`. The durable review record is
[`PHASE12_P12B_DYNAMIC_NPC_CENSUS_IMPLEMENTATION_REVIEW.md`](PHASE12_P12B_DYNAMIC_NPC_CENSUS_IMPLEMENTATION_REVIEW.md).
This does not claim P12-B completion or P12-A readiness.

- **Implementation base:** `0a37e9f053b482d80d0815c95352e3d96b56ed8f`
- **Feature branch:** `codex/phase12/P12BDynamicNpcCensusImplementation`
- **Code-bearing tip:** `2c782a7a08abfc1c8b2ba9201efad12f4ac279ae`
- **Reviewed design:** `156dcb19ecb8f15dc9611e9e0ee45637f775faa8`; review record `8b31a82652ffd958fea6ce2fb68939777a4bd99f`
- **Contract clarification:** `f3c7edee28968b6af0020c09a08fc0ab8740fe64`; amended review record is on design branch tip `f6a8640`.
- **Implementation review:** PASS against base `0a37e9f053b482d80d0815c95352e3d96b56ed8f`; exact-tip review was recorded after the cross-thread context correction.

## Delivered boundary

The runtime census now starts with exactly two schema-v1 SpatialKnowledge sections for each currently rostered NPC. Sections are ordinally ordered by `RuntimeId`, pair-adjacent, and tied to the exact `NpcRuntime` and `SpatialKnowledgeRuntime` instances. Bootstrap publishes the runtime's latest reconciled provider snapshot.

Direct successful NPC registration and removal each form one outer membership operation. Materialization and existing-NPC adoption own the outer operation; nested roster changes share a runtime-owned transaction context. A successful boundary stages and validates the whole dynamic family and both fixed PersonStore witnesses before publishing maps, baselines, provider snapshot, or epoch. One boundary advances the epoch at most once.

Only successful PersonStore materialization bind and rollback commits are counted in the runtime transaction context. An unmarked revision change fails census closed without publishing a staged family or fixed baseline. Compensation failure faults the mutation guard and census. Assessment during an active operation returns `OperationInProgress` before reading the partial family.

The local NPC membership context records its owning `Thread` and managed thread ID. A cross-thread nested entry faults this partial census and receives a detached scope; it cannot change the live context's nesting count, write accounting, provider snapshot, or epoch. Scope disposal and PersonStore revision marks also check the local owner. This boundary does not establish global owner-thread or quiescence proof.

The change preserves current gameplay acceptance and rejection behavior. A same-ID owner replacement is supported through the existing successful unregister and later register calls, each as its own operation; the current runtime has no atomic same-ID replacement operation.

## Validation

Validation used the repository Unity harness. XML and log artifacts are retained under `Library/ValidationResults/P12BDynamicNpcCensus/`.

| Suite | Result | XML |
|---|---:|---|
| `ContinuationCensusProtocolTests` | 22/22 | `EditMode-20260930-191626-acdd334eb2054966a880b68087d1ad02.xml` |
| `SpatialKnowledgeCensusTests` | 16/16 | `EditMode-20260930-191607-35127422d7174ca99825f11a8a06ffa3.xml` |
| ALL EditMode | 2035/2035 | `EditMode-20260930-191703-a6832eff92e74bcbac836c5b82afbf55.xml` |
| Complete official `Smoke` filter | 5/5 | `EditMode-20260930-191747-d89b7c87b4b545c593dffc17ff56f79c.xml` |

`git diff --check` passed for the code-bearing change.

## Evidence and limits

Tests cover the day-zero ten-NPC/twenty-section census, dynamic add/remove in reverse identifier order, no-op and rejected membership mutations, successful materialization, successful adoption, reachable pre-bind adoption rejection, PersonStore revision `+2` bind/rollback notification of both fixed sections with one epoch and unchanged family, active-operation assessment, cross-thread nested entry fail-closed behavior, malformed and missed reconciliation, unexpected in-scope PersonStore revision drift, death/emigration retention, and fresh-owner reseeding after explicit removal and re-registration.

No post-bind adoption failure is reachable through the current API: once the existing residence setter is reached, it cannot fail. No failure seam or adoption behavior was added. The `+2` compensation accounting is verified at protocol-unit level using the supported PersonStore bind and rollback operations. Compensation paths check rollback results and fault the census if restoration fails.

This remains a partial passive census. Existing City `ImportantNpcs` projection, unrelated PersonStore writers, complete profile inventory, shared writer coverage, global owner-thread/quiescence proof, and capture eligibility remain excluded blockers. It adds no P12-A implementation, exports or hydration, and no P12-D/E/F/G work. P12-B remains incomplete.
