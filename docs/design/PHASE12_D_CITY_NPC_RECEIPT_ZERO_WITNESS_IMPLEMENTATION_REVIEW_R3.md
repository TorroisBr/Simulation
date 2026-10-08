# P12-D NPC receipt-owner exact-zero witness — implementation review R3

**Verdict: VALIDATED_CANDIDATE.** The saturated-epoch atomicity finding is closed. The source-digest discrepancy from the first R3 pass is resolved as a single line-ending byte transformation, and the corrected validation record is included at the exact reviewed tip. No code tree changed after the tests.

## Exact identity and authority

- Candidate branch: `codex/phase12/P12DCityNpcReceiptWitnessImplementation`
- Exact candidate tip reviewed: `cc9f11471887c71f2a9c4834e1bcc1edeb3025fe`
- Candidate commit tree: `0c2cf1ed206e8c4cbb601efba46e511035378ef1`
- Reviewed code/test tip: `4947ec926b3a48427e9c07de5d722d45014cf1bf`
- Reviewed code/test tree: `dd0cda707693237a56f5664b2eabc8aa6821b100`
- Reviewed `Assets` tree at both tips: `b239758a62c9c670f7400af2567515308e138a4f`
- Candidate base and current remote `codex/phase12/canonical`: `04e7c3f49a7c9690ebc091fcfc50f05ef67009d6`
- `merge-base(base, candidate)`: `04e7c3f49a7c9690ebc091fcfc50f05ef67009d6`; candidate is a clean fast-forward.
- Architecture: `47eff220c7ce00f6e7c759bdc2b76780bb46f628`
- Accepted technical design: `5b10a58a680af9adcf56a585f6f2ac50f8e6b172`; independent design PASS: `d4d2e343d3297d1dfc61b9ff52c9db25aba464e3`.
- Prior implementation review records: R1 NEEDS_CHANGES `331590bae2eaaac70758ab39c2a2078c9c07097d`; R2 NEEDS_CHANGES `d7f5dc8c3e9b272dcf2d3b3cfa3c76a8240f1c9f`.
- The only change from code/test tip `4947ec9` to the exact reviewed tip `cc9f114` is the corrected validation follow-up document. The `Assets` tree is unchanged. No unrelated ProjectSettings or `.meta` changes are in the candidate diff.

## Source and contract review

The complete base-to-candidate source diff was reviewed against the accepted D exact-zero design and the current P12 Brief/State. The implementation remains limited to two required schema-v1 rows per current NPC for the excluded LocalObservation and MerchantTradeState receipt owners.

- Raw owner reads bypass lazy list/owner accessors, do not allocate or expose receipt contents, and reject null lists or negative revisions. Providers additionally require both cardinality and local revision to be exactly zero on every read.
- Section IDs use the stable RuntimeId; providers retain the exact parent NPC and child-owner object. The factory rejects duplicate IDs/objects, aliases, missing owners, and malformed/populated receipt owners, and orders rows deterministically.
- The rows join the existing required owner vector and selected-profile inventory. Token/capture validation sees the same exact child owners and fails closed after an excluded P18 receipt is committed.
- Dynamic roster add/remove/re-add reconciles the family in the existing staged shared-epoch transaction. A same-object re-add retains its exact child owners; a different object reusing a retained RuntimeId is rejected by the existing registry contract. No second epoch, receipt notification, export, replay, or P12-B mutation semantics were introduced.
- The staged family maps/providers are published only after provider validation and the shared epoch-capacity guard. Failure faults the existing census boundary.

## Prior review findings

1. **LocalObservation receipt must stale an issued completed-boundary token — RESOLVED.** The parameterized test covers LocalObservation and MerchantTradeState writes after token capture, verifies the exact owner becomes `1/1`, then verifies token validation rejects it.
2. **Committed roster addition with a populated receipt owner must not publish partial rows — RESOLVED.** Both owner kinds are covered. The roster membership write is observed as committed; neither new receipt row/provider is published, the epoch does not advance, and the census faults closed.
3. **Saturated shared epoch must preserve atomicity — RESOLVED.** `SaturatedEpochCommittedRosterAddFaultsWithoutPublishingReceiptMapsOrProviders` stages a valid empty-owner roster addition, sets the existing protocol epoch to `long.MaxValue` before outer reconciliation, and verifies the epoch remains saturated, provider and receipt maps remain the original objects and counts, neither new section appears, registered/expected maps remain unchanged, assessment and epoch read fault closed, and later admission is rejected. No production code changed for this follow-up.

The implementation and tests preserve the accepted exclusions: no P18 receipt export/replay, no complete P12-D assembly claim, no broad owner/shared-epoch coverage, no capture eligibility beyond the existing validated boundary, no export/hydration, no P12-A/P13 readiness, and no Phase closure.

## Validation evidence checked

The committed evidence is bound to code/test tip `4947ec9`; the exact reviewed tip `cc9f114` changes only its documentation. I recomputed all retained result/log hashes. Git stores the XML artifacts with LF; converting those XML bytes to the recorded Windows CRLF representation reproduces the manifest SHA-256 values. Compressed log hashes match the manifest directly.

- Receipt-owner focused suite: 13/13, zero failed/skipped. XML `262cf3d5dd0d40b34f1d18662c772562ae05c7e2ae01e5d52cc59bea91c0f7f1`; compressed log `df43c35160d7fe9c2b20fcbaf73640a1843bc532bde200b7ae85fcc234238beb`. The XML includes the saturated-epoch test as Passed.
- ALL EditMode: 2582/2582, zero failed/skipped. XML `979783a3c596d657dffb7d348cc3c78543e67b00c6b8adf016b00e9c9f55d7f8`; compressed log `f3c659c04bf16b6d11c1df8b83ff5daca2499bcf13620c6714c97ff386d01312`. The XML includes the saturated-epoch test as Passed.
- Official Smoke: 5/5, zero failed/skipped. XML `7d78251769972d6a452768f999b6b02d2412710c7cc8d359e7f9baef932dad8b`; compressed log `0f22ddb7354774f9005297463f248324636e94c3782ca4d2ff606fe3c735494d`.
- `git diff --check`: PASS.

The corrected follow-up records both source byte identities: validation-worktree SHA-256 `0e11b6751567d164bbaf8307c8579a6a3658f0088d3cab8b7876a369084465a6`; committed Git blob SHA-256 `bc09681aae9074e8d060e61394a66f10c8c2b1dbc550bbfb122295b289cbcf6e` (blob `5d416e64a8579c82e895ae145deae790a23fbc1f`). I independently inserted one CR byte at offset 21439, immediately before the LF at that offset in the committed blob; the resulting SHA-256 equals the recorded worktree digest. The correction states the worktree was clean and identifies this as a line-ending/filter transformation, not a post-test edit. No Unity rerun was needed.

## Integration constraints

This validates only the bounded P12-D exact-zero receipt-owner witness/reconciliation slice. It does not deliver the City/NPC snapshot assembler or complete P12-D, broaden P12-B, or change P12-A/P13 status. Before integration, refresh canonical and revalidate this candidate against any later P12 canonical tip. Canonical promotion remains a separate workflow.
