# CORE-F selected publication-state boundary (historical run)

This record and its package refer to the earlier publication-state-only source
input. The current combined publication/admission checkpoint is recorded in
[`CURRENT_VALIDATION.md`](CURRENT_VALIDATION.md).

## Scope

This checkpoint removes the selected runtime host-port readback for
`IsFactualReadWorldPublished`. `DailyV1ExecutionBoundaryState` was already the
single owner of that bit; `SimulationRuntime` exposed the same value only by
proxy. The selected Core advance and restore checks now read their exact Core
execution state directly. Unity's existing publication timing and setter call
remain in place. Census capture, the Unity quiescence protocol, mutation epoch,
and the other host ports are unchanged. This does not move any of the selected
89 P12 owner families or make `TryStart` implementation-ready.

The Core regression now tests an unpublished Core execution state, then marks
that same state published before continuing with the active-operation and
census-failure cases. The manually staged restore regressions mark the Core
state after creating their restored boundary token, matching the production
restore-finalization order. No assertion or result was weakened.

## Exact change and validation input

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Tested source base: `ff3852fd3a283a2ad2bcba8fe29d3660908e397f`
- Core committed tree at test time: `0cd628e54979c57a53d9fc126bcd2a12150d1362`
- CoreValidation working-input digest: `839d16b33692ca049b33e670ecf2b53edfac1d9b66e9c4967c2a4a6efe6f8330` (stable before/after)
- Unity working-input digest: `b724225f6921b6257de73b71172fe9fb51022f5504561d3691eb225eb0347ce6` (stable before/after)
- The relevant changed-source SHA-256 values are recorded below; both runners' input inventories bind their applicable files.

| File | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs` | `1640782cd22b4a3a5703cb3217dc48ef41d156541d4d92491fb4ff1f7f3b4d2d` |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `dd73e5af8d623311eb19401db868e64ea553b347c68f4bc9ead9969ef987cf0e` |
| `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs` | `1c484957b8bafc0d48aa80a14d163a4a8693691e763d925a3a2d79ebc7be8a0e` |

The three user-owned `ProjectSettings` modifications and unrelated untracked
files were preserved and were not staged. The official Unity input inventory
records the current ProjectSettings bytes used by that run. No claim is made
that those local inputs were part of the candidate commit.

## Validation

- Official CoreValidation run `7c1341bf91ee4a7cacc7a785a08dc6b3`: cold `997/997`, warm `997/997`; no failures, skips, or inconclusive results. Pinned SDK `9.0.315`.
- Official Unity EditMode run `cc95a0a3237740f5abd4b62f796c36a6`: `3296/3296`, no failures, skips, or inconclusive results; Unity Editor `6000.3.9f1`.
- Official filtered Smoke run `3e78fb2077e1440c852103ffb1c88f76`: `5/5` passed against the same Unity input digest.
- `git diff --check`: passed.

An initial CoreValidation run found two manual restore fixtures that had not
marked the Core restored state published after making a boundary token. The
fixtures were corrected to follow the production finalization order; the final
cold/warm run above passed. The first run's original report and diagnostics are
retained privately and are not presented as passing evidence.

## Retained evidence

The normalized Unity full-EditMode report package is
[`coref-selected-boundary-full-20261103.zip`](Evidence/coref-selected-boundary-full-20261103.zip)
with manifest
[`coref-selected-boundary-full-20261103.manifest.json`](Evidence/coref-selected-boundary-full-20261103.manifest.json).
The manifest SHA-256 is
`5eaf9330bcdff4a105a4e3e673dd2d2720c46be5f2332465a93b623828978632`; the
archive SHA-256 is
`efe24d0923d496f2186212774759eaac58f3c989b12d12767587a46f6a7a7b7b`.
The approved verifier passed against the original run manifest. The original
XML SHA-256 is
`75aad7a9db6850ffe82ee6e41b21a35f14d478407b039edf421238a8238bff8f`.

The separate filtered Smoke XML and the .NET cold/warm originals remain in
owner-only E: evidence storage, unmodified. Smoke original XML SHA-256:
`6ceded435a151f2638932d8209702d9333ecd9ed31632af6d29315254f025823`.
CoreValidation private manifest SHA-256:
`5beccd34a82f4bcbf6b3ebdda40a87c865d4f78fd720f7fd0182a64485c7d9b1`.
Cold TRX SHA-256:
`c3b3f55b32b6035bb738e86776098f58c019ed7f57edc58182df00c76d4dd149`;
warm TRX SHA-256:
`10010edfd16dbed6cd296e0eb76eb9a096932c913312070d2bcc472f921ee2a2`.

The approved `DotNetTrx` packager rejected the CoreValidation manifest because
that runner does not emit the required `dotnet-trx-source-context-v1` input.
No unsupported context was fabricated and no raw TRX was published. The private
reports and manifest remain available for exact-tip review. A durable public
.NET result package is still required before any canonical promotion.

An attempt to combine the filtered Smoke report with the full Unity report was
also rejected by the official packager because their shared suite aggregate
counts differ. No report was edited or manually combined. The full EditMode
package is retained as its own complete report; the separately verified Smoke
result and original remain private.

## Status

This is an intermediate CORE-F candidate checkpoint only. It has not been
independently reviewed yet, is not promoted, and does not complete or unblock
`DailyV1Application.TryStart`, full Core-owned P12 capture/restoration, or the
detached .NET host.
