# P12-D City root owner snapshot validation

## Candidate and scope

- P12 canonical base: aa1a40f2e6da53a1a7388601bd13052f1a535545.
- Architecture baseline: 47eff220c7ce00f6e7c759bdc2b76780bb46f628.
- Exact reviewed City design: f30e6ba520eff7841cf35849818910d17e5e4202; independent design review is recorded at e30adc771a51b62000032bf501366c8d7a435dae.
- Validated code Assets tree: e83151063141cb1395e1d53d30e850c6f81ed778.
- Unity Editor: 6000.3.9f1.

This bounded P12-D owner slice captures and privately stages exact City, Market, MarketCounterparty, PopulationEconomy, and SettlementPopulation values. It preserves ordered City membership, exact market rows and stored prices, aggregate and receipt revisions, and retained population operation receipts. It requires the Daily-v1 completed-boundary token, shared stamp, and exact four-section owner vector supplied by the outer orchestration.

This slice does not add runtime/bootstrap composition, NPC serialization, a shared D/E/F graph builder, P12-B invalidation wiring, P12-G publication, whole-graph validation, P12-A readiness, P13 readiness, or Phase closure. It rejects account-backed City economy, P18 City receipt state, and P14 finite-source/material-flow state.

## Validation results

| Gate | Result | Evidence |
|---|---:|---|
| P12DCityRootOwnerSnapshotTests | 7/7 PASS | Raw/Focused/EditMode-20261008-151223-deba1bcb38d2496f83e4a86f65bbc6c0.xml |
| ALL EditMode | 2563/2563 PASS | Raw/AllEditMode/EditMode-20261008-151242-a1754d1d80cc46ef8e9dfd9495855225.xml |
| Official Smoke | 5/5 PASS | Raw/OfficialSmoke/EditMode-20261008-151321-c62fee404c214d1e99489d71ab854a10.xml |
| git diff --check | PASS | Staged implementation tree |

All three result XML files report result Passed, zero failures, and zero skipped tests. Unity logs are retained losslessly as gzip; each decompressed SHA-256 matches the original raw log.

## Source hashes

| Source | SHA-256 |
|---|---|
| Assets/_Project/Scripts/CityRuntime.cs | A6FEB7FA4750804B6D4AD8FDABBFDDC4E20D7A9BE3197999C0775F7E688A3852 |
| Assets/_Project/Scripts/MarketRuntime.cs | 6925C65964DA6B7CF36662C8BB4034AF68D1AAE808C06EF2317D3CF3BDB06370 |
| Assets/_Project/Scripts/Population/SettlementPopulationRuntime.cs | 3FAD3C8589CF91DBFD67B10A9912BD1988821878CDB8832FD03200E0ECEE225E |
| Assets/_Project/Scripts/P12DCityRootOwnerSnapshot.cs | 1F043DE30926B8FBEDBE8395E22C3E0C2F1D18B56F2A7075DD16509EC2DA4D4F |
| Assets/_Project/Tests/EditMode/Editor/P12DCityRootOwnerSnapshotTests.cs | 2E3C83CC7A2A2BEAA5D7B895DB93B9AE56BF0B8A7B562BE7F41D6FD05E78926C |

## Validation artifact hashes

| Artifact | SHA-256 |
|---|---|
| Focused XML | F4F98E7B15FD67E5CBB673239FBDCEBF27BE1BD47F93E67B9C26F1DE523746D5 |
| Focused compressed log | 9DF6666271E17E2D3B41218B6573B3F65B3BC2CE4C8216E934E42082CC669B8E (raw A6ACF1C52A2CA3B2287385AE0AEB040C692722F7271AB8A8BA1B64CA113F468C) |
| ALL EditMode XML | 334ABB4B826BC1292A0DD3ABDBEF7F8EDA615CC499A1112B12B490FB994A026F |
| ALL EditMode compressed log | FEC4B3180F7F6170A1190D7DC5E24CDFD4989E4AE90FCD66003B834C01368394 (raw 743F74E6936AFFAB4C11A00DF368A01D53B5ABF5FE4A31F16F3E8BE4BF6E9C79) |
| Official Smoke XML | 14B68F99FA1D984F4ADC2130A0F83A713D61A77FCDAE58F66381B8EEEE692D08 |
| Official Smoke compressed log | C5457836E75CA47B6A48E4FEC3682F1DE09E1CC27D0EC12DA86F7F8DE5845E2A (raw DFF6A2600956E887F25FB28F5CAC33AD8E05BDC759B9ED26FFF86511D3AF296F) |
