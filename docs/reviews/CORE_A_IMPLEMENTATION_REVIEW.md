# CORE-A Calendar Shared Assembly — Independent Implementation Review

**Verdict: NEEDS_CHANGES**

## Review identity and exact refs

- Reviewer: independent reviewer, separate from the implementation candidate author.
- Candidate branch: `codex/core/CORE-A-calendar`.
- Exact reviewed candidate: `78011db2cbbe36981e30f9a58b2f14a3756d6e5e`.
- Candidate tree: `c62cd64e3e01f88b287b6b12a10810eaad36dc9a`.
- Exact base: P12 canonical `4cc31073f0ec6ba5c718084e4cebd6e914931d5d`.
- Architecture: `32358d2a0faf511e098c4f295a5132a4bfb8cfb2`.
- The candidate was reviewed at its fetched remote SHA. No candidate files were changed.

## Scope and technical review

The calendar extraction is within the CORE-A handoff. `CalendarDefinition`, `SimulationCalendar` and its age calculator were moved into the embedded `com.simulation.core` package; `SimulationDate` was extracted without changing its behavior; and `SimulationTime` remains the Unity-side advance dispatcher. The Unity package and SDK project compile the same source files without engine references or a second implementation. The reviewed changes preserve the existing calendar/profile boundary and do not expand P12 save semantics or gameplay scope.

The source move and extraction preserve the reviewed behavior. The validation record covers the calendar/parity/authoring cases, P12 compatibility and capture/restore, Daily-v1 admission/continuation, full Unity EditMode and Smoke suites, and .NET cold/warm tests. The Unity report archive was checked against its recorded digest and contains 3,080 passed cases, including the recorded focused and Smoke cases. All 27 .NET manifest input hashes and lengths match the candidate files; the recorded cold and warm runs each report 45/45 passing cases. `git diff --check` passes.

## Required correction

`Tools/CoreValidation/CoreValidation.psm1` verifies the TRX root name and required sections, but does not require a nonempty `TestRun/@id` or validate that the run-level `ResultSummary/@outcome` is coherent with its counters and case outcomes. The parser returns the root `id`, but `Tools/CoreValidation/Invoke-CoreValidation.ps1` does not persist that identity or the summary outcome in the public validation manifest. `Tools/CoreValidation/tests/Invoke-CoreValidationTests.ps1` has no synthetic cases for a missing run ID or contradictory summary outcome. This leaves the runner able to report a passing gate without binding it to a valid run identity and coherent run-level summary.

Before accepting CORE-A, require and retain the TestRun identity, validate the supported ResultSummary outcome against the parsed cases/counters, and add synthetic tests for absent identity and incoherent summary. Keep the actual TRX reports private until their contents pass the required privacy review.

## Separate evidence promotion blocker

The original .NET cold and warm TRX reports are not in this candidate. The validation record supplies their sizes and hashes, but states that the reports remain privately retained; the durable original-TRX bundle and its path metadata are unresolved. The execution handoff requires that original bundle and privacy disposition, so this is an actionable evidence-only promotion blocker in addition to the code correction above. Hashes and a normalized/public manifest do not substitute for the original reports. The machine-local host and absolute worktree paths noted for those reports must be inspected before any publication; this review did not access or modify the separate untracked local archive.

## Applicability and gate

This review applies only to CORE-A candidate `78011db2cbbe36981e30f9a58b2f14a3756d6e5e` against the refs above. CORE-A remains the prerequisite for CORE-B and CORE-C; neither implementation readiness nor canonical promotion follows from this review. Re-review the corrected exact tip and resolve the original-report evidence requirement before promoting CORE-A.
