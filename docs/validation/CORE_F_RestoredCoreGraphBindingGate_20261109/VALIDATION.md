# CORE-F restored Core graph-binding gate

**Status:** validated candidate evidence; no Core-canonical promotion or CORE-F completion claim.

## Scope

The restored Core session now validates its exact Person/NPC/Genealogy and TravelParty graph bindings before the host captures or projects its target census. The existing Core target-invariant gate reuses this validation before its allocator, expedition, and sentinel checks. Unity continues to validate its compatibility projection separately. A focused regression proves that the Core gate accepts a valid prepared graph and rejects a dangling genealogy edge.

This is a bounded pre-census binding gate. It does not complete the 89-family census protocol, the full restore lifecycle, detached .NET hosting, or the remaining CORE-F runtime authority.

## Source identity

- Repository: `TorroisBr/Simulation`.
- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`.
- Git HEAD during validation: `79aa178d6fef006d46f49879c4b88ca4c170a0cf`.
- Committed tree during validation: `fcc3c4737a2d96c64298426476748d48df20318d`.
- Core canonical during validation: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- CoreValidation working-input digest: `03b0c6b60c1fbe6a5a1c66c4b0ef7cce33f8efc3ffb509336e2ea316d7d415a3`.
- Unity evidence reports a stable before/after validation-input inventory. Its captured repository identity is the same HEAD/tree above; the package records and verifies production input hashes.

The exact implementation/test files validated were unchanged afterward:

| File | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` | `a47b6c4ea2e5743aaba3cb3c519fc8da0e10232195aab357ad7fabf081545176` |
| `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.ImageRestore.cs` | `81f94dc3e14d1a3f2786e0a80c80f8c10dd2db31a15a27448faaef7be2ab62ba` |
| `tests/Simulation.Core.Tests/DailyV1ApplicationHostTests.cs` | `e923781e6b5448c71ca985966daf10e5734ab58d7a5294ac1bf4f4ec3dd0ca12` |

## Validation

| Gate | Result | Evidence identity |
|---|---:|---|
| Official CoreValidation cold | PASS, 1,073/1,073 | Run `0eb06e6dd4d54091b1793cc8ad9ba81f`; SDK `9.0.315`; native private manifest SHA-256 `c13adf443633907626c7ea9c6370e0c3c6b9f2042b19500528f534d363620fe6`; cold TRX SHA-256 `79598324f32e6d38ca865b52188e6aaa2f556b3887f4df822f1a8a6264dea8b0` |
| Official CoreValidation warm | PASS, 1,073/1,073 | Same run; warm TRX SHA-256 `56f5a8cf3ddcb8f80efe0e4976682517be7a52d114e78b9175df4112180209e3` |
| Full Unity EditMode | PASS, 3,302/3,302 | Unity `6000.3.9f1`; run `79965c417f0e426d9ebf53281841d7c0`; protected run-manifest SHA-256 `bb9da54df958247091c6a52a1df5ebf7795dc4045f1fde8dfd57d3656923255d`; original XML SHA-256 `3c2ff31467671498092336704eabb63da71cf573111c59c97a76604b365bc30f`; original log SHA-256 `2d4bb13fbac3db65ccf44c3441005ac5a3e756f38561d2bd2988d0247e564fb9` |
| Filtered official EditMode Smoke | PASS, 5/5 | Run `360b0d29f2bb451cbf38dbd553cd7ade`; protected run-manifest SHA-256 `1603160eb37118195edbbc76a8b13a34cd5e1f7b140e0962cce91e110268b073`; original XML SHA-256 `adc4fcf8c4f5d9a52a0f2ef72cb7444eb6c9fd5fa88cbbb98ab36eb02cfb5249`; original log SHA-256 `1a9ac0084a969446524d7790f79d7378333faa28388d1137e2fda3f145ee6a3c` |
| `git diff --check` | PASS | Candidate changes |

The full EditMode evidence package is [manifest](UnityEvidence/coref-restored-core-graph-binding-editmode.manifest.json) and [archive](UnityEvidence/coref-restored-core-graph-binding-editmode.zip). The package was verified against its protected source run manifest: one run, one archive entry; public manifest SHA-256 `3b79a5cda745650d8538b355544ee93c89c2d9455985b43d41b1735c04cc778b`; archive SHA-256 `d7aeeee09564e8eb43567ad1756b37cb5eb92141e0c9d276c3c6e60dc931092f` (174,648 bytes). It contains only normalized NUnit XML; no raw log is published.

The standalone filtered Smoke run passed, but its separate packaging attempt was rejected by the packager because the filtered XML's aggregate `Simulation` suite `testcasecount` did not match its descendants. No Smoke package was published. The full packaged EditMode report contains the same five Smoke test identities, all passing. The filtered run's original XML, log, and run manifest remain in protected private retention.

Original CoreValidation reports/logs and both Unity run originals remain in protected private retention. No public TRX package is claimed: the approved CoreValidation runner's native evidence format is not the separate `dotnet-trx-source-context-v1` adapter context, and no context was fabricated. Private file locations are intentionally omitted.

## Preservation and limits

The pre-existing ProjectSettings edit, pre-existing validation-record edit, untracked user files, and protected evidence were not staged or changed by this validation. The capture was private to the E: environment. This checkpoint is not Core canonical, does not complete the 89-family restore/census protocol, does not establish P12 readiness, and does not complete CORE-F. Independent exact-tip review remains required.
