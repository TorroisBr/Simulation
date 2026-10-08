# P12-D Person owner export and staged hydration design

**Status:** Bounded owner design for independent review. It defines the P12-D
PersonStore/PersonRuntime implementation seam only; it does not deliver code,
change Phase State, integrate P12-A, or close P12-D.

**Design base:** `codex/phase12/canonical` at
`ad4b20c42a25c9fd453c98695a79ce59490bf4fe`; architecture baseline
`codex/architecture/world-identity-projection` at
`47eff220c7ce00f6e7c759bdc2b76780bb46f628`.
The architecture baseline includes the intraday/extensibility and
multi-participant activity alignments recorded by the current P12-D design;
this Person owner adds no temporal operation or activity semantics.

**Accepted boundary:** `docs/design/PHASE12_D_TECHNICAL_DESIGN.md` at the
design base. The selected profile is `UnityBootstrap-Daily-v1` from
`Simulation-DailyV1.asset`; `Simulation-GeneralTest.asset` and its P10-A
Ruin/LocalTopology facts remain a separate proving profile.

## 1. Purpose and implementation readiness

Provide a detached exact-value snapshot and private staged factory for the
existing `PersonStore` and its registered `PersonRuntime` owners. Preserve
Person-owned factual identity, dates, residence and optional materialization
relation, including Persons without an NPC representation and Persons that
have died. The implementation must remain a local owner capability that the
later D adapter can compose with P12-C roots, City, NPC and Genealogy owners.

**Implementation disposition: READY after independent exact-content design
review.** The current P12 State records P12-B and P12-C as promoted within
their accepted scopes. For this owner, P12-B already provides the two fixed
PersonStore witnesses (`p12d.person.membership` and
`p12d.person.materialization-binding`) and the dynamic singleton
`p12b.person-life-residence/<encoded PersonId>` witness for every registered
Person. Their cardinalities and revisions are sourced from the exact installed
owners. The completed Daily-v1 token binds those owner sections, their schema,
cardinality and revision, the mutation epoch, WorldId, absolute day and
successful completed-core sequence. Current writes to Person life/residence
are admitted/notified through the P12 population lifecycle boundary; registry
and materialization writes use the P12 NPC-membership boundary. No missing
P12-B census row or Person-local revision source was found for this bounded
owner slice.

This readiness permits only an isolated owner implementation after the design
review passes. It is not a claim that the full owner/operation matrix is
exhaustive for future ingress, nor that the D graph or P12-A is ready. The
whole-D adapter must still obtain and revalidate the exact completed-boundary
token under P12-B's serialized runtime ownership window. If that token or any
required section is absent, stale, has unknown schema, or differs from the
snapshot values, the adapter rejects capture. It must not substitute the
initial empty-profile result.

## 2. Exact ownership boundary

| Fact | Authority and representation |
|---|---|
| Stable individual identity | `PersonRuntime.PersonId`; one row per exact `PersonStore.Persons` member. The identity string uses the existing ordinal equality and is preserved byte-for-byte; do not trim, case-fold, or derive it from an NPC or display name. |
| Birth | Nullable `PersonRuntime.BirthAbsoluteDay`, immutable after construction. It is the existing absolute simulation-day value, not age or a calendar-relative duration. |
| Death | Nullable `PersonRuntime.DeathAbsoluteDay`, exact Person-owned fact. A dead Person remains registered and exportable. |
| Residence | Nullable `PersonRuntime.ResidenceSettlementRuntimeId`, the Person-level settlement identity. Preserve the existing exact runtime ID string. Do not convert it to a P8 `LocationId`, infer it from current City, or duplicate it as an NPC-owned Person residence. |
| Materialization relation | Nullable `PersonRuntime.MaterializedNpcRuntimeId`, the PersonStore-owned Person→NPC link. The reverse `personsByNpcRuntimeId` map is derived and rebuilt from rows; it is not exported as a second relation. |
| Store list order | Preserve `PersonStore.Persons` order. It is the current store's authoritative insertion order; do not sort unless a later source audit proves every included consumer is order-insensitive and a separately reviewed D contract permits the change. |
| Owner-local revisions | Preserve `PersonStore.Revision` and each row's exact `PersonRuntime.LifeResidenceRevision` separately. Do not synthesize an aggregate Person revision. |

`PersonId` equality is ordinal and the existing `PersonId` type is immutable.
Snapshots should store the exact identity value and reconstruct the typed ID;
they must not retain a mutable `PersonRuntime`, a live `PersonStore` view, an
NPC/City object, a Unity object, or a source collection. The result contains an
explicit schema ID/version even for the empty owner. Empty means the required
Person owner exists with zero rows, distinct from an absent or unknown section.

The accepted Daily-v1 bootstrap begins with an empty PersonStore. Its initial
fixed membership and binding cardinalities are both zero, with store revision
zero and no dynamic Person life/residence sections. Thereafter Person count
and binding cardinality are runtime values, not authored maxima or constants.
For every state, binding cardinality equals the number of rows with a
nonempty materialized NPC ID. An unmaterialized Person is valid and contributes
one membership row and no binding row. A materialized Person contributes one
of each. No universal Person/NPC ratio beyond the current one-to-one
materialization invariant is introduced.

## 3. Temporal and revision contract

All date fields are copied exactly as nullable absolute-day values. Local
Person validation requires nonnegative present values and, when both dates are
present, `death >= birth`. The merged D graph compares every present birth and
death day with the captured/restored P12-C absolute day; future dates are
rejected. The current `TryRegisterPerson` path rejects future birth/death, and
death transitions cannot precede a known birth. Do not infer an age field,
advance a day during staging, rewrite dates using calendar configuration, or
drop historical/dead Persons.

Revision semantics are intentionally split:

* `PersonStore.Revision` advances for each successful registration,
  materialized-NPC bind and exact compensation removing that registration or
  binding. It may contain gaps from successful work later compensated. For
  the accepted profile, whose store begins empty, a locally impossible value
  is one below `PersonCount + BindingCount`; validate this lower bound using
  widened arithmetic. Preserve any greater value exactly. This lower bound
  does not require equality with counts.
* Each `PersonRuntime.LifeResidenceRevision` advances on successful death or
  residence commits. It is independent for each Person and may contain gaps.
  Birth is immutable owner data; initial residence can be set before the
  Person is bound into an admitted runtime, so a non-null residence does not
  imply a minimum lifecycle revision. Preserve the revision exactly; require
  only a nonnegative value.
* The materialized NPC link changes with `PersonStore.Revision`; it does not
  advance `LifeResidenceRevision`. Death plus the normal resident-death
  residence clear can advance the Person-local revision twice in one admitted
  operation. Preserve both successful increments and reject overflow rather
  than wrapping.

The P12-B vector binding is exact. At capture, the outer P12-D package must
compare the snapshot with the current token's witnesses:

1. Require fixed section IDs `p12d.person.membership` and
   `p12d.person.materialization-binding`, schema v1, the same installed
   `PersonStore` identity, row/binding cardinalities derived from the snapshot,
   and the same `PersonStore.Revision`.
2. Require exactly one schema-v1
   `p12b.person-life-residence/<length>:<PersonId>` witness for each row,
   bound to that exact `PersonRuntime`, cardinality 1, with the same
   `LifeResidenceRevision`. Require no such dynamic witness for an absent
   Person, and do not treat a missing expected witness as empty.
3. Bind the detached D owner package to the same ephemeral token identity and
   exact owner-section vector used by C/D/E/F projections. Capture the owner
   once inside the serialized window; after copying, validate that the same
   token remains current. A changed completed sequence, absolute day, mutation
   epoch, owner identity, schema, cardinality or revision invalidates the
   capture.

The token and vector are capture evidence, not Person data and not serialized
continuation state. Do not put the token in the saved Person snapshot, create
a new token, invent a Person-wide epoch, or claim a Person Store revision
substitutes for the per-Person lifecycle revisions. The owner-local snapshot
method has a caller precondition like the promoted Genealogy owner method; it
does not itself authorize capture or validate the runtime fence.

## 4. Export and private staged construction

Add internal schema-v1 immutable value records adjacent to `PersonStore` /
`PersonRuntime`, with these fields per row: exact `PersonId` value,
`BirthAbsoluteDay`, `DeathAbsoluteDay`, nullable residence settlement runtime
ID, nullable materialized NPC runtime ID, and exact local life/residence
revision. The owner header carries exact `PersonStore.Revision`, derived
membership/binding counts and the ordered immutable rows. The enclosing D
capture object supplies the transient token/vector association described
above; do not make it persistent row state.

The export operation copies every row and string/typed ID value into a new
private collection, exposes it read-only, and records source order. A repeated
capture at an unchanged token/vector has equal values. Later source writes or
mutations to caller-owned input buffers cannot change the snapshot.

An internal factory constructs a fresh unpublished `PersonStore` and exact
`PersonRuntime` rows without calling public registration, binding,
compensation, birth, materialization, or residence gameplay APIs. Populate the
store's primary ordered rows and `PersonId` lookup, restore each Person's
immutable birth, exact death, residence, materialized ID and local revision,
then rebuild `personsByNpcRuntimeId` from the materialized IDs. Set
`PersonStore.Revision` directly to the validated captured value after all rows
are built. No owner mutation is replayed, no IDs are allocated, no population
or genealogy state is changed, and no active runtime object is replaced. The
factory returns either a complete private store or failure with no partial
store published.

Keep the runtime identity index derived: it is rebuilt from the unique
materialized ID values and has cardinality exactly equal to the derived
binding count. Do not export it. Bind the fresh mutation guard, P12 admission
context and P12 lifecycle callbacks only during later unpublished D/G
composition, after validation and without emitting commit notifications for
hydration writes.

## 5. Validation ownership

### PersonStore-local checks

The isolated owner factory validates only invariants available from these
owners:

* supported schema and non-null row collection;
* non-null, non-whitespace exact Person IDs, unique by existing ordinal
  `PersonId` equality, retaining original string values;
* nonnegative birth/death values and death not earlier than birth;
* nullable residence and NPC-link values, where a present value is not
  whitespace-only and is preserved exactly;
* nonnegative per-Person lifecycle revision and valid store-revision lower
  bound for the known empty-start lifecycle;
* unique non-whitespace materialized NPC IDs using ordinal comparison;
* captured membership and binding counts equal the row count and derived
  link count; and
* successful local rebuild of both PersonId and NPC-ID lookup indexes.

Do not require every Person to have an NPC, require all Persons to be alive,
require residence, require birth date, or force local historical ordering
beyond the existing birth/death constraint. Reject malformed staging before
returning any store. Do not add PersonStore dependency to Genealogy or NPC
authorities.

### Merged D graph checks

After P12-C roots and the single merged NPC projection have been staged, the
outer D graph validator—not `PersonStore`—must validate cross-owner meaning:

* each Person materialized-NPC ID resolves to exactly one staged NPC;
* the staged NPC has the reciprocal Person identity for that same Person,
  with no NPC linked to multiple Persons and no duplicate Person link;
* each staged NPC's reciprocal Person reference is derived/wired from the
  validated PersonStore relation, not emitted as an independent duplicate
  identity fact. Reject any serialized NPC-side identity that conflicts with
  the Person-owned relation rather than selecting a winner;
* each non-null Person residence resolves to an admitted City/settlement
  identity. For a Person-backed NPC, use the Person's residence as the one
  authoritative relation; do not add or override it with NPC-only residence
  data. Existing life-state/residence consistency is checked against the
  corresponding NPC facts; and
* each birth/death day is no later than the exact restored P12-C absolute day.

These checks run before staged acceptance/publication. A Person-only row with
no NPC is valid. NPC-only actors remain valid under their existing NPC owner
contract. The Person snapshot does not contain City/NPC objects and the local
factory must remain usable without those authorities.

## 6. Supported writes and focused proof

The implementation review must trace the existing committed mutation paths,
confirming their exact current P12-B section/revision notifications and
existing failure/compensation behavior:

| Source path | Person facts affected | Census/revision boundary to verify |
|---|---|---|
| `SimulationRuntime.TryRegisterPerson` / named birth | Adds one Person row; birth fixed; birth may seed residence before registration. | Membership operation updates fixed membership/binding witnesses; dynamic Person life/residence row joins the current roster. Store revision advances once; capture records the lifecycle revision actually installed (including an unbound initial residence write). |
| `PersonMaterializationSystem.TryMaterializePerson` and `TryBindExistingNpcToPerson` | Changes one Person's optional NPC link; adoption may copy an NPC residence into an empty Person residence. | `runtime.npc-membership` binds/compensates exact PersonStore revision changes; if adoption changes residence, its per-Person life/residence section is included in the membership operation and its local revision advances once. NPC reciprocal state remains a single merged-D association. |
| `PersonStore.TryRollbackRegistration` and `TryRollbackMaterializedNpcBinding` | Removes only the exact just-created row or binding during compensation. | Each successful compensation advances `PersonStore.Revision`; roster reconciliation removes/updates dynamic witnesses and commits the actual changed set. Failed compensation faults the existing boundary; snapshot does not reconstruct a rolled-back intermediate state. |
| `SimulationRuntime.TryApplyPersonDeath` and resident-death path | Sets death absolute day; resident death may clear Person residence; current materialized NPC may receive matching life-state change. | Registered Person death operation includes the exact Person life/residence section and any applicable NPC/City sections; local Person revision capacity is preflighted for one or two increments. Rejection/no-op does not change the row. |
| `SimulationRuntime.TryBindExistingResident` | Sets an existing living Person's previously empty residence. | `person.residence-bind` includes its exact life/residence section, preflights local revision capacity and notifies the same section on commit. Same-residence no-op is not a successful write. |

These paths, as observed at the design base, provide the census row and local
revision needed for this isolated export. The design review should compare
them against current source again; any newly supported write that changes
`BirthAbsoluteDay`, `DeathAbsoluteDay`, residence or the materialized link
without advancing its bound local witness/token vector changes this disposition
to **NOT READY** until P12-B's accepted scope is amended and reviewed. Do not
patch a missing notification solely inside this snapshot slice.

Required focused tests:

1. Empty exact owner and populated detached round trip, preserving empty vs
   absent, insertion order, exact IDs/nullable dates/residence/link, local
   revisions and store revision. Include an unmaterialized Person, a
   materialized Person, a known-birth Person, unknown birth, a dead Person,
   and a Person with residence.
2. Mutate source Persons, links and residences after export; verify snapshot
   values remain unchanged and its collection cannot be changed. Repeated
   reads with an unchanged valid token/vector produce the same snapshot.
3. Verify cardinality/revision after register, materialization bind, exact
   registration/binding compensation, death, residence bind, and histories
   where compensation leaves the same final count but a higher revision.
   Verify initial residence assigned before P12 binding preserves its actual
   per-Person revision.
4. Restore nonzero/gapped exact revisions without replay. Preserve a saturated
   valid revision exactly; subsequent supported mutations fail through
   existing overflow preflight without changing the staged owner.
5. Reject unsupported schema, null collection/row, null/blank Person ID,
   duplicate Person ID, negative date/revision, death before birth, blank
   present residence/link, duplicate materialized NPC link, mismatched
   declared cardinalities, and impossible store-revision lower bound. Every
   rejection returns no staged owner and leaves the source unchanged.
6. In the later merged-D test, reject missing/nonreciprocal/duplicate NPC
   links, nonexistent settlement identity, future birth/death relative to
   restored absolute day, and mismatched Person/NPC life or residence. These
   are cross-owner tests; do not make the isolated PersonStore factory query
   NPC, City, Calendar, or SimulationRuntime state.
7. Run the focused Person owner snapshot suite, Person registration,
   materialization and named-birth/death regressions, selected Daily-v1
   inventory/token tests, ALL EditMode, complete official `Smoke`, and
   `git diff --check` for implementation. No Unity tests are required for this
   documentation-only design candidate.

## 7. Ownership, exclusions and next integration

Only `Person/PersonStore.cs`, `Person/PersonRuntime.cs`, a small adjacent
immutable snapshot contract file if needed, and a focused EditMode test file
are owned by this isolated implementation. Keep `SimulationRuntime.cs`,
`P12PopulationLifecycleCensus.cs`, `SimulationBootstrapComposition`,
`NpcRuntime.cs`, city/population owners, P12 envelope/admission and the daily
loop untouched. The later D adapter, which has shared `SimulationRuntime` and
D/E/F composition hotspots, consumes this owner API in its serialized
integration window.

This design adds no duplicate NPC/Person identity truth, age state, new
Person lifecycle semantics, authorization/security checks, population
recalculation, NPC adapter, bootstrap/provider registration, operation IDs,
shared-epoch wiring, owner-thread enforcement, capture eligibility, envelope,
P12-A profile integration or P13 behavior. It does not reclassify excluded
P8-D/E, P10, P14, P18, P19 or P20 facts. P12-D remains open; P12-A remains
`WAIT_DEPENDENCY`; P12-F/G and P13 retain their explicit dependencies.

After independent design review, an isolated Person owner implementation may
start on the exact reviewed base. Its completion does not make the entire
Person/NPC graph ready or P12-A ready. The D adapter must merge the
Person-owned link exactly once, run the cross-owner checks above, and validate
the same P12-B capture token and owner vector for the whole D/E/F capture.
