# CORE-F PoliticalKnowledge census and graph-retention exact-tip review

**Verdict: VALIDATED_CANDIDATE**

**Review type:** Independent, read-only implementation review. Candidate files and protected validation artifacts were not modified; validation was not rerun.

## Exact identities

- Repository: TorroisBr/Simulation, origin https://github.com/TorroisBr/Simulation.
- Candidate branch: codex/core/CORE-F-core-owned-runtime-application.
- Base commit: 7cd9fd97e6b0acdce966b1cee84a7c437d1c82bf.
- Candidate commit: 5a204c891bca3ab82f0fc59a87420f44ee176489.
- Candidate tree: 9f360e355254edb9d6826a223606c11e60b58393.
- Core canonical at review: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea.
- P12 canonical at review: 4cc31073f0ec6ba5c718084e4cebd6e914931d5d.
- Candidate ref on origin was exact at review. The review delta is the five requested files only.

## Scope reviewed

The complete base-to-candidate delta changes:

- SimulationRuntime.cs
- DailyV1RuntimeAdvanceStage.cs
- DailyV1SelectedRuntimeOwnerGraph.cs
- DailyV1ApplicationSessionGateTests.cs
- DailyV1CityEconomicOwnerCoreTests.cs

The bounded change binds the exact P12-F PoliticalKnowledgeStore to the selected runtime graph and includes its existing required census row in Core-owned P12-F census composition. Immutable NPC-membership graph rebuilds carry the same owner instance forward.

## Review findings

No blocking implementation or test-integrity findings.

The exact owner binding is fail-closed: the graph accepts the PoliticalKnowledgeStore only when it is bound to the graph's exact PersonStore, and refuses substitution once bound. Runtime census filtering additionally requires the graph owner to be the exact runtime PoliticalKnowledgeStore. If the complete Core-owned graph is not present, the compatibility path retains the complete host census instead of filtering the row and creating a coverage gap.

Capture checks the complete host vector and compares the PoliticalKnowledge row's required role, schema, exact owner instance, cardinality, and revision against a witness from the exact graph owner before removing the host row. Core capture and restore composition then emits the witness from that same owner, and host reintroduction of the Core-owned row is rejected. The day boundary, mutation epoch, sequence, owner mutation, revision, and P12 section schema are unchanged.

The restore candidate binds the staged P12-F PoliticalKnowledgeStore to the candidate graph's P12-D PersonStore before publication. No parallel owner or new political writer is introduced. P12 remains closed only within its accepted Daily-v1 scope; this review does not reopen P12, change its readiness, or imply P13 readiness.

The membership regression binds one PoliticalKnowledgeStore to the graph PersonStore and verifies that add/remove graph rebuilds retain the identical store and census owner identity. Session/census tests reject foreign owner identity, stale revision, and host reintroduction of the Core-owned row. Existing assertions remain intact.

**Non-blocking diagnostic note:** two P12-F prerequisite diagnostics still enumerate the prior owner set and omit PoliticalKnowledgeStore. This affects diagnostic specificity only; the checks remain fail-closed and no admission, capture, or restore behavior is weakened.

## Validation evidence

- Focused graph/census validation: 5/5, as recorded for this candidate.
- CoreValidation run 5006c5d54aee49c68e47aa7b956c6683: cold 988/988 and warm 988/988, zero failed. The runner recorded a stable input snapshot. Its source commit was f8c714d8561f42c1230662cf842b80fa8ef2e8ee with source tree 4c957feed1d788beb44fdcb9c308194a875a400a and working-tree input digest d4d15bd15381c0f610b8408709f9947a2bf707aec772e0095a76c795409753d4. The snapshot SHA-256 values match the candidate contents for the four Core/test files in this delta: DailyV1RuntimeAdvanceStage.cs, DailyV1SelectedRuntimeOwnerGraph.cs, DailyV1ApplicationSessionGateTests.cs, and DailyV1CityEconomicOwnerCoreTests.cs. SimulationRuntime.cs is outside the CoreValidation source inventory and is covered by the exact-candidate Unity validation below.
- Official Unity EditMode result: 3294/3294 passed, zero failed, skipped, or inconclusive. The validation worktree HEAD and tree were verified as the exact candidate commit and tree. XML SHA-256: 4a63e7bd0bc6ca0f40f4964245437ea00af0ca303b8968fbb0d4e276ba2b9402.
- Official Unity Smoke: the correct EditMode filter was Smoke; 5/5 passed, zero failed, inputStable true. Run ID: 2cdf1ea52e0d470a92168b4b15c4517b. Input snapshot SHA-256: 51ca5be671305ffe1cddb3d1859efcd4afd39c5f3009e08ec8920711c9c414e4. Manifest SHA-256: 73530fa0ea3b3f57c4920051271c28d52df8eb3e18fa732ddc2da0eadf97a1ff. Result XML SHA-256: f63120c77831a307802af6953cbaaf10f441a7178955a5992a98e11441b9f4ea.
- The earlier 2/2 differently filtered run and zero-test PlayMode-filter attempt are not counted as official Smoke.
- git diff --check passed for the exact base-to-candidate delta.

## Review boundary

This record applies only to candidate 5a204c891bca3ab82f0fc59a87420f44ee176489 and its exact tree above. It is a review result, not a canonical promotion, P12 change, or CORE-F completion claim.
