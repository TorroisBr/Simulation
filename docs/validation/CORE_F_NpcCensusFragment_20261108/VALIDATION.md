# CORE-F selected NPC census fragment validation

Date: 2026-11-09

## Scope

Core reconstructs the selected NPC census fragment from the exact admitted owner graph. The Unity continuation protocol still captures and validates the full owner vector under the existing quiescence and shared mutation-epoch boundary. Core omits rows only when every selected family row exactly matches its Core owner witness. Mixed rosters or stale/substituted rows retain the complete host vector. P12-D receipt owners remain in the host remainder.

This is a bounded capture-composition step. It does not move the full 89-family protocol to Core, change the accepted P12 profile, claim complete P12 restoration, or complete CORE-F.

## Exact-source validation

The accepted CoreValidation run was performed at candidate tip 6208ed3be16d417505d02c403c2805d866e315ec, tree 198026a8d9b1663253a068806f0d2ec0a2b8ef20. The runner input digest was 190cd67f9df68951b0f16d9793a64e2b4509acab78dc4a028fce6b1ab2100eb2. The test source bytes matched the normal Windows checkout of the committed blob: 275,954 bytes, SHA-256 fa9b880b15984bcf0da666b5c68148af7978c8c357de7d26440655ad497e4c80; its normalized Git blob is 46e09cf896b88979c83a202f6fce2946c7d54e9b.

The Unity runs used the exact committed implementation/test source bytes in the candidate worktree based at 9768ad27b2a2362064a33d701752056075317fc7, tree 6c02cff863302fa946bc194a2c9cb9c99d708b28. The full Unity input snapshot digest was 9fbbf720cf1d2f1205118e1698c8165b9026f61303df17fe69cf46e6cfa7b2c6. All focused, full, and Smoke input snapshots remained stable. The existing local ProjectSettings change was present in that captured configuration and remained untouched and unstaged.

| Gate | Result | Run/evidence identity |
|---|---:|---|
| CoreValidation cold, exact checkout | PASS, 1,071/1,071 | Run 3f0f84cd59554fc3bb9560e8db43ba61; protected manifest SHA-256 2b6bf4a8c64f96f45e21ff5c596d649cb70a00fdc091982a5728beb437cf3741; cold TRX SHA-256 a7e5b208037d7f681ed6bf18295499c0348cc09b73589e49f9ccb33aacc63f0e |
| CoreValidation warm, exact checkout | PASS, 1,071/1,071 | Same run; warm TRX SHA-256 6f1d8a2798250a15a9801714d665cdd79b8545353cdbee339299ce4372d43aec |
| Focused Unity owner-closure suite | PASS, 1/1 | Run a3ff700617a742b9b976e80cbed1bca9; private run-manifest SHA-256 99e5959bd52bcaafd4d73af12935229de0d04a02b253c6cd1f5d448a91653066 |
| Full Unity EditMode | PASS, 3,302/3,302 | Run f67023929cee4b589fedbd14efb0ba20; private run-manifest SHA-256 1a7e8b056287a92d72ab2a1c1d07a6f88b2849ec90d3a4c7d27c2f2378efefa6 |
| Official filtered Smoke | PASS, 5/5 | Run 714839992d2d4369a23ff2deb94743c3; private run-manifest SHA-256 1d5f63e119f785efd9037c75e94a7083953581e2f92eca79c58255d0a4046180 |
| Normalized full EditMode evidence | PASS | [Manifest](UnityEvidence/coref-p12-npc-census-editmode-20261108.manifest.json), [archive](UnityEvidence/coref-p12-npc-census-editmode-20261108.zip); official verification passed against the protected source run manifest. Archive SHA-256 143e44ef79ccdb8d9b017652e445abb042da3b9895dbc0c55c338658a1454f1c; public manifest SHA-256 7063255c5b42dfeb66a5ce4717100ab99b167250b3043f2188b2b4b4143f5765 |
| git diff --check | PASS | Candidate implementation and current validation record |

The earlier CoreValidation run 34c71a2ed31a4fcbb1aac927175a0e59 also passed 1,071/1,071 cold and warm, but is retained as preliminary evidence only and is not counted above: its test file had mixed LF/CRLF bytes, so its raw input hash did not match the standard Windows checkout. Only line endings were normalized; the Git blob and source text did not change. The exact-checkout run above binds the committed test bytes and supersedes the preliminary run. The earlier reports and logs remain unchanged in protected retention.

The full EditMode package contains the focused owner-closure case and all five Smoke identities. The official packager rejected a public package for the filtered one-test XML because its parent suite testcasecount did not match the filtered descendants. That was an evidence-package shape rejection, not a test failure. The original focused XML and logs remain unchanged in protected local retention; the complete report containing that test was packaged and verified successfully.

## Candidate checkpoint and integration boundary

- Base commit: 9768ad27b2a2362064a33d701752056075317fc7.
- Implementation commit: e1629cad26addf64d6b149dd474de313316eee79.
- Implementation tree: eeb99ea6579eb7725b75c6686636ec42ac56d548.
- Validation-record tip before the exact-source addendum: 6208ed3be16d417505d02c403c2805d866e315ec.
- Changed implementation/test files: SimulationRuntime.cs; DailyV1RuntimeAdvanceStage.cs; DailyV1ApplicationSessionGateTests.cs.

The change transfers census-fragment reconstruction for the selected NPC owners into Core. It preserves section IDs, roles, schemas, exact owner-instance identities, cardinalities, revisions, the completed-vector contract, and the mutation epoch supplied by the existing Unity continuation protocol. It does not alter daily execution, RNG, operation ordering, owner writers, or persistence encoding. Mixed rosters and stale/substituted host rows retain the whole host vector for existing P12 validation.

The complete ContinuationCensusProtocol, quiescence admission, and epoch publication remain host-owned. P12-D receipt owners remain host residual rows. This checkpoint does not establish the full 89-family protocol as Core-owned, detached restoration readiness, or CORE-F completion. Next integration work must move the remaining capture/restoration authority without duplicating the protocol or weakening the current boundary.

The protected original CoreValidation reports, Unity XML, logs, and run manifests remain in local owner-only retention. Public Unity evidence contains normalized XML only; no raw logs are included.
