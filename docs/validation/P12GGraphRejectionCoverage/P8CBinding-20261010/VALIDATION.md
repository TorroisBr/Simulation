# P12-G P8-C target-owner identity validation — 2026-10-10

This bounded slice closes the target-side identity check for the selected Daily-v1 `p8c.city-site-location-bindings` owner. The restore coordinator now compares the target owner vector against a fresh witness from the exact `LegacySpatialAnchorBindingStore` installed on the staged target runtime, requiring cardinality 0 and local revision 0.

The focused rejection case substitutes a distinct, valid empty binding store into the still-private target runtime while the registered census provider remains bound to the original staged store. Both owners report 0/0, so generic owner-vector shape/cardinality comparison alone cannot distinguish them. Restore must reject the mismatched target owner before publication; the shared assertions verify the active source session/token/graph remain valid, continuation parity holds, and a subsequent valid restore succeeds.

This proves only the selected Daily-v1 P8-C binding target-owner check. It does not complete P12-G, certify all P8-C/D targets, complete whole-graph failure injection/no-replay/parity, or change P12-A/P13 readiness.

## Exact source baseline and tree

- Canonical base: `c57fd22bb058e43672c01d8f5755d5d29eaa40b0`
- Candidate evidence commit Git tree: `83e7c6be65026625baad9d5780aaf1ec19050657`
- Validated `Assets` tree: `8d3f75eb85588b8a1bf4e5b90054d1214831955b`
- Changed production file: `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.cs`
- Changed test file: `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs`

## Results

| Gate | Result | XML SHA-256 | Raw log SHA-256 | Compressed log SHA-256 |
|---|---:|---|---|---|
| `SimulationRuntimeAdmissionTests` | 118/118 PASS | `68F28D6E5BDCED6006BB271F3A9A1C1681B919539690F431D205CBDC75FC8FEC` | `28773660EE5A471829F95411884CC9E90879BB8881C24672A3BFF992A25829C4` | `7EE7551C191990E6118CF01183A5E341C9E57CD76357EA255F889B06A6832F05` |
| ALL EditMode | 2792/2792 PASS | `55FEE07D6C13141DA78D2B106BE83AC1EB21F7F670DE542C07F77B09A512A0DD` | `D52628D32FF109ABF7C16F74FE533E11700C07E67AE9C32CE41155EAD1EF5710` | `08A6E86DF7DC5CB6A489B01270E0F54875650AA9D225923BE578478DC778F012` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 PASS | `6B74A4E0D80CB48520B10A80E959333BE5AC62FA08F6636A5B737A3717B6FB6A` | `50624434434DA1132AE8E7F7F08960FC58A9E5EEBFD652962E6FFE05325FC3D4` | `B51E9111C25B435A298E6C84D3899E717949DF864DFF2A864F8CCA07C4430E58` |
| `git diff --check` | PASS | — | — | — |

The matching XML and compressed logs are retained in `Raw/`. Unity reported zero failures, skips, and inconclusive tests for all three passing gates.
