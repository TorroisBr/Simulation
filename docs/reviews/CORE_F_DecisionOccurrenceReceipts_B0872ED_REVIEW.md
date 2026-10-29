# CORE-F decision occurrence receipts — exact-tip implementation review

**Verdict: `VALIDATED_CANDIDATE`**

The earlier exact-tip review identified two implementation findings and a validation-record hash discrepancy. The implementation findings are resolved in the current code tree, the new regressions pass, and the Smoke log digest now matches both the protected run manifest and original log. No actionable implementation or evidence findings remain for this bounded candidate.

## Exact identities

- Repository: `TorroisBr/Simulation` (`origin` points to the authorized Simulation repository).
- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`.
- Actual candidate base: `8a5730ee1ead8de138a7cdfaee455377f7a00669`.
- Candidate tip reviewed: `b0872eddd7bbcf89d702e80434f066eb6ca948a5`.
- Candidate implementation commit: `4e31d7cdc19628c403e9503ddbcd9514f08fd1df`.
- Reviewed implementation source tree: `b4898ec7d62869bde10dee20db5736d81d688b69`.
- Current `codex/core/canonical` at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Prior `NEEDS_CHANGES` review record: `d8d88cf5ec94a5116521d5fa8573a91cb9b49910`, which reviewed the prior candidate `088244f0f9f67f38dabdaad2a45c40f6fed6e7ec`.
- Separate review branch: `codex/core/CORE-F-receipt-fix-review-b0872ed`, based on the exact candidate tip. This record is not part of the candidate branch.

The implementation commit is a direct child of the prior candidate. The subsequent candidate commits contain only validation evidence and a one-line correction to the Smoke log digest; the executable source and test tree remain `b4898ec7d62869bde10dee20db5736d81d688b69`. The candidate worktree has no source or test edits beyond the committed tree. Existing unrelated user changes and private evidence were left untouched.

## Review findings and resolution

The full base-to-code change moves the selected Daily-v1 occurrence receipt dictionary, owner identity, revision, replay/fingerprint logic, allocator/time/sequence roots, and exact `NpcDecisionStore` binding into `DailyV1DecisionOccurrenceReceiptAuthority` in `Simulation.Core`. The Unity recorder forwards to that exact authority. Genesis and detached restore construct the authority with the same decision store used by the selected runtime. P12 section identity/schema and receipt cardinality/revision remain unchanged; receipt replay and fingerprint conflict semantics retain the same allocator, sequence, store, and receipt order.

The first review finding concerned the optional recorder logger. `NpcDecisionRecorder` now applies its existing `UnitySimulationLoggerAdapter.Create(null)` fallback before creating the Core authority and passes that effective logger to it (`DecisionRecords.cs`, lines 86–95). The internal authority adapter requires a non-null logger. The new `CoreRuntimeTests.DecisionOccurrenceReceiptFailureUsesDefaultRecorderLoggerWhenOmitted` regression verifies the default-construction path logs the expected sequence-exhaustion diagnostic, returns failure, and leaves decision/receipt state uncommitted. That exact test identity is present and passed in the full Unity report.

The second finding concerned missing `NpcDecisionStore` roots in restore Try methods. Both `DailyV1Application.TryCreateRestoredActiveRuntimeSession` composition overloads now reject a missing store before creating `DailyV1DomainEventAuthority` (`DailyV1Application.cs`, lines 243–258 and 427–439). The tests cover both the staged execution-boundary and explicit composition paths; they assert failure diagnostics and null candidate/calendar. The explicit composition regression also asserts that no active session was published. Both modified Core test identities, `CoreRestoreCommitRechecksCandidateCensusBeforeInitialPublication` and `ApplicationComposesRestoredOwnerGraphBoundaryAndUnpublishedSessionTogether`, are present and pass in the cold and warm CoreValidation TRX reports.

Exact-zero census coverage was retargeted to the Core owner without weakening section/schema/owner/cardinality/revision or drift assertions. The exact owner instance is checked across the recorder, event authority, genesis and detached restore. The mixed-roster admission path is unchanged. This candidate does not claim receipt export/hydration, P12-B completion, full CORE-F completion, `DailyV1Application.TryStart`, or detached .NET hosting.

## Validation evidence checked

All evidence refers to implementation commit `4e31d7cdc19628c403e9503ddbcd9514f08fd1df` and source tree `b4898ec7d62869bde10dee20db5736d81d688b69`. Private evidence is cited only by run identity and SHA-256; this record contains no machine-local paths or raw private metadata.

- Official CoreValidation run `0debb15805f34e42a78501891c94ec8b`: manifest status `Passed`, exact implementation commit/tree, stable input digest `117f50eda40385d584366f12c543c9a687e49dff37313986dc852b098d80da73`. Cold and warm each passed 916/916 with zero failed, errors, timeouts, or unexecuted tests. Manifest SHA-256: `73a777a1b13e733a70b73aedb95c812767bb602c127546eb62e80f44a48e8ecf`. Cold report SHA-256: `f35b792f704b412c42aab62593e3e8d0ff4ae54993dc0318c8df81fec605d75c`. Warm report SHA-256: `6ceaf0d4414fdfe8a14383574f1e595b37068b98d44171502d5d3195131bda1f`.
- Full Unity EditMode run `eb0e66758f9f42338f10ef4d06abcdb5`: Unity `6000.3.9f1`, 3277/3277 passed, zero failed/skipped/inconclusive, stable source input whose repository tree is the implementation tree above. Private manifest SHA-256: `ce83cb4c17b066d9c4b590e592db613c25a401c7b3fddf49e557b8a7208a5126`. Original XML SHA-256: `cfc99a6bb5c107b45ed84b4737e623f6967f2d254ca4d4434d07990397630dfc`. Original log SHA-256: `ff6f557e394b6303b15e0eb7688b4e5b4eea97773ff06f1f66da498aa440fc03`.
- Official Unity Smoke run `94fbe0c6db8f4f2da4646f7d40ac6025`: 5/5 passed, zero failed/skipped/inconclusive, stable input on the same implementation tree. Private manifest SHA-256: `086b81d85fa8bcee714ee077c934ea3f7688d751831fd200f699a3d9f9bb8758`. Original XML SHA-256: `d8fd7917ab1a4c5b5407caf7f6d709ad5b32d3c50941b0b69a7c01525367a0bb`. Original log SHA-256, as declared by the protected manifest and verified against the original log: `eb99b2b4fe6b37dbb1e38a94a2d64feefe2ecd7f623820a2378dd2ecd54dfd4d`. This is the corrected value in the validation record at candidate tip `b0872ed`; the previous documentation-only tip had a different, incorrect digest.
- Privacy-safe full EditMode package manifest SHA-256: `cb76c6cb25634077d817972029f0e6f23a6f260c0aef743fe64eafa7aedac937`.
- Privacy-safe full EditMode archive SHA-256: `4adc06df2d0c974a5cd40928df14015d99f1462e3806343d3e20e51b03776217`. The repository's official verifier returned `VALID` for one run and one normalized XML entry, bound to the protected full-run manifest.
- `git diff --check` passed for the implementation range and exact candidate range.

The CoreValidation raw TRX/logs and Unity original reports remain private and unchanged. The candidate validation record does not claim a normalized CoreValidation TRX package. The reviewer did not rerun tests; exact-source manifests and original result summaries were inspected, the package verifier was run, and artifact hashes were checked.

## Integration constraints

This is a validated intermediate candidate only. It does not complete or promote CORE-F, change P12 profile scope, claim P12-B completion, or establish `DailyV1Application.TryStart`, detached .NET hosting, or T/T+1/T+2 continuation. Canonical promotion remains a separate workflow with a refreshed preflight.
