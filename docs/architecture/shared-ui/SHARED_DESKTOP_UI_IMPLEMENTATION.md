# Shared viewport library implementation

## Candidate provenance

- Implementation branch: `codex/shared-ui/viewport-library`
- Architecture baseline: `codex/architecture/world-identity-projection` at `1ad2835524fe135c87eeca13f15cd16d4d9cb303`
- Implementation source commit: `a0f593e59829c6f570ad4c30952dfb6a978b18c7`
- `SharedDesktopUi` source tree: `2fa0a46729a0278f1fd86f3b61967f69a3856061`
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
| Complete NUnit suite | 18 of 18 passed. |
| Clean-checkout reproducibility | Passed in `.worktrees/shared-ui-viewport-clean` at the implementation source commit, using a separate NuGet cache and no local feed. Locked restore, both builds, and all 18 tests passed. |
| Allocation check | Passed; project, unproject, and reanchor loops allocated 0 managed bytes. |
| Dependency inspection | Shipping project has no package dependencies. Its compiled assembly references only `System.Collections` and `System.Runtime`; no Avalonia, Dungeon, Atlas, Unity, or Simulation Core dependency was found. |
| `git diff --check` | Passed. |

The mandatory regression uses X origin `0`, viewport center `500`, initial pan `200.0005`, equal old and new scale `0.002`, anchor `89d / 3`, and click X `700`. With reanchoring the half-centimetre midpoint calculation rounds to `-1`; skipping the pan recalculation rounds to `0`.

## Evidence hashes

| Evidence | SHA-256 / Git tree |
| --- | --- |
| `SharedDesktopUi` source tree | `2fa0a46729a0278f1fd86f3b61967f69a3856061` |
| `ViewportTransform.cs` | `705BCCC87A5079C5EF086DA7CCFEC9F20B82745F40D3E69A9AC8ED276D9B0841` |
| `ViewportTransformTests.cs` | `FFB685AE28579C52C3EA42C854C9B0563E990B83CFA22C11576983743C929C18` |
| Library `packages.lock.json` | `1E39B4149E1289978E01A366893A0697356E855BC7156B7C70747288030042F9` |
| Test `packages.lock.json` | `2949AE64C05DD30EE1897A4FA89AA75F953F738408088FC08596207AC51FDCB2` |
| Clean-checkout NUnit TRX | `0FE60D813A39BE3CCE43DCAF1F8AAD05174A16AAB263B1AF01CD2DD593CDD090` |

The TRX is retained at `.worktrees/shared-ui-viewport-clean/SharedDesktopUi/.codex-work/evidence/viewport-tests.trx`. Build outputs, SDK files, and NuGet caches remain in ignored `.codex-work` directories inside the two E: worktrees.

## Independent review

Exact-tip implementation review is pending. The candidate must not be treated as promoted or consumer-ready until that review passes.

## Consumer integration handoff

- **World Atlas — `WAIT_DEPENDENCY`:** wait for ATLAS-A promotion, verify the accepted viewport baseline, then integrate the same reviewed shared source in a separate adapter candidate with its own review and validation. Preserve Atlas projection, selection, visibility clamping, and navigation policy.
- **Dungeon Studio — `WAIT_DEPENDENCY`:** wait for a stable selected `MapCanvas` baseline and release of the source hotspot by its owner. Validate snapping and geometry against that baseline in a separate adapter candidate with its own review and validation.
- Both applications must consume this one reviewed source subtree. Neither adapter, gesture rewrite, or consumer behavior change is part of this candidate.

See the [technical handoff](SHARED_DESKTOP_UI_HANDOFF.md) for the full per-consumer gates.
