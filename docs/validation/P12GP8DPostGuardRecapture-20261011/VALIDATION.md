# Validation record

## Candidate identity

- Base canonical: baf3ca2cbe09f0e568b9066644593ac4d98a849f
- Code commit: d09c0e3cb2cca908b9b4bb44c70c74d36a32f93a
- Code tree: 756e06f191f484e3b4e304c7ba35e6179c6b65a6
- Assets tree: bf9155bd4165aaf89c4bbac3cc0f94e6920bbfcf
- Test source: Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs
- Test source blob: fff1258a1977f9c640d5ff07028b09463889a099

The code and test source were unchanged after validation and sanitation. The final package modifies only evidence artifacts and their records; test results, assertions, failure counts, source identities, executable tree identities, and validation semantics are preserved.

## Validation results

| Suite | Result | Original XML SHA-256 | Path-only sanitized XML SHA-256 | Published XML blob SHA-256 | Original log SHA-256 | Path-only sanitized log SHA-256 | Final sanitized log SHA-256 | Final sanitized gzip SHA-256 |
| --- | ---: | --- | --- | --- | --- | --- | --- | --- |
| Focused P8-D test | 1/1 | 4820088c6512887aba6194ee1d1053185a7a8ab50c063f02b12f9e3bb312ed52 | c7ef8b9bff5e1f5fac3408b8aae01a5e75249f836c405236399500c7e1f3f355 | c333d1e8880207ef07fe8df8b30471652e03785fce5995c9026e8053ea844913 | 58844206530fad448a834ba2076a4f32f69381bed2a0861ab1b1d2cf723fba51 | b5cfc6b75d6849d71dc4b6ec156ff974fd7a3dec84801a03e5d44814c2622a9b | d3e609e1cd8503b85df70fb4f3a07bd592e8cd0f13c31c6f5fe9d59af9ac3092 | 24f7df42196353a2c608d6e5cd52a51a02c1343cffadb8d2637c1332234a8514 |
| SimulationRuntimeAdmissionTests | 196/196 | daf6b65afe0521fb7cc992da10de7cec05d478349241d19fece110d55ec0a885 | 281727fe138a1a5c3f537944908ee1ebb50834a8e8046e07361ca5278da66c21 | c3aa99cc5dac2745d5fdb239ba41b0ff34fbec7b825878bcbf7f6481e87918c3 | d0158f5c7009bcd1d6413a2980e79b29fea07567b131db398a31df027cfcfb97 | 1c5e67a5eef9566a29261da3d74d658444576ee367b92e40ec23ecc60599e770 | abf414b155719039d8ff635c729a9186505b01639a0cbecb93ae47ad8a76d85c | e22dd397f66eeb6ebcf7300ac6d52c9c84ce9080867e4b3ce2d77bd9536f5a2e |
| ALL EditMode | 2875/2875 | dc82a17f51ada52872eb44c32d26870d38e8712321cb3db0c2c18f9076ceb7ce | 9169b9db1ba08208b01f9db2af308a23cadd89cefe73a9716bedd3ea24d80db5 | 96fb2b16a2c471eedfbd9ab9f914b4dcfb6a785038db1c5c2effd4faae1a4661 | 6ae07320e3ac6d42a87a412765d8e935974e0ce6a46014807bf231626c562263 | d92117fc19323e7f81ce70f3fbe45ce948bd015308197121a21897adb07714be | 2c59e9dfda5efd2e472f0bf4c55b0bff85b55c221b975e85a3b7cd6f726f0632 | 3397c24c71a615a7aca51f18d189c74ea0fb800ade3b85fa3ff75c14850eb0fd |
| Official Smoke | 5/5 | f8f5aaca66e639ef237f2cf381c921b6caff21b7090f7a26fb6f4ce82bc06ae7 | a6efc2f7c7e9a3104f923ed6c8b517c94c56fa4ba3f1aa8d6a33d31446cc9104 | b0a7b0c89d6aaffa0a2158e5827de8992a2e38e47d0b6b38cabe41577f86bb1b | 372dd22cad4ff3863d0ac852ed20c494b5170298a4d01352bb92b904385c4140 | 86906b5c5c7edb69d98f13906941169cf49379597005e0a95096c2be558992da | 3c71d021613876bf15172fd36af930e3d44d5c4a002128bfd52ebe6e36f01b09 | d8a011eab70d1e456ef1b2c9935bdb093566d8209873d55e17d5a325a9c33288 |

All runs had zero failures, skipped tests, and inconclusive results. The exact Unity editor version was 6000.3.9f1. The official Smoke suite used the repository UnityValidation harness. Focused, admission, full EditMode, official Smoke, and git diff --check all passed.

## Scope boundary

The focused case adds a valid P8-D SpatialRouteKnowledgeStore observation to the staged target after CandidateGuardBound and before final target-owner recapture. It proves rejection with TargetOwnerVectorFailed / OwnerCoverageIncomplete, no recapture or boundary admission, preservation of the source session and token, and successful retry with continuation parity.

This is one bounded late-recapture witness. It does not claim P12-G completion, complete owner coverage, complete shared-epoch coverage, global quiescence, P12-A readiness, export, hydration, or P13 readiness.