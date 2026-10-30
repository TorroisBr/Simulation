# Independent exact-tip review — Core Inventory/MoneyAccount census rebind

**Verdict: VALIDATED_CANDIDATE**

Review date: 2026-10-30
Repository: https://github.com/TorroisBr/Simulation
Candidate branch: codex/core/CORE-F-core-owned-runtime-application
Reviewed candidate: b8e3cbc57f6c0815d9d27ce3104e4cb00f399703
Candidate tree: fcd17cfdaa2c45fe8de13f053d6f6ac919387c46
Exact base: 978e0db0a7aa43ac9b8c71e34a0aca67d4a53acf
Current Core canonical at review: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea

This review was performed in a clean, separate E: worktree at the exact
candidate commit. The remote branch was at b3cc6a7f168b2eaeaa19e57e9398d3040b5bc5ff
when the review concluded; b8e3cbc remains its ancestor. This verdict covers
only b8e3cbc and its tree. It does not review or approve the later PersonStore
alias-pair change in b3cc6a7f.

## Scope reviewed

The complete base-to-candidate diff contains six files:

- Core's DailyV1RuntimeAdvanceStage adds a selected-NPC Inventory and
  MoneyAccount census rebind at the shared capture boundary.
- Two Core test files update their census fixtures and add rebind cases.
- VALIDATION.md, the normalized Unity result manifest, and its result-only
  archive record the candidate's evidence.

No ProjectSettings, Unity authoring/runtime source, unrelated user file,
or untracked/private evidence is in the commit. The original candidate
worktree's unrelated modifications and protected evidence were left
untouched. git diff --check passes.

## Review result

No actionable finding remains for this exact tip.

The operation order is sound: Core first applies the existing owner-thread,
profile, lease/scope, world-publication, and mutation-guard checks; it then asks
the host for its quiescent complete P12 census and captured mutation epoch;
it rechecks the outer scope; and only then obtains fresh Core witnesses for
Inventory and MoneyAccount. Token publication and continuation-save
revalidation consume this same boundary. Failed rebind returns no owner
vector, clears the epoch output, maps to OwnerCoverageIncomplete, and cannot
publish a completed-boundary token. The outer advance faults admission if
token publication fails.

For an all-selected roster, the rebind resolves the exact Core graph providers
and checks each selected row's section ID, Required role, schema, reference
identity, cardinality, and local revision before returning fresh snapshots.
Provider construction also rejects duplicate RuntimeIds and aliased
per-NPC Inventory or MoneyAccount instances. Inventory cardinality is read
without lazy allocation; MoneyAccount has the contract's singleton
cardinality. The implementation uses reference identity, not value equality.

The mixed-roster discriminator is valid under the existing full host contract.
The host path captures the sealed profile census through
ContinuationCensusProtocol.TryCaptureQuiescentOwnerSectionSnapshot, which
validates the registered provider families against their live rosters and
requires stable owner-thread/scope/epoch state. The P12 census contract and
downstream capture-token checks enforce the complete dynamic family key sets,
roles, aliases, and full profile shape. Within those preconditions, equal
Inventory and MoneyAccount family counts greater than the selected Core
graph's NPC count mean the host has additional compatibility NPCs. The
implementation preserves both complete host families intact in that case;
it fails closed if the family counts disagree. It does not mix Core rows
with Unity compatibility rows. This bounded rebind does not replace the full
89-family host census or claim to migrate its mutation epoch, admission, or
completed-boundary publication authority.

The revised P12F-without-City fixture preserves its meaning. It now replaces
the generic NPC-family rows with graph-bound NPC rows, including the exact
Inventory and MoneyAccount owners required by this new capture seam. It does
not add Core City binding to that fixture. The complete-owner-vector
restoration fixtures likewise use the actual graph. No assertion was removed
or weakened. New tests cover exact Core rebinding, whole-family mixed-roster
preservation, inconsistent family cardinality, missing/duplicate/substituted
rows, stale Inventory cardinality/revision, and stale MoneyAccount revision.

## Source identity and validation

The three committed production/test inputs match both the candidate blobs and
the validation record:

| Input | SHA-256 |
|---|---|
| Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs | 21a43e981f202366358d8abe722ae04d4adaa99a89a2e2d6b24af3444b9c1392 |
| tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs | 9a9213a323f2bc1d07edaf6e36c48d49a1b131e8efa3b9546623e0ac80c74a40 |
| tests/Simulation.Core.Tests/DailyV1CityEconomicOwnerCoreTests.cs | a070c8b6aa1f0cbee8c601237cdd418506922f8d96c3757d0b741989ec9721e4 |

- Official CoreValidation cold and warm runs: **944/944 PASS each**; each
  retained TRX parses to 944 Passed results and zero failures. The four new
  census tests pass in both runs. Protected manifest SHA-256:
  dde7637aa38bf46c00f7133f2fbffce68cf1e1fe978b244fa1f39ae5e9df4ec0;
  stable input digest:
  a518ff857ed86887c7f048060df6c6f9ad05f7684d84ac714ac5f06d840db2ed.
  The recorded runner source commit is the exact base 978e0db; its stable
  captured input hashes match all three candidate source/test blobs above.
  Cold/warm TRX SHA-256 values are
  78feea641b60c2892092fb067ed91ad5ad699aa3dcd4b84c6baa988e4548689d and
  bc8952717b0663279a07095b1781dfa90a494cb529714ee6b587d96adc8e664b.

- Focused Unity EditMode runs: **1/1 PASS each**, for
  SelectedDailyV1OwnerVectorAndSupportedNpcTransitionStayBoundToTheLiveProtocol
  and
  SelectedCrimeJusticeDailyStagePublishesP12WritesAndUsesWholeRosterCompatibilityFallback.
  Their protected run manifests, XML, and logs were found; each manifest is
  stable, the production-source hash matches this candidate, and original
  XML/log hashes match their manifests.

- Official filtered Smoke: **5/5 PASS**. Its protected run-manifest SHA-256 is
  01212ab59877af9b3c3c097833a56c966d5affddc477247845783c837cd99df3;
  original XML and log hashes match that manifest. The same five cases also
  appear as Passed in the complete EditMode report.

- Full Unity EditMode: **3282/3282 PASS**, with zero failed, skipped, or
  inconclusive cases; all seven LongRun cases are included. Unity input
  inventory was stable across 1,974 files, and the captured production source
  hash matches this candidate. Protected run-manifest SHA-256:
  b9dd7144f755c90f7249e546ccbeffa30d20c301227eed76a2fb7b13c41b0af5;
  original XML and log hashes match the manifest.

- The committed normalized EditMode archive was independently verified using
  the official Package-UnityValidationEvidence.ps1 -Action Verify workflow
  against the protected run manifest. Result:
  VALID: runs=1 entries=1; archive SHA-256
  c145413699bdb9d7bf21c3af83276b0d98c64f2f3c0beb107b5e2b1c8d112e0f.
  The committed public manifest SHA-256 is
  848978f639db28012a1f42ef6dec01802ecad3f707d2bf9eb05991d5cb969ba9.
  The archive contains one normalized XML entry with all 3,282 outcomes
  Passed, including both focused integration cases and all five Smoke cases.

The official package verifier passed. No new test execution was performed
during review.

## Limits and integration notes

This review validates only the bounded Core Inventory/MoneyAccount census
rebind at b8e3cbc. It does not claim complete Core ownership of all 89
families, runtime-wide epoch/capture ownership, P12-B completion, P12-A
readiness, detached .NET restoration, or complete CORE-F acceptance. The
separately advanced b3cc6a7f candidate requires its own exact-tip review and
validation applicability assessment before that later change is accepted.
