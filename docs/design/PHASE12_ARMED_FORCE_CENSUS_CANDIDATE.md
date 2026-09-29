# P12-E ArmedForceStore passive census candidate

**Status:** Implementation and required validation complete; exact-tip integration review pending.

**Integration anchor:** reviewed census integration candidate
`codex/phase12/P12BActorChoiceRecordSequenceIntegration` at `b355919`.
This tree also contains the independently reviewed RuntimeIdAllocator
census candidate, revalidated through the integrated full-suite run.

**Reviewed design:** `codex/phase12/P12EArmedForceCensusDesign` at `72a13a4`;
design review PASS is recorded by
`codex/phase12/P12EArmedForceCensusDesignReview` at `3eca6e0`.

## Delivered boundary

`SimulationBootstrapComposition` publishes three fixed schema-v1 witnesses
from `SimulationRuntime.ArmedForceStore`, the runtime-installed clone:

- `p12e.armed-force.forces` reports the store's force count;
- `p12e.armed-force.contingents` reports its contingent count;
- `p12e.armed-force.relevant-person-references` reports its relevant-Person
  reference count.

All three use the installed store instance as owner identity and its existing
shared revision. The selected daily profile composes the owner with zero
records in all three sections and revision 0. Separate manpower and spatial
position authorities remain separate and unwitnessed.

The census tests cover the live counts through successful force, contingent,
and relevant-Person writes; a same-cardinality force mutation; and a rejected
duplicate registration that preserves counts and revision. The store's
prepared Battle mirror commit/restore methods are internal and unavailable to
the separate EditMode test assembly. Their rollback behavior remains in the
reviewed source audit; this candidate adds no test hook or public API for
those internals. The provider reads the existing owner revision directly.

## Validation

| Gate | Result | Evidence |
|---|---:|---|
| `ArmedForceStoreCensusTests` | 1/1 | `Temp/ValidationResults/EditMode-20260929-233255-227f15f749f54cf788c45f186c756022.xml` |
| `SimulationBootstrapCompositionTests` | 14/14 | `Temp/ValidationResults/EditMode-20260929-233312-e4edb02493674a2aac064aa8e5602139.xml` |
| ALL EditMode | 1975/1975 | `Temp/ValidationResults/EditMode-20260929-233402-bfe01bd212064903b3950fbaf4491eea.xml` |
| Official complete Smoke | 5/5 | `Temp/ValidationResults/EditMode-20260929-233439-9816a5f84ce24f418d6bab911d749c46.xml` |
| `git diff --check` | PASS | Integrated candidate tree |

## Limits retained

This adds passive census evidence only for `ArmedForceStore`. It does not
witness the separate manpower or spatial-position stores, register a complete
P12 census, add global mutation invalidation, establish owner-thread or
quiescence, make capture atomic, grant capture eligibility, or add
serialization/export/hydration/restore. P12-B remains incomplete and P12-A
remains `WAIT_DEPENDENCY`.
