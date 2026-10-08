# Independent review — P12-D Person owner snapshot design

**Review disposition:** `VALIDATED_CANDIDATE` — technical design review PASS; the isolated PersonStore/PersonRuntime owner slice is ready for bounded implementation.

**Candidate:** `codex/phase12/P12DPersonOwnerSnapshotDesign` at `0c602d37589c993ea9956f8b7f42e9d807479c08`\
**Candidate base / current P12 canonical at review:** `ad4b20c42a25c9fd453c98695a79ce59490bf4fe`\
**Architecture baseline:** `codex/architecture/world-identity-projection` at `47eff220c7ce00f6e7c759bdc2b76780bb46f628`\
**Review branch:** `codex/phase12/P12DPersonOwnerSnapshotDesignReview`, based on the unchanged candidate tip.

## Review scope and method

Reviewed the complete candidate diff against its stated base, the current P12 State and Brief, the accepted P12-D and P12-F boundaries, the current P12-B owner and operation evidence, and the underlying PersonStore, PersonRuntime, census providers, runtime entrypoints, materialization, birth, death, and residence callsites. The candidate diff is one documentation-only file addition. No Unity test run was required for this design review; the review worktree's documentation diff was checked with `git diff --check`.

## Findings

### Ownership and architecture fit — PASS

The design serializes existing Person-owned values only: stable `PersonId`, nullable absolute birth/death days, Person-level settlement residence, optional materialized NPC runtime ID, owner insertion order, `PersonStore.Revision`, and each owner's `LifeResidenceRevision`. This matches the architecture's durable identity rule: identity survives death/dormancy/materialization and persisted individual relations use `PersonId`, not a representation-only `NpcRuntimeId` (`docs/SIMULATION_ARCHITECTURE.md:370-390,398-421`).

The current D contract assigns those fields and the reciprocal materialization index to `PersonStore`/`PersonRuntime`; it explicitly forbids duplicate NPC residence and genealogy collections (`docs/design/PHASE12_D_TECHNICAL_DESIGN.md:42-50`). The design makes the Person-owned link authoritative, rebuilds the reverse index as derived state, and defers reciprocal NPC identity checks to the merged D graph. It does not add age, gameplay, population recomputation, genealogy edges, or a second Person/NPC identity authority.

The single concrete `NpcRuntime` owner boundary is preserved. D/E/F projections must share one token and component revision vector and merge before one NPC reconstruction (`docs/design/PHASE12_D_TECHNICAL_DESIGN.md:25-27,42,47`; `docs/design/PHASE12_F_TECHNICAL_DESIGN.md:75-90`). The Person package contains no NPC object or separate NPC snapshot. Cross-owner validation remains with D; local staging stays usable without City/NPC authorities.

### Identity, cardinality, revisions, and time — PASS

The proposed exact ID preservation and ordinal uniqueness agree with `PersonId`'s immutable value and ordinal equality (`Assets/_Project/Scripts/Person/PersonId.cs:6-20,34-47`). One row per registered Person and one optional unique NPC link match the existing PersonStore's primary and reverse indexes (`Assets/_Project/Scripts/Person/PersonStore.cs:26-36,43-75,165-222`) and the D contract's one-to-one rule (`docs/design/PHASE12_D_TECHNICAL_DESIGN.md:86-90`). Unmaterialized and dead Persons remain representable.

The revision split is correct: registration, binding, and exact compensation advance `PersonStore.Revision`; death and residence commits advance the relevant Person's local revision; materialization does not masquerade as a life/residence mutation (`PersonStore.cs:65-75,97-116,208-248`; `PersonRuntime.cs:113-179`). The `PersonCount + BindingCount - 1` lower bound is checked with widened arithmetic and preserves gaps from compensated operations. It is valid for the explicitly accepted empty-start Daily-v1 lifecycle and is not asserted as a universal historical rule.

Nullable birth/death values are copied as absolute days without age derivation or calendar rewrite. Existing owners reject negative dates and death-before-birth (`PersonRuntime.cs:71-104,113-139`); the merged D graph checks dates against the staged P12-C day and validates settlement references (`PHASE12_D_TECHNICAL_DESIGN.md:78-90`). The design also correctly accounts for initial residence being assigned before Person registration, so that non-null residence need not imply a positive lifecycle revision (`PersonBirthLifecycle.cs:395-405`).

### P12-B census and committed-write prerequisites — PASS

The current B witnesses provide an exact installed `PersonStore` owner with membership/binding cardinalities and the common exact store revision (`Assets/_Project/Scripts/Person/PersonStoreCensusProviders.cs:9-29,36-56`). The dynamic provider emits exactly one schema-v1 section per exact Person owner with cardinality 1 and its `LifeResidenceRevision`; provider creation rejects duplicate Person IDs/owners (`Assets/_Project/Scripts/Population/P12LifecycleCensusProviders.cs:5-29,31-60`). Lifecycle protocol initialization and current-roster reconciliation install/assess those providers (`Assets/_Project/Scripts/P12PopulationLifecycleCensus.cs:159-171,246-265`).

The supported write paths match the design's section/revision claims:

- `SimulationRuntime.TryRegisterPerson` wraps registration in the runtime-owned membership census scope and records the PersonStore revision commit (`SimulationRuntime.cs:6088-6122`). That scope reconciles the updated fixed sections and rebuilt dynamic Person roster before completing (`SimulationRuntime.cs:5600-5637`).
- Death and resident-death paths build the exact Person section, preflight the needed one or two local revision increments, and commit through the lifecycle operation (`P12PopulationLifecycleCensus.cs:556-582,584-635`; `SimulationRuntime.cs:6137-6199`; `PersonDeathLifecycle.cs:540-573`).
- Residence binding creates the named `runtime.person.residence-bind` operation, preflights local revision capacity, and commits through the same lifecycle boundary (`SimulationRuntime.cs:7843-7884`; `P12PopulationLifecycleCensus.cs:638-681`). Residence changes on a Person-backed NPC are delegated to the Person owner (`NpcRuntime.cs:973-990`); direct NPC death rejects a Person-backed NPC (`NpcRuntime.cs:473-487`).
- Materialization/adoption enters the runtime membership scope with the affected Person lifecycle section, marks each PersonStore revision change, and records exact compensation when needed (`SimulationRuntime.cs:7788-7841,5600-5637`; `PersonMaterialization.cs:111-145,230-251`). The current repository's supported callsites use these runtime facades; the isolated static helpers are implementation callees, not alternate Daily-v1 ingress.

The latest canonical State records P12-B complete within its accepted bounded scope, including source-linked owner/ingress and committed-write reconciliation, runtime-wide owner-thread/quiescence, and token publication only after a successful completed boundary (`docs/PHASE12_STATE.md:141-146,183-192`). The candidate does not inflate that evidence into a general guarantee about future ingress, nor claim complete P12-D, P12-A readiness, or P13 readiness.

### Staging and scope — PASS

The proposed factory is private and unpublished, restores exact values/revisions without replaying registration, birth, death, binding, or population operations, and rebuilds only the PersonStore-derived NPC index. Its local validation is restricted to PersonStore facts; settlement existence, NPC reciprocity/life consistency, and P12-C temporal comparison stay in the merged D validator. The retained focused test plan covers empty versus absent, detachment/order, unmaterialized/materialized/dead Persons, revisions and compensation, saturation, malformed local input, and cross-owner rejection.

The permitted implementation seam is limited to `PersonStore.cs`, `PersonRuntime.cs`, an adjacent immutable value type if needed, and focused tests. It excludes `SimulationRuntime`, census/admission, bootstrap, NPC owner reconstruction, envelope publication, P12-A integration, and P13 (`candidate: docs/design/PHASE12_P12D_PERSON_OWNER_SNAPSHOT_DESIGN.md:281-304`). This respects P12-D's instruction to implement isolated owners while keeping shared D/E/F runtime integration serialized (`PHASE12_D_TECHNICAL_DESIGN.md:113-127`).

## Disposition and integration constraints

The design is compatible with architecture `47eff220c7ce00f6e7c759bdc2b76780bb46f628`, the current P12-D/F contracts, and the promoted P12-B/C dependencies. No unresolved product or canonical architecture choice remains for this local owner slice.

Implementation may proceed only on an isolated branch/worktree from the reviewed base and within the listed file boundary. The implementation must consume the exact current token/owner vector at the later D capture boundary; this owner-local snapshot method does not itself authorize capture. A newly supported write that changes a serialized Person field without the existing P12-B witness/vector notification would invalidate this review and require renewed P12-B evidence. This review does not promote code, integrate the whole D graph, change canonical State, make P12-A ready, or close P12-D/Phase 12.
