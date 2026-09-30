# P12-D Per-City Population and Receipt Census Design

> **Revalidation:** This design originated at `codex/phase12/P12DSettlementPopulationCensusDesign`
> commit `811c639ff2f3697eba674e87c44ff4a0463369f3`, against canonical
> `d0c2733`. Independent review against current canonical `676196b` passed
> with composition revalidation required. The current integration at code tip
> `44fc3ab` adds the fixed bootstrap publication and preserves the promoted
> Genealogy provider; see
> [`PHASE12_P12D_SETTLEMENT_POPULATION_CENSUS_REVALIDATION.md`](PHASE12_P12D_SETTLEMENT_POPULATION_CENSUS_REVALIDATION.md).

**Original status at `811c639`:** Bounded technical proposal; requires
independent exact-tip design review before implementation.

**Base:** `codex/phase12/canonical` at
`d0c2733994aaf51e417b7c9f49f2b3489c4c49c3`.

**Authority:** The accepted P12 capability decomposition assigns population
aggregates and their runtime relationships to P12-D. The accepted P12-B owner
census work permits bounded passive owner evidence without making the live
inventory complete or capture-eligible. This adds no checkpoint ID and no new
product/profile scope.

## Purpose and owner boundary

Publish passive evidence for every `SettlementPopulationRuntime` already
installed under the selected runtime's `CityRuntime` children. The selected
`UnityBootstrap-Daily-v1` profile currently composes two such owners. Use each
exact `CityRuntime.Population` object; do not create a replacement owner, use
the City object as owner identity, or expose mutable registration.

For each runtime City, require the City `RuntimeId` and the population owner's
`SettlementRuntimeId` to match. Sort the fixed bootstrap provider collection
by City `RuntimeId` for deterministic publication. Encode each runtime ID as
`<string-length>:<RuntimeId>` in the dynamic section suffix so section IDs are
stable and unambiguous.

## Section contract

Publish two schema-v1 sections per installed City population owner:

| Section identity | Exact owner | Cardinality | Revision |
|---|---|---|---|
| `p12d.city-population.aggregate/<encoded RuntimeId>` | that City's installed `SettlementPopulationRuntime` | `1` while the aggregate owner exists | `SettlementPopulationRuntime.Revision` |
| `p12d.city-population.operation-receipts/<encoded RuntimeId>` | the same installed `SettlementPopulationRuntime` | retained `operationReceipts.Count` | receipt-ledger-local revision |

Aggregate cardinality records the presence of the one aggregate value. The
value itself remains `CurrentPopulation`, outside `OwnerSectionCensusWitness`;
the later exact owner export must preserve it. A population of zero is still
one composed aggregate owner. An absent City/owner is missing composition, not
an exact-zero witness. The receipt section counts retained idempotency entries,
not named Persons or NPCs.

Add a narrow internal accessor on `SettlementPopulationRuntime` that reads the
receipt count and receipt-local revision together under the existing
`operationReceiptGate`. Add a receipt-local `long` revision initialized to
zero. Increment it once after each newly installed receipt. Exact replay,
fingerprint conflict, stale/invalid operations, and operations without a
receipt do not change it. `RestoreSnapshot` increments it once when that call
actually removes one or more retained receipts; a restore that removes none
does not increment it. This revision does not rewind with the population
snapshot, so a same-cardinality receipt replacement remains distinguishable.

Before receipt installation, preflight receipt-revision exhaustion along with
the existing population revision and semantic checks; reject through the
existing `RevisionOverflow` failure before the first write. Before
`RestoreSnapshot` removes receipts, increment the receipt revision once if its
revision is below `long.MaxValue`. At the saturated maximum, receipt-backed
installs are rejected and restore can only remove entries; each such removal
changes cardinality, so no same-cardinality replacement can occur while the
revision remains saturated. This preserves the existing void rollback API and
prevents overflow without changing rollback success semantics. Do not add a
test-only revision setter; review this unreachable saturation path by code
inspection.

The two sections use different revisions because the aggregate `Revision`
rewinds during `RestoreSnapshot`, while the receipt set may be pruned and later
repopulated to the same count and aggregate revision. The receipt revision
tracks only receipt-ledger content changes and is not serialized state.

## Bootstrap publication

Add one fixed, read-only provider collection to `SimulationBootstrapComposition`,
constructed from the existing runtime's City list and each exact
`city.Population` owner. Build two `IOwnerSectionCensusProvider` adapters per
City, one for each section in the table, both bound to the same exact population
owner; the published collection contains four providers for the current
two-City profile. Keep owner references private to each adapter. This provider
collection is inventory evidence only and does not register sections with the
incomplete P12-B coordinator.

At the selected profile's initial day-zero bootstrap, assert two distinct
population owner objects, aggregate owner cardinality `1` and revision `0` for
each, receipt cardinality `0` and receipt revision `0` for each, and the
profile's current aggregate values: CampoVerde `1,000` and SerraDeFerro `800`.
These values are initial profile facts only; later witnesses must observe live
state rather than assume the initial values or empty receipt ledgers.

## Required evidence

- Provider reads preserve exact installed owner identity and stable section
  identity. Repeated reads without a write return the same witness tuple.
- A successful unreceipted population transition increments aggregate
  revision once while its aggregate section cardinality stays `1`.
- A successful receipt-backed operation increments aggregate revision once,
  receipt cardinality once, and receipt revision once. Exact replay is
  successful without a second install and changes neither witness. A
  same-key conflicting receipt and stale/invalid transition fail without
  changing either witness.
- A successful net-zero births/deaths transition preserves `CurrentPopulation`
  while advancing aggregate revision and installing its new receipt. This
  proves that owner revision, rather than scalar equality, records committed
  state change.
- Rollback pruning removes only receipts selected by the existing
  `RestoreSnapshot` rule; receipt cardinality and receipt-local revision reflect
  that removal. Repeated restore with no additional removed receipts leaves
  the receipt witness unchanged. A later different receipt key can return the
  ledger to its former cardinality and aggregate revision while the
  receipt-local revision still distinguishes the replacement. Observe only
  after the outer rollback returns.
- Successful paired cross-settlement migration changes origin/destination
  aggregate values by `-1/+1`, advances each aggregate revision once, and
  leaves both receipt sections unchanged. Rejected stale/repeated migration
  leaves all four witness tuples unchanged.
- The live selected profile has exactly two unique runtime IDs. The day-zero
  population values are not assumptions about later day boundaries.

## Timing and limits

Population edges have no per-row day identity, and these sections report
current aggregate/receipt-owner state rather than daily occurrences. Do not
assume a fixed number of population writes per day. Existing population
revision may rewind during transactional rollback; the receipt-local revision
does not. These providers do not provide an atomic cross-owner snapshot or
prove owner-thread/quiescence. Read them only after the outer domain operation
returns and rely on the later P12-B runtime fence before any capture decision.

The adapters add no population behavior, Person/NPC membership rules, exports,
staged hydration, shared mutation-epoch wiring, owner-thread enforcement,
quiescence proof, or capture eligibility. P12-B remains incomplete and P12-A
remains `WAIT_DEPENDENCY`. P12-D remains dependency-gated on P12-B and P12-C.

## Integration ownership

The reviewed Genealogy census candidate also appends a provider assignment and
property to `SimulationBootstrapComposition.cs`. Keep implementations
serialized across this composition hotspot: revalidate this proposal after
Genealogy promotion, then add the population providers without replacing or
rewriting the Genealogy provider. The population-only owner/store changes stay
in `SettlementPopulationRuntime.cs`, the new per-City census adapter, and a
separate `Population` census test file.
