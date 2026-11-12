# Independent exact-tip review — selected Guard Arrest Core operation boundary

## Verdict

**VALIDATED_CANDIDATE**, with no actionable implementation findings in the material independently reviewed.

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`.
- Reviewed evidence tip: `feb3a678d294aeb7ecf1d07903218dcd90c0b8ed`.
- Evidence-tip tree: `9ddeb3655c76b94bcc81a95b7f4b951a428d25c7`.
- Reviewed code commit: `934027a89d2a48783142db592f3a69d01f815260`.
- Code tree: `887c9d240478b74619a91d20550c44c8f35fa050`.
- Code base: `191380b87adc3f9fe16e1a37109798ac3eef6a01`.
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Review method: independent read-only review of the exact pushed commits and diff through the connected GitHub repository view. The reviewer did not modify candidate code or rerun tests.

## Findings

The review confirmed that Core pins the admitted execution state and owner thread, exact graph, Guard actor, action and payload, optional target, Justice host binding, and protocol before opening the scope. Non-null targets must be the exact owner in the graph and match the payload identity. A missing target remains supported for the existing failed-roll path. The operation derives the guard/target NPC status sections, Justice records section, and receipt sections from the pinned graph and binding. The wrapper continues to route sentence, status, hidden-state, and arrest projection callbacks through the existing Unity adapter.

The added tests cover exact participant membership and ordered sections, stale-action rejection before census access, and replaced-host-binding rejection before opening P12 scope. The reviewer found no weakened assertions or actionable defects.

## Evidence boundary

The reviewer verified the exact commit ancestry and code diff, fetched the validation record and public Unity package manifest, and confirmed the recorded run identities and package hash metadata. The record reports CoreValidation cold/warm 1,112/1,112, full Unity EditMode 3,318/3,318, filtered Smoke 5/5, and stable inventories of 541 Core and 1,982 Unity inputs.

The reviewer could not retrieve the binary Unity archive or protected E: original run manifests/TRX/logs through the remote review channel, and therefore did not independently recompute every source hash or inspect every private original report. The implementation verdict is independent; those private validation comparisons remain supported by the local official-run manifests and the committed validation record, not by the reviewer's remote access. No additional tests were run during review.

## Scope limits

This review validates only the selected Guard Arrest operation-admission boundary. It does not claim complete Core-owned daily execution, detached .NET hosting, completion of shared restoration, full CORE-F validation, Phase closure, P12 scope change, or Core-canonical promotion.
