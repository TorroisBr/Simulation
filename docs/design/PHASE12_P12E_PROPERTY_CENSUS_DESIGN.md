# P12-E Property Ownership Census Design

**Status:** Proposed bounded owner witness; independent exact-tip design review
pending.

**Base:** implementation-reviewed Estate census candidate
`codex/phase12/P12EEstateCensus` at
`da611028c4fd9066fef740e2eda84838c7362cd2`.

**Authority:** `docs/design/PHASE12_TECHNICAL_DESIGN.md` includes property
among the supported core runtime authorities and permits exact empty state.
Existing accepted P12-B/P12-E capability authorization covers these passive
sections; P12-A remains blocked.

## Fixed sections and owner

The installed `Runtime.PropertyOwnershipStore` owns two related but distinct
record sets under one local revision. Expose each with its own fixed schema-v1
section, both reporting the exact same installed owner identity:

| Section | Installed owner | Cardinality | Existing stamp |
|---|---|---|---|
| `p12e.property.ownership` | `Runtime.PropertyOwnershipStore` | `Count` | `Revision` |
| `p12e.property.transfer-history` | `Runtime.PropertyOwnershipStore` | `TransferHistory.Count` | `Revision` |

Current ownership contains one row per registered `PropertyId`. Transfer
history contains one row per retained explicit transfer. The two
cardinalities must not be added together or collapsed into one count. The
same owner identity and revision across two section IDs reflect two views of
distinct facts in one authority; the accepted census protocol already allows
multiple sections from one owner. The selected authored bootstrap begins with
both sets empty and must report exact zero cardinalities/revision and stable
owner identity on repeated reads.

The provider only reads existing facts. It does not add property, transfer,
estate, or succession behavior.

## Existing revision and write semantics

A supported successful `TryRegisterPropertyOwnership` adds one current
ownership row and advances the store revision once; it adds no transfer
history. A successful `TryTransferProperty` replaces the current owner row,
appends one transfer-history record atomically, and advances the same revision
once. Thus after one registration and one transfer the cardinalities are
ownership 1, history 1, with revision 2. Rejected duplicate registration,
same-owner transfer, missing-property transfer, stale transition, invalid or
unregistered Person, runtime fault, and revision overflow leave the witness
unchanged.

Runtime composition clones both current rows and retained history into the
installed store; the internal history insertion used for that construction
also advances the new store's revision. It does not mutate the source store.
The provider does not add a shared P12-B epoch or claim that every possible
write path has been registered for invalidation.

## Required evidence

- The selected live profile reports exact-zero ownership/history cardinality
  and revision under two stable sections bound to the same exact
  `Runtime.PropertyOwnershipStore` object.
- Through normal runtime operations, register one property and verify
  ownership 1/history 0/revision 1, then transfer it and verify
  ownership 1/history 1/revision 2. Confirm the history identifies the
  previous/new owners and the live record has the new owner.
- Duplicate property registration and a rejected transfer leave both
  sections' counts, owner identity, and shared revision unchanged.
- Reuse existing property and transfer semantics, same-world Person
  validation, stale-transition check, runtime guard, and transfer history.
  No new setter, write path, repair, or transfer behavior is added.

## Deferred boundaries

This slice does not alter estate opening, succession, inheritance, ownership
law, or gameplay. It provides no property export/staged hydration, complete
profile owner registration, shared invalidation, owner-thread/quiescence,
capture eligibility, or restore. It does not complete P12-B or make P12-A
`READY`.
