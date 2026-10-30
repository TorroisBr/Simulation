# Core-Owned P12-C Runtime Identity Capture

## Candidate

- Implementation candidate: `4e8307e6f4ad742969fad0edee0a885a30f6fc52`
- Candidate tree: `fcf5f07a5a0b54b760e305b45a43cc28857ee623`
- Base: `ef6cae682638416c4bf7da7e3a52eac9ea075923`
- Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

The active Core continuation-save operation now captures the four admitted P12-C runtime identity families from `Session.OwnerGraph.RuntimeIdentityIndex`: NPC, City, Location, and Route. It requires one exact Required census entry per family, the same index instance, and matching schema, cardinality, and shared revision. The snapshot is cached on that save operation and encoded by the existing P12-C codec. `DailyV1Application.TryComposeContinuationImage` now includes these sections itself and rejects host-supplied P12-C identity sections. The active Unity restore coordinator no longer samples those identities through `RuntimeIdentityRegistry`; the legacy no-Core-operation path remains available.

Unsupported identity families remain subject to the existing codec rejection rules. This slice does not remove the remaining Unity P12-E adapters, complete the shared application/host, or claim P12 detached-host acceptance.

## Source identity

Both official runner inventories were stable. CoreValidation working-input digest: `12d38d26ba6489028990bc77717714cceeddc7036fb4d0a78728101a168d276d`. Unity input digest: `99751a19906bbecebda4fb08cd0d960d74f8875766a59edbf3979209033aebc9`. The Unity runs used base `ef6cae682638416c4bf7da7e3a52eac9ea075923` / tree `7a8760441dc0efd509f59230fd39dcb52a66a148`; all five file hashes below match the stable runner input and committed implementation:

| File | SHA-256 |
|---|---|
| `Assets/_Project/Scripts/P12ADailyV1OwnerSectionEncoder.cs` | `12fab0f94aa4f57cac4e87824a73b3404424e1d53a8bc4009ee6f63ec3d93eb3` |
| `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.cs` | `d607d08fc8f155a637a5a68dfda09be4e9eaeb0a86e5413bc4a44df93170d738` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` | `9c4bc7d6ae74a95b1bd8550cad02e5aa83295717641f1a9ff5e620c0683f8c58` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs` | `eb247298db7808fc0815a6d7c8d9204de363330c2a5e7db9753d11a5fbe64943` |
| `Packages/com.simulation.core/Runtime/Persistence/DailyV1P12GContinuationSourceWitness.cs` | `48b13b2056a250998ba95154a8c40e029cac9a1874ee334dde1311a2bde100f3` |

## Validation

- Official CoreValidation, SDK `9.0.315`: **936/936 cold and 936/936 warm**; all test failures, aborts, skips, inconclusive, and not-executed counts were zero, and the default Core/Lab gates passed. Run ID: `fc7a0e7fbd8b4ab2959ffcc0b4e1e5b9`; private manifest SHA-256: `a1206678db462cbecc4b78bdb13503c368b933ef1ebbf69730db6bd3d7c06ed2`; cold/warm TRX SHA-256: `5df08e0ce2e7f8b46dd1ba60465c81a4290092698ec4ac32ca2bd6cb1553f7ad` / `a5750c138816f18cc367f17c225b005d30a7e1d80e6eae4fd921d10fed62752e`.
- Official Unity EditMode `SimulationRuntimeAdmissionTests`: **228/228 PASS**. Run ID: `af0bed3d486540ea9c7cc66ae3ec9db4`; private run-manifest SHA-256: `5e2e9691fa1340e2b82ddbaaabcd314bf4efde769ba8cba035eea816f2383f4d`; original XML/log SHA-256: `f5c583cf1cdf26f89646af4ae8a8ce83df0ce42324da59f1374cb65d994c8f00` / `9f0e030bc1951590782c840f35c3b2635615727ebe85aa26595941acbe1a508b`.
- Official Unity EditMode `P12EDailyV1OwnerPackageTests`: **6/6 PASS**. Run ID: `bb1ed8fcb4864e4f9a3d9e940abfce13`; private run-manifest SHA-256: `fa245fd96f0fa4186bf26a50086f43eb325604a88265845f1291fe780d3cdbd3`; original XML/log SHA-256: `4b863f30a070dda0e0546d762374ca93067b57c6afb06c1d792712920248aae2` / `fb197b2863e92feebeba655774732de633790045ea125392d39e738f0c5d0aab`.
- `git diff --check`: PASS.

Original TRX, XML, logs, and private manifests remain in owner-only local evidence storage and are not committed. This focused candidate does not claim full Unity EditMode, Smoke, LongRun, independent final-package review, or complete CORE-F validation. The three unrelated `ProjectSettings` modifications were preserved and excluded.
