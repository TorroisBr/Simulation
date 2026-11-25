# CORE-F restored-source replacement admission correction

## Candidate and scope

- Repository: `https://github.com/TorroisBr/Simulation`.
- Candidate branch: `codex/core/CORE-F-p12d-core-hydration`.
- Implementation commit: `e164ccca18c6ce89c69628f148fa58593e748127`.
- Implementation tree: `204b0ec706bf62f3d009ef992ac8ee5d7f4d6b38`.
- Parent and validation base: `3cbc25c1ad02d0c679c854c6d2519a61ae2aa83e`.
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Previous exact-tip review: `NEEDS_CHANGES`; the finding and correction are recorded in the follow-up review document.

The source-session replacement check now accepts either valid current-token
provenance (`CompletedAdvance` or `RestoredContinuation`) while an exact
exclusive replacement reservation is active. Ordinary completed-boundary
admission remains restricted to `CompletedAdvance`; the staged replacement
candidate remains restricted to `RestoredContinuation`. The added Core test
publishes a restored source session, rejects replacement when a fresh source
census has a changed owner identity, preserves the old session, then accepts
the same candidate after the matching census is restored.

Only `DailyV1RuntimeAdvanceStage.cs` and
`DailyV1ApplicationSessionGateTests.cs` changed in the implementation commit.
Their validated SHA-256 values are respectively
`0062286a70b69b2cdbfeae19f6d59dc9a912576686bbfafa8634555c5edcf7a5` and
`6bf749516b4167de56a5a61650c3003678efb7d0a10a9ee1fa721311aa919a5f`.

## Validation

| Gate | Result | Run and input identity |
|---|---:|---|
| Official CoreValidation cold | 748/748 PASS | `10e460c6e160402da7159272bc6b0181`; 373-input digest `70a1577d09b4d2a86275e426d96f5dc723259fa3b904fb1b183808e6727ab7f7` |
| Official CoreValidation warm | 748/748 PASS | same run and input digest |
| Unity `SimulationRuntimeAdmissionTests` | 223/223 PASS | `625bf2cd1832498cb5b02017c42ce9ee`; stable input digest `a6d918c073051f7a3bbbb49c9950608532ebe551a2a7c12419a4e9e6f4c182fe` |
| Unity `SimulationRuntimeOrchestrationTests` | 13/13 PASS | `53c36e92dc8f4797a416a2839dc07375`; stable input digest `bac0ba84fc82d39def5a34e23f7d8cc0c9c6cfae0459e2f233ec4e6fb0ccbad9` |
| `git diff --check` | PASS | implementation commit `e164ccca` |

The two CoreValidation TRX reports each contain 1,058,421 bytes. Cold SHA-256:
`4c812564576757683363b9a8e7674e6c7839a58497c4f77be4cb27ad989a3329`; warm
SHA-256: `f323cb18cc6dd8b491ea7362fb177da64e8e5412ee7543c2dc3f2651db9f4924`.
The admission XML is 474,281 bytes, SHA-256
`1239ad2bacce25e7b0e696e46f3e10d951dbb397109a197a64805aad2cafffe5`; its
original log is 94,481,305 bytes, SHA-256
`19975b788cf78c0e0db237add54cf4412d760af67c56b91890a22c16f009a043`. The
orchestration XML is 12,391 bytes, SHA-256
`9f098977a8e97e52f57af552964e41cb85e73348432044b35ef9cde60f3feb66`; its
original log is 202,599 bytes, SHA-256
`c006fdd1a618ffaed587ba40ba56e26f2730171e683971250c1fd18cb2838752`.

All original reports, logs, and run manifests remain in owner-only private
retention. They are not copied into Git. Unity input inventories were stable,
and the recorded runtime source hash matches the implementation commit. The
CoreValidation checkout contained a Unity Library; no Library-independence
claim is made.

## Review and limits

Fresh independent exact-tip review of `e164ccca` passed with verdict
`VALIDATED_CANDIDATE`; see
`docs/reviews/CORE_F_CoreRestoreAdmission_e164ccca.md`. The earlier candidate
`f00c015` remains historical and unpromoted. This correction is an incremental
source-session admission fix; it does not move Unity P12 staging or storage
into Core, deliver detached .NET continuation, close CORE-F, or claim full
EditMode, Smoke, LongRun, or whole-package acceptance. Core canonical is
unchanged.
