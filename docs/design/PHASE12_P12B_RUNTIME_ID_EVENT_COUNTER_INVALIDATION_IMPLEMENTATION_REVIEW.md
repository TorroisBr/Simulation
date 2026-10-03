# P12-B RuntimeIdAllocator Event-counter invalidation implementation review

**Verdict:** PASS — independent exact-tip code review.

## Reviewed identity

- Canonical base: `aa8f0305bea9f10c15045e07400d8785c2bd9e23`
- Code commit: `a573e5120951f8ac10c2da5b6ad79e066991a57a`
- Code tree: `8e3e2966601d834c2c23429e253d02a9d1a7bb8c`
- Candidate evidence commit reviewed: `4a1aa0a99830a188b6ba584fe5244ad706a45c0d`
- The evidence commit is a direct child of the code commit and changes only
  `PHASE12_P12B_RUNTIME_ID_EVENT_COUNTER_INVALIDATION_CANDIDATE.md`; the reviewed
  code tree is unchanged.
- Validation and candidate evidence:
  `PHASE12_P12B_RUNTIME_ID_EVENT_COUNTER_INVALIDATION_CANDIDATE.md` at the
  evidence commit above.

## Review findings

The exact base-to-code diff was reviewed. The Event exhaustion message matches
the pre-existing allocator behavior exactly:
`RuntimeId sequence exhausted for type 'event'.` The selected profile registers
and binds only its exact Events counter. Before incrementing, the callback
checks allocator exhaustion, owner thread, section baseline, and mutation-epoch
capacity. Successful allocation is reported through the existing batch-aware
notification path. Notification failure faults admission and propagates
without rolling back the consumed ID.

Coverage includes direct allocation, stale baseline, wrong thread, exhaustion,
epoch `long.MaxValue` and `long.MaxValue - 1`, a later event-factory failure,
exact bootstrap owner composition, TravelParty batching, and unchanged non-P12
ID output. No actionable review findings remain.

## Independently verified validation

All XML and log SHA-256 values were recomputed and matched the candidate
evidence record. Every XML reports Passed, with zero failures, skipped, or
inconclusive tests.

| Gate | Result | XML SHA-256 | Log SHA-256 |
|---|---:|---|---|
| `SimulationRecordSequenceP12InvalidationTests` | 11/11 | `E3775F97AD50A347E43426BDF20BB67C6DEC4859C5730396167453110E9A0F66` | `1CBC8B11B210F42FD028E1533C315B65372AA7826BA7421CDAAFF667D2EF12D7` |
| `P12TravelPartyAdvanceTests` | 10/10 | `32C712D400FD2A96EEFFC29114BD31C4A2B974977EE0ABEAE1CF838759D52ED7` | `EF253437C043A4A503EA31B8399299269C760986F989C39E30ED94CA82F67545` |
| `SimulationBootstrapCompositionTests` | 21/21 | `3F5C6BF582F1DD7D25EBE2DF14E1F1E8793BF554FE9B2FA49A4DC62CFE58BDB7` | `BDD6B0A1700D7B322C64C3ED0736C6DE4E9F6404BBE8FC032DAFC45B66B7BC82` |
| ALL EditMode | 2205/2205 | `5E98045693AD3DBB098AF93978C986846B8F37927E46BF0B7865BAB28935163F` | `5BD5B0E803FBC4AF62E3214A9A5708BB12F4BCCC82779AC76DF9036A53715BEE` |
| Official Smoke (`EditMode -TestFilter Smoke`) | 5/5 | `87E153B5D6A8EEF9AD467FBD63150A94C1F337CE2CFD7B13FB1553788F248353` | `34903C87822719DA22E504579F5BDBC2E28F47EEB24A77197E590D5F6FD07FAE` |

`git diff --check` passes for the exact candidate diff.

## Scope limits

This advances only selected-profile successful Event-ID allocation
invalidation, including coalescing inside an existing mutation batch. It does
not establish complete P12-B owner coverage, complete shared-epoch coverage,
global quiescence, capture eligibility, export, hydration, P12-A readiness,
P13 readiness, or Phase 12 closure. P12-B remains incomplete; P12-A remains
`WAIT_DEPENDENCY`; P13 remains blocked.

This review is not canonical promotion approval.
