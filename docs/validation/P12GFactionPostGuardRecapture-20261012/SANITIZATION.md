# Validation artifact sanitization record

Only incidental filesystem path and local-user labels in generated validation artifacts were normalized after the Unity runs. Test source and production script content were not edited during sanitation. Gzip round trips were verified for all four compressed logs. The XML projection (root result/count metadata plus every test-case attribute: identity, result, duration, and assertion count) was checked before and after path normalization; the projection hashes below identify the resulting unchanged records.

Replacement labels: PROJECT_ROOT, UNITY_INSTALL, USER_HOME, UNITY_BUILD_ROOT, and LicenseClient-LOCAL_USER. Raw uncompressed logs are excluded from the candidate. A scan of all four XML reports and decompressed logs found no absolute drive-root path marker and no local account-name marker.

The original hashes below are the SHA-256 values captured from the validation artifacts before path normalization. Sanitized hashes are for files included in this candidate.

| Suite | Cases/result | Original XML SHA-256 | Sanitized XML SHA-256 | Original raw log SHA-256 | Sanitized compressed log SHA-256 | Stable XML projection SHA-256 |
|---|---|---|---|---|---|---|
| Focused | 1/1 passed | C721324D9F03EE420B94E0A99FA37B8FA51981BFB6F27DD3838304D52B365559 | D24172157D2B5052B9ABB1AE2B989D85CA1EBE1F4FC0A47DDB68676F6D60FC98 | CF75242272D21D401D3CB27895708D6FFFEF04DC8D9BA6441EF8D444486C36F4 | E0E683C7D7E6AF29B9DDE442685C659B3A2BA76FABDA9B42F2262ACE22AEEE25 | 673AB82FE95C04B232CC2726A44D27837CDBA8DA94FB9AA4CFA3B7C9AECEB3F4 |
| Admission | 200/200 passed | 7471752AB8EF1B6C50B3E3E8500895C2F9ECD3A61012B843FF8ADD14DBBB1A59 | 70601C7A185B2D57635D6C5EBA04886C5F9267E49886413D9768E476A4050FC0 | 53362BE6EE7A001B2FEC9B83330BCB46A397BAC4D461F0F027FED62E2943B41E | C48E1A6FBE08480F5C341811BF621DC38794D4B4427AF08AFBB955DD83859791 | B8CE096A6C5555027FFB18FB8331A92C2B5BDF64B6A7BD8DDB325466690E9FCD |
| ALL EditMode | 2879/2879 passed | 60128AA36022B3CEF042284FB25E710A8736440EAF1F205DA4FCB40CF7DA13F8 | A023FBCBD90762E20DA41E0F847340239FF97C9420DA13FA790CF318D5C1CD14 | C0041F4B2D76A17C1C370CED3101A7AC38E708ED293FB2808BE0E6455E1E7DB7 | E871CBB0BEEB749A8C8AAA052318233E08362BF915D0E5AFD88A3C49090604E4 | C537AEBF3D50EFAF8AA7C917E484385D66E5E5C4E2A752E388F5A158D225567C |
| Official Smoke | 5/5 passed | F11A90D0C1A6A38B46F4CB193C899A8FE120148E7ED4AC254B0B28D9F8D32122 | A208B6068FD0E540068E3C0798D1DCB36480B62C8DC150ACAC49AAFEF126EFF5 | 4F718D299309747FCC09832CFBFD6427047B5E61F931651508354E5656792EB0 | 6A26C9E38C241653F9050658983B40E3E31A7BFACF6DB394DBEA010E2D2AA165 | DB5E87FF13ADF107309212DA9BF07725EEFA623CB558BEA9968F98526A8B45B6 |

The focused test, full admission suite, ALL EditMode suite, official Smoke, and diff-check results retain their original counts and outcomes. This sanitation record changes artifact presentation only and does not substitute for independent review of the new test.