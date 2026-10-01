# World Identity and Read-Only Projection Surface — architecture study

**Status:** reviewed architecture candidate; semantic recommendations below are **NEW PROPOSAL**, not promoted implementation contracts. **Baseline:** `origin/codex/phase12/canonical` at `43dba1b5d16cba1558c4c39239f3cd0d4c669958` (2026-10-01). P20's separate canonical State is retained on `origin/codex/phase20/canonical` at `7a81cc0ecbc511dd36c248ec62c7b20f7e477f53`; the architecture and execution model files match this baseline. This study changes no runtime code or External project.

**Evidence labels:** **CANONICAL EVIDENCE** identifies existing promoted contracts or code; **ARCHITECTURAL INFERENCE** states a consequence that has not been accepted as a new contract; **NEW PROPOSAL** names the decision requested at the architecture gate. The labels apply to each section's recommendation, not to an External requirement.

## 1. Problem statement

**CANONICAL EVIDENCE:** Simulation owns mutable World Truth in domain authorities and distinguishes Truth, Knowledge, diagnostics, save, replay and History (`SIMULATION_ARCHITECTURE.md` §§4, 77–80, 91–92). **ARCHITECTURAL INFERENCE:** a portable read-only export needs a stable source identity and approved factual reads, but cannot make a save image, diagnostic snapshot or implementation Store into an interoperability API. **NEW PROPOSAL:** establish a durable world continuation identity and a bounded, consumer-neutral factual projection boundary before a real producer is implemented.

## 2. Current canonical evidence

- **CANONICAL EVIDENCE:** P9-A/B build a deterministic pre-start world and atomically publish selected output through owning authorities (`phases/PHASE9_BRIEF.md`, `PHASE9_STATE.md`, `SimulationGenesisPipeline.cs`). `SimulationGenesisManifest` records a seed, profile contract, fingerprint, stage/provenance and first boundary; it has no WorldId. A seed and fingerprint describe reproducible inputs/content, not an individual resulting world.
- **CANONICAL EVIDENCE:** P18's `DailyBoundaryOperation`, `ActivityLifecycleStore` and `P18DIntradayProfile` accept a nonempty string called `WorldId` (`LogicalTimeline.cs`, `ActivityLifecycle.cs`, `SimulationRuntime.P18D.cs`). Their constructor input namespaces occurrences and activities. No promoted contract assigns it at successful genesis, persists it across a load, or specifies copy/fork semantics. P18's subphase `ContinuationId` is a boundary continuation identifier, not the identity of a simulation branch (`design/PHASE18_A_BOUNDARY_SUBPHASE_EXTENSION_DESIGN.md`).
- **CANONICAL EVIDENCE:** P12 is in progress: passive owner census and bounded runtime admission foundations are promoted, while a complete read epoch, capture eligibility, exact export, staged hydration and P12-A save/load are not (`PHASE12_STATE.md`, `phases/PHASE12_BRIEF.md`).
- **CANONICAL EVIDENCE:** P13 promises independent continuation from every actually simulated boundary, but is `WAIT_DEPENDENCY`; it has no approved checkpoint or fork identity rule (`phases/PHASE13_BRIEF.md`).
- **CANONICAL EVIDENCE:** Faction Truth is owned by `FactionStore`; `FactionRecord` has a stable `FactionId`, optional-in-practice display text (empty string allowed), creation day and membership policy. `FactionAffiliationRecord` carries affiliation, faction and persistent `PersonId`, joined/ended days and an active predicate. Store reads are sorted, but `SimulationRuntime` currently exposes records and the diagnostic `WorldStateSnapshot` is expressly not domain Truth (`FactionContracts.cs`, `FactionStore.cs`, `SimulationRuntime.cs`, architecture §§23, 26–27, 77–80). The selected P12 daily profile composes political stores even when initially empty; populated save coverage is not proved (`design/PHASE12_OWNER_COVERAGE_INVENTORY.md`).

## 3. External integration requirement summary

**CANONICAL EVIDENCE (external evidence supplied by requester, not Simulation authority):** Simulation-External owns `world-schema`, `world-io`, portable `.world.json` and reference consumers; World Exchange presently requires a stable WorldId. No real Simulation producer exists. The External readiness labels are hypotheses against Simulation's current state. **ARCHITECTURAL INFERENCE:** the required identifier blocks even a factual Faction exchange document today. **NEW PROPOSAL:** an adapter may translate approved Simulation projections to World Exchange; neither TypeScript types nor consumer display requests define Simulation domain semantics.

## 4. World identity analysis

**CANONICAL EVIDENCE:** no discovered canonical WorldId has the full lifecycle guarantee. Existing P18 `worldId` is an injected local namespace, P9 fingerprint and seed are generation provenance, `RuntimeId` identifies a runtime object, and a save path identifies a file. **NEW PROPOSAL:** `WorldId` identifies one independently continuable **causal world branch**: the authoritative world founded at a successful initial publication and its same-branch continuations. It is opaque, stable, unique among independently evolving branches, and owned by the top-level world composition/identity boundary, not by a spatial store, generator stage, save file, Unity asset, exporter or P18 timeline. The exact representation and allocation algorithm are later technical design; a GUID is one possible implementation, not the semantic decision. A user-facing world name is optional metadata.

**ARCHITECTURAL INFERENCE:** P18's existing `worldId` inputs can eventually consume this identity for supported compositions, but are not proof it already exists as a durable domain fact. They must not be silently treated as a backward-compatible persisted identity for old test fixtures.

## 5. World lifecycle and fork semantics

| Boundary | **NEW PROPOSAL** | Existing constraint / rationale |
|---|---|---|
| New generated or manually authored world | Reserve a candidate identity during construction if needed; publish WorldId atomically with a validated complete initial World Truth before the first simulated boundary. Failed genesis publishes no world or committed WorldId. A failed candidate value need not be reused. | P9's private pre-start composition and first-boundary rule are **CANONICAL EVIDENCE**. |
| Save and load | Preserve the same WorldId when loading and continuing the same branch. | P12's continuation equivalence is **CANONICAL EVIDENCE**; the identity rule is new. |
| File copy | Byte-for-byte copies initially refer to the same branch. File names and copy operations do not create world identity. Before both copies continue independently, an explicit branch operation assigns a new WorldId to one continuation; silent divergence under one identity is unsupported. | Distinguishes physical copy from causal fork. The UI/automatic detection rule is an open technical/product question. |
| P13 historical fork | Reconstruct the exact parent truth and continuation state at T, then publish an independent branch with a **new WorldId** and origin provenance `(parent WorldId, actual simulated boundary T)`. Inherited pre-T facts retain their own domain identities and semantics; future branch-local allocations/occurrences must be unambiguous. | P13 guarantees forkability but does not currently specify branch identity. No retroactive simulated history is invented. |
| Template, clone, import, scenario | Content or a template has its own provenance/identity; creating an independently continuable world assigns a new WorldId. Importing a continuation may preserve identity only when it is the same branch with compatible provenance and no independent divergence. | No import/template implementation is implied. |

**NEW PROPOSAL:** a distinct global `LineageId` or `BranchId` is not required in v1 if WorldId itself identifies the branch and fork/clone origin provenance records the parent. Add a separate lineage identity only for a concrete query/interop need that provenance cannot satisfy. Runtime-instance/session ID, save-file ID and worldgen seed remain distinct. **Open P13 technical boundary:** existing P18 identifiers encode an injected `worldId`; a fork design must preserve inherited receipts/identities while changing the namespace for future branch-local occurrences, without replaying old effects or rekeying historical domain facts. This is a P13 design gate, not permission to narrow P13's guarantee.

## 6. Projection surface alternatives

| Alternative | Assessment |
|---|---|
| One aggregate World read object | Convenient discovery; risks a giant synchronized DTO and all-owner dependency. Reject as the factual contract. |
| Direct Store / `SimulationRuntime` access | Existing accessors help internal diagnostics, but expose mutable implementation topology and permit incoherent mixed-time reads. Reject as external API. |
| Reuse `WorldStateSnapshot` or P12 capture | Snapshot is diagnostic; P12 capture must contain continuation state and compatibility detail. Reject as the public factual model. |
| Capability-specific factual readers with immutable records, composed by a read session | Fits separate domain authorities and optional capabilities; choose this for bounded slices. |

## 7. Recommended architecture

**NEW PROPOSAL:** a small world read root exposes WorldId, a declared logical boundary/read-consistency mode and discovery of supported factual capabilities. Each capability supplies immutable, value-based records and stable semantic IDs through read-only query/enumeration ports; adapters compose only needed readers. The first implementation should have one Faction reader, not a universal entity interface. The read layer is a projection of current owner truth, never a second writable store or persistent cache. It cannot mutate, consume RNG, allocate IDs, trigger simulation, or silently substitute stale diagnostics. Sort output by canonical semantic IDs and explicitly declare relation completeness and omissions. Core contracts should avoid `UnityEngine` types where current domain ownership allows; Unity may supply the composition adapter.

## 8. Read consistency model

**NEW PROPOSAL:** provide two explicitly named service levels: (1) best-effort/current inspection for diagnostics, labelled noncoherent and never used to promise an exchange document; (2) bounded coherent factual read session for interoperable export. A coherent session reads a declared set of owners at one completed logical boundary or validated quiescent/epoch cut and either returns an immutable, internally referentially consistent result or fails/retries. It does not lock the entire game indefinitely or claim that an individual Store's sorted copy is a cross-owner transaction. Epoch validation or quiescence may be reused from P12 if available; an earlier bounded read can use its own verified owner-thread/boundary discipline. Any selected mode must document which writes it excludes and must reject an unsupported live/concurrent composition instead of calling it coherent.

**ARCHITECTURAL INFERENCE:** the initial Faction-only catalogue may require only Faction owner coherence; including Person-linked affiliations requires a coherent Faction+Person cut and reference validation. A file producer should not quietly weaken that guarantee.

## 9. Actor Knowledge boundary

**CANONICAL EVIDENCE:** World Truth and Knowledge are separate (§4); PoliticalKnowledge and SpatialKnowledge have their own authorities. **NEW PROPOSAL:** the factual surface exposes owner-validated objective facts only. Actor-specific Knowledge needs a separate explicitly scoped holder/perspective and uncertainty contract later. Missing from a factual projection means unsupported/omitted, not unknown to a named actor. Do not emit an actor's belief as an objective Faction affiliation or location.

## 10. P12 relationship

**CANONICAL EVIDENCE:** P12-B's census, mutation epochs and quiescence work serves save admission; P12-C–G and P12-A save are not complete. **NEW PROPOSAL:** classify P12-B/A/full-P12 as **NO_DEPENDENCY** for a bounded read port and first Faction projection. Selected epoch/quiescence machinery is **OPTIONAL_REUSE** after it is promoted and shown to cover the projected owners. P12 semantic identity, owner inventory and compatible logical boundaries require **ARCHITECTURAL_ALIGNMENT**. Coherent export has its own proof burden; it must not read P12's private capture token or expose P12 save structures as public API. Projection must not hydrate or become a second continuation authority.

## 11. P13 relationship

**CANONICAL EVIDENCE:** every actually simulated boundary is reconstructible and independently forkable; generated backstory is excluded. **NEW PROPOSAL:** a fork is a new WorldId with parent/boundary provenance after exact reconstruction. Projection of the fork reads its current factual state, not parent history or a retrospective merge. WorldId is a **HARD_DEPENDENCY** for a P13 implementation claiming distinguishable branch identity; P13's reconstruction mechanics are **NO_DEPENDENCY** for a present-time projection. The exact interaction with P18 namespace and branch-local allocation remains a required P13 technical design item.

## 12. P19 relationship

**CANONICAL EVIDENCE:** P19 is a later mechanics/extension API/loader, not an external factual reader (`phases/PHASE19_BRIEF.md`). **NEW PROPOSAL:** P19 is **NO_DEPENDENCY** for core WorldId, bounded Faction read port and first official producer. Future P19-defined facts may require domain-owned projection contributors only when real extension consumers exist; preserve the possibility of capability discovery and versioned meaning, without a generic registration/loader scheme now. A code mod's private data is not automatically externally publishable.

## 13. Unity/core boundary

**CANONICAL EVIDENCE:** current world bootstrap uses `TesteSimulacao`/Unity configuration, while Faction contracts and stores are plain C# domain classes. **NEW PROPOSAL:** put immutable factual records and ports beside plain C# domain/application contracts; a Unity bootstrap adapter supplies the selected runtime and stable identity. No Unity asset, `MonoBehaviour`, `NpcRuntime`, GameObject or `UnityEngine.Object` crosses the read contract or World Exchange adapter. A headless consumer can compose the same readers when it has an authoritative world instance.

## 14. First bounded projection slice

**NEW PROPOSAL:** Faction is the strongest first real slice **after** WorldId publication and a verified coherent Faction read. Source: `FactionStore` for FactionId, created day, membership policy, expulsion rule and FactionAffiliation records; `PersonStore` only for validating any projected PersonId relationship; top-level world identity owner for WorldId. `FactionRecord.DisplayName` may be empty and must not be fabricated. Project active affiliations only when the adapter can represent their Person references validly and the cross-owner cut is coherent; otherwise omit that optional relation explicitly. Support a fixture with at least one Faction and affiliation, since an empty selected bootstrap would not prove semantics. The adapter may choose only the subset World Exchange supports; support/loyalty/office/Organization, actor Knowledge, full Person details and historical affiliation narrative are legitimate omissions. Current Simulation readiness is therefore **Faction domain facts available, producer not ready**; External's `READY_TO_PROJECT` is correct only as a bounded source assessment, not as end-to-end readiness.

## 15. Dependency DAG

| Edge | Classification | Reason |
|---|---|---|
| P9/P12/P13 delivery → WorldId concept | NO_DEPENDENCY | A manually authored world also needs identity; its semantics can be accepted before procedural genesis, save or fork implementation. |
| P9 pre-start publication → new generated WorldId | ARCHITECTURAL_ALIGNMENT | Identity shares the successful publication boundary; no dependency on procedural P9/P10 scope. Existing P9-A/B remain valid. |
| P12 save/load → preserve WorldId | ARCHITECTURAL_ALIGNMENT | Continuation must retain identity; this study does not require redoing the accepted P12 profile or adding a save schema now. |
| WorldId → P12-A supported save implementation | HARD_DEPENDENCY for identity-preserving save claim | The later profile must round-trip the identity once adopted; current P12-A remains WAIT_DEPENDENCY for its existing gates. |
| WorldId → P13 fork | HARD_DEPENDENCY for branch publication | New branch identity and parent provenance accompany independent continuation. |
| P12 continuation + causal historical inputs → P13 reconstruction | HARD_DEPENDENCY | Existing P13 Brief; unaffected by export. |
| P12-B epoch/quiescence → coherent projection | OPTIONAL_REUSE | A bounded independently validated read cut may precede P12-B closure. |
| P12-A or full P12 → projection surface | NO_DEPENDENCY | Projection is read-only facts, not hydration or continuation. |
| WorldId + coherent Faction read + Faction owner → first producer | HARD_DEPENDENCY | Valid document identity and factual source are needed; producer must validate chosen external subset. |
| P12 capture / P19 loader → first producer | NO_DEPENDENCY | Neither owns external interoperability. |
| P13 mechanics → current-world projection | NO_DEPENDENCY | Historical projection is a later consumer and would need P13. |
| P19 extension API → future *mod-defined* projection registration | HARD_DEPENDENCY when that consumer is scheduled | Actual mod-defined contributors require a reviewed public extension contract; basic official projection does not. |

## 16. Roadmap and phase placement

**NEW PROPOSAL:** World identity is cross-phase architecture with a small future implementation boundary at successful world composition and continuation admission; align P9-like genesis, P12 save and P13 fork consumers without reopening closed P9. The read-only factual surface is a future bounded capability checkpoint after identity semantics approval, not a new numbered phase and not a P12 subfeature. The first World Exchange producer/adapter is a later bounded integration checkpoint after WorldId plus one coherent entity port; it belongs to no existing persistence/mod phase by implication. Future projection extensibility is deferred alignment with P19 and real consumers. All four are **DESIGN PROPOSED**, not `READY_FOR_IMPLEMENTATION`; checkpoint IDs, code ownership and validation need reviewed technical design before scheduling.

**Impact classification:** closed P9-A/B and promoted P18 capabilities remain valid in their recorded scope; their use of seed/provenance or injected `worldId` is not retroactively relabelled. Current P12-B passive census and admission candidates are **UPSTREAM_IRRELEVANT** to the proposed reader implementation and need no code revalidation for this study. P12-C identity/provenance design and P12-A's eventual supported-profile save contract need **REVALIDATE** against WorldId if the semantic proposal is promoted; neither is implementation-ready now. P13 entry design must take the branch rule and P18 namespace question into account. P19 remains deferred. No State is rewritten by a proposed architecture choice.

## 17. Rejected alternatives

**NEW PROPOSAL rejects:** seed or genesis fingerprint as WorldId (same input may produce distinct worlds); save path/copy as identity (storage is not causality); runtime instance as identity (load changes process); one WorldId silently shared by divergent continuations (consumer collision); automatic new WorldId on every load (breaks continuity); P12 image or diagnostic snapshot as public factual DTO; direct Store exposure; one `SimulationWorldExportDto`; World Exchange TypeScript models in Simulation; implicit Knowledge-to-Truth mapping; mandatory P19 loader; mandatory full P12 before read-only export.

## 18. Open questions

1. **Product/architecture gate:** confirm that WorldId names a causal branch and P13 fork publishes a new WorldId with origin provenance. An alternative lineage-shared WorldId plus BranchId would need a two-part external identity and different P18 namespace rules. This study recommends the single branch WorldId.
2. **Technical design:** allocation/compatibility for old worlds and fixtures without a WorldId; durable representation and collision checks; identity propagation through existing P18 namespaces and pending receipts at P13 fork.
3. **Product/technical design:** how a copied save announces an independent continuation before the first divergent mutation, including headless workflows. The semantic rule is explicit even though the trigger/UI is open.
4. **External contract alignment:** confirm which Faction fields/relationships can be omitted while producing a valid World Exchange document. Do not change Simulation's domain to satisfy a display field.

## 19. Implementation prerequisites

**NEW PROPOSAL:** accept identity semantics at a human architecture gate; review a bounded technical contract for creation/publication, same-branch retention and fork namespace compatibility; establish a read session with explicit consistency and owner coverage for the chosen slice; review immutable Faction port fields and omission semantics; test nonempty faction/affiliation truth, mutation during read rejection or stable cut, deterministic order, world identity stability across same-branch continuation and distinct identity across fork/copy-divergence when those consumers are implemented. Producer work additionally requires the external contract's current valid-document rules, but no Simulation dependency on its TypeScript package. P12 tests remain specific to save capabilities; no runtime tests are required for this study.

## 20. Explicit non-goals

No WorldId/runtime code, exporter, World Exchange types or dependency, `.world.json` serialization, REST/socket/IPC/server/live sync, import/writeback, P19 loader, universal projection, P12 save schema, P13 replay algorithm, phase closure or runtime canonical promotion. This document is an architecture candidate only.

## Architecture review record

**Method:** adversarial second pass against the baseline architecture, P9/P12/P13/P19 Briefs/States and the cited code, after drafting. **Result:** no unresolved blocking contradiction in the candidate. Specifically checked seed/fingerprint and file identity separation; P13 branch provenance and inherited P18 identifiers; factual surface versus P12 capture and diagnostics; bounded readers rather than a giant DTO or direct Store; Unity-free contract where available; External adapter direction; optional display names; Knowledge separation; absence of P19/full-P12 execution locks. **Remaining gate:** branch identity semantics and copy-divergence trigger need explicit architecture/product acceptance before an implementation contract is approved. This second pass is self-review and does not claim an independent second author; an independent reviewer must inspect the exact candidate tip before canonical promotion.
