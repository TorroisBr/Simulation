# CORE-F P18 receipt authority migration

Status: candidate validation complete; independent exact-tip review is pending. No Core canonical promotion is claimed.

## Scope

The P18 receipt ledgers for Crime and Justice now live in `Simulation.Core`:

- Crime hidden-status receipt stream;
- Justice `BeginDay`, `AdvanceSentences`, and `SyncWantedStatuses` receipt streams.

`CrimeSystem` and `JusticeSystem` retain the existing Unity-facing receipt DTOs and runtime effects as compatibility projections, while receipt identity, replay matching, ledger revisions, and P12 receipt snapshots use the Core owners. The P12 section IDs, codecs, and selected 89-family census contract are unchanged. This adds no census family and makes no full-runtime, host, or phase-completion claim.

## Validation

All successful runs used the exact E: candidate source. Unity was 6000.3.9f1; the CoreValidation runner used SDK 9.0.315 and executed the net8.0 test assembly.

| Gate | Result | Run identity |
| --- | ---: | --- |
| CoreValidation cold | 1204/1204 passed | `d2cc3de02ec649aebbf0cec18b210862` |
| CoreValidation warm | 1204/1204 passed | `d2cc3de02ec649aebbf0cec18b210862` |
| Core receipt-owner focused tests | 3/3 passed; also included in both CoreValidation runs | same run |
| Unity P12E Justice snapshot | 11/11 passed | `e8bf1632fb134645b47e11648fdde49a` |
| Unity full EditMode | 3326/3326 passed | `ff3d7c92361944e0830a227bc50267a7` |
| Official filtered Smoke | 5/5 passed | `4ed2fdf00a18413cab2f3f30a2e96466` |
| `git diff --check` | passed | after implementation |

The unfiltered EditMode XML was packaged and verified against its original run manifest. Archive SHA-256: `5fc74a54dd0b79e5d475667b5a23c400eb35dd713e6b289c8717bcf8c67fe023`; archive size: 177106 bytes. Package manifest SHA-256: `cd00e0b710d465b134a71c8f49dc72225075f09dd9520dcc5ddf3748cbefe482`.

The private CoreValidation manifest records working-tree input digest `20a93e2f59ce7f84177a958ff3c1567387e4761b64ee1723c57d793a922b6086`; cold and warm TRX SHA-256 values are `bf830d4214b9e9fb96ab6697796a4b6ebc2e7225683225052593c0b8ec7220e9` and `3f9e0ede4f701e307f5e876e27dab43cbf53b6e8815c0be6bd7702ffebfe4f4e`. The raw TRX, logs, and private manifest remain in the owner-only E: evidence directory and are not included in the public package.

## Preserved validation diagnostics

The first full Unity attempt used a deeply nested E: temporary path. Five unrelated P12 store tests returned `StorageFailure` on their first file save. The generated temporary filename would have been 284 characters. The unchanged focused store test passed when the process-local `TEMP`/`TMP` values pointed to the short E: path `E:\SimCoreFTemp\p18receipt-20261124` (temporary file path length 179), and the unchanged full EditMode suite then passed 3326/3326. The initial failed XML and log remain preserved privately; neither the store nor its tests were modified.

The 11-case focused XML and five-case Smoke XML are preserved unchanged in the private evidence area. The approved packager rejected filtered reports because NUnit emitted suite `testcasecount` values for the unfiltered suite while serializing only selected result nodes. No XML was edited or normalized, and those filtered originals were not placed in the public archive. The official full EditMode report is count-consistent and passed package verification. NUnit documents that a filtered run's root `testcasecount` may exceed `total`; see [NUnit Test Result XML Format](https://docs.nunit.org/articles/nunit/technical-notes/usage/Test-Result-XML-Format.html).

## Storage

E: free space was 16796708864 bytes (15.64 GiB) before validation and 16279633920 bytes (15.16 GiB) afterward, a net free-space decrease of 517074944 bytes (about 493 MiB) across validation and package activity. No evidence or worktree was deleted.