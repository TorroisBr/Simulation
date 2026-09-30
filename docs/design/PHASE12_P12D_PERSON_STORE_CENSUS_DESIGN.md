# P12-D PersonStore Registry Census Design

**Status:** Bounded technical design for independent review.

**Base:** `codex/phase12/canonical` at
`676196bcd807603deb9d01bd2855342a7d47a01e`.

**Authority:** The accepted P12-B live owner/cardinality census includes the
P12-D Person registry. The P12 capability decomposition requires bounded
technical design and independent review before implementation. This adds no
new product behavior or checkpoint scope.

## Purpose and boundary

Publish passive owner-issued evidence for the selected
`UnityBootstrap-Daily-v1` runtime's Person registry structure: registered
Person cardinality and Person-to-materialized-NPC binding cardinality. The
selected authored bootstrap composes an empty `PersonStore`; these witnesses
must distinguish that exact-zero owner from an absent owner and report later
successful structural changes.

The witness covers registry membership and binding-map cardinality only. It
does not witness mutable `PersonRuntime` facts such as birth/death dates or
residence, NPC facts, genealogy, residence/population relations, identity
registry state, or any export/hydration representation. Those remain separate
P12-D/P12-E ownership work. This passive evidence does not register a complete
P12-B profile, connect writes to the shared invalidation epoch, establish
owner-thread/quiescence, or grant capture eligibility.

## Section contract

Publish two required schema-v1 sections from the exact installed `PersonStore`:

| Section | Cardinality | Revision |
|---|---|---|
| `p12d.person.membership` | `PersonStore.Persons.Count` | shared `PersonStore.Revision` |
| `p12d.person.materialization-binding` | `PersonStore.MaterializedBindingCount` | shared `PersonStore.Revision` |

Both witnesses report the same installed store object as owner identity. The
shared revision changes after every successful structural write, including a
write to one section that leaves the other section's cardinality unchanged.
It does not change for Person death-date or residence-field writes; those facts
are excluded from these structural sections and require separate owner evidence
before a complete P12-D Person census can be claimed.
Construction of the provider does not expose a mutable census-registration
hook or raw owner through a new bootstrap API.

`SimulationBootstrapComposition` should expose a fixed read-only provider set
created from `Runtime.PersonStore` after runtime composition. Day-zero evidence
is exact zero for both sections and revision zero; the provider and owner
identity remain stable across repeated reads. Runtime identity is ephemeral
and is not serialized.

## Revision and compensation behavior

Append `RevisionOverflow` to `PersonStoreFailure` without renumbering existing
values. Append the corresponding `RevisionOverflow` to
`PersonMaterializationFailure` and map it explicitly from store failures.
Initialize the owner-local revision at zero. Preflight overflow before any
ordinary registry or binding write:

- successful `TryRegister` increments once;
- successful `TryBindMaterializedNpc` increments once;
- a successful rollback below saturation increments once after removing the
  exact prior write;
- rejection, duplicate/no-op, failed binding, and failed rollback leave
  cardinalities and revision unchanged.

`TryRollbackRegistration` is a compensating removal of the exact unmaterialized
Person registration created by the current named-birth attempt. Materialized
binding removal is likewise only used by the existing materialization
compensation paths; rename the internal store operation to make its
rollback-only role explicit. Preserve current guard behavior and all normal
Person/binding semantics.

If registration or binding advances the revision from `long.MaxValue - 1` to
`long.MaxValue` and the enclosing operation then compensates, the rollback-only
path removes only the exact write already installed by that operation, without
wrapping the revision. The affected cardinality strictly decreases, while
ordinary registration and binding are closed at saturation. Thus the pair of
section cardinalities plus shared revision changes and cannot be replaced by a
supported same-cardinality write at that saturated revision. Do not permit
ordinary Person deletion or unbinding at saturation.

The rollback paths must preserve their existing exact-record/object checks.
They must not become general public removal APIs or bypass the existing
runtime mutation guard for binding operations. Keep the current PersonStore
registration-rollback guard behavior unchanged.

## Required evidence

- Selected-profile composition returns both sections at `0/0` with stable
  provider and installed-owner identity.
- Successful Person registration updates registry cardinality/revision;
  duplicate, invalid, future-dated, and guard-rejected registrations leave the
  witness unchanged.
- Successful materialization binding updates binding cardinality/revision;
  invalid Person/NPC, duplicate binding, already-materialized, and
  guard-rejected attempts leave both sections unchanged.
- Successful registration and binding compensation restore their respective
  cardinalities; revisions advance below saturation and remain at Max only
  when exact rollback is needed after a write advanced the revision to Max.
- At Max, valid ordinary registration/binding fails before mutation with
  `RevisionOverflow`; rollback of the exact just-installed Person or binding
  remains possible, leaves the final cardinality restored, and does not wrap.
- Tests confirm existing failure enum numeric values remain stable and the new
  overflow failure maps through `PersonMaterializationFailure`.
- Focused Person/materialization suites, ALL EditMode, complete official
  Smoke filter, and `git diff --check` pass before implementation promotion.

## Explicit exclusions

No per-Person fact revision, death/residence/genealogy/population policy,
runtime operation scope, shared mutation notification, P12-B profile admission,
P12-A export or staged hydration, restore, or continuation parity is included.
The two sections remain partial D-owner evidence and do not close P12-D.
P12-B remains blocked until the complete live profile inventory,
committed-write invalidation, owner-thread/quiescence and exact-zero coverage
are proven. P12-A remains `WAIT_DEPENDENCY` and separately gated.
