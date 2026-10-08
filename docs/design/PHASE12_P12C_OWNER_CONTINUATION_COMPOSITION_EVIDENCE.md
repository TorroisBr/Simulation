# P12-C Owner Continuation Composition Candidate Evidence

**Checkpoint:** P12-C — identity, genesis provenance, and deterministic roots.
**Status:** Implementation candidate; exact integrated-tip review is pending. No canonical promotion or Phase closure is recorded here.
**Canonical base:** `codex/phase12/canonical` at `82125b8e20ca997226ede0069bc875472cf90430`.
**Validated implementation commit/tree:** `4001c2471df9088e2e51e7a1420bfa0e9b385b88` / `dc54747a3c64ab9189c1425e0a6d92eba38f33b7`.
**Integration branch:** `codex/phase12/P12COwnerContinuationIntegration-20261007`.
**Validation environment:** Unity `6000.3.9f1`; repository harness `Tools/UnityValidation/Invoke-UnityValidation.ps1`.

## Bounded contents

This composition carries the three accepted P12-C owner slices:

- P8-A: detached snapshot and private reconstruction for the selected profile's one Hex, one anchored Location, scale, and supported spatial facts.
- P9-B: detached copy and direct staged reconstruction of the existing Daily-v1 genesis manifest, retaining the full `OutputOwners` field and the producer's canonical provenance records as separate ordered lists. The existing fingerprint producer omits `SpatialAuthorityStore` from its `output-owner` records even though the manifest `OutputOwners` field contains it; both existing values are preserved without changing P9 fingerprint generation.
- Deterministic root: detached root state for the selected deterministic random source and its effective seed.

This candidate does not implement the P12 envelope, complete profile owner census/export/hydration, capture eligibility, global quiescence, or P12-A. P12-C remains partial until its other accepted identity roots and composition gates are complete. P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked. P12-B's existing bounded completion is unchanged.

## Source and test blob hashes

These identify the exact raw file contents in validated implementation tree `dc54747a3c64ab9189c1425e0a6d92eba38f33b7`. `Git blob` is the repository's 40-character object ID; `Raw SHA-256` is SHA-256 over the blob bytes returned by `git cat-file blob` (without Git's object header):

| File | Git blob | Raw SHA-256 |
|---|---|---|
| `Assets/_Project/Scripts/DeterministicRandom.cs` | `337af6064d470c7e4418ef6f6c3a2cad35f5e232` | `0b7eaeaf35b647fe08c3f7fb8adaed20b448ef06e51b509004dca931f903f5d5` |
| `Assets/_Project/Scripts/P9GenesisManifestContinuationSnapshot.cs` | `05adc7fb3a843a77303de55861609f0bc828375b` | `6331c4f8d490610c986fef7ae92c908fa4014ed7293dc9b5eca5d3db38ec212c` |
| `Assets/_Project/Scripts/SimulationGenesisPipeline.cs` | `7c225bc066da5f2a8ad838cd1633920b384de917` | `ce6b84870d4c2a8519892bac08024c91774c6599b852e5d96efac9f96ae0d6c8` |
| `Assets/_Project/Scripts/SpatialAuthorityContinuationSnapshot.cs` | `568a7a36f87d781a51bb29b1307a54411ed824c1` | `5454d48509797d2b8e135e62e8e8d07ecf7ede473368f526baa2ef209d42e83f` |
| `Assets/_Project/Tests/EditMode/Editor/DeterministicRandomRootSnapshotTests.cs` | `f7f9140700c20a3000da2056669f4f712e3d3dd9` | `0c22246eb190244a93de2b66672da319b6be14d4eef10b5bcffdb237460702e8` |
| `Assets/_Project/Tests/EditMode/Editor/P12CP9GenesisManifestSnapshotTests.cs` | `a75eb8c084bab73ccb498821846a0aa1ebc4d9e0` | `e945cac50d153e74905cf854656f3c18053e3710eee145d79aeccf9de07a780b` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs` | `4e11a58562edb0dcda8f5fe070dc3e8dcbabf1ad` | `807163db7bacb3e6539b0d9323a5a791391ad1343826eb82391ec55551de352a` |
| `Assets/_Project/Tests/EditMode/Editor/SpatialAuthorityContinuationSnapshotTests.cs` | `ec8196dcc513074d5ab8b4e895a974c4a757c03f` | `4048ee6200b0eaac1a929353956bf91349dfb460cf966a712c237345247bcc98` |

## Validation

All result XML and log files are retained outside the Unity project's transient `Temp` directory at:
`.worktrees/p12c-validation-evidence-20261008/`.
The following SHA-256 hashes identify the exact files. Each run used the validated implementation tree above.

| Suite | Result | XML / SHA-256 | Log / SHA-256 |
|---|---:|---|---|
| `SpatialAuthorityContinuationSnapshotTests` | 7/7 | `EditMode-20261008-010719-74d2010fc8ec4b47b74c1b2d49d7236a.xml` — `28E7D224C460026812328C47C9E4812227A11C2BEE2BECE9E11F7E5776C7798F` | `EditMode-20261008-010719-74d2010fc8ec4b47b74c1b2d49d7236a.log` — `DA01A7CD068AA0A3D3934E73DB8FA7ADC21B9E354CC0A46E856230FF09775CA6` |
| `P12CP9GenesisManifestSnapshotTests` | 3/3 | `EditMode-20261008-010735-e5335f61256943ea8b786e202e2fe0a0.xml` — `B62AA1476D624420E8D4E8980E5B5119247C88774C21E982508B21FE5F88B5CE` | `EditMode-20261008-010735-e5335f61256943ea8b786e202e2fe0a0.log` — `CB828ACECAD6485F4BFF5AA6473EEB1A48566A69C2842D76018DADD7FF703CDC` |
| `DeterministicRandomRootSnapshotTests` | 7/7 | `EditMode-20261008-010752-abe725d44a9f4f529e057d088141de5e.xml` — `0C765DCC841BCFBDBB7FB8695EAC3954B137A2065901E6C91B857248EDFA3C74` | `EditMode-20261008-010752-abe725d44a9f4f529e057d088141de5e.log` — `2BBC64ACF303FFE917DB60732F793A5CB6121FB1327C11CA1747F489AB7000C1` |
| `SimulationBootstrapCompositionTests` | 26/26 | `EditMode-20261008-010806-8d6ebd8bd407486db2979e47a351b49e.xml` — `39B041A51B650EA93D36D4D7C2A5BCCC3B6EA378BCF53F352E39ACC83A2E3F6B` | `EditMode-20261008-010806-8d6ebd8bd407486db2979e.log` — `E11E91A433A5370ABAAE058C65076E8D7B70FE0A31AA3C37EB5DF73F04C743BA` |
| ALL EditMode | 2484/2484 | `EditMode-20261008-010826-4b88fdfba62045b2bdbb62995836ca7a.xml` — `51ED828B8D836FEEE0A006B00229D0EB8290E8185D1CC1D7D8AF343AD3B10E8C` | `EditMode-20261008-010826-4b88fdfba62045b2bdbb62995836ca7a.log` — `0F2DDB0AE3F209252B75942E99B96DF67E1A54DC25D03C729CA85C1621BAF7DD` |
| Official Smoke | 5/5 | `EditMode-20261008-010906-9a7a6f6cbda144549cc0aaaf19498b94.xml` — `623C831456596DAF9AE1650AC018D0534D7549784BFE5E22698B152AD7E9E2F4` | `EditMode-20261008-010906-9a7a6f6cbda144549cc0aaaf19498b94.log` — `81E413F0C1017AA0BD11F64B969768BE52EE652284EAE3F99D11955DA8938A62` |
| `SimulationRuntimeLongRunTests` | 7/7 | `EditMode-20261008-010930-a8bbc0470ebd44d0b2061eb5c2363987.xml` — `43580B42DD42BF201C5BE65351C320BB03996A79F324538356F05C2F7AC26B5A` | `EditMode-20261008-010930-a8bbc0470ebd44d0b2061eb5c2363987.log` — `423E1320D4F8E6CD6676AD90972778DD5E3AF444198DF23ECB088B88FEC97B9A` |

`git diff --check origin/codex/phase12/canonical HEAD` passed on the validated implementation commit. The independent exact-tip review of the integrated candidate remains a required next step.

## Workspace notes

Unity validation left local ProjectSettings changes and three untracked `.meta` files in this isolated integration worktree. They were not staged or committed. The main worktree's pre-existing ProjectSettings edit and untracked files were left untouched.
