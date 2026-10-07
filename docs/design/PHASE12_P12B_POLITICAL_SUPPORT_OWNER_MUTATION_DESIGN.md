# P12-B PoliticalSupport owner admission and mutation epoch design

**Status:** bounded technical design submitted for independent review; implementation has not started.

**Canonical base:** `codex/phase12/canonical` at
`2760fe199909708f22ea61d3eb2dd929b542521b`.

## Contract reconciliation

The accepted P12-E checkpoint explicitly includes populated faction/support
state among the core owners requiring exact export and staged hydration. It also
states that an owner being empty at bootstrap does not exclude later supported
populated state. The P12-B blocker matrix therefore identifies
`PoliticalSupportStore` as a separate owner task, subject to confirmation from
the actual selected profile and supported write surface.

Architecture §36 keeps PoliticalSupport as its own targeted relation store and
lifecycle. This design adds no relation meaning, producer, gameplay, or
cross-owner coupling. It only adds a required census witness and P12 invalidation
for the existing owner and runtime commits.

The current `TesteSimulacao` Daily-v1 composition constructs
`SimulationRuntime` without a source `PoliticalSupportStore`. Runtime creates
a private owner through `ClonePoliticalSupportStore`, bound to the runtime's
installed Person, Faction, and PoliticalClaim owners; the null-source path
starts with zero relation rows and revision zero. The census provider must bind
that exact installed clone. The initial zero cardinality describes the current
Daily-v1 bootstrap boundary; it does not exclude support relations created by
later supported commits, nor replace P12-E's future populated-state export and
staged-hydration obligation.

## Current owner and write boundary

`PoliticalSupportStore` owns one relation-row collection keyed by stable
`PoliticalSupportRelationId`, a local `Count` and `Revision`, and a derived
`activeByPair` index. Registration inserts a relation row and advances the
revision once. Applying an add inserts one active row and advances the revision
once. Applying an end replaces an existing row with its ended form, leaves
`Count` unchanged, removes its derived active-pair entry, and advances the same
revision once. Queries and proposal methods are read-only. The clone rebuilds
the active-pair index from relation records and preserves local revision.

The production source call graph has no internal writer callers beyond the
existing `SimulationRuntime` facade definitions and the constructor clone/setup
path. The runtime keeps the installed owner private and exposes these current
commit facades:

- `TryRegisterPoliticalSupport`, documented for registering existing history
  during load/composition;
- `TryApplyPoliticalSupportAdd`, after its read-only proposal method;
- `TryApplyPoliticalSupportEnd`, after its read-only proposal method.

The registration facade retains its existing current-day checks. Add/end
continue to revalidate the transition's owner, local revision, world day, and
relation state in the store. No direct installed-store write path or new
producer is introduced.

## Bounded contract

Register one schema-v1 `Required` section in the selected Daily-v1 census:

| Section | Owner identity | Cardinality | Revision |
|---|---|---|---|
| `p12e.political-support.relations` | Exact installed `PoliticalSupportStore` clone | `PoliticalSupportStore.Count` (all relation rows, active and ended) | `PoliticalSupportStore.Revision` |

One section is sufficient because active and ended relations occupy the same
authoritative row collection and share one local revision. `activeByPair` is
derived and is not a separate owner section. The current Daily-v1 initial
inventory is zero relations at revision zero. Later successful supported
writes refresh the section through the P12 mutation protocol. This count/revision
witness is not an export, relation payload, or hydration implementation.

Register one bounded operation ID, `p12.political-support.owner-commit`, for
exactly the three existing runtime commit facades above:

| Facade | Existing owner commit | Existing semantics retained |
|---|---|---|
| `TryRegisterPoliticalSupport` | Register an existing relation row | Null, current-day, identity, endpoint, disposition, duplicate, active-pair, mutation-guard, and revision-overflow checks |
| `TryApplyPoliticalSupportAdd` | Add the proposed active row | Exact store identity, expected store revision, expected/current day, duplicate relation/pair, endpoint, mutation-guard, and revision-overflow checks |
| `TryApplyPoliticalSupportEnd` | Replace an active row with its ended form | Exact store identity, expected store revision, expected/current day, relation identity/state, end-day, mutation-guard, and revision-overflow checks |

Proposal and query methods remain outside the operation because they do not
mutate authoritative state. Keep the pre-runtime clone/setup path outside the
post-publication runtime operation. Add no policy, producer, API, or lifecycle
semantics.

Before a listed facade attempts a possible store commit in selected Daily-v1,
the runtime must require the current admission owner thread, a healthy protocol,
an unchanged baseline for the Required relation section, available shared
mutation-epoch capacity, and entry into the registered operation. Preserve
existing domain prechecks and failure codes. On a successful store commit,
preserve the existing `AdvancePoliticalWorldRevision` behavior and notify the
single relation section once through the existing P12 mutation protocol. This
advances one shared mutation epoch for the commit. An end commit must refresh the
revision witness even though row cardinality is unchanged. A rejected domain
commit notifies no section and advances no P12 epoch. Admission/preflight
rejection faults the runtime admission boundary and returns the existing
`RuntimeFaulted` facade failure before the store write. If post-commit
notification fails, fault admission without rolling back the already committed
domain result, consistent with existing P12 owner adapters.

When no selected P12 admission context exists, the existing facade behavior
remains unchanged. The design adds no lock and makes no claim for manual
reflection, mutation of a separate pre-runtime source object, or other
unsupported write paths.

## Reconstruction and scope boundary

P12-E remains responsible for the relation payload needed to reconstruct
support truth: stable relation ID; typed source and target identities;
disposition; start and optional end logical day; and exact owner revision as
required by its serialization contract. Rebuild only the existing derived
active-pair index from hydrated relation rows. This design does not implement
that export/hydration path or alter the diagnostic WorldStateSnapshot.

The slice covers only this exact owner and its three existing runtime commit
facades. It does not establish complete P12-B owner/writer coverage, complete
shared-epoch coverage, runtime-wide quiescence, capture eligibility, export,
hydration, P12-A readiness, P13 readiness, or Phase 12 closure. The selected
Daily-v1 profile remains P9-B-only; the separate P10-A Ruin/LocalTopology
proving profile is unchanged.

## Validation and integration boundary

Focused coverage should verify the provider's schema, section identity, exact
installed-owner identity, all-row cardinality, local revision, and stable
repeated reads. The live selected Daily-v1 inventory should grow from 257 to
258 Required sections and bind the section to the actual runtime clone at the
current 0-count/0-revision initial boundary. Bootstrap composition tests must
retain Daily-v1/P10-A separation.

A focused mutation suite should prove:

- successful registration and add each increase relation cardinality and local
  revision and notify the section once;
- successful end preserves cardinality but increments local revision and
  refreshes the witness;
- each successful existing facade preserves its current
  `PoliticalWorldRevision` behavior and advances one shared P12 epoch;
- duplicate, stale-day, wrong-store, stale-revision, invalid-state, and local
  revision-overflow failures preserve owner count/revision, PoliticalWorldRevision,
  and mutation epoch;
- owner-thread, unchanged-baseline, and epoch-capacity preflight failures occur
  before an owner commit and fail closed;
- proposals remain read-only and do not notify the section.

Retain existing `PoliticalSupportFoundationTests`,
`PoliticalKnowledgeSupportWorldIntegrationTests`, and runtime-admission
regressions. Run the exact Daily-v1 bootstrap/admission inventory, affected
focused suites, ALL EditMode, official Smoke, and `git diff --check` on the
final code tree. Record inspectable validation artifacts and hashes for review.

Implementation ownership is `SimulationRuntime.cs`, one new support census
provider, and focused census/protocol/composition tests. The runtime is an
explicit serialization hotspot; no concurrent writer should edit or integrate
that adapter until this slice is reviewed and its integration order is agreed.
No broad registry, generic mutation framework, or changes to the separate
P10/P14/P18/P19/P20 tracks are part of this design.

## Review and readiness

A passing independent design review makes this bounded P12-B prerequisite
slice ready for implementation under the already accepted P12 capability-work
authorization. It does not promote implementation or alter P12-A/P12-B/P13
status. Any changed canonical architecture or P12-E owner scope requires
revalidation before implementation.
