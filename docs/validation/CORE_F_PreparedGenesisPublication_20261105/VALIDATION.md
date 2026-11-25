# CORE-F prepared-genesis publication validation

## Scope and exact source

This checkpoint connects Core's existing admitted NoSave start preparation to its single active-session publication gate. The new publication entry accepts only the exact prepared Core session, its unchanged genesis composition, and this application's exact NoSave result. Unity forwards the matching preparation and its projection; Core still binds and publishes only after checking the exact execution-state boundary. A mismatched boundary leaves the candidate private and retryable with the correct boundary. A successful publication consumes the prepared candidate, so duplicate publication is rejected.

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Implementation commit: `c1153332124135c10192f841aff465fd488ddc5e`
- Implementation tree: `0af7f157cdaddd286a361e19ddc0ba71f93d4a8e`
- Parent candidate: `56fbb2f3b0ae34d81d565d7560fa339347870c8b`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

Implementation files:

- `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs`
- `Assets/_Project/Scripts/TesteSimulacao.cs`
- `tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs`

SHA-256 of the exact committed source files:

- `DailyV1Application.cs`: `d4eb04d7b7227acffd758e1f06a41bdb71c600303c1df2c99fdeb072032bf669`
- `TesteSimulacao.cs`: `465636b20225144a374a0300830e158f22eb15e5cc49342ff12aee69ae460510`
- `DailyV1ApplicationSessionGateTests.cs`: `45f0b0a7595b2e097b472cbbaeb3e8efd93b0220d1e8546face70324c6f99a73`

The official runner snapshots contain these same source hashes. CoreValidation binds the application and Core test hashes; Unity validation binds the Unity adapter and shared application hashes. No source edits occurred between validation and the implementation commit. `git diff --check` passed.

## Validation results

| Gate | Official run | Result | Input identity |
|---|---|---:|---|
| CoreValidation cold | `2fc33d29e3cb4125a8cd9d7a8621a7a8` | 1,015/1,015 PASS | `bdd48f0588b58395464a63ed361a97337286df0ded35077922996b572a1125d8` |
| CoreValidation warm | `2fc33d29e3cb4125a8cd9d7a8621a7a8` | 1,015/1,015 PASS | `bdd48f0588b58395464a63ed361a97337286df0ded35077922996b572a1125d8` |
| Unity EditMode, all | `fce45917466e4c86955e2f26dcb057ed` | 3,299/3,299 PASS | `8320043336c211b69420a39d1f9f9fd94301f8063ddf082ed97ae5e6da2713f2` |
| Official Unity Smoke filter | `6d1cd4d38048402c971d0ac7406fe010` | 5/5 PASS | Same committed source bytes; independent exact run manifest retained privately |

CoreValidation also passed its locked build, CalendarDemo cold/warm, and Simulation.Lab build/cold/warm gates. Its private manifest SHA-256 is `a4911d55e884febbd9a4f53bab4e03448efc3a385797e7786678f4b174a78846`; cold and warm TRX SHA-256 values are `1171340177c656dd0d2cc5bb51c19fecb8b884be61ca2a91e9fb4cac77cb3840` and `0e10e24e204bf2f13ccaf44afc17c9772d24f47389ef1ebd087953960c14d753`. These original reports remain in the protected E: validation store. The checkout contained Unity's `Library`; this run does not claim a Library-independent .NET host.

The full EditMode run's private manifest SHA-256 is `465b5bce74f3ffc01c1eff8ed557c2ca9ab1c9e588a94ee30fdf5b42d30d9dca`; original XML and log hashes are `e588e72e82077da37e9e2007d18bf4bc2a62a35b0bcc7e787fb4e11add6eb66f` and `38412f10a60e18606ae32b890e5608c8fc445fe972d605d263459d76631c34dd`.

The full EditMode report is available in the sanitized, verified package [manifest](Unity/coref-prepared-genesis-publication-editmode.manifest.json) and [archive](Unity/coref-prepared-genesis-publication-editmode.zip). Public manifest SHA-256: `904f0d2d8f587a02e80ce14fe1e70ac648ab82b45f88b610b591bff98c25ea4c`; archive SHA-256: `1c7af50cace916fcda549a25d71f45c658fa332120be3b09fdbf829bc3646bb2`. The official verifier returned `VALID` for one run and one result entry; a path/credential scan found no workstation path or credential pattern in the public manifest. The package is bound to the private original run manifest.

The separate Smoke report's private manifest SHA-256 is `394383e8234e5db66ced9a91adf8859bd9fd6543c9d0b0db6e5e1c9578285026`; original XML and log hashes are `d6d41a5e96940cd17972f0cdcdc177068726c6e2c49c3805899a65f93ddfab5c` and `bda5c39d6b6ec4bece922fa9c586be5d799a0645131f30a6b2b0bd81e7f14dfa`. The passing Smoke cases are also present in the full EditMode package. Protected original reports and logs remain unchanged on E:.

The runner-created CoreValidation and Unity evidence directories retain their private, owner-only ACLs. Only the normalized full EditMode package is published in this candidate; private manifests, original XML, and raw logs are retained locally.

## Independent review

Independent exact-tip implementation and evidence review: **PASS — VALIDATED CANDIDATE**. See the durable [review record](../../reviews/CORE_F_PreparedGenesisPublication_c1153332_REVIEW.md). The review applies to implementation commit `c1153332124135c10192f841aff465fd488ddc5e` and tree `0af7f157cdaddd286a361e19ddc0ba71f93d4a8e`; subsequent changes are documentation only. This validation/review record is not Core canonical promotion.

## Limits

This is an incremental CORE-F candidate only. It does not complete the selected Unity-independent runtime, public `DailyV1Application.TryStart`, shared detached restore, descriptor-v2 persistence, the fixed .NET host, or T/T+1/T+2 continuation. It does not change Core canonical or claim full CORE-F acceptance.
