# Simulation Core extraction state

This file tracks unnumbered Core infrastructure checkpoints. It does not
change numbered Phase State or imply Phase readiness.

Architecture approval: `32358d2a0faf511e098c4f295a5132a4bfb8cfb2`.
Production base for CORE-A: `codex/phase12/canonical` at
`4cc31073f0ec6ba5c718084e4cebd6e914931d5d`.

## CORE-A — Calendar Shared Assembly

**Status:** implementation committed; implementation review and canonical
promotion remain pending. The required durable original .NET TRX bundle is
not yet available because the reports contain machine-local publication
metadata; no report was edited or published. See
[`CORE-A validation record`](validation/COREA_CalendarSharedAssembly/VALIDATION.md).

- Candidate branch: `codex/core/CORE-A-calendar`.
- Implementation commit: `e702cdf196703ead98d8d37ff818bd1c0d0219e4`.
- Candidate tree: `4de0dd2dfd557f2112e395f7c69a52e0eff3b0dc`.
- Scope: one engine-free `Simulation.Core` assembly for the existing calendar,
  date and age capability; Unity and .NET compile the same source.
- Promotion target: `codex/core/canonical`, to be initialized from current
  reviewed production state under the approved CORE-A handoff. It has not been
  created or advanced by this candidate.
- Numbered Phase canonicals were not changed.

The implementation and test source are complete within the approved scope.
Unity and .NET executions passed; the evidence record distinguishes those test
results from publication readiness. Canonical delivery remains blocked until
the complete original .NET reports are safely retained in the durable reviewed
bundle required by the host evidence contract, followed by independent exact-tip
review and normal promotion preflight.

## CORE-B — Deterministic and Temporal primitives

Technical design candidate `d20c2352887184ccc964d7f9586407d00a727d32` received
independent PASS. Review record `96a7df8c2ebad6cf5ce3bfe2bde140f98c6e46dc`
is pushed. CORE-B remains design-reviewed, not implementation-ready, until
CORE-A is promoted and its production-baseline refresh is reviewed.

## CORE-C — Market numerical capability

Technical design candidate `30e260d7be19f1daa288ec3a5672b0957dabc874` needs
changes. The local review record is
`637a7ee3f79de36c0d2c4c5c631be020bbbec26d`; its push failed because GitHub
HTTPS credentials were unavailable. Findings: duplicate price authority,
incomplete mutation-admission callback order, and underspecified cross-assembly
API/adapter boundaries. No CORE-C implementation is authorized by that review.

## Readiness boundary

CORE-A remains the first implementation checkpoint. CORE-B implementation waits
for CORE-A promotion and revalidation. CORE-C waits for its design corrections,
independent PASS, and then its documented dependencies. Full world hosting,
equivalent .NET P12 continuation, P13 and broader simulation hosting are not
claimed by CORE-A.
