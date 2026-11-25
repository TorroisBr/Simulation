# CORE-F Restored Core Target Invariant Review Fix — Validation

## Scope

This test-only correction adds direct coverage for the Core restored-target invariant gate:

- Exact-zero PersonSpatialPosition and LegacySpatialAnchorBinding census rows pass when those owners are bound to the candidate session graph.
- Missing rows, nonzero cardinality, wrong owner identity, and wrong role are rejected.
- A populated Required P11 ActorChoice census row is accepted using the exact selected owner identity, current cardinality, and revision.

Production source is unchanged from candidate 70290dee1a62b36afc724a2fb266f87f431b0028. This does not complete CORE-F or claim detached-host acceptance.

## Exact source identity

- Validation worktree HEAD: 70290dee1a62b36afc724a2fb266f87f431b0028.
- Committed tree: 46cbb0469d6c663f90b563ef6efc29b3e49e18aa.
- Core canonical base: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea.
- Changed test input SHA-256: 771f6a6a250dee8bae3826c3b0e6f529bb836c194a1089b37f95aa5e7e59f2ce.
- The active candidate and validation worktree had identical test-file bytes when packaged.
- Runner-recorded input digest: 2e3c28e9fc43aa7d6246575842f6ee91f3ed32aeeaa25bba6eead7b48aae21d6.

## .NET CoreValidation

Official CoreValidation passed cold and warm:

| Run | Result |
| --- | --- |
| Cold | 1,022/1,022 passed; 0 failed, inconclusive, or not executed |
| Warm | 1,022/1,022 passed; 0 failed, inconclusive, or not executed |

- Runner ID: a64dabe2f91148079908d6ede49e5b0a.
- Protected runner-manifest SHA-256: 0a570e7062809f89031c32b25b7494174f9505e231c436b92dd8ceac4e752f31.
- The original cold/warm TRX reports were retained unchanged in protected evidence storage.
- The approved DotNetTrx package contains the normalized cold/warm reports and passed original-source comparison: originalComparisonVerified.
- Package manifest SHA-256: 93f075c1cfcf6f885316f880bd9c45843db172c51ea2be755f2132938b37bad4.
- Package archive SHA-256: 57d8430e80ecf320314dd689454f33ce87775b3940c8afa62af94fc3b11420db.
- Archive-only verification also passed and correctly reports originalComparisonNotPerformed.
- The public manifest has no workstation paths or credential/private-key patterns; the official package privacy policy passed.

## Prior Unity evidence and boundary

The previously published Unity package reports full EditMode 3,299/3,299 and Smoke 5/5 on input snapshot `c88e939396a2a4e728710ebe66028b68cf1f78282a6f2eb49a6bf2807d57b5f0`. Its package manifest SHA-256 is `5671ac0c0bb486db9e87a1db441b1a2fcf304967b59eb88a4dbce1411e612ae7`, and its archive SHA-256 is `df8e91349ddad888eabbbbc6ec7cd5a1831c12aadaf4b3bbf9f5ee5846b7650d`; official package verification passed.

This test-only correction did not change Unity source. Unity was not rerun. The existing package snapshot's ProjectSettings asset hash differs from the user-preserved ProjectSettings file in the current worktree, so those prior Unity totals are recorded as historical evidence only and are not claimed as validation under the current local ProjectSettings. The ProjectSettings file was not changed by this task.
## Limits

- This record validates the P8/P11 restored-target test correction only.
- It does not claim full CORE-F runtime/host integration, CORE-F completion, or canonical promotion.
- Protected original reports, logs, runtime traces, and adapter context remain in the owner-only evidence location; no raw private artifacts are committed.