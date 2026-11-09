# CORE-F Host-Owned Census Remainder — Exact-Tip Review

Date: 2026-11-09

## Reviewed identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Candidate tip: `aa0dccbbb6c80242df90e2109e31bd5aff4e584a`
- Candidate tree: `28699e7e0f1f2dfd08db7dfee44be4c3bfd62cbb`
- Implementation commit: `805f855a20e625f5b26470e0689185336a29e00a`
- Exact base: `7330b4f3016ffb4813f6d05f0c988ddbdffdf948`
- Current Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Candidate branch was synchronized to the reviewed tip. Core canonical was unchanged.

## Verdict

**VALIDATED_CANDIDATE.** The exact three-file implementation diff was reviewed against the stated base. No actionable correctness or contract findings remain. This is an incomplete CORE-F recovery checkpoint, not canonical promotion or CORE-F acceptance.

The first review identified duplicated XML summary lines. The docs-only follow-up `aa0dccbbb6c80242df90e2109e31bd5aff4e584a` removes those two lines; it does not alter executable behavior. The resulting comment-corrected source was included in the validation input snapshots below. Their runner metadata names implementation commit `805f855a20e625f5b26470e0689185336a29e00a` as HEAD; file hashes and stable input digests bind the source bytes carried by the final candidate tip.

## Review result

`SimulationRuntime` now requests the host-owned remainder from the Core census helper. Core omits selected NPC census rows only when the existing exact-NPC-fragment check proves the complete fragment matches the selected owner graph. The exact RuntimeIdAllocator triplet is omitted only when all three rows match the Core witnesses, including section identity, schema, role, owner-instance reference, cardinality, and revision. Core later recomposes its NPC and allocator rows before applying the unchanged P12 census gates.

Mixed or stale NPC facts remain on the host compatibility path. A partial, absent, or stale allocator set remains intact for downstream admission. Duplicate or invalid section identities fail closed. Rows retained in the remainder keep their original order and object references. The added regression covers exact rows, stale NPC and allocator rows, mixed-roster preservation, and reference identity.

The change preserves the existing 89-family P12 contract and does not claim that every family is Core-owned. It changes the source/recomposition boundary for the exact selected NPC fragment and RuntimeIdAllocator triplet; it adds no owner family, changes no persisted schema, and does not widen the Daily-v1 profile. P12 and CORE-F closure/readiness are not inferred from this checkpoint.

## Validation evidence verified

The independent reviewer checked the protected manifests and compared their source hashes with the final candidate files. Raw XML and logs remain private; only run identities, counts, and artifact hashes are recorded here.

- CoreValidation run `2aac1a3496c44928ab2de8b667197c7a`: **1,074/1,074 cold and 1,074/1,074 warm PASS**. Stable input digest: `5521ffeca1a21a1b26a56404c0d894865d29f7136951cd50858835ce92d3c9d7`. Cold TRX SHA-256: `2a02af2eaff875777dfc094272b7c79f66deca57cac2a78de3291346ba215433`; warm TRX SHA-256: `b8bf4d9db803903138c55f033d685ebd5d0e7cb6f351987f2847676ec4f9fdde`.
- Unity EditMode run `6d7f34d9e2b14f1aad275232daa2dc37`: **3,303/3,303 PASS**. Input snapshot SHA-256: `fb7a2ffb823d324ba8ff8a83069f9a212a265872eee7b5957e8474244e4ed5d8` (before and after identical). XML SHA-256: `175924d8bfa85a78bc58a55d2f17da7f9f1ba1db7a4b4132d53b9c7283b1b6cc`; log SHA-256: `d4442637c9a634de0e629650176381c5e663d1019e92a37d9ceb27a316737f48`.
- Official Smoke run `057ee5571dbd424cbc8a017d5bfd4438`: **5/5 PASS**. It used the same stable input snapshot as the full EditMode run. XML SHA-256: `80aeef7c5b86d32e208db9af04132f1ccd1bfbbefebe9704bcffabf1989be490`; log SHA-256: `d6ca54feeb0efbddb5c45c088a66e3251256f67b1cb8bc0cf37ba5111f3d7120`.
- Verified source SHA-256 values: `DailyV1RuntimeAdvanceStage.cs` `05d4a7ead6f5bd5af2daa6709167d1eb28cd2fc37a5bc6e556b0d090b48a9fe8`; `SimulationRuntime.cs` `6ac60952690a369bcd0601a8990a573ab83140692095de19c13dab7a23aaed9d`; `DailyV1ApplicationSessionGateTests.cs` `ab2b110dcaa7494c154624c0e4f5a5f2e3cc4a7edd0b8f607f352c90c8c56a31`.
- `git diff --check` from the exact base to candidate: PASS.

CoreValidation reported `LibraryAbsent=false`; this evidence does not claim .NET-only validation in a Library-free worktree. The reviewer did not rerun tests. No candidate code or test files were changed during review.
