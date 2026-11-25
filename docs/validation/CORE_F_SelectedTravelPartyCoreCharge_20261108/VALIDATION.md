# CORE-F selected TravelParty Core charge

**Status:** validated review-fix candidate; independent exact-tip re-review pending. This is not Core-canonical and does not complete CORE-F.

## Candidate identity

- Repository: https://github.com/TorroisBr/Simulation
- Candidate branch: codex/core/CORE-F-restore-stage-transaction
- Implementation commit: f103dcfc9d00b3b1c1cccbf3ceb6faf901cc2aff
- Implementation tree: aad87ecdf157c4388e4fa39cac6a4fbe0a80c49d
- Parent candidate: 7c3c0050cf7f7fc9dc4a7b454c7be29fd2298bcb
- Core canonical: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea

The remote candidate branch matched the implementation commit at verification. The canonical ref was unchanged.

## Scope

For a TravelParty whose roster is wholly bound to the selected Core runtime owner graph, the shared account charge now runs through the exact DailyV1NpcRuntimeOwners.MoneyAccount owners. The operation preserves member order and IDs, rejects absent, foreign, repeated, or invalid owners and invalid amounts, preflights debit eligibility, then applies the existing TravelCharge / ExplicitSink semantics. If a later debit fails, already-applied debits are compensated through those same owners.

TravelPartySystem selects this Core operation only after confirming every party member is bound to the exact same selected graph and exact NPC owner instance. The existing Unity transaction path remains for mixed or unselected rosters. Existing travel-start preparation and projection remain host responsibilities in this slice.

This checkpoint does not move the complete TravelParty stage, action dispatch, daily execution, capture boundary, or application lifecycle into Core. It does not deliver DailyV1Application.TryStart, detached restoration, a fixed .NET host, separate-process continuation, or full CORE-F acceptance.

## Validation

| Gate | Result | Run |
|---|---:|---|
| Official CoreValidation cold | 1,064/1,064 PASS | 5168dd4e478c4a6da192a7498e4f49d5 |
| Official CoreValidation warm | 1,064/1,064 PASS | 5168dd4e478c4a6da192a7498e4f49d5 |
| Unity P12TravelPartyStartOperationTests | 16/16 PASS | c95c3b7ee1944498bb762270df25801c |
| Unity EconomyTransactionTests | 59/59 PASS | 97ffe9df7d204c6ab97f1bed991d687b |
| Unity TravelPartyCensusTests | 10/10 PASS | a8a7895b0427440dbebcf8d0c8ca243b |
| Official Unity Smoke | 5/5 PASS | 46c674c1df554915b256fa99fe745fad |
| git diff --check | PASS | implementation commit |
| Unity P12TravelPartyStartOperationTests (review-fix source) | 18/18 PASS | d8f9a18ff7e048b4b16606aa644d7c04 |

CoreValidation working input digest: 9928f9a90a200c79be0755a6a6120e33ad4c7e8af1e426d22580a593fd4301a5. Its committed base was 7c3c0050cf7f7fc9dc4a7b454c7be29fd2298bcb. The exact hashes of the added Core operation, its test, and both project-file registrations match the validated inputs and implementation commit. CoreValidation reported worktreeLibraryAbsent=False; this evidence makes no Library-independent .NET claim.

Unity validation inputs had stable digest 48bb0a305e2b323cf86cee0f18677901a2c515825c9af79b2d1d56cd5379afba across the runs. The protected original XML, logs, and run manifests remain in private evidence retention, keyed by the run IDs above. No full Unity EditMode suite or full CORE-F validation is claimed.

## Independent review finding resolution

The first exact-tip review of the implementation requested end-to-end coverage through `SimulationRuntime.TryStartTravelParty` for an all-selected roster and an explicit mixed selected/unselected fallback case. The new tests now use the real authored `UnityBootstrap-Daily-v1` bootstrap, exact selected runtime owner graph, registered travel route, and P12 runtime. The all-selected test reaches the selected Core charge path and verifies account-owner revisions and one admitted P12 epoch. The mixed test registers a compatibility NPC through `TryRegisterNpc`, installs its reciprocal City presence through `SetCurrentPresence`, then verifies the mixed roster starts through the retained transaction path with one P12 epoch and a valid final census.

### Focused validation recovery

| Run | Result | Disposition |
|---|---|---|
| `acbe261137394870b2fc3bf16ff98cb5` | 16/18 PASS; 2 failed | Superseded synthetic-profile fixture: it did not provide selected NPC owners. Failures were test setup, not production behavior. |
| `0edba563c87242c388957ec1cfbe5d53` | NoResultXml | Superseded compile failure: the test used a nonexistent lookup name; corrected to `TryGetNpcRuntime`. |
| `d8f9a18ff7e048b4b16606aa644d7c04` | 18/18 PASS; 0 failed/skipped/inconclusive | Final official focused result after using the authored bootstrap fixture. |

Final-run input was stable. The original manifest, XML, and log are preserved in private validation storage, unchanged. The final run reports both new test identities as Passed.

| Artifact | SHA-256 |
|---|---|
| Final run manifest | d815f038145a3af40db2f7f56f00357d60fef9cd4ce4b3a68acbb7f330b1777e |
| Original XML | 6e9a61beb892319a209a5bdf975e759f628c91d5deb4cf585a42c265e37d5b73 |
| Original log | 6bfd325ec80a321653b9f139bb91e000cccd5e85991d71122485a613a2ac5d6f |
| Test source SHA-256 for the final run | acb26b26a0add588e84fb5b1b7e5dbfca697cafaaa9bdb35594fb3f2f0d6a3ba |
| Test source Git blob for the final run | be0c4b9c313e05d996bd74cd4bc54a31e2add28d |

## Evidence integrity

### CoreValidation

- Input digest: 9928f9a90a200c79be0755a6a6120e33ad4c7e8af1e426d22580a593fd4301a5
- Private runner-manifest SHA-256: e9af721870b2038ed0009886ff26559020798ba637270a21cb130cf1bdb78d34
- Cold TRX SHA-256: 65cc9de13144264ae94f5c882d51f58a6e2073ec2491e4d5652cc0a48ba154df
- Warm TRX SHA-256: 948cbedb958f568d1c2fa5355eb05cc83d8e2401a1685aca365bf14fe80d1682

### Unity

| Filter | Run manifest SHA-256 | Original XML SHA-256 | Original log SHA-256 |
|---|---|---|---|
| P12TravelPartyStartOperationTests | 2138badad4b8ea25fa09784fdb4887fd385abf81af1009734d982ef8b76e759d | 2755fee4dc96dee00f8725451a66a0ffea416b451be9e1861d3c8a9f59669723 | 7120b76ef82b330d056675826bd84cb2e1234541236bb014ecc0e7b6f5a1514f |
| EconomyTransactionTests | 7afa4342885aef69ca4e65be180655aeb2e5910c29218d97f425480a9af039eb | c0f2cd37044aecb0f0050ed8f6b69b89e55b8bcc4d9c29c7b645c2dd3b04785e | e06978c8eea6ea7efe3f1b37934adcb0dd6652f0ca3cd2c820b34300339fb883 |
| Official Smoke | c86e3d48bd33f9d3b1fcf1c7299371cef16c859485bfdb5301ebd5ce4a7619b3 | 6db896862652f50ba1d9c734e89605dad748fb3cd28edbc85c2f2f02868fb3a1 | 81198b74c7e2423e35a31e10c8beeb1e0a7c506479447bbcf03b67b54b4a787d |
| TravelPartyCensusTests | a451047159acd8ce892ca88b85055ed5deecdf08b532ee107d6db20e413a1e03 | 403ed1b19357748c493ef48a95585ff03ecd31446cbe9d4750c4488e9b0eead2 | 8e2689df8120b4b339a38ef55275b792102b78cc0ae152f2869b9b69422924bd |

The Unity input digest was stable before and after the runs. Original XML and logs are retained unchanged; this record stores their identities and hashes, not copies of the private artifacts.

## Source identities

| File | SHA-256 |
|---|---|
| Assets/_Project/Scripts/TravelParty.cs | e39f48397f58b76cdae0f8b34a16e5044386cb854ce7d202256fe0136410b42b |
| Packages/com.simulation.core/Runtime/City/DailyV1SelectedTravelPartyChargeExecution.cs | 35c10bc4ea1c12c0ebe32f8101cefea25e936270a7bb75599e90fd1403f3a414 |
| Packages/com.simulation.core/Runtime/City/DailyV1SelectedTravelPartyChargeExecution.cs.meta | 86997d36185ea82319ef209fbc1a37a91a5859c42c8754b14fd0540711e8343d |
| src/Simulation.Core/Simulation.Core.csproj | 4d8460fbe762c141074620e0a43ce0495d9c7a25f544db712f7d664736ac05c4 |
| tests/Simulation.Core.Tests/DailyV1SelectedTravelPartyChargeExecutionTests.cs | bfda8a239743b4d76b3c7cdc3d2656d8de4d6f47d8296043a00e1203b11feaeb |
| tests/Simulation.Core.Tests/Simulation.Core.Tests.csproj | f30689a0c51c21b184fa6ef814cc6104cc0e9fc7ba2dcac93960eeba69f68533 |

## Limits and preservation

This is an incremental Core authority transfer for the selected TravelParty group charge only. It is not a claim that Core now owns the full action, TravelParty execution stage, runtime session, or persistence lifecycle. CORE-F remains open and unpromoted. Independent exact-tip review is pending.

The pre-existing ProjectSettings/ProjectSettings.asset change, private/untracked evidence, and unrelated files were not staged or modified.