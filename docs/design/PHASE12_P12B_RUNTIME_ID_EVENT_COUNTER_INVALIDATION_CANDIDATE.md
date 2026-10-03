# P12-B selected-profile RuntimeIdAllocator Event-counter invalidation candidate

**Status:** implementation and required validation passed. Independent exact-tip
review PASS is recorded in
`PHASE12_P12B_RUNTIME_ID_EVENT_COUNTER_INVALIDATION_IMPLEMENTATION_REVIEW.md`.
This candidate is not promoted and does not complete P12-B.

## Candidate identity

- Canonical base: `aa8f0305bea9f10c15045e07400d8785c2bd9e23`
- Technical design: `b82ce73363c0c2e8e6601b461e9846c32ac5ab8b`
- Revised design review and implementation authorization basis:
  `3e54f21bcfa5ca0710bdd7bed715e31870ae4c70` (PASS)
- Code candidate: `a573e5120951f8ac10c2da5b6ad79e066991a57a`
- Code tree: `8e3e2966601d834c2c23429e253d02a9d1a7bb8c`
- Implementation branch:
  `codex/phase12/P12BRuntimeIdEventCounterInvalidationImplementation`
- Validation worktree:
  `E:/GitHub/GeneralSimulation/MainSimulation/.worktrees/p12-record-sequence-validation`

## Bounded delivery

The selected P12 runtime registers only the existing cardinality-one
`p12c.runtime-id-allocator.events` section when given the exact allocator. The
Unity bootstrap now passes the allocator shared with `DomainEventRecorder`.
Before `AllocateEventId()` increments its cursor, the bound selected-profile
callback checks allocator exhaustion, owner-thread and exact section baseline,
then validates that the shared mutation epoch has capacity. A successful
counter increment reports only the Events section through the existing P12
notification path. Event allocations inside a TravelParty or Merchant batch
join its existing changed-section set; direct allocations advance one epoch.
Notification failure after allocation faults admission and propagates without
rolling back the consumed ID.

Bootstrap composition compares its allocator-built Events witness to the
runtime-bound witness by section, schema, cardinality, exact opaque owner, and
current revision. Non-P12 allocation behavior and all other thirteen allocator
counters are unchanged.

Focused tests cover the exact witness and owner comparison, direct successful
allocation, stale baseline and wrong-thread rejection, exhaustion, the
`long.MaxValue`/`long.MaxValue - 1` epoch-capacity boundary, event-factory
failure after ID allocation, unchanged non-P12 ID output, exact bootstrap
composition, and Event-counter batching into the existing TravelParty arrival
epoch. The implementation does not add rollback semantics.

## Validation on exact code tree

The compatibility correction preserves the pre-existing Event allocator
exhaustion message exactly and adds an assertion for it. All required suites
were rerun on code candidate `a573e5120951f8ac10c2da5b6ad79e066991a57a`,
tree `8e3e2966601d834c2c23429e253d02a9d1a7bb8c`. Every retained XML reports
`result="Passed"`, zero failures, zero skipped, and zero inconclusive tests.
XML/log artifacts are under
`Library/ValidationResults/P12BEventCounterInvalidation-20261003-r2/` in the
validation worktree.

| Gate | Result | XML | XML SHA-256 | Log | Log SHA-256 |
|---|---:|---|---|---|---|
| `SimulationRecordSequenceP12InvalidationTests` | 11/11 | `EditMode-20261003-034339-9dec2d8f361a4d43bbfa6c23e5b12788.xml` | `E3775F97AD50A347E43426BDF20BB67C6DEC4859C5730396167453110E9A0F66` | `EditMode-20261003-034339-9dec2d8f361a4d43bbfa6c23e5b12788.log` | `1CBC8B11B210F42FD028E1533C315B65372AA7826BA7421CDAAFF667D2EF12D7` |
| `P12TravelPartyAdvanceTests` | 10/10 | `EditMode-20261003-034357-6460af2f54954cf6a4a76fd4c7205d6c.xml` | `32C712D400FD2A96EEFFC29114BD31C4A2B974977EE0ABEAE1CF838759D52ED7` | `EditMode-20261003-034357-6460af2f54954cf6a4a76fd4c7205d6c.log` | `EF253437C043A4A503EA31B8399299269C760986F989C39E30ED94CA82F67545` |
| `SimulationBootstrapCompositionTests` | 21/21 | `EditMode-20261003-034411-6680fd5bbc5d4af2bf75e039983b7e40.xml` | `3F5C6BF582F1DD7D25EBE2DF14E1F1E8793BF554FE9B2FA49A4DC62CFE58BDB7` | `EditMode-20261003-034411-6680fd5bbc5d4af2bf75e039983b7e40.log` | `BDD6B0A1700D7B322C64C3ED0736C6DE4E9F6404BBE8FC032DAFC45B66B7BC82` |
| ALL EditMode | 2205/2205 | `EditMode-20261003-034430-683d12d4080c43308a1da7573e19e5ea.xml` | `5E98045693AD3DBB098AF93978C986846B8F37927E46BF0B7865BAB28935163F` | `EditMode-20261003-034430-683d12d4080c43308a1da7573e19e5ea.log` | `5BD5B0E803FBC4AF62E3214A9A5708BB12F4BCCC82779AC76DF9036A53715BEE` |
| Official Smoke (`EditMode -TestFilter Smoke`) | 5/5 | `EditMode-20261003-034508-80fe860ba6cf4bf48346e9565f1d58a4.xml` | `87E153B5D6A8EEF9AD467FBD63150A94C1F337CE2CFD7B13FB1553788F248353` | `EditMode-20261003-034508-80fe860ba6cf4bf48346e9565f1d58a4.log` | `34903C87822719DA22E504579F5BDBC2E28F47EEB24A77197E590D5F6FD07FAE` |

The exact candidate diff from canonical passes `git diff --check`. The
unrelated `ProjectSettings` edits and untracked ArmedForce `.meta` files remain
outside the candidate commit and were not staged or changed by this work.

## Limits retained

This candidate connects only successful selected-profile Event-ID allocations
to the partial P12 mutation epoch. It does not add Event export/hydration,
decision-ID coverage, other allocator counters, a solo-travel operation,
complete owner or shared-epoch coverage, global quiescence, capture eligibility,
P12-A readiness, P12-B completion, P13 readiness, or Phase 12 closure. P12-B
remains incomplete; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked.
