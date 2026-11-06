# CORE-F TravelParty Core Baseline — Review Record

## Scope

This record covers the bounded selected TravelParty advance change at candidate `36978765b77aa95f28152b8fe966a02f64e2e5d8`, tree `0a02bfe1b85aa2799cc85b708aeed9bade3ff70a`, based on `8013345de152ed8455e02d765169a776014a9e2b`. Core canonical at review time was `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

Changed files:

- `Packages/com.simulation.core/Runtime/Application/DailyV1ExecutionBoundaryState.cs`
- `tests/Simulation.Core.Tests/DomainEventAuthorityCoreTests.cs`

The Core session now captures and validates the exact `TravelPartyStore` cardinality/revision baseline for its selected advance operation and requires one active-party removal per recorded completion. The Unity host continues to validate the complete owner vector, including selected NPC/city rows and compatibility-roster owners, and receives the committed section invalidation. This slice does not remove the host census protocol.

## Independent source review

**PASS — bounded source review.** The independent reviewer examined the exact candidate diff and confirmed the production baseline checks, one-completion revision/cardinality rule, and fail-closed handling. The review-requested regression coverage now includes unrecorded store drift and a notification that attempts to represent multiple party completions.

The source review is not a validation-complete or promotion verdict.

## Validation evidence

- Official CoreValidation: cold **1028/1028 PASS**, warm **1028/1028 PASS**.
- CoreValidation private evidence run ID: `3109e8d354e144c3a938877097281375`.
- `git diff --check`: **PASS**.
- Unity EditMode: **NOT RUN**. The approved evidence wrapper rejected execution before launching Unity because the installed PowerShell is 7.6.9 and the wrapper requires 7.6.5. No Unity test result or XML is claimed for this candidate.

The raw CoreValidation reports remain in the existing owner-only E: evidence store and are not copied into the repository by this record.

## Status and limits

This remains an unpromoted partial CORE-F candidate. The full P12 owner vector, mixed-roster census, runtime composition, `DailyV1Application.TryStart`, detached .NET host, and separate-process continuation remain governed by their existing contracts and are not completed by this change. Unity validation is still required before this integration slice can be accepted for promotion.
