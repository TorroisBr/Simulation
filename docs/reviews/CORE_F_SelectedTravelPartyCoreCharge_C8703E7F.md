# CORE-F Selected TravelParty Core Charge - Independent Exact-Tip Review

Date: 2026-11-08
Disposition: VALIDATED_CANDIDATE / PASS

## Reviewed identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Reviewed tip: `c8703e7fe8fe5f41c1a9030d83f931c4cae958dc`
- Reviewed tree: `5d4a4027ada3ed63d69821d58b3a851c27361b34`
- Exact base: `62539a722ad89efb5060cc5d30354c190ef36c0e`
- Implementation commit: `f103dcfc9d00b3b1c1cccbf3ceb6faf901cc2aff`
- Implementation tree: `aad87ecdf157c4388e4fa39cac6a4fbe0a80c49d`
- Current Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

The candidate remote ref matched the reviewed tip. The canonical ref was unchanged. Production and Core implementation files are unchanged from the implementation commit; the review-fix tip adds the Unity integration tests and their validation record.

## Verdict and prior finding closure

**PASS. No actionable findings remain.**

The previous `NEEDS_CHANGES` finding was that tests called the Core charge helper directly but did not exercise `TravelPartySystem` dispatch through a real selected runtime. The review-fix adds two tests in `P12TravelPartyStartOperationTests` using the authored `UnityBootstrap-Daily-v1` bootstrap and `SimulationRuntime.TryStartTravelParty`.

- `FullySelectedRosterUsesCoreChargeAndKeepsOneP12Epoch` verifies exact selected NPC owners in the runtime graph, a successful party start, each member account's balance and revision change, one party-store revision, one P12 epoch, and a healthy final census.
- `MixedRosterKeepsUnityTransactionFallbackAndP12StartAdmission` registers an unselected compatibility NPC, adds its City presence through the runtime projection, and verifies a successful mixed-roster start, account changes, one P12 epoch, and a healthy final census.

Source inspection confirms the all-selected roster dispatches through the Core charge operation only after exact graph and NPC-owner checks; the mixed roster retains the Unity transaction fallback. Existing assertions were not weakened. The prior one-line teardown was replaced with cleanup for the new bootstrap test objects.

## Validation evidence

The final official focused Unity run `d8f9a18ff7e048b4b16606aa644d7c04` passed `18/18` with zero failures, skipped tests, or inconclusive tests. Its input snapshot was stable with digest `b08d9207f8b3b2bcffd6cfe1239720defe9a5b3146675ae49587629a92dc8e0e`. The XML reports both new test cases as Passed.

| Evidence | SHA-256 |
|---|---|
| Final run manifest | `d815f038145a3af40db2f7f56f00357d60fef9cd4ce4b3a68acbb7f330b1777e` |
| Final XML | `6e9a61beb892319a209a5bdf975e759f628c91d5deb4cf585a42c265e37d5b73` |
| Final log | `6bfd325ec80a321653b9f139bb91e000cccd5e85991d71122485a613a2ac5d6f` |
| Final test source SHA-256 | `acb26b26a0add588e84fb5b1b7e5dbfca697cafaaa9bdb35594fb3f2f0d6a3ba` |
| Final test source Git blob | `be0c4b9c313e05d996bd74cd4bc54a31e2add28d` |

The source hash, Git blob, run manifest, XML, and log match the validation record and retained artifacts. The implementation-only evidence previously reviewed remains CoreValidation cold/warm `1,064/1,064`, Unity P12 start `16/16`, EconomyTransaction `59/59`, TravelPartyCensus `10/10`, and Smoke `5/5`. The review-fix adds focused Unity coverage; it does not claim a full Unity EditMode run.

`git diff --check` passes for the review-fix delta. Unrelated `ProjectSettings` changes and private/untracked validation artifacts were unstaged and excluded from this review record.

## Limits

This validates the bounded selected TravelParty group-charge integration and its mixed-roster compatibility path. It does not complete the full TravelParty stage or CORE-F, change P12 scope, or authorize Core canonical promotion. The reviewer inspected the preserved evidence and did not rerun tests.
