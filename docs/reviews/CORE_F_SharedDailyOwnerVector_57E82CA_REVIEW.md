# Independent exact-tip review: CORE-F shared Daily-v1 owner composition

## Verdict

**VALIDATED_CANDIDATE.** The independent review found no actionable findings on the reviewed implementation. This checkpoint does not promote Core canonical or close CORE-F.

## Reviewed identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Implementation commit: `57e82ca6106da0662b42f4306a719349dfedc065`
- Implementation tree: `ab0cbf997423ffd1b698543174ce5c4f5e748ad1`
- Exact base: `2251eb658da625f9f3819434e622a2128359860c`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Independent reviewer: `/root/p12e_graph_review`
- Remote candidate was verified at the implementation commit; Core canonical was unchanged.

The reviewed base-to-candidate delta is one commit and four files. The implementation remains a bounded shared runtime/application integration checkpoint.

## Review coverage

The review verified that the Core census composition routine is shared by live capture and restored-target admission. Its live path preserves the previous order: exact Justice validation, City and PoliticalDecision composition, P12-F composition, host-remainder selection, then rebinding of the remaining Core-owned families and final complete-vector validation. The existing structural contract checks the fixed and dynamic selected owner families, preserving the accepted 89-family census.

The added regression verifies the complete section-ID set, the shared opaque Justice records/receipt projection identity, owner-derived Justice record cardinality and revision, the exact-zero receipt sentinel, unchanged identity/cardinality/revision of a host-owned row, and rejection of a nonzero receipt revision. Existing P12 assertions and identity semantics were not weakened or changed.

The application binds one exact `DailyV1ApplicationHost` instance for its start lifetime and rejects rebinding. The host reference, storage root, file-operation object and opaque instance reference are not serialized. The existing P12 v2 descriptor continues to serialize its approved typed compatibility fields derived from the .NET host identity; those fields are distinct from persisting the host object or storage path. The Justice/P18 host-neutral identity decision is unchanged.

## Validation evidence

Official CoreValidation manifest status: `Passed`; stable source-input digest `65a90e0516e65867b1f9192bd4e47f4afaed08c2230199cbad3bfd843e1a0fa1`.

- Cold CoreValidation: **1,083/1,083 PASS**.
- Warm CoreValidation: **1,083/1,083 PASS**.
- Both new regressions passed in both runs.
- Cold TRX SHA-256: `c82e50d01f876025a0b2c03401ba3984d6bf99015ae40971358083377f65c375`.
- Warm TRX SHA-256: `f87490a44a94a5e3f9116ff192f1bda43aca47417b0702cfb621f4723ee0272a`.

Official Unity EditMode run `8b6bc6e5efd5483db20bc40cf66e89ab`: **3,303/3,303 PASS**, zero failed/skipped/inconclusive; the stable input snapshot digest is `ed7c81b0ddc5124021e0f6cdbfc1b2f4e702acb47edde3c2f7df63d7905a8aac`. All five Smoke-named cases were included and passed. The privacy-safe package contains one normalized report; archive SHA-256 `3bc9590b5e53887191cfe9912ef4dfd8062572302e92c1d1b0abe1deba812398` (174,884 bytes). Original XML SHA-256: `f930cee33fb244ff1be2c021a7042d854326a6783e87f6395891e8b742caeda3`. Package verification passed. Raw Unity logs and original XML remain in protected local evidence; they are not committed.

The official CoreValidation manifest and private raw reports remain in protected local evidence. Their source hashes match the exact committed files:

- `DailyV1Application.cs`: `7ee47deabc8833af24a6d84d0417b678400d93921e4edfff424ab44d75f086fb`
- `DailyV1RuntimeAdvanceStage.cs`: `55d6c1d7da35ed53b112e641d5109d6c775282b090dec9a41fc83b2938ec0494`
- `DailyV1ApplicationHostTests.cs`: `d3b98ec521ef7c3c0794fdd176cf600087ed2c2c7e4a9195d64b480672cc1e4f`
- `DailyV1ApplicationSessionGateTests.cs`: `a671cc9570ee466778ea1ace07c51a74652c0eee1dc77050379c26c617623892`

`git diff --check` from the exact implementation base to candidate: **PASS**. A filtered Unity report also passed 244/244, but its parent suite nodes retained the full-suite testcase count and the approved packager rejected that filtered artifact. The original report was preserved; the complete EditMode run above supplied the packageable regression evidence.

## Disposition and limits

This review covers only the shared complete owner-vector composition and exact application-host binding in this checkpoint. It does not claim public `DailyV1Application.TryStart`, complete Core-owned runtime execution, detached .NET host continuation, T/T+1/T+2 parity, CORE-F completion, or Core-canonical promotion.
