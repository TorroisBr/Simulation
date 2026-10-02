# FR-C current-base integration handoff

**Status:** `INTEGRATION_HANDOFF_READY`

This handoff is for the Phase Master. Phase Master remains the sole writer of numbered Phase canonical refs; this work did not move `codex/phase12/canonical`.

## Exact identities

| Item | SHA / value |
|---|---|
| Exact current Phase 12 canonical base | `0f36331d84ad3139171d36f980dfe6fa635ae30c` |
| Promoted current-base FR-B live integration | `28d33a2c8f10ddb724819893a0b5f2804a6f5b0b` |
| FR-C code candidate | `ec042b30b1c0a390f611c47cb22b75631cfb9556` |
| FR-C code tree | `fabb182e79bf7c36035b791736edcb42785056ac` |
| Candidate documentation commit | `4a083e30efffd3761128ff5bdfb7b606397c74b0` |
| Independent exact-tip review record | `360dcdf76f69dadb4498dcfab06379621e7ac18d` |
| Architecture authority | `451340c56e9b676bf6ea43412bcb856b9ccde3de` |
| Candidate branch | `codex/frc/frc-factual-reader-current` |
| Review record branch | `codex/frc/frc-factual-reader-current-review` |

The Phase canonical at handoff is `0f36331`, which descends from promoted FR-B `28d33a2`. FR-B live is `PROMOTED/CURRENT`. The FR-C code commit is a descendant of the exact current canonical base.

## Validation

All gates below were run on exact code `ec042b30` / tree `fabb182e`. Current validation files are retained in `Library/ValidationResults/FRC-CurrentBase/` in the candidate worktree. The XML and log SHA-256 values were rechecked before this handoff was written.

| Gate | Result | XML SHA-256 | Log SHA-256 |
|---|---:|---|---|
| `FactualReadFoundationTests` | 9/9 | `1CBDE83DFECD749C9C1596FA5C2CC433F68E8A753D26630DC5C39A21EFEB5729` | `C6076125DE84B0C0FBA8FF73F2EE1BA93905FFB8700921547B56F5C1C5F6F120` |
| `FactionFactualReaderTests` | 7/7 | `3B671120D4FEAE06055C4E5CD85408B171AEBC85C7808A2117DEFCD704CB4E2D` | `3DC191A3D4B545CB923AB49A03A92ABD910CFE0F54490E4E611544857FF91386` |
| `SimulationBootstrapCompositionTests` | 21/21 | `D2D3BA841C18ECF59FE30FD4D386A237F6742A46BB27012F38F73712EC3E4F81` | `04DD407CDEC389B7EAB95C796C2A8C100761ED9B5B879553CAD22B6033BD2514` |
| ALL EditMode | 2190/2190 | `5F33479FFB89EAE6A5FC0909CC71400DBEAADA57B558013F76E52A165FAFAC4B` | `490BA2108B600BBEB71FBA1E3C570147096072818E150216FCB928A82A7FD402` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 | `3575333AEDB6A41CBE35D0380EDD1925F323D7DC9A9EE75893F45300710AC894` | `02AB01BDA6F65CA24E7352631EBD2942E6F21E010FD180DD8D3E430E72D48E55` |
| `git diff --check` from exact base | PASS | — | — |

Exact artifact filenames are in `docs/design/FRC_FACTUAL_READER_CANDIDATE.md`. The empty `-TestCategory Smoke` selector result was not counted; the official `-TestFilter Smoke` run above passed 5/5.

The independent exact-tip review passed against exact base `0f36331` and exact code tip `ec042b3`. The reviewer ran no tests; validation was performed separately.

## Touched files and composition

Production runtime registration is limited to `Assets/_Project/Scripts/SimulationRuntime.cs`. The runtime integration test update is `Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs`. FR-C adds `Assets/_Project/Scripts/FactualRead/FactionFactualReader.cs` and its Unity metadata, modifies `FactualReadContracts.cs` and `FactualReadCoordinator.cs`, and adds/updates the focused FactualRead and Faction reader tests. The candidate and review records are documentation-only.

`FactionStore.cs`, `PersonStore.cs`, `FactualReadAdmission.cs`, and `SimulationBootstrapComposition.cs` were not modified. Current P12 sequence/bootstrap/admission work is retained. No direct mutable Store exposure was added.

## Integration shape and scope limits

This is an additive candidate based directly on the current canonical and is a clean fast-forward descendant while `0f36331` remains canonical. There are no unresolved code conflicts. If the canonical ref advances before Phase Master consumes this handoff, the exact-base ancestry and touched-file compatibility must be refreshed.

The candidate exposes copied Faction facts through `simulation.faction-truth/v1`. It does not establish complete owner/epoch coverage or a global quiescence guarantee, define Faction membership or expulsion policy, provide collection completeness, or produce a World Exchange payload. WX-D remains dependent on canonical/promoted FR-C and the approved Simulation-External collection-coverage contract. No Simulation-External files were changed.
