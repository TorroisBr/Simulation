# P12-D Genealogy Saturation Rollback Amendment

**Base:** P12 canonical `543196a6e29b10675cfab70d4883c1d7f5551195`.

**Purpose:** Correct the promoted passive Genealogy witness's interaction with
the existing named-birth rollback path. This is a bounded technical correction
within accepted P12-D/P12-B scope; it adds no gameplay behavior.

## Evidence-backed failure

`PersonBirthLifecycleSystem.TryApplyNamedBirth` registers a candidate Person,
adds each proposed parent edge through `PersonGenealogySystem.TryStoreAdd`,
and removes already-added edges if a later edge or the aggregate population
commit fails. The current promoted store advances its revision on every normal
add and remove, and rejects both operations at `long.MaxValue`.

If a two-parent birth starts at revision `long.MaxValue - 1`, the first edge
advances the revision to `long.MaxValue`; the second edge is rejected before
mutation. The rollback currently tries the normal remove, which is also
rejected at saturation, and then unregisters the candidate Person. This leaves
an edge to a Person that does not exist in `PersonStore`.

## Bounded correction contract

- Keep public `TryAddParentage` and ordinary public `TryRemoveParentage`
  semantics unchanged: both reject with `RevisionOverflow` before mutation
  when the owner revision is already `long.MaxValue`.
- Add an internal, rollback-only store path for removing an edge that this
  named-birth attempt previously installed. It uses the same runtime guard and
  validates that the exact edge exists.
- Below saturation, a successful rollback removal advances the revision once,
  matching ordinary removal.
- At saturation, the rollback-only path removes the edge and its adjacency
  indexes but leaves the revision at `long.MaxValue`. Successful Adds are
  already closed at this revision, and the cleanup strictly decreases direct
  edge cardinality, so the `(Count, Revision)` witness changes and cannot be
  replaced by a same-cardinality Add at saturation.
- Route only `PersonBirthLifecycleSystem.RollbackParentage` through this
  internal path. Public removal still fails at saturation; no other system
  gains a removal bypass.
- The rollback path must report whether cleanup succeeded. If it cannot remove
  the exact installed edge, retain the existing failure handling and do not
  claim transaction atomicity beyond what is proven.

This saturated cleanup is a narrowly scoped compensation after a previously
committed edge addition, not a general successful `TryRemoveParentage` at
saturation. The passive witness remains unsynchronized and still does not
prove a capture boundary.

## Required regression evidence

Set the store revision to `long.MaxValue - 1` in the test assembly. Register two
valid parent Persons, then apply a two-parent named birth. It must fail without
leaving the child registered, any parentage edge, or a population change; the
store revision may be `long.MaxValue` because the first edge committed before
the second edge was rejected and the rollback removed it without wrapping.
Also prove ordinary public add and remove still reject at `long.MaxValue`, and
that count/revision are unchanged by those rejections.

Review the full diff against the failure path above. Run focused Genealogy and
named-birth suites, ALL EditMode, the complete official `Smoke` filter, and
`git diff --check` before any canonical promotion request. P12-B remains
incomplete; this correction does not establish shared-epoch invalidation,
quiescence, capture eligibility, P12-A readiness, or P12-D completion.
