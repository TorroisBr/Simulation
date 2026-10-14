# CORE-E Implementation Validation Record

Date: 2026-10-14 (UTC)

## Candidate identity

- Branch: `codex/core/CORE-E-demographic-policy`
- Baseline commit/tree: `6950f596cab9f96921290e24a9aeb091229dcc6d` / `fe5ec35243530dc132758ff2ee9198cf2c982ab5`
- Design source identity checked against: `ad7a18899a13ec42a3d2b6b3098dff2f0a318492`
- Validated implementation commit/tree: `2dcd4933f214d797d4e064b6cea26cdbe5bd5cba` / `17543d1f9f318a77b5e6155f64c62d769e6b6557`
- Architecture handoff canonical: `7166ed088483c69d37300b4f12528ebb4b27be84`
- The implementation commit contains only the authorized extraction, shared characterization, project links, and CoreValidation input inventory. Architecture documentation and Unity-generated incidental paths are outside that commit.

## Source mapping

- `Assets/_Project/Scripts/Person/PersonId.cs` and its `.meta` moved to `Packages/com.simulation.core/Runtime/Identity/`. The original Unity GUID is preserved: `7b6e0a8b2d5d4de8a1a4c7e9f3b2d601`.
- The aggregate policy contract, deterministic demographic sampler, and deterministic aggregate provider moved into three package files under `Packages/com.simulation.core/Runtime/Population/`.
- `AggregateDemographyFailure` and `IAggregateDemographyRandomSource` remain in Unity's `AggregateDemographyContracts.cs`; `DailyDemographicSystem`, represented-floor snapshot, and diagnostics remain in Unity's `DailyDemographicSystem.cs`.
- `AggregateDemographyContext` constructor visibility changed from `internal` to `public`; no demographic formula or policy semantics changed.
- `Simulation.Core.csproj` has exactly four new production Compile links. `Simulation.Core.Tests.csproj` links the single shared characterization file. Unity assembly definitions were not changed.
- Shared test: `Assets/_Project/Tests/EditMode/Editor/Core/DeterministicDemographyPolicyTests.cs` (12 NUnit tests), with its Unity meta. `Tools/CoreValidation/CoreValidation.psm1` input inventory now includes these source, folder, meta, and test inputs.

## Characterization

The shared 12-test characterization passed against unchanged production sources before the move (run `00d5a225dda0411db7f11d5d5925e084`) and after the move (run `855161cd0a414db0b88511e29c265a71`). The captured vector records were compared byte-for-byte; expected sample bit assertions also passed.

| Case | Expected sample bits |
| --- | --- |
| Person, ASCII ID, day 0 | `3FDF60128CC9CC74` |
| Person, surrogate-pair ID, day 42 | `3FBCA3AC1E0A24B0` |
| Person, null ID, negative day | `3FE0000000000000` |
| Person, long.MaxValue day | `3FE4F04DF1F4E696` |
| Settlement Birth, day 0 | `3FD9FA9DB4FF45DA` |
| Settlement Death, day 42 | `3FE1F14E570A7ADE` |
| Settlement Birth, surrogate-pair ID, day 7 | `3FD805461D3A02F2` |
| Settlement null/empty/whitespace ID, negative day | `3FE0000000000000` |
| Settlement Death, long.MaxValue day | `3FD6D839F9750656` |

The original-code run and post-move run each passed 12/12. Their private result XML hashes are `d5a76436bafb358e1438acd12d24d80f51daf2ceacbbfcfd9d22cda07f2d1aca` and `3eb68fecf83deccf62ba2b9a26898cf6d73959b6d26c55c2d71305e4678ce0ce`; private log hashes are `156f45e386f037bb319539aa971e7bf76ccce8fddd3f91a756cc2fab25fdccb3` and `5cc6cca36e3a7ae25969906fa2128af4e71da87528c9ff41c957230d08d7668a`. Both initial characterization manifests report `inputStable: false` because they ran at the uncommitted baseline with the test file and temporary source restoration; these are private characterization records, not packaged release evidence.

## Unity environment and gates

Unity validation used Unity `6000.3.9f1`, Windows 10 build `26200`, x64, PowerShell `7.6.5`, .NET runtime `10.0.11`, culture `pt-BR`, and time zone `E. South America Standard Time`. Evidence was captured by `Tools/UnityValidation/Invoke-UnityValidationWithEvidence.ps1`; original logs and XML remain in owner-only private evidence storage.

All selected suites passed with zero skipped cases:

| Gate | Run ID | Cases | Private manifest SHA-256 |
| --- | --- | ---: | --- |
| AggregateDemographyFoundationTests | `40b2d94e56a4486a8e90a92bf6de64b9` | 25/25 | `04E6532E49D0AA77761CA871A580C1F1AD10C281F6FB5EFD5C0C2767F5E2A8CE` |
| DailyDemographyIntegrationTests | `bd18df06c2244c45975565bfcd39b01a` | 4/4 | `D031693168787FC165F6EE3C309B60D8D393498BC81EFE80D132A400A51C8449` |
| DailyDemographyBoundaryStepProviderTests | `eb4e362ea28548febcfe6b298615b84a` | 4/4 | `F6EF4FCEE4C48C5665A53B8F6472C26A5599736D3997435A5C3626003C616DF7` |
| PersonNaturalMortalityFoundationTests | `21fce07be76d4df29566a03d650e230c` | 15/15 | `DAC9458CD735625B169D3E0ED173AF9DD63774EC88439D30DD7184AD56702DA1` |
| PersonDemographicAgeTests | `142096419fa348c6b995f85f227d86ef` | 16/16 | `585F16C97E96EB4824A29BA81AEEFA922DB1FED177499CD4702E4BF73EA7B4E1` |
| PersonNamedBirthLifecycleTests | `61e44aa905a54d449eb862f2a88a473b` | 15/15 | `49A7C07D567B06D5FEDEA8D7B59362BA5576C1C65981AC701359C92B23FAD4AB` |
| PersonFoundationTests | `0667735016c04e7c89043a0c1720f02f` | 11/11 | `B818EF33D7FB3EFB38283F107A3E63C053ED4FE0CCAE6F4E0C3D18FA1E8AEDA7` |
| PersonStoreCensusTests | `27e64a248c6c4333a0a1776b20fa4ab3` | 7/7 | `AA8F2562F75670741DB5279BB6B6B756F24A431238DFCC55F2EC00B91034EB6D` |
| P12CP9GenesisManifestSnapshotTests | `f8ee265fa8484a34af5086a43ae4e5f4` | 3/3 | `1D8785BFE02706A4A39AB284B43343F2F7C6ACCED993A6A2EE178E1CD2706A7E` |
| P12AContinuationCodecTests | `6b6eecc1ff56401f81d500ef025b4183` | 33/33 | `A5C9968072FB3D80D9AD39D721E830228DEB442EF8FA5605838CBE7C46BD4CF6` |
| P12EDailyV1OwnerPackageTests | `01ee26eff7a24c4db71a8c44c6405d8e` | 6/6 | `97D6945919F58D7F6332154D1A0215A43BE925C2B76635AAFE2F953D62D2A151` |
| SimulationRuntimeAdmissionTests, including restored admission, persisted detached restore/continue, and invalid-save fail-closed cases | `7462072c99684642823ba0162e4ed2e1` | 205/205 | `85D874ED032B13D8C55E75A8B0DCEB6E4B84F353D729338099250DCDC88ADB56` |
| Full ALL EditMode | `158999113c994fb1a9d5f77967acbbfa` | 3,132/3,132 | `802D984F87177F32C9AB09DF48D9CD19E7A31F01EC299BC2632C8069F175AA08` |
| Official Smoke EditMode | `9de24905f31f40c9bca5d64eb2a43e48` | 5/5 | `5CC527B8EAA0144C35E92B87BB2D1D14CB31E27A4A37CEA9E0E0E8A9B47A7FA0` |
| SimulationRuntimeLongRunTests | `12ff5c05b9c5469bb36b5d1ead9adead` | 7/7 | `3E160BAE026D0BBAD07615565DE9641393AB076C652DEB01DB7FA6ABB24D6BFD` |

The unchanged LongRun source includes the original 100-, 1,000-, and 5,000-day cases. It was neither edited nor shortened. The focused 205-case admission suite exercises restored-boundary rejection and admission; full ALL EditMode also passed all P12 tests. The test source asserts persisted Daily-v1 save, detached restore, and identical continuation, plus invalid persisted-save rejection before genesis.

For the focused package-source gates, full ALL, Smoke, and LongRun runs, `inputStable` was true and each manifest records baseline HEAD/tree above. The two characterization runs deliberately bracketed source movement at that same baseline commit.

## .NET validation and packaging

An exploratory CoreValidation run on the E Unity worktree passed cold/warm Core and locked cold/warm Lab, with 125/125 discovered tests each Core pass and 12 new characterization tests included. Run ID `a9a43788d5a5424d9dc7a58a04214e24`; manifest SHA-256 `1258D39993134FBC0A80CC161E279F1B6DFBE107684C2398CF82D39D5356987F`; cold/warm TRX SHA-256 values `18A0DB6BB96E1F77071E73CEE345030587059BB2CB326582796B0337665A9E00` / `4404DAFA87ADC8D00CDEA86696A476036F03CE207549E0AFA646A8BC43075B76`. This run is diagnostic only: Unity had created Library and incidental local files, so it is not the supported packaged evidence for the candidate.

The final supported validation ran in the clean .NET-only worktree at commit `2dcd4933f214d797d4e064b6cea26cdbe5bd5cba`, tree `17543d1f9f318a77b5e6155f64c62d769e6b6557`, with no Unity `Library`. CoreValidation run `94a4b82c74494b8fa3f94f7094fa55be` passed cold and warm with 125/125 cases each, zero non-passing outcomes, stable source/generated inputs, pinned SDK `9.0.315`, selected CLR `8.0.28`, target `net8.0`, Windows 10 build `26200`, and x64 process/OS architecture. The private runner manifest SHA-256 is `15b26e4d0d5165ee55a4eb869bda7765be5a16a275c72151517218c930672b03`; its complete input digest is `a011b76010ca6e330126fa65619c3c75ff0abf47958dd6575a7f06d3e59b9d92`.

| Attempt | Original TRX bytes / SHA-256 (private retained original) | Normalized TRX bytes / SHA-256 | Semantic SHA-256 |
| --- | --- | --- | --- |
| Cold | 175,106 / `f8876468510dff386a979b24dac77b5e49ba555394ee48ef6421bb6bfda2ca95` | 153,417 / `a5d4fc604a81cd35c08e01f80147eebca9a2c40bbf151ab769cd03b4bf184df6` | `a5d4fc604a81cd35c08e01f80147eebca9a2c40bbf151ab769cd03b4bf184df6` |
| Warm | 175,106 / `080929376c20d7c89b8b1167cc8b10d3f8fbd67781fe3fb5807278e835c55845` | 153,417 / `c8a36ce6cbb0a4722fbc00ec8e90cc5e6f8c7b5df7cc7e915b4bb96e8f0e1953` | `c8a36ce6cbb0a4722fbc00ec8e90cc5e6f8c7b5df7cc7e915b4bb96e8f0e1953` |

The promoted `DotNetTrx` adapter verified the cold/warm originals against the complete normalized projections (`originalComparisonVerified`). The public package has exactly the two result-only TRX entries and contains no absolute drive/UNC paths, user-home names, or `AppData` references in its manifest or decompressed entries. Archive SHA-256 is `4b74838a29be7fbe63eb83aaff2dc8e015e54931c6011a4cd09dae89d827628f` (55,255 bytes); manifest SHA-256 is `72bdcb177ca05af1caa5f51a7e9dfe2e928e7fc51842ee668dd3a17f051f91e1` (53,352 bytes). Both are retained at `docs/validation/CORE_E_DemographyPolicy/`; archive-only verification and private original comparison passed. The source/tool identity recorded by the package is commit `2dcd4933f214d797d4e064b6cea26cdbe5bd5cba`, tree `17543d1f9f318a77b5e6155f64c62d769e6b6557`.

Standalone locked Lab validation `c9440f9d14f0463187a13e9b83982fca` also passed at the same candidate commit with no Unity `Library`; cold and warm output hashes match. Its private manifest SHA-256 is `ca3afb075d57ac366b9c55463f436f9a59353659b4223deac7dfa4425b458c58`.

## Repository hygiene and remaining gates

- `git diff HEAD --check` passed after the implementation commit.
- The E worktree's remaining status is limited to two Unity-mutated protected ProjectSettings files, three unrelated generated `.meta` files, and imported architecture documentation. None is included in the implementation commit.
- Independent implementation review and canonical promotion remain outstanding. The package and this validation record are not yet part of the implementation candidate commit.
