# CORE-F Selected Genesis Genealogy Owner — exact-tip review

## Verdict

**VALIDATED_CANDIDATE** for this bounded selected-genesis integration. This is a review result only; it is not Core canonical promotion, P12 scope promotion, or CORE-F completion.

## Reviewed identities

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Candidate commit: `399470abd67462251baf5091f5b227ec02229be3`
- Candidate tree: `6aa0e8af441f37e37a1a22f0cd99b2b5d5f181fc`
- Immediate base commit: `ee8c7bd41f8a44bdd97ffc68614c4ea2b6f115f1`
- Base tree: `66a30a593a0703a291b6bb0f4b584c7ebebbe271`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea` (local and remote refs agreed)
- Candidate local and remote branch refs both pointed to the exact candidate commit at review.

## Scope and implementation review

The complete base-to-candidate diff contains exactly eight paths: five production/test files and the three scoped validation files under `docs/validation/CORE_F_CoreOwnedRuntimeApplication/FreshGenesisGenealogy_20261029/`. It changes no `.meta` file or GUID, ProjectSettings, private evidence, or unrelated/untracked file.

The Core `DailyV1GenesisComposer` constructs one new empty `GenealogyStore` and binds that exact instance into the selected runtime owner graph before the graph is attached to the execution boundary. The bind operation rejects null and refuses replacement by a different store. The newly created owner remains private until the composed graph is returned; this adds no writer, row, P12 wire field, or mutation.

`TesteSimulacao` forwards `selectedCoreGenesis.OwnerGraph.GenealogyStore` to `SimulationRuntime`. The runtime first resolves an explicit store or, when absent, the selected graph's store; it validates the store against the Person owner, then requires reference identity with the graph owner and throws on mismatch. For selected genesis it retains the graph's exact owner, so the runtime and P12 census source do not acquire a parallel clone. The normal no-graph compatibility path remains unchanged: ordinary construction uses `CloneGenealogyStore`; restored continuation retains its existing `CloneRestoredGenealogyStore` behavior. No P12 codec, snapshot, stage order, revision, or epoch semantics changed.

The added Core test asserts that selected genesis has exactly one empty owner at count/revision zero and that a different owner cannot replace it. The Unity composition test asserts that the runtime retains the same Core graph and exact GenealogyStore instance at count/revision zero. No prior assertions were weakened. The explicit runtime mismatch guard is verified by source inspection; this diff does not add a direct negative constructor-mismatch test. That is a non-blocking follow-up test recommendation if the guard or constructor boundary changes.

No `.meta` files were added or changed, so Unity script GUIDs remain untouched. The affected P12-D exact-empty genealogy census continues to observe the runtime's same installed owner; this is not a change to P12 save scope or readiness.

## Validation and evidence applicability

The committed `VALIDATION.md` source hashes match the stable official runner input manifests for all five changed source/test files. The worktree files at review had those recorded byte hashes; the Core repository uses `core.autocrlf=true`, so committed blob identities remain the separate Git-level identities.

- Official CoreValidation cold and warm: **933/933 each**, zero failures. Private manifest run `5c222d9598a14c58b1601c448710220b`; manifest SHA-256 `B5B6F52BE4375ED2D5CD78D88AAEC507326B1DA80B95FDF5D61FE7EFD4F139AA`; stable input digest `4fcb7ad57df3729d57d37622e15cfa3ef81c11f139f5e93b128dcf1d14cb9acc`. The input manifest records the changed Core composer and test file hashes from the validation record.
- Unity focused selected-genesis test: **1/1**. Full EditMode: **3279/3279**. The changed `SelectedDailyV1CoreGenesisOwnsWorldIdentityAllocation` case appears once and Passed in the packaged full report. The three runs had stable project input digest `543a609c979e0137474833ce5f78673b93d8b99720e4d78b7087f56855044fa3`.
- Official filtered Smoke: **5/5**, coherent counts and stable input; all five exact passing Smoke case identities are also present in the full EditMode report.
- Full EditMode package ZIP SHA-256: `73d0fff25c8afceae635ff055e052b3c28707fc62aa3a1d73c9d42c061ae808e`; public manifest SHA-256: `1f09bf5ff2ec02e66e42fb428b0994f2c2cad910642b7fe69b9deade10fd0802`. Independently checked archive hash and XML totals (3279 passed, zero failed); the public package and manifest contain no local workstation path or detected credential pattern. Protected originals remain outside the commit.
- `git diff --check`: **PASS**.

The tests were recorded against stable inputs. The Unity runner input snapshot includes the checkout's preserved user-owned ProjectSettings state, while those files are excluded from the candidate commit. The tested implementation/test source identities match the committed candidate; no ProjectSettings file was staged or changed by this candidate.

## Findings and limits

No actionable implementation defect was found. The diff establishes only selected fresh-genesis creation/binding of the empty GenealogyStore and Unity adoption of that exact Core owner. It does not complete or promote CORE-F, alter Phase 12, or imply additional save-profile coverage.
