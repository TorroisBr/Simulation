# P12-B ActorChoiceStore census implementation candidate

**Status:** Implementation and required validation complete; independent exact-tip implementation review pending.

**Canonical base:** `codex/phase12/canonical` at
`69f456d5e3c6d6f7e4b85b36e98968ced0549bf3`.

**Reviewed design:** `codex/phase12/P12BActorChoiceCensusDesign` at
`b5504722cc4a12ff271e25f5ea171ee0668bf4ce`; exact-tip technical design review
PASS is recorded by `codex/phase12/P12BActorChoiceCensusDesignReviewRecord`
at `c1292ea`. The reviewer rechecked compatibility against refreshed P12
canonical `69f456d` and confirmed accepted P12-B–P12-G authorization covers
this passive witness slice.

## Delivered boundary

The installed `ActorChoiceStore` now reports two schema-v1 owner sections:

- `p12f.actor-choice-inputs`: retained inputs without temporal capture;
- `p12f.actor-choice-temporal-inputs`: retained P18 temporal inputs.

Both providers bind to `SimulationRuntime.ActorChoiceStore`, which is the
runtime-installed clone. They share one stable opaque token and one monotonic
owner revision. The selected daily profile starts with both section counts at
zero. Separate cardinalities preserve the proof that excluded temporal input
state remains empty after P11 history evolves.

The revision advances exactly once after each new P11/P18 capture and each
new P11/P18 disposition append, including same-cardinality transitions.
Identical temporal capture/operation replays and failed or conflicting
operations leave the revision unchanged. Revision exhaustion is returned
through `ActorChoiceStoreFailureCode.RevisionExhausted` before a new row or
disposition is installed. `Clone` copies the source revision and records but
retains its own distinct owner identity token.

The `RuntimeFaulted` result applies to the existing mutation-guard rejection
path. No new transactional guarantee is claimed for unexpected thrown
exceptions. The provider remains passive and is not registered in a complete
profile protocol.

## Validation

| Gate | Result | Evidence |
|---|---:|---|
| `ActorChoiceCensusTests` | 5/5 | `Temp/ValidationResults/EditMode-20260929-230033-ad01c541c1ac4d27aaf8ecfbd840f2ed.xml` |
| Complete `ActorChoice` filter | 45/45 | `Temp/ValidationResults/EditMode-20260929-230050-ca2be48884cd4d87a6d1e7876a3b9159.xml` |
| `SimulationBootstrapCompositionTests` | 14/14 | `Temp/ValidationResults/EditMode-20260929-230107-5efece0fd38b4c8aadef239f46cf9358.xml` |
| ALL EditMode | 1968/1968 | `Temp/ValidationResults/EditMode-20260929-230142-ff815c5292bc43fb8c2d30d3b0600528.xml` |
| Official complete Smoke | 5/5 | `Temp/ValidationResults/EditMode-20260929-230221-5d6857d8f44045d0a381d244f78d44aa.xml` |
| `git diff --check` | PASS | Implementation tree after the candidate evidence update |

## Limits retained

This adds only live ActorChoice owner census evidence. It does not add the
shared P12 mutation epoch, runtime section registration, owner-thread or
quiescence enforcement, capture eligibility, serialization, export, staged
hydration, or restore behavior. P18 temporal inputs remain excluded and must
be exact-zero for `UnityBootstrap-Daily-v1`. P12-B remains incomplete and
P12-A remains `WAIT_DEPENDENCY` pending full included-owner coverage,
committed-write mapping, owner-thread/quiescence proof, staged hydration, and
its separate implementation authorization.
