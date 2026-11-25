# Shared viewport library implementation

## Candidate provenance

- Implementation branch: `codex/shared-ui/viewport-library`
- Architecture baseline: `codex/architecture/world-identity-projection` at `1ad2835524fe135c87eeca13f15cd16d4d9cb303`
- Implementation source commit: `ef9bcdcd8c41cf341fa08160738465da62d2aa20`
- `SharedDesktopUi` source tree: `8f7b204878c1235b1e0c049762daff537407c92b`
- Required SDK: .NET `10.0.404`
- Shipping project: `Simulation.Ui.Viewport`, package ID `Simulation.Ui.Viewport`; `IsPackable=false`, with no NuGet package published.
- Test project: `Simulation.Ui.Viewport.Tests`; NUnit `4.6.1`, `Microsoft.NET.Test.Sdk` `18.12.0`, NUnit adapter `6.3.1`.

The implementation is confined to `SharedDesktopUi/` and this validation record. No World Atlas or Dungeon Studio files were changed.

## Public contract and numerical behavior

The library exposes `ViewportPoint(double X, double Y)`, `ViewportYDirection.Down` / `Up`, and immutable `ViewportTransform` state for local origin, viewport center in DIPs, pan in DIPs, positive scale, and Y direction. Its operations are `TryProject`, `TryUnproject`, and `TryReanchor`.

Each operation validates the complete transform and its inputs, checks finite intermediate and output values, and leaves its `out` value at `default` on failure. It uses the documented component order, performs no rounding, clamping, snapping, or scale policy, and allocates no managed objects in ordinary projection operations. Equal-scale reanchoring still inverse-maps the pointer and recalculates pan.

## Validation results

| Check | Result |
| --- | --- |
| Locked restore | Passed for `net10.0` and the `win-x64` graph from the committed lock files. |
| Release solution build | Passed with 0 warnings and 0 errors. |
| Release `win-x64` build | Passed with 0 warnings and 0 errors. |
| Complete NUnit suite | 22 of 22 passed. This includes all four directed endpoint transitions: Atlas `1e-9 ↔ 5` in Y-down and Dungeon `0.002 ↔ 0.8` in Y-up. Each case checks the exact reanchored pan components and anchor preservation within `1e-7` DIPs. |
| Clean-checkout reproducibility | Passed in `.worktrees/shared-ui-viewport-clean` at implementation source commit `ef9bcdcd8c41cf341fa08160738465da62d2aa20`, after clearing project build outputs, with a separate NuGet cache and no local feed. Locked restore, both builds, and all 22 tests passed. |
| Allocation check | Passed; project, unproject, and reanchor loops allocated 0 managed bytes. |
| Dependency inspection | Shipping project has no package dependencies. Its compiled assembly references only `System.Collections` and `System.Runtime`; no Avalonia, Dungeon, Atlas, Unity, or Simulation Core dependency was found. |
| `git diff --check` | Passed. |

The mandatory regression uses X origin `0`, viewport center `500`, initial pan `200.0005`, equal old and new scale `0.002`, anchor `89d / 3`, and click X `700`. With reanchoring the half-centimetre midpoint calculation rounds to `-1`; skipping the pan recalculation rounds to `0`.

## Evidence hashes

| Evidence | SHA-256 / Git tree |
| --- | --- |
| `SharedDesktopUi` source tree | `8f7b204878c1235b1e0c049762daff537407c92b` |
| `ViewportTransform.cs` | `705BCCC87A5079C5EF086DA7CCFEC9F20B82745F40D3E69A9AC8ED276D9B0841` |
| `ViewportTransformTests.cs` | `C5B733A4C9F864690EFFB69549FAC3F1A2193AD989FBA1E13127962E772956C9` |
| Library `packages.lock.json` | `1E39B4149E1289978E01A366893A0697356E855BC7156B7C70747288030042F9` |
| Test `packages.lock.json` | `2949AE64C05DD30EE1897A4FA89AA75F953F738408088FC08596207AC51FDCB2` |
| Clean-checkout NUnit TRX | `31900143FE5BD58258057C580EFF369504720A07CC7172C751D820C3BBC67E87` |

The TRX is retained at `.worktrees/shared-ui-viewport-clean/SharedDesktopUi/.codex-work/evidence/viewport-tests.trx`. Build outputs, SDK files, and NuGet caches remain in ignored `.codex-work` directories inside the two E: worktrees.

## Independent review

Independent exact-tip review passed for candidate `df7e0cbf053fbed8ca55831f70664533d92609a5` (tree `f79277f01d87bd192803afe26510e865e52dbb8a`) against baseline `1ad2835524fe135c87eeca13f15cd16d4d9cb303`. The reviewer confirmed that the four endpoint cases resolve the original coverage finding, the refreshed validation hashes and TRX counters match, and the implementation conforms to the approved API, arithmetic, failure, allocation, dependency, and scope requirements.

## Consumer integration handoff

- **World Atlas — `WAIT_DEPENDENCY`:** wait for ATLAS-A promotion, verify the accepted viewport baseline, then integrate the same reviewed shared source in a separate adapter candidate with its own review and validation. Preserve Atlas projection, selection, visibility clamping, and navigation policy.
- **Dungeon Studio — `WAIT_DEPENDENCY`:** wait for a stable selected `MapCanvas` baseline and release of the source hotspot by its owner. Validate snapping and geometry against that baseline in a separate adapter candidate with its own review and validation.
- Both applications must consume this one reviewed source subtree. Neither adapter, gesture rewrite, or consumer behavior change is part of this candidate.

See the [technical handoff](SHARED_DESKTOP_UI_HANDOFF.md) for the full per-consumer gates.
