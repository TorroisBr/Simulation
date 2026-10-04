# P16-A implementation candidate handoff

This candidate implements the force-owned P16-A domain slice in
`ArmedForceSpatialStateStore` and its isolated EditMode proof. It does not
compose the proving profile into `SimulationRuntime` or any Unity bootstrap.
The candidate starts from P12 canonical code `54fc23b89cb13598dd184a2b5b6bacf9dc23b0a7`.

## Candidate validation

- `P16AMilitaryMovementTests`: 10/10 passed.
- `ArmedForceSpatialPositionTests`: 12/12 passed.
- `SpatialPassageAuthorityTests`: 13/13 passed.
- `git diff --check`: clean.
- ALL EditMode is intentionally deferred until the serialized P12 integration
  boundary is assembled.

Unity result XML/log pairs for these focused runs are retained in the
candidate worktree under `Temp/ValidationResults/P16A`.

## Implemented domain boundary

- The existing per-force spatial record now retains optional factual position,
  finite carried quantity, and an immutable successful-crossing receipt.
- A separate P16-A profile factory authors one active selected force at one
  registered Hex with one compatible item definition/content revision, initial
  quantity, and fixed positive one-crossing debit.
- Crossing execution checks the selected force, current source, owner/force/
  passage revisions, registered adjacent endpoints and explicit P8 passage
  option, the fixed `P16A-MilitaryOneHop/v1` traversal context, availability,
  stock, and the one-shot receipt before a single operational-record replacement.
- Direct post-profile `TrySetPosition` and `TryClearPosition` cannot bypass the
  selected force's supply and crossing receipt. Unprofiled P7 position semantics
  remain unchanged.
- Clone copies the selected profile and operational records against target
  force/spatial authorities. `CaptureP16AState` is the exact semantic-state
  export seam; `ValidateP16AStateForHydration` checks force, Hex, passage,
  receipt, item, quantity, and revision relationships without applying state.
  This is not Save/Replay implementation or a P12 adapter.

## Required serial integration after the domain candidate

P12 canonical advanced during this isolated implementation to
`a6572ab3d4330d81edb334ae8b4c84ca5e6b173e`; its code tree remains
`248abaac`/`b8dced9`, with changes in `SimulationRuntime`, bootstrap composition,
and State/docs. Recompose this candidate on that current tip before integration
and classify the State/document drift. Integrate the owner change first, then
make a separate serialized integration change at the P12 runtime/bootstrap
hotspot. Do not infer that the daily profile excludes P16 merely because its
current fixture has no P8-B passage: the existing owner census already sees
`ArmedForceSpatialStateStore`, and supply/profile/receipt state changes the
meaning of that owner.

Before a P16 proving profile is published, the serial P12 integration must
explicitly choose either complete P12 inventory/admission support for the new
selected-force binding, item/revision, initial/current quantity, typed position,
receipt, passage/context revision, logical boundary/order and operation ID, or
fail closed for any populated unsupported P16 state in
`UnityBootstrap-Daily-v1`. The required negative EditMode proof must feed both
an unmoved but populated P16 profile and a successfully crossed P16 state into
the daily-profile admission path and assert rejection before publication; it
must also prove the unchanged zero-P16 daily profile still composes. The test
must inspect exact position, supply, receipt, and owner revision before/after
rejection so no partial admission or dropped owner state is hidden by the
current position-only census witness. That runtime/bootstrap/admission work
belongs to the serialized P12 integration and is deliberately not part of this
isolated candidate.

After rebase/integration, rerun P7 ArmedForce spatial/composition tests, P8
passage tests, P12 owner-census/admission tests, the new P16-A suite, and
`git diff --check` on the exact integration tip. Do not run ALL EditMode until
that integration boundary is assembled.
