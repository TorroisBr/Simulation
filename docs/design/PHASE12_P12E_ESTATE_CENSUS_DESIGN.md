# P12-E Estate Census Design

**Status:** Implementation and validation complete; independent design and
exact-tip implementation reviews PASS. Records:
`PHASE12_P12E_ESTATE_CENSUS_DESIGN_REVIEW.md` and
`PHASE12_P12E_ESTATE_CENSUS_REVIEW.md`.

**Base:** implementation-reviewed Battle census candidate
`codex/phase12/P12EBattleCensus` at
`6300c5b3debd1ebbca7c68a545dc9c3469dd5d50`.

**Authorization:** Existing accepted P12-B/P12-E capability authorization
covers this passive owner census. P12-A profile integration and its separate
implementation authorization remain blocked.

## Dependency and fixed section

The accepted profile includes the runtime's Estate owner as a supported
domain state authority. Add one fixed schema-v1 section, `p12e.estate.records`,
through the existing `SimulationBootstrapComposition` handoff:

| Section | Installed owner | Cardinality | Existing stamp |
|---|---|---|---|
| `p12e.estate.records` | `Runtime.EstateStore` | `Count` | `Revision` |

The provider reports the exact installed `EstateStore` instance as opaque
owner identity. `Count` is one row per retained `EstateId`; the store also
maintains a unique deceased-Person index, but that secondary lookup index is
not a second set of estate records. The selected authored bootstrap contains
no Estate rows, so its profile witness must report exact count/revision zero
and stable installed-owner identity across repeated reads.

The read provider adds no Estate mutation or lifecycle behavior. It does not
open an estate when a Person dies; only the existing explicit
`SimulationRuntime.TryOpenEstate` domain operation can do so.

## Existing revision and write semantics

`EstateStore.TryRegister` is the only owner write. A live explicit open reaches
it through the existing Estate-opening path. Runtime composition also uses
that internal registration while populating a new cloned store; this is
construction-time copy population, not another live gameplay writer. A
successful open inserts the EstateId row and the deceased-Person secondary
index together, then advances the installed store's local revision once.
Duplicate EstateId, an existing estate for the same deceased Person, an
unregistered or living Person, invalid/stale opening data, a faulted runtime,
or revision overflow rejects without advancing the witness. The installed
store is bound to the runtime mutation guard and is cloned against the
runtime-resolved PersonStore during composition.

No delete or Estate record update API exists in the current owner. The witness
reports its present local revision; it does not connect Estate writes to the
P12-B shared mutation epoch or establish whole-profile invalidation.

## Required evidence

- The selected live profile reports schema-v1, exact `Runtime.EstateStore`
  identity, count/revision zero, and stable identity on repeated reads.
- A focused runtime test registers one already-deceased Person before runtime
  composition, explicitly opens one Estate through
  `SimulationRuntime.TryOpenEstate`, and verifies the installed-owner witness
  advances from zero rows/revision to one row/revision.
- Repeating the open for that deceased Person, reusing the same EstateId for a
  different deceased Person, or opening for an unknown/living Person leaves
  count and revision unchanged. Existing Estate domain failures remain
  authoritative; the census adds no alternate validation.
- Verify the store retains one Estate record and its deceased-Person lookup
  resolves that same record; do not double-count the secondary index.
- Reuse existing Estate invariants, runtime guard, and composition behavior.

## Deferred boundaries

This slice does not change factual Person death, Estate opening semantics,
property ownership/transfer, succession, inheritance distribution, or gameplay.
It does not deliver Estate export or staged hydration, complete P12 profile
coverage, shared invalidation, owner-thread/quiescence, capture eligibility, or
restore. The Estate witness is a P12-E census checkpoint and does not complete
P12-B or make P12-A `READY`.
