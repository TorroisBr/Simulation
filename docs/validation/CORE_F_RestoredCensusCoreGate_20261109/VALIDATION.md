# CORE-F restored census eligibility gate

**Status:** validated candidate only; this does not promote Core canonical or complete CORE-F.

## Scope

`DailyV1ExecutionBoundaryState` now checks the exact selected Daily-v1 restored-continuation target, owner graph/thread, unpublished capture boundary, quiescence and mutation-epoch state before calling the host's unadmitted restored-owner census provider. Core repeats the eligibility and epoch checks after the host call. The focused test uses a fully admitted `GenesisPrepared` bootstrap session and proves the host census callback is not invoked because the target is not a restored continuation.

This transfers only the private restored-target eligibility gate. It does not move the 89-family census protocol, host capture implementation, full restore lifecycle, detached .NET host or remaining CORE-F runtime authority.

## Candidate identity

- Repository: `TorroisBr/Simulation`.
- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`.
- Candidate base: `0926a0423db7cbe47ff3e2d3226827e667ff468c`.
- Core canonical at validation: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Production change: `91e5e3f07e4c77df26e4355c428ae4c1b868d226`.
- Test correction: `5dcba571123ceea3ce42eee48e6eaa845947861c`.
- Exact code/test source tree used for the final runs: `680daae97ba07f4c88f7acb45a39cf01286c397f`.
- The final validation source commit and tree match the test correction commit and tree above. Subsequent changes in this directory are evidence documentation only.

## Validation

| Gate | Result | Evidence identity |
|---|---:|---|
| CoreValidation cold | PASS, 1,072/1,072 | Run `717d36f8a07a4ec29254d0ccb61ec988`; SDK `9.0.315`; input digest `fb641cbc6d30a48068d8c3cad54a94bd82bb22a090cbd145b1ebdf1fcf886059`; protected native manifest SHA-256 `2749cd4e51966b3c5bf88c61528789669da4663c2f3c5a4dd907d14f43cfafbd`; cold TRX SHA-256 `96ab569aa4d168a58d06dbe5659686a91e63c050156ae7273c79f5560f5d1466` |
| CoreValidation warm | PASS, 1,072/1,072 | Same run; warm TRX SHA-256 `f633525a3625000005d042324e3a73d196f285991feeb19a651416866d8d6b7d` |
| Full Unity EditMode | PASS, 3,302/3,302 | Unity `6000.3.9f1`; run `9b31893dd14843a8be1f80e25f24e5d0`; protected run-manifest SHA-256 `4b14e27d48fb9ac39efb7e15c4a044f9fc6bf9eda6b729d2bc2c3f4ceed32f1d`; stable input SHA-256 `c9ab78dd6990786e606b18f08a3f35367b0a12c7b6269946442d36421645b445`; original XML SHA-256 `82aa3031cfc044112341c9fd0033291a73cbaafaa2358e5a3fdef1fa194e9f3a`; original log SHA-256 `3a0c7158d6ec643e19141dccb15410122f74e2190e93b6c4a50c7a9e6c0f2cc0` |
| Official filtered Smoke | PASS, 5/5 | EditMode filter `Smoke`; run `21bfefd7819a4ca19af51c73b43c8f9a`; protected run-manifest SHA-256 `1c86e14800626fa80c67ab46595a3a9a177b8e42ac35c8b8f1bc41dd60d88ca2`; same stable Unity input SHA; original XML SHA-256 `2baa22b3013d6036174b248f83557fb7e02f03a9d53a4fe7d65cdded7a20f916`; original log SHA-256 `092adc302570406c6d52c573c7cd1106ffd6dd1bcd0309350a1b7d078d30c773` |
| `git diff --check` | PASS | Candidate changes against the base above |

The full EditMode XML contains the same five Smoke test identities as the filtered Smoke XML. A separate PlayMode attempt using the generic `GMConsolePlayModeSmokeTests` example selected zero tests and is not counted as validation evidence; its original result remains in private retention.

The normalized full EditMode package is verified against its protected source run manifest: [manifest](UnityEvidence/coref-restored-census-editmode-20261109.manifest.json), [archive](UnityEvidence/coref-restored-census-editmode-20261109.zip). Public manifest SHA-256: `0fdb24d5bd75f58ffd5b8d06021301af974d063f6479b98de322a43f8560c360`. Archive SHA-256: `a227bf813b3634f4a7e028ddb083d4449280c3952e79f26405d00979ed94aefb` (175,500 bytes). The report contains all five Smoke identities. No raw log is in the package.

Original Core TRX files, logs and native manifest remain unchanged under owner-only local retention. No public DotNetTrx package is claimed: the approved CoreValidation runner emits `simulation-core-validation/v1`, not the separate `dotnet-trx-source-context-v1` adapter context required by the public TRX packager. No context was fabricated. The Core run reports that this worktree has a Unity Library, so it makes no Library-independent .NET claim.

## Preservation and limits

The existing ProjectSettings edit, the user's pre-existing validation-record edit, untracked files and pre-existing private evidence were not staged or altered. New runner evidence was written only under the existing private evidence roots. This checkpoint is not Core canonical, does not make P12 readiness claims, and does not complete CORE-F. Independent exact-tip review is pending.