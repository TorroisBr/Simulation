# P12-G selected P9 lineage rejection validation

Base canonical SHA: 18f9410a08b2d4731c6ee8a5f67332feb1d71787
Code candidate commit: d9335fc250da17bd2d225fd0aa04dc661d6b1a00
Candidate Git tree: eb068b209ac71cfed9723949d43d448ca23204e4
Candidate Assets tree: 8d38e01570fc1a4e4fbcfab247762d0997622e9c
Validated test-source blob: 69b1744f28f37ecf6844480f1b2e83195d1ba447

## Scope

This test-only slice adds an integrated rejection case for a corrupted selected P9-B fingerprint. It verifies that the Daily-v1 coordinator rejects during P9 manifest capture after the source census but before P12-C candidate roots are staged; the active source session, completed-boundary token, owner graph, health, and subsequent deterministic behavior remain intact; and restoring the original fingerprint allows a valid retry. It changes no production code, owner, profile, or runtime behavior.

## Validation

| Gate | Result | XML | XML SHA-256 | Compressed log | Compressed-log SHA-256 |
|---|---:|---|---|---|---|
| Focused EditMode: SimulationRuntimeAdmissionTests | 117/117 PASS | Focused/EditMode-20261010-164709-6df285eef6694dec861772b0d60ccb3c.xml | D4BB9F4027AC0C15937D181F2308511F0A4BD9ABFFE87EFBBD54C85BE4E74E8C | Focused/EditMode-20261010-164709-6df285eef6694dec861772b0d60ccb3c.log.gz | 9500157BE014152C5FB77DEDDB531079BF914B2C23BCC7188A8B1A2BC3E4E178 |
| ALL EditMode | 2791/2791 PASS | AllEditMode/EditMode-20261010-164727-038a103de45a40c1815febfa4ebc9553.xml | EFD1C8B55C89BA772A19F5F61984903215B6553100DEE904E27996912B97EFB2 | AllEditMode/EditMode-20261010-164727-038a103de45a40c1815febfa4ebc9553.log.gz | 35F09F815B1607A1F18D519A1F29A821551225F47B2BDF58E1835CA8AFD3E0FF |
| Official Smoke | 5/5 PASS | OfficialSmoke/EditMode-20261010-164805-dbe0263328664929a5fe177abe8f73f1.xml | 2BB2E3E8D63FD3ADC00C6A45F70ACB76E6D2D31A9F10FE6AEDD1A3364FC8F090 | OfficialSmoke/EditMode-20261010-164805-dbe0263328664929a5fe177abe8f73f1.log.gz | BFB244556C9E6CC4EFEED98ABB7EB22098B61B982F896590322E0325FD44F836 |
| git diff --check | PASS | Exact code change checked before commit |  |  |  |

Each result XML reports Passed, with zero failures, skips, or inconclusive tests. Compressed logs were decompressed and SHA-256 checked against their original run logs before the originals were removed from this evidence directory.

## Limits retained

This closes only the integrated selected-P9 lineage rejection case. The complete live owner/cardinality/transition and consumer joins, remaining target-owner census, owner/relation/commitment/global-validation failure injection, publication lifecycle/disposal, full included-owner multi-boundary parity, and other P12-G obligations remain open. P12-G and P12-A remain WAIT_DEPENDENCY; P13 remains BLOCKED; Phase 12 remains OPEN. It does not claim capture eligibility, export/hydration readiness, P12-A/P13 readiness, or Phase closure.