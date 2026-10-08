# P12-C Private Root Composition Validation

**P12 canonical base:** `6886f5876c756a7abb86c541f6d783da885941d0`
**Architecture authority:** `47eff220c7ce00f6e7c759bdc2b76780bb46f628`
**Composition design:** `b35a7f876097439794937f06197943bd6ea08189`
**WorldId addendum blob:** `4673a039ad36b2461bd593354d38f7222c70e2c2`
**Implementation candidate:** `f32f5d895deedff176c09dbcc19ed622dd5226ce`
**Candidate tree:** `fdf3d684af1b471643a7f940e128a90ad7445a9c`

## Source and test hashes

| File | Git blob | Raw SHA-256 |
|---|---|---|
| `Assets/_Project/Scripts/P12CContinuationRootStager.cs` | `4a04fd136e7402495530682b4721e2d48926bb06` | `9F217E1B2202664B89CDD78335A28A080E4839EB87B83742D4316E1E52693C37` |
| `Assets/_Project/Tests/EditMode/Editor/P12CPrivateRootCompositionTests.cs` | `4166552d24b1fc7b9fa0a834396c65fae6362c6b` | `4CC06A2DF193A8575E57D1DE7FB901FB0C4436C7C2ABA4D8EF1881C7202FFBAF` |

## Results

| Suite | Result | Raw XML | XML SHA-256 | Raw log | Log SHA-256 |
|---|---:|---|---|---|---|
| `P12CPrivateRootCompositionTests` | 51/51 PASS | `focused-world-identity/EditMode-20261008-021117-a31805bf0b424c48bb69df52d2936785.xml` | `8F71893A738D025D498275A51AAFBECDB21941561972A4BBED0AC426F5F8402B` | `focused-world-identity/EditMode-20261008-021117-a31805bf0b424c48bb69df52d2936785.log` | `320EBAFBCC103A02C9851FAEB65545063E22AF15DC03294D1D317C4A49A3CA42` |
| `RuntimeIdAllocatorCensusTests` | 3/3 PASS | `owner-suites/EditMode-20261008-021936-05173addc4b74878824d51233e68f70a.xml` | `E013178ED87EF0F1BE5DFE2A5BABAD36317612A205BFEED201A27AD000E07703` | `owner-suites/EditMode-20261008-021936-05173addc4b74878824d51233e68f70a.log` | `11635B3C058625916FB7B2CD9AB07D3F3AF954CBBEFED7139563D694DBE9A2B2` |
| `IdentitySequenceSnapshotTests` | 4/4 PASS | `owner-suites/EditMode-20261008-021947-3a8643c49f1e446982d10769675270cd.xml` | `AEF4AFFA4EDCDAF1F7FDFBDC5C6F1718C9A032EBF37676B49D06F719B1EAEAB7` | `owner-suites/EditMode-20261008-021947-3a8643c49f1e446982d10769675270cd.log` | `72F5003C38A39BA659F8BC71BF419108068227173AA72AEB767538826F5401B1` |
| `SimulationRecordSequenceP12InvalidationTests` | 17/17 PASS | `owner-suites/EditMode-20261008-021957-84e6f0225fe5438bbf299a6dd86af2fb.xml` | `36608BF4E49E8FD84A0AC13E9975552C70E17089D66A13AB31B1DDEE97BA6B74` | `owner-suites/EditMode-20261008-021957-84e6f0225fe5438bbf299a6dd86af2fb.log` | `63976EE2FC48A8178A41938F2DD8B97C6B7C297F782D8F787226639A70D5C892` |
| `SpatialAuthorityContinuationSnapshotTests` | 7/7 PASS | `owner-suites/EditMode-20261008-022009-626f337b7bac418c870b099ab2398946.xml` | `D4F913F7D8A89427D36C087EB9DBB321B695B5E123388AAD45861D1C08836E10` | `owner-suites/EditMode-20261008-022009-626f337b7bac418c870b099ab2398946.log` | `6E52C813A3A8A30DDAD9F636ACA828D69D9A81BD439D85DA2A111E17F7D89002` |
| `P12CP9GenesisManifestSnapshotTests` | 3/3 PASS | `owner-suites/EditMode-20261008-022020-8950af10d62e4163b292ea8b3369ab25.xml` | `8A222020A58577BA14ADCE77BBFD163A11F4589D4A9997F02BD0B894D9C9AAEB` | `owner-suites/EditMode-20261008-022020-8950af10d62e4163b292ea8b3369ab25.log` | `15124E9C944C0E9C1FF57A97FFBD4F7CAE792376E3512E96E003C3134E19096C` |
| `DeterministicRandomRootSnapshotTests` | 7/7 PASS | `owner-suites/EditMode-20261008-022031-083db7a64b12417796ac303cec34903f.xml` | `1D2A476425607075E136F398B3E3AB45DB4E5723D09094674C9E61D10369557A` | `owner-suites/EditMode-20261008-022031-083db7a64b12417796ac303cec34903f.log` | `E094DFBAD902FD6F5C6E3ADEC57421709434987B124BA922EBFE218C3ADC6204` |
| `SimulationBootstrapCompositionTests` | 26/26 PASS | `owner-suites/EditMode-20261008-022042-64c6295747094db2876ff0bc6ecc1d21.xml` | `3C96C928265836C0268BFEF2A7C1F0FBD482F75F75B7AF8E8211089D585E2AC9` | `owner-suites/EditMode-20261008-022042-64c6295747094db2876ff0bc6ecc1d21.log` | `5367EC0DA4F442F71E337849140AB29F912E4A5992A093E6E82989DE410D8F93` |
| ALL EditMode | 2535/2535 PASS | `release-all-editmode/EditMode-20261008-021211-bafe2e66193046a6b84d3a1cfe47a59e.xml` | `3446015A478C4AB337E97AD7C430DDDB541FB382E830668B537AF7C2C572B551` | `release-all-editmode/EditMode-20261008-021211-bafe2e66193046a6b84d3a1cfe47a59e.log` | `6D1CB6ADEC465E03E148386646B7F997F5076DA9E38E24047A7C466387F54233` |
| Official Smoke | 5/5 PASS | `release-smoke/EditMode-20261008-021248-aa6f6e297a054bfb940dc97c90d51dfe.xml` | `15E127774F7837951C1DB82475A15AEC27F25C245EF1B09C79BABDE98AE3511B` | `release-smoke/EditMode-20261008-021248-aa6f6e297a054bfb940dc97c90d51dfe.log` | `6AD6E4B87CE9705BB37918ADF3C43115B9AF8646A9491ECC73285CAA5D8EE159` |
| `SimulationRuntimeLongRunTests` | 7/7 PASS | `release-long-run/EditMode-20261008-021311-ebd8558a574e44ffa2437ba1dcdbbd08.xml` | `102F2972F6C49F8623EEE914B96C015838D198BC57A7620C35C55DE2F1FA5772` | `release-long-run/EditMode-20261008-021311-ebd8558a574e44ffa2437ba1dcdbbd08.log` | `BC71BC1D8EEC809291F2FC6773D56C718EA190C70214F3CBE4842EC2657BD401` |

`git diff --check` passed on the candidate changes. The raw XML and logs correspond to candidate code tree `fdf3d684af1b471643a7f940e128a90ad7445a9c`. The raw `.log` files are bundled in [`RawLogs.zip`](RawLogs.zip), SHA-256 `72A99E81AECC425E0E9FE64F693DD5109BD11969BE446982011E41090ABCBDC2`; each log path and uncompressed SHA-256 is listed above. Unity `.log` files are ignored by the repository, so the archive is the durable copy.

The seven P12-C owner/integration suites above were run serially on the same
candidate checkout on 2026-10-08 with Unity 6000.3.9f1 using
`Tools/UnityValidation/Invoke-UnityValidation.ps1`. The protected ProjectSettings
and unrelated `.meta` file hashes were unchanged by those runs.

## Scope retained

The candidate privately stages the existing allocator, record sequence, P8-A geography, P9-B manifest, deterministic-random root, and the already-published `WorldId` value as one all-or-none Daily-v1 root. It preserves the identity value without allocating a new world. P9 geography matching rejects missing, duplicate, contradictory, and malformed reserved provenance tags.

This remains a private in-memory P12-C capability. It does not add a serialized envelope, capture hook, active runtime publication, asset resolution, genesis execution, random draws, copied-save branching, P13 fork, P12-A readiness, or P12-G whole-graph validation.
