# P12-B selected daily Merchant operation implementation candidate

**Status:** validated implementation candidate; independent exact-tip code
review and canonical promotion are pending. This is accepted P12-B
prerequisite capability work. P12-B remains incomplete, P12-A remains
`WAIT_DEPENDENCY`, P13 remains blocked, and Phase 12 remains open.

| Identity | Value |
|---|---|
| Candidate branch | `codex/phase12/P12BMerchantDailyOperationImplementation` |
| Canonical base | `codex/phase12/canonical` at `a66c215d1e8e9db34ea559a8bf2a0803fbdbf4ab` |
| Reviewed design | `e9fbdada98eb6fed2d1e7a446ac9a4085504c676`; exact-tip PASS record `6cb0837fdf85c777febeb51f1c20fab65fbf2db6` on `codex/phase12/P12BMerchantDailyOperationDesignReview2` |
| Code candidate | `1701b416c1524080e5017ef722e9e2645cd9b261` |
| Tested code tree | `9a76379ecd38ee5a10a7756c11b50c6bc68ac561` |
| Architecture baseline | `451340c56e9b676bf6ea43412bcb856b9ccde3de` |

## Delivered boundary

For the existing selected-profile daily pass, `SimulationRuntime` now enters
the registered operation `runtime.merchant.advance-npc-trade-state` around
each normal per-roster-NPC `MerchantSystem.AdvanceNpcTradeState` invocation.
The scope remains active through the call and post-commit census accounting.
This is an accounting boundary for the existing call; it does not introduce a
durable occurrence receipt, deduplicate extra callers, or promise exactly-once
execution.

The runtime admits only the exact current roster actor and exact embedded
owner witnesses. Each included NPC has one merchant-plan section and one
travel-plan section, both cardinality one. The operation also binds the
existing SpatialKnowledge location/route pair and CommercialKnowledge
market/liquidity/share-receipt triple. Direct supported owner writes on the
serialized runtime owner thread refresh their exact current baselines. During
the named operation, committed changed sections are collected and reported as
one epoch batch; a no-change invocation produces no batch. Shared local owner
revisions notify every sibling section. Plan revisions advance only on a
successful state change and reject saturation before changing that plan.

If a domain method throws after owner writes, the operation reports those
committed owners in its `finally` path and preserves the exception and domain
state; it does not claim whole-call rollback. Failed admission rejects before
the selected domain call. These changes preserve the existing Merchant
decision/result semantics and do not alter gameplay behavior.

## Files and exclusions

The code candidate changes `CommercialKnowledge.cs`,
`ContinuationCensusProtocol.cs`, `NpcActionRuntime.cs`, `NpcRuntime.cs`,
`P18DMerchantTradeStateOwner.cs`, `SimulationRuntime.cs`, and
`SpatialKnowledge.cs`; it adds `NpcPlanCensusProvider.cs` and tests in
`NpcPlanCensusTests.cs`. It extends `NpcKnowledgeCensusTests.cs` and
`SimulationRuntimeAdmissionTests.cs` for dynamic roster reconciliation,
owner-hook admission, temporal batching and long-run behavior.

The optional `NpcDecisionRecorder` allocator, sequence and decision store are
not included in this operation's preflight or epoch claim. Separate Merchant
entrypoints, other observation callers, travel, other economy transactions,
and unrelated plan writers remain outside the boundary. No global
owner-thread/quiescence, complete owner or operation census, universal epoch
coverage, capture eligibility, export, hydration, P12-A readiness, P12-B
completion, or P13 readiness is claimed. The candidate changes no architecture
rule, product scope, Phase State, or checkpoint inventory.

## Exact-tree validation

All validation below ran against tested code tree
`9a76379ecd38ee5a10a7756c11b50c6bc68ac561`, before documentation-only
candidate evidence was added. Focused tests were rerun after committing the
code candidate. Unity's harness clears `Temp/ValidationResults` on its next
launch, so each result was copied to the ignored local archive
`Library/ValidationResults/P12BMerchantDailyOperationImplementation/` and
identified by SHA-256. These result artifacts are available in the active
worktree for independent review; they are not code changes.

| Gate | Result | XML SHA-256 | Log SHA-256 |
|---|---:|---|---|
| `SimulationRuntimeAdmissionTests` | 30/30 | `83EF51C57860EB4A03425537CE24BB4C94886DCA1BA34F6E0CE37D0A72E69F14` | `490A7D7F4780C67E8A741D79743F8C4F335E960E6794BF07F59C87226AAFE04E` |
| `NpcPlanCensusTests` | 5/5 | `F6206DFC8674184D46814BC9E99860D8A54E0466908090E2288DAA3E6087719C` | `2B040ADED9822738C4D7E4988BBB85BD1DBAD81BC9E3FC6F7E6D8ED6EF50215E` |
| `NpcKnowledgeCensusTests` | 17/17 | `87380BF49CC4D85CFC5F97539D2BF0FE4931F4BD9A6E86FAEC848FF3FD6CB802` | `9999719E3ADBAEDECE110DCB06E91546757A716B07FB3EF64AC1694FE9A5D2B3` |
| `ContinuationCensusProtocolTests` | 22/22 | `7711B402952A6DC9A88BC1542773BD62440A67446860D3BE4CD9212118D81E45` | `FC096AE7EC29AC559E5FEF46DD5EDDD590894407ED77A3AB9C47CE329F56F6F1` |
| `NpcOwnerCommitInvalidationTests` | 13/13 | `700B462659052D15AC67E8E35B9507BEBD7CC585D597BFD0B3C436B710194199` | `DFFA90B740934CDCDDB459DBF2EB2789DD114F3A6DC4605FB9E907A8BFDFA80F` |
| `P18DLocalKnowledgeObservationTests` | 6/6 | `ECFD7A767004431AAC4059949A480215D63678DDD06E4ADEB857F4F684B34AFC` | `359CE8C1E2D4C6CEEF43200EB63C0B219FB60C6B4A5FB798186319E2DF53276A` |
| `P18DConsumerIntegrationTests` | 9/9 | `71B4376C032961469B0D411641F5571D7972F532E1F278FC5CC937DA4D741B8B` | `CB566C1C25F3F974DA1446D996EDBE7064990C86C19FCFF3AAA960E799B8A3E2` |
| `MerchantLiquidityTests` | 11/11 | `90585C628B85AD37EE29E4863092C388EBCD38A3CD15F9484A268F2C7F074442` | `566850152388513EEDFBD539EB941BCC87D7B3F951FB6BEF2324B32AC107AF45` |
| `SimulationBootstrapCompositionTests` | 14/14 | `FE5F4B2973D15AFF7BAB7A3FEF741D76C0DFDC967836B21E18C6EBE25BB223E1` | `48660BA8A9468B1FA0994DE1F2013076ACBDA6E44A93E91A33DDDB72B6868763` |
| `SimulationRuntimeLongRunTests` | 7/7 | `D55FFD6AA62298F83A728F0FAEE677570DE24C1EBDE62BF812CB1E5729C0333F` | `BA1215693AB89DACFB24FD0E4084C25A503794EC8E6A5810480EB28B9FB58807` |
| ALL EditMode | 2159/2159 | `10F5967FF7D1B162B164F8A191F604D3A81B5D6E743055362C56C884C953CFB4` | `8C946F70E2DD096F1134D9F749C27FEB005A62CD238D0FEC54A066E42616EF4B` |
| Official EditMode `Smoke` | 5/5 | `F30FEBA0E95D306DEB96E940F2817FDE2C01A37B8AE878B432B5C61E1ECE34A5` | `57DC30A9E84F3067A7571BBD2A34EA489F5A22F8968AAEB9D4F3FCD88594987A` |
| `git diff --check` | PASS | — | — |

The focused coverage includes owner identity/cardinality, dynamic roster
addition/removal and same-ID actor replacement, exact plan revisions and
saturation, wrong-thread rejection before owner writes, direct Knowledge
baseline refresh, one-batch operation accounting, exception/partial-commit
semantics, selected-profile composition, and the 100-day merchant execution
path. Full EditMode and official Smoke gates passed on the same code tree.

Canonical promotion is a separate approval gate. Until that gate passes,
P12-B remains incomplete and P12-A/P13 retain their recorded blocked status.
