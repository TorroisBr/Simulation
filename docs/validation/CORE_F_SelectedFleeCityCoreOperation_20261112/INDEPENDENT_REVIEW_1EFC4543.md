# Independent exact-tip review — selected FleeCity Core operation boundary

## Verdict

**VALIDATED_CANDIDATE**, with no actionable code findings.

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`.
- Reviewed implementation/evidence tip: `1efc45434745e32739d8142a8a48a869d26d529e`.
- Reviewed evidence-tip tree: `de18185f58e9e87e60005bae6823520eed8aad27`.
- Implementation commit: `ad8b321e37dd9ae613eb904ef64523ef8ff83078`.
- Implementation tree: `e4fda2c7b6edb3dc6b7c3b49596ba018dac3135d`.
- Implementation base: `662cdcd19213513f8fc3c65f8ebe6b9b39f86e9d`.
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- The local and remote candidate refs matched at review. This record is an additive review-only commit; it does not change implementation or validation evidence.

## Review scope and findings

Reviewed the complete implementation diff and added tests in `SimulationRuntime.cs`, `DailyV1P12CrimeJusticeMutationCoordinator.cs`, `DailyV1SelectedActionExecutionStage.cs`, `DailyV1SelectedCrimeActionExecutionStage.cs`, and `DailyV1SelectedCrimeActionExecutionTests.cs`, along with the candidate validation record and public Unity package.

Core owns the admitted FleeCity operation boundary: it pins the current admitted session/thread, graph, actor, action and payload references, Justice owner/census identity, and protocol; it rejects a stale or replaced host binding before opening the P12 scope. The operation uses the expected ordered owner sections: Crime status, Justice records, Justice P18 receipts, and the optional Crime P18 receipt section. The successful action roll precedes opening that immediate scope.

Unity retains exact City/Location projection preflight and publication, and the existing TravelPlan leaf admission/revision/epoch path. Non-admitted compatibility remains on the host boundary. Tests cover exact scope, host replacement and stale action rejection. The Unity regression covers projection reconstruction, rejection without mutation for an unadmitted slot, and the admitted TravelPlan-only revision/epoch change. I found no weakened assertions or actionable findings.

## Validation and evidence verified

No tests were rerun by the reviewer.

- Official CoreValidation run `781c6ef0ba6e47278b8c9359e389dff8`: cold 1,109/1,109 and warm 1,109/1,109; both protected TRX files report zero failures. Their SHA-256 hashes are `9519ca6e79888c2c7a4dd6f1b1deb7d6f02270fa00b7d5f40f1542d0c417b749` (cold) and `651c73fdd30defb7f752882c6b300a633b3e6b732ecdd1938f8881ed7e272038` (warm). Core input digest: `9bc76d052639f594dda769664699b41933dacb7be402ea02a4ff0792688c3667`; 541 captured source inputs were rehashed against the post-commit checkout with zero mismatches. The run record also reports four generated dependencies.
- Unity admission run `4662199487c5419f9cf374505e4c1c3d`: 246/246 passed, zero failures/skips; original XML hash and byte count match its private run manifest.
- Full EditMode run `6f41ccb1fb8941a7898dbfc008de834f`: 3,318/3,318 passed, zero failures/skips; original XML hash and byte count match its private run manifest.
- Smoke run `914d3beed45a4b06a6a8c45a9421736b`: 5/5 passed, zero failures/skips; original XML hash and byte count match its private run manifest.
- Unity input digest recorded by the validation record: `eddda14f6972e5999cc1853d4d207de3884cab831422014a0d518525d2246c2a`. The full EditMode manifest reports a stable 1,982-file input inventory; all 1,982 captured paths, byte counts, and SHA-256 values match the post-commit checkout.
- Public normalized full EditMode package manifest SHA-256: `9cbf474f2221ad6e051b6d9ed54e4b39a143a226175885161138296afe0a3513`.
- Public results archive SHA-256: `ba80d95cd391971e5e0ebf9ec81c25922799ddd3c12c7de3f0d6c31dd56b6841` (175,742 bytes). The archive XML contains 3,318 cases, with zero failed or skipped cases. The package verification and absolute-path scan are recorded as passed.
- The implementation validation record reports `git diff --check` passed.

The CoreValidation and original Unity reports/logs remain in protected private E: evidence. No public Core TRX package is claimed: the approved `dotnet-trx-source-context-v1` context was not assembled. Focused admission and filtered Smoke originals also remain private; the public package is the complete full EditMode report, which includes those test identities. The attempted combined filtered package was rejected for aggregate suite-count mismatch and produced no package.

## Status limits

This review validates only the selected FleeCity boundary at the exact reviewed implementation/evidence tip. It does not claim full CORE-F completion, P12 scope change, phase closure, or Core-canonical promotion. The candidate remains unpromoted.
