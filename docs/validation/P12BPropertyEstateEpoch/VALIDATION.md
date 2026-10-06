# P12-B Property/Estate mutation-epoch validation

Latest validation target: code commit `167a488c09fc7a2dc51517e1886250c43303bc20`, tree `7527439309841a8f68302e7c7630ddcecbc36b21`, based on P12 canonical `82735cb0ac7878fda0efd7d9e6a3029fe8a501f7`. Unity Editor: `6000.3.9f1`.

## Architecture §92A revalidation — 2026-10-06

The current Architecture branch is `codex/architecture/world-identity-projection` at `e16796014d348e3b59da7ed848101c4c03926ba5`. Its §92A requires a composable owner in an incomplete P12-B profile either to remain outside the selected composition by construction or to fail closed, with negative validation proving that rejection before promotion.

This candidate includes the existing Property ownership, transfer-history, and Estate owners in the selected Daily-v1 census and rejects any nonzero initial cardinality while registering those Required sections. Two new negative tests construct the selected Daily-v1 `SimulationRuntime` with prepopulated initial state: one has a Property row plus transfer history, and one has an Estate row. In both cases construction throws at the P12 runtime-admission bind because census initialization fails; no runtime is returned for bootstrap publication, and the supplied source owner contents remain unchanged. The tests do not add or alter domain behavior.

The latest Architecture §2 development-artifact boundary supports keeping `Simulation-DailyV1.asset` separate from the P10-A `Simulation-GeneralTest.asset`. The reviewed Property/Estate semantics and operation boundary remain unchanged. Other §91A/B, P17–P20, economy, and Ruin changes do not apply to this slice. Exact-tip implementation review was refreshed after these tests were added.

## Current results — code tip `167a488`

| Suite | Result | XML | XML SHA-256 | Log member in `architecture92a/raw-logs.zip` | Log SHA-256 |
| --- | ---: | --- | --- | --- | --- |
| `PropertyEstateMutationEpochTests` | 5/5 | `architecture92a/focused/PropertyEstateMutationEpochTests-20261006-162827-878.xml` | `FA4153A0D1CEDED769835B8DC69750AA01DF063CB7AE4B95EEE700EBA678065F` | `focused/PropertyEstateMutationEpochTests-20261006-162827-878.log` | `52613A80789D3C4AB55F49B2119931E542F4F36878E41BBC455FB5780E6DAE16` |
| `PropertyOwnershipCensusTests` | 2/2 | `architecture92a/focused/PropertyOwnershipCensusTests-20261006-162851-423.xml` | `22801CAA026713D04EBF4ABEE5E77203084F9C71AC73AB063B3646A08BEFB207` | `focused/PropertyOwnershipCensusTests-20261006-162851-423.log` | `152642BF94D067B4E15A4ED066FE8FCC84C6FCFECEBBD9DF6759AE8129A7D64B` |
| `EstateCensusTests` | 1/1 | `architecture92a/focused/EstateCensusTests-20261006-162901-304.xml` | `B3F68314D03028907F745B04EB53ECD1AB39AA6C06AFB82069C42A7F3EDEB843` | `focused/EstateCensusTests-20261006-162901-304.log` | `BA755E34012DA95BB08712B7A218A7034AE42F1FF7F2EB80002436A3B684728B` |
| `SuccessionIntegrationTests` | 21/21 | `architecture92a/focused/SuccessionIntegrationTests-20261006-162911-518.xml` | `E46D449A642EFD49939F1276B19C1EE2223C03BBD1176405DFDCD0F5ACA28BAB` | `focused/SuccessionIntegrationTests-20261006-162911-518.log` | `CA082875FE749EB54F80614B92E1A60C3C2D80DCE58F22CADEF228B4C304FA28` |
| `SimulationBootstrapCompositionTests` | 22/22 | `architecture92a/focused/SimulationBootstrapCompositionTests-20261006-162921-224.xml` | `81A3EB38D1388A795A5309F4D5EDDB648FD8FEC9B48C042C101603CA412A2137` | `focused/SimulationBootstrapCompositionTests-20261006-162921-224.log` | `806B00796B4DCF93D58F48F1DAF79A5F5147AA716C06BCD5302AD20798337DF8` |
| **Focused total** | **51/51** | — | — | — | — |
| ALL EditMode | 2415/2415 | `architecture92a/all-editmode/EditMode-20261006-162941-518.xml` | `135802605AC04F5B2977C7A142BAEE8EA74F3B51289893C8F4767275FB6B86EF` | `all-editmode/EditMode-20261006-162941-518.log` | `E628AA0024CBB2BA8811054A5418ADF0A68D64FC61640EBA4120A6F781C50365` |
| Official Smoke (`-testFilter Smoke`) | 5/5 | `architecture92a/official-smoke/EditMode-Smoke-20261006-163018-327.xml` | `D98640E4FD095B2FBDE4DED454166DFA57E803B7B682D55DE93DF83F356DC180` | `official-smoke/EditMode-Smoke-20261006-163018-327.log` | `44B899EBD9CA83612F8CF2E72BFB565EC52DFFC0FD04121E00E77A4351A607E8` |

`git diff --check` passed on the final code candidate. The hash of `architecture92a/raw-logs.zip` is `C16D87F4F46F04F198EB3B2CE04E9A1E80849621CAAC9447ABBEA26FF6C5A7A7`; it contains exactly the seven successful logs listed above. The exact XML files and archive members were checked against the recorded SHA-256 values.

## Initial implementation validation — code tip `955dc08`

The following results are retained as historical evidence for the original implementation tree. They are superseded by the §92A revalidation above for promotion.

### Original results

| Suite | Result | XML | XML SHA-256 | Log | Log SHA-256 |
| --- | ---: | --- | --- | --- | --- |
| `PropertyEstateMutationEpochTests` | 3/3 | `focused/EditMode-20261006-190900-ce85be59ab474ccb89bab7ca126b380e.xml` | `3920FDA17F80BFC100ABCC09AE18345F6818F35FB6D9AB5C619A7270ECFC5232` | `focused/EditMode-20261006-190900-ce85be59ab474ccb89bab7ca126b380e.log` | `250BA11A24071B6E349400694C3B4FBB99F98FD873086198C08A2E51AC72DDFB` |
| `PropertyOwnershipCensusTests` | 2/2 | `focused/EditMode-20261006-190916-0814b99ffcec49d7b23f2b537c4e1d26.xml` | `D69D9A58EE98916E6286B2E6AC0B95581E84A03B6D6EABD4278BC7ED690E649E` | `focused/EditMode-20261006-190916-0814b99ffcec49d7b23f2b537c4e1d26.log` | `E1B51019100680A03DE4F21FEB1ED4EA7DD817EEEADF938568A530578982612F` |
| `EstateCensusTests` | 1/1 | `focused/EditMode-20261006-190929-0087749411b7406da390137d7719bb46.xml` | `36F57207424EF000AFE4ACC29AF61836B14BB7CABFBAD1FC77616BEF4CAA3DFF` | `focused/EditMode-20261006-190929-0087749411b7406da390137d7719bb46.log` | `96C761C7B03EC769846DD3FF46193173D77A68DEA7D363FE794A3FB760043B0A` |
| `SuccessionIntegrationTests` | 21/21 | `focused/EditMode-20261006-190941-eaf1aa8132c24bd3a5176dac11ea7ba3.xml` | `D6FB6F55DCDA47B6B9BB5BDD92E0095EBBEB90812B59E1AAEE7C2C0BB8B6F1A9` | `focused/EditMode-20261006-190941-eaf1aa8132c24bd3a5176dac11ea7ba3.log` | `2FA79FD1860F2B74D79B4AC62B0F098FA0DCF33659F52E29CDECD3CD325FF921` |
| `SimulationBootstrapCompositionTests` | 22/22 | `focused/EditMode-20261006-191033-33db9e3237e14c1e85cce2d53240e8a0.xml` | `30CAE5BCC67C80C710D513E607381E5C12842CA9A9C912893EB3379D4A69A24A` | `focused/EditMode-20261006-191033-33db9e3237e14c1e85cce2d53240e8a0.log` | `6B2CB8E9B8D29FBC1DF87F55CA5BE1A0AAA48928106831BA117DA2905A82A213` |
| ALL EditMode | 2413/2413 | `all-editmode/EditMode-20261006-191056-de6afd789279441e9836a2c4a69c1950.xml` | `619B06B4D520E8884D572933967FC1C415C7D6E03E8A3E947435684C5C18DEE4` | `all-editmode/EditMode-20261006-191056-de6afd789279441e9836a2c4a69c1950.log` | `86E027369957D42486AA65B52D64401E5BF1A8CB9B840AF0D0AA6A28AB13AEE9` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 | `official-smoke/EditMode-20261006-191131-9d4075263d094d5fb54579c6f5afe791.xml` | `E412740E86D0574CF17B081BB8329BA6CC44531FBEE245B3548C8123CC049C6C` | `official-smoke/EditMode-20261006-191131-9d4075263d094d5fb54579c6f5afe791.log` | `DE7B16EED3C512E6A0553FB81893E47D6491B61E74DD878C696BB4E2B71BAC66` |

`git diff --check` passed on the code candidate diff. The exact Daily-v1 composition test verifies 242 sections, Required roles, schema v1, exact installed Property/Estate owner identity, and zero initial cardinality/revision. It also verifies the P10-A GeneralTest asset remains separately enabled.

The original raw Unity logs listed above are bundled unchanged in `raw-logs.zip` (SHA-256 `72924F3CE1AAD486015ED992DEB16038940B1018151C57F7C29FAEB71CB0FFC2`).

## Source SHA-256

These latest source hashes are computed over the exact Git blob bytes at code commit
`167a488c09fc7a2dc51517e1886250c43303bc20` (tree
`7527439309841a8f68302e7c7630ddcecbc36b21`), so checkout line-ending
conversion does not affect the source identity.

| File | SHA-256 |
| --- | --- |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `5EC58A518740FCBE0D5ABDFEAC2ADEA7253F82202F12FFB5D7786E69A40CF0D8` |
| `Assets/_Project/Scripts/Succession/SuccessionIntegrationContracts.cs` | `C9E17CE40F3E425ECBEAA6129BF3727C61CE28949BEFE8869D1EE13972E5378C` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs` | `62DAAD83BAD3940905561BD49ECC6FB512D672AB53B33F49D8B263035603EB2C` |
| `Assets/_Project/Tests/EditMode/Editor/PropertyEstateMutationEpochTests.cs` | `E648EC1D9BCBC339259819011771DA3F625E90AB3B01D12377CED2CEF0BA2148` |
