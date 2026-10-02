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
| Code candidate | `3ed113bf35546964cd4a56f424578dc1213f41fa` |
| Tested code tree | `876e810757c55b098d805e6e9ac012da7e6cab4d` |
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
serialized runtime owner thread refresh their exact current baselines. This
includes direct `MerchantTradePlan.Set` and `TravelPlan.Set` commits outside
the named operation; each successful direct plan change advances its exact
local revision and produces its own epoch invalidation. During the named
operation, committed changed sections are collected and reported as one epoch
batch; a no-change invocation produces no batch. Shared local owner revisions
notify every sibling section. Plan revisions advance only on a successful
state change and reject saturation before changing that plan.

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
owner-hook admission, temporal batching, long-run behavior, and direct plan
owner invalidation outside the named operation.

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
`876e810757c55b098d805e6e9ac012da7e6cab4d`, before documentation-only
candidate evidence was added. Focused tests were rerun after the code change;
the direct-plan invalidation regression was rerun after its commit. Unity's
harness clears `Temp/ValidationResults` on its next
launch, so each result was copied to the ignored local archive
`Library/ValidationResults/P12BMerchantDailyOperationImplementation/` and
identified by SHA-256. These result artifacts are available in the active
worktree for independent review; they are not code changes.

| Gate | Result | XML SHA-256 | Log SHA-256 |
|---|---:|---|---|
| `SimulationRuntimeAdmissionTests` | 31/31 | `95025CFDFA37D556BF2DEE5A7F7E6D8527EABDBF1CF744EBD62D6589BB946245` | `155D3C6AD500E61E58B39827CF895F7A82D481FF43B472F27AE86F3FBC3318E7` |
| `NpcPlanCensusTests` | 5/5 | `8FBC080E552D39ECE7F521864274159523982313F220AB42F1F95D1C4134D3D6` | `D79A0F392716A6102E84160FA41BC71A330C550C95C557E851078462D5704F39` |
| `NpcKnowledgeCensusTests` | 17/17 | `D846D728127DECC676AC036BD7B194F8D9539E87DB1218DBD90C976D1AFBCC8D` | `74ADC3C7338E0A150D1429BCCE8F8B6711CA9F24883D7C913AFE40086301B859` |
| `ContinuationCensusProtocolTests` | 22/22 | `FB58A467459A6F74CFF9681B7433173266C851E3A22B18282C9E5BB1319F11D4` | `00EE72A854C18B2EB5001CAD8688CCD0F1AD44D45E0E83FCAB8D41549C2C0449` |
| `NpcOwnerCommitInvalidationTests` | 13/13 | `3D5190F710FAFCAD32B7C6B9FE3DB432D994DCB080ED0E5B193D91FC691C321A` | `F754947029D91A7E539D260398C15745A83DA41AAC3C7F54EB51D6631C0CBE69` |
| `P18DLocalKnowledgeObservationTests` | 6/6 | `A7A9CC8AAB78962063AA1B05DC0AC6F86670DD8A1E076FD9432A2610ADD28FAC` | `75B064897F05E56EBD831602965C8753AEEC9E5F54226E51B5FA7A859397E4B9` |
| `P18DConsumerIntegrationTests` | 9/9 | `25B45EACAB86BBC84B6E7E04D1A9F0C91FFFBAED96450F35C14AC4B2B583FB56` | `EFFD4804E9CE3E1D8D42407A308AB9E696211C5104F615500D8D58629F85A941` |
| `MerchantLiquidityTests` | 11/11 | `558BE608A9EBE6271239AEC472ABDBD9DE07828BC515EA6C94110038406700DE` | `3C455CABB65241F34616FFCECD1280636EA424734D5D6ADC418A0CFA20530ADF` |
| `SimulationBootstrapCompositionTests` | 14/14 | `ECC41F329DE65638C9C601C108BC2733AC9D315150D2D786B69EF1F154D73075` | `3C7B88F87B0779AE1B94079231A17885199BF8E73D8DEDCAD478A36DD44B32F3` |
| `SimulationRuntimeLongRunTests` | 7/7 | `01CD547EBE01D3A565A3FADCBB77B40E3D97DDD531E4CB3975BD31A69B25B8F0` | `504A151702872F3128BCD88C70F02069C808679BB1F6F601EACBFA0739A35A86` |
| ALL EditMode | 2160/2160 | `F6D7735FD2AD5230B55A72203CF1EFE351CF6310D3B31A59B9A16BFB463A5B41` | `B2FC3A8AFDCB6B3DC7DA1D1EA7CA8372B5678F59B7BD48CA29A91294071715CA` |
| Official EditMode `Smoke` | 5/5 | `B0870F0E2A4DDB0DA2F6928451DB0925A79C6075D4337731FCCF9A5BA96544F8` | `3DDEDCD2800C2EF2AF9FA818B0DF502DB456578AB789E4B32707FE3680B5B3B1` |
| `git diff --check` | PASS | — | — |

The focused coverage includes owner identity/cardinality, dynamic roster
addition/removal and same-ID actor replacement, exact plan revisions and
saturation, wrong-thread rejection before owner writes, direct Knowledge and
plan-owner baseline refresh, per-write plan invalidation outside the named
operation, one-batch operation accounting, exception/partial-commit semantics,
selected-profile composition, and the 100-day merchant execution path. Full
EditMode and official Smoke gates passed on the same code tree.

Canonical promotion is a separate approval gate. Until that gate passes,
P12-B remains incomplete and P12-A/P13 retain their recorded blocked status.
