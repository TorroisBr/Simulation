# CORE-F Selected NPC Inventory and MoneyAccount Census Binding

## Scope

This checkpoint binds the selected Daily-v1 Core census to the exact live `Inventory` and `MoneyAccount` owner instances of each selected Core NPC. Both rows are required; the MoneyAccount row must have cardinality exactly one. Providers report their current owner revision and identity.

Unity's existing census registrations remain the compatibility projection. When Core registrations are created, selected NPC Inventory and MoneyAccount rows use direct Core providers. Unselected or legacy NPCs continue to use their existing Unity compatibility providers. Dynamic roster changes stage the Core provider map and publish it only after the Core census/epoch transition is accepted.

This is a partial census composition capability. It does not claim complete Core-owned composition of all 89 P12 families, Core-only startup, P12-B completion, capture eligibility, detached restoration, or an independent .NET host.

## Validation

- Official CoreValidation: cold and warm runs each reported 1,186/1,186 passing. The original reports and runner manifest remain in protected local evidence storage; they were not copied into this public package. This runner execution had the Unity `Library` available and is not evidence of a Library-free host.
- Full Unity EditMode: 3,325/3,325 passed; failed, skipped, and inconclusive counts were zero. Run ID: `304ce3e593ec41cc97424779e632b608`.
- The five Smoke cases passed 5/5 on the EditMode platform and are included in the full EditMode report. The separate filtered report was retained privately because the packager rejected that partial report's suite-level count; it is not represented as a separate public package.
- `git diff --check`: passed before checkpoint preparation.

The public Unity package contains the normalized full EditMode XML and its manifest. The manifest records input snapshot SHA-256 `a238e9f2fbe1b9a4ebbca31d68c52da3916cd40d3e20ef0894e3823bb46d7a57`. Package verification passed with one run and one report entry.

- Archive: `Unity/coref-npc-inventory-moneyaccount-full-editmode.zip`
- Archive SHA-256: `a750016e620265ae92bed88a4522e091fcdd9ff4c104afaf167a9886b990cbd2`
- Manifest: `Unity/coref-npc-inventory-moneyaccount-full-editmode.manifest.json`
- Manifest SHA-256: `554af45724c9fb6c7c5102b6015b0fbaDE097551a3a8b7bd04ff1b03cad7a465`

The manifest's input snapshot matches the current tested Unity sources:

| Source | SHA-256 |
| --- | --- |
| `Assets/_Project/Scripts/ContinuationCensusProtocol.cs` | `20f437663f3abeb0f60da3f903873a6fa440ae9c7fd2960383cf8795876d69f4` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs` | `6bca3432a248484792008ee59c0c5f06ab5ea96b85c75286ca2678ca8b0a2c41` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs` | `b2b2de61371b0679708cd73b946da5f63ddbba0af1d0b1600443507218cc3000` |

## Review

Independent implementation review passed for the current source. It confirmed that selected NPC owners are represented by direct Core providers, Unity compatibility providers remain available for mixed rosters, dynamic membership updates are staged, and identity/cardinality/revision checks remain enforced. A committed exact-tip review is required after the recoverable candidate commit.