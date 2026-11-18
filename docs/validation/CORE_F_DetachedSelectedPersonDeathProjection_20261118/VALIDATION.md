# CORE-F Detached Selected Person Death Projection

Status: **validated candidate checkpoint; not promoted**.

## Identity and scope

- Candidate branch: codex/core/CORE-F-restore-stage-transaction.
- Candidate implementation commit: d075715c69d69a6f05d67ea1b8ab179c97fe5545.
- Candidate base: 708ba8edd346a576971b1bb3f791c24d91633d0.
- Core canonical at validation: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea.
- Reviewed source blob: 93afbaa404f8b7a87e1003187e78b76eb7ebefe1 — Packages/com.simulation.core/Runtime/Application/DailyV1SelectedDemographyStage.cs.
- Reviewed test blob: 7caf666c5cbac3b33f7314dca5046626acb7c44c — tests/Simulation.Core.Tests/DailyV1CoreSelectedPersonDeathBoundaryTests.cs.
- Core remains the sole owner of P12 admission/reservation, exact owner mutation, census validation, mutation-epoch publication, and rollback. A null Unity projection supports detached execution; the existing Unity projection path remains unchanged.
- The new rollback regression rejects the reserved census commit and checks restoration of Person/NPC/City state and revisions, unchanged epoch, released operation/reservation, healthy protocol, and successful complete census recapture.
- This checkpoint does not claim completion of CORE-F, general detached restoration, or full host integration.

## .NET CoreValidation

Official CoreValidation run 49fa304098844f20a22f040b6cf6a266 passed **1,158/1,158 cold and 1,158/1,158 warm**. The protected runner manifest SHA-256 is d7490f6a01565db5856c810295c778c93c3ef387dc6eb8fcc32e285ae0ed2a90. The focused selected-death boundary suite passed **4/4**. The official inputs include the final source and test blobs listed above.

## Unity

Unity 6000.3.9f1, Test Framework 1.6.0:

- Full EditMode run 50fc2c53907449fdbf5c4f28fdcf8c2c: **3,322/3,322 passed**, with zero failures, skips, or inconclusive tests. The protected manifest SHA-256 is 06ee479da96423582081d66a3a55d83e3ecbac44896192fe6c9c688b964ae089; its exact-source input snapshot SHA-256 is b02a7b2b6210e91c8031e219fb1bb23348dd82d3cf81d062a00812c4bf66302e.
- Filtered official Smoke run 38dacd137a47415f9e685319af5de8b8: **5/5 passed**, with zero failures, skips, or inconclusive tests. The protected manifest SHA-256 is cf986ee98fd83dd7e77925a87b16ae8f4c2235c9859f76b1faa98a0ab7b53028.
- The .NET-only test-fixture addition followed the Unity runs and is not part of the Unity project input. Unity production inputs were unchanged and stable.

## Review and evidence boundary

git diff --check passed. Independent review passed for both exact source/test blobs, including detached success, reserved-commit rollback, and preservation of the existing Unity projection path. Original reports and private evidence remain in protected local retention; this record identifies the runs and manifest hashes without publishing private artifacts. The candidate is not canonical and does not complete CORE-F.