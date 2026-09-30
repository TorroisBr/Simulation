# P12-D Settlement Population Census Candidate

**Status:** Owner-local implementation and validation passed; complete design
implementation is blocked on bootstrap-composition integration after the
Genealogy composition change is promoted and revalidated.

**Branch:** `codex/phase12/P12DSettlementPopulationCensusWitness`.

**Code tip:** `0428d596259344c788c3738b184e2681861cea07`.

**Base:** `codex/phase12/canonical` at
`d0c2733994aaf51e417b7c9f49f2b3489c4c49c3`.

**Reviewed design:**
`codex/phase12/P12DSettlementPopulationCensusDesign` at `811c639`.

## Delivered owner-local surface

The code adds two single-section schema-v1 providers per City, ordered by
ordinal `RuntimeId` and keyed with the length-prefixed City ID. Both providers
for a City identify the exact installed `SettlementPopulationRuntime` owner.
The aggregate section cardinality is `1`; its live value remains
`CurrentPopulation`. The receipt section reports retained receipt count and a
receipt-ledger-local monotone revision read together under the existing receipt
lock.

The receipt revision advances once for each newly installed receipt and, while
below `long.MaxValue`, once for a `RestoreSnapshot` call that prunes one or more
receipts. Replay,
conflict, stale/rejected operations, unreceipted transitions, and restores
without pruning do not advance it. At revision saturation, new receipt-backed
installs fail with the existing `RevisionOverflow` result; pruning at
saturation may still reduce receipt cardinality without wrapping, and further
installs remain closed.

## Independent review result

Independent exact-tip review of code `0428d59` returned **BLOCK as a complete
implementation** because the diff does not publish the provider collection
from `SimulationBootstrapComposition`. The selected-profile test calls the
provider factory directly, so it does not prove the required normal bootstrap
handoff. The reviewer found no other concrete issue in owner identity,
cardinality, revisions, receipt semantics, ordering, tests, or scope.

The remaining integration must add the fixed read-only provider collection to
`SimulationBootstrapComposition` and assert access through
`simulation.Bootstrap`. The Genealogy census candidate also edits that
composition hotspot. Revalidate this branch against the promoted/revalidated
Genealogy composition change before editing or integrating that file.

## Validation

| Gate | Result | XML |
|---|---:|---|
| `SettlementPopulationCensusTests` | 6/6 | `EditMode-20260930-130010-7bbe5f6575a44eddb84623e43cfd787d.xml` |
| `AggregateDemographyFoundationTests` | 25/25 | `EditMode-20260930-130111-da7ab801bbf745049eab72ec7e2572ac.xml` |
| `NpcResidenceMigrationTests` | 33/33 | `EditMode-20260930-130128-3482e6e5633f43da9b2dba5ac54afdf4.xml` |
| `PopulationCanonicalIntegrationTests` | 9/9 | `EditMode-20260930-130146-5734df3738dc46eca8771e318477755b.xml` |
| ALL EditMode | 1991/1991 | `EditMode-20260930-130218-3a553307c55145eb8aededd55892ceab.xml` |
| Official complete Smoke filter | 5/5 | `EditMode-20260930-130412-45a5193ecc5148359628eda759ed5dc1.xml` |
| `git diff --check` | PASS | clean |

The selected authored profile test observed two distinct population owners,
initial populations 1,000 and 800, aggregate cardinality 1/revision 0 per
owner, and receipt cardinality 0/revision 0. Tests also cover runtime-ID order
and identity, net-zero changes, receipt install/replay/conflict/stale behavior,
rollback pruning and same-cardinality replacement, and successful/rejected
paired migration.

This candidate does not register its providers with the P12-B coordinator,
connect writes to a shared mutation epoch, prove owner-thread or quiescence,
issue capture eligibility, or provide population export/hydration. P12-B
remains incomplete, P12-A remains `WAIT_DEPENDENCY`, and P12-D remains blocked
on P12-B and P12-C.
