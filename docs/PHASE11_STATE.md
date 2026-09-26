# Phase 11 State — Actor Perspective & Commands v1

**Status:** INTEGRATED REFRESHED CANDIDATE — final review pending; canonical promotion pending

**Canonical base:** `codex/phase8/canonical` at `d95b60d174cb0b17df09e2775b3cbd134c74b21f`

**Architecture baseline:** `c285466c355103d3637ac165246591b72eb7bda0`

**Integrated source commit:** `e40ebd63bcdfb043caa7e50001404afaa0ce85b7` on `codex/phase11/ActorChoiceIntegrationPostP8E`; merge commit `5b4674cf8a59c7376a1d9cad4ca5dad956697cb7` refreshes the reviewed P11 candidate against canonical P8-E at `d95b60d174cb0b17df09e2775b3cbd134c74b21f`.

The candidate combines the P11 actor-choice implementation and diagnostics
with P8-E, which is now canonical. P11's local SellGoods contract has no
semantic dependency on P8-E; the refreshed branch records the shared-runtime
integration against the promoted base. P8-E validation and promotion evidence
remain in `docs/PHASE8_STATE.md`.

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
| P11-03 | Runtime application at the ordinary actor decision boundary and existing SellGoods path | INTEGRATED CANDIDATE | One-shot handling and no-fallback semantics implemented; exact P8-C position/location eligibility applied to input and autonomous local SellGoods. Refreshed `ActorChoice` tests 24/24. |
| P11-04 | Deterministic diagnostics, invariants, focused integration tests, and acceptance review | INTEGRATED CANDIDATE; REFRESHED COMBINED REVIEW PENDING | Actor-choice state is included in canonical snapshots, diffs, formatting, and invariant validation. The prior candidate passed independent review at `a281d98`; missing failed/thrown execution regressions were restored at `e40ebd6`; final review of the corrected refreshed candidate is pending. |

The initial integration review found two defects: autonomous SellGoods could
execute during P8-C transit using stale `CurrentCity`, and P8-E interruption
could accept a caller-supplied day that differed from runtime time. Both were
fixed in `32b16bf5ded371750fb2e028d3f39786dfb85c94` and
`a554e8f9199ee77d98d4a512592e965057d72c0b`. Independent review of the complete
fix diff passed; tests cover transit, exact Location mismatch at the same Hex,
legacy no-position behavior, and future/stale caller days with no mutation.

The refreshed independent review found two missing runtime regression cases
for failed and thrown actor-choice execution. Both are restored in
`e40ebd6`, adapted to the refreshed fixture, and assert one provider creation
and execution, one ActorChoice decision, a terminal attempt result, and no
same-turn autonomous fallback.

## Architecture and dependency revalidation

The Phase 11 entry and technical contracts were reread against canonical
`d95b60d`, architecture update `c285466`, the current Phase 8 State, the
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

The pre-promotion combined review at `a281d98` passed, confirming
one-shot/no-fallback semantics, daily precedence, stable PersonId/action
identity, diagnostics parity, the exact P8-C position rule, and conditional
P18/P20/P19 boundaries. The refreshed candidate now includes canonical P8-E;
independent review of the exact refreshed source is pending.

## Validation evidence

Results marked `5b4674c` ran after merging canonical P8-E. ActorChoice, ALL
EditMode, and Smoke were rerun after the test-only coverage correction at
`e40ebd6`. Every suite passed with zero failures and skips.

| Gate | Result | Retained report |
|---|---:|---|
| `ActorChoice` | 24/24 | `Temp/ValidationResults/EditMode-20260926-212902-f8a155c8beba40e8957da36b11617090.xml` (`e40ebd6`) |
| `ActorActionChoiceCommandTests` | 6/6 | `Temp/ValidationResults/EditMode-20260926-211930-97788d207a25444da25c536a8ae842fd.xml` |
| `SpatialRoutePlanning` | 20/20 | `Temp/ValidationResults/EditMode-20260926-211954-f7f29e8c223543cba78dc703cfcb7730.xml` |
| `SimulationRuntimeLongRunTests` | 7/7 | `Temp/ValidationResults/EditMode-20260926-212013-e79e0af396c64d72878a8517eb9dd4d8.xml` |
| ALL EditMode | 1729/1729 | `Temp/ValidationResults/EditMode-20260926-212925-d486e681b0c34c67a4aa2c5dd9528177.xml` (`e40ebd6`) |
| Official complete Smoke | 5/5 | `Temp/ValidationResults/EditMode-20260926-213005-d3ae3bbfc53f4ef8825109198bb92c01.xml` (`e40ebd6`) |

`git diff --check d95b60d e40ebd6` passed after integration and test correction. The refreshed
candidate still needs independent final review and its own human canonical-
promotion approval. P8-E's implementation and promotion evidence remain
recorded in `docs/PHASE8_STATE.md`.

## Next actions

1. Complete independent review of refreshed source commit `5b4674c` and record
   its verdict and final State SHA.
2. Respect the explicit human promotion gate for this P11 candidate.
3. After promotion, recompute the full DAG and continue only newly unblocked
   Phase work.
