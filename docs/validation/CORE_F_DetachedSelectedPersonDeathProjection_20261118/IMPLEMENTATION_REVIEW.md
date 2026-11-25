# CORE-F Detached Selected Person Death Projection Review

## Review identity

- Verdict: **PASS**
- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Base: `708ba8edd346a576971b1bb3bf791c24d91633d0` (tree `aa4d8ac600ad2129e9c89082af9361f588f9c824`)
- Candidate: `d075715c69d69a6f05d67ea1b8ab179c97fe5545` (tree `1c9e5e2622da5d02d439e60aea7c227e035d0c93`)
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`; local canonical and `origin/codex/core/canonical` refs both resolved to this SHA.

The base-to-candidate diff is limited to the two reviewed files below. Candidate ancestry from the base is valid, the Core canonical SHA is an ancestor of the candidate, and the committed worktree copies of both reviewed files match their candidate blobs.

| File | Candidate Git blob | SHA-256 |
|---|---|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedDemographyStage.cs` | `93afbaa404f8b7a87e1003187e78b76eb7ebefe1` | `40d12d4c8cc7f651ba4128f95db9705c47c9474416817c4e57484c9a57826225` |
| `tests/Simulation.Core.Tests/DailyV1CoreSelectedPersonDeathBoundaryTests.cs` | `7caf666c5cbac3b33f7314dca5046626acb7c44c` | `23df68cbe11646d5fda2cfb0f89a12a7cf8c6b5fa83ef7625e63a39980d0cfba` |

## Findings

The optional projection path is fail-closed on Core-owned state. Core still validates the exact execution state, owner graph, registered `PersonRuntime`, materialized NPC identity/action, residence City identity, current thread, and expected action transition. It derives and checks the exact operation section list before admitting the operation, reserves the mutation epoch, and commits the exact section set through the Core census mutation protocol. Successful detached execution publishes one epoch. The added resident-death test checks the Person, NPC lifecycle/action, and City population values and revisions, cleared reservation/operation state, healthy protocol, and complete census capture.

The added null-projection rejection test targets the Person census provider's second read after recording its current count: admission reads it once while validating the unchanged vector, then reserved Core commit refresh reads it again. That commit read is rejected. Assertions verify that all affected Person/NPC/City values and revisions are restored, the epoch remains unchanged, reservation and operation state are released, the census protocol remains healthy, and the complete census can be recaptured. The initial review's missing null-projection rollback coverage is therefore resolved.

The existing Unity call site remains unchanged and still supplies `SelectedPersonDeathProjectionBoundary`. For a non-null projection, validation, admission, pre-publication compatibility-baseline staging, post-commit permit revocation, rollback, and world-revision advancement retain their existing calls and order. No unrelated test assertions were weakened. No actionable findings remain.

## Validation applicability

The official CoreValidation manifest binds both reviewed file contents by SHA-256. Its cold and warm runs each report **1,158/1,158 passed**, with zero failures, errors, or skipped tests.

- CoreValidation run: `49fa304098844f20a22f040b6cf6a266`
- CoreValidation manifest SHA-256: `d7490f6a01565db5856c810295c778c93c3ef387dc6eb8fcc32e285ae0ed2a90`
- Manifest working-tree input digest: `0ea715b9ea107d5ec69ccf8767e81875fbbefcfc979dc777930a70ac83cd5dd2`

The official Unity EditMode and Smoke manifests report stable input snapshots before and after execution. Both include the reviewed production package file with the exact candidate SHA-256. The Unity runs occurred before the candidate commit was created, while repository HEAD/tree still identified the base; their package-source hash therefore establishes applicability to the candidate production file. `DailyV1CoreSelectedPersonDeathBoundaryTests.cs` is a .NET Core test source and was not part of the Unity input manifest. Unity results establish production-source compatibility only; they do not claim that Unity executed the new Core test fixture.

- Unity EditMode run: `50fc2c53907449fdbf5c4f28fdcf8c2c`; **3,322/3,322 passed**; manifest SHA-256 `06ee479da96423582081d66a3a55d83e3ecbac44896192fe6c9c688b964ae089`.
- Unity Smoke run: `38dacd137a47415f9e685319af5de8b8`; **5/5 passed**; manifest SHA-256 `cf986ee98fd83dd7e77925a87b16ae8f4c2235c9859f76b1faa98a0ab7b53028`.
- Both Unity runs recorded stable pre/post input SHA-256 `b02a7b2b6210e91c8031e219fb1bb23348dd82d3cf81d062a00812c4bf66302e` and the production file hash listed above.

This record covers only the bounded detached Core Person/NPC/City death operation and Unity compatibility of its unchanged projection path. It does not claim completion or promotion of the broader CORE-F package.
