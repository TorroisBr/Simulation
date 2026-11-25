# Core validation for excluded per-NPC P12-D receipt rows

Status: validated candidate checkpoint. This does not promote Core canonical or complete CORE-F.

## Implemented boundary

Core now validates the two excluded P18 receipt-owner census rows for every admitted NPC before live capture publication, while validating a restored image owner vector, and during the final restored-boundary recapture. The rows must use their canonical schema-v1 IDs, `Required` role, exact cardinality/revision `0/0`, and distinct non-null host owner identities. Every selected NPC must have both rows; any additional compatibility-roster NPC must also have a complete pair. The complete host vector is preserved; Core does not create receipt owners, enable P18 writers, or export/replay receipt payloads.

Focused tests cover a selected roster, a mixed roster with an extra paired compatibility NPC, missing/unpaired rows, populated cardinality, nonzero revision, wrong role/schema, and aliased owner identity. The tests exercise the validation helper directly. The independent reviewer inspected each production call site and noted that the new malformed-row tests do not separately inject failures through all three call sites; existing capture/restore transaction tests cover failure handling generally. This limitation is retained rather than represented as end-to-end fault-injection coverage.

## Source identity

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`.
- Validated code commit: `457912c4805fedbfce48718def067fbdb6a280b2`; code tree: `864ad34cbcf5142da58fde6fc38ba3da347d261f`.
- Validation ran from parent commit/tree `32b2fe0d175af04d03878e5ca58ecf8e05beeacf` / `11d751a2dc37131c45763b841d0700eb88471e67` with only the three implementation/test files listed below modified. The exact file hashes match the runner input manifests and the committed candidate.

| File | Bytes | SHA-256 |
|---|---:|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` | 113866 | `6fc3c5ebca6127ba100758ac651d9fb2438b89396c7290feff123a403212252e` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs` | 136598 | `96ad645fba73b8cd87ebebe6e6f56bc867f19c6e020c4a333b6a0e9f6e47babd` |
| `tests/Simulation.Core.Tests/DailyV1CityEconomicOwnerCoreTests.cs` | 152584 | `e4b6f6cfc3eb5c424a8dd41fbe8e65406a784a0b416116fb7ee7d0896a2aa5cd` |

## Results

| Gate | Result | Provenance |
|---|---:|---|
| Official CoreValidation cold | 984/984 PASS | Run `2995b886fa824941a6c95b155630c8db`; stable input digest `644b597a7f6c2444177e0d23c79877053f017291632e77924934a3d409062d5b`; build exit 0. |
| Official CoreValidation warm | 984/984 PASS | Same run and stable source/input set. Both new receipt-row tests are included. |
| CoreValidation private manifest | retained | SHA-256 `43c48a77d3b8f104807c072996aed4b99dea20c4ca026c29f99b215942639a9f`. Original cold/warm TRX SHA-256: `eb06997331ee76f74918c25b7c26b0b0ff53863b6badaa108ccd780f3275dc6a` / `01e8ca9ec560dfc5de7b3d83cec0325a47cbb4ababb93260ff6698cb863cb67f`. |
| Unity ALL EditMode | 3284/3284 PASS | Official Unity `6000.3.9f1`; run `da1e43d70a6f48fbb6536a3d65288f75`; input digest `27c708047f73fd9c31d513992bd30db00b04d69b6f3912c247fca1a53da0e165`; inventory stable; private run-manifest SHA-256 `8e01ed27e7a59b3e544a1e045ca2ee07043cfed64944f1f9efe8f16eec4da97e`; original XML/log SHA-256 `7f4217fd5fde363e7e537524f5162007fc8b981c91b9f5f0e22fe7953650b0d1` / `018cc96a83f82d2867e5ad97aa5eb1075c0318a4c2c7b4063d20e23d25aa4452`. |
| Official Unity Smoke filter | 5/5 PASS | Run `b8b385c7ee0e4ec8af84a697bc9c6f73`; same stable Unity input digest; private run-manifest SHA-256 `74c21465bd75440ce1e3db72c8209092940ce5beb261ca1d66178702d1c46d88`; original XML/log SHA-256 `1edfb3db27bf51a53fdaaa5e5d73b36b5beff9521866adc0c50d260062827f22` / `09d20bf85c839f6f7d4e1710f7522385db3378a6fbce13022b0dc6519fea5b0b`. All five cases are also in the full EditMode result. Filtered original remains private. |
| Unity result package | VERIFIED | `coref-p12d-npc-receipt-zero-editmode-3284.manifest.json` SHA-256 `a5c850497b359d5b7c5cc5a15a37c9f9f4bbc5f89cdf24d39b1f6eb5fdc71fde`; archive SHA-256 `9874cb9c2999c3ed288d3eb8d605a8f5f66b753973703ad57f7e6c114894b833`. Official verifier passed against the protected original run manifest. |
| `git diff --check` | PASS | Validated implementation/test diff. |

The CoreValidation runner emitted its native `simulation-core-validation/v1` manifest but not the separate `dotnet-trx-source-context-v1` required by the public TRX adapter. Accordingly no public CoreValidation TRX package or original-to-public comparison is claimed. The private manifest and original cold/warm reports remain protected and unchanged. The Unity full-run originals and filtered Smoke run also remain in protected local evidence. The public Unity archive contains normalized result XML only; no raw logs or private paths are published.

This bounded checkpoint does not claim P12-D completion, P12-A readiness, a complete 89-family P12 census, detached restoration, or CORE-F completion.
