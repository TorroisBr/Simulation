# Incremental Core/P12 epoch-owner validation — 2026-10-22

This record covers the additive epoch-owner change and its validation-source
closure correction. The executable candidate tested here is
`6248829fee6dd6dd10fd2836d8973afc0d9e5bb0`, tree
`4cb77b76401eedbe6113eb1175902732b74c7f2a`. The prior implementation base is
`e65b8803d62f7cf66b42c5c94562841affd42093`, tree
`884c808bef704613f31a706d0ff5217a6f93f002`. This remains an incremental
integration checkpoint, not a completed CORE-F package or canonical
promotion.

## Change

`DailyV1ExecutionBoundaryState` now owns the selected runtime's
`DailyV1MutationEpochState`. A `ContinuationCensusProtocol` attached to that
runtime uses the same object; standalone census protocols retain their
independent epoch state. The Unity integration test asserts exact reference
identity while retaining the existing operation-registry behavior.

The CoreValidation runner now asks the pinned MSBuild SDK for the evaluated
`Compile` and `ProjectReference` inputs of the projects it executes. It
recursively hashes repository-contained compile sources and project files,
including linked Core sources and transitive project source. Generated
`bin`/`obj` source is excluded; project/configuration files, package locks, and
generated dependency snapshots remain separately bound. Self-tests assert that
the changed execution-boundary source, its .NET test, and the transitive host
identity source are included under repository-relative identities. The Lab
runner selects its own project graph explicitly.

## Exact-source validation

The official CoreValidation runner passed cold and warm **640/640** on the
executable candidate above. Private run ID:
`ee0302626fe443dc96d68940eb478602`; protected manifest SHA-256
`fe6e0e31ab9394a790edd381e9202c5c43f399d54437e4bfaa49a7c943c7ec1e`. Its
manifest records 337 inputs
with digest
`43def510d476ae3f24a9e3d221aa5d7938f3f281d2d0bc8bdabb50a75aaf7f21` and binds
`DailyV1ExecutionBoundaryState.cs` to SHA-256
`3259eaaae85d7cb11b2653c948d339c677321b1eaf4494ddfb59bdd4188caf9f`.
The preserved cold and warm TRX reports are private; their names, byte counts,
and hashes are recorded in the protected manifest. The official runner
self-tests passed, including the transitive compile-source closure assertions.

| CoreValidation report | Bytes | SHA-256 |
| --- | ---: | --- |
| Cold `CoreTests-cold.trx` | 915,521 | `b1fbf1d80eb798086923c11c205c4460f108342182dd91b7e2643a3704595212` |
| Warm `CoreTests-warm.trx` | 915,521 | `dbf1f8c096a8fbc23add4b8c41e655f371690ab90db848adb4f8ebd69a4553a8` |

This CoreValidation run occurred in the Unity worktree, which contains a
Unity `Library`; it does not establish Library-independent .NET execution.
That separate .NET-only confirmation remains a full-package validation gate.

The official Unity EditMode runner executed
`ContinuationCensusProtocolTests`: **26/26 passed**, with zero failed,
skipped, or inconclusive tests. Private run ID:
`b51c8ae0c1784fcfacc87fe4460fa7ff`; protected manifest SHA-256
`868254cfc0e580fd25c0b5efac7d13a5100405a3843a6a3b719842330f724096`. The
manifest records a stable
1,846-file input snapshot with digest
`54feb141c0212cdb17610216af0cfaedec70818b0800cc2b6e628d7ae70f4a67` and exact
repository HEAD/tree matching the executable candidate. Its relevant source
hashes are:

| File | SHA-256 |
| --- | --- |
| `Packages/com.simulation.core/Runtime/Application/DailyV1ExecutionBoundaryState.cs` | `3259eaaae85d7cb11b2653c948d339c677321b1eaf4494ddfb59bdd4188caf9f` |
| `Assets/_Project/Scripts/ContinuationCensusProtocol.cs` | `bc1c0d14d76a131a24c1b7412f3db1d910cc77975a84c2b0e5bc4d8048d676f4` |
| `Assets/_Project/Tests/EditMode/Editor/ContinuationCensusProtocolTests.cs` | `15f6f2deeffdd93f630a357de302e5c2049ddaf189cc837c28e1f96615871118` |

The private original Unity XML is 18,943 bytes with SHA-256
`ab5acacda135b022f65955fe65a83118062209b3494e5e379d76dc9d86f4fa0c`; the
private log is 38,207 bytes with SHA-256
`4b55bbc0706948873dbf03288fe53c67bde3ea20c0a734f9d3568e8195c2dcf1`. Both
remain in the protected run directory and were rehashed against the manifest.
The prior cited Unity and CoreValidation run IDs could not be located by the
independent reviewer; this record supersedes those citations, and no result
depends on them.

`git diff --check` passed for the implementation, test, runner, and evidence
record changes.

## Scope boundary

This change does not implement the shared application/session, move P12
capture or detached restoration, add an executable .NET host, or establish
T/T+1/T+2 parity. The complete CORE-F integration remains open and unpromoted.

## Exact-tip review correction — 2026-10-22

The independent review of candidate `e1c6c57ee5c1ae54531ef4148b9bab6d0c226fa9`
verified the referenced Core and Unity evidence, and identified a containment
gap in the CoreValidation source-identity helper: lexical path containment did
not reject an in-repository junction or symbolic link that targets an external
source. The reviewed 337-input run contained no reparse-point paths, so its
result remains valid for that exact input set.

Candidate `6a7b80fdf3be70673af61e72a7916bdfd67681c1` now fails closed when the
repository root or any path component below it is a filesystem reparse point.
The CoreValidation self-test creates a temporary in-root junction to an
external source and asserts rejection. The normal CoreValidation runner passed
cold and warm **640/640** on this exact commit. Its protected manifest SHA-256
is `353d562300b895b88b91f3ea2c5a9701218c5d1a0c7d00125a104255515cc94b`; the
stable 337-input digest is
`a5280ddc791c5cfa2c4f9f57df38b998ae27f16dd42a66d6e224a98a6148d5be`. The
private cold and warm report hashes are `39c682a37449a0c3555a14cd79f8176a64e249736d340ba58b0b51e43f41f072`
and `b4974558fb14460bc8022473268aaa56310d31e76c9fdf118a4672512666d435`.
The evidence directory ACL check passed. The Unity Library was present during
this .NET run; no Library-independent execution claim is made. Unity code and
test inputs were unchanged from the exact-source Unity run recorded above, so
this runner-only correction did not require a Unity rerun.

## Second exact-tip review correction — generated-output containment — 2026-10-22

The independent review of candidate `f3ea1b36d4aef7f5f3a9e508fdbc52b6af67c62b`
found that `Get-CoreProjectCompileInputs` excluded SDK `bin`/`obj` items before
validating their paths. That ordering could bypass the source-containment
check for a compile input traversing a junction under a generated-output path.

Candidate `3b93196461c3388bf66a386b758216dd9c7f75b9` moves containment and
existing-component reparse-point checks ahead of generated-output exclusion.
Missing leaves are tolerated only for recognized generated-output paths; any
existing junction in those paths is still rejected. The self-test retains a
direct helper check and now creates a synthetic SDK project with a compile
item beneath an in-repository `bin` junction, then asserts that the actual
`Get-CoreProjectCompileInputs` caller rejects it. Self-test output is retained
in protected evidence (SHA-256
`600576d2362379c6bddae9ad75d61278aa342c1611fc4291ba1bf8468de8d7bb`).

The official CoreValidation runner passed cold and warm **640/640** on exact
commit `3b93196461c3388bf66a386b758216dd9c7f75b9`, tree
`e2755cac3a301a8f0993375956272eef6a26bef7`. Its protected run is
`b074a1c56a4a4290ae7fb4b134bcdee8`; manifest SHA-256 is
`c751822e90ccc2c6aa95583720efde953c7f07c4551bbf9c3222297b8d425a21`, and the
stable input digest is
`2675e48c4e09ee553400f2a99a9f50300baf71b50c682bd78cb97b178e3232e2`. Cold
and warm TRX SHA-256 values are
`fd97091ed0161440870866b64a73772332817052b83642a45a802b45a7ae55cf` and
`c4ce012a873d09c82e41e31a6341063426c21efbf2921cb067cdcbaeec8b6f34`; the
protected evidence ACL checks passed. Unity production/test sources and their
inputs were unchanged, so this runner-only correction does not require
repeating the retained Unity run.
