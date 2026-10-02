# FR-C Factual Reader — current-base candidate

**Status:** `CURRENT_BASE_VALIDATED`; exact-tip implementation review `PASS`; `INTEGRATION_HANDOFF_READY` for Phase Master consideration. This is an additive candidate based on the current Phase 12 canonical. No numbered Phase canonical ref was moved.

## Identity and dependency state

| Identity | Value |
|---|---|
| Candidate branch | `codex/frc/frc-factual-reader-current` |
| FR-C code commit | `ec042b30b1c0a390f611c47cb22b75631cfb9556` |
| FR-C code tree | `fabb182e79bf7c36035b791736edcb42785056ac` |
| Exact Phase 12 base | `0f36331d84ad3139171d36f980dfe6fa635ae30c` |
| Promoted FR-B live integration | `28d33a2c8f10ddb724819893a0b5f2804a6f5b0b` |
| FR-B code tip / tree | `aeb76c687d00a49f505ab264a58a508a20e4923b` / `862eb904c6679a66d4dd2a2e2ad7f174ec8479c2` |
| Architecture authority | `451340c56e9b676bf6ea43412bcb856b9ccde3de` |
| Diagnostic revision proposal | `64f33fd2c935ff1ab78ccfa08fb3bbecdf4b4c17` |
| Diagnostic acceptance record | `775f3c9c7f11dc3312dd4dbf7ccac424d67f2029` |
| Current-base exact-tip review record | `360dcdf76f69dadb4498dcfab06379621e7ac18d` |

The refreshed `codex/phase12/canonical` is at `0f36331`. It is a descendant of promoted FR-B live integration `28d33a2`, which is also the current-base FR-B integration/review branch tip. FR-B live is `PROMOTED/CURRENT`; FR-C has no remaining FR-B dependency.

The previous provisional FR-C code (`2177f7a`) was based on the old FR-B candidate (`5c43733`). This candidate was rebuilt in a fresh E:-based worktree from exact canonical `0f36331`, replaying the reviewed FR-C changes and preserving intervening P12 work. It was not produced by reusing the old tree as a new base.

## Current-base difference classification

| Classification | Finding |
|---|---|
| `UNCHANGED` | Accepted FR-C fact and diagnostic semantics are preserved. `FactualReadContracts.cs`, `FactualReadCoordinator.cs`, and `FactualReadFoundationTests.cs` had no canonical changes between the prior FR-C base and `0f36331`. |
| `BASE_DRIFT_ONLY` | Current P12 runtime, sequence, census, bootstrap, and test changes were retained from canonical. The old FR-C patches replayed without conflict. |
| `REAL_CONFLICT` | None. |
| `SEMANTIC_REVALIDATION_REQUIRED` | Runtime registration and bootstrap behavior changed. They were validated on the recomposed exact tree by the focused, bootstrap, full EditMode, and official Smoke gates below. |

## Delivered scope and runtime integration

`FactionFactualReader` provides the `simulation.faction-truth/v1` capability. It returns copied immutable Faction and active-affiliation facts, preserves source fields, represents blank names as absent, validates active Person endpoints and Faction endpoints/join chronology for active and ended affiliations, filters ended affiliations from current facts, and applies deterministic ordinal/numeric ordering. Results retain no Store references. Capture remains behind FR-B admission and its boundary/revision checks.

The failed-capture path exposes a sorted immutable diagnostic collection while keeping factual results `Unavailable` and discarding partial values. Reader exceptions map to a stable generic diagnostic without exposing exception details. Diagnostics must identify a requested capability whose result is `Unavailable`.

The only production runtime integration file changed is `Assets/_Project/Scripts/SimulationRuntime.cs`: the runtime now registers one `FactionFactualReader` in its factual reader set. `Assets/_Project/Tests/EditMode/Editor/SimulationBootstrapCompositionTests.cs` now tests the registered capability, including recognized-but-unavailable behavior before admission and `Unsupported` for an unknown capability during an admitted capture.

`FactionStore.cs`, `PersonStore.cs`, `FactualReadAdmission.cs`, and `SimulationBootstrapComposition.cs` are unchanged. Existing P12 sequence initialization, FR-B publication/health checks, owner-thread checks, logical-boundary capture, and exact-store revision checks remain in place. This scope reads no Knowledge, support, presentation, or World Exchange data and defines no External collection-completeness semantics.

## Exact-tree validation

These gates ran against code commit `ec042b30` / tree `fabb182e`. Results are retained under the ignored local archive `Library/ValidationResults/FRC-CurrentBase/`. The archive hashes were rechecked before this record was written.

| Gate | Result | XML | XML SHA-256 | Log | Log SHA-256 |
|---|---:|---|---|---|---|
| `FactualReadFoundationTests` | 9/9 | `EditMode-20261002-231611-bc51a5c2451448a3812eca819038a620.xml` | `1CBDE83DFECD749C9C1596FA5C2CC433F68E8A753D26630DC5C39A21EFEB5729` | matching `.log` | `C6076125DE84B0C0FBA8FF73F2EE1BA93905FFB8700921547B56F5C1C5F6F120` |
| `FactionFactualReaderTests` | 7/7 | `EditMode-20261002-231628-1ed80753928f4e4789d7a04e6eb4861c.xml` | `3B671120D4FEAE06055C4E5CD85408B171AEBC85C7808A2117DEFCD704CB4E2D` | matching `.log` | `3DC191A3D4B545CB923AB49A03A92ABD910CFE0F54490E4E611544857FF91386` |
| `SimulationBootstrapCompositionTests` | 21/21 | `EditMode-20261002-231644-c1115d6de99340ccac5bbb48bee29b7f.xml` | `D2D3BA841C18ECF59FE30FD4D386A237F6742A46BB27012F38F73712EC3E4F81` | matching `.log` | `04DD407CDEC389B7EAB95C796C2A8C100761ED9B5B879553CAD22B6033BD2514` |
| ALL EditMode | 2190/2190 | `EditMode-20261002-231700-14e1530f864b495ca528449d00001116.xml` | `5F33479FFB89EAE6A5FC0909CC71400DBEAADA57B558013F76E52A165FAFAC4B` | matching `.log` | `490BA2108B600BBEB71FBA1E3C570147096072818E150216FCB928A82A7FD402` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 | `EditMode-20261002-231738-5b9349305d5b4fb3a8fa31afa69f72e3.xml` | `3575333AEDB6A41CBE35D0380EDD1925F323D7DC9A9EE75893F45300710AC894` | matching `.log` | `02AB01BDA6F65CA24E7352631EBD2942E6F21E010FD180DD8D3E430E72D48E55` |
| `git diff --check` from exact Phase 12 base | PASS | — | — | — | — |

The zero-test `-TestCategory Smoke` invocation is excluded; the official `-TestFilter Smoke` gate above ran 5/5.

## Exact-tip review

Independent read-only exact-tip implementation review passed for code `ec042b30b1c0a390f611c47cb22b75631cfb9556` / tree `fabb182e79bf7c36035b791736edcb42785056ac` against exact base `0f36331d84ad3139171d36f980dfe6fa635ae30c`. The durable review record is `docs/design/FRC_FACTUAL_READER_CURRENT_BASE_REVIEW.md` at commit `360dcdf76f69dadb4498dcfab06379621e7ac18d`. The reviewer ran no tests; all validation evidence above was executed separately on the exact code tree.

## Integration handoff shape and limits

The candidate is a clean descendant of exact base `0f36331`; its source and documentation changes form an additive fast-forward candidate while that base remains canonical. Phase Master retains sole authority to integrate it and move numbered Phase refs. If canonical advances before integration, exact-base ancestry and touched-file compatibility must be refreshed.

FR-C supplies a factual read capability. It does not complete P12-B, provide a global owner/thread quiescence guarantee, prove complete collection coverage, define expulsion or membership policy, or produce a World Exchange payload. WX-D still requires canonical/promoted FR-C and an approved Simulation-External collection-coverage contract.
