# CORE-F P12 NPC Knowledge and Plan census validation

Status: validated candidate checkpoint; not Core canonical promotion or full CORE-F acceptance.

## Implemented boundary

Core now derives the nine required per-NPC Knowledge/Plan census witnesses from the exact admitted owner graph: seven Knowledge families, MerchantTradePlan, and TravelPlan. The LocalTopology pair retains one exact-empty owner with zero cardinality and revision. Adventure Intel's four rows retain one shared owner and revision. The two plan rows retain singleton census cardinality, including when empty.

Live capture and both restored-vector gates rebind these rows before P12 validation. Missing/duplicate rows fail closed; stale source snapshots remain available for existing P12 token classification; mixed rosters preserve the full host vector unchanged. No profile content or mixed-roster behavior was expanded.

## Source identity

- Validation base commit/tree: 95920611fa453ac23543490eb3fa0fc364eaca34 / 06936f83d35619f04eeeb7ad75a67a3894b7d37b.
- CoreValidation working-tree input digest: d0ccf9681722aee51fcb2ecf99d570caf4f6fa857092351030e344f8a3a8f3e7; input stability: True.
- The changed source/test file hashes below were checked against the official CoreValidation input manifest after all validation runs.
- Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs — 113315 bytes; SHA-256 ea16493d15b46cfb8a880b90f9c20ba4c923267d69a6929b540681af36974539.
- Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs — 130006 bytes; SHA-256 4a0383d22a461a714ee4e4bce971210a8ba5a7c89727eef6062078cf82d90de9.
- Packages/com.simulation.core/Runtime/Person/DailyV1NpcOwnerCensusProviders.cs — 31088 bytes; SHA-256 a17710032df67298f543c572210c92aea8a60bf5bc86aebd25aa43b9413047f7.
- tests/Simulation.Core.Tests/DailyV1CityEconomicOwnerCoreTests.cs — 147223 bytes; SHA-256 a7b71ae301d33db42f3cdd6037a06df8950ebe9345f46dd23367b8d824fa6d2a.

## Results

| Gate | Run | Result | Source identity |
|---|---|---:|---|
| CoreValidation cold | abf450dd4c4f496581863a1671b44d56 | 982/982 PASS | Stable Core input digest above; original TRX is retained in protected local evidence. |
| CoreValidation warm | abf450dd4c4f496581863a1671b44d56 | 982/982 PASS | Same run and Core input digest. |
| Unity ALL EditMode | ec14b024e5534f02be54e5ee880a2113 | 3284/3284 PASS | Stable Unity input digest fd95cfadc992be1f07baf829ab9d0e05a05cdb320d32bc36aa34ce74814282d4. |
| Official Smoke (EditMode filter) | 18694b29eaf7486894997681e68fbdb9 | 5/5 PASS | Same stable Unity input digest; all five cases are also in ALL EditMode. |
| git diff --check | final implementation/test diff | PASS | No whitespace errors. |

The sanitized result-only package coref-p12-npc-knowledge-plan-editmode.zip is verified against the protected full EditMode run manifest. Archive SHA-256: ab7980540b458bca71232fce83bff207ba938a7c11b756e4fff0d25ea46bd813. Public package manifest SHA-256: 76f5acd236b9f981ba6861871b10cc01fca2042dd0a6c8f17c1e65988f49404d. The normalized report XML SHA-256 is e47aa6a8f5699cdc357f7a2eb0909e970e8cc5251e5d996af2b07881bd36afb3. The original XML/logs and full before/after input inventories remain protected locally; the filtered Smoke XML remains private and is not substituted for the complete report.

The official CoreValidation manifest SHA-256 is 2d1d988b961d41cd28b6fd3281b35464b004b2eff12358803246a6e7aa25d582; cold and warm original TRX files remain in protected local evidence. The Unity and Smoke private run-manifest SHA-256 values are ea9a27cf116d9527943404abd7b20339ba4ada5b0c598b0b3bba659b98ed8697 and 76928f42349efccf1dc67091027914f30df503d865db3cdb30e8a80b33718c7c. No original evidence was modified.

This checkpoint does not claim P12-A readiness, P12-B completion, complete CORE-F runtime integration, or detached .NET continuation.