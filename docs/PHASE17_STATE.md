# Phase 17 State — Strategic War v1

**Status:** PHASE 17 IN PROGRESS — P17-A promoted; later strategic War capabilities and the formal closure demonstration remain open.

**Canonical branch:** `codex/phase17/canonical`. The branch was created by a clean fast-forward from P16 canonical `75a27d7ac97e66c2762835ccea7950a945c2f20d` to the reviewed P17-A candidate and durable review record at `192c3505daeaf7248a1745d57e4369f553b5b0f0`. This State update is documentation-only.

**Architecture:** `codex/architecture/world-identity-projection` at `ffd75652d89d862b83d634868c560f8540869b89`. **P17-A technical design and handoff:** `codex/architecture/p17a-technical-design` at `235de342c45a3f3ef3cc3e095e6b5b69be28f3bd`; reviewed contract content `4088d3485f360144cc60ee301cc4ecb2550ef5bc`.

## Checkpoint status

| Checkpoint | Status | Evidence / boundary |
|---|---|---|
| P17-A — Explicit Withdrawal Demand and Explicit War Termination | PROMOTED | Implementation `172ff15fcc7fbe28577b403bce2cf33eee5ac8a9`, tree `d070c1dfa5c8c6b1429f04a2a4e19067212f1aed`; independent exact-tip review `VALIDATED_CANDIDATE` is recorded in `docs/design/PHASE17A_IMPLEMENTATION_REVIEW.md`. |

## P17-A promotion evidence

The P17-A implementation is based on current P16-A canonical
`75a27d7ac97e66c2762835ccea7950a945c2f20d`. The required P7, P8, P12, P16,
architecture, and P17 design refs were refreshed before review and promotion;
they matched the handoff assumptions. The reviewed code tree did not change
after validation. The review-record commit adds only its review document and
the promotion adds no source-code delta.

Independent exact-tip code review passed for candidate
`172ff15fcc7fbe28577b403bce2cf33eee5ac8a9`, tree
`d070c1dfa5c8c6b1429f04a2a4e19067212f1aed`. Validation on that exact tree:

| Suite | Result |
|---|---:|
| P17-A runtime | 10/10 |
| P16 movement regression | 21/21 |
| Runtime admission | 37/37 |
| ALL EditMode | 2335/2335 |
| Official Smoke | 5/5 |
| `git diff --check` | PASS |

The final evidence archive is
`docs/validation/P17A/P17A-validation-review-fix-20261005.zip`, SHA-256
`FB3871A7B1E16B41A76B5E850D937F5D59FB7E723C0C2B3E749E259B5C7EA80D`.
The independent review verified the retained focused XML/log artifacts and
archive hash. No tests were rerun for the review-record or State-only commits.

## Scope and limitations

P17-A retains the existing P7 War authority and proves one bounded
non-territorial withdrawal demand: participant A's goal is satisfied by the
selected participant B force leaving the specified Hex through one committed
P16 crossing. Goal achievement does not terminate War. An explicit concession
by B ends the bounded War with reason and accepted-input provenance. Movement
remains owned by P16; Battle victory does not satisfy this goal or end War.

P17 state is rejected by Standard and selected P12
`UnityBootstrap-Daily-v1` composition, both with and without matching P16
state. This is a tested exclusion boundary; P17-A does not join or enlarge
the P12 profile. It does not add territorial control, occupation, ownership
or jurisdiction changes, treaties, a universal War winner, generalized
strategic AI, Campaign, or diplomacy.

P17-A does not claim full P12 continuation or save/fork readiness. P12-B
remains incomplete, P12-A remains `WAIT_DEPENDENCY`, and P13 remains blocked.
P17-A implementation and promotion are not Phase 17 closure. The accepted
`FOLLOW-UP_DEMONSTRATION` remains preferred before the separate formal Phase
closure gate.

## Dependency refresh

P17-A's required promoted foundations remain P7 War/force authority, P8
registered spatial truth, and P16-A selected crossing/receipt. Its P12 gate is
the negative selected-profile admission proof, which is now part of the
promoted checkpoint; complete P12-B is not a prerequisite. This promotion
does not change the P12 dependency chain: P12-B remains incomplete; P12-C
waits on B; P12-D/E wait on B and C; P12-F waits on C/D/E; P12-G waits on
B-F plus validated live-profile inventory; P12-A remains `WAIT_DEPENDENCY`;
P13 remains blocked on continuation and recoverable causal history.

Phase 17 remains open for separately scoped strategic War capabilities and
its closure demonstration. No additional P17 checkpoint is made
implementation-ready by P17-A.
