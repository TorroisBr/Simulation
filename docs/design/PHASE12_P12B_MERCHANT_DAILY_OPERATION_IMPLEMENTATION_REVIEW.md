# P12-B selected daily Merchant operation — independent implementation review

**Verdict: `VALIDATED_CANDIDATE`**

This is an independent exact-tip review of the implementation candidate. It is
not authorization to promote the candidate or close Phase 12.

| Identity | Exact value |
|---|---|
| Candidate branch | `codex/phase12/P12BMerchantDailyOperationImplementation` |
| Candidate tip | `5e643b7e4535b5bb699eb021d3b313243bf45658` |
| Candidate tree | `8c7151d223eeef55c04fa24ffc7776dc59fb8d37` |
| Code commit | `3ed113bf35546964cd4a56f424578dc1213f41fa` |
| Tested code tree | `876e810757c55b098d805e6e9ac012da7e6cab4d` |
| Canonical base / current P12 canonical | `a66c215d1e8e9db34ea559a8bf2a0803fbdbf4ab` |
| Accepted design | `e9fbdada98eb6fed2d1e7a446ac9a4085504c676` |
| Exact-tip design review | `6cb0837fdf85c777febeb51f1c20fab65fbf2db6` |

The candidate branch and remote-tracking ref resolve to the same tip. The
candidate is based on the current canonical tip. The canonical ref has not
advanced from that base during this review.

## Findings

The code changes match the accepted, bounded P12-B operation contract. The
outer selected-profile daily pass admits the existing
`MerchantSystem.AdvanceNpcTradeState` call under the registered
`runtime.merchant.advance-npc-trade-state` operation. Exact current roster
actors and embedded owner instances are checked before the domain call. Each
NPC contributes one merchant-plan and one travel-plan section, plus the
already reviewed SpatialKnowledge pair and CommercialKnowledge triple. The
operation batches committed changed sections into one post-commit epoch and
keeps accounting active through `Dispose`; direct supported owner commits
outside the operation refresh their baselines immediately.

The changed code since the previously reviewed implementation tip
`1701b416c1524080e5017ef722e9e2645cd9b261` is limited to the 45-line
`SimulationRuntimeAdmissionTests` regression. Candidate evidence and the
design document also received documentation-only updates. No production source
changed in this revision.

The new test,
`DirectPlanOwnerCommitsOutsideMerchantOperationRefreshTheirBaselinesImmediately`,
constructs an admitted runtime without a MerchantSystem operation caller,
then directly commits `MerchantTradePlan.Set` and `TravelPlan.Set`. After each
successful change it checks the plan's local revision, the immediate single
epoch increment, and successful census assessment. The test appears as passed
in the exact-tree `SimulationRuntimeAdmissionTests` XML (31/31). This resolves
the prior review's direct-plan-owner invalidation coverage gap.

The broader source review remains applicable because production code did not
change after it. Owner callbacks are tied to exact installed owner identities,
serialized runtime-thread admission, and local revisions; wrong-thread or
stale-baseline writes are rejected before owner facts change. The operation
does not claim whole-call rollback: already committed writes are reported in
the exit path while domain exceptions and state are preserved. The operation
does not add a durable occurrence receipt or exactly-once promise, alter
Merchant decision/result semantics, or include allocator/sequence/decision
store roots in its mutation claim.

The complete candidate diff from the canonical base was reviewed, including
its design and evidence documents, census providers, runtime admission and
owner hooks, and focused tests. Temporal actor identity/cardinality, dynamic
roster reconciliation, deterministic ordering, revision behavior, stale and
wrong-thread rejection, partial-commit behavior, reconstruction-sensitive
inputs, and overlap with the `SimulationRuntime` hotspot were checked against
the accepted design and current canonical sources. No material defect or new
product/architecture choice was found.

## Exact-tree validation evidence

The candidate's evidence document reports the following validation against
code tree `876e810757c55b098d805e6e9ac012da7e6cab4d`. I inspected the retained
XML/log artifacts and recomputed each SHA-256; every pair matches the
candidate document. The XML reports all tests passed. I did not rerun Unity.

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

No remaining focused-test gap was identified for the previously requested
direct plan-owner invalidation behavior. The separate contract exclusions
remain limitations, not review defects: P12-B is incomplete, P12-A remains
`WAIT_DEPENDENCY`, P13 remains blocked, and this candidate makes no claim of
complete census coverage, complete shared-epoch coverage, capture eligibility,
export, or hydration. Other Merchant entrypoints and unrelated writer
families remain outside this operation.

Candidate checkout user changes were observed and left untouched: the two
`ProjectSettings` edits and two untracked `.meta` files. No edits were made to
the candidate checkout.
