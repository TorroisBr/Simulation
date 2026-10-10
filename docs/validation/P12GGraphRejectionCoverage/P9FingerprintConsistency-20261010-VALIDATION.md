# P12-G selected-P9 fingerprint consistency validation — 2026-10-10

This test-only slice extends the existing integrated restore rejection test to
cover both malformed selected-P9 fingerprint format and a different but
well-formed lowercase SHA-256 fingerprint. It proves that each inconsistency
is rejected during P9 manifest capture after source census and before target
root staging, with the active source session, completed-boundary token, health,
and owner graph preserved; it then proves deterministic continuation parity
and successful restore after the original fingerprint is reinstated. No
production code, profile, owner, or gameplay behavior changed.

## Exact baseline and tested tree

- P12 canonical base: `f8fea5b603fdfb42dbbf11ddc447ce1e4931f9ce`
- Code commit: `3e1e1eea9aa94bc0c2bbb509faaccc45c35292a4`
- Code Git tree: `60ab88b969f7b626f9e2eb88340fba24c5a94750`
- Tested `Assets` tree: `d188470c2cf8a07ea0cd4c2de98773ed514b8798`
- Test source: `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs`
- Test-source Git blob: `cd7e0f50aeccae03fb2eab5267fa7b09297eed8e`
- Unity: `6000.3.9f1`

## Validation results

All test runs report zero failures, skips, and inconclusive cases. XML result
files and compressed Unity logs are retained beside this manifest.

| Gate | Result | XML | XML SHA-256 | Raw log SHA-256 | Compressed log SHA-256 |
|---|---:|---|---|---|---|
| `SimulationRuntimeAdmissionTests` | 121/121 PASS | `EditMode-20261010-172957-f61171122cfd48a987568a1588ab5e35.xml` | `39820D3AE2B1C278BFB5A351AFB9BF3DC50333EB6C6E4C246DE3609BADDB5F23` | `C99534A02FCEA900C21BF96B534349F2FCC52028FC154A2113974857F0A1AFF6` | `8C83A9290E032D53C2DD8F463CE4325E47F7CFE8AA797A22D648AE8E554E0AB3` |
| ALL EditMode | 2795/2795 PASS | `EditMode-20261010-173043-58dc68f8020441e5ac636a05f4bb05d9.xml` | `E3E45BFAC92B88C3F189A32AD96F162F5D7DBE7772EC04D8E257A04DAA4D5506` | `BB603ACA8BA0D511F8AED8E53E536A592EF8BF7DE5C36B491D0871BDEC143593` | `DE4DD934E238CB889E1BB9ED341C8568F644FD8C81FAC8A70052C424815E5785` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 PASS | `EditMode-20261010-173122-c901b1be81654370a94c68f4c2c98de5.xml` | `2AE0212019E1C9D1DC750986E79D42010CB70D20F8DF77F548E44E95B43EF101` | `056BAD378018D0DC5ECCDED5CB5D3393C52618A28498C721E3F7B09A8BF93466` | `96AADAC0A3A380B9F213E356B0F0110A27651D2D6435B3149A588BA5A455F557` |
| `git diff --check` | PASS | — | — | — | — |

The reviewer can decompress each `.log.gz` to reproduce the listed raw-log
SHA-256. Validation was run against the exact tested `Assets` tree above.
No ProjectSettings changes, protected `.meta` files, or pre-existing
untracked XML evidence were included.

## Scope and limits

This closes only the distinct valid-format-but-inconsistent selected-P9
fingerprint rejection case alongside the existing malformed-format case. It
does not close all P9 lineage/compatibility cases, the P12-G graph rejection
matrix, whole-graph failure injection, causal no-replay, or full included-owner
multi-boundary continuation parity. P12-G remains `WAIT_DEPENDENCY`; P12-A
remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains open. No
capture eligibility, export, hydration, or downstream readiness is implied.

