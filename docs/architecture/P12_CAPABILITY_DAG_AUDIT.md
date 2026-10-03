# Phase 12 capability DAG audit — 2026-10-03

**Status:** architecture/roadmap candidate; no checkpoint implementation or canonical promotion. **Architecture base:** `451340c56e9b676bf6ea43412bcb856b9ccde3de`. Delivery evidence was refreshed from `origin/codex/phase12/canonical` at `54fc23b89cb13598dd184a2b5b6bacf9dc23b0a7`, the owning Phase States, and Simulation-External `main` at `0ce8403ba05f778db6850f566a974a4c56cf4edb`. The phase branches and this architecture branch are separate; planning status must not be read as integration into one executable tree.

## Verdict and why P12 is large

**PARTIAL_PARALLELIZATION_APPROVED** as a dependency policy. No new numbered-phase implementation checkpoint becomes ready merely through this audit. P12-B remains incomplete; P12-C–G and P12-A retain their accepted dependency chain. P12 is large because a truthful completed-boundary capture must know the live selected profile, every included authoritative owner, every committed writer and invalidation, quiescence, deterministic roots, exact exports, private staged hydration, graph validity, and continuation parity. These are cross-cutting obligations of the supported profile, not evidence that unrelated domain construction must wait for save/load. P12-B's passive witnesses and bounded operation hooks do not yet prove capture eligibility or complete owner coverage.

The accepted `UnityBootstrap-Daily-v1` profile is a versioned, selected Unity bootstrap configuration, compatible build/current-host numeric behavior, and successfully completed daily boundaries. P12 closure still requires complete exact state and deterministic continuation for **every owner actually included in that admitted profile**. A newly delivered capability does not silently expand this profile or P12-A. If newly configured or composed in the selected profile, its authoritative state must be included exactly or admission must reject; no owner may be hidden behind a default, empty array, diagnostic snapshot, or an unsupported label. A later profile/version may integrate additional capability with its own exact continuation adapter and compatibility rules. This is a scope boundary, not a partial-save exception.

## Dependency classification

Here `specific` means a named state/continuation capability for the *chosen supported world/profile*, not a generic promise that P12 will eventually save it. No numbered phase has an unconditional hard dependency on the administrative `P12 CLOSED` marker. Actual P13 fork delivery has the strongest specific dependency: complete, validated continuation for the forked world/profile and recoverable causal history. For the first accepted daily profile, that entails P12's relevant B–G/A capability and parity evidence; a closure label alone is insufficient.

| Phase | Relationship to P12 implementation | Current actionable boundary |
|---|---|---|
| P10 Local Generation | Independent for pre-start authoring; later saved generated profiles consume profile-specific continuation | P10-A promoted; further content scope is `READY_FOR_PRODUCT_SCOPE_DECISION`, no approved new checkpoint. |
| P12 Save | Own B→C→D/E→F→G→A chain | P12-B active/incomplete; no capture eligibility, exact export/hydration or supported save yet. |
| P13 History/Fork | Design independent; fork implementation blocked by specific continuation and causal-history capabilities | Retention/checkpoint and causal-input model can be designed now. An authoritative fork claim waits for exact selected-world continuation, historical initial state and mutations/inputs, compatible execution, identity/provenance, and validation at each simulated boundary. |
| P14 Material Flow | Local and later bounded domain slices are persistence-aware, not blocked | P14-A promoted. New source/transport/crew scope requires product and checkpoint decisions; future save profile includes its owners only when admitted. |
| P15 Construction/Founding | Persistence-aware, not blocked by P12 or P13 implementation | First consumer, founder authority, atomic spatial/domain effects and product scope remain `READY_FOR_PRODUCT_SCOPE_DECISION`; a reviewed bounded checkpoint could then implement against promoted spatial/domain capabilities. |
| P16 Military Movement/Logistics | Persistence-aware, not blocked by P12 closure | P7/P8/P14 and, for duration, P18 are actual capability edges. First operational/logistics/command scope remains `READY_FOR_PRODUCT_SCOPE_DECISION`; no approved implementation checkpoint yet. |
| P17 Strategic War | Persistence-aware, not P12-blocked; presently deferred on domain/product grounds | P16 operational facts, territorial/political authority, goals, pressure, control, occupation and peace semantics are unresolved. P12 does not explain this deferral. |
| P18 Intraday Execution | Independent bounded delivery; specific later save/fork profile integration | Closed within approved A–D scope. A future supported intraday profile must carry temporal state and causal ordering; daily P12 does not claim it. |
| P19 Code Mods | Public-surface architecture can advance from real consumers; loader has its own scope gate; durable mod state needs specific continuation integration | No loader checkpoint approved. Mod-owned state/retrofit needs compatible code/content, exact state and causal inputs before saved-mod-world claims; no blanket P12 prerequisite for stateless or new-world-only extensions. |
| P20 Shared Activities | Independent bounded delivery; specific later save/fork profile integration | P20-A synthetic operation promoted; broader consumers need separate scope. P12 daily profile excludes shared-activity state. |

WI-A, FR-B, FR-C and WX-D are cross-cutting promoted bounded capabilities/integrations, not P12 save checkpoints. They were integrated or handed off through the active P12 canonical branch with serial runtime hotspot review. WX-D produces the bounded World Exchange v2 Faction artifact; Simulation-External v2's required `collectionCoverage` contract distinguishes included, known-empty, unsupported and not-included collections. The producer's coherent factual read and WorldId do not prove P12 capture or continuation. P12-C and future P12-A must preserve WorldId on same-continuation load; P13 must mint a new WorldId with source/boundary provenance.

## Foundation milestone decision

Do **not** add `PHASE 12 FOUNDATION READY` as a global P12 checkpoint or make P12-B+C an unlock for P15/P16. B is profile-specific capture admission and C is profile-specific identity/deterministic-root export; neither is logically required to design a new domain owner, and neither by itself proves future exact hydration or historical reconstruction. The existing architecture plus a per-checkpoint **continuation-aware entry gate** provides the stable foundation. This is a design/review requirement, not a claim that continuation is implemented. A later consumer may name an actual promoted P12 capability as a hard dependency only where it uses it. Preserve `P12 CLOSED` for the full accepted profile guarantee.

For each new authoritative owner/checkpoint, the Brief and reviewed design must identify stable semantic IDs; one owner and no duplicate truth; the logical commit boundary and deterministic causally relevant order; retained versus derived state; ID-based relationships; effective configuration/content and random context; external input payload, authority, boundary and ordering; an exact semantic-state export seam; private staged hydration and pre-publication relationship validation seams; and which caches can be rebuilt. Record which future save profile could admit the owner and how unsupported profiles reject it. This does **not** require implementing P12 DTOs, storage or hydration code inside the domain checkpoint. Where a mutation is simulated history, retain or deterministically reproduce the causal inputs and effects needed for P13, including creation, change and destruction at their actual boundaries. A late adapter cannot recover information the runtime discarded.

## Capability DAG and scheduling

```text
P8/P9 spatial and genesis capabilities ──→ P10-A (promoted)
P7 + relevant P8 + relevant P14 ────────→ P16 scoped design/implementation
relevant P8 + domain owner (+ P14 if cost) → P15 scoped design/implementation
relevant P18 ──→ timed P14/P15/P16 and P20 consumers

canonical semantic contracts + continuation-aware entry gate
  ├─→ new bounded authoritative domain checkpoints (after their own scope/design gates)
  ├─→ P13 causal-input and retention design
  └─→ P19 public-surface design for selected real consumers

P12-B → P12-C → P12-D/E → P12-F → P12-G → P12-A parity → P12 CLOSED
complete continuation for chosen world/profile + recoverable historical
initial state, mutations and causal inputs + compatible execution/identity
  → P13 authoritative reconstruction → independent fork continuation

WI-A + FR-B → FR-C + External v2 coverage → WX-D (bounded producer; promoted)
```

P13 strategy and causal-input design can run `PARALLEL SAFE` as documents. P15/P16 scope and technical design can run `PARALLEL WITH ISOLATION` after the product choices; a candidate implementation then needs its own reviewed design and promoted upstream capabilities. They are not implementation-ready today. P10/P14/P20 follow-ons and P19 loader require their own bounded scope/checkpoint decisions. P17 integrated implementation remains `MUST WAIT` for domain/product prerequisites. P12-B continues in its current owner window. Shared `SimulationRuntime`, composition, spatial authority, force/material stores, diagnostics and future save adapters require explicit serial integration even when isolated candidate development is safe. Do not edit the active P12-B worktree from this architecture track.

The Master recalculates readiness from owning States and code after each promotion, never from this record alone. Existing promoted P10-A/P14-A/P18/P20-A/WI-A/FR-B/FR-C/WX-D scopes remain valid; their optional future save integration is not a retroactive implementation defect. Active P12-B candidates need no broad revalidation from this dependency clarification, but any candidate touching newly named ownership, profile admission or reconstruction seams gets a targeted impact review before promotion. No P13, P15, P16, P17 or P19 implementation authorization is created here.
