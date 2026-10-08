# P12-D City/NPC receipt-owner exact-zero witness — implementation review R2

**Verdict: NEEDS_CHANGES.** The two review-requested test additions are valid and cover the two principal findings from R1. One design-required failure case remains without direct test evidence: receipt-family reconciliation at a saturated shared mutation epoch.

## Exact identity and authority

- Candidate branch: `codex/phase12/P12DCityNpcReceiptWitnessImplementation`
- Exact remote candidate: `50c92061603ec34150cba0bf2a0f6bfddf13d40e`
- Candidate commit tree: `e9c79168e6f0770e3ce4a0c098273afbff9298b1`
- Candidate `Assets` tree: `d5bf7d95123d9188dd4b75500ec8416388cd0745`
- Current remote P12 canonical / base: `04e7c3f49a7c9690ebc091fcfc50f05ef67009d6`
- `merge-base(base,candidate)`: `04e7c3f49a7c9690ebc091fcfc50f05ef67009d6`; candidate is three commits ahead and a clean fast-forward.
- Prior code candidate: `95a5466a78432b245ff2ec835c726c06e795cfc7`.
- Prior R1 review NEEDS_CHANGES record: `331590bae2eaaac70758ab39c2a2078c9c07097d`.
- Accepted technical design: `5b10a58a680af9adcf56a585f6f2ac50f8e6b172`; exact design review PASS: `d4d2e343d3297d1dfc61b9ff52c9db25aba464e3`.

Remote `origin` was refreshed from `https://github.com/TorroisBr/Simulation` before review. The follow-up commits after `95a5466` add test-only coverage and validation documentation/artifacts; the only changed path under `Assets`, `Packages`, or `ProjectSettings` is `Assets/_Project/Tests/EditMode/Editor/P12DNpcReceiptOwnerCensusTests.cs`. Its SHA-256 is `55e5bac1a6dfe1e8117f5198f340ba90d4b956cf6c41c1265472376d2ea135f8`, matching `VALIDATION-FOLLOWUP-95A5466.md`. The P12-D runtime and provider implementation from the earlier reviewed code commit is unchanged.

## Re-review of R1 findings

1. **LocalObservation mutation after a completed token — RESOLVED.** The test `SelectedRuntimeVectorIncludesRequiredExactReceiptRowsAndEachReceiptMutationStalesToken` now runs for both `local` and `merchant`. The LocalObservation case commits through `NpcLocalKnowledgeDailyBoundaryStepProvider`, verifies the exact owner has cardinality/revision `1/1`, then verifies the previously issued completed daily token no longer validates. The test therefore covers an actual LocalObservation receipt through the token's census vector, not only a direct provider read.

2. **Failed receipt-family rebuild after roster commit — RESOLVED for a populated owner.** `CommittedRosterAddWithPopulatedReceiptFaultsWithoutPartialRowsOrEpochAdvance` runs for both owner kinds. It registers an NPC with a populated receipt owner and verifies the roster membership commit, unchanged receipt-provider count, absence of both new receipt section IDs, unchanged epoch, and `ProtocolFaulted` assessment. This covers the malformed/nonzero receipt-family rebuild path and its no-partial-publication/fault-closed result.

## Remaining required test gap

The accepted reconciliation contract also treats mutation-epoch capacity exhaustion as an atomicity boundary. The follow-up does not exercise this specific path. Add a focused roster-add test that establishes a valid empty receipt-owner family, sets the protocol epoch to `long.MaxValue`, then attempts a new NPC membership write. Assert the existing roster boundary's documented membership result, no publication of either new receipt section/provider or receipt-owner maps, epoch remains exactly `long.MaxValue`, and census admission is fault-closed. This is distinct from the populated-provider failure test: it must reach the shared capacity guard after valid receipt candidates are staged.

Source inspection confirms `TryReconcileSpatialKnowledgeRosterAndNotifyCommittedMutations` includes `npcReceiptOwnersChanged` in `anySectionChanged`, checks `mutationEpoch == long.MaxValue`, and only publishes the staged receipt-owner maps/provider list after that guard. That ordering is sound, but the accepted design's test obligation for this failure boundary remains unproven by the candidate tests. The candidate is therefore still **NEEDS_CHANGES**.

## Validation evidence checked

`docs/validation/P12DCityNpcReceiptOwnerCensus/VALIDATION-FOLLOWUP-95A5466.md` is bound to the unchanged implementation plus the exact test SHA above. I recomputed the SHA-256 values for its focused XML/log, diagnostic XML/log, ALL EditMode XML/log, and Official Smoke XML/log; each matches the manifest. The XML counts are focused 12/12, diagnostic 12/12, ALL EditMode 2581/2581, and official Smoke 5/5, with zero failed/skipped. The added tests are present in both focused and full-suite XML. `git diff --check` passes for base-to-candidate. The two dedicated filtered invocations noted in the follow-up exited before producing XML; ALL EditMode includes both suites and passed. No Unity tests were rerun during this source review.

## Scope and integration constraints

No P12-D production implementation or phase scope changed in this follow-up. The reviewed checkpoint remains limited to the two exact-zero per-NPC nested P18 receipt-owner census families and dynamic roster reconciliation. This does not complete P12-D, change P12-B semantics/status, or establish complete owner/epoch coverage, capture eligibility, export, hydration, P12-A readiness, P13 readiness, or Phase 12 closure. P12-A remains `WAIT_DEPENDENCY`; P12-B remains incomplete; P13 remains blocked.
