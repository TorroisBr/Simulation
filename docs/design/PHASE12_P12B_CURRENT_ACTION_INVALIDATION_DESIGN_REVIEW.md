# P12-B current-action census and invalidation design review

**Result:** PASS — bounded design is ready for implementation under the
existing accepted P12-B capability authorization.

**Reviewed design tip:** `8cf4ba70ef2793deb3a314b70c4c51bcbfdcd675`

**Reviewed design tree:** `5bb9293f6a3150c8bc0165cfbe0a21953693f40c`

**P12 source base:** `90a8a2aef212bec2c68d7224739ce512d44c9fe3`

**Architecture source:** `e16796014d348e3b59da7ed848101c4c03926ba5`

**Reviewer:** Independent exact-tip review by `/root/p12_cj_revalidation`;
the reviewer did not edit the candidate.

## Findings

- The owner identity/cardinality matches the existing P12-F whole-NPC owner
  seam: one action section per exact admitted `NpcRuntime`, keyed by
  `RuntimeId`, with zero or one consistent current action. Shared immutable
  `NpcActionData` definitions remain valid; mutable `NpcActionRuntime`
  instances cannot be shared between NPC slots.
- Supported Person materialization/binding and ActorChoice paths are included
  without treating the initially empty PersonStore as permanently empty or
  adding external WorldCommand scope. The design preserves current behavior
  for rejected choices, accepted attempts, and the action left in the slot.
- Direct Person death and both legacy and Person-backed resident-death paths
  reserve action revision capacity before domain writes when an action is
  present. The action section joins the existing lifecycle operation's
  changed-section set and uses its shared epoch reservation once. Unsupported
  direct NPC death remains fail-closed on the selected profile.
- Required overflow/no-partial-write and once-only epoch tests are explicit.
  The design introduces no new product decision, operation ID, or broader
  P12 readiness claim.

## Authority and limits

The reviewer found the design consistent with the P12 Brief's P12-F
ActorChoice/whole-NPC seam and the current owner inventory. This review is a
technical-design PASS only. It is not implementation review, canonical
promotion, or a P12-B completion/readiness finding.

P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked.
No complete owner/operation/shared-epoch coverage, capture eligibility,
quiescence, export, hydration, or Phase closure is implied.
