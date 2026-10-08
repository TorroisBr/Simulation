# P12-D City/NPC receipt-owner exact-zero witness design — exact-content review

**Verdict: PASS — docs-only technical design review.** The candidate was independently reviewed at its exact remote tip. No candidate files were edited. No Unity tests were run because the candidate changes only a design document; `git diff --check` passed.

## Exact identity and baseline

- Candidate branch: `codex/phase12/P12DCityNpcReceiptZeroWitnessDesign`
- Candidate remote tip: `5b10a58a680af9adcf56a585f6f2ac50f8e6b172`
- Candidate tree: `f86fba388509f658c4b41a6c31cd4f7762fc2bef`
- Candidate document: `docs/design/PHASE12_D_CITY_NPC_RECEIPT_OWNER_ZERO_WITNESS_DESIGN.md`
- Candidate document blob: `f87b4787617cbfc537854ab56ac92fb59920290a`
- Exact P12 canonical base: `04e7c3f49a7c9690ebc091fcfc50f05ef67009d6`
- Architecture baseline: `47eff220c7ce00f6e7c759bdc2b76780bb46f628`
- The refreshed remote P12 canonical still equals the exact base; the refreshed candidate branch equals the reviewed candidate tip. The candidate descends from the base, and the complete base-to-candidate diff adds only the named design document.
- Prior exact-content review: `b587ba3a27d4104f8bc40e99ffdcede4000e6d98`, reviewing `22d1befe971f74f6551b307f704abef1975bd861`.

## Review findings

No unresolved design findings.

The two prior findings are fixed. The design now places its proposed focused test at `Assets/_Project/Tests/EditMode/Editor/P12DCityNpcReceiptOwnerCensusTests.cs`, matching the current EditMode assembly path. It also specifies committed roster add/remove reconciliation in the existing `ContinuationCensusProtocol.TryReconcileSpatialKnowledgeRosterAndNotifyCommittedMutations` transaction rather than relying on the live `npcRuntimeSnapshot` view alone.

The revised RuntimeId case matches the selected Daily-v1 registration path: unregister removes the NPC from the world roster while `RuntimeIdentityRegistry` retains its RuntimeId-to-object binding; the same object may be registered again, while a different object reusing that RuntimeId is rejected as `DuplicateRuntimeId`. The required add/remove and re-add tests reflect this source-backed behavior.

The proposal preserves owner-thread and active-operation checks, stages provider families and expected/registered maps before publication, folds family changes into the existing single `anySectionChanged` epoch decision, checks epoch capacity before publication, and fault-closes on failed reconciliation. It does not add a second epoch increment or receipt-specific notification. The existing protocol captures and validates all registered owner sections at one quiescent boundary; the proposed rows are bound to their exact embedded owner instances and parent NPCs and are included in that same vector.

The raw owner reads avoid both lazy `ReceiptList` and lazy `NpcRuntime` accessors. The design explicitly requires the provider to accept only `(cardinality=0, revision=0)` and fail closed on malformed or populated state. Since generic `Required` sections permit nonzero cardinality, implementation must realize this stated provider rule by rejecting/throwing for every nonzero count or revision; it must not rely on the generic role alone to enforce exact zero. This is an implementation constraint already contained in §§2–4, not a design blocker.

The capture/staging boundary remains bounded: receipt contents and P18 occurrence/operation history are never exported, staged, hydrated, or replayed. The design adds no P18 writer admission, P12-B operation semantics, P12-A readiness, P12-G publication, P13 behavior, P10 topology, or Phase closure. A profile that later composes either P18-D writer needs a separate causal-history contract rather than relaxation of this zero-only witness.

## Demonstrability assessment

Section 9’s `NOT_MEANINGFUL_FOR_THIS_CHECKPOINT` classification matches the architecture’s demonstrability policy. This checkpoint adds passive fail-closed evidence and no standalone user-visible world behavior. It identifies the smallest automated acceptance/rejection proof, ties it to the existing Daily-v1 runtime/capture boundary, and explains why a human scenario, non-Unity host scenario, or Lab read-surface addition would not demonstrate this infrastructure capability. The automated validation gates remain in force.

## Readiness boundary

The reviewed technical design is ready to guide implementation of **only** the two exact-zero embedded receipt-owner witness families and their committed NPC-roster reconciliation. This review does not validate implementation, approve promotion, close P12-D/Phase 12, or imply P12-A/P13 readiness. Required focused, full EditMode, official Smoke, and `git diff --check` validation remain implementation gates as listed in §7.
