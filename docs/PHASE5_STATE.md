# Phase 5 — Current Canonical State

## Canonical

Canonical branch:

`codex/phase5/canonical`

Last validated domain integration commit:

`a556c365dadb776ebcff29aeced45c22dd1cadfd`

This commit consolidated:

- Genealogy World Integration
- Parent-Aware Named Birth
- Person Maturity Foundation
- world-owned GenealogyStore alias hardening

Validated baseline:

- ALL EditMode: `1259/1259`
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

### Configuration

Resolution:

`Defaults → Preset → World → Content → Effective`

Population configuration includes:

- representation mode;
- decision scope;
- maturity age threshold.

Default maturity:

`18 completed years`

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

Full world integration remains.

### Diagnostics

Completed deterministic diagnostics including parentage.

Genealogy validation detects:

- missing endpoints;
- self-parent;
- duplicate edge;
- cycle.

Maturity is intentionally derived rather than stored in diagnostics.

## Architecture

`WORLD TRUTH != KNOWLEDGE != INSTITUTIONAL RECOGNITION`

Factual death and institutional recognition of vacancy are not necessarily the same event.

Decision uses knowledge.

Execution revalidates truth.

Events/history are not primary truth.

## Remaining Phase 5 areas

The Orchestrator must inspect current code before converting these into tasks.

Likely remaining work:

### Natural mortality

Build age/natural-death foundations using existing Person/calendar/lifecycle architecture.

No mutable age.

No terrestrial 365-day assumptions.

### Aggregate demography

Support demographic change for non-individualized population.

Preserve aggregate births without mandatory Person creation.

### Institution world integration

Integrate Institution/Office stores with world runtime ownership and Person validation.

Preserve distinction between factual state and institutional recognition.

### Death, property and estate

Establish minimal property continuity after Person death.

Avoid premature full inheritance/political claims systems.

### Institutional continuity and vacancy recognition

Factual death must not necessarily equal immediately recognized vacancy.

### Succession foundation

After genealogy, death/estate and institutions are stable, implement minimal succession foundation.

Deep politics remains Phase 6.

## Parallelism guidance

Reevaluate actual code before every wave.

Potentially independent:

- mortality/lifecycle;
- institution world integration.

Potentially conflicting:

- natural mortality vs aggregate demography;
- death/estate vs succession;
- vacancy recognition vs institution runtime integration.

Evaluate semantic overlap, not just Git conflicts.

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
