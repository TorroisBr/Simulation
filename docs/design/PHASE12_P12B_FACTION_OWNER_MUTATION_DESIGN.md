# P12-B Faction owner admission and mutation epoch design

**Status:** bounded technical design for independent review; implementation has not started.

**Canonical base:** `codex/phase12/canonical` at
`54e95a325812baa8db2fe233d3dd566be93f7fa2`.

## Current-source finding

The selected `UnityBootstrap-Daily-v1` composition constructs a private
`FactionStore` on every `SimulationRuntime`. `TesteSimulacao` supplies no source
FactionStore at the `p9.genesis.validate-profile/v1` stage, so this exact
composition starts with zero factions and zero affiliations. The runtime still
exposes normal domain operations that can populate that owner after publication:
`TryRegisterFaction`, `TryApplyFactionAffiliation`, and
`TryApplyFactionAffiliationEnd`. Each successful commit advances the existing
`PoliticalWorldRevision`; none registers or notifies a P12 owner section.
`InitializeNpcRosterCensusProtocol` currently seals 253 sections and does not
register a FactionStore provider. The selected Daily-v1 FR-B read surface also
binds the exact runtime FactionStore, so this is an included factual authority,
not the separate P10/P14/P18/P19/P20 owner set.

`FactionStore` already exposes exact local `Count`, `AffiliationCount`, and
`Revision` values. Registering a faction, adding an affiliation, and ending an
affiliation each advance the same store revision once after their domain checks.
The two row collections have separate cardinalities but share that revision.
The store's public runtime records are detached, sorted read-only snapshots;
the runtime does not expose the installed FactionStore object itself.

## Bounded contract

Register two schema-v1 `Required` sections in the selected P12 census:

- `p12e.faction.records`: exact installed FactionStore identity, cardinality
  `FactionStore.Count`, revision `FactionStore.Revision`.
- `p12e.faction.affiliations`: the same exact owner identity, cardinality
  `FactionStore.AffiliationCount`, revision `FactionStore.Revision`.

Both sections are required because the runtime always composes this core
authority. Their current Daily-v1 admission cardinalities are exactly zero,
because normal bootstrap passes no source FactionStore. A future supported
commit changes the captured cardinality/revision through the normal protocol;
it does not change the profile contract or add faction gameplay.

Register one bounded operation ID, `p12.faction.owner-commit`, for exactly the
existing successful runtime facade commits listed below:

| Facade operation | Store commit | Existing domain semantics retained |
|---|---|---|
| `TryRegisterFaction` | add one stable `FactionId` record | Existing ID, day, duplicate, revision-overflow, and mutation-guard failures |
| `TryApplyFactionAffiliation` | add one affiliation history row | Existing proposed-transition identity/store-revision and current-day checks |
| `TryApplyFactionAffiliationEnd` | end one active affiliation history row | Existing stale, day, membership, and revision-overflow checks |

Proposal/query methods remain read-only and do not open the operation. The
store's `TryRegisterAffiliation` remains a pre-runtime clone/setup path; the
installed store is private and has no post-publication direct runtime accessor.
No new API, membership policy, faction lifecycle, or UI path is added.

Before any listed facade attempts a commit in selected Daily-v1, require the
existing admission owner thread and healthy protocol, validate both exact
section baselines and shared mutation-epoch capacity, and enter the registered
operation. The current domain operation then runs unchanged. On success, keep
the existing `AdvancePoliticalWorldRevision` behavior and notify **both**
sections once through the existing mutation protocol. Both must be notified
because both witnesses carry one shared FactionStore revision, even when only
one collection's cardinality changes. The mutation protocol advances one
shared epoch for the commit. Failed domain operations notify neither section
and do not advance the epoch. Admission/preflight rejection uses the runtime's
existing fail-closed `RuntimeFaulted` result path and occurs before the store
commit. A post-commit notification fault faults admission and preserves the
already-committed faction result, consistent with other P12 owner adapters.

When no P12 admission context is selected, the existing facade behavior is
unchanged. The design adds no thread lock and claims no protection for manual
reflection, mutation of a separate pre-runtime source object, or unsupported
external mutation paths.

## Validation and integration boundary

Focused coverage should verify both providers' exact section/schema/owner,
shared revision, independent row cardinalities, and stable repeated reads; the
selected Daily-v1 inventory should grow from 253 to 255 sections and bind both
Required sections to the installed runtime clone at 0/0/revision 0. A focused
mutation suite should prove one epoch advance and refreshed baselines for each
successful facade commit, both-section revision notification, and no epoch or
cardinality change for each rejected commit. Existing non-P12 facade behavior
must remain unchanged. Run the affected Faction, FR-B factual-read, runtime
admission, and bootstrap composition regressions, then ALL EditMode, official
Smoke, and `git diff --check` on the exact code tree.

Implementation hotspots are `SimulationRuntime.cs`, one new Faction census
provider, and focused P12 tests. The Faction methods have no current nested
Faction operation in the production call graph; their operation scopes still
participate in the existing outer daily scope if invoked synchronously during
`AdvanceDay`. Integration must preserve one protocol epoch per successful
FactionStore commit. This small owner slice is independent of the P12-F
Expedition dependency and does not occupy the P18 temporal-owner boundary.

The slice closes only this exact owner and these three existing commits. It
does not establish complete P12-B owner or writer coverage, complete shared-
epoch coverage, global quiescence, capture eligibility, export, hydration,
P12-A readiness, P13 readiness, or Phase 12 closure. The 253-to-255 count is
partial protocol evidence, not a completeness claim.
