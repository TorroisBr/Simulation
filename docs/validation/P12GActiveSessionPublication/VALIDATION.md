# P12-G Active-Session Publication Validation

Implementation candidate: `02d2d40d197985085c2d2370ebc1b26d9b0dbec5`  
Candidate Git tree: `26625801b8df7e097f52db1ccd4ac39633ee3e5c`  
Candidate `Assets` tree: `51551080fe397606acea273787f4e515ff756db3`  
Base canonical: `8f5ec2cb9211a6520f31e65e83450d5bd9bfb039`

Every run below used `Tools/UnityValidation/Invoke-UnityValidation.ps1` in
EditMode after the final implementation and test edits. Results are retained
under `Raw/`; passing Unity logs are archived in `p12g-unity-logs.zip` because
Unity emits trailing whitespace in its logs, which would fail `git diff --check`
if committed as plain text. The original local log files remain unchanged.

| Validation | Result | XML | XML SHA-256 | Archived log | Log SHA-256 |
|---|---:|---|---|---|---|
| `SimulationBootstrapCompositionTests` | 26/26 | `Raw/Focused-Bootstrap-Final/EditMode-20261010-000231-a98d47847fa94fb2bfaaaf584defcc09.xml` | `D7C3C4527A53B7B576AC7D538257037BE2EFD2CDD1863935A99D2FC4C361EB91` | `EditMode-20261010-000231-a98d47847fa94fb2bfaaaf584defcc09.log` | `A30BA4D2C2A96CE0BFDA4EA9A7DA5B417093B441B430FCF54E06EFC88A5AD603` |
| `SimulationRuntimeAdmissionTests` | 75/75 | `Raw/Focused-Admission-Final/EditMode-20261010-000248-26dcb1e457994c5086cd0f4c40af5842.xml` | `1C5E26E5ED9387B3DDBED96B20F72EE4B95128C3042F2418293DA2CAA8F65ACE` | `EditMode-20261010-000248-26dcb1e457994c5086cd0f4c40af5842.log` | `DA7FC2839CA56266477C24A5CF5FD01CA0B127E9A85372C85BFE8B0F28177BB7` |
| `P10BGeneratedRuinGenesisTests` | 10/10 | `Raw/Focused-P10B/EditMode-20261009-235932-fafdc02808034c628c2b8b63d4c0a4ab.xml` | `194504978EE7CC748DB6B388E216EFA0BC5DE9ABA86C9393858EA810AB83DC9A` | `EditMode-20261009-235932-fafdc02808034c628c2b8b63d4c0a4ab.log` | `A35D04A5BEFF18DF05A195A3136C663D5F2F3ADB5185584949FA1040577906BE` |
| `P12CrimeSocialAppraisalInvalidationTests` | 12/12 | `Raw/Focused-P12Crime/EditMode-20261009-235952-571fb78af1cd46448894d082d9bff91a.xml` | `D25807EFB0D0BC5DC6E4E77BF6889E516C79D5C8594A674505E9C4B83AA5CA90` | `EditMode-20261009-235952-571fb78af1cd46448894d082d9bff91a.log` | `77B5B7C2845E8D63B7F378400722B7C459E499DC5384434D534AECF097ACE99C` |
| ALL EditMode | 2738/2738 | `Raw/All-EditMode/EditMode-20261010-000007-4f4154f7f69f48fb8f01378365db460b.xml` | `7A1450ED092C6BAE38D9D4D8E7F2BEE4455E44A44F1A5C3623FFA7AF9992259C` | `EditMode-20261010-000007-4f4154f7f69f48fb8f01378365db460b.log` | `0484E70472CDF4A358FA98C4AA0A4BB2DEEA319E17828854DBDB651932D8AE4C` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 | `Raw/Official-Smoke/EditMode-20261010-000046-5f09586342e74adb8a71727578677f98.xml` | `A7BBE5C96FB06F66C228A013B0D2632527BE8F26B4801AE33F26D8D4ADA311CD` | `EditMode-20261010-000046-5f09586342e74adb8a71727578677f98.log` | `9EAF6CB832506242FA47CD6D58A2174FA05A0083F78788C23F575052595439C4` |

The archive SHA-256 is
`4629BB0B69DFBDA8E548B62F631E2679F481F8C3197AD6E1D61BF1ABE3BE1B05`.
`git diff --check` passed for the candidate patch.

## Scope boundary

This candidate provides one active-session reference containing the current
composition and runtime/reporting aliases, routes published consumers through
that reference, releases bootstrap aliases after publication, and adds a
serialized owner-thread exchange seam for a separately built restored
Daily-v1 session. It does not implement a restore coordinator, full graph
staging/validation, export/hydration, whole-graph failure atomicity, no-replay
or continuation-parity proof, complete owner coverage, P12-G readiness, P12-A
readiness, P13 readiness, or Phase 12 closure.
