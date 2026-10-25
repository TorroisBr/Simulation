# CORE-F P12-G Core-bound owner sources — independent review

Verdict: **PASS** for the exact bounded candidate diff.

## Reviewed identity

- Exact commit/tree: `7a34e1c1c5b698e9bf9761ae491a9fe5b9a9f447` / `3cdf1e4c686653ca14d796df5466d46c272b2387`
- Reviewed against parent: `2f3892973c59e41fb4c08ef4f00a2c40ea82c316`

## Review result

Core genesis binds the exact newly created `ExpeditionStore` into the selected owner graph, and the binding remains identity-stable. The selected Daily-v1 fixture retains that store empty at revision zero, with no P10 site/content creation. Restored graphs remain separately constructible: P12-D staging binds the staged ExpeditionStore on the detached candidate graph; restore composition does not require a genesis-store instance.

The new source aggregator is limited to ActorChoice, ScheduledDirective, TravelParty, and Expedition. It validates the exact bound stores and source token/vector, captures their immutable snapshots, revalidates after the batch, and returns no partial aggregate on failure. PoliticalKnowledge remains excluded. The added tests verify exact ExpeditionStore identity, local cardinality/revision or sequence values, missing bindings, stale recapture, and operation disposal without weakening existing assertions.

No source findings remain in this diff. The reviewer did not rerun tests or independently inspect validation artifacts; the separately recorded validation record binds those results.

## Explicit exclusions

This review does not establish complete P12-G owner coverage, full image composition, detached target admission, atomic storage integration, public save/load completion, P12-G completion, CORE-F completion, or Core canonical promotion.