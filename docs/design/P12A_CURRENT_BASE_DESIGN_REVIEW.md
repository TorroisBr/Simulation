# P12-A current-base detached continuation design review

Date: 2026-10-12

## Reviewed identities

- P12 canonical design base: `25d2dcf1371137ceb0e158eef4ba02d2e2304612`.
- Architecture readiness handoff and independent review: `e940714bcc05c0d790e0cb9a7f614ee6b493e21a` (review PASS).
- Full design review target: `666a4b216275aef2fa9d9b9f152e93d00db51757`, directly based on the P12 canonical design base.
- Final design candidate: `225b8d08a2ef7570c767ddc3363029f3c4e9eadc`, a docs-only child of the reviewed full-design target.
- Candidate branch: `codex/phase12/P12ATechnicalDesign`.
- Exact current P12 canonical remained `25d2dcf1371137ceb0e158eef4ba02d2e2304612` throughout review.

## Independent review results

The independent full-design review passed at `666a4b216275aef2fa9d9b9f152e93d00db51757`. It checked the detached persistence design against the accepted P12-A scope and current P12 C–G contracts. The corrected census mapping contains 48 fixed Required rows, 13 fixed ExplicitlyEmpty rows, and the documented dynamic NPC, Person, and City rows. The design correctly treats P8-E and P10 LocalTopology as having no selected-profile provider rows, while retaining the two per-NPC P12-F LocalTopologyKnowledge rows. The named C–G snapshot/staging adapters, compatibility descriptor, source-independent staging, fresh-target owner checks, atomic host publication/storage, rejection behavior, and exact continuation validation sequence are defined without changing domain owners or the Daily-v1 profile.

An independent inventory audit also passed the 299-row cardinality and classification at the reviewed design tip. It confirmed 22 Required rows per NPC plus the conditional unbound-NPC residence row, one Required row per Person, and four Required rows per City.

A separate exact-tip delta review passed for `225b8d08a2ef7570c767ddc3363029f3c4e9eadc` against parent `666a4b216275aef2fa9d9b9f152e93d00db51757`. The change preserves `SimulationSessionReportState.LastEconomySnapshotDay` as a signed 64-bit typed envelope header value, outside the owner/provider census. The reviewer verified that the field is a `long`, affects host reporting, and is not a census provider; `SimulationRecordSequence` remains in its C owner section. The 299-row manifest and the prior full review remain applicable.

No actionable design findings remain. The exact candidate passes `git diff --check`. This is a documentation-only review; no Unity tests were run or needed. The candidate contains no production code or test-source change.

## Readiness and limits

The architect’s reviewed current-readiness handoff marks P12-A upstream prerequisites satisfied within the accepted profile and P12-A ready for technical design. The independent technical-design review now passes, so P12-A is `READY_FOR_IMPLEMENTATION`. The user’s current orchestration instruction authorizes continuing through implementation, validation, exact-tip review, and promotion after this design gate; no additional routine checkpoint acceptance is required.

Implementation remains limited to the selected same-build/current-host `UnityBootstrap-Daily-v1` continuation contract described in `P12A_PERSISTED_CONTINUATION_CURRENT_BASE_DESIGN.md`. No General-Test/P10 payload, new profile, P13 fork/history guarantee, or broader save/load product semantics are admitted. P12-B through P12-G remain delivered only within their recorded scopes. P13 remains blocked. Phase 12 remains open and is not formally closed by this readiness record.