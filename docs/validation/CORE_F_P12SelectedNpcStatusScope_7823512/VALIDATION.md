# CORE-F selected NPC status scopes — validation

## Scope

This candidate changes how selected NPC status-only mutations derive P12 section bindings: the Core owner graph supplies the bindings, while the Unity bridge uses the Core coordinator only when every distinct participant is selected and P12-bound. Mixed/unselected and Justice-containing operations retain their existing host path. The change does not complete the shared runtime, application, detached restore, or .NET host contract.

## Source identity

- Repository: `https://github.com/TorroisBr/Simulation`
- Candidate implementation commit: `7823512fade5a0d8c797155446efef40ba5076f3`
- Candidate tree: `329b7727bad4b442465ff8f86e598b803b5f070c`
- Parent/base: `64a63a90beff495ccefdee135f1f90e1b6c92399`
- Canonical Core baseline: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Implementation files: `DailyV1P12CrimeJusticeMutationCoordinator.cs`, `P12CrimeJusticeInvalidation.cs`, and `DailyV1P12CrimeJusticeMutationCoordinatorTests.cs`.
- The implementation source hash recorded by CoreValidation is `fd6c37ac13e7882f1c031c6553b304d84876d5a24e991f4c3563ec5f102d0cd7`; the test source hash is `b6279ae6a419d5c4babf8db25242b2b96da95373d303325c9917fc635333109a`; the Unity bridge hash is `40d65f213e9b9cfba11b80891045ad4a3cf6f2c4589017a7dfab3f89c975ddf5`.

## Validation

- Official CoreValidation, run `4f321fa732384dd5bf72068e4b96b97c`: cold 825/825 and warm 825/825; zero failures. Stable input digest: `f5e4467de3d5fea7b2aad2ac6f174a206148606e16b0671dd7f5e0435c2d4291`. Run manifest SHA-256: `1553db7f366f0c522a788e272f61f487d3b6ebb3b50d7df7de97fe30895f9743`; cold TRX SHA-256: `bc7d9f219b91026f7389161ff584ebae51d609384fc0ef5105970b0bc1550c0a`; warm TRX SHA-256: `91d1203a7ea29817adcfcdb518f373b34df13f783e682bf01b52e74fd67b0a9d`.
- Focused Unity test `ProjectionIndependentNormalActionCommitsActorStatusFromCoreSlot`, run `a11153fd4ed84926b0bc185034996110`: 1/1 passed.
- Full Unity EditMode, run `797195c8a2d84f3b90f565f578724015`: 3269/3269 passed, zero failed, skipped, or inconclusive. Input snapshot digest: `a3f497d3ba6904e1d6d4beff778e140fae7e0a615f3d2e6794bf6efac089b150`.
- Official Smoke, run `7b340b4d03c248b7a9829d0bce9c1ac0`: 5/5 passed, zero failed. It used the same Unity input snapshot digest. Original XML SHA-256: `83b1d6e86087af535f18eac6bf1e0b205951920bfc98c3420985093bcefbaff9`; raw log SHA-256: `d979015da9bae6f9a300d986f4b3c7657db1f3c11af4bac0dc364c1c32cb14b1`. Both remain in the protected E-drive evidence store.
- Unity result package: `Evidence/CORE-F-P12SelectedStatusScope-7823512.zip`; 172804 bytes; SHA-256 `3c4ed47c5aa424e82759b44939d20bdfdd55728085238aaa2aa14142e637ad85`. Package manifest: `Evidence/CORE-F-P12SelectedStatusScope-7823512.manifest.json`; SHA-256 `4965d3d49216f41e4f5365264fc9ff80430c5ca5881142fdaead296e6409e905`. The official verifier reported `VALID: runs=1 entries=1`; the archive contains the normalized complete EditMode result report. The current packager rejected the Smoke XML because its root `total=5` and passed count are coherent, but the XML advertises suite `testcasecount` values from the unfiltered assembly that do not match the selected result subtree. The official runner still reports the Smoke run as passed. The original was left unchanged and private; no hand-normalized replacement was made.
- `git diff --check`: passed for the implementation change.

## Evidence boundary and limitations

Unity ran from the captured worktree input snapshot whose Git HEAD was the parent listed above; the runner also hashed the complete project inputs. The implementation and test source hashes in that snapshot match the committed candidate files. The snapshot includes the two pre-existing local `ProjectSettings` modifications; those user changes were not staged or committed and remain untouched. The normalized Unity package is the durable EditMode result package for this checkpoint. Smoke has a passed original runner record, but no normalized package yet; this packaging incompatibility is a limitation for future permanent evidence, not a failed test.

CoreValidation's original TRX and logs remain in the protected E-drive evidence store. This record publishes only their hashes and results; it does not claim that a normalized `trx-vstest-flat-v1` source-context package was produced. Therefore this is a validated candidate checkpoint for independent review, not a complete CORE-F acceptance or canonical-promotion evidence package.

The first CoreValidation invocation was a runner path-resolution error and executed no tests; the successful invocation above corrected the working directory. No test outcome was inferred from that failed invocation.

## Status

The owner-graph scope derivation and Unity bridge are a bounded candidate change. CORE-F remains incomplete and unpromoted. Shared `DailyV1Application.TryStart`, Core-owned full runtime/session composition, shared P12 detached restoration, and fixed .NET host/continuation remain open.
