# Phase 11 State — Actor Perspective & Commands v1

**Status:** IN_PROGRESS — integrated candidate; canonical promotion pending

**Canonical base:** `codex/phase8/canonical` at `c5b2e06b534f4b2af38f10e6510b10800aa8b28c`

**Architecture baseline:** `c285466c355103d3637ac165246591b72eb7bda0`

**Integrated code/design candidate:** `5d16851172ee8fd0815f944dcbec89f87966c581` on `codex/phase11/ActorChoiceIntegrationRefresh`

The candidate combines the P11 actor-choice implementation and diagnostics
with the separately validated P8-E integration candidate. P8-E remains
noncanonical at this base. P11's local SellGoods contract has no semantic
dependency on P8-E, but this shared-hotspot integration branch includes its
code; therefore P8-E must pass its own human promotion gate before this
combined branch can be promoted. The candidate is not a declaration that P8-E
is canonical.

## Authorized bounded scope

The user selected one actor choosing one supported action, with local
`SellGoods` as the first consumer. The ordinary local game/UI caller is trusted
to name an eligible actor. No actor-control grants, player-to-actor ownership,
anti-cheat, anti-tamper, or security architecture is introduced. The existing
domain/action checks remain responsible for normal gameplay validity.

The choice uses stable `PersonId` and action `DefinitionId` input identity. It
replaces autonomous action selection for one decision only. A rejected choice,
inability to construct its action, or failed/partial execution does not trigger
an autonomous fallback in that same decision. Scheduled-directive and existing
activity precedence remain intact. Actor Knowledge informs the existing
merchant path; the market transaction revalidates current truth.

If P8-C Person position is absent, the legacy `CurrentCity` behavior remains.
If position exists, SellGoods requires the actor to be `At` the exact
`LocationId` bound to its current City. `InTransit`, a stable position at a
different Location, a Hex-only position (even at the City's anchor Hex), or a
missing City binding makes the local action unavailable for that turn. This is
gameplay position eligibility, not caller authorization. P8-E is not a
capability dependency for this rule.

## Checkpoint status

| ID | Scope | Status | Evidence |
|---|---|---|---|
| P11-01 | Stable actor-choice input store, ordered dispositions, terminal attempt lifecycle | CANDIDATE COMPLETE | Store branch `codex/phase11/ActorChoiceStore`, commit `f1221d4e3275e21a876076350ca12058927dbaa9`; independent review against c5b2 and both alignment records; `ActorChoiceStoreTests` 9/9. |
| P11-02 | Typed WorldCommand ingress and trusted local UI capture | INTEGRATED CANDIDATE | Integrated with the ordinary WorldCommand/domain boundary; `ActorActionChoiceCommandTests` 6/6. No GM authority expansion. |
| P11-03 | Runtime application at the ordinary actor decision boundary and existing SellGoods path | INTEGRATED CANDIDATE | One-shot handling and no-fallback semantics implemented; exact P8-C position/location eligibility applied to input and autonomous local SellGoods. `ActorChoice` tests 22/22. |
| P11-04 | Deterministic diagnostics, invariants, focused integration tests, and acceptance review | INTEGRATED CANDIDATE; FINAL COMBINED REVIEW PENDING | Actor-choice state is included in canonical snapshots, diffs, formatting, and invariant validation. Diagnostics tests are included in the integration suite; final whole-candidate review remains outstanding. |

The initial integration review found two defects: autonomous SellGoods could
execute during P8-C transit using stale `CurrentCity`, and P8-E interruption
could accept a caller-supplied day that differed from runtime time. Both were
fixed in `32b16bf5ded371750fb2e028d3f39786dfb85c94` and
`a554e8f9199ee77d98d4a512592e965057d72c0b`. Independent review of the complete
fix diff passed; tests cover transit, exact Location mismatch at the same Hex,
legacy no-position behavior, and future/stale caller days with no mutation.

## Architecture and dependency revalidation

The Phase 11 entry and technical contracts were reread against canonical
`c5b2e06`, architecture update `c285466`, the current Phase 8 State, the
Roadmap and Execution Model, and both 2026-09-26 alignment records.

- The actor-choice input does not invent an activity identity, participant
  roster, or permanent one-activity-to-one-actor rule. Activity definition,
  instance and participant identities remain distinct under the multi-
  participant alignment.
- The current integration is a bounded daily adapter. Exact logical instants
  and stable causal ordering are required if a selected input consumer moves
  into P18 intraday execution; P18-D is the relevant later integration point.
- P20 is not a prerequisite for the single-actor SellGoods choice. P19's public
  extension API and loader remain deferred; stable semantic action identity and
  extension-compatible data shape are review constraints now.
- P8-C is the only spatial capability read by the selected local action. P8-E
  is included in this integration history to serialize shared runtime and
  diagnostics hotspots, not because the SellGoods semantics require civil
  travel.

## Validation evidence

All results below are from the merged integration worktree at candidate code
commit `a554e8f` plus P8-E design-status synchronization. Every suite passed
with zero failures and skips.

| Gate | Result | Retained report |
|---|---:|---|
| `ActorChoice` | 22/22 | `Library/ValidationResults/P11IntegrationRefresh/EditMode-20260926-204716-fa49a3af3d054508a39eeed1e66f5221.xml` and `.log` |
| `ActorActionChoiceCommandTests` | 6/6 | `Library/ValidationResults/P11IntegrationRefresh/EditMode-20260926-204744-8600c723a71149afa76f91f4aa279d0a.xml` and `.log` |
| `SpatialRoutePlanning` | 20/20 | `Library/ValidationResults/P11IntegrationRefresh/EditMode-20260926-204801-102611ee16944b8c9561a29ce1741438.xml` and `.log` |
| `SimulationRuntimeLongRunTests` | 7/7 | `Library/ValidationResults/P11IntegrationRefresh/EditMode-20260926-204850-6b875dd3f9e545a7955b742989ad77c4.xml` and `.log` |
| ALL EditMode | 1727/1727 | `Library/ValidationResults/P11IntegrationRefresh/EditMode-20260926-204934-a1244488dc134cd9885336204f86f1e6.xml` and `.log` |
| Official complete Smoke | 5/5 | `Library/ValidationResults/P11IntegrationRefresh/EditMode-20260926-205015-4d3539cf16654047b45c4bf92debba68.xml` and `.log` |

Focused and final gates were run after the runtime/day fixes. The final
combined-candidate independent review and `git diff --check` are the remaining
integration gates before the required human canonical-promotion approval.
P8-E's implementation and promotion evidence remain recorded independently in
`docs/PHASE8_STATE.md`.

## Next actions

1. Finish independent review of this exact combined candidate and run
   `git diff --check` against the canonical base.
2. Record the final review result, exact integration SHA and diff-check in this
   State; publish the candidate branch.
3. Respect the explicit human promotion gate. Promote P8-E first because its
   candidate code is part of this branch, then refresh this candidate's base
   record and re-evaluate P11 promotion readiness.
4. After successful promotion, recompute the full DAG and continue only the
   newly unblocked Phase work.
