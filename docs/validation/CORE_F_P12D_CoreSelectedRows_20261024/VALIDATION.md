# CORE-F P12-D Core Selected Rows Validation

Date: 2026-10-24

## Candidate identity

- Candidate branch: `codex/core/CORE-F-p12d-core-hydration`.
- Base candidate: `d6b18334bb1ec7e4dcd73fa1ce34b78ded9921bd`.
- Core canonical at validation: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Code commit: `74caa0bb16b1a4ddf9a544f2616eaa60a297b3fd`.
- Code tree: `084e32be0a9f1fb6eb5f86792719d8770352723f`.
- Working-tree source overlay, captured before the code commit: stable Core input `d8d274bbf5dbee916b80ba14af70c476b21e743b6f3f794c2a5064a72e22f46c`; stable Unity input `d5d25badc22b19d7542e498bc8691427e2e525a2418bba94d6755674cc23e0eb`.
- The source files and new metadata in the Unity input inventory were hash-compared with the committed files and match. Test sources were unchanged.
- Two pre-existing local ProjectSettings edits were present as unchanged validation inputs and remained outside the candidate commit. Their contents were neither modified nor staged.

## Delivered scope

Moved only the plain selected P12-D row value types `P12DNpcInventoryRowValue`, `P12DNpcDRow`, and `P12DCityMarketItemSnapshot` into the existing `Simulation.Core` source assembly. The original type bodies are text-identical after line-ending normalization. The types remain internal; the existing friend-assembly declaration provides the current Unity compatibility access.

This does not move P12-D capture, image staging, City/NPC runtime hydration, or the P12-G restore coordinator. It is partial restore-source extraction only; P12-G and CORE-F remain incomplete. No Phase State, canonical ref, public API, or runtime behavior changed.

## Validation

- Official CoreValidation run `1b5cde3fbb5644898d7c49d849565c7b`: cold `723/723`, warm `723/723`; SDK `9.0.315`; test input stable. Private manifest SHA-256: `5097d994247e3c28ddd46da011309f491ca0fd6877dd617d9713578a80666c77`. Original cold/warm TRX SHA-256: `3a1435e916ec5e4b0126327ca7ec384e55ee4a0f998f3cd0386dfd55ef551411` / `719a7afbeee2bcab3d9bd51ec35141d5c6e6eedd21abee30baca3d30fca38422`. They remain in private E: evidence; this repository does not yet have the approved .NET TRX adapter.
- Official Unity EditMode run `ed85134d69044d829c521a063a7be453`: `3263/3263`, zero failed/skipped/inconclusive; Unity `6000.3.9f1`; input inventory `1874` files, stable before/after. Source-run manifest SHA-256: `7d8c4c60e2894fec15356a8dd99a7654c037c1ed5175a581faf423ea5f8616c4`; original XML SHA-256: `293fb4414673dc405e88d45d643d4caa797ab5eff9644cb46356e203a1f9e561`.
- Official Unity Smoke run `9839d70de4f84530856c54df8e3b1e53`: `5/5` passed, zero failed/skipped/inconclusive; it used the same stable input digest as the full EditMode run. Original XML SHA-256: `571cf44dc4af2ed36d409244f4707eff26a5c6d4c00deca1ccefbfe71c073860`. The official packager rejected the filtered XML because Unity retained full-run counts on nested suite nodes; the original was left untouched and remains private. All five Smoke cases also occur in the packaged full EditMode report.
- Result-only package: [full EditMode results](coref-p12d-core-selected-rows-20261024.zip); [package manifest](coref-p12d-core-selected-rows-20261024.manifest.json). Archive SHA-256: `0cdb05676f4cbed5bb35df7f6c2d965118fca691666dc86884905cd3785cbdfa`; manifest SHA-256: `7bb4bf4a8275be751e1e9e15294dfdd9386423422eb658d6edab1b562c02b7de`. Official package verification against the protected source-run manifest passed. The bundle retains the complete full-suite report, including the Smoke test identities/results; it removes XML output and machine-specific absolute Assembly suite paths as declared by the package policy.
- Original Unity XML/logs and both source-run manifests remain unchanged in private E: evidence retention; none were added to Git.
- `git diff --check`: PASS.

## Review and limitations

Independent exact-tip implementation review is pending. Do not treat this record or code commit as canonical promotion or CORE-F completion.
