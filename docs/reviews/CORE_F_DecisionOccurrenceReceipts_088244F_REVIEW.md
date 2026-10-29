# CORE-F decision occurrence receipts — exact-tip implementation review

**Verdict: `NEEDS_CHANGES`**

This independent review examined the complete candidate source diff against its actual base, the applicable P12 receipt-owner and RuntimeIdAllocator Decision-counter contracts, the accepted CORE-F runtime/restore design, and the exact-source validation evidence. The candidate is not ready for acceptance or canonical promotion until the findings below are resolved and reviewed at a new exact tip.

## Exact identities

- Repository: `TorroisBr/Simulation` (`origin` is the authorized Simulation repository).
- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`.
- Actual candidate base: `8a5730ee1ead8de138a7cdfaee455377f7a00669`.
- Candidate tip: `088244f0f9f67f38dabdaad2a45c40f6fed6e7ec`.
- Candidate implementation commit: `93ec441801712a5e522b98ec84d04098ef0e5ee3`.
- Reviewed implementation source tree: `5392023906dc308c3ab6869263b37c8597893371`.
- Current `codex/core/canonical` at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Separate review branch: `codex/core/CORE-F-event-authority-review-088244f`, based on the immutable candidate tip. This record is not part of the candidate branch.

The candidate commit after the implementation commit changes only the validation record and its evidence manifest/archive; the implementation source and test tree remain the code tree above. The candidate branch is published at the stated tip. The candidate checkout contained unrelated local changes and private/untracked evidence; the review did not modify or stage them.

## Scope and contract assessment

The implementation moves the selected Daily-v1 occurrence-receipt dictionary, owner identity, revision, replay/fingerprint logic, allocator/time/sequence roots, and exact `NpcDecisionStore` binding into `DailyV1DecisionOccurrenceReceiptAuthority` in `Simulation.Core`. The Unity `NpcDecisionRecorder` delegates to that authority. Genesis and both P12 restore construction paths bind the recorder to the same authority/store instance. The P12 section identity remains `p12f.npc-decision-occurrence-receipts`, schema 1; cardinality and revision remain exact; fingerprint replay/conflict behavior and allocator/sequence ordering are retained.

The exact-zero test changes retarget reflection to the new Core owner and retain assertions for section identity, schema, owner identity, zero cardinality/revision, populated state, and revision drift. The mixed-roster admission path is unchanged. The implementation does not claim receipt export/hydration or P12-B completion.

Two bounded regressions remain:

### Finding 1 — optional recorder logger is lost by the moved receipt authority

`Assets/_Project/Scripts/DecisionRecords.cs` constructs the Core authority using the raw optional `logger` argument before the recorder applies its existing non-null fallback (`UnitySimulationLoggerAdapter.Create(null)`). The authority stores the nullable argument and its `ArgumentException` and `InvalidOperationException` handlers use null-conditional logging (`Packages/com.simulation.core/Runtime/Application/DailyV1DecisionOccurrenceReceiptAuthority.cs`, constructor and catch paths). Consequently, a supported `NpcDecisionRecorder` construction that omits the optional logger now silently drops the diagnostics from `TryRecordOccurrenceOnce` failures. In the base implementation those catches used the recorder's non-null fallback logger. This path is used by the P18-D merchant decision receipt consumer and can fail on allocator/sequence exhaustion or invalid record construction.

Preserve the prior failure diagnostics by giving the Core authority the effective logger or an equivalent host-neutral reporting callback, and add a focused regression that exercises a caught receipt failure with the optional logger omitted.

### Finding 2 — restore Try wrappers can throw for a missing required DecisionStore

`Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` creates `DailyV1DomainEventAuthority` in the restore wrappers around lines 245–257 and 430–438 after checking allocator/time/record-sequence roots, but it does not check `SelectedDecisionStore` or the explicit `decisionStore`. The new `DailyV1DomainEventAuthority.Create` rejects a null DecisionStore by throwing `ArgumentNullException` (`DailyV1DomainEventAuthority.cs`, lines 24–38). Thus an incomplete execution boundary or incomplete explicit restore composition escapes a `TryCreateRestoredActiveRuntimeSession` call as an exception before its downstream validation can return `false` with a diagnostic. The first overload checks `HasAdmittedSelectedDailyExecution` only after creating this authority. In the actual base implementation, the three-root event authority could be created and the restore composer rejected a missing DecisionStore through its normal Try result; the candidate changes that failure contract.

Validate the newly required DecisionStore before authority creation (or contain the constructor exception as a Try failure), return a specific diagnostic with no candidate/session publication, and add regressions for both restore entry paths.

No product or architecture decision is required to resolve these findings.

## Validation and evidence checked

The exact-source evidence below identifies the implementation commit/tree above. Private evidence is cited by run identity and artifact hash only; this record intentionally omits machine-local paths and raw private metadata.

- Official CoreValidation run `44bf58608d2348f69d40796ea56748a7`: manifest status `Passed`, exact commit/tree, stable input snapshot; cold and warm reports each contain 916/916 passed and zero failures. The new `DecisionOccurrenceReceiptAuthorityOwnsReplayFingerprintAndP12Census` identity is present in both reports. Private manifest SHA-256: `aca5dcc3152ea1dbf35df74fa26afe4b6387592149705708dbf68e9bd43d6168`. Cold TRX SHA-256: `2418ca05169b33d7ca79ec504452dab1664cfb76e4d972db549ddd6557e0938a`. Warm TRX SHA-256: `e15956d860a3830aeed5d97794ac3b74cc31257541613173389677cae1455ef6`.
- Focused Unity `SimulationRuntimeAdmissionTests`, run `1fd347dcbf4a49e0b92f52eb5ae87247`: 228/228 passed, zero failed/skipped/inconclusive, stable exact-source input. Private run-manifest SHA-256: `d93cfcc318d734a4b94f9230ba9241854287a795eb1be9c6374c1f0c5026acb6`.
- Full Unity EditMode run `44b20c22bd5d46d8a1dfef95e4c9c9c7`: 3276/3276 passed, zero failed/skipped/inconclusive, stable exact-source input. Private run-manifest SHA-256: `29a8501ee4798152c8128f3391ae2245b09f88d0704529927d76a86ecb09cb02`.
- Official Unity Smoke run `e807a1fd59884aa4a9fe70834d722aab`: 5/5 passed, zero failed/skipped/inconclusive, stable exact-source input. Private run-manifest SHA-256: `df7cfa56061a40f11f70dc70acc258b998944c98f64c3ee89ec54da9be4a95b6`.
- Privacy-safe EditMode package manifest SHA-256: `213699bb007ecdd9294a83d1715f6eee4f581f0d682a483e2fbf3aec6ab1eb82`.
- Privacy-safe EditMode package archive SHA-256: `e0c0d782b361b865f115c10d63e9554dac294128a5a833406b5f1340311f1019`. The candidate validation record reports the official verifier result `VALID`; the reviewed hashes match the committed record.
- `git diff --check`: recorded `PASS` against the implementation diff.

The reviewer did **not** rerun tests. The private original reports were inspected through their official manifests and result summaries; they remain private and unchanged. No validation result is inferred for a code tree newer than the exact implementation tree above.

## Review limitations and next gate

This review is not canonical promotion and does not accept the incomplete CORE-F package. Resolve the two findings, add the targeted regressions, run the required affected validation on the changed exact tree, and obtain a fresh independent exact-tip review. P12-B, P12-A, P13, `DailyV1Application.TryStart`, detached .NET hosting, and full CORE-F closure remain governed by their separate contracts.
