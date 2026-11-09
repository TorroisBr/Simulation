# CORE-F selected NPC census fragment validation

Date: 2026-11-08

## Scope

Core reconstructs the selected NPC census fragment from the exact admitted owner graph. The Unity continuation protocol still captures and validates the full owner vector under the existing quiescence and shared mutation-epoch boundary. Core omits rows only when every selected family row exactly matches its Core owner witness. Mixed rosters or stale/substituted rows retain the complete host vector. P12-D receipt owners remain in the host remainder.

This is a bounded capture-composition step. It does not move the full 89-family protocol to Core, change the accepted P12 profile, claim complete P12 restoration, or complete CORE-F.

## Exact-source validation

The runs were performed in the candidate worktree based at commit 9768ad27b2a2362064a33d701752056075317fc7, committed tree 6c02cff863302fa946bc194a2c9cb9c99d708b28. The official CoreValidation source-input digest was 98947da88cad05755915539fa5490723c0c6df0a15eb2ef0a295e04fca5da2d3. The Unity full-run input snapshot digest was 9fbbf720cf1d2f1205118e1698c8165b9026f61303df17fe69cf46e6cfa7b2c6. All Unity input snapshots remained stable.

| Gate | Result | Run/evidence identity |
|---|---:|---|
| CoreValidation cold | PASS, 1,071/1,071 | Run 34c71a2ed31a4fcbb1aac927175a0e59; protected manifest SHA-256 f1cf75818c0b724af349de99d26058962c6313166537b18eedcc6b1e5744a13f; cold TRX SHA-256 bc62510fa5c2b8953602ce54706fe3e7b25b58cff7397e89c00cb92b5509c064 |
| CoreValidation warm | PASS, 1,071/1,071 | Same run; warm TRX SHA-256 8e0383083ee92f4a0ee6ed6e6e808964866bf3492d88286fb0581b07fb6ea467 |
| Focused Unity owner-closure suite | PASS, 1/1 | Run a3ff700617a742b9b976e80cbed1bca9; private run-manifest SHA-256 99e5959bd52bcaafd4d73af12935229de0d04a02b253c6cd1f5d448a91653066 |
| Full Unity EditMode | PASS, 3,302/3,302 | Run f67023929cee4b589fedbd14efb0ba20; private run-manifest SHA-256 1a7e8b056287a92d72ab2a1c1d07a6f88b2849ec90d3a4c7d27c2f2378efefa6 |
| Official filtered Smoke | PASS, 5/5 | Run 714839992d2d4369a23ff2deb94743c3; private run-manifest SHA-256 1d5f63e119f785efd9037c75e94a7083953581e2f92eca79c58255d0a4046180 |
| Normalized full EditMode evidence | PASS | [Manifest](UnityEvidence/coref-p12-npc-census-editmode-20261108.manifest.json), [archive](UnityEvidence/coref-p12-npc-census-editmode-20261108.zip); official verification passed against the protected source run manifest. Archive SHA-256 143e44ef79ccdb8d9b017652e445abb042da3b9895dbc0c55c338658a1454f1c; public manifest SHA-256 7063255c5b42dfeb66a5ce4717100ab99b167250b3043f2188b2b4b4143f5765 |
| git diff --check | PASS | Candidate implementation and evidence-record changes |

The full EditMode package contains the focused owner-closure case and all five Smoke identities. The official packager rejected a public package for the filtered one-test XML because its parent suite testcasecount did not match the filtered descendants. That was an evidence-package shape rejection, not a test failure. The original focused XML and logs remain unchanged in protected local retention; the complete report containing that test was packaged and verified successfully.

The protected original CoreValidation reports, Unity XML, logs, and run manifests remain in local owner-only retention. Public Unity evidence contains normalized XML only; no raw logs are included.
