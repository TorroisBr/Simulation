# Core-owned decision occurrence receipts — candidate validation

Date: 2026-10-29
Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
Candidate base: `8a5730ee1ead8de138a7cdfaee455377f7a00669`
Candidate code commit: `93ec441801712a5e522b98ec84d04098ef0e5ee3`
Candidate code tree: `5392023906dc308c3ab6869263b37c8597893371`
Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

This candidate moves the selected Daily-v1 decision-occurrence receipt authority into `Simulation.Core`. `DailyV1DomainEventAuthority` retains the same allocator, event-time and record-sequence roots; Unity’s selected recorder forwards to that Core authority. Genesis and restore composition pass the same authority through the selected runtime. P12 section identity/schema, replay fingerprint semantics, local revisions, exact owner identity, mutation admission and mixed-roster fallback are preserved. The exact-zero admission tests now inspect the Core-owned receipt authority rather than fields removed from the Unity recorder.

This is an implementation candidate only. It does not complete or promote CORE-F and does not claim completion of `DailyV1Application.TryStart`, detached hosting, or full P12 continuation.

## Validation

All runs below identify the committed code tip and tree above. The Core and Unity input snapshots reported stable inputs; generated Core dependencies were stable.

| Gate | Result | Run identity |
|---|---:|---|
| Official CoreValidation, cold | 916/916 passed | `44bf58608d2348f69d40796ea56748a7` |
| Official CoreValidation, warm | 916/916 passed | `44bf58608d2348f69d40796ea56748a7` |
| Focused Unity `SimulationRuntimeAdmissionTests` | 228/228 passed | `1fd347dcbf4a49e0b92f52eb5ae87247` |
| Full Unity EditMode | 3276/3276 passed; 0 failed, skipped, or inconclusive | `44b20c22bd5d46d8a1dfef95e4c9c9c7` |
| Official Unity Smoke filter | 5/5 passed | `e807a1fd59884aa4a9fe70834d722aab` |
| `git diff --check` | passed | candidate diff against the stated base |

CoreValidation used .NET SDK `9.0.315`, input digest `4416b90005f96269a12b3b0444b5c81557e9beabafc497d5f16c95bad6a992b0`, and reported stable inputs and generated dependencies. Its private manifest SHA-256 is `aca5dcc3152ea1dbf35df74fa26afe4b6387592149705708dbf68e9bd43d6168`. The original cold and warm TRX files remain in the protected evidence store (SHA-256 `2418ca05169b33d7ca79ec504452dab1664cfb76e4d972db549ddd6557e0938a` and `e15956d860a3830aeed5d97794ac3b74cc31257541613173389677cae1455ef6`). The official runner produced its native CoreValidation manifest; no public .NET TRX projection/package is claimed because the required adapter source context was not produced.

The full Unity run used Unity `6000.3.9f1`, exact candidate head/tree, stable input digest `0a74f1a565abb0e96bf8b977e8c9d548245e72fa04dfc33090f640566a05002a`, and private run-manifest SHA-256 `29a8501ee4798152c8128f3391ae2245b09f88d0704529927d76a86ecb09cb02`. The separate Smoke run used stable input digest `d837dc1313dc631295a4d0ce0c3a9dccc087592de2cef8f4823f4ec97f8b71b8` and private manifest SHA-256 `df7cfa56061a40f11f70dc70acc258b998944c98f64c3ee89ec54da9be4a95b6`. The focused admission run used the full-run input digest and private manifest SHA-256 `d93cfcc318d734a4b94f9230ba9241854287a795eb1be9c6374c1f0c5026acb6`.

The full EditMode report is packaged below using the approved Unity evidence packager. It contains the Smoke identities as well; the separate filtered Smoke result remains preserved privately rather than being combined with the full-suite report. The isolated Unity validation checkout had local Unity-generated `EditorBuildSettings` and `ShaderGraphSettings` changes during the run; the runner captured stable inputs before and after execution. Those settings were not staged or included in the candidate. The committed production and test sources correspond to the exact candidate tree above. The candidate checkout’s unrelated `ProjectSettings` edits and protected/untracked evidence remain untouched.

## Privacy-safe EditMode package

- Manifest: [`coref-decision-receipts-editmode-93ec441.manifest.json`](coref-decision-receipts-editmode-93ec441.manifest.json), 468,992 bytes, SHA-256 `213699bb007ecdd9294a83d1715f6eee4f581f0d682a483e2fbf3aec6ab1eb82`.
- Archive: [`coref-decision-receipts-editmode-93ec441.zip`](coref-decision-receipts-editmode-93ec441.zip), 173,921 bytes, SHA-256 `e0c0d782b361b865f115c10d63e9554dac294128a5a833406b5f1340311f1019`.
- The archive contains the one normalized full EditMode XML report. Output elements were removed and absolute local assembly-suite paths were projected according to the packager policy. The official verifier returned `VALID` for one run and one entry against the preserved private run manifest. The package contains no machine-local filesystem paths or credentials.

Original Unity XML/logs and all CoreValidation TRX/logs remain private and unchanged. This record does not reinterpret them or claim that a hashes-only record replaces the original artifacts.
