# P12-G ScheduledDirective and TravelParty continuation parity validation

- Canonical base: `4804fd7b732f6069083d8c413593eff004173569`
- Test implementation commit: `66679ec607b29ab21cb6003966ff8112c84d5908`
- Implementation Git tree: `d8cc2f729d5b7ab32e420b96bc83e9dc0f995d53`
- Validated `Assets` tree: `4baa557a00f14c71abcdcd51ea430c2c773695fc`
- Changed executable path: `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` only
- Unity Editor: `6000.3.9f1`

## Evidence scope

This test-only slice combines the existing future ScheduledDirective and active TravelParty fixtures in equivalent uninterrupted and restored Daily-v1 runtimes. It restores from the first completed boundary, verifies the directive remains pending and the party remains active with identical IDs, member balances, travel progress, sequence/random/allocator roots and complete included-owner projection, then advances both runtimes through the directive due boundary and TravelParty arrival. The directive targets an unarrested NPC, so the ordinary action eligibility path produces no EscapePrison action and the directive reaches terminal `Skipped` state on day 5. The test compares the complete included-owner projection after each completed boundary. It proves parity through due-date terminal-skip processing and party arrival, not successful action dispatch or action-effect parity. It adds no production seam, domain behavior, serialized owner, or gameplay semantics.

The test demonstrates one combined directive/commitment continuation fixture. It does not close the full compatibility/cross-section matrix, universal causal no-replay, all-owner parity for every state combination, owner/provider inventory, writer/epoch coverage, runtime-wide quiescence, capture eligibility, P12-A export/hydration, P13 readiness, or Phase 12.

The worktree contained pre-existing user changes in `ProjectSettings/EditorBuildSettings.asset` and `ProjectSettings/ShaderGraphSettings.asset`, unrelated untracked `.meta` files, and prior raw validation output directories. They were not changed or staged.

## Validation

All retained XML reports have `result="Passed"`, zero failures, skips and inconclusive tests. All runs used the same validated Assets tree listed above.

| Gate | Result | XML SHA-256 | Compressed log SHA-256 | Decompressed log SHA-256 |
|---|---:|---|---|---|
| Focused new case | 1/1 | `BCE1D0DFDC17E37B3060511ED3E9AB3B33E2A638981B370ABBDB0DEA0E0FFE03` | `780CEF6EE2B1FA68F3A9C787D68BA97313FDC689C90C4065FFB5EAC829C94B15` | `8719BA6D2D3AAB12CA05DAC9E77096266F2AA6B1CACB46800937F9B7C76815D5` |
| `SimulationRuntimeAdmissionTests` | 190/190 | `0681996AD4C5F021C791E9D4B643DFC5DA3BAA89D6992BE054E6E88AFCBB20C5` | `27C6EAA39495D0921265E9A96171C4E26E5E28EAB96E43FF9B68B19ABC012CAC` | `C6294FEA14C65D4C2413CC5FACBB8F566A6CEA54E2CDF0935ADD53F3A3544129` |
| ALL EditMode | 2864/2864 | `49656D9A03AB705A2D88709BED9F5063BDCAF395584EBEC7EA5BEE6675986300` | `0B24A28769167A2112882945D62ED981AE0AC439723E2375FE14431C24273EE0` | `D1D43C22D741CD9879AF5B47734F652AAAF9EF246B9BE202E46D2DEDA531553B` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 | `0BB8E7E104610CE4BC5353E60A761EC249EA43B47C710A157652D88F4AD6BC79` | `C7F4FEE86DC00DFBB3D8EA518A26C8AC1635A5FBE2EB423E42FB4866F891EDA6` | `93D3F3DA6013CA7ED13F141556A543E52A529EEBCCB1A1C45FD256FF4EF4E763` |

`git diff --check 4804fd7b732f6069083d8c413593eff004173569..66679ec607b29ab21cb6003966ff8112c84d5908` passed.

XML and compressed logs are retained alongside this manifest in `Focused/`, `Admission/`, `AllEditMode/`, and `OfficialSmoke/`.
