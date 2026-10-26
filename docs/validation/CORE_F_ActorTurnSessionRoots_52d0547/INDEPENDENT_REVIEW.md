# CORE-F actor-turn session roots — independent exact-tip review

## Verdict

**PASS — bounded candidate review.** Candidate `52d0547bf602a4993a69ed3700781a143d47e72b`, tree `d571988a54a5b555903ed77f0fc082d4f4fa5682`, based on `2f520558b8a3a61d868052070c4ac223dce231bc`. It is published on `codex/core/CORE-F-runtime-app-session-boundary-20261025`. Current Core canonical is `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

The independent reviewer inspected the complete 13-file diff and the current CORE-F session/runtime contract. No actionable code or test-integrity findings remain.

## Reviewed behavior

- The execution state retains one actor-turn Core root bundle bound to its exact owner graph, runtime roots, `PersonStore`, and `ActorChoiceStore`.
- When selected membership changes, the graph, roster, and matching roots are staged together and replaced atomically. Invalid replacement preserves the prior graph, roster, and roots.
- Graph-backed action choice derives eligibility and current-city truth from Core state. Unity receives a projection-only boundary; the legacy graphless provider path remains available for compatibility.
- Candidate ordering, random-stream keys, action-slot/decision-record ordering, and P12/restore implementation are unchanged by this delta.
- Tests exercise root identity/reuse, valid and rejected roster rebind, rejection before projection when Merchant is disabled, and restore continuation after dynamic NPC membership changes.

## Validation evidence reviewed

| Gate | Durable run | Result |
| --- | --- | ---: |
| CoreValidation cold | `4686b448e77c40f7b9cb8c6aa7800d23` | 820/820 |
| CoreValidation warm | `4686b448e77c40f7b9cb8c6aa7800d23` | 820/820 |
| Unity Runtime Admission | `4dead23c47d1488c94f11163e357f21f` | 223/223 |
| Unity Bootstrap Composition | `841720198e734f60831db28a0fb36d4a` | 39/39 |
| Unity dynamic-roster restore continuation | `75f1dd1817fe4f2baa1e76611da88afb` | 1/1 |
| `git diff --check` | candidate against base | PASS |

The Core manifest reports stable inputs and lists candidate modifications as working-tree inputs over its recorded base commit/tree. All 462 listed input hashes match the published candidate checkout. Each Unity manifest reports a stable, identical before/after input snapshot; all 1,928 listed input hashes match the candidate checkout, and changed production-source hashes match. Thus the executed content matches the published source tree.

**Evidence limitation:** these manifests are content-bound but do not record `52d0547` and its tree as their top-level source commit/tree: Core records base `2f520558` plus working-tree changes, and Unity records input snapshots. Two unrelated pre-existing `ProjectSettings` edits were present in the validation checkout and are not part of this candidate. No Unity `Library` independence claim is made.

## Scope boundary

This review does not establish the Core-owned active `SimulationRuntime`, `DailyV1Application.TryStart`, detached P12 persistence/restoration, fixed .NET host, or separate-process T/T+1/T+2 parity. CORE-F remains incomplete; no Core canonical promotion is claimed.
