# P12-E Justice Records Owner Snapshot Validation

Date: 2026-10-09

Base: P12 canonical `565f6b5817e0126c2f89e0a1452b09a8fb03cc19`

Design: `fb16e4f` (exact-content design re-review PASS; durable record `f2f4615`)

Implementation/test tree: `ed209e7c9d707f396a93de06cae2e07f0e00bd4e` (`e0b464a`)

## Scope

This candidate captures and privately stages only ordered Justice wanted-record and prison-sentence values for the current `JusticeSystem` owner. It preserves exact Justice local revision, target NPC/Person identity, City identity, and sentence-to-warrant relation. It consumes the existing exact-zero Justice P18 receipt witness without serializing receipts. Runtime composition/publication, P12-G, P12-A readiness, P12-E closure, and P13 readiness are not claimed.

## Validation results

All runs used `Tools/UnityValidation/UnityValidation.psm1`. XML and compressed logs are retained under `RawCurrent/`; every XML reports a passed run and zero failures. The XML/log SHA-256 values below identify those exact artifacts.

| Run | Result | XML SHA-256 | Compressed log SHA-256 |
| --- | ---: | --- | --- |
| `P12EJusticeRecordsOwnerSnapshotTests` | 9/9 | `024FA7D8A1442C8185C50D0019DAC4F399C58F6C47A022B2C3AA2E8B35E0A378` | `E0E4004EA0123158E4BB204410310EB5C0364C2C5DDDD928FDB92E6EAAABAEE5` |
| `P12CrimeJusticeInvalidationTests` | 12/12 | `31FA4712C26BF5912ECFF1539EAB6484777F93394FAC65264232097E751C4459` | `ABAE432CC0951EC4E53832F14C3110E15524CC85FFA814BCEF5D6CE0D3C5463D` |
| `P12DNpcRootOwnerSnapshotTests` | 26/26 | `0928ADB019F8DF6A4573738E9474D8D3D2EE7289A82C4BFAF43611F8578805D8` | `B81AF97F31B8F9BD8D290687ED7A06AAB033976B84537070F5D518551210581C` |
| `P12DNpcReceiptOwnerCensusTests` | 13/13 | `755B5F701BA6279253EBC4A33C17F1EC7689039CD8C2D270986D8D036D068297` | `1CFEE2A82EE053B31DDF397C7226576E2B00A5F5C0F390B7BCCDD586B727AA69` |
| `SimulationRuntimeAdmissionTests` | 70/70 | `1DBF468EA37923448353A1279AC21594CDCE5E171AC841BF8002DCF9A99F2CFB` | `C4148E3FCA2B0DD4249A7DDF848998DC8F2EEF8207F518DB4FE00B2D05DA8879` |
| ALL EditMode | 2709/2709 | `B2352BEDA1C7DE51EE6B5C7591E8D2DCCF955C8FA35B0E1A203B5E4796FFE956` | `1B909A5664786BEE70C8DFAE71007EEF6F6261E550DD672AF78AE556B2F3CA5C` |
| Official Smoke (`EditMode -TestFilter Smoke`) | 5/5 | `E8EEDB89A7D54E0AC9B7549B9CE10069A116CB97C74B6E056AE4DBB769D783AB` | `C78422E75FA69F12D82701ABE4105D4AB91EFCE829FF75D59B1F29B922065D85` |

`git diff --check`: PASS after the final test addition.

## Covered rejection and side-effect cases

The focused suite covers malformed section/schema/cardinality/revision and row values, checked combined-count overflow, null and duplicate row/reference cases, missing and ambiguous staged roots, wrong/missing/duplicated/reordered witness vectors, stale completed-boundary tokens, exact-zero receipt evidence, dangling and one-sided Person bindings on capture and staging, invalid and out-of-range sentence-to-warrant ordinals, and no event/EventId/record-sequence/NPC-status/P12 callback effects while staging.

A valid completed Daily-v1 token cannot coexist with nonzero Justice receipt state under the runtime admission contract. Receipt predicate behavior is therefore tested directly for exact-zero, nonzero revision/cardinality, wrong owner, and null witnesses; a separate live capture test verifies stale-token rejection after the boundary changes.

## Preserved limitations

- P12-E remains open; this does not close Phase 12.
- P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked.
- This does not add Justice runtime composition, persistence publication, or P12-G callbacks.
- No P18 receipt persistence or broader owner/readiness claim is made.