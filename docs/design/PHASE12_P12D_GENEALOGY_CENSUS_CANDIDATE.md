# P12-D Genealogy passive census-witness candidate

**Status:** Promoted as passive owner evidence to `codex/phase12/canonical` at
`bde930477b7614a7fb1baed01497dbd1fe063927`. It does not establish P12-D
completion, P12-B readiness, or P12-A readiness.

**Base:** P12 canonical `d0c2733994aaf51e417b7c9f49f2b3489c4c49c3`.

**Code-bearing commit:** `3afbc593fad8648e5a2ae20b7ec1a9d988769fb9`.

**Promoted branch tip:** `codex/phase12/P12DGenealogyCensusWitness` at
`bde930477b7614a7fb1baed01497dbd1fe063927`.

**Post-promotion revalidation:** A cross-owner birth/rollback audit found a
revision-saturation defect. The bounded correction is implemented on
`codex/phase12/P12DGenealogyRollbackSaturation` at
`f631de8a9209956cf61d0f901867cca244befcaf`, independently reviewed PASS
against `543196a`, and validated. It remains a separate candidate pending its
canonical-promotion gate; until promoted, this witness is not treated as
integrated with named-birth rollback.

The bounded proposal in
`PHASE12_P12D_GENEALOGY_CENSUS_DESIGN.md` was independently revalidated
against the promoted canonical base `d0c2733`. The revalidation passed without
requiring a proposal amendment: P12-E only added composition properties, and
the existing runtime clone/accessor remained unchanged.

## Delivered evidence

- `GenealogyCensusProvider` publishes schema-v1 section
  `p12d.genealogy.parentage` from the installed runtime `GenealogyStore`.
- The witness identity is the exact installed store object, cardinality is
  `GenealogyStore.Count` (direct parentage edges), and ordinary public add and
  remove commits increment the local revision once. The internal named-birth
  compensation path can remove a previously added edge at saturation without
  advancing the revision; cardinality strictly decreases while ordinary
  mutations are closed.
- The fixed provider is constructed by `SimulationBootstrapComposition` from
  `Runtime.GenealogyStoreForWorldBoundary`. The public bootstrap exposes the
  provider and does not expose the mutable store.
- The `RevisionOverflow` failure was appended after existing enum values.
  Both add and remove preflight it before the first edge or adjacency write.
  There is no test-only revision setter; direct overflow injection is therefore
  not included.
- Runtime clone construction still replays deterministic source records. The
  installed clone starts at its own replay-local revision, and construction
  leaves the source store and revision unchanged.
- `PersonGenealogySystem` retains its existing wrapper mapping: an unrecognized
  owner failure such as `RevisionOverflow` maps to `StoreFailure`. This change
  does not expand the runtime wrapper failure contract.

The tests cover the selected profile's stable exact-zero witness, successful
add/remove commits, rejected duplicate/self/cycle/missing/null/unregistered
endpoints with unchanged cardinality and revision, null `ParentageRecord`
rejection, same-cardinality edge replacement, and installed-clone identity and
replay-local revision. Genealogy edges have no temporal fields, so the witness
does not add a day, occurrence, or temporal-cardinality assumption.

## Independent review and validation

Independent implementation review passed at exact candidate tip
`3afbc593fad8648e5a2ae20b7ec1a9d988769fb9`, against base `d0c2733`. The review
confirmed owner and clone identity, exactly-once revision behavior, overflow
preflight ordering, enum compatibility, runtime wrapper semantics, the
additive P12-E composition integration, and the scope limits below.

Validation on the final candidate tree passed:

| Gate | Result | Evidence |
|---|---:|---|
| `GenealogyCensusTests` | 3/3 | `Library/ValidationResults/P12DGenealogyCensus/EditMode-20260930-031626-4f9e23f6d8ed45e2a131bda8d9fc8001.xml` |
| ALL EditMode | 1988/1988 | `Library/ValidationResults/P12DGenealogyCensus/EditMode-20260930-031645-89ead902cfeb470998b6247433aee7bc.xml` |
| Official complete Smoke filter | 5/5 | `Library/ValidationResults/P12DGenealogyCensus/EditMode-20260930-031719-3ecfb319f50e4e129f6be315f4ecdde8.xml` |
| `git diff --check` | passed | candidate diff from `d0c2733` |

The three result XMLs are retained in the candidate worktree under
`Library/ValidationResults/P12DGenealogyCensus`; the validation rerun did not
change source code after the exact-tip implementation review.

## Saturated named-birth rollback correction candidate

The follow-up defect was corrected without changing public add/remove
semantics. Ordinary `TryAddParentage` and `TryRemoveParentage` continue to
reject at `long.MaxValue`; an internal exact-edge rollback path is called only
by named-birth compensation for an edge recorded as added by that same attempt.
At saturation it removes the edge and adjacency links without wrapping the
revision. The regression proves that a two-parent birth starting at
`long.MaxValue - 1` fails with no child registration, genealogy edge, or
population change, leaving `(Count, Revision) = (0, long.MaxValue)`.

Implementation commit `f631de8a9209956cf61d0f901867cca244befcaf` has tree
`ae029655824dd3ec6a73bc3c17a9bb2708014401`. Independent exact-tip review
passed against parent `8f04a62` and confirmed the rollback path has no other
callers. Validation on that tree passed: named-birth lifecycle 15/15,
Genealogy census 4/4, ALL EditMode 1990/1990, complete official Smoke 5/5,
and `git diff --check`. This is a correction candidate only and does not
establish P12-B readiness or change P12-A/P12-D blockers.

## Limits retained

The witness is passive and unsynchronized. It is not registered into the
incomplete P12-B profile inventory, does not connect writes to the shared
mutation epoch, does not prove owner-thread/quiescence, and does not grant
capture eligibility. It adds no genealogy semantics, export, hydration,
restore, population behavior, or Person membership policy.

P12-B remains incomplete. P12-A remains `WAIT_DEPENDENCY`, with its separate
implementation authorization still outstanding. P12-D remains dependency-
gated on P12-B and P12-C; this candidate does not satisfy those prerequisites
or close Phase 12.
