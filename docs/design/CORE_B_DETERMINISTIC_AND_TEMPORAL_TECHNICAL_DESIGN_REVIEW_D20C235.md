# CORE-B deterministic and temporal technical-design review

**Verdict: PASS — bounded design is technically reviewable.** This is a design
review only. It does not authorize implementation or promote a capability.

## Exact reviewed refs

- Candidate: `codex/core/CORE-B-technical-design` at
  `d20c2352887184ccc964d7f9586407d00a727d32`.
- Production audit baseline: `codex/phase12/canonical` at
  `4cc31073f0ec6ba5c718084e4cebd6e914931d5d`.
- Architecture authority: `codex/architecture/simulation-core-extraction` at
  `32358d2a0faf511e098c4f295a5132a4bfb8cfb2`.
- Candidate review branch began at the candidate tip; the candidate's diff from
  the production audit baseline adds only
  `docs/design/CORE_B_DETERMINISTIC_AND_TEMPORAL_TECHNICAL_DESIGN.md`.
- The remote candidate and architecture refs matched these exact SHAs during
  review. The P12 remote canonical matched its stated baseline.

## Review findings

The design stays within the architecture's CORE-B boundary: it extracts the
existing deterministic random implementation, stable length-prefixed key
encoding, and complete logical-time/continuation capability into the one
shared Core source tree. It does not move `SimulationRuntime`, temporal owners,
P18 consumers, persistence codecs, or a second runtime. The design retains the
explicit CORE-A prerequisite and correctly states that implementation must
start from the refreshed `codex/core/canonical`, not from the architecture
branch's stale executable tree.

The proposed compiled closure is supported by source inspection at the
production baseline. `DeterministicRandom.cs` uses BCL types. The complete
`LogicalTimeline.cs` references `SimulationCalendar`, `SimulationDate`,
`SpatialStableKey`, and BCL collections/culture APIs; it has no Unity or host
references. `SpatialStableKey`'s encoding body is a pure length-prefixed
`StringBuilder`/invariant-culture helper co-located with spatial types. Moving
only that helper to the Core encoder while leaving `OptionKey` and the
compatibility wrapper in Unity avoids importing spatial authority. CORE-A
provides the calendar/date dependency. The design correctly requires actual
allowlist compilation to prove closure and stop if another dependency appears.

The RNG description matches the inspected source, including the current
`long.MaxValue` counter progression: the first stream draw consumes that index
and advances the counter to `long.MinValue`; the next draw consumes the
negative index, advances once more, then is rejected by `NextUnit` validation.
The design appropriately preserves this behavior, requires the Unity and SDK
overflow settings to be compared, and calls for the rollover test in Unity
without widening `ConsumeIndex` solely for tests. The shared provider boundary
checks cover direct `long.MaxValue` acceptance and negative-index rejection on
both hosts. FNV-1a constants, byte order, conversion sequence, metadata, and
staged snapshot failure behavior are explicitly protected.

The proposed assembly bridge is narrow and justified by current cross-assembly
callers: the raw `DeterministicRandomRootSnapshot` constructor and
`TryCreateStagedFromSnapshot` become public, with no friend-assembly grant or
second type implementation. The stable-key API is likewise an assembly
composition utility rather than a declared mod surface. The design preserves
the `SpatialStableKey` Unity adapter and makes the Core encoder the one moved
implementation.

The test conservation map is concrete and consistent with the audited source:
the logical-time fixture has 38 NUnit attributes across 33 methods and uses
only System, collections/LINQ, and NUnit; the selected random root snapshot
cases are seven parameter-expanded cases; the Unity-only NPC action-order test
remains in Unity. The design requires unchanged case identity, shared
portable-source execution on both hosts, consumer/P12 regressions, full selected
profile compatibility evidence, and retrievable per-case evidence. These are
future validation gates, not claimed results.

No unresolved product or canonical architecture decision is introduced. The
document distinguishes code extraction from equivalent-host and save-profile
compatibility milestones and keeps P12 same-build/current-host and prior-build
fail-closed behavior as explicit validation requirements.

## Readiness and gates

This PASS means the bounded technical design is reviewed. CORE-B remains
`WAIT_DEPENDENCY` until CORE-A is promoted, and the implementation worker must
refresh the current Core canonical and revalidate the exact compile closure
before coding. Required Unity/.NET parity, rollover, P12 compatibility,
full-suite, Smoke, evidence, independent implementation review, and integration
gates remain outstanding. No Unity tests or implementation validation were run
for this documentation review.
