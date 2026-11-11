# CORE-F P12-E Core Justice projection and capture

Status: validated candidate checkpoint. CORE-F remains incomplete and unpromoted.

## Source identity and scope

- Repository: Simulation.
- Candidate branch: codex/core/CORE-F-restore-stage-transaction.
- Exact implementation commit: c4e698f8864fefde25073aa0fe40c50cb70f5893.
- Exact implementation tree: 8469f2eeeba12d272a7d3b8d8bbae28d2b674ea6.
- Exact base: 3bf3eb954fb112d2e511be1f146757cb44640656.
- Core canonical at validation: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea.
- Scope: the active Core save path serializes the Justice snapshot captured from its selected Core owner graph. The Unity adapter validates the existing Justice compatibility projection against that exact Core snapshot and retains the separate P18 receipt-owner proof. The legacy Unity capture path remains available only when there is no active Core save operation.
- No P12 owner identity, census, revision, epoch, capture-boundary, or P18 shared-identity contract changed.

## Validation

| Gate | Result | Evidence |
|---|---:|---|
| Focused Justice continuation | 1/1 PASS | Official EditMode run 0ccf09dc6edd4db0a932533a23efe385; input stable; XML SHA-256 c9b33c538b9efe6b3a7ca9a52d5f23d7e42fda9c4f95fbf7d96e203802a964ff. |
| Full Unity EditMode | 3,306/3,306 PASS | Official run 66a465990c9a4770b1b52420f5cf3c77; input stable; XML SHA-256 986db16d6a3bf1cbf073ce6a6408ec9e7a060fee28ded85f254b3f8a3b0a0fcf. |
| Official Smoke | 5/5 PASS | Official EditMode filter Smoke, run 39ccd75be6614770b693de4bba2c6f7e; input stable; XML SHA-256 6800abfbd19f9d393b46bcdd1181ec7d4f77d6242cbd8f33130f6e3a723a9d4b. |
| git diff --check | PASS | Exact implementation commit. |

All three official runs share input digest 70a98adff1a850f8d00f9e33d348aac1c9a8add4169180ea6d535944036089b2. The four committed implementation/test files match the source hashes recorded by the full run.

The focused and Smoke source reports remain in protected local evidence storage. Their filtered NUnit XML retains whole-assembly testcasecount values while including only selected cases, so the approved packager rejects those individual filtered reports on count consistency. The full EditMode report includes the focused test and all five Smoke identities and is the published normalized evidence.

## Public evidence package

- Normalized package: UnityFull/coref-p12e-justice-full-editmode-20261111.zip.
- Package-manifest SHA-256: 1978c17fbf16a4aa09b5a909c02eaf522a857dc77dd6e15f6740bf029741c3b5.
- Archive SHA-256: 37b899b7dccdc1a76c4b3ce43b9b30d1bc189b95df7e585863d5dd411314e146.
- Official package verification: PASS; one normalized full EditMode report.
- Original XML and logs remain protected and unchanged. The package contains normalized result XML only; no raw logs or private workstation paths.
- ProjectSettings and unrelated worktree changes were not staged or committed.

## Source hashes

| File | SHA-256 |
|---|---|
| Assets/_Project/Scripts/JusticeSystem.cs | afd6fb9716cacf874752a86f454dd6e9d2125db297fada164a0b7dd8634f42a5 |
| Assets/_Project/Scripts/P12EDailyV1OwnerPackage.cs | f9bfb2071233604e54256aa2d2e6f19055ad11e2ddac07a0699cfa9a064aaf37 |
| Assets/_Project/Scripts/P12EJusticeRecordsSnapshotAdapter.cs | 3acae1a07ba09b06dd00f3f973d548ae654791792167d19bf23f45c52c7f08dd |
| Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs | c8481809da3832d069bc1d9ad43b15598d53a67a90b5cb897a86e1c5ac7825dd |

This checkpoint does not complete CORE-F, establish detached .NET continuation, or promote Core canonical.