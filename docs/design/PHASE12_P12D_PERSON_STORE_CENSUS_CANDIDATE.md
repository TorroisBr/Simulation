# P12-D PersonStore Registry Census Candidate

**Status:** The bounded implementation is complete, independently reviewed,
and validated. Exact-tip documentation review is pending; the candidate has
not been promoted to P12 canonical. P12-B remains incomplete and P12-A remains
`WAIT_DEPENDENCY`.

**Branch:** `codex/phase12/P12BPersonStoreCensusWitness`.

**Code tip:** `1e8d940d6b8db41098cd99c7c5c1c9f7ed5c7db8`.

**Code tree:** `32ad03b23c58f3a25e085adf2930710e7b464c2f`.

**Base:** P12 canonical `676196bcd807603deb9d01bd2855342a7d47a01e`.

**Accepted design:** `PHASE12_P12D_PERSON_STORE_CENSUS_DESIGN.md`, design tip
`d06f721bc24eb0d7202fa523e495f45df8cb11c6`.

## Delivered bounded surface

The installed `PersonStore` exposes two passive schema-v1 owner sections:

| Section | Cardinality | Revision |
|---|---|---|
| `p12d.person.membership` | `PersonStore.Persons.Count` | `PersonStore.Revision` |
| `p12d.person.materialization-binding` | `PersonStore.MaterializedBindingCount` | `PersonStore.Revision` |

`SimulationBootstrapComposition` publishes a fixed read-only provider list
constructed from its exact `Runtime.PersonStore`. Both witnesses report that
same store object as owner identity. The selected authored profile returns
exact zero for both sections at revision zero, and repeated reads preserve
provider and owner identity.

Successful Person registration, materialization binding, registration
compensation, and binding compensation each advance one shared owner-local
revision. The admission rule reserves one increment for immediate exact
compensation: forward writes reject at `long.MaxValue - 1` and `long.MaxValue`;
from `long.MaxValue - 2`, a forward write and its successful compensation can
advance to Max without wrapping. A rollback attempted at Max fails before
changing the Person row or binding indexes. The materialization rollback
operation is explicitly named as compensation-only; registration rollback
keeps its previous mutation-guard behavior.

The candidate appends `RevisionOverflow` without renumbering the existing
PersonStore or materialization failure values and maps the store failure
explicitly. The section covers structural membership and binding cardinality
only. Person death/residence facts, genealogy, population, exports, and
hydration are excluded.

## Independent review and validation

Independent implementation review passed at exact tip
`1e8d940d6b8db41098cd99c7c5c1c9f7ed5c7db8` against canonical base
`676196bcd807603deb9d01bd2855342a7d47a01e` and accepted design
`d06f721bc24eb0d7202fa523e495f45df8cb11c6`. The review confirmed installed
owner identity, shared revision behavior, reserved headroom, compensation
preflights, guard preservation, enum compatibility, and partial-scope limits.
It specifically rechecked blank NPC runtime ID and cross-Person binding
collision rejection after those cases were added to the census tests.

| Gate | Result | XML |
|---|---:|---|
| `PersonStoreCensusTests` | 7/7 | `Library/ValidationResults/P12BPersonStoreFocus/EditMode-20260930-144032-0697bee127c8475985f3b37daf5068cc.xml` |
| ALL EditMode | 1997/1997 | `Library/ValidationResults/P12BPersonStoreAll/EditMode-20260930-144101-3cf205c78fc44d2ba2b4e8093829f71a.xml` |
| Complete official `Smoke` filter | 5/5 | `Library/ValidationResults/P12BPersonStoreSmoke/EditMode-20260930-144140-c305ce74af5847ddb9c633c8d7e548cc.xml` |
| `git diff --check` | PASS | implementation diff clean |

The focused suite covers exact-zero published witnesses, successful writes and
compensations, duplicate/invalid/future-dated/guard-rejected writes, blank and
reused NPC IDs, mapped materialization overflow, reserved headroom, saturated
rollback rejection, and stable existing failure enum values.

## Limits retained

These are passive, unsynchronized owner witnesses only. They do not register a
complete profile in the P12-B coordinator, connect writes to its shared
invalidation epoch, establish owner-thread/quiescence, or issue capture
eligibility. They do not cover mutable Person facts or complete P12-D.
P12-B remains incomplete, P12-A remains `WAIT_DEPENDENCY`, and no export,
staged hydration, restore, or continuation parity is delivered.
