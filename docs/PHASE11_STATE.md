# Phase 11 State — Actor Perspective & Commands v1

**Status:** IN_PROGRESS
**Canonical base:** `codex/phase8/canonical` at `c5b2e06b534f4b2af38f10e6510b10800aa8b28c`
**Architecture baseline:** `c285466c355103d3637ac165246591b72eb7bda0`
**Current integration branch:** `codex/phase11/ActorChoiceIntegration` at `af3b7723ae3d40e859e54d5cc6e5bb5e2e6a833b`

## Authorized bounded scope

On 2026-09-26, the user selected one actor choosing one supported action, with
Local SellGoods as the first consumer. The user specified trusted local
game/UI input naming an eligible actor, without actor-control grants or
security/anti-cheat defenses. The user then directed full-roadmap execution.
The accepted behavior is one-shot: the choice replaces autonomous selection
for that decision; ordinary gameplay and current-truth domain checks remain;
rejection, inability to construct the action, or execution failure does not
trigger an autonomous fallback for the same decision.

The actor's `PersonId` and action `DefinitionId` are the input identity. The
actor's `CommercialKnowledge` informs existing merchant planning; existing
domain execution revalidates current market truth. This authorization is
limited to the documented SellGoods slice and does not authorize new gameplay
actions, a general actor-control system, a Mod API/loader, or a security layer.

## Checkpoints

| ID | Scope | Status | Dependency / evidence |
|---|---|---|---|
| P11-01 | Stable actor-choice input contracts/store, ordered dispositions, and terminal attempt lifecycle | CANDIDATE COMPLETE | Store branch `codex/phase11/ActorChoiceStore` commit `f1221d4e3275e21a876076350ca12058927dbaa9` independently reviewed for compatibility with c5b2 and both alignment records; `ActorChoiceStoreTests` pass 9/9 on this c5b2-based integration branch. |
| P11-02 | Typed WorldCommand ingress and trusted local UI capture | READY | Use the existing WorldCommand/domain boundary; no GM authority expansion. |
| P11-03 | Runtime application at the ordinary actor decision boundary and existing SellGoods path | READY | Preserve scheduled-directive/activity precedence; apply the reviewed P8-C position reconciliation; terminal rejection and every dispatched attempt return without same-decision autonomous fallback. |
| P11-04 | Deterministic diagnostics, invariants, focused integration tests, and Phase acceptance review | BLOCKED BY P11-02/P11-03 | Include input payload, logical boundary, ordering, and terminal disposition in canonical snapshots/diffs. |

P11-03 current-position eligibility is now specified against promoted P8-C:
an absent optional Person position preserves legacy city behavior; `InTransit`
uses the existing `Deferred/Traveling` path; a stable position must be the
exact Location bound to the actor's current City. Anchor-Hex co-location does
not establish Location entry/access. A stable mismatch or missing city
binding rejects the one-shot input as unavailable without autonomous
fallback. This compatibility check is based on P8-C authorities and does not
make P8-E a capability dependency.

P11-02 and P11-03 are semantically independent of P8-E. They are serialized
after the reviewed P8-E implementation only because both use `SimulationRuntime`
and diagnostics shared with it. The P8-E integration candidate at
`07b953bee214728c326a9a121c0f7360382f94a8` passed its independent review and
required gates; P11 runtime work will use that candidate as its implementation
base to preserve the validated owner changes. This is an integration ordering
choice, not a P8-E capability dependency. The bounded daily adapter is
transitional. Intraday use requires exact P18 logical instants and stable
causal ordering. Shared activity participation requires P20 contracts; P20 is
not a gate for this single-actor slice.

## Revalidation and implementation evidence

- The Phase 11 entry and technical designs were independently reviewed against
  c5b2 and the updated architecture. The P11 Store candidate introduces only
  `PersonId` and action-definition identity; it does not introduce an activity
  identity, participant roster, or one-activity-to-one-actor rule.
- `ActorChoiceStoreTests`: 9 passed, 0 failed, 0 skipped on the current
  integration branch. XML:
  `Temp/ValidationResults/EditMode-20260926-183513-9b6bd8038f304e4bab7c2342562384ac.xml`.
- The Store's day/roster ordinal is bounded legacy daily-profile causality,
  not a permanent actor cadence. P18 migration must preserve pending and
  dispatched inputs and add exact logical-time/order fields.
- P20 is not a prerequisite for one-actor SellGoods choice. P19's public
  extension API/loader remains deferred; stable semantic action identity and
  extension-compatible data are review constraints now.
- Luna-first seam review confirmed the P8-C rule against the actual owner APIs
  and the architecture's separation of Location containment, Hex co-location,
  and traversal. It found no remaining product or canonical ambiguity.

## Next action

Implement P11-02 and P11-03 in one isolated runtime integration worktree after
the current P8-E owner-level replan guard has passed independent review. Then
complete P11-04, run targeted and required regression suites, and update this
State before requesting any canonical promotion required by repository policy.
