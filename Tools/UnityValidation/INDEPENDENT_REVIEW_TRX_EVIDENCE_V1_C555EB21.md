# Independent review: TRX-EVIDENCE-v1

Date: 2026-10-14

## Exact target and authorities

- Candidate branch: `codex/tools/trx-evidence-v1-remediation`
- Candidate commit: `c555eb21c6f0f658dd851fbeda8d12ac59ddd18c`
- Candidate tree: `274cb87538ca0038fd31c35457fd3f99d29ba48d`
- Candidate parent: `27e4647169c97dac84d71dda1d70f25c2a938d01`
- Direct P12 base: `4cc31073f0ec6ba5c718084e4cebd6e914931d5d`
- Promoted TRX contract: `63d3b8352a450b27b9a5f593c4ea166c0343c59e`, `docs/operations/DOTNET_TRX_EVIDENCE_CONTRACT.md`
- Applicable evidence policy: `58157f9ac7bf573e9a4ed1b2d485a2db190e7c4f`, `docs/operations/VALIDATION_EVIDENCE_POLICY.md`
- Independent reviewer: Codex subagent, not the candidate author. Review was read-only against the exact candidate; this record is additive.

The full six-file cumulative diff from the direct P12 base was inspected, including the final remediation chain. The remote branch was synchronized at the exact candidate commit before review.

## Verdict

**PASS — exact-tip implementation review of the TRX tooling capability.** No open code-contract findings remain from the prior review rounds. This is not an attestation that the actual CORE-A original evidence has been privately compared or that P12-A is ready to integrate.

## Review disposition

The candidate resolves the prior findings as follows:

- Native `Error` outcomes remain distinct from native `Failed` and `Executed` counters; complete failed and supported non-passing summaries can be retained without passing the gate.
- Recovered command claims bind the fixed runner script, `<EVIDENCE>/<runnerRunId>` result path, command logs, three generated Core/test/demo `project.assets.json` inputs, and TRX assembly targets. Cwd recovery is explicitly evidenced and checked against these relations.
- Captured dirty inputs are matched to the current worktree status and complete source/generated-input inventory, including exact bytes and hashes.
- Retained assembly/PDB identities and receipts are checked against source outputs; the existing-receipt path requires exact deterministic receipt bytes and revalidates retained copies.
- Discovery remains the exact ordered protected runner/list-tests claim. Archive-only verification compares each cold/warm report’s test-name multiset with the public selection and preserves duplicate-name multiplicity.
- Private-context verification reconstructs and compares all public claim groups, including provenance arrays, while separately binding runner identity, attempts, normalized report bytes, and package tool identity.
- Archive-only manifest validation enforces the safe string `bundleId`, exact `<bundleId>.zip` binding, and archive-only discovery/report consistency.
- The PE metadata reader call and timestamp-offset normalization fixes are present in the reviewed cumulative tree.

## Validation

Fresh validation ran against the exact candidate:

- TRX synthetic suite: 116 assertions passed.
- Existing shared evidence packager suite: 64 assertions passed.
- Unity validation runner self-tests: 33 passed, 0 failed.
- `git diff --check 4cc31073f0ec6ba5c718084e4cebd6e914931d5d c555eb21c6f0f658dd851fbeda8d12ac59ddd18c`: passed.
- Candidate worktree: clean.

These tooling changes do not modify simulation-domain or Unity project code, so Unity EditMode/Smoke suites were not rerun for this review.

## Remaining evidence gate and integration constraints

This review did not open or inspect the protected CORE-A originals, source-context bundle, retained assemblies, or ACL records, and did not create the actual CORE-A evidence package. The exact private original-to-normalized comparison, current input/output/retention verification, and durable comparison attestation required by the promoted contract remain separate outstanding evidence obligations. The 116 synthetic assertions and code review do not satisfy those obligations. Do not treat this candidate review as P12-A implementation readiness, a real-run package PASS, canonical promotion, or Phase closure.