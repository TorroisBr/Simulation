# Multi-phase execution model

This document defines the scheduling and promotion process, not simulation semantics, a Master Orchestrator prompt, or permission to execute. `SIMULATION_ARCHITECTURE.md` is the semantic authority; `ROADMAP.md` and Phase Briefs are subordinate planning; Phase States and canonical code record delivery. Skills apply these rules to a particular workflow without owning phase scope.

A Phase Brief names objective/closure, approved checkpoint IDs and dependencies, gates, exclusions, reconstruction-sensitive state/inputs, hotspots, downstream unlocks, readiness and deferred questions. It does not carry execution progress. A future Phase without a `PHASE*_STATE.md` has no Phase-specific promotion record yet; do not synthesize delivered status from its Brief or a candidate branch.

Unity Scenes, prefabs, inspectors and general-purpose configuration assets are
presentation/bootstrap/test harnesses by default, as defined in architecture
§2. Treat them as profile-contract inputs only where that role was explicitly
approved and promoted. Their current contents do not amend Phase scope,
domain semantics, persistence-profile membership or supported owner inventory.
Use the explicit composition contract and its validation evidence to determine
scope; check the live composed owners against it. A development edit that adds
unsupported authoritative state must trigger the applicable admission rejection
or a separately reviewed contract change. Prefer dedicated profile fixtures or
configuration when useful, without requiring a retrofit of accepted profiles.

## Two readiness layers

Architectural readiness does not imply implementation readiness:

```text
architecture and checkpoint scope accepted
→ bounded technical design, when needed
→ independent technical review
→ READY_FOR_IMPLEMENTATION
→ isolated implementation
```

Technical design may settle concrete ownership, affected components/interfaces, migration and integration sequence, and validation within accepted semantics. It may not choose a durable semantic or product answer. Such an issue blocks that track as `BLOCKED_ARCHITECTURE` or `BLOCKED_PRODUCT_DECISION`.

For a substantial checkpoint, keep a bounded design artifact in its named design/candidate branch or a reviewed checkpoint execution record. A small checkpoint may use a recorded reviewed plan instead of a permanent file. Either form must be recoverable through a known Git ref or durable record, not only an untracked local file. The Master must be able to identify checkpoint ID, canonical base SHA, reviewed scope/technical boundary, reviewer/verdict, and whether architecture assumptions are still valid. Technical plans are evidence, not a second constitution; their approval does not promote code.

Planning readiness vocabulary for a Phase or checkpoint entry:

| Label | Meaning |
|---|---|
| `DEFERRED` | No current entry work should be scheduled. |
| `WAIT_DEPENDENCY` | A named contract, capability, or gate is missing. |
| `ENTRY_ARCHITECTURE_READY` | Entry questions may be resolved; no implementation contract is implied. |
| `READY_FOR_PRODUCT_SCOPE_DECISION` | Semantic prerequisites permit a bounded slice, but the user must choose its product scope before checkpoint design or implementation readiness is claimed. This is not an implementation checkpoint. |
| `READY_FOR_TECHNICAL_DESIGN` | Architecture, scope, and closure are sufficient for bounded technical design; implementation is not ready. |
| `TECHNICAL_DESIGN_IN_PROGRESS` | Bounded design/review is under way. |
| `READY_FOR_IMPLEMENTATION` | Required technical review and implementation dependencies passed. |
| `IN_PROGRESS` | Authorized implementation is under way. |

These are planning views, not declarations that a whole Phase's capabilities exist. Use the current Phase 8 State for checkpoint delivery/readiness, not a phase-wide label. No future implementation checkpoint is schedulable merely because its entry architecture or Brief can be discussed.

Checkpoint execution states are `PLANNED`, `IN_PROGRESS`, `SUBMITTED`, `VALIDATED_CANDIDATE`, `APPROVED`, and `PROMOTED`. `READY`/`NOT_READY` are derived from current dependencies, gates, technical review, and available isolation rather than stored as independent truth. `CLOSED` is a Phase status. Block reasons (`BLOCKED_DEPENDENCY`, `BLOCKED_ARCHITECTURE`, `BLOCKED_PRODUCT_DECISION`, `BLOCKED_INTEGRATION`) annotate the affected track; a block in one track does not freeze independent work.

## Dependency evaluation

| Dependency | What it can unlock |
|---|---|
| Semantic contract | Once consolidated in canonical `SIMULATION_ARCHITECTURE.md`, downstream architecture or technical design that needs only that contract. An approved Brief cites it and records which checkpoint depends on it; the Brief does not establish domain meaning. |
| Promoted capability | Downstream implementation/integration that needs working upstream behavior, after canonical code and State validation. |
| Integration | Isolated candidate development may proceed; combined promotion waits for the named composition boundary. |
| Architecture gate | Requires an explicit semantic decision and its approved consolidation. |
| Product decision gate | Requires user intent; neither worker nor Architecture Lab supplies it implicitly. |
| Validation dependency | Used only where reliance on upstream requires specific evidence beyond its mere existence; ordinarily validation is a promotion gate. |
| Soft/ordering | Preference, not a readiness blocker. |

Readiness is calculated from current canonical docs/code and explicit dependency IDs, not from historical chat memory or phase-number order. A candidate or unreviewed plan does not satisfy a promoted-capability edge. An explicit isolated dependency on a named candidate may allow preparation, but not implicit downstream canonical unlock.

For the promoted P12 capability DAG, the Master must not treat `P12 open` as a blanket implementation lock on later numbered phases. Apply the per-owner continuation-aware gate and the actual promoted capability edge for each bounded consumer. P12's accepted save profile still closes only with complete validated continuation of every admitted owner, and P13 fork still needs reconstructible causal history. See [the 2026-10-03 Master handoff](architecture/P12_CAPABILITY_DAG_MASTER_HANDOFF.md) for the time-stamped ready set, product decisions and shared hotspots; re-evaluate it against current States and code on each run.

On each run start and after canonical/architecture/roadmap changes: verify branch and local/remote SHA; load current architecture, roadmap, Briefs, States, and relevant code; derive the checkpoint DAG; evaluate gates and technical design; classify READY work; assess semantic and file/hotspot interference; schedule a safe set; repeat after promotion. Stop rather than inventing missing checkpoint contracts. Record the baseline and dependency evidence used for each dispatched track.

## Context, parallelism, and blockers

Give workers the exact base SHA, current `AGENTS.md`, relevant architecture sections, their Brief/State/checkpoint contract, upstream evidence, explicit exclusions, owned hotspots, and applicable workflow Skill. They must reread current architecture when a semantic question arises. Do not paste all historic conversation or assume a long-lived agent remembers it.

`MUST WAIT` means a hard gate/contract/capability or unresolved shared semantic boundary prevents safe work. `PARALLEL WITH ISOLATION` permits separate worktrees with explicit ownership and serial integration. `PARALLEL SAFE` requires both semantic and Git/file independence; read-only investigation often qualifies. Different files do not prove semantic independence. `SimulationRuntime`, daily loop, diagnostics, spatial/travel, Knowledge, command capture, persistence composition, and shared stores merit explicit ownership windows rather than a universal lock manager.

An architecture blocker record identifies checkpoint, base SHA, exact unanswered decision, why current architecture does not answer it, options and consequences, affected invariants/downstream work, safe independent work, and the minimum decision required. Product blockers use the same traceability but ask the user for the missing intent. The affected track stops; independent READY tracks continue. After a decision is approved and consolidated, refresh canonical and recalculate the DAG before resuming.

## Candidate, promotion, and closure

```text
implementation → focused validation → SUBMITTED
→ independent review → VALIDATED_CANDIDATE
→ integration/regression → AUTONOMOUS_BOUNDED_PROMOTION when all criteria pass
→ PROMOTED + State evidence → DAG refresh
```

Worker completion, green tests, candidate existence, and technical-plan approval are not promotion. Review the full diff against its actual base and check semantic architecture, technical design, mutation authority, deterministic and replay/fork implications, stale state, atomicity, scope, tests, and hotspot interference. Before promotion verify ancestry, expected diff, current remote, integration evidence, State accuracy, required tests, `git diff --check`, and absence of unresolved blockers. Do not force-push, silently merge main, or discard user work.

### Autonomous bounded promotion

Routine bounded numbered-phase checkpoint/slice promotion is authorized without another human approval when **all** of these conditions hold:

1. The slice is within an already accepted Phase Brief, canonical architecture baseline, or approved checkpoint decomposition.
2. No unresolved gameplay/product or canonical architecture decision remains.
3. The change has not materially broadened beyond its reviewed contract.
4. Required technical design review has passed where applicable.
5. Independent implementation review passed on the exact proposed code tip/tree.
6. Required focused validation passed.
7. Required full EditMode validation passed.
8. Official Smoke passed where applicable.
9. `git diff --check` passed.
10. Immediately before promotion, refreshed remote canonical and candidate refs confirm clean fast-forward ancestry.
11. The reviewed code tree is unchanged and review/validation artifacts still match it.
12. No newer canonical change invalidates reviewed assumptions; any required revalidation/review passed.
13. Integration is safe, additive, and non-destructive.
14. Unrelated user files and changes remain untouched.

When every condition passes, promote autonomously, record the exact final SHA and evidence in State, push the named canonical branch, verify synchronization, and rebuild the complete dependency DAG. Do not ask for a separate approval for this bounded promotion. If any condition fails, identify and resolve the technical gap; stop only for a genuine human gate or unresolved decision. This rule reduces approval stops, not review, validation, preflight, or scope discipline.

Formal Phase closure separately requires the closure objective and mandatory checkpoints canonical, appropriate regression and independent closure review, known limitations and deferred consumers recorded, and a formal State marker. Human approval is still required for closure and reopening; it is also required for genuine product/canonical architecture decisions, changing an accepted Phase objective, a major new checkpoint not implied by canonical planning, incompatible public/external contracts not already authorized, destructive/irreversible actions, force-pushing or rewriting shared history, or deleting important unintegrated remote history. A state-only closure commit may be appropriate. Closing a phase never requires every future consumer.

After canonical advancement, classify each active candidate with orchestrator/reviewer evidence: `UPSTREAM_IRRELEVANT` (continue, still validate at integration), `REVALIDATE` (rerun affected review/tests), `REINTEGRATE` (refresh composition without discarding work), or `INVALIDATED` (return to design/work). A worker alone cannot declare a semantic change irrelevant. Failed tests should be diagnosed in-track; failed review returns for changes; merge conflicts require inspection of both semantics. Preserve a recoverable candidate record rather than restarting or deleting blindly.

## Demonstrability assessment for future checkpoints

For each new bounded checkpoint design, record a short assessment of: the
behavior a person should be able to observe; the smallest executable scenario;
whether it can run outside Unity; the approved application/read surfaces it
would use; any Unity coupling the attempt exposes; and whether an existing Lab
surface can be extended. Keep human demonstration distinct from automated
correctness/regression validation. Choose exactly one planning classification:

| Classification | Gate |
|---|---|
| `REQUIRED_FOR_CHECKPOINT` | The selected behavior and a small human-executable scenario are jointly part of the reviewed checkpoint closure. Validate both before its code promotion. A text/CLI scenario may suffice. |
| `FOLLOW-UP_DEMONSTRATION` | The domain checkpoint can be reviewed and promoted independently; record a concrete demonstration follow-up and its dependencies. For user-observable behavior, prefer completing it before formal Phase closure when practical. It is not a hidden prerequisite for the domain implementation. |
| `NOT_MEANINGFUL_FOR_THIS_CHECKPOINT` | Explain why the checkpoint is infrastructure, contract, or otherwise lacks a useful standalone human behavior. Keep its automated/review gates. |

Classify against the selected checkpoint scope and current executable
capabilities, not an imagined full product UI. A non-Unity demonstration must
exercise the actual Simulation authority and approved factual/actor-perspective
reads; replaying a parallel Lab model or inspecting a static export alone does
not prove execution independence. If Unity coupling blocks the chosen scenario,
record the exact seam and a bounded follow-up rather than exposing mutable
Stores, copying domain rules, or silently expanding the domain checkpoint.
This policy applies to subsequent design/review work; it neither reopens closed
phases nor interrupts already active candidates. The unnumbered Simulation Lab
foundation remains a separate, technical-design-ready supporting workstream;
implementation awaits reviewed technical scope and a safe scheduling boundary.
When a demonstration exists, the handoff reports `HOW TO TRY IT` alongside
automated validation evidence, with the exact supported profile and scenario.

## Reconstruction-sensitive gate and stop conditions

Each checkpoint introducing authoritative mutable state or external causal input must answer: **what must future reconstruction recover to reproduce this causality?** Consider stable IDs, factual state and commitments, payload/authority/logical boundary/ordering of commands, effective content/configuration and calendar, random context, and causally relevant provenance. Put the declaration in the Brief/checkpoint contract, verify it in technical and candidate review, and record actual delivery in State. This gate does not require early save/replay implementation. Diagnostics, events, and selective History are not primary truth.

For a new authoritative owner, apply architecture §92A before implementation review. The reviewed contract identifies one authority and stable IDs, committed mutation and deterministic causal order, retained versus derived state, ID-based links, exact semantic export and private staged-hydration/validation seams, effective random/configuration context, and captured external inputs. Before domain promotion, show either that the owner cannot enter the selected P12 composition, or that its integration adds a verified fail-closed admission/inventory hook with a negative rejection test. The latter requires serial integration with the active P12-B hotspot while its general admission proof remains incomplete. The domain checkpoint need not implement P12 storage. A later continuation adapter cannot supply historical causality that the checkpoint discarded. Do not treat P12-B/C completion or a proposed `PHASE 12 FOUNDATION READY` marker as a global prerequisite. For supported save/fork claims, require the actual profile-specific continuation and historical capabilities.

After a dependency refresh, schedule independently reviewed bounded domain checkpoints against their real promoted prerequisites, using separate worktrees and serial integration at shared runtime/store hotspots. `READY_FOR_PRODUCT_SCOPE_DECISION` permits preparation and a precise user decision request, not implementation dispatch. P12's accepted profile and own B–G/A chain remain intact; new domain delivery neither silently enlarges it nor bypasses admission rejection for unhandled composed state. See [the P12 capability DAG audit](architecture/P12_CAPABILITY_DAG_AUDIT.md) for the 2026-10-03 phase classification and current candidate impact.

Stop a run when all safe READY work is exhausted; only genuine human/architecture gates remain; an active semantic conflict cannot be isolated; validation cannot be resolved in scope; canonical changed unexpectedly and impact cannot yet be classified; or resource/context limits require a resumable handoff. Do not promise background execution absent an actual scheduled/run mechanism, wait indefinitely inside a worker for a human decision, or start speculative distant phases to keep agents busy.

Roadmap revisions may insert/split/merge phases or checkpoints. Preserve historical State, record new dependency impact, and reclassify candidates. A change to simulation meaning must go through architecture approval; a change to product scope or material roadmap priorities remains human-gated.

## Intraday and extension impact review

The 2026-09-26 approved requirements are consolidated in architecture §§2,
11–12 and 91–92, Roadmap and Phase 18/19 Briefs. Preserve existing phase and
checkpoint IDs. Read `architecture/INTRADAY_EXTENSIBILITY_ALIGNMENT.md` when
refreshing pre-change candidates; do not infer that this documentation change
promotes their code or cancels their delivered daily-profile contracts.

For timed work, inventory the logical instant, pending work/activities,
availability, same-time ordering/sequences and compatible calendar/profile.
For extensible generation/mods, inventory stage/contributor identities and
versions, causal contribution order, authoritative extension state, and explicit
retrofit inputs. A daily timestamp or seed alone cannot capture this causality.
Retain these semantics when introduced; later persistence cannot recover
information discarded by an earlier checkpoint.

Design to current natural semantic seams now. Defer loader/public API mechanics
to P19; no worker may add speculative mod infrastructure or new sleep/theft/needs
gameplay to satisfy P18. Day-based proof fixtures may remain explicitly scoped,
but a worker may not turn their daily cadence into the permanent actor or travel
contract. Domain validation is distinct from adversarial player/mod security.

## Multi-participant activity scope review

Use `architecture/MULTIPARTICIPANT_ACTIVITY_ALIGNMENT.md` for the additional
requirement accepted against canonical `4b6dd1d`. P18 technical review must
distinguish definition, instance identity/lifecycle and participant identity.
An individual proving slice is sufficient for P18, but cannot establish a
permanent one-Activity-to-one-Actor or NPC-owned authority contract. P20 layers
formation, shared execution and coordinated participant commitments on relevant
P18 capabilities; no reverse P18 → waits-for-P20 gate is introduced.

Include formation/agreements, role/count constraints when applicable, participant
IDs, future reservation intervals, scheduled start, lifecycle/context and
participant-specific effects in the causal inventory when supported. Preserve
coherent mutation boundaries and deterministic decisions/ordering. Persistent
Group membership and aggregate military execution are separate contracts.
Single-actor P8/P11 candidates and delivered capabilities are not invalidated
merely by their bounded scope. Review future broad temporal/mod/save claims
for these boundaries; do not dispatch gameplay or invent a workflow engine.

## Validation evidence, reuse and publication

Apply [the evidence policy](operations/VALIDATION_EVIDENCE_POLICY.md) to new
packages. Keep complete retrievable results, source/tree and full effective
validation-input identity, independent review and owning State provenance.
Use one final cumulative package where safe; all existing mandatory focused,
full EditMode, official Smoke, LongRun and demonstration gates remain intact.

Proven docs/metadata-only successors may reuse runs after independent exact-tip
applicability verification; changed source, tests, configuration, toolchain or
scope requires the corresponding validation. Original XML remains run authority.
Use normalized result packaging where the reviewed Tools capability is delivered
at the selected execution ref and supports the report/input scope; otherwise keep
complete original-report evidence under the policy transition rule.

Review actual outgoing objects and archive contents, retain necessary diagnostics
safely, preserve historical promotions and never evade a publication rejection.
This operational policy establishes no new simulation capability, profile member,
Phase closure or routine human approval gate.

## Simulation Core extraction and host-specific validation — 2026-10-13

Apply [the unnumbered extraction contract](architecture/SIMULATION_CORE_EXTRACTION.md)
and [CORE-A handoff](architecture/SIMULATION_CORE_A_MASTER_HANDOFF.md) to this workstream.
Name the execution host, effective profile, complete compiled dependency closure,
source ownership and SDK/CLR/Unity/numeric/build identity in each design/review.
One shared source implementation serves all hosts; adapters own no world rules.
A portable compile is distinct from authoritative parity and from save compatibility.
Documentation/Core-only worktrees need no Unity import. Existing integration,
full EditMode/Smoke and causally required LongRun gates remain; no blanket .NET
substitution is authorized. CORE-A needs both hosts and P12/asset regressions.

Current production P12 has adopted the evidence packager; older future-implementation
wording above describes its earlier state. Verify the selected production ref and
Tools README before using it. It supports its declared Unity NUnit/input boundary,
not arbitrary TRX/SDK roots. CORE-A uses a complete reviewed original-TRX bundle
plus full .NET input manifest and a Unity input sidecar, under evidence policy.
No unsupported normalization or inaccessible hashes-only evidence may pass.

Architecture promotion uses the user's explicit extraction/autonomous-compatible-
promotion authority, independent exact-tip review and refreshed docs-only preflight.
It changes no numbered-phase branch. First code implementation goes through the
normal independent implementation/promotion gates from current production, with
only the reviewed documentation delta imported; never merge the architecture
branch's older executable tree into a newer numbered-phase canonical.

For this user-authorized unnumbered Core workstream, apply the same bounded
implementation/candidate/promotion conditions to `codex/core/canonical`, including
independent exact-tip review, the handoff's two-host gates, safe ancestry and
current input identity. Record delivery in `docs/CORE_STATE.md`, not a new Phase
or parallel world authority. This adds no automatic numbered-phase merge or
formal Phase-closure permission. Architecture remains on its own canonical branch.
