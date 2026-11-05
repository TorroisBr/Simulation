# CORE-F restored Core target invariant gate

## Candidate and scope

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Implementation commit: `21cccfeb0e92aed5fad0c08224d1befe99ac8205`
- Implementation tree: `dfdb6d756d5df83bca942d34c78cde9bfb3d88fc`
- Parent/base commit: `5e2ffeb27df97be00101ae3ce18f4b36b92ed4ed`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

The shared application now validates the exact Core runtime/owner-graph binding,
Core TravelParty relationships, RuntimeIdAllocator high-water mark and required
exact-zero Expedition census before an application-backed restored target can
proceed. Unity delegates the Expedition and allocator rules to the shared Core
implementation. The legacy restore route retains its checks through those same
Core rules. Unity still validates host projection/spatial sentinels, NPC census,
PoliticalKnowledge bindings and the exact projected TravelParty graph.

The Core stage transaction, image-to-target comparison, complete census,
mutation-epoch recapture, boundary admission and one-publication rules remain in
their existing order. This checkpoint does not complete `TryStart`, standalone
restoration, the detached .NET host, T/T+1/T+2 continuation or CORE-F. No
canonical promotion is claimed.

## Tested source identity

The official runners executed before the implementation commit was created.
The five implementation/test files were unchanged by that commit; their tested
worktree bytes match the committed blobs below. The Unity manifest binds the
complete execution input snapshot to digest
`ea8340d62bba3515acc97aa9b6b58e5b5cca9cc892e08d71d545cb5b29feeb1c`.
The CoreValidation manifest binds its input snapshot to digest
`7c239aa71c2dfd811a75f876aac398a41cab87b4ae76bb9d709ad4e88f60bf9e`.

| File | Git blob | Tested file SHA-256 |
|---|---|---|
| `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.ImageRestore.cs` | `12cb295833e9297e1f5d4849c01676dde22c1ae2` | `eb2cd1d3f405d6b605aba4af06912616ef002ae0501151ccfe8937a0b8e716d3` |
| `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.cs` | `72fe63b4811cd8822e1dbc38bf0664d435b29d93` | `734efed27eb6626d550bbd4923ab25f31f6c7ea88111a07a9f4af0fb29ae2902` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` | `3289e4116504b056a459b152e7d04616fcffae72` | `d277742b295dcd36e4704a96e93d242f57319c4f2c62ed9659c324b09d9e30eb` |
| `Packages/com.simulation.core/Runtime/Persistence/DailyV1RestoredOwnerVectorContract.cs` | `4a318760b86d629e446484c96f021d0c3fb268f9` | `dd2497a786cbda7c6f6f97bc8c62433995f463701f04c172f9c824d6d4372537` |
| `tests/Simulation.Core.Tests/DailyV1ApplicationHostTests.cs` | `d439b8751c5bc40bb6b18b5f70bcb5ee9b2b4080` | `5cd49542fc0b70940d8eebea0bca7c63c52558537c0435f87a2ee58fa8b18fbe` |

A pre-existing user-owned `ProjectSettings/ProjectSettings.asset` modification
was present during validation and is recorded in the protected runner inputs. It
was not changed, staged or included in the candidate commit. No other local or
protected files were staged.

## Validation

| Gate | Result | Evidence |
|---|---:|---|
| Focused Core `DailyV1ApplicationHostTests` additions | 2/2 passed | Included in the cold/warm CoreValidation run below. |
| Official CoreValidation cold | 1,019/1,019 passed; zero failed, skipped, inconclusive or not executed | Run `aac6561b487749acaa0b3cf97134929c`; protected manifest SHA-256 `6615b9d9216008d65be4d8bd14ef7d9a120c882e44919a4fa1185d6a0cf3bd2d`. |
| Official CoreValidation warm | 1,019/1,019 passed; zero failed, skipped, inconclusive or not executed | Same run and manifest. |
| Focused Unity `SimulationRuntimeAdmissionTests` | 243/243 passed | Run `841a935829484aa6a16079eddd282892`; protected manifest SHA-256 `f4c7905d6ecedd1b953e94a25f877427c2a0b3739fa252207f71321854a7bf98`. |
| Full Unity EditMode | 3,299/3,299 passed; zero failed, skipped or inconclusive | Run `5ecafc38f81b4115a0a6fdb7417ae40d`; input stable. Original XML SHA-256 `af24cb31134853f8bacfb28fac0b6307e22bfef26aa45d0dce580fdbd24ccb39`. |
| Filtered Unity Smoke | 5/5 passed; zero failed, skipped or inconclusive | Run `47d78712e85b40698d5db035ab6e71b0`; input stable. Protected original XML SHA-256 `ad2f630845b177cb09a5653d0b51b92e035dc70cdec42d2ed469da65165a6732`. |
| Privacy-safe full EditMode package | Valid; one result-only XML entry | Archive SHA-256 `1a0d6dc99481852bd0fc7fc88260503cdd9ca6c8bd1fe07987c218f752244943`; package manifest SHA-256 `755d71fee3324ea24dc3a9e9898e4f56a184ec9277fa66fe22bc460745312a9c`. Official package verification against the protected original passed. |
| `git diff --check` | Passed | Before and after the implementation commit. |

The official result-only packager rejected the separate filtered Smoke XML because
the Unity suite root's `testcasecount` does not equal its filtered descendants.
No Smoke package was retained. The filtered report remains unchanged in
protected evidence; all five Smoke tests and outcomes are also present in the
verified complete EditMode package. The public EditMode manifest and archive
were scanned; neither contains private workstation paths or account/machine
identifiers.

CoreValidation's originals and native manifest remain in protected local
retention. No public TRX package was created: the recorded run had a Unity
`Library` present, while the approved DotNetTrx source-context contract requires
a Library-free Core worktree for a package that claims that proof. The private
manifest records stable source/dependency inputs and the complete cold/warm
reports; its hash above binds those retained bytes. This is not the final
Library-free .NET host proof and is not represented as one. A future promotion
or final CORE-F acceptance must use its applicable approved TRX package workflow.

## Review and remaining work

Independent exact-tip review of implementation commit
`21cccfeb0e92aed5fad0c08224d1befe99ac8205` remains required. The candidate is
not promoted. Continue the approved shared-runtime and restoration work after
review; this target-invariant slice alone does not establish detached
restoration or a Unity-independent host.