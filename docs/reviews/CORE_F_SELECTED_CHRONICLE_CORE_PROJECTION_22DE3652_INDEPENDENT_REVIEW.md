# CORE-F selected chronicle Core projection — independent review

**Verdict: `VALIDATED_CANDIDATE`**
**Review date:** 2026-11-05
**Candidate branch:** `codex/core/CORE-F-restore-stage-transaction`
**Final candidate tip:** `40a687780f2cb1648f44524ccdc1e9390bc231c1`
**Implementation tip:** `22de365227a71e8327a2c1ff576ef4e7d4c3d993`
**Implementation tree:** `687d9a91c294dbf4a4fa3f3d47a0877bc3e673ce`
**Code-review base:** `efd1ae2b41acd54661eebf37a077e973e66a0bd3`
**Core canonical at review:** `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

## Reviewed scope and result

The implementation adds an internal `DailyV1SelectedNpcChronicleFormatter` over the existing `DailyV1SelectedRuntimeOwnerGraph`. NPC names resolve through the graph's exact runtime-owner index; City names resolve through the selected City owner for the exact `LocationRuntimeId`; Item and Action labels come from the same `DailyV1EffectiveDefinitions` maps already used by the selected graph. The formatter owns no simulation state and introduces no new identity or authority.

The P12-G restore coordinator now constructs the formatter from `runtime.SelectedRuntimeOwnerGraph`, so detached selected-profile formatting reads the owners admitted with that restored runtime rather than reaching through `TesteSimulacao`. The selected, site-free Core genesis path uses the same formatter. Existing legacy and P10 site-bearing paths remain on their host-specific formatter, consistent with the bounded profile contract.

The additive restore test compares output for an Action decision and a TradePurchase decision against the existing Unity projection, covering NPC/Action names and Item/City names on the selected graph. Existing formatter rendering and its missing-ID fallback are unchanged: both resolvers return `null` for an unavailable lookup, and the existing formatter emits the stable ID or `<unknown>` as before. The test diff adds assertions; it does not weaken or remove existing assertions.

No actionable implementation or test-integrity finding remains for this bounded change.

## Exact-tip and evidence checks

The candidate's final docs/evidence commit is a direct child of the implementation commit. Its only changes are the validation record and the Unity package manifest/archive. The implementation source tree at the final tip is exactly `687d9a91c294dbf4a4fa3f3d47a0877bc3e673ce`, unchanged from the tested implementation tip.

- Official CoreValidation run `8de984b15df546b79931c19888c0d818`: cold 1017/1017 and warm 1017/1017, zero failures; source inputs stable; no Unity Library in the .NET worktree.
- Focused Unity EditMode run `d50bb1384d2a41d8a13a7e001d0c78ed`: 16/16 passed, input stable.
- Full Unity EditMode run `74b5e756a39c438d8230465aab0c9069`: 3299/3299 passed, input stable.
- Official Smoke run `51a882c9fb294addba607bccce767fad`: 5/5 passed, input stable. The full EditMode report also contains the Smoke cases.
- Official Unity package verification passed against the exact full-suite private run manifest: one source run, one normalized result entry, archive SHA-256 `e87eae5c70d1149eb7911bcc055afc9357dbe6c8fdb4d2e6bb2e3b3eefbd7716`.
- Public package privacy and deterministic archive checks passed. Original TRX/XML and raw logs remain protected and unmodified.
- `git diff --check` passed for the candidate diff.

The complete public evidence record and package are at [`CORE_F_SelectedChronicleCoreProjection_22de3652_20261105/VALIDATION.md`](../validation/CORE_F_SelectedChronicleCoreProjection_22de3652_20261105/VALIDATION.md).

## Limitations and integration disposition

This is a validated partial integration contribution only. It does not establish a Core-owned full daily runtime, complete `DailyV1Application.TryStart`, a detached executable .NET host, persistence-v2 end-to-end hosting, or separate-process T/T+1/T+2 continuation. It does not change capture, hydration, owner census, profile scope, or canonical state. CORE-F remains open and must continue through its accepted runtime/host integration contract.

The candidate is suitable to retain as a reviewed implementation checkpoint and continue integrating. This verdict is not canonical promotion or CORE-F closure.
