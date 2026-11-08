# CORE-F TravelParty Start Owner Footprint

Status: validated implementation checkpoint; this does not close CORE-F or promote to Core canonical.

## Scope

Core now derives the admitted P12 owner-section footprint for a TravelParty start when every participant is bound to an exact selected Core NPC owner. The Unity host compares that derived footprint with its prepared owner section set. Mixed or unselected rosters retain the existing host compatibility path. The footprint includes the party store and allocator/sequence owners, each selected member's MoneyAccount, TravelState and SpatialKnowledge rows, and each affected City presence row once per City, including the origin City.

The regression covers two selected members sharing a City presence row, unique/sorted sections, exact selected owner identity, and rejection of duplicate or foreign member/City owners. No claim is made that this completes the shared application, detached host, or CORE-F acceptance contract.

## Validated source identity

The runners captured the uncommitted implementation against base commit `45ece0b27ab48c96a79f28d3832a20961ff8cae6` and base tree `48fa7bd936095b899ad86e7c674c91d6487c4bf5`. The Core runner's working-input digest is `3e0b08b69c707ef43b444c271e50178171cb9d5cf18e0462e3131eef5629fdb5`; its source snapshot and the Unity before/after snapshots remained stable.

| File | SHA-256 |
|---|---|
| `Packages/com.simulation.core/Runtime/Application/DailyV1ExecutionBoundaryState.cs` | `bd3b48d8b0669e0be1447360e4f22ad605b7333572c1778113816e10867209eb` |
| `Assets/_Project/Scripts/P12TravelPartyStartOperation.cs` | `52e62bcb741ee8c04e57cf897b3a5d5c9b10eedc20166fd7defe82a0e28ebbb1` |
| `tests/Simulation.Core.Tests/DailyV1SelectedSoloTravelStartOperationTests.cs` | `cb5ca8a17433c5f5da538f6db105670a14c3928182bd51d9ce1f2e2ae48426cf` |

## Validation

| Gate | Result | Run ID |
|---|---:|---|
| Official CoreValidation, cold | 1,057/1,057 passed | `873af367207d4ae5a0a5005b5ccca754` |
| Official CoreValidation, warm | 1,057/1,057 passed | `873af367207d4ae5a0a5005b5ccca754` |
| Unity EditMode, full | 3,300/3,300 passed; 0 failed/skipped/inconclusive | `71e9bb8468f7439fa878a8ebb366aed4` |
| Unity EditMode `TestFilter=Smoke` | 5/5 passed; 0 failed/skipped/inconclusive | `46dc3e2789754e358ca990938a9464ae` |
| `git diff --check` | PASS | — |

Unity was `6000.3.9f1`. The full EditMode and Smoke evidence both report stable source snapshots. Result hashes:

- CoreValidation private manifest SHA-256: `8d1fd0bfe5cc623faf3862dbae26ed1f3b42e1d8ec761a1398ad2b790bf8f4ce`.
- Full EditMode XML/log SHA-256: `8814038f4250293789757797fe94c00a0d4578694b5e53050388145ebae05e69` / `08c4fc79ba8499e136d049ba4949649fd57dbd97cc1f01edfe7ae3e4ad5529a6`.
- Smoke XML/log SHA-256: `e50a6e3729e7c717d434fce2f6fea2cc80d4e6e7a903d8957eea816649edb179` / `d06854a839244512ad6fd0bf49ac315884a180fa7f42ec3ce0457c89b30ba144`.

Original private manifests, XML reports and logs remain in the protected private validation store. Failed exploratory attempts were preserved separately and are not used as passing evidence. No private absolute paths or raw logs are included in this record.
