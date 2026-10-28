# CORE-F selected P8 Core owner graph validation

**Purpose:** validate the bounded binding of the selected Core spatial authority and the existing P8-A/P8-B census providers into the Daily-v1 runtime owner graph. This does not change the accepted P12 profile, owner cardinalities, save scope, or any P8/P12 domain semantics.

## Candidate identity

- Candidate code commit: `0a100cdc578d679149002f1b91a660d7306fc3ca`
- Candidate code tree: `656ab4c49eb5678d99ecfb43f1f76fc405b98a94`
- Base: `4e562c4a7d349e289185a51d653854ac54bf64d2`
- Remote: `origin/codex/core/CORE-F-initial-restore-orchestration`
- The only preserved working-tree changes outside this candidate were unrelated user ProjectSettings edits, untracked user files, and protected evidence. They were excluded from the candidate.

## Change covered

- The Core genesis graph binds the exact `SpatialAuthorityStore` used to compose P8 geography before binding its Person-position and legacy-anchor children.
- Owner-graph membership replacement retains that exact authority instance.
- The Core graph checks live identity, cardinality, and revision witnesses for P8-A Hexes, Locations, scale context; P8-B passage/barrier state and crossings; and the already-bound P8-C owners.
- The existing P8-A/P8-B provider source and `.meta` files moved into the Core package and are explicitly included by the .NET project. Both `.meta` GUIDs were preserved: P8-A `f7a01169efaa462790178e2b43c507c3`; P8-B `529bf2019e3f413895341a10eb7a5a6d`.
- Regression coverage accepts the live P8 owner vector and rejects substituted identities, cardinalities, or revisions for each P8-A/P8-B section.
- No census section, alias group, Required/ExplicitlyEmpty role, or supported profile composition changed.

## Validation

| Gate | Result | Run identity |
|---|---:|---|
| Official CoreValidation cold | 919/919 passed | `bf088973-a43b-475e-9281-ed6c52eb00f1` |
| Official CoreValidation warm | 919/919 passed | `082e4123-79ae-48e7-ba36-c726051c823d` |
| Official Unity EditMode | 3275/3275 passed | `feb73446dfa043c3a41ab75d72b5cbc4` |
| Official Unity Smoke | 5/5 passed | `5e31f8ab5ccc42d78de29d27e574efa0` |
| `git diff --check` | passed | candidate code tree above |

CoreValidation used pinned SDK `9.0.315`. Its private run manifest reports stable input snapshots with digest `70d5c4e67cfe610de3b7f0fb8aae4fdeee2b89bd33c35043fe209a25c1008542`. The official Unity manifests report stable before/after inputs; the full EditMode result used Unity `6000.3.9f1`. The source SHA-256 values below were independently recomputed from the candidate files and matched the captured CoreValidation inputs; the Unity manifest independently matches the four production package sources.

| Candidate source | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedRuntimeOwnerGraph.cs` | `53c98ee60878f53e5ac2308a37899cfff6388e1179cebf7c8119bf9bc29a83b1` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1GenesisComposer.cs` | `ea285fb9fcd594337175544fc46526c8e71dde69d99cc119f07845dcfdcd4ede` |
| `Packages/com.simulation.core/Runtime/Geography/P12P8ACensusProviders.cs` | `f660cee1876be1d0873828b9d28a405f6e0bead1bed10ac4b8d3cf33e6c68996` |
| `Packages/com.simulation.core/Runtime/Geography/P12P8BCensusProviders.cs` | `c07e9ac372b5303241cefa4814c4b30a72053eae737675c416541a9c42b8d6d0` |
| `src/Simulation.Core/Simulation.Core.csproj` | `5a6ead8f98feefe31622665891648205a3e829dcbf567340aa8f05623d14b651` |
| `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs` | `3947f0ebb526eac9c02f1ca289f36e4e7f7a456db96849c5107dc2a24755d93f` |
| `tests/Simulation.Core.Tests/DailyV1GenesisComposerTests.cs` | `cd0af522de9e2b9ab517fc03d697b03f82784d43f01060c955e40fbfc40a4d90` |

## Protected original evidence

The official private manifests, original NUnit/TRX reports, raw logs, and their owner-only evidence-directory ACLs remain in their protected local evidence storage. No report was edited or sanitized for this run. The public manifest and report identities are:

- CoreValidation manifest SHA-256: `a62528b50251d019cce511ee3c61f7ac8ab0b7e9bc7556ea69a61829225b195f`
- Core cold report SHA-256: `4416c88547d840cf3aac0b78e8f8a21aff548d0ee6603f7c04f76ddae1bc362b`
- Core warm report SHA-256: `0e8aca507757aa259453aa21461e4173e29ef2f8ce33f2d4a2ebe02de91ef79d`
- Unity EditMode manifest SHA-256: `323405ddaff4ce592bb73b76bc3b3abe40d8629f3b4db3b67c694db128b36df5`
- Unity EditMode report SHA-256: `76cd58334b8acd8fc4061c38aaddf882eaf7ee2e73199454e67382d09dad885c`
- Unity Smoke manifest SHA-256: `ac8839b1fc5cb93bbe4532071234296574a3bfc7d73e6f930582dc7b16e33c4c`
- Unity Smoke report SHA-256: `104378e095281fc00f7a96e96417c340adbfefb51ae3a77130998a96beb39b7a`

This record intentionally contains no machine-specific evidence paths or raw logs. It records validation of this bounded integration only; CORE-F remains incomplete and unpromoted to Core canonical.
