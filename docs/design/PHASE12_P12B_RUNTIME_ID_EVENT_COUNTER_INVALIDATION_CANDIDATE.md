# P12-B selected-profile RuntimeIdAllocator Event-counter invalidation candidate

**Status:** implementation and required validation passed; independent exact-tip
code review is pending. This candidate is not promoted and does not complete
P12-B.

## Candidate identity

- Canonical base: `aa8f0305bea9f10c15045e07400d8785c2bd9e23`
- Technical design: `b82ce73363c0c2e8e6601b461e9846c32ac5ab8b`
- Revised design review and implementation authorization basis:
  `3e54f21bcfa5ca0710bdd7bed715e31870ae4c70` (PASS)
- Code candidate: `5c11d0999d624ff2e2bf417e4cb73e4ede64d5f7`
- Code tree: `ad749622ec48680f09a33deb8561b058a4a6fe73`
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

All retained XML files report `result="Passed"`, zero failures, zero skipped,
and zero inconclusive tests. XML/log artifacts are under
`Library/ValidationResults/P12BEventCounterInvalidation-20261003/` in the
validation worktree.

| Gate | Result | XML | XML SHA-256 | Log | Log SHA-256 |
|---|---:|---|---|---|---|
| `SimulationRecordSequenceP12InvalidationTests` | 11/11 | `EditMode-20261003-033552-34f35db29505456dbabc6477c5851bd2.xml` | `A8FBEA1593EA8C3D7474F99571E088BB607C08AC466EEA4CA053198262990F5C` | `EditMode-20261003-033552-34f35db29505456dbabc6477c5851bd2.log` | `885D22D573D8149A8D8E970CE8C107C27C4B6F3CA4D65D3C90FECDA2BB7CD09F` |
| `P12TravelPartyAdvanceTests` | 10/10 | `EditMode-20261003-033608-6f03900a270c4baaae16a0dca05a758d.xml` | `37D8588F745B95F3A44D461E69BB78C926E187183DB133C84002E30E442B16F9` | `EditMode-20261003-033608-6f03900a270c4baaae16a0dca05a758d.log` | `DC60B5A5F0CD0B62510F359CE448062AF7A526E4A0EBBD766DDB3F0D7DBBB928` |
| `SimulationBootstrapCompositionTests` | 21/21 | `EditMode-20261003-033624-85935ec626a342538b5614f272ea0d03.xml` | `8524AC593F4E97605C09A3FC800EE01E24C2120EC73A6CAAD2F9C6EE828CBAEF` | `EditMode-20261003-033624-85935ec626a342538b5614f272ea0d03.log` | `9B508DA452A20CCD7B0B8ADE32BDA9EAF67E0CC5071DACDC4B38DC551C30BF00` |
| ALL EditMode | 2205/2205 | `EditMode-20261003-033640-810bbecdac7440bcbbc9ae95d3353e5f.xml` | `89BAE36DC4AE3CC0EEAF43FDE2937D0675258D9E783E7D854499007A86EE6625` | `EditMode-20261003-033640-810bbecdac7440bcbbc9ae95d3353e5f.log` | `666609CA79F106102BC3CECC9CE0138CA2A84BBF3885AB73F5F770CFB93839E2` |
| Official Smoke (`EditMode -TestFilter Smoke`) | 5/5 | `EditMode-20261003-033714-a885d915bc94403fa81da8f06f18127a.xml` | `442AF80C1746FFD9010B2A93BC87160AA1DA26F9FB5924E2A57FF571FB330C4D` | `EditMode-20261003-033714-a885d915bc94403fa81da8f06f18127a.log` | `9B1D005C042782B350B4C14405D1BAC907D35D14BA4F80CBCBD4630C38C44254` |

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
