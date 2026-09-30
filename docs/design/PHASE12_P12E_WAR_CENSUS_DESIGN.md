# P12-E Persistent War Census Design

**Status:** Initial independent design review PASS; optional-Conflict evidence
refinement is pending exact-tip recheck and durable review record. Existing
accepted P12-B/P12-E capability authorization applies.

**Integration anchor:** reviewed Conflict census candidate
`codex/phase12/P12EConflictCensus` at `b170d4e`.

## Dependency and fixed section

War follows the Conflict owner in the existing domain dependency chain. Add
one schema-v1 passive owner section through the existing
`SimulationBootstrapComposition` handoff:

| Section | Installed owner | Cardinality | Existing stamp |
|---|---|---|---|
| `p12e.wars` | `Runtime.WarStore` | `Count` | `Revision` |

Use the exact runtime-installed `PersistentWarStore` object as opaque owner
identity. `SimulationRuntime` constructs or clones it after the installed
Conflict and ArmedForce stores, validates its references, and binds it to the
runtime mutation guard. The selected authored profile supplies no War state;
the default store therefore starts with count zero and revision zero.

The War section does not duplicate Conflict records, ArmedForce records, or
Battle state. The War owner validates its own sides and bindings, plus
referenced Conflict and ArmedForce identities. Battle remains a downstream
owner and validates its own War references.

## Existing revision semantics

Successful `TryRegister`, `TryAddParticipantBinding`, and `TryEnd` operations
each advance the War store's local revision once. `ConflictId` is optional;
a War with no Conflict reference is valid, while an unresolved supplied
Conflict reference, invalid or unregistered ArmedForce binding, duplicate
identity/binding, missing War, ended War, invalid day, runtime fault, and
revision overflow reject without advancing revision. No War rollback/restore
API was found. The witness reads the existing local state stamp; it is not a
global P12-B mutation epoch or proof of owner-thread/quiescence.

## Required evidence

- The selected live profile reports exact installed-owner identity, schema,
  count/revision zero, and stable identity across repeated reads.
- Register a War linked to the existing Conflict owner, then add a participant
  binding and end the War. Confirm local revision advances while War
  cardinality stays one.
- Verify a War without an optional Conflict reference remains valid.
- Check a War referencing an unregistered Conflict, unregistered-ArmedForce
  binding, duplicate registration, and post-end participant binding preserve
  the witness.
- Reuse existing Conflict/War invariants. The provider is read-only and adds
  no duplicate Conflict facts, validation rules, or write path.

## Deferred boundaries

This slice adds no Battle witness, export, staged hydration, restore, complete
profile census, global epoch wiring, operation/thread scopes, quiescence
proof, or capture eligibility. A later Battle witness is dependency-ordered
after War and must separately cover its rollback revision restoration. P12-B
remains incomplete and P12-A remains `WAIT_DEPENDENCY`.
