# P12-B RuntimeIdentityRegistry census witness design

**Status:** Design review passed; implementation is authorized within the accepted P12-B capability scope.

**Design review:** Exact-tip review passed at `abf433f6767be39d0ddac5cf2b4c194ca0fa10a4`, against canonical base `1ada62b031e738e2bdd5d3d623e028a114961d6e`. Durable review record: `codex/phase12/P12BIdentityRegistryWitnessDesignReview` at `7f911ab`.

**Canonical base:** `codex/phase12/canonical` at
`1ada62b031e738e2bdd5d3d623e028a114961d6e` (including promoted P8-A
populated-geography witnesses).

**Authority:** accepted P12-B–P12-G capability decomposition;
`docs/design/PHASE12_B_BLOCKER_RESOLUTION.md`; selected-profile evidence in
`docs/design/PHASE12_OWNER_COVERAGE_INVENTORY.md`; the current P12 Brief and
Execution Model; and the present extension constraints. This is a bounded
P12-B owner-witness work package, not a new checkpoint ID or P12-C export /
hydration implementation.

## Gap and bounded outcome

The selected `UnityBootstrap-Daily-v1` profile has a live
`RuntimeIdentityRegistry`, but its eight typed dictionaries are private and
the selected-profile identity total is currently inferred from construction
inputs. The registry has public successful registration paths for NPC, City,
Location, Route, ExplorableSite, LocalPlace, LocalConnection, and NotableItem
runtime identities, plus an internal validated batch path for local topology
members. There is no unregister path.

Add owner-issued, passive live cardinality and revision evidence for those
eight indexes. Each index is a separate required owner section. Do not collapse
the index counts into one witness: a sum could hide a missing identity in one
type. Every witness uses the exact `RuntimeIdentityRegistry` object as owner
identity and the registry's monotone `Revision` as its conservative change
stamp.

## Sections

Schema version is 1 for all sections. Section IDs and cardinalities are:

| Section ID | Count source |
|---|---|
| `p12c.runtime-identities.npcs` | `npcsByRuntimeId.Count` |
| `p12c.runtime-identities.cities` | `citiesByRuntimeId.Count` |
| `p12c.runtime-identities.locations` | `locationsByRuntimeId.Count` |
| `p12c.runtime-identities.routes` | `routesByRuntimeId.Count` |
| `p12c.runtime-identities.explorable-sites` | `explorableSitesByRuntimeId.Count` |
| `p12c.runtime-identities.local-places` | `localPlacesByRuntimeId.Count` |
| `p12c.runtime-identities.local-connections` | `localConnectionsByRuntimeId.Count` |
| `p12c.runtime-identities.notable-items` | `notableItemsByRuntimeId.Count` |

The implementation may use a narrowly scoped immutable counts snapshot for the
private dictionary reads, but each `IOwnerSectionCensusProvider` must return
one section ID, its own exact count, the same registry identity, schema 1, and
the revision read from that owner. Preserve the registry's ordinal keys and
existing cross-type RuntimeId uniqueness rules. Do not probe IDs or derive the
counts from authored configuration.

The selected profile currently composes 10 NPC, 2 City, 2 legacy Location,
and 2 Route runtime identities. It composes no ExplorableSite, LocalPlace,
LocalConnection, or NotableItem runtime identity. Its eight live section
cardinalities must therefore be `10/2/2/2/0/0/0/0`, total 16. If every
successful registration increments the registry revision once and the
bootstrap uses the currently observed individual registration paths, the
selected-profile revision is 16. The implementation test must confirm this
against the live registry; any batch registration in the profile would require
recording the observed revision and explaining the corresponding successful
owner operations rather than assuming 16.

Registry LocalPlace/LocalConnection counts describe only typed identities in
this index. A zero in either section does not claim that a P10
`LocalTopologyStore` exists, is absent, or has that cardinality; composition
presence remains a separate profile fact.

## Revision semantics

- A new registry starts at revision 0. Each successful single-item
  `Register*` call advances it exactly once, after the corresponding dictionary
  insertion succeeds.
- Invalid input, a duplicate same-type ID, a cross-type ID collision, or any
  failed `Register*` leaves all eight counts and the revision unchanged.
- The atomic local-topology member batch advances revision once if it commits
  one or more members across the LocalPlace/LocalConnection indexes. A valid
  empty batch that changes no index does not advance it. Its existing
  prevalidation must leave counts and revision unchanged on every rejected
  batch.
- Revision never wraps or decreases. Exhaustion must fail before an owner
  mutation instead of publishing a stale or negative witness.
- Registry revision covers identity membership only. It does not change when
  an already registered runtime entity's domain state changes; those states
  belong to their owning D/E/F sections.

This is a local owner stamp, not the P12-B shared mutation epoch, a lock, or a
coherent cross-owner snapshot. Providers remain passive and unsynchronized.

## Selected-profile access seam

The live registry is retained by the normal bootstrap but is not currently a
property of `SimulationBootstrapComposition`. Add a read-only ordered
`RuntimeIdentityCensusProviders` collection to that existing handoff,
constructed from the same `RuntimeIdentityRegistry` used by `TesteSimulacao`
and the normal systems. Do not expose a new raw mutable registry reference.
This collection is a required P12 owner-evidence seam, not a general-purpose
provider registry, mod hook, or P19 API. Do not use config counts or a
pre-publication substitute as the selected-profile evidence.

## Required tests

1. In the selected-profile bootstrap test, obtain the eight witnesses from the
   published composition and assert their stable section IDs/schema, one
   shared installed-registry identity, distinct counts
   `10/2/2/2/0/0/0/0`, total 16, and the live revision. Repeated reads retain
   that registry identity and do not change counts or revision.
2. On a fresh registry, assert all counts and revision are zero. Cover each of
   the eight successful typed registration paths and confirm only its own
   cardinality increases and the owner revision advances once.
3. Cover null/blank input where supported, same-type duplicates, cross-type
   duplicate IDs, and a rejected local-topology batch. All must preserve every
   count and the revision.
4. Cover a successful multi-member local-topology identity batch. The LocalPlace
   and LocalConnection counts must reflect the committed rows and revision
   advances once for the batch. A no-op empty batch leaves the stamp unchanged.
5. Verify temporal revalidation: after a successful NPC identity insertion
   on a fresh registry, the NPC witness changes 0 to 1 and all eight witnesses
   report the new shared registry revision; unchanged index cardinalities
   remain unchanged.

Existing domain semantics remain authoritative for each registration result.
These tests establish owner cardinality/revision observation only.

## Exclusions and readiness

Do not add ID allocation/high-water witnesses, record-sequence or RNG state,
profile census registration, shared epoch wiring, capture eligibility,
thread/quiescence enforcement, serialization, staged hydration, new entity
types, gameplay, or P19 loader/extension infrastructure. Preserve
`codex/phase12/P12CIdentityRuntimeSnapshot` at `531d835` for later selective
revalidation; do not transplant its stale `DecisionRecords.cs` or
`RuntimeIdentity.cs` changes wholesale.

This fills one live P12-B census evidence gap only. It does not complete
P12-B, make P12-A ready, or authorize P12-A implementation. P12-A still waits
for complete included-owner export/staged hydration, a validated complete live
profile inventory, and its separate implementation authorization.
