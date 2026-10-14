# CORE-B Deterministic and Temporal validation

## Candidate

Branch `codex/core/CORE-B-deterministic-temporal-c957`, based on Core canonical `c95752846a0ce0e1ebf985c4d79e0e17b02bb7af` and including current-baseline review commit `3eb18a6a09ee430e15b2d479c02031b221443fd8`. Design `d20c2352887184ccc964d7f9586407d00a727d32`; independent design PASS `96a7df8c2ebad6cf5ce3bfe2bde140f98c6e46dc`.

The bounded change moves DeterministicRandom and LogicalTimeline into the engine-free Core package with their original `.meta` GUIDs; adds one invariant stable-key encoder and Unity adapter; assigns explicit SDK source ownership; links shared Unity fixtures into the SDK test project; and exposes the specified narrow P12 snapshot bridge. The RNG source changes only for that bridge and explicit unchecked rollover expression. LogicalTimeline delegates stable identity encoding to the shared encoder. No ProjectSettings file changed.

## Validation

- Source-closure compile: `dotnet build Simulation.Core.csproj --no-restore -c Release` passed with zero warnings/errors.
- Locked restore and shared .NET tests passed. CoreValidation cold and warm each passed 97/97 cases. Run `83e8e0475f154ff6a531b1f5cf06550d`; stable 41-file input digest `d955f8a7084878a408312b91d27071d60e5fd9fbb88ef92055c9b7facfcace24`. Cold/warm TRX SHA-256: `08b6a10e2fc0a08e0eefdfbab52fd938183a2b96bf744170bbb432ae5cd1c88a`, `fb025dd74e851b24a1ddf8065fc7b45511029dd7869f33a81a94bb75e40e2380`. The four folder metadata files are outside those 41 CoreValidation inputs; production sources, test sources and CoreValidation inputs are unchanged by their whitespace cleanup, so the results remain applicable. Original .NET reports/logs remain private.
- Focused Unity RNG/snapshot passed 17/17 and focused LogicalTimeline passed 38/38 on the implementation source. The final post-cleanup complete EditMode rerun passed 3,085/3,085 with zero failures, skips or inconclusive results. The shared Unity test files are linked directly by the SDK project; no duplicate test implementation is maintained.
- All 1,559 files in each final Unity input snapshot matched their recorded SHA-256 values, with stable before/after snapshots. The official ALL EditMode and Smoke runs captured identical input digest `89e1295f79608b8da96f5d31a739bf8357c5a6626bd8322a61300ac5efcbead4`.

## Unity evidence

The current complete ALL EditMode wrapper run is `48298dd5a6e542f2a8b644730f6ade28` (3,085/3,085). It is packaged and verified as `core-b-final-meta-clean.zip` with one run and one report. Archive: 161,441 bytes, SHA-256 `c6b46224c4e504e965fa5c484ab27d4cda3ed5b2afeb44424bb561e4f3cb05d8`. Committed manifest blob (UTF-8/LF): 355,649 bytes, SHA-256 `e52f62c49d331ad41051652aa9abce2aea681ad11e232eab616716627e9c0069`. Windows CRLF checkout representation: 363,552 bytes. Normalized XML: 1,605,562 bytes, SHA-256 `dabd1ec4e4f4667f6ac7f40b1ba04ea3a83835a4228572685f91346773b62d11`. The original XML is retained privately (3,110,414 bytes; SHA-256 `8007115789b5b6c4a1f2a6aa2a887955a798c4dffac1636517b034854d781e76`). The package and manifest redact local report paths. The earlier `core-b-final` bundle is superseded by this metadata-clean rerun.

The four implementation-generated folder metadata files were changed only to remove trailing whitespace from empty YAML values; GUIDs and other values are unchanged. Their final captured bytes are now durably represented: `Assets/_Project/Tests/EditMode/Editor/Core.meta` (169 bytes, SHA-256 `178e693d26ca94b08251f4c96ff9202cdef88d9e69f2150393f0e908cec01844`), `Packages/com.simulation.core/Runtime/Determinism.meta` (169 bytes, SHA-256 `77fef014d75e1727d53d561ab93d1a2d71d7249d66b871e90fe6d0b2123e45fa`), `Packages/com.simulation.core/Runtime/Identity.meta` (169 bytes, SHA-256 `e9c128b01020e3547ecf46db59525a157a7d175c816508f9421fbf1c65028644`), and `Packages/com.simulation.core/Runtime/Temporal.meta` (169 bytes, SHA-256 `3b746ac6ff7c49ffe47c911bb373e02d51ffcb315de0489cf97f6a40f01a0f80`). ALL and Smoke reports independently recorded exactly these same bytes.

Official Smoke independently passed 5/5 in private run `ddf1801efb7b42df83206059f214ba97`. Its filtered XML/logs remain private and are omitted from the package because the supported packager rejects filtered reports with incoherent aggregate suite counts. The report was not edited and the packager was not weakened; all five Smoke cases also passed in the complete ALL report.

The previous implementation exact-tip PASS is superseded for the metadata-clean candidate. Fresh exact-tip review is required. Three unrelated user files, `Assets/_Project/Scripts/ArmedForceSpatialPosition.cs.meta`, `Assets/_Project/Tests/EditMode/Editor/ArmedForceSpatialPositionTests.cs.meta`, and `Assets/_Project/Tests/EditMode/Editor/P17ARuntimeTests.cs.meta`, remained unmodified and uncommitted. The run manifests captured HEAD/tree `8e86bf545b9e8c3a5ed7fd68f9ee6ccdf1eb5413` / `7eac40f12c46ef0fc96b9b806e1f2917f88446d8`; those identify the pre-commit run snapshot, not the final candidate metadata.

## Limits

CoreValidation records `worktreeLibraryAbsent=false`; this checkout has an existing Unity Library, so no Library-free SDK claim is made. This checkpoint does not claim CORE-B promotion, P12 readiness, full SimulationRuntime hosting, cross-build save compatibility, or CORE-C readiness.
