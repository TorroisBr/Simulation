# P12-C Identity Snapshot Candidate — Canonical Revalidation

> Historical revalidation for superseded candidate `531d835f01a9070df42d54291ffde32387fb4358`, based on the 2026-09-29 canonical state. It does not describe the current P12-C composition candidate or P12-B readiness. Current promoted status and dependency state are recorded in [`PHASE12_STATE.md`](PHASE12_STATE.md); current composition evidence is in [`PHASE12_P12C_OWNER_CONTINUATION_COMPOSITION_EVIDENCE.md`](design/PHASE12_P12C_OWNER_CONTINUATION_COMPOSITION_EVIDENCE.md).

## Status

**PRESERVE; REVALIDATE AND REINTEGRATE BEFORE PROMOTION.** The candidate's
allocator and shared record-sequence snapshot work is reusable, but it does not
complete P12-C or satisfy the P12-B dependency. It is not ready for integration
or canonical promotion.

## Exact evidence

- Candidate branch: `codex/phase12/P12CIdentityRuntimeSnapshot`
- Candidate commit: `531d835f01a9070df42d54291ffde32387fb4358`
- Candidate base / merge base: `8db8cfc0096c0a2bc05981e7577da048441f65e4`
- Current P12 canonical at revalidation: `4d2a9ad5c7f98a7805dede72f9722aec063231e8`
- Architecture: `c285466c355103d3637ac165246591b72eb7bda0`
- Intraday/extensibility alignment: `4b6dd1d38cffeaf3cc1ac3effea0f8ede8771194`
- Multi-participant alignment: `c285466c355103d3637ac165246591b72eb7bda0`
- Selected profile: `UnityBootstrap-Daily-v1`; P12-B remains blocked.

The candidate is a sibling of current P12 canonical after merge base
`8db8cfc`; it is not an ancestor of the current canonical tip. Its original
review/validation does not establish compatibility with the current base.

## Preserved capability and required integration

The implementation adds immutable snapshots and private staged reconstruction
for all 14 `RuntimeIdAllocator` next-value families, preserving gaps, plus a
staged `SimulationRecordSequence` next-value snapshot. It correctly keeps
`RuntimeIdentityRegistry` separate from allocator counters. These additive
owner seams should be retained.

The candidate's full `DecisionRecords.cs` diff is not safe to apply wholesale
to current canonical. Relative to `4d2a9ad`, it adds the sequence snapshot API
but also removes `NpcDecisionRecorder.TryRecordOccurrenceOnce`, its
`occurrenceReceipts` owner, and the `NpcDecisionType.ActorChoice` enum member.
The promoted optional P18-D merchant execution still calls
`TryRecordOccurrenceOnce`; integration must retain current P18/P11 behavior and
apply only the intended snapshot changes. The enum deletion is unrelated to
the bounded identity-snapshot change and should not be carried forward without
an explicit, separately justified owner review.

P12-C's accepted design also covers selected P9 genesis provenance, P8-A
Hex/anchored-Location/scale facts, and deterministic-random/provider roots.
This candidate does not implement those sections. It remains a partial
capability, not a complete P12-C delivery. The selected daily profile excludes
P18 temporal state and P20 activities; retain the alignment constraints for
any future profile extension: logical time/order and occurrence identity stay
distinct, `ActivityInstanceId` remains distinct from definition and participant
identity, and activity cardinality remains one-or-more.

## Gate and next action

P12-B's accepted admission and completed-boundary contract is a prerequisite
for integrating this work as P12-C. Keep the branch recoverable. Once P12-B is
promoted, rebase or selectively reintegrate the additive snapshot seams onto
the then-current canonical code, preserve the P18/P11 recorder contract, and
rerun the affected identity/decision suites and the required integration
validation. Obtain a fresh exact-tip independent review after that integration.
No tests were run as part of this read-only revalidation, and no P12-C
capability or P12-A readiness is claimed.

## Current-canonical follow-up — 2026-09-29

P12 canonical subsequently advanced from the reviewed base
`4d2a9ad5c7f98a7805dede72f9722aec063231e8` to
`0b5b4abb0d0a6064500adafe6a3454e41868c102`. The intervening changes are
documentation-only; the executable `Assets` tree and the promoted P18/P11
recorder behavior remain unchanged. The preserved P12-C candidate
`531d835f01a9070df42d54291ffde32387fb4358` therefore retains its existing
classification: preserve the reusable allocator and record-sequence snapshot
seams, then selectively reintegrate them on current canonical without removing
P18-D `NpcDecisionRecorder.TryRecordOccurrenceOnce`/receipt ownership or P11
`ActorChoice` decision type. This follow-up does not satisfy P12-B and does not
authorize integration or promotion.
