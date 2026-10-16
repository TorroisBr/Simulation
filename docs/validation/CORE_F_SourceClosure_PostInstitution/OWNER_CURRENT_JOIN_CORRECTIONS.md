# CORE-F current owner-join corrections

This additive correction set is tied to production source commit
`ca55c3f9fa4d9c99e6a47a25adbbd1956ea04a1d`, tree
`e0d3de22bdc7bcc16e16a925f6f8c38f6a6397a5`, present unchanged at current Core
canonical `1998974515b6357c308d861fb608f2db5071a63a`. It supersedes only the
identified stale or incorrect current-source joins; the prior promoted
validation artifacts and their historical source identities remain unchanged.

| Family / artifact | Stale or incorrect join | Current-source correction |
| --- | --- | --- |
| All 89 owner family anchors | `OWNER_WRITER_MATRIX.csv` repeats the historical `P12AContinuationImage.cs` line and blob anchors from the earlier inventory revision. | [`owner-current-source-anchors.csv`](owner-current-source-anchors.csv) records each current fixed/prefix/transition authority line and the current `P12AContinuationImage.cs` blob `b324bdab7d4432b36e923b767a5b81bb129dc46e`. The generator fails if keys are missing or the pinned source differs from `HEAD`. |
| `p12f.npc-decision-occurrence-receipts` provider | The old crosswalk points its provider declaration to `OwnerSectionCensusWitness`. | The actual source owner/provider is `NpcDecisionRecorder` in `Assets/_Project/Scripts/DecisionRecords.cs:800`; `GetOccurrenceReceiptCensus` is at line 835. Source blob: `fbae07f2ca93a9f7242e9fb42e0d44c31232981f`. The runtime identity map binds this row to the recorder's owner identity. |
| `p12f.political-knowledge.holders` codec case | The old crosswalk points to `P12ADRootOwnerCodecTests.cs`. | The actual codec case is `P12AContinuationCodecTests.PoliticalKnowledgeCodecPreservesTypedHoldersObservationsAndProvenance` at line 640, with encode/decode assertions in the same case. Test-source blob: `a484f7eaf0d2f163d4c5d3fcac743931576b1143`. |
| `p12f.expeditions` encoder | The old crosswalk points to `TryEncodeTravelParties` at line 15. | The correct encoder is `P12AFCommitmentOwnerCodec.TryEncodeExpeditions` at line 104. Source blob: `63a2906f2021873a7a071f27b196e7a0fe8d6d44`. |
| Fixed exact-empty composer case | The old matrix points to `P12AContinuationCodecTests.cs:874` for `DailyV1ImageComposerBuildsExactEmptyRowsAndRequiresCompleteOwnerSections`. | The current method is at line 1027 in test-source blob `a484f7eaf0d2f163d4c5d3fcac743931576b1143`. The exact-empty codec remains `P12AExactEmptyOwnerCodec.cs` blob `be988ae67d88ab477a949982378a7132b4886a26`. |

These are anchor repairs only. They do not change an owner role, invent a
writer, assert exact-zero where the contract does not require it, or change any
production/test source.
