# P12-E Conflict Owner Snapshot Design — Exact-Tip Review

**Result: PASS — READY_FOR_IMPLEMENTATION for the bounded owner slice only.**

- Candidate branch: `codex/phase12/P12EConflictCurrentBaseDesign`
- Exact candidate: `9ea0e122ce7916ac4f5cd0b5332db6a980c6346d`
- Candidate root tree: `9a82326004c3e964b9076e8a5c88327d6cb00956`
- Reviewed design blob: `222c2d42f11547ed729ae60fa7ae448c9a393799`
- P12 canonical base/current during review: `a4ce0abcf261226f4b52fbacc8df9ef8f67a0de2`
- Architecture authority: `codex/architecture/world-identity-projection` at `47eff220c7ce00f6e7c759bdc2b76780bb46f628`
- Current P12 Brief blob: `31d9e1b4df41fc994f0a747274e35c4ef40b6e3a`
- Current P12 State blob: `700a1fad188049f23819c0beae23ea4e34e1ad1f`
- Accepted P12-E design blob: `15aaee09d3295cff81a48e166b620c89f5156346`
- Existing Conflict census design blob: `4b5625b8ac6e8a5ef32771e4fc57b893163c7cf5`
- Owner inventory blob: `0757cd0c39e7c1e53ae0c0ae99fe191151ef5dc3`

The remote candidate, canonical, and architecture refs matched those exact SHAs during review. The full candidate diff against P12 canonical is one new design document. `git diff --check` passes.

## Findings

No blocking findings. The proposal is a bounded P12-E owner snapshot and private staged-hydration design for the existing `PersistentConflictStore`.

- **Accepted owner boundary:** The selected Daily-v1 inventory admits `p12e.conflicts` as a Required schema-v1 section with exact owner identity, cardinality, and local revision. The empty day-zero fixture is not an exclusion. Populated Conflict rows are therefore in the already accepted E export/hydration scope; this design does not add a new gameplay promise.
- **Value and revision fidelity:** The proposed DTOs cover all retained fields in `PersistentConflictRecord`, `ConflictStateSide`, and `ConflictParticipantBinding`: IDs, created/ended day and lifecycle, side IDs/display names, and participant binding/parent/side/ArmedForce references. This matches the current source. Ordinal order, immutable detached collections, exact cardinality, and exact local revision are preserved without replaying registration, binding, or end operations.
- **Writer and invariant audit:** The three supported successful store mutations are `TryRegister`, `TryAddParticipantBinding`, and `TryEnd`; each validates before its one revision increment. Their failure and overflow behavior, two-side minimum, per-Conflict side/binding uniqueness, parent references, required Force existence, and the distinction between initial registration and newly added active-Force bindings match `PersistentConflictWarBattleStores.cs`. Hydration correctly does not require a previously bound Force to remain active.
- **Capture binding:** The design consumes the existing completed-boundary token and the unique Required `p12e.conflicts` witness, checking schema, exact installed store identity, count, and revision against the same owner-section vector. Its detached copy and before/after witness checks use the existing P12-B boundary contract; it adds no lock, epoch, thread-safety, or quiescence claim.
- **Staging and relation order:** The private factory validates the complete detached package and references against the exact staged `ArmedForceStore`, restores the captured revision through an owner-private seam, and returns no partial owner on failure. The order ArmedForce → Conflict → War → Battle matches the store constructors and references. War and Battle keep their own Conflict links; Conflict gains no reverse authority or duplicated facts. Whole-graph validation, guard binding, and atomic runtime publication remain with P12-G.
- **Scope and evidence plan:** The design preserves the architecture’s distinction between Conflict, War, and Battle and adds no lifecycle, outcome, resolution, casualty, control, or other gameplay. Its required empty/populated round trips, writer and rejection invariance, malformed-reference/import rejection, token/witness mismatch, no-partial-stage, and later War/Battle binding tests are appropriate for implementation. P10/P17, runtime/bootstrap composition, P12-G publication/parity, P12-A readiness, and P13 remain excluded.

## Readiness and limits

Under the already accepted P12-E prerequisite authorization, this exact design is **READY_FOR_IMPLEMENTATION for the Conflict owner snapshot/private staged-hydration slice**, after the normal immediate canonical/source revalidation. The slice uses the existing promoted ArmedForce owner and does not wait for unrelated P12-D NPC integration. Serialize edits to `PersistentConflictWarBattleStores.cs` with concurrent War/Battle work; new DTOs and focused tests can remain isolated.

This is a design review only. No Unity/tests were run or required, no candidate file was changed, and no capability or Phase was promoted. P12-E remains open; P12-A remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`.