# Independent exact-tip review — P12-D selected row-value extraction

## Verdict

**PASS.** No actionable findings for the bounded row-value source relocation.

## Reviewed identity

- Candidate branch: `codex/core/CORE-F-p12d-core-hydration`.
- Exact reviewed candidate: `7aeec2eb3d9e2c360be9bd13a6176c25b61d027c`.
- Candidate tree: `5df6a5675e9c29a6cee6eee09c055ff7a1e190fe`.
- Base: `d6b18334bb1ec7e4dcd73fa1ce34b78ded9921bd`.
- Implementation commit/tree: `74caa0bb16b1a4ddf9a544f2616eaa60a297b3fd` / `084e32be0a9f1fb6eb5f86792719d8770352723f`.
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Independent reviewer: `/root/p12d_hydration_exact_review`, using a clean detached review checkout. The reviewer did not modify candidate or evidence files.

## Findings

The change moves only the internal value types `P12DNpcInventoryRowValue`, `P12DNpcDRow`, and `P12DCityMarketItemSnapshot` into the existing `Simulation.Core` source assembly. Their class bodies match the base versions after line-ending normalization. The Core assembly has no Unity engine references; existing friend-assembly access preserves current Unity callers. The candidate adds no test changes and does not commit either protected ProjectSettings modification.

The change does not move P12-D capture, image staging, City/NPC runtime hydration, or the P12-G restore coordinator. It remains a partial source extraction, not a completion or readiness claim for P12-D, P12-G, or CORE-F.

## Validation evidence verified

- CoreValidation run `1b5cde3fbb5644898d7c49d849565c7b`: cold and warm `723/723`; stable input digest `d8d274bbf5dbee916b80ba14af70c476b21e743b6f3f794c2a5064a72e22f46c`; private manifest SHA-256 `5097d994247e3c28ddd46da011309f491ca0fd6877dd617d9713578a80666c77`. Original cold/warm TRX SHA-256 values are `3a1435e916ec5e4b0126327ca7ec384e55ee4a0f998f3cd0386dfd55ef551411` and `719a7afbeee2bcab3d9bd51ec35141d5c6e6eedd21abee30baca3d30fca38422`; these remain private, and the repository has no approved .NET TRX adapter for this run.
- Official Unity EditMode run `ed85134d69044d829c521a063a7be453`: `3263/3263`, zero failed/skipped/inconclusive; Unity `6000.3.9f1`; stable 1,874-file input. Run manifest SHA-256 `7d8c4c60e2894fec15356a8dd99a7654c037c1ed5175a581faf423ea5f8616c4`; original XML SHA-256 `293fb4414673dc405e88d45d643d4caa797ab5eff9644cb46356e203a1f9e561`.
- Official Smoke run `9839d70de4f84530856c54df8e3b1e53`: `5/5`, zero failed/skipped/inconclusive, same source input digest as the full run. Original XML SHA-256 `571cf44dc4af2ed36d409244f4707eff26a5c6d4c00deca1ccefbfe71c073860` remains private. Its filtered XML was rejected by the packager because nested suite counts describe full discovery; the original was not modified. The reviewer verified all five Smoke identities and passing outcomes occur in the packaged full report.
- Official result-only full EditMode package SHA-256 `0cdb05676f4cbed5bb35df7f6c2d965118fca691666dc86884905cd3785cbdfa`; package manifest SHA-256 `7bb4bf4a8275be751e1e9e15294dfdd9386423422eb658d6edab1b562c02b7de`. Package verification passed against the protected source-run manifest. The normalized report contains 3,263 passes, zero failures/skips/inconclusive, no XML output nodes, no absolute drive paths, and no detected credential-pattern matches.
- `git diff --check`: PASS.

## Applicability and limits

This review applies to the exact source tree and evidence above. A docs-only successor may record the review while retaining applicability because it leaves the reviewed source and test tree unchanged; the exact reviewed implementation commit/tree remain identified above. This is a candidate review record only: it does not promote Core canonical, close CORE-F, or establish broader P12 restore readiness.