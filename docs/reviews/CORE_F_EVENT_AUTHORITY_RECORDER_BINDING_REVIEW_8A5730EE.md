# Independent exact-tip review - Core travel recorder admission

Date: 2026-10-28

## Identity and verdict

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Candidate: `8a5730ee1ead8de138a7cdfaee455377f7a00669`
- Candidate tree: `b51bcc1e985bd8fd2cf1d53a351e4853ed138abd`
- Immediate base: `a817ccf3fbab1dd0dd5eb8d591c410b9cd0fac98`
- Production delta also re-reviewed: `446b90c9b1303785319d7e247cdb86c32f1bd7ea` to `a817ccf3fbab1dd0dd5eb8d591c410b9cd0fac98`
- Current Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Verdict: **VALIDATED_CANDIDATE** for this bounded recorder-admission change.

The candidate ref and its origin-tracking ref both resolve to 8a5730ee. The 8a5730ee commit changes only `tests/Simulation.Core.Tests/DomainEventAuthorityCoreTests.cs`; production is unchanged from a817ccf3. The complete reviewed production delta contains `DailyV1ApplicationSessionGate.cs` and `DailyV1DomainEventAuthority.cs` plus the associated test. No unrelated code was included.

## Previous finding and closure

The preceding review found no production defect and one test-integrity gap: the old test called the Core authority's bind method and then asserted success, so it could perform the first bind and did not prove session creation had bound it already.

The new regression creates a fresh selected genesis/session, then constructs a competing recorder using the composition's exact allocator, clock, and record-sequence objects but a separate HistoryStore and DomainEventStore. Binding this competitor into both travel slots must fail. The binding method would accept that same recorder in both slots if the boundary were still unbound, so the rejection proves a prior recorder pair is established. Binding the composition's exact Core authority afterward succeeds, proving the rejected attempt did not replace it and that later host validation is idempotent. The fresh composition excludes cross-test state. Existing identity, store, history, and session assertions remain; none were removed or weakened.

## Production review

`DailyV1ActiveRuntimeSession.TryCreateCore` verifies the admitted selected Daily-v1 boundary, exact definitions and identity roots, constructs a local candidate session, validates P12-C roots, and binds the Core event authority before returning the candidate. A failed bind does not expose a session. `DailyV1DomainEventAuthority.TryBindSelectedTravelEventRecorders` checks exact allocator, clock, and sequence object identities, then binds its single Core recorder to both Travel Party and Solo Travel slots. `DailyV1ExecutionBoundaryState` accepts the first valid pair and thereafter only the same recorder references.

The later Unity `SimulationRuntime` bind is consequently an idempotent validation, not an opportunity to substitute a different store that reuses the identity roots. Event/history remains one downstream writer authority; action outcome, mutation ordering, and P12 persistence behavior are unchanged. The architecture's outcome/commit/event ordering remains intact. No actionable production finding.

## Validation and exact-source applicability

CoreValidation run `c8adf7d7102e4aac8dd1860ed9948fee` is Passed: cold and warm each 915/915, zero failed, aborted, skipped, or inconclusive; the new `SelectedGenesisAndPreparedSessionRetainOneCoreEventHistoryAuthority` case passed in both. Manifest SHA-256: `27192bdece740d446296ad52a2a11c1c108d8029b4398814af4d67eab699a5c2`. Input digest: `6543c576ff19eb70fca719679243f8c27f46e6fdc7e9a1bef14e8f70fdc3c3ce`; all 507 manifested inputs matched current candidate bytes (none missing or changed). The manifest labels source commit a817ccf3 because validation preceded the test-only commit; the captured test source hash `7947d06ead06b1c9dbc942e5beccedb125cea4aa3fae12742c8c03a3ec01bc57` matches the file committed in 8a5730ee. Cold/warm TRX hashes match the manifest: `72050b97bbadb497216d7116aaf3a8d38263e6615eed63d3137ae8f1d6131d8a` and `8d4613e6f52952fe5657bb344f5129ea7dd7164b2051fd82445b7859e2a15be4`.

Retained Unity evidence remains applicable because the only post-Unity-run change is the separate .NET test file above. All five Unity manifests have stable before/after input digest `40e5a34f1a94909c19f2c5977ec712fbac5a598ebc3801818a7f81008d8b0807`; the full 1,960-file inventory matches the candidate worktree, and each original XML hash matches its manifest. Results: focused P12 1/1 (run `30771b384f114870b2b85577a21a248e`), bootstrap 39/39 (`03fee5682f3b4b57ba7188e9f9c4cad0`), runtime admission 228/228 (`b683d46e9ed34b0c8bfcb402a128ee4b`), full EditMode 3276/3276 (`e82846808801441bb92df4c73fc5a336`), and Smoke 5/5 (`acf743b193d34b8b8313ac3a7c685a78`); no failures, skips, or inconclusive tests. Unity does not execute the separate .NET CoreValidation suite.

`git diff --check 446b90c9..8a5730ee` passed.

## Limitation and scope

The original linked candidate worktree's `git status` returned a worktree/path-access error. I therefore make no claim that it had no unrelated dirty or untracked files. Exact Git objects and diffs remained inspectable, and all manifested source inputs matched the candidate. This review does not promote Core canonical or claim CORE-F completion; canonical promotion remains separate.