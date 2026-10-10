# P12-G P8-D populated route-observation target rejection

**Scope:** test-only proof that the selected Daily-v1 restore coordinator rejects a populated explicitly-empty `p8d.spatial-route-observations` target row before publication. The fixture registers the same stable Person in the source/control runtimes, records one valid direct observation about the existing staged P8-A Location in the private candidate, and verifies exact target owner identity, cardinality `1`, revision `1`, and the coordinator's `OwnerCoverageIncomplete` rejection. Existing assertions verify the active source/session/token/health/graph are preserved, the next normal continuation matches the uninterrupted control, and a later valid restore succeeds. This does not claim that populated P8-D route observations are eligible for hydration.

**Base canonical:** `cabf47185bf4b5d157157a4cb48b4f897db25127`  
**Code commit:** `5a36ba5a3d1d27201e52156428ef2da32bbfacbd`  
**Code Git tree:** `59fbb4406a7248e371a768b2aa73b7ada8f1369b`  
**Tested Assets tree:** `fb53856cec5ed6ca36c170aab7eb6dee90ca3d46`  
**Changed test blob:** `27d1be30ba8aa02d02f476c8f737021e20c80b78`  
**Unity:** `6000.3.9f1`; runner: `Tools/UnityValidation/Invoke-UnityValidation.ps1`.

| Gate | Result | XML artifact | XML SHA-256 | Compressed log SHA-256 | Uncompressed log SHA-256 |
|---|---:|---|---|---|---|
| Focused `DailyV1RestoreRejectsCorruptedRootOrOwnerVectorAtomically` | 22/22 PASS | `FocusedRaw/EditMode-20261010-195920-6ebcf199feac4657ab6e108946dbeee7.xml` | `069DAE215F280351FA3C86EB9C7BC27E26F8335EE8D8400709CD999101FCAF37` | `DFAFB36C69CD6BCD1F5F2CAEFA717570EC73F9AF0CAF702AD4FF8A6112F47E8E` | `F62CFA563574A45F6B69C699E18EE24662DC8AABB0BAA13FA014F431F42337F2` |
| ALL EditMode | 2,848/2,848 PASS | `AllEditModeRaw/EditMode-20261010-200013-4ab1f8a0f2464c4baa377c2fb4de6ff3.xml` | `C96182EE9FA9B049692AE78D7174CFDDE7188B853EF534B635B70E4B8FD1090F` | `076975EF22499EAD187AB2F28EABABE160241E259500A2A92E36980E4E86DE00` | `1684830A5A3211BABE4205766A3540A8433D1F61870E7378AA7F1307C53704CB` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 PASS | `SmokeRaw/EditMode-20261010-200105-09f1815f4aea47ef96728086a1cdd43d.xml` | `D394F917044676F34CBCD07FADC1C17B695CC9E585A6C25D2EE0A558285AF528` | `0BA365FD1DC4136ADF8CA9683AFEDBA23F71E94926D52A4E62E75910E00666E0` | `641E256108DB87765B6F8768A064C3F2CFABB780AA9DA15A0D3B2E06911970A7` |
| `git diff --check` | PASS | — | — | — | — |

The compressed logs were decompressed and their SHA-256 values verified against the original runner logs before the uncompressed copies were removed. The focused, full EditMode, and Smoke results were produced after the final assertion adjustment and match the committed code/Assets tree. No production code or profile configuration changed. ProjectSettings and untracked `.meta` files were excluded from the candidate and were not staged.