# P12-E Justice Records Owner Snapshot Validation

Date: 2026-10-09

Base: P12 canonical `565f6b5817e0126c2f89e0a1452b09a8fb03cc19`

Design: `fb16e4f` (exact-content design re-review PASS; durable record `f2f4615`)

Code tree: `251b40a9edeb82a75e793723aac758d937603f6f` (`957b911`)

## Scope

This candidate captures and privately stages only ordered Justice wanted-record and prison-sentence values for the current `JusticeSystem` owner. It preserves exact Justice local revision, target NPC/Person identity, City identity, and sentence-to-warrant relation. It consumes the existing exact-zero Justice P18 receipt witness without serializing receipts. Runtime composition/publication, P12-G, P12-A readiness, P12-E closure, and P13 readiness are not claimed.

## Validation results

All runs used `Tools/UnityValidation/UnityValidation.psm1` and retained their XML and compressed log under `Raw/`. Each XML reports a passed run and zero failures.

| Run | Result | XML SHA-256 | Compressed log SHA-256 |
| --- | ---: | --- | --- |
| `P12EJusticeRecordsOwnerSnapshotTests` | 8/8 | `6e422e492ea5b0c0e3596914f24c92fe733e9f6a44094fc13a858f5b714de2c5` | `970a2791eed3e8283bcd8512ac3b1c524bff0c2d7131ebac2ba242f8a51a9655` |
| `P12CrimeJusticeInvalidationTests` | 12/12 | `143e1f8b11c382e5d080bfbbf8e15212e4d17aebbc5ce109fdbade1d0b22494c5` | `61e8bfb868bb033a54522fc4fc6c7e85566326abb241804c8856496d2d13c370` |
| `P12DNpcRootOwnerSnapshotTests` | 26/26 | `61793eb6d34eee5196db46634d25b820e1f37f21d902c25eb3b212c97b7c72ea` | `a8956de052b2d97124b9b90427e59bd424ba7ab388716fa9fc8ca008c9f1fb30` |
| `P12DNpcReceiptOwnerCensusTests` | 13/13 | `ff410de71e2260f5073a04f25cd44736049675a50f67838da1f1ef5ac3a56969` | `7dea957e44f9a6572708b736f8f02889631bdde180b55760fb1f89ba2278eb69` |
| `SimulationRuntimeAdmissionTests` | 70/70 | `136dae2b9b989cbdec965c654917f0f66032e77500ee36036f419b3bdd983a82` | `2237bfd493fd7ce8ac0f76ff4b1a9cbd337acfa2ecba46a032ee58297e59f426` |
| ALL EditMode | 2708/2708 | `06451beffa7ea13e0d3642b2b3871638a058da11b331e42d44cb928fb2d91cce` | `ca3a4ef573686d4ed66e25530255e3ea0b0748118db4d4c5953fcf6b928120eb` |
| Official Smoke (`EditMode -TestFilter Smoke`) | 5/5 | `b5fb5f97826ece29a0b4321fab9e295cb8b18edd94503e599e69c5cd0225530c` | `e7a75094b46fdea12b65d81362af52ff6906fa429c2589375b58984d60b9f352` |

`git diff --check`: PASS after implementation and test changes.

## Covered rejection and side-effect cases

The focused suite covers malformed section/schema/cardinality/revision and row values, checked combined-count overflow, null and duplicate row/reference cases, missing and ambiguous staged roots, wrong/missing/duplicated/reordered witness vectors, stale completed-boundary tokens, exact-zero receipt evidence, dangling and one-sided Person bindings on capture and staging, and no event/EventId/record-sequence/NPC-status/P12 callback effects while staging.

## Preserved limitations

- P12-E remains open; this does not close Phase 12.
- P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked.
- This does not add Justice runtime composition, persistence publication, or P12-G callbacks.
- No P18 receipt persistence or broader owner/readiness claim is made.
