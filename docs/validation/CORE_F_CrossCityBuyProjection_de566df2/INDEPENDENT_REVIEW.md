# Independent exact-tip review — Cross-City BuyGoods projectionless dispatch

Verdict: **VALIDATED_CANDIDATE**. No actionable findings. This review does not promote Core canonical or claim CORE-F completion.

## Reviewed identities

- Repository: `https://github.com/TorroisBr/Simulation`.
- Candidate branch: `codex/core/CORE-F-cross-city-buygoods-projection`.
- Reviewed exact tip: `0dbf3413a7c0e5c0a143690fc47b0ff30f6681d3`.
- Reviewed tip tree: `dc503d02be00a01984de965a8994db551dc08f09`.
- Exact base: `ef2007e274b47ed62b93d45d974802abd6d64c72`.
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Production implementation: `954e265449a2e76e4c9c9d67395ff9dccb86df3e`.
- Test fixture path-only correction: `de566df204c571115592cb889fd767f60ed8b9cc`.
- Candidate branch and remote refs matched; the review checkout was clean.

The reviewer inspected the complete base-to-tip diff in a separate read-only checkout. The final tip adds only evidence documentation, manifest, and result archive after the validated code/test tip. The final full EditMode run is bound to the exact implementation/test tree at `de566df`.

## Findings

The new projectionless path accepts only the exact admitted cross-city `BuyGoods` payload shape: a positive amount, no NPC target, no active Merchant plan, an actor whose exact current City matches its Travel owner, and a distinct target City. Recovery resolves exactly one target City and the selected `ItemData` binding, and installs only the Unity compatibility projection without a P12 mutation notification. Missing or duplicate projections fail before RNG or owner writes. The existing Core operation remains authoritative for route and purchase eligibility, deterministic resolution, ordered admissions, mutations, and failure handling.

The additive regression covers missing and duplicate destination projections before RNG/mutation; successful recovery without changing the census epoch; successful Core dispatch; expected revisions and the five committed owner epoch changes; and final census admission. Assertions were not weakened. The P12 test change only shortens a temporary fixture directory name to remain under the Windows path limit. No unrelated files are part of the candidate diff.

## Validation evidence checked

- Final official Unity EditMode run `158d3c5528dc4b56a96a50282e615f93`: 3,294/3,294 passed; zero failed, skipped, or inconclusive. Stable input digest: `81da27a0d6c7079ff6bf0b5a5724c882d0283ff55355b0f4498dfb5150d4a73a`.
- Official Smoke run `91a79c969680454d93a5e90f61808242`: 5/5 passed on the same stable input digest. All five Smoke identities also appear as Passed in the packaged complete EditMode report.
- The official evidence verifier accepted the complete EditMode result-only archive against its protected original run manifest. Archive SHA-256: `80d1acbe7c19f4cd700a3c16ee7234110de4e94c5cbc5f192020eba817004522`. Public manifest SHA-256: `89163de7b9b8c1dd779eb039ad066d94fc197e230f7f51b7272d0547c8c3fb35`.
- No sensitive local path or credential-like pattern was found in the public manifest or archive. `git diff --check`: PASS.
- No CoreValidation result is claimed; this candidate changes a Unity adapter and tests, not Core source.

## Integration limits

This checkpoint addresses one projection-recovery gap for an existing Core operation. It must integrate only into the existing CORE-F candidate lineage and remains serialized with other `SimulationRuntime`/P12 work. It does not complete the shared runtime, `DailyV1Application`, detached restore, .NET host, or CORE-F; Core canonical remains unchanged.
