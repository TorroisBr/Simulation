# P12-D Genealogy passive census-witness candidate

**Status:** Exact-tip independent implementation review passed. This bounded
owner-evidence slice is not canonical delivery, P12-D completion, P12-B
readiness, or P12-A readiness.

**Base:** P12 canonical `d0c2733994aaf51e417b7c9f49f2b3489c4c49c3`.

**Candidate:** `codex/phase12/P12DGenealogyCensusWitness` at
`3afbc593fad8648e5a2ae20b7ec1a9d988769fb9`.

The bounded proposal in
`PHASE12_P12D_GENEALOGY_CENSUS_DESIGN.md` was independently revalidated
against the promoted canonical base `d0c2733`. The revalidation passed without
requiring a proposal amendment: P12-E only added composition properties, and
the existing runtime clone/accessor remained unchanged.

## Delivered evidence

- `GenealogyCensusProvider` publishes schema-v1 section
  `p12d.genealogy.parentage` from the installed runtime `GenealogyStore`.
- The witness identity is the exact installed store object, cardinality is
  `GenealogyStore.Count` (direct parentage edges), and the local revision
  increments once after every successful add or removal.
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
| `GenealogyCensusTests` | 3/3 | `Temp/ValidationResults/EditMode-20260930-030901-9949f66e829b450c888879af5e895045.xml` |
| ALL EditMode | 1988/1988 | `Temp/ValidationResults/EditMode-20260930-030918-8b7c5a08578d4d0190f7ec46bbab8402.xml` |
| Official complete Smoke filter | 5/5 | `Temp/ValidationResults/EditMode-20260930-030953-dcae70a5f9fa405894faeaba7ffa0b6e.xml` |
| `git diff --check` | passed | candidate diff from `d0c2733` |

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
