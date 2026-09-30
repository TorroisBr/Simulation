# P12-B Dynamic NPC SpatialKnowledge census implementation candidate

**Status:** Submitted candidate; awaiting independent implementation review. This does not claim P12-B completion or P12-A readiness.

- **Implementation base:** `0a37e9f053b482d80d0815c95352e3d96b56ed8f`
- **Feature branch:** `codex/phase12/P12BDynamicNpcCensusImplementation`
- **Code-bearing tip:** `b2e46b3c9b0fefe2fefc08b3825c63b427e46ef0`
- **Reviewed design:** `156dcb19ecb8f15dc9611e9e0ee45637f775faa8`; review record `8b31a82652ffd958fea6ce2fb68939777a4bd99f`
- **Contract clarification:** `f3c7edee28968b6af0020c09a08fc0ab8740fe64`; amended review record is on design branch tip `f6a8640`.

## Delivered boundary

The runtime census now starts with exactly two schema-v1 SpatialKnowledge sections for each currently rostered NPC. Sections are ordinally ordered by `RuntimeId`, pair-adjacent, and tied to the exact `NpcRuntime` and `SpatialKnowledgeRuntime` instances. Bootstrap publishes the runtime's latest reconciled provider snapshot.

Direct successful NPC registration and removal each form one outer membership operation. Materialization and existing-NPC adoption own the outer operation; nested roster changes share a runtime-owned transaction context. A successful boundary stages and validates the whole dynamic family and both fixed PersonStore witnesses before publishing maps, baselines, provider snapshot, or epoch. One boundary advances the epoch at most once.

Only successful PersonStore materialization bind and rollback commits are counted in the runtime transaction context. An unmarked revision change fails census closed without publishing a staged family or fixed baseline. Compensation failure faults the mutation guard and census. Assessment during an active operation returns `OperationInProgress` before reading the partial family.

The change preserves current gameplay acceptance and rejection behavior. A same-ID owner replacement is supported through the existing successful unregister and later register calls, each as its own operation; the current runtime has no atomic same-ID replacement operation.

## Validation

Validation used the repository Unity harness. XML and log artifacts are retained under `Library/ValidationResults/P12BDynamicNpcCensus/`.

| Suite | Result | XML |
|---|---:|---|
| `ContinuationCensusProtocolTests` | 22/22 | `EditMode-20260930-190156-1b669bbfb43e40879aa5826229023fba.xml` |
| `SpatialKnowledgeCensusTests` | 15/15 | `EditMode-20260930-185909-cc0e3da2ca1a42e398efce9bae4f9093.xml` |
| `PersonStoreCensusTests` | 7/7 | `EditMode-20260930-185935-e850117c1c0547fe857a1986a4b40f30.xml` |
| `SimulationBootstrapCompositionTests` | 14/14 | `EditMode-20260930-190306-464dc09fd90a4a719ff4f4be0dd88c0b.xml` |
| ALL EditMode | 2034/2034 | `EditMode-20260930-190017-ef817dfa8c23456db8dd1f6d2244c967.xml` |
| Complete official `Smoke` filter | 5/5 | `EditMode-20260930-190111-ab4712293c2b401892157ee425f85abb.xml` |

`git diff --check` passed for the code-bearing change.

## Evidence and limits

Tests cover the day-zero ten-NPC/twenty-section census, dynamic add/remove in reverse identifier order, no-op and rejected membership mutations, successful materialization, successful adoption, reachable pre-bind adoption rejection, PersonStore revision `+2` bind/rollback notification of both fixed sections with one epoch and unchanged family, active-operation assessment, malformed and missed reconciliation, unexpected in-scope PersonStore revision drift, death/emigration retention, and fresh-owner reseeding after explicit removal and re-registration.

No post-bind adoption failure is reachable through the current API: once the existing residence setter is reached, it cannot fail. No failure seam or adoption behavior was added. The `+2` compensation accounting is verified at protocol-unit level using the supported PersonStore bind and rollback operations. Compensation paths check rollback results and fault the census if restoration fails.

This remains a partial passive census. Existing City `ImportantNpcs` projection, unrelated PersonStore writers, complete profile inventory, shared writer coverage, global owner-thread/quiescence proof, and capture eligibility remain excluded blockers. It adds no P12-A implementation, exports or hydration, and no P12-D/E/F/G work. P12-B remains incomplete.
