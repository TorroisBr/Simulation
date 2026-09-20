# Phase 5 — Current Canonical State

## Canonical

Canonical branch:

`codex/phase5/canonical`

Last validated domain integration commit:

`29b5100`

This commit consolidated:

- Genealogy World Integration
- Parent-Aware Named Birth
- Person Maturity Foundation
- world-owned GenealogyStore alias hardening
- Institution World Integration
- Institutional vacancy recognition and tenure history
- Natural Mortality Foundation
- Aggregate Demography Foundation
- deterministic daily demographic integration
- minimal property ownership and estate continuity
- deterministic succession candidate discovery
- explicit property transfer foundation
- office succession after recognized vacancy
- estate succession composed with explicit property transfer

Validated baseline:

- Property transfer foundation: `5/5`
- Succession candidate foundation: `5/5`
- Succession integration: `8/8`
- Estate foundation: `9/9`
- Institutional vacancy: `7/7`
- Runtime integration: `5/5`
- Runtime orchestration: `6/6`
- Diagnostics: `46/46`
- Long-run: `7/7`
- ALL EditMode: `1348/1348`
- official Smoke: `5/5`
- `git diff --check`: green

The succession wave did not modify `SimulationRuntime.AdvanceDay`; the prior
long-run gate therefore remains the applicable daily-loop validation.

Historical demographic baseline before the continuity wave:

- ALL EditMode: `1309/1309`
- official Smoke: `5/5`
- `git diff --check`: green

The HEAD of `codex/phase5/canonical` is the authoritative starting point for new Phase 5 work.

## Phase 5 purpose

Phase 5 establishes:

- scalable population representation;
- lightweight Person identity;
- demographic lifecycle;
- genealogy;
- institutions and offices;
- death and property continuity;
- institutional continuity;
- succession foundations.

Deep political simulation belongs to Phase 6.

## Completed foundations

### Population

Completed:

- settlement aggregate population;
- authoritative NPC roster;
- residency/membership;
- immigration;
- emigration;
- migration;
- resident death;
- fatal conflict → population lifecycle;
- atomic injury/death integration.

Representation:

`Population aggregate → Person → NpcRuntime → Active/Dormant`

Materialization does not change aggregate population.

### Calendar

Completed:

- simulation calendar;
- absolute-day timeline;
- custom calendars;
- date conversion;
- chronological age calculation.

Runtime ownership is now explicit: `SimulationRuntime` receives the resolved
calendar definition once and owns an immutable `SimulationCalendar` copy for
the run. UI/date access uses that runtime copy after initialization.

### Configuration

Resolution:

`Defaults → Preset → World → Content → Effective`

Population configuration includes:

- representation mode;
- decision scope;
- maturity age threshold.

Default maturity:

`18 completed years`

Demographic policy is resolved through the same precedence chain. Natural
mortality has an explicit policy and annual probability. Aggregate demography
has an explicit policy and annual birth/death rates. Invalid finite/range
constraints are rejected by the effective configuration validator.

### Person

Completed:

- PersonId;
- PersonRuntime;
- PersonStore;
- BirthAbsoluteDay;
- PersonAgeQuery;
- PersonMaturityQuery;
- Person-level residence;
- materialization;
- Named Birth.

Age and maturity are derived.

### Named Birth

Completed:

- aggregate population +1;
- Person registration;
- birth day;
- residence;
- stale day/population validation;
- rollback;
- no NpcRuntime creation.

Parent-aware birth supports `0..N` parents.

Aggregate-only birth remains separate.

### Genealogy

Completed:

- ParentageRecord;
- GenealogyStore;
- multiple parents/children;
- ancestors/descendants;
- cycle protection;
- world integration;
- Person existence validation;
- multi-world isolation;
- private world-owned copied store.

### Institutions

Completed foundations:

- InstitutionId;
- OfficeId;
- InstitutionRecord;
- OfficeRecord;
- OfficeIncumbency;
- InstitutionStore;
- OfficeStore;
- vacancy;
- assignment;
- vacating.

Incumbency references PersonId.

World integration completed:

- world-owned InstitutionStore and OfficeStore;
- paired-store validation and copied world state;
- PersonStore existence validation for incumbencies;
- deterministic world-level institution and office queries;
- multi-world isolation;
- no automatic vacancy recognition from factual death.

Natural factual death remains separate from institutional recognition; offices
are not vacated by the demographic phase.

Institutional continuity foundation now also includes:

- explicit vacancy-recognition proposals and applications;
- factual-death recognition only through the world PersonStore boundary;
- deterministic vacancy reasons and stale-incumbency protection;
- closed `OfficeTenureRecord` history, including legacy explicit vacates;
- no automatic office vacancy when a Person becomes factually dead.

The institution core does not inspect PersonStore or SimulationRuntime. World
composition performs the factual-death check before delegating recognition.

### Natural mortality

Completed and integrated:

- `PersonRuntime.DeathAbsoluteDay` is chronological factual truth;
- alive/dead is derived for a requested absolute day;
- deterministic calendar-aware mortality evaluation;
- atomic world-owned death for dormant and materialized resident Persons;
- aggregate decrement and residence clearing exactly once for represented
  resident death;
- deterministic entity/day sample boundary;
- production conflict paths use world-owned Person death authority.

Unknown-birth Persons are not autonomously evaluated until a birth day exists.

### Aggregate demography

Completed and integrated:

- deterministic aggregate-only birth/death provider and transitions;
- explicit represented-resident floor supplied by the world boundary;
- no Person/Npc inspection inside aggregate demography;
- floor-preserving aggregate deaths;
- aggregate-only births/deaths do not create or delete Persons/NpcRuntime;
- provider mutation/exception rollback and stale/revision guards.

### Property and estate continuity

The minimal continuity foundation is now integrated with the world boundary:

- property ownership is an immutable PersonId-based world fact;
- property ownership is separate from NpcRuntime money/inventory custody;
- estate opening is explicit, deterministic, and gated by factual Person death;
- estate records preserve the deceased PersonId and opening absolute day;
- estate state is separate from institutional vacancy recognition;
- SimulationRuntime clones and validates property/estate stores, including
  exact EstateStore/PersonStore pairing and current-day constraints;
- property and estate facts participate in deterministic snapshots, canonical
  export, diffs, formatting, and invariant validation.

No inheritance law, transfer rules, taxes, creditors, or automatic daily estate
processing are part of this foundation.

Succession continuity now also includes:

- explicit deterministic PersonId-based property transfer transitions;
- factual birth/death-day validation for new property owners;
- property-store revision and stale-ownership guards;
- immutable transfer history exposed to deterministic diagnostics;
- explicit office succession only after institutional vacancy recognition;
- explicit estate succession that transfers a named property only after factual
  death and direct-child candidate validation;
- no automatic selection, inheritance law, estate administration, or daily
  succession processing.

### Daily demographic phase

The shared daily-loop owner is the demographic integration in
`SimulationRuntime.AdvanceDay`. The deterministic ordering is:

1. advance absolute time and existing place-content/day bookkeeping;
2. evaluate and apply named natural deaths at the new `CurrentDay` from a
   stable PersonId-sorted snapshot;
3. compute living represented-resident floors at the world boundary;
4. apply aggregate-only births/deaths in stable settlement RuntimeId order;
5. begin remaining day systems, scheduled directives, economy, knowledge,
   expeditions, NPC decisions/actions, and travel.

Dead NPCs therefore cannot advance merchant urgency or autonomous activity later
on the same day. The phase exposes deterministic diagnostics and its last
report through the runtime. Aggregate provider policy is disabled unless the
effective configuration enables it, even when an implementation is injected.

### Diagnostics

Completed deterministic diagnostics including parentage, property ownership,
and estate opening. Institutional tenure history remains a world-owned
institutional record and is covered by its domain integration tests.

Genealogy validation detects:

- missing endpoints;
- self-parent;
- duplicate edge;
- cycle.

Maturity is intentionally derived rather than stored in diagnostics.

Property transfer history includes previous owner, new owner, property, and
absolute day in snapshots, canonical output, diffs, and invariant validation.

### Succession foundation

Completed:

- deterministic direct-child candidate discovery from PersonId genealogy;
- factual alive filtering from `DeathAbsoluteDay`;
- derived calendar/configuration maturity filtering;
- eligibility-sensitive, delimiter-safe discovery fingerprints;
- explicit selected-candidate office succession transitions;
- explicit selected-candidate estate succession transitions;
- stale world-day, candidate-set, tenure, estate, PersonStore, and property
  ownership validation;
- world-owned property transfer history and diagnostics integration.

The candidate foundation is domain-neutral and performs no mutation or automatic
selection. Office succession composes recognized vacancy. Estate succession
composes an explicit property transfer. Deep politics remains outside Phase 5.

## Architecture

`WORLD TRUTH != KNOWLEDGE != INSTITUTIONAL RECOGNITION`

Factual death and institutional recognition of vacancy are not necessarily the same event.

Decision uses knowledge.

Execution revalidates truth.

Events/history are not primary truth.

## Remaining Phase 5 areas

No implementation area remains on the Phase 5 roadmap after promotion of the
validated succession integration. The orchestrator must perform the final
canonical history/remote synchronization audit and then stop Phase 5.

Deep politics remains Phase 6 and must not be started automatically.

## Parallelism guidance

Reevaluate actual code before every wave.

Current next wave:

- None. Succession is the final bounded Phase 5 wave and is validated on the
  integration branch.

Institution world integration is complete and is no longer an active parallel lane.

Factual Person death remains separate from institutional vacancy recognition.
Aggregate demography remains aggregate truth and must not inspect Person/Npc runtime
state to calculate represented-resident floors.

## Phase 5 completion gate

Phase 5 is complete only when required foundations for:

- Persons;
- lifecycle;
- genealogy;
- aggregate demography;
- institutions;
- death/property continuity;
- vacancy/institutional continuity;
- succession

are implemented, integrated and validated.

Before declaring completion:

- architecture review;
- relevant targeted tests;
- ALL EditMode;
- official Smoke;
- `git diff --check`;
- canonical history audit;
- remote synchronization.

Historical demographic integration gates for the earlier demographic wave:

- targeted EditMode: `490/490` across 23 suites;
- `SimulationRuntimeLongRunTests`: `7/7`;
- ALL EditMode: `1309/1309`;
- official Smoke: `5/5`;
- failures/skips: `0/0`;
- `git diff --check`: green;
- local/upstream/remote integration branch synchronized at
  `eb93c7fcd4283f566f840fa1cde7e0269ed804b6`.

Final succession validation gates:

- Property transfer foundation: `5/5`;
- Succession candidate foundation: `5/5`;
- Succession integration: `8/8`;
- ALL EditMode: `1348/1348`;
- official Smoke: `5/5`;
- failures/skips: `0/0`;
- `git diff --check`: green;
- no `AdvanceDay` or long-run behavior changes in the succession wave.

Do not automatically begin Phase 6.

## Frozen historical work

Do not merge/cherry-pick unless explicitly requested:

`codex/phase6/DemographicAgeFoundation`

commit:

`d192c473...`

Superseded by current Person-based age architecture.

Timeline spike:

`bbae410d9e3abcaf57282e70a40edb9044b785d9`

Frozen for future Platform/Persistence work.
