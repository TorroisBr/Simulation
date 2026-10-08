# P12-D NPC receipt owner census validation follow-up

This supplement records the review-requested test-only follow-up on implementation candidate `95a5466a78432b245ff2ec835c726c06e795cfc7`. The earlier [`VALIDATION.md`](VALIDATION.md) remains the record for the original implementation and is unchanged.

## Follow-up scope

The test changes cover both nested receipt owner families:

- A committed LocalObservation receipt, like a MerchantTradeState receipt, invalidates a previously completed census token.
- Adding an NPC whose receipt owner is already populated faults the census reconciliation without publishing either receipt-family row or advancing the shared mutation epoch. The membership write itself has committed, as required by the existing roster boundary.

Only `Assets/_Project/Tests/EditMode/Editor/P12DNpcReceiptOwnerCensusTests.cs` changed for this follow-up. Its SHA-256 is `55e5bac1a6dfe1e8117f5198f340ba90d4b956cf6c41c1265472376d2ea135f8`.

## Current-tip validation

| Suite | Result | XML artifact | XML SHA-256 | Compressed Unity log | Log SHA-256 |
|---|---:|---|---|---|---|
| Receipt owner focused suite | Passed 12/12; failed=0; skipped=0 | `Raw/Followup-95a5466/Focused/P12DNpcReceiptOwnerCensusTests/EditMode-20261008-175813-e888689eb7194c1297a64e7f75105b8a.xml` | `33bf36afcf291167667100a892270af49bf80c9f2a01e87f85661096fa393221` | `Raw/Followup-95a5466/Focused/P12DNpcReceiptOwnerCensusTests/EditMode-20261008-175813-e888689eb7194c1297a64e7f75105b8a.log.gz` | `875f80a8166f255f99df857bf35694e3cb971aa127bd4c340b352e0ca5855a10` |
| Additional receipt diagnostic run | Passed 12/12; failed=0; skipped=0 | `Raw/Followup-95a5466/Diagnostic/EditMode-20261008-175740-bbb54f1d68344744859d06957518c557.xml` | `f1d7661d444ad2d903f8c2c10074b82adcf1ca8bf50a5c879830f2c88727295b` | `Raw/Followup-95a5466/Diagnostic/EditMode-20261008-175740-bbb54f1d68344744859d06957518c557.log.gz` | `9cf9df1daff8d47f69a6f30768fa8fccc76566a95bfe1e01050847aba1f2a35c` |
| ALL EditMode | Passed 2581/2581; failed=0; skipped=0 | `Raw/Followup-95a5466/AllEditMode/EditMode-20261008-180058-68320995255c4719aaee886ffa5842ac.xml` | `58ac153d448ff29ecd9bf4b8fe92f8a67a986539ca4c352a70ca30a5dc89e670` | `Raw/Followup-95a5466/AllEditMode/EditMode-20261008-180058-68320995255c4719aaee886ffa5842ac.log.gz` | `751f62d56caf93aa95986271c93af6ecd4074f16f8092fc39fd1f781c153a99b` |
| Official Smoke | Passed 5/5; failed=0; skipped=0 | `Raw/Followup-95a5466/OfficialSmoke/EditMode-20261008-180140-ad29a1064f1b432dbc577dd29d67f716.xml` | `5d5c5805e47b394302a144f8203982a37596ea1a77972adcba76a91cc577071f` | `Raw/Followup-95a5466/OfficialSmoke/EditMode-20261008-180140-ad29a1064f1b432dbc577dd29d67f716.log.gz` | `0b43cbce2eb68d901c1283e1087dfb61e691094898cd0c555da9170abc747bf7` |

`P12DCityRootOwnerSnapshotTests` and `ContinuationCensusProtocolTests` were also requested as dedicated filtered regressions. Their two structured-harness invocations exited with process code 127 before producing NUnit XML (`NoResultXml`); no tests ran in those attempts. The subsequent ALL EditMode run passed 2581/2581 and includes both suites, so they are covered by the complete current-tip run. These harness-startup attempts are not recorded as test failures.

`git diff --check` passed for the test patch and is repeated for the staged candidate. The compressed Unity logs were retained under `Raw/Followup-95a5466`; prior to compression, each compressed log was verified against its original raw log by decompression and SHA-256.

## Scope boundary

This follow-up adds coverage only. It does not change runtime implementation, receipt semantics, P12-B readiness, P12-A readiness, P13 status, export, hydration, or capture eligibility. No unrelated ProjectSettings edits or Unity-generated `.meta` files are included.
