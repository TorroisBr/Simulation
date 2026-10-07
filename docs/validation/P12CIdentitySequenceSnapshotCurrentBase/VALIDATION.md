# P12-C identity and sequence snapshot validation

**Date:** 2026-10-07

**Unity:** 6000.3.9f1

**P12 canonical base:** `f23fe5a1c70dce8cb32a4ca6aa088820b3ad7279`

**Architecture canonical:** `47eff220c7ce00f6e7c759bdc2b76780bb46f628`

**Reviewed current-base design:** `bac562f212a31017dba78a6f2c6bc92aa6eea0fa` (independent review PASS)

**Code candidate:** `d9ce4502b6b1601660f2c44629d6e0f34c72036d`
**Candidate tree:** `5222c38f4c4e56313efa1a7caa7542e830d644ba`

## Scope

This slice adds immutable snapshots and staged reconstruction for the fourteen
existing `RuntimeIdAllocator` counters and the shared
`SimulationRecordSequence`. It preserves exact next values and gaps, rejects
unsupported schema/family/value shapes, gives reconstructed owners fresh census
identity with local revision `next value - 1`, and leaves their mutation hooks
unbound until normal runtime composition binds them.

The current P12 census providers, mutation hooks, P11 `ActorChoice` origin, and
P18-D occurrence-receipt behavior remain present. The implementation does not
capture decision/event records or receipt contents. This is partial P12-C work;
it does not deliver P12-C provenance, P8-A roots, deterministic-random roots,
profile-wide export/hydration, P12-A readiness, P13 readiness, or Phase 12
closure.

## Exact source

The following SHA-256 values identify the code and test files in the reviewed
candidate:

| File | SHA-256 |
|---|---|
| `Assets/_Project/Scripts/RuntimeIdentity.cs` | `D8BA01B114FC97551573E852DD5783CF80B4D5624EF4569C50780A6527D284A7` |
| `Assets/_Project/Scripts/DecisionRecords.cs` | `F340BBDA4EFB463D0F585D1B51CAFBB2773D79C6758DD265BC3119735578975A` |
| `Assets/_Project/Tests/EditMode/Editor/IdentitySequenceSnapshotTests.cs` | `CD1007350078F8223119A5046A31BE8E18EEC6004F008A0AA062CA901C60A1F1` |
| `Assets/_Project/Tests/EditMode/Editor/IdentitySequenceSnapshotTests.cs.meta` | `D584020388BB4DDE814B78DE6D57AA13AC3C4D216F2A321A8C73A544E5A3E7B4` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRecordSequenceP12InvalidationTests.cs` | `71ACEFF2A4EAA67FF8E4FF035FFE129E793B1F9EBFABA4526D8DB296E0648514` |

## Results

Each XML reports zero failed, skipped, or inconclusive tests for a passing
gate. XML files are stored beside this manifest; their SHA-256 values are
listed below. `RawLogs.zip` contains all eleven Unity logs, including the two
diagnostic attempts described after the table. Its SHA-256 is
`13AE95A96F07D736D4EB56C13836FD233AD96285CACCE969AE40990B75643B75`.

| Suite | Result | XML | XML SHA-256 |
|---|---:|---|---|
| `IdentitySequenceSnapshotTests` | 4/4 PASS | `focused/EditMode-20261007-234914-39c4071ee8fb44c9ac25ec45e4df4c6a.xml` | `7C2B1DC06EA1FF9FD74ACD4B25CA257DFC6B3F0152A5AD123F7E764800F1C6DB` |
| `RuntimeIdAllocatorCensusTests` | 3/3 PASS | `focused/EditMode-20261007-235023-4f377e6b25b744409dc1798c4fad0b8a.xml` | `6A543421998E26C2AE04E770BB1F5E38DEC62506D41F27A3696EA3B3B2F9ECD5` |
| `SimulationRecordSequenceP12InvalidationTests` | 17/17 PASS | `focused/EditMode-20261007-235231-7e8bb10b6f9442cdba22cd995e9590b5.xml` | `BB2C8DC548D6812C0C674C8E778001CCE81AC31877A1D8743D46824C0DEFA72E` |
| `ActorChoiceCensusTests` | 9/9 PASS | `focused/EditMode-20261007-235317-76d00b1518e64efaabde06f0a86496b8.xml` | `A9BABDBF32270312E1F9B3288F5DFCF64D392049D31481FA2EEDB700341F3702` |
| `ActorChoiceRuntimeTests` | 11/11 PASS | `focused/EditMode-20261007-235330-901c316ab834465cbd1e516813a6a7a5.xml` | `A2ABD1FACD0C85F93E71A16EF9C097048F3E80814F8367B48C2654EA0DE59803` |
| `P18DConsumerIntegrationTests` | 10/10 PASS | `focused/EditMode-20261007-235344-356121be4dde41cbb59106b52c0fe2d9.xml` | `A424CEDC95014F32DBDCAABC62EC059181CD8B7E33EDAF9A8F04AB6557B39025` |
| Official Smoke (`Smoke`) | 5/5 PASS | `regression/EditMode-20261007-235358-523a61feca9c44a188ab65cc4b3a7f71.xml` | `61E1D03074090F946D0E0C6182946709076F74A3058202592613BC4694585D55` |
| `SimulationRuntimeLongRunTests` | 7/7 PASS | `regression/EditMode-20261007-235420-80dbbfe95b8243718f4464ae7f2fbc05.xml` | `3AEB25D880799923D63F437547C3F90B2C6A978FDDBEE289EF5481E67B1C6484` |
| ALL EditMode | 2466/2466 PASS | `regression/EditMode-20261007-235436-757998c6822b4686bfdf4372cde67ed8.xml` | `2EA0301D2B8801FEBD286F649AA79A59A642ABE66C251C3B49D5EC2F70AF4CC5` |

`git diff --check origin/codex/phase12/canonical..d9ce4502b6b1601660f2c44629d6e0f34c72036d`
passed. The XMLs and archived logs were produced by
`Tools/UnityValidation/Invoke-UnityValidation.ps1` against this worktree and
the source hashes above.

Two earlier attempts failed and were corrected before the final focused and
full runs. The first compile caught the missing explicit public default
constructor after adding the private staged constructor; the constructor was
restored, and the snapshot suite and full suite then passed. The first
invalidation integration attempt called `AllocateTravelPartyId` outside the
supported P12 runtime facade; the test now starts a party through
`SimulationRuntime.TryStartTravelParty`, proving the supported mutation path.
That diagnostic XML is preserved as
`diagnostic/EditMode-20261007-235038-b1ebfc4a71b746bcb94e07ef8b380781.xml`
inside `RawLogs.zip` (SHA-256 `1A8B93F7BB06E41CC635F553B8A345929E3C261E63C11A49D2C5747A6FF3157D`);
all corresponding logs are retained in the same archive.

Unity left `ProjectSettings/EditorBuildSettings.asset` and
`ProjectSettings/ShaderGraphSettings.asset` marked modified in the worktree,
and generated unrelated untracked `.meta` files. None are in the candidate
commit; they were left untouched and unstaged.
